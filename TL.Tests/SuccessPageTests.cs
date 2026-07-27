using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;

namespace TL.Tests;

public class SuccessPageTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public SuccessPageTests(FormSaveWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Progress_save_shows_saved_confirmation_and_continue_editing()
    {
        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sub = new ShiftSubmission
            {
                TeamLeaderDisplay = "Test TL",
                ShiftDate = DateOnly.FromDateTime(DateTime.Today),
                Shift = "Day",
                Area = AreaList.All[0].Label,
            };
            db.ShiftSubmissions.Add(sub);
            await db.SaveChangesAsync();
            id = sub.Id;
        }

        var client = _factory.CreateClient();
        var html = await (await client.GetAsync($"/Success?id={id}&saved=true")).Content.ReadAsStringAsync();

        Assert.Contains("Progress saved", html);
        Assert.Contains("Continue editing", html);
        Assert.DoesNotContain("submitted!", html); // not the final-submit screen
    }
}
