using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class HodAuditSummaryTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public HodAuditSummaryTests(FormSaveWebAppFactory factory) => _factory = factory;

    static DateOnly WeekStart(int weeksAgo) =>
        WeekMath.Bounds(DateOnly.FromDateTime(DateTime.Today)).Start.AddDays(-7 * weeksAgo);

    async Task SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.HodDailyAudits.RemoveRange(db.HodDailyAudits);

        void Add(string type, DateOnly date, int total, int max, string? actions = null) =>
            db.HodDailyAudits.Add(new HodDailyAudit
            {
                AuditorName = "Dana HOD",
                AuditDate = date,
                Department = "Phase 1 Weld",
                EffectivenessArea = "Zone 7",
                Area = "Zone 7",
                AuditType = type,
                AnswersJson = "[]",
                TotalScore = total,
                MaxScore = max,
                ActionsRaised = actions,
            });

        // TPM: present in the current week (two audits → averaged) and 2 weeks ago;
        // MISSING last week and 3 weeks ago → the line must break there.
        Add(HodAuditTypes.Tpm, WeekStart(0).AddDays(1), 8, 10, "Fix leak on German Jig 1");
        Add(HodAuditTypes.Tpm, WeekStart(0).AddDays(2), 6, 10);           // same week → avg 70%
        Add(HodAuditTypes.Tpm, WeekStart(2).AddDays(1), 5, 10);

        // A different auditor in-window must NOT appear on Dana's summary.
        db.HodDailyAudits.Add(new HodDailyAudit
        {
            AuditorName = "Someone Else", AuditDate = WeekStart(0).AddDays(1),
            Department = "Assembly", EffectivenessArea = "1 — HP", Area = "1 — HP",
            AuditType = HodAuditTypes.SixS, AnswersJson = "[]", TotalScore = 9, MaxScore = 10,
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Renders_four_charts_rolling_weeks_and_traceable_actions()
    {
        await SeedAsync();
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/HodAuditSummary?hod=Dana%20HOD&shift=Nights")).Content.ReadAsStringAsync();

        // Four audit-type charts, in board order.
        Assert.Contains("TPM Board Audit", html);
        Assert.Contains("6S Audit", html);
        Assert.Contains("Quality Audit", html);
        Assert.Contains("Parts ID &amp; Non-Conformance", html);

        // Rolling window = current ISO week + previous three, recomputed from today.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cur = WeekMath.IsoWeekNumber(WeekMath.Bounds(today).Start);
        Assert.Contains($"WK{cur}", html);
        Assert.Contains($"WK{WeekMath.IsoWeekNumber(WeekStart(3))}", html); // oldest column

        // Averaging: current-week TPM is (80+60)/2 = 70%.
        Assert.Contains("70%", html);

        // Traceability: action shows its text, type and week.
        Assert.Contains("Fix leak on German Jig 1", html);

        // Shift label prints but is a label only.
        Assert.Contains("Nights", html);

        // Isolation: the other auditor's 90% 6S score must not be plotted on Dana's
        // charts (they still appear in the selector dropdown, which is expected).
        Assert.DoesNotContain("90%", html);
    }

    [Fact]
    public async Task Missing_weeks_break_the_line_rather_than_plotting_zero()
    {
        await SeedAsync();
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/HodAuditSummary?hod=Dana%20HOD")).Content.ReadAsStringAsync();

        // The red trend segments are only drawn between consecutive weeks that BOTH
        // have data. Dana's TPM has data in weeks 3-ago and 0 (current) with gaps
        // between, so NO connecting segment should exist → 0 red <line> segments.
        var redSegments = Regex.Matches(html, "stroke='#CC1F2C' stroke-width='3'").Count;
        Assert.Equal(0, redSegments);

        // But the data points themselves are still plotted (red circles present).
        Assert.Contains("<circle", html);
    }
}
