using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TL.Tests;

public class DashboardPageTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public DashboardPageTests(FormSaveWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Dashboard_defaults_to_current_week_report()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/Dashboard");
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Target vs Actual", html);
        Assert.Contains("Week", html);
    }

    [Fact]
    public async Task Dashboard_opens_a_historical_week_without_error()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/Dashboard?week=2026-07-20");
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Week 30", html); // 2026-07-20 is ISO week 30
    }
}
