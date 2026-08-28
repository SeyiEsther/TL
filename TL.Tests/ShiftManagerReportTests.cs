using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Services;

namespace TL.Tests;

public class ShiftManagerReportTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public ShiftManagerReportTests(FormSaveWebAppFactory factory) => _factory = factory;

    static string? Token(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        return m.Success ? m.Groups[1].Value : null;
    }

    [Fact]
    public async Task Shift_manager_tab_and_report_form_load()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ShiftManager")).StatusCode);
        var html = await (await client.GetAsync("/ShiftManagerReport")).Content.ReadAsStringAsync();
        Assert.Contains("Shift Manager Daily Report", html);
        Assert.Contains("Accident", html);       // HSE row present
        Assert.Contains("PH1 recovery", html);    // production row present
    }

    [Fact]
    public async Task Daily_report_saves_metrics_and_comments()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = Token(await (await client.GetAsync("/ShiftManagerReport")).Content.ReadAsStringAsync())!;

        var body = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("ReportDate", DateTime.Today.ToString("yyyy-MM-dd")),
            new("Shift", "Days"),
            new("ManagerName", "Nicky Gleeson"),
            new("HseTarget[0]", "0"), new("HseActual[0]", "1"),   // Accident 0/1
            new("ProdTarget[0]", "90"), new("ProdActual[0]", "88"),
            new("AuditDone[0]", "N"), new("AuditDone[1]", "Y"),   // 6S=N, TPM=Y
            new("AuditDone[2]", ""), new("AuditDone[3]", ""),
            new("ManagerHseComments", "One near miss reviewed."),
        };
        var resp = await client.PostAsync("/ShiftManagerReport", new FormUrlEncodedContent(body));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("/ShiftManagerReportSuccess", resp.Headers.Location?.ToString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var r = await db.ShiftManagerReports.OrderByDescending(x => x.Id).FirstAsync();
        Assert.Equal("Nicky Gleeson", r.ManagerName);
        Assert.Equal("Days", r.Shift);
        Assert.Contains("Accident", r.HseJson);
        Assert.Contains("One near miss reviewed.", r.ManagerHseComments);
        var audits = ShiftReportSerializer.Audits(r.AuditsJson);
        Assert.Equal("Y", audits.First(a => a.Type == "TPM").Completed);
    }
}
