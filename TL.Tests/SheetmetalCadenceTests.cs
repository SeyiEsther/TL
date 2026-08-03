using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
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
