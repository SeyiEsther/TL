using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;

namespace TL.Tests;

public class HistoryPageTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;

    public HistoryPageTests(FormSaveWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task History_page_returns_ok()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/History");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("History", html);
    }

    [Fact]
    public async Task History_delete_removes_shift_submission()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ShiftSubmissions.RemoveRange(db.ShiftSubmissions);
            db.ShiftSubmissions.Add(new ShiftSubmission
            {
                TeamLeaderDisplay = "Test TL",
                ShiftDate = DateOnly.FromDateTime(DateTime.Today),
                Shift = "Day",
                Area = AreaList.All[0].Label,
                HoursCompleted = 8,
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var historyHtml = await (await client.GetAsync("/History?tab=shifts")).Content.ReadAsStringAsync();
        Assert.Contains("Actions", historyHtml);
        var token = ExtractAntiforgeryToken(historyHtml);
        Assert.False(string.IsNullOrEmpty(token));

        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = db.ShiftSubmissions.Select(s => s.Id).First();
        }

        var deleteResp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/History?handler=Delete")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token!,
                ["tab"] = "shifts",
                ["kind"] = "shifts",
                ["id"] = id.ToString(),
            }),
        });
        Assert.Equal(HttpStatusCode.Redirect, deleteResp.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Empty(db.ShiftSubmissions);
        }
    }

    static string? ExtractAntiforgeryToken(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        if (!m.Success)
            m = Regex.Match(html, @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""");
        return m.Success ? m.Groups[1].Value : null;
    }
}
