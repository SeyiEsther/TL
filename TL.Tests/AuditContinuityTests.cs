using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using TL.Data;
using TL.Models;

namespace TL.Tests;

// Continuity: one shared record per natural key, so a second person opening the
// same area/period continues the first person's audit instead of duplicating it.
public class AuditContinuityTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public AuditContinuityTests(FormSaveWebAppFactory factory) => _factory = factory;

    // ---------- HoD daily audit: key = date + department + zone + type ----------

    [Fact]
    public async Task Hod_second_auditor_same_zone_day_type_continues_same_record()
    {
        await ResetHodAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var dept = AreaList.All[0].Group;
        var zone = AreaList.All[0].Label;

        var firstId = await StartHodAsync(client, today, "Alice Auditor", dept, zone, HodAuditTypes.SixS);
        var secondId = await StartHodAsync(client, today, "Bob Auditor", dept, zone, HodAuditTypes.SixS);

        Assert.Equal(firstId, secondId); // same shared record, not a fresh one

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.HodDailyAudits.CountAsync());
    }

    [Fact]
    public async Task Hod_different_audit_type_starts_a_separate_record()
    {
        await ResetHodAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var dept = AreaList.All[0].Group;
        var zone = AreaList.All[0].Label;

        var sixsId = await StartHodAsync(client, today, "Alice", dept, zone, HodAuditTypes.SixS);
        var tpmId = await StartHodAsync(client, today, "Alice", dept, zone, HodAuditTypes.Tpm);

        Assert.NotEqual(sixsId, tpmId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.HodDailyAudits.CountAsync());
    }

    // ---------- Senior weekly audit: key = area + ISO week (Mon–Sun) ----------

    [Fact]
    public async Task Senior_second_auditor_same_week_area_updates_shared_record()
    {
        await ResetSeniorAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var monday = MondayOfThisWeek();
        var wednesday = monday.AddDays(2);
        var area = AreaList.All[0].Label;

        await SubmitSeniorAsync(client, monday.ToString("yyyy-MM-dd"), "Alice", area);
        await SubmitSeniorAsync(client, wednesday.ToString("yyyy-MM-dd"), "Bob", area);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.SeniorWeeklyAudits.CountAsync()); // one shared record
        var row = await db.SeniorWeeklyAudits.SingleAsync();
        Assert.NotNull(row.LastEditedAt); // who-changed-what tracking recorded
    }

    [Fact]
    public async Task Senior_next_week_same_area_starts_new_record()
    {
        await ResetSeniorAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var monday = MondayOfThisWeek();
        var nextMonday = monday.AddDays(7);
        var area = AreaList.All[0].Label;

        await SubmitSeniorAsync(client, monday.ToString("yyyy-MM-dd"), "Alice", area);
        await SubmitSeniorAsync(client, nextMonday.ToString("yyyy-MM-dd"), "Alice", area);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.SeniorWeeklyAudits.CountAsync());
    }

    // ---------- helpers ----------

    static DateOnly MondayOfThisWeek()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
    }

    async Task ResetHodAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.HodDailyAudits.RemoveRange(db.HodDailyAudits);
        await db.SaveChangesAsync();
    }

    async Task ResetSeniorAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.SeniorWeeklyAudits.RemoveRange(db.SeniorWeeklyAudits);
        await db.SaveChangesAsync();
    }

    static async Task<int> StartHodAsync(HttpClient client, string date, string auditor, string dept, string zone, string type)
    {
        var token = ExtractToken(await (await client.GetAsync("/AuditStart")).Content.ReadAsStringAsync())!;
        var resp = await client.PostAsync("/AuditStart", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["auditDate"] = date,
            ["auditorName"] = auditor,
            ["department"] = dept,
            ["effectivenessArea"] = zone,
            ["auditType"] = type,
        }));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var id = ParseId(resp.Headers.Location?.ToString());
        Assert.True(id > 0, $"AuditStart did not redirect to an audit id: {resp.Headers.Location}");
        return id;
    }

    static async Task SubmitSeniorAsync(HttpClient client, string date, string auditor, string area)
    {
        var getUrl = $"/SeniorAudit?date={date}&auditor={Uri.EscapeDataString(auditor)}&area={Uri.EscapeDataString(area)}";
        var token = ExtractToken(await (await client.GetAsync(getUrl)).Content.ReadAsStringAsync())!;

        var body = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("auditDate", date),
            new("auditorName", auditor),
            new("area", area),
            new("A.LastTeamMeeting", "This week"),
            new("A.OverallVerdict", "Green"),
            new("auditorSignature", auditor),
        };
        string[] scores =
        [
            "HandoverStandardsFollowed", "VisualManagementCurrent", "EscalationPathsUsed",
            "PpeComplianceFull", "NearMissesReported",
            "FirstOffRecordsComplete", "NcProcedureFollowed", "QualityGatesMaintained",
            "LeaderVisibilityCheck", "TrainingMatrixCurrent",
            "SixSStandardMaintained", "TpmScheduleFollowed", "StandardWorkMaintained",
            "TrackingAgainstWeeklyPlan", "ImprovementActionsProgressing",
        ];
        foreach (var s in scores)
            body.Add(new($"A.{s}", "1"));

        var resp = await client.PostAsync("/SeniorAudit", new FormUrlEncodedContent(body));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("/SeniorSuccess", resp.Headers.Location?.ToString() ?? "");
    }

    static int ParseId(string? location)
    {
        var m = Regex.Match(location ?? "", @"[?&]id=(\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    static string? ExtractToken(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        if (!m.Success)
            m = Regex.Match(html, @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""");
        return m.Success ? m.Groups[1].Value : null;
    }
}
