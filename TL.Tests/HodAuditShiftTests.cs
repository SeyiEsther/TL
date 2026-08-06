using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using TL.Data;
using TL.Models;

namespace TL.Tests;

public class HodAuditShiftTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public HodAuditShiftTests(FormSaveWebAppFactory factory) => _factory = factory;

    static string? Token(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        return m.Success ? m.Groups[1].Value : null;
    }

    [Fact]
    public async Task Start_form_offers_the_shift_selector()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/AuditStart")).Content.ReadAsStringAsync();
        Assert.Contains("name=\"shift\"", html);
        Assert.Contains(">Days<", html);
        Assert.Contains(">Backs<", html);
        Assert.Contains(">Nights<", html);
    }

    [Fact]
    public async Task Shift_is_required_to_start_an_audit()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = Token(await (await client.GetAsync("/AuditStart")).Content.ReadAsStringAsync())!;
        var resp = await client.PostAsync("/AuditStart", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["auditDate"] = DateTime.Today.ToString("yyyy-MM-dd"),
            ["auditorName"] = "Test HOD",
            ["department"] = "Assembly",
            ["effectivenessArea"] = AreaList.GetLabelsForDepartment("Assembly")[0],
            ["auditType"] = HodAuditTypes.SixS,
            // no shift
        }));
        // Stays on the page (no redirect to /Audit) with a validation error.
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("select the shift", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Selected_shift_is_saved_on_the_new_audit()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var area = AreaList.GetLabelsForDepartment("Assembly")[0];
        var token = Token(await (await client.GetAsync("/AuditStart")).Content.ReadAsStringAsync())!;
        var resp = await client.PostAsync("/AuditStart", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["auditDate"] = DateTime.Today.ToString("yyyy-MM-dd"),
            ["auditorName"] = "Backs HOD",
            ["department"] = "Assembly",
            ["effectivenessArea"] = area,
            ["auditType"] = HodAuditTypes.Quality,
            ["shift"] = HodShifts.Backs,
        }));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await db.HodDailyAudits
            .Where(a => a.AuditorName == "Backs HOD" && a.Area == area)
            .OrderByDescending(a => a.Id)
            .FirstAsync();
        Assert.Equal(HodShifts.Backs, audit.Shift);
    }
}
