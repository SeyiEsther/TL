using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;

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
}
