using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;

namespace TL.Tests;

public class SeniorAccountabilityTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public SeniorAccountabilityTests(FormSaveWebAppFactory factory) => _factory = factory;

    // A Monday two weeks back — safely inside the default 12-week window.
    static DateOnly TargetMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var thisMonday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        return thisMonday.AddDays(-14);
    }

    async Task<(string completed, string missed, DateOnly monday)> SeedAsync()
    {
        var monday = TargetMonday();
        var dt = monday.ToDateTime(TimeOnly.MinValue);
        var team = SeniorRota.TeamForWeek(ISOWeek.GetYear(dt), ISOWeek.GetWeekOfYear(dt),
            SeniorManagementList.Names);

        // First rostered person completes; second is a genuine non-completion.
        var completed = team[0];
        var missed = team[1];

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.SeniorWeeklyAudits.RemoveRange(db.SeniorWeeklyAudits);
        db.SeniorWeeklyAudits.Add(new SeniorWeeklyAudit
        {
            AuditorName = completed,
            AuditDate = monday.AddDays(2),
            Area = "Zone 7",
            OverallVerdict = "Green",
        });
        await db.SaveChangesAsync();
        return (completed, missed, monday);
    }

    [Fact]
    public async Task Shows_completed_and_missed_for_rostered_seniors()
    {
        var (completed, missed, monday) = await SeedAsync();
        var from = monday.AddDays(-7).ToString("yyyy-MM-dd");
        var to = monday.AddDays(6).ToString("yyyy-MM-dd");

        var client = _factory.CreateClient();
        var html = await (await client.GetAsync(
            $"/SeniorAccountability?from={from}&to={to}")).Content.ReadAsStringAsync();

        // Both rostered people appear; the completer shows 1/ and the misser shows a missed count.
        Assert.Contains(completed, html);
        Assert.Contains(missed, html);
        // The completer has zero missed for the single in-window week they were rostered.
        // The misser must have at least one missed cell (✗ present somewhere).
        Assert.Contains("&#10007;", html); // ✗ missed marker rendered
        Assert.Contains("&#10003;", html); // ✓ completed marker rendered
    }

    [Fact]
    public async Task MissedOnly_filter_excludes_full_completers()
    {
        var (completed, missed, monday) = await SeedAsync();
        // Window = just the target week, so the completer has no misses at all.
        var from = monday.ToString("yyyy-MM-dd");
        var to = monday.AddDays(6).ToString("yyyy-MM-dd");

        var client = _factory.CreateClient();
        var html = await (await client.GetAsync(
            $"/SeniorAccountability?from={from}&to={to}&missed=true")).Content.ReadAsStringAsync();

        // The misser is a non-completion → present; the completer has 0 missed → filtered out.
        Assert.Contains(missed, html);
        Assert.DoesNotContain($">{completed}<", html);
    }

    [Fact]
    public async Task Csv_export_returns_csv_with_expected_columns()
    {
        var (_, _, monday) = await SeedAsync();
        var from = monday.ToString("yyyy-MM-dd");
        var to = monday.AddDays(6).ToString("yyyy-MM-dd");

        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/SeniorAccountability?handler=Csv&from={from}&to={to}");
        Assert.True(resp.IsSuccessStatusCode);
        Assert.Equal("text/csv", resp.Content.Headers.ContentType?.MediaType);
        var csv = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Person", csv);
        Assert.Contains("Expected,Completed,Missed", csv);
    }
}
