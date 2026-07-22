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

    [Fact]
    public async Task Dashboard_keeps_week_mode_when_filters_post_week_bounds()
    {
        // Filter form always posts from/to = ISO week Mon–Sun; that must not exit week mode.
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/Dashboard?from=2026-07-20&to=2026-07-26&shift=Day");
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Target vs Actual", html);
        Assert.DoesNotContain("custom date range", html);
    }

    [Fact]
    public async Task Dashboard_custom_range_hides_weekly_targets()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/Dashboard?from=2026-07-01&to=2026-07-10");
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("custom date range", html);
        Assert.Contains("Switch to a week view to see target vs actual", html);
        Assert.DoesNotContain("Per-shift target vs actual", html);
    }
}
