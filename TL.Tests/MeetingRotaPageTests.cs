using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TL.Tests;

public class MeetingRotaPageTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public MeetingRotaPageTests(FormSaveWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Meeting_rota_renders_with_reference_content()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/MeetingRota");
        var html = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Meeting Rota", html);
        Assert.Contains("Cycle start date", html);
        Assert.Contains("06:45", html);
        Assert.Contains("08:00", html);
        Assert.Contains("15:15", html);
        Assert.Contains("2026-08-03", html); // default cycle start
    }
}
