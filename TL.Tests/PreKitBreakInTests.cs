using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using TL.Data;
using TL.Models;

namespace TL.Tests;

// The pre-kitting / break-in quality check is Assembly-only and optional.
public class PreKitBreakInTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public PreKitBreakInTests(FormSaveWebAppFactory factory) => _factory = factory;

    static string AssemblyArea => AreaList.All.First(a => a.Group == "Assembly").Label;
    static string NonAssemblyArea => AreaList.All.First(a => a.Group != "Assembly").Label;

    [Fact]
    public async Task Assembly_area_shows_the_prekitting_question()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var id = await StartShiftAsync(client, AssemblyArea);
        var html = await (await client.GetAsync($"/Form?id={id}")).Content.ReadAsStringAsync();
        Assert.Contains("pre-kitting and break in work", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("H[0].Pkb", html);
    }

    [Fact]
    public async Task Non_assembly_area_hides_the_prekitting_question()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var id = await StartShiftAsync(client, NonAssemblyArea);
        var html = await (await client.GetAsync($"/Form?id={id}")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("H[0].Pkb", html);
    }

    [Fact]
    public async Task Prekitting_answer_persists_for_assembly()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var area = AssemblyArea;
        var id = await StartShiftAsync(client, area);

        var html = await (await client.GetAsync($"/Form?id={id}")).Content.ReadAsStringAsync();
        var token = ExtractToken(html)!;
        var body = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("ShiftDate", today),
            new("Shift", "Day"),
            new("TeamLeader", "Test Leader"),
            new("Area", area),
            new("HoursCount", "8"),
            new("EditingId", id.ToString()),
            new("H[0].Pkb", "True"),
        };
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Form?id={id}&handler=SaveProgress")
        { Content = new FormUrlEncodedContent(body) };
        req.Headers.Add("Accept", "application/json");
        req.Headers.Add("RequestVerificationToken", token);
        var resp = await client.SendAsync(req);
        Assert.True(resp.IsSuccessStatusCode, await resp.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hour = (await db.ShiftSubmissions.Include(s => s.Hours).SingleAsync(s => s.Id == id)).Hours.Single();
        Assert.True(hour.PreKitBreakInFollowed);
    }

    static async Task<int> StartShiftAsync(HttpClient client, string area)
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var resp = await client.GetAsync($"/Form?date={today}&shift=Day&area={Uri.EscapeDataString(area)}&tl=Test%20Leader");
        var m = Regex.Match(resp.Headers.Location?.ToString() ?? "", @"[?&]id=(\d+)");
        Assert.True(m.Success, $"No shift id from start for area {area}");
        return int.Parse(m.Groups[1].Value);
    }

    static string? ExtractToken(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        if (!m.Success)
            m = Regex.Match(html, @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""");
        return m.Success ? m.Groups[1].Value : null;
    }
}
