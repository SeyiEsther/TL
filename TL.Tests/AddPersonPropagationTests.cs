using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class AddPersonPropagationTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public AddPersonPropagationTests(FormSaveWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Adding_an_individual_also_makes_them_an_action_owner()
    {
        var name = "Casey Newstarter " + Guid.NewGuid().ToString("N")[..6];
        using var scope = _factory.Services.CreateScope();
        var people = scope.ServiceProvider.GetRequiredService<PersonListService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ok = await people.AddPersonAsync(PersonListKinds.Hod, name);
        Assert.True(ok);

        // Present in the HoD list AND auto-added to the ActionOwner list.
        Assert.True(await db.PickerPersons.AnyAsync(p => p.ListKind == PersonListKinds.Hod && p.Name == name));
        Assert.True(await db.PickerPersons.AnyAsync(p => p.ListKind == PersonListKinds.ActionOwner && p.Name == name));

        // And it shows up in the live (cached) lists immediately — no restart.
        Assert.Contains(name, people.Hods);
        Assert.Contains(name, people.ActionOwnersList);
    }
}
