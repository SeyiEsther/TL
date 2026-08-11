using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class ActionServiceTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public ActionServiceTests(FormSaveWebAppFactory factory) => _factory = factory;

    async Task<int> SeedActionAsync(Action<AuditAction> mutate)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AuditActions.RemoveRange(db.AuditActions); // isolate — shared in-memory DB
        await db.SaveChangesAsync();
        var a = new AuditAction
        {
            SourceType = ActionSourceTypes.HodDaily, SourceId = 1,
            SourceLabel = "HOD Daily — TPM — Zone 7", AuditType = "TPM Board Audit",
            Area = "Zone 7", AuditDate = DateOnly.FromDateTime(DateTime.Today),
            Text = "Fix the thing", RaisedByName = "Ray Raiser", RaisedByUsername = "rraiser",
            OwnerName = "Dana Owner", OwnerKey = PortalNameMatcher.Normalize("Dana Owner"),
            Status = ActionStatus.Open,
        };
        mutate(a);
        db.AuditActions.Add(a);
        await db.SaveChangesAsync();
        return a.Id;
    }

    [Fact]
    public async Task Owner_sees_their_open_action()
    {
        var id = await SeedActionAsync(_ => { });
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ActionService>();
        var mine = await svc.OpenForUserAsync("Dana Owner");
        Assert.Contains(mine, a => a.Id == id);
        Assert.Equal(1, await svc.OpenCountForUserAsync("Dana Owner"));
        Assert.Empty(await svc.OpenForUserAsync("Someone Unrelated"));
    }

    [Fact]
    public async Task Completion_requires_a_real_note()
    {
        var id = await SeedActionAsync(_ => { });
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ActionService>();

        var (okEmpty, err) = await svc.CompleteAsync(id, "Dana Owner", "   ");
        Assert.False(okEmpty);
        Assert.False(string.IsNullOrEmpty(err));

        var (ok, _) = await svc.CompleteAsync(id, "Dana Owner", "Replaced the seal and tested.");
        Assert.True(ok);

        using var scope2 = _factory.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var a = await db.AuditActions.FindAsync(id);
        Assert.Equal(ActionStatus.Complete, a!.Status);
        Assert.Equal("Replaced the seal and tested.", a.CompletionNote);
        Assert.Equal("Dana Owner", a.CompletedByName);
        Assert.NotNull(a.CompletedAt);
    }

    [Fact]
    public void Unresolved_flags_a_name_that_matches_nobody_but_not_shared()
    {
        var known = new List<string> { "Dana Owner", "Michael Tregillis" };
        var mismatch = new AuditAction { OwnerName = "Xyz Nobody", OwnerIsExternal = false };
        var resolved = new AuditAction { OwnerName = "Dana Owner", OwnerIsExternal = false };
        var shared = new AuditAction { OwnerName = "Maintenance", OwnerIsExternal = true };

        Assert.True(ActionService.IsUnresolved(mismatch, known));
        Assert.False(ActionService.IsUnresolved(resolved, known));
        Assert.False(ActionService.IsUnresolved(shared, known)); // shared is legitimate
    }

    [Fact]
    public async Task Complete_endpoint_rejects_missing_note_then_accepts()
    {
        var id = await SeedActionAsync(_ => { });
        var client = _factory.CreateClient();

        var bad = await client.PostAsJsonAsync($"/api/actions/{id}/complete", new { note = "" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var good = await client.PostAsJsonAsync($"/api/actions/{id}/complete", new { note = "Done and verified." });
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
    }

    [Fact]
    public async Task External_owner_is_flagged_and_reassign_reopen_work()
    {
        var id = await SeedActionAsync(a => { a.OwnerName = "Maintenance"; a.OwnerIsExternal = true; a.OwnerKey = PortalNameMatcher.Normalize("Maintenance"); });
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ActionService>();

        Assert.True(await svc.ReassignAsync(id, "Dana Owner", "Admin"));
        var reassigned = await svc.FindAsync(id);
        Assert.Equal("Dana Owner", reassigned!.OwnerName);
        Assert.False(reassigned.OwnerIsExternal);

        await svc.CompleteAsync(id, "Dana Owner", "Handled it.");
        Assert.True(await svc.ReopenAsync(id, "Admin"));
        var reopened = await svc.FindAsync(id);
        Assert.Equal(ActionStatus.Open, reopened!.Status);
        Assert.Null(reopened.CompletedAt);
    }
}
