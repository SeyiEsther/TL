using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TL.Tests;

public class SessionRefreshedPageTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public SessionRefreshedPageTests(FormSaveWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Page_post_without_a_token_shows_the_friendly_session_screen()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/AdminHodNames?handler=AddPerson")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["name"] = "No Token Person" }),
        };
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var resp = await client.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();

        // Missing antiforgery token → the friendly page instead of a raw 400.
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("Your session refreshed", body);
        Assert.DoesNotContain("No Token Person", body); // nothing was saved
    }
}
