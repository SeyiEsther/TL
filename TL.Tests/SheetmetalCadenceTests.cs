using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TL.Models;

namespace TL.Tests;

// Sheetmetal (Phase 1 Weld / Phase 3 Pierce and Fold) runs a 2-hourly cadence:
// 4 checks per shift, not the hourly cadence used elsewhere.
public class SheetmetalCadenceTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public SheetmetalCadenceTests(FormSaveWebAppFactory factory) => _factory = factory;

    [Fact]
    public void Zones_are_grouped_into_the_two_sheetmetal_phases()
    {
        Assert.Equal("Phase 1 Weld", AreaList.GetDepartment("Zone 7"));
        Assert.Equal("Phase 1 Weld", AreaList.GetDepartment("Zone 15"));
        Assert.Equal("Phase 3 Pierce and Fold", AreaList.GetDepartment("Zone 1"));
        Assert.Equal("Phase 3 Pierce and Fold", AreaList.GetDepartment("Zone 19"));
        Assert.Equal("Phase 3 Pierce and Fold", AreaList.GetDepartment("Zone 4"));
    }

    [Theory]
    [InlineData("Zone 7", true)]
    [InlineData("Zone 19", true)]
    [InlineData("1 — HP", false)]   // Assembly
    [InlineData("13 — Black Line", false)] // Paint
    public void IsSheetmetal_matches_only_sheetmetal(string label, bool expected)
        => Assert.Equal(expected, AreaList.IsSheetmetal(label));

    [Fact]
    public async Task Sheetmetal_form_shows_four_2hourly_checks()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var resp = await client.GetAsync($"/Form?date={today}&shift=Day&area={Uri.EscapeDataString("Zone 7")}&tl=Test%20Leader");
        var id = int.Parse(Regex.Match(resp.Headers.Location?.ToString() ?? "", @"[?&]id=(\d+)").Groups[1].Value);

        var html = await (await client.GetAsync($"/Form?id={id}")).Content.ReadAsStringAsync();

        Assert.Contains("every 2 hrs", html);
        Assert.Contains("data-hours=\"4\"", html);
        Assert.Contains("Check", html);
        Assert.DoesNotContain("Hours in shift", html); // hourly selector hidden
    }

    [Fact]
    public async Task Existing_legacy_8hour_sheetmetal_record_is_preserved()
    {
        // A sheetmetal shift started under the old hourly cadence (8 hours of data)
        // must NOT be truncated to 4 when reopened — nothing captured is lost.
        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TL.Data.AppDbContext>();
            var sub = new ShiftSubmission
            {
                TeamLeaderDisplay = "Legacy TL",
                ShiftDate = DateOnly.FromDateTime(DateTime.Today),
                Shift = "Day",
                Area = "Zone 19",
                HoursCompleted = 8,
            };
            for (byte n = 1; n <= 8; n++)
                sub.Hours.Add(new HourlyCheck { HourNumber = n, HourlyTargetAchieved = true });
            db.ShiftSubmissions.Add(sub);
            await db.SaveChangesAsync();
            id = sub.Id;
        }

        var client = _factory.CreateClient();
        var html = await (await client.GetAsync($"/Form?id={id}")).Content.ReadAsStringAsync();

        Assert.Contains("data-hours=\"8\"", html); // all 8 preserved, not clamped to 4
        Assert.Contains("H[7].", html);            // the 8th check's inputs are rendered
    }

    [Fact]
    public async Task Non_sheetmetal_form_keeps_the_hourly_selector()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var area = AreaList.All.First(a => a.Group == "Assembly").Label;
        var resp = await client.GetAsync($"/Form?date={today}&shift=Day&area={Uri.EscapeDataString(area)}&tl=Test%20Leader");
        var id = int.Parse(Regex.Match(resp.Headers.Location?.ToString() ?? "", @"[?&]id=(\d+)").Groups[1].Value);

        var html = await (await client.GetAsync($"/Form?id={id}")).Content.ReadAsStringAsync();
        Assert.Contains("Hours in shift", html);
    }
}
