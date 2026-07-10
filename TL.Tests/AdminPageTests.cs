using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;

namespace TL.Tests;

public class AdminPageTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;

    public AdminPageTests(FormSaveWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_page_loads_for_admin_user()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/Admin")).Content.ReadAsStringAsync();
        Assert.Contains("Admin", html);
        Assert.Contains("In-progress shifts", html);
    }

    [Fact]
    public async Task Admin_can_delete_in_progress_shift()
    {
        int id;
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
            id = db.ShiftSubmissions.Select(s => s.Id).First();
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var adminHtml = await (await client.GetAsync("/Admin?tab=sessions")).Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(adminHtml);
        Assert.False(string.IsNullOrEmpty(token));

        var deleteResp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/Admin?handler=DeleteSession")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token!,
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
