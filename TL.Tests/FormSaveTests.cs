using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;

namespace TL.Tests;

public class FormSaveTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;

    public FormSaveTests(FormSaveWebAppFactory factory) => _factory = factory;

    async Task ResetDbAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ShiftSubmissions.RemoveRange(db.ShiftSubmissions);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveProgress_persists_hour_data_and_resumes_by_id()
    {
        await ResetDbAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var area = AreaList.All[0].Label;

        var startResp = await client.GetAsync(
            $"/Form?date={today}&shift=Day&area={Uri.EscapeDataString(area)}&tl=Test%20Leader");
        Assert.Equal(HttpStatusCode.Redirect, startResp.StatusCode);
        var id = ParseIdFromLocation(startResp.Headers.Location?.ToString());
        Assert.True(id > 0);

        var formHtml = await (await client.GetAsync($"/Form?id={id}&tl=Test%20Leader")).Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(formHtml);
        Assert.False(string.IsNullOrEmpty(token));

        var saveReq = BuildSaveRequest(id, today, area, token!, includeEditingId: true);
        saveReq.Headers.Add("RequestVerificationToken", token);

        var saveResp = await client.SendAsync(saveReq);
        var saveText = await saveResp.Content.ReadAsStringAsync();
        Assert.True(saveResp.IsSuccessStatusCode, $"Save failed: {(int)saveResp.StatusCode} {saveText}");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sub = await db.ShiftSubmissions.Include(s => s.Hours).FirstAsync(s => s.Id == id);
            Assert.Single(sub.Hours);
            Assert.Equal((byte)1, sub.Hours[0].HourNumber);
            Assert.True(sub.Hours[0].HazardsObserved);
        }

        var reloadHtml = await (await client.GetAsync($"/Form?id={id}")).Content.ReadAsStringAsync();
        Assert.Contains("id=\"h0_Haz_y\"", reloadHtml);
        Assert.Contains("checked", reloadHtml);
    }

    [Fact]
    public async Task SaveProgress_without_EditingId_finds_existing_shift()
    {
        await ResetDbAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var area = AreaList.All[0].Label;

        var startResp = await client.GetAsync(
            $"/Form?date={today}&shift=Day&area={Uri.EscapeDataString(area)}&tl=Test%20Leader");
        var id = ParseIdFromLocation(startResp.Headers.Location?.ToString());
        var formHtml = await (await client.GetAsync($"/Form?id={id}")).Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(formHtml)!;

        var saveResp = await client.SendAsync(BuildSaveRequest(id, today, area, token, includeEditingId: false));
        Assert.True(saveResp.IsSuccessStatusCode, await saveResp.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.ShiftSubmissions.CountAsync());
        Assert.Single((await db.ShiftSubmissions.Include(s => s.Hours).SingleAsync()).Hours);
    }

    [Fact]
    public async Task Partial_save_does_not_wipe_completed_hour()
    {
        await ResetDbAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var area = AreaList.All[0].Label;

        var startResp = await client.GetAsync(
            $"/Form?date={today}&shift=Day&area={Uri.EscapeDataString(area)}&tl=Test%20Leader");
        var id = ParseIdFromLocation(startResp.Headers.Location?.ToString());
        Assert.True(id > 0);
        var formHtml = await (await client.GetAsync($"/Form?id={id}")).Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(formHtml)!;

        var fullSave = await client.SendAsync(BuildSaveRequest(id, today, area, token, includeEditingId: true));
        Assert.True(fullSave.IsSuccessStatusCode, await fullSave.Content.ReadAsStringAsync());

        var partialBody = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("ShiftDate", today),
            new("Shift", "Day"),
            new("TeamLeader", "Test Leader"),
            new("Area", area),
            new("HoursCount", "8"),
            new("EditingId", id.ToString()),
            new("H[0].Haz", "True"),
        };
        var partialReq = new HttpRequestMessage(HttpMethod.Post, $"/Form?id={id}&handler=SaveProgress")
        {
            Content = new FormUrlEncodedContent(partialBody),
        };
        partialReq.Headers.Add("Accept", "application/json");
        partialReq.Headers.Add("RequestVerificationToken", token);

        var partialResp = await client.SendAsync(partialReq);
        Assert.True(partialResp.IsSuccessStatusCode, await partialResp.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hour = (await db.ShiftSubmissions.Include(s => s.Hours).SingleAsync()).Hours.Single();
        Assert.True(hour.HazardsObserved);
        Assert.False(hour.UnsafeBehaviours);
        Assert.Equal("Green", hour.OverallSafetyStatus);
    }

    [Fact]
    public async Task Different_team_leader_name_resumes_same_slot_shift()
    {
        await ResetDbAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var area = AreaList.All[0].Label;

        var first = await client.GetAsync(
            $"/Form?date={today}&shift=Day&area={Uri.EscapeDataString(area)}&tl=john%20smith");
        var id = ParseIdFromLocation(first.Headers.Location?.ToString());
        var html = await (await client.GetAsync($"/Form?id={id}")).Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html)!;
        Assert.True((await client.SendAsync(BuildSaveRequest(id, today, area, token, includeEditingId: true, teamLeader: "john smith"))).IsSuccessStatusCode);

        var resume = await client.GetAsync(
            $"/Form?date={today}&shift=Day&area={Uri.EscapeDataString(area)}&tl=JOHN%20SMITH");
        var resumedId = ParseIdFromLocation(resume.Headers.Location?.ToString());
        Assert.Equal(id, resumedId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.ShiftSubmissions.CountAsync());
        Assert.Single((await db.ShiftSubmissions.Include(s => s.Hours).SingleAsync()).Hours);
    }

    static HttpRequestMessage BuildSaveRequest(int id, string date, string area, string token, bool includeEditingId, string teamLeader = "Test Leader")
    {
        var body = BuildHourOneSaveBody(id, date, area, token, includeEditingId, teamLeader);
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Form?id={id}&handler=SaveProgress")
        {
            Content = new FormUrlEncodedContent(body),
        };
        req.Headers.Add("Accept", "application/json");
        return req;
    }

    static int ParseIdFromLocation(string? location)
    {
        var m = Regex.Match(location ?? "", @"[?&]id=(\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    static string? ExtractAntiforgeryToken(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        if (!m.Success)
            m = Regex.Match(html, @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""");
        return m.Success ? m.Groups[1].Value : null;
    }

    static IEnumerable<KeyValuePair<string, string>> BuildHourOneSaveBody(
        int id, string date, string area, string token, bool includeEditingId, string teamLeader = "Test Leader")
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("ShiftDate", date),
            new("Shift", "Day"),
            new("TeamLeader", teamLeader),
            new("Area", area),
            new("HoursCount", "8"),
        };
        if (includeEditingId)
            pairs.Add(new("EditingId", id.ToString()));

        string[] boolFields = ["Haz", "Uns", "Pos", "Qchk", "Dev", "Nc", "Qiplan", "Tgt", "Maint", "Mat", "Tools",
            "Escl", "Pconf", "Pid", "Ncp", "Wb", "Sup", "Acc"];
        foreach (var f in boolFields)
            pairs.Add(new($"H[0].{f}", f == "Haz" ? "True" : "False"));

        pairs.Add(new("H[0].Ss", "Green"));
        pairs.Add(new("H[0].Qs", "Green"));
        pairs.Add(new("H[0].Ps", "Green"));

        return pairs;
    }
}

public class FormSaveWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("FormSaveTests"));
        });
    }
}
