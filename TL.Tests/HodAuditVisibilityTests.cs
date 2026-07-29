using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;

namespace TL.Tests;

// Reproduces: a saved HoD audit (Zone 19, PartsIdNc, today, another auditor)
// must be visible to any reviewer in History and the HoD dashboard.
public class HodAuditVisibilityTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public HodAuditVisibilityTests(FormSaveWebAppFactory factory) => _factory = factory;

    async Task<int> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.HodDailyAudits.RemoveRange(db.HodDailyAudits);
        var a = new HodDailyAudit
        {
            SubmittedBy = "someoneelse",
            AuditorName = "Someone Else",
            AuditDate = DateOnly.FromDateTime(DateTime.Today),
            Department = "Phase 3 Sheetmetal",
            EffectivenessArea = "Zone 19",
            Area = "Zone 19",
            AuditType = HodAuditTypes.PartsIdNc,
            AnswersJson = "[]",
            TotalScore = 5,
            MaxScore = 10,
            AuditorSignature = "Someone Else",
        };
        db.HodDailyAudits.Add(a);
        await db.SaveChangesAsync();
        return a.Id;
    }

    [Fact]
    public async Task Appears_in_history_hod_tab()
    {
        await SeedAsync();
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/History?tab=hod")).Content.ReadAsStringAsync();
        Assert.Contains("Zone 19", html);
    }

    [Fact]
    public async Task Appears_in_history_all_tab()
    {
        await SeedAsync();
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/History")).Content.ReadAsStringAsync();
        Assert.Contains("Zone 19", html);
    }

    [Fact]
    public async Task Appears_on_hod_dashboard()
    {
        await SeedAsync();
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/HodDashboard");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Zone 19", html);
    }
}
