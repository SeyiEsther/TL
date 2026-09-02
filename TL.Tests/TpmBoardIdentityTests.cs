using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;

namespace TL.Tests;

public class TpmBoardIdentityTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public TpmBoardIdentityTests(FormSaveWebAppFactory factory) => _factory = factory;

    static string? Token(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        return m.Success ? m.Groups[1].Value : null;
    }

    async Task<int> StartAsync(HttpClient client, string zone, string type)
    {
        var token = Token(await (await client.GetAsync("/AuditStart")).Content.ReadAsStringAsync())!;
        var resp = await client.PostAsync("/AuditStart", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["auditDate"] = DateTime.Today.ToString("yyyy-MM-dd"),
            ["auditorName"] = "Tester",
            ["department"] = "Assembly",
            ["effectivenessArea"] = zone,
            ["auditType"] = type,
            ["shift"] = "Days",
        }));
        var m = Regex.Match(resp.Headers.Location?.ToString() ?? "", @"[?&]id=(\d+)");
        return int.Parse(m.Groups[1].Value);
    }

    [Fact]
    public async Task Tpm_different_lines_in_a_group_continue_the_same_board_record()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.HodDailyAudits.RemoveRange(db.HodDailyAudits);
            await db.SaveChangesAsync();
        }
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var idOzeki = await StartAsync(client, "2 — OZEKI", HodAuditTypes.Tpm);
        var idHp = await StartAsync(client, "1 — HP", HodAuditTypes.Tpm);   // same board group

        Assert.Equal(idOzeki, idHp); // one shared board record

        using var check = _factory.Services.CreateScope();
        var db2 = check.ServiceProvider.GetRequiredService<AppDbContext>();
        var a = await db2.HodDailyAudits.SingleAsync();
        Assert.Equal("HP / Ozeki / SPC / TX", a.EffectivenessArea); // stored as the board
    }

    [Fact]
    public async Task Non_tpm_keeps_individual_zones_as_separate_records()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.HodDailyAudits.RemoveRange(db.HodDailyAudits);
            await db.SaveChangesAsync();
        }
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var idOzeki = await StartAsync(client, "2 — OZEKI", HodAuditTypes.SixS);
        var idHp = await StartAsync(client, "1 — HP", HodAuditTypes.SixS);

        Assert.NotEqual(idOzeki, idHp); // 6S stays per-line
        using var check = _factory.Services.CreateScope();
        var db2 = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db2.HodDailyAudits.CountAsync());
    }
}
