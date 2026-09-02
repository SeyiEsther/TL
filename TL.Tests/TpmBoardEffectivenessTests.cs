using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class TpmBoardEffectivenessTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public TpmBoardEffectivenessTests(FormSaveWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Tpm_board_aggregates_shift_findings_from_every_line_tagged_by_line()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ShiftSubmissions.RemoveRange(db.ShiftSubmissions);
            // A shift form on two different lines of the HP/Ozeki/SPC/TX board.
            db.ShiftSubmissions.Add(new ShiftSubmission
            {
                TeamLeaderDisplay = "TL Hp", ShiftDate = today, Shift = "Day", Area = "1 — HP", SubmittedBy = "t",
            });
            db.ShiftSubmissions.Add(new ShiftSubmission
            {
                TeamLeaderDisplay = "TL Ozeki", ShiftDate = today, Shift = "Back", Area = "2 — OZEKI", SubmittedBy = "t",
            });
            // A line NOT on the board — must not appear.
            db.ShiftSubmissions.Add(new ShiftSubmission
            {
                TeamLeaderDisplay = "TL Meta", ShiftDate = today, Shift = "Day", Area = "3 — META", SubmittedBy = "t",
            });
            await db.SaveChangesAsync();
        }

        using var s = _factory.Services.CreateScope();
        var svc = s.ServiceProvider.GetRequiredService<HodEffectivenessService>();
        var findings = await svc.GetFindingsAsync("Assembly", "HP / Ozeki / SPC / TX", today, HodAuditTypes.Tpm);

        var lines = findings.Select(f => f.Area).ToList();
        Assert.Contains("1 — HP", lines);       // both board lines aggregated...
        Assert.Contains("2 — OZEKI", lines);
        Assert.DoesNotContain("3 — META", lines); // ...but not an off-board line
    }

    [Fact]
    public async Task Non_tpm_still_looks_up_the_single_line()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ShiftSubmissions.RemoveRange(db.ShiftSubmissions);
            db.ShiftSubmissions.Add(new ShiftSubmission { TeamLeaderDisplay = "TL Hp", ShiftDate = today, Shift = "Day", Area = "1 — HP", SubmittedBy = "t" });
            db.ShiftSubmissions.Add(new ShiftSubmission { TeamLeaderDisplay = "TL Ozeki", ShiftDate = today, Shift = "Day", Area = "2 — OZEKI", SubmittedBy = "t" });
            await db.SaveChangesAsync();
        }

        using var s = _factory.Services.CreateScope();
        var svc = s.ServiceProvider.GetRequiredService<HodEffectivenessService>();
        var findings = await svc.GetFindingsAsync("Assembly", "1 — HP", today, HodAuditTypes.SixS);
        Assert.All(findings, f => Assert.Equal("1 — HP", f.Area)); // only the picked line
    }
}
