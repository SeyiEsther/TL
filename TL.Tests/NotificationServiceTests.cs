using Microsoft.Extensions.DependencyInjection;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class NotificationServiceTests : IClassFixture<FormSaveWebAppFactory>
{
    private readonly FormSaveWebAppFactory _factory;
    public NotificationServiceTests(FormSaveWebAppFactory factory) => _factory = factory;

    // Fixes the "current user" without a real HTTP identity.
    sealed class StubUser : UserService
    {
        private readonly string _name;
        public StubUser(string name) : base(null!, null!, null!) => _name = name;
        public override AppUser GetCurrentUser() =>
            new() { Username = "tester", DisplayName = _name, IsManager = true };
    }

    NotificationService Build(AppDbContext db, string userName)
    {
        var users = new StubUser(userName);
        return new NotificationService(new HistoryListService(db), new ActionService(db, users), users);
    }

    async Task ResetAsync(AppDbContext db)
    {
        db.HodDailyAudits.RemoveRange(db.HodDailyAudits);
        db.AuditActions.RemoveRange(db.AuditActions);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Surfaces_own_unfinished_audit_and_assigned_action()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ResetAsync(db);

        // Unfinished HOD audit (no signature) authored by Dana.
        db.HodDailyAudits.Add(new HodDailyAudit
        {
            AuditorName = "Dana HOD", AuditDate = DateOnly.FromDateTime(DateTime.Today),
            Department = "Weld", EffectivenessArea = "Zone 7", Area = "Zone 7",
            AuditType = HodAuditTypes.Tpm, AnswersJson = "[]", TotalScore = 0, MaxScore = 10,
            AuditorSignature = null,
        });
        // A finished audit by Dana must NOT appear.
        db.HodDailyAudits.Add(new HodDailyAudit
        {
            AuditorName = "Dana HOD", AuditDate = DateOnly.FromDateTime(DateTime.Today),
            Department = "Weld", EffectivenessArea = "Zone 8", Area = "Zone 8",
            AuditType = HodAuditTypes.SixS, AnswersJson = "[]", TotalScore = 9, MaxScore = 10,
            AuditorSignature = "Dana HOD",
        });
        // Open action assigned to Dana.
        db.AuditActions.Add(new AuditAction
        {
            SourceType = ActionSourceTypes.HodDaily, SourceLabel = "HOD — TPM",
            Text = "Replace the seal", RaisedByName = "Ray", RaisedByUsername = "ray",
            OwnerName = "Dana HOD", OwnerKey = PortalNameMatcher.Normalize("Dana HOD"),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), Status = ActionStatus.Open,
        });
        await db.SaveChangesAsync();

        var notes = await Build(db, "Dana HOD").ForCurrentUserAsync();

        Assert.Single(notes.Unfinished);
        // Resume link reopens the EXISTING record, never starts a new one.
        Assert.StartsWith("/Audit?id=", notes.Unfinished[0].ResumeUrl);
        Assert.Single(notes.Actions);
        Assert.True(notes.Actions[0].Overdue);
        Assert.Equal(2, notes.Count);
        Assert.True(notes.Any);
    }

    [Fact]
    public async Task Other_users_items_are_not_shown()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ResetAsync(db);

        db.HodDailyAudits.Add(new HodDailyAudit
        {
            AuditorName = "Someone Else", AuditDate = DateOnly.FromDateTime(DateTime.Today),
            Department = "Weld", EffectivenessArea = "Zone 7", Area = "Zone 7",
            AuditType = HodAuditTypes.Tpm, AnswersJson = "[]", TotalScore = 0, MaxScore = 10,
        });
        await db.SaveChangesAsync();

        var notes = await Build(db, "Dana HOD").ForCurrentUserAsync();
        Assert.Empty(notes.Unfinished);
        Assert.Empty(notes.Actions);
        Assert.False(notes.Any);
    }

    [Fact]
    public async Task Empty_display_name_yields_nothing()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ResetAsync(db);

        var notes = await Build(db, "").ForCurrentUserAsync();
        Assert.Equal(0, notes.Count);
    }
}
