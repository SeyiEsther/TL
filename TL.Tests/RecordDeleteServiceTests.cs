using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Tests;

public class RecordDeleteServiceTests
{
    [Fact]
    public async Task Hod_delete_does_not_remove_tl_shift_with_same_numeric_id()
    {
        await using var db = new InMemoryDb();
        db.ShiftSubmissions.Add(new ShiftSubmission
        {
            Id = 7,
            TeamLeaderDisplay = "TL Seven",
            ShiftDate = DateOnly.FromDateTime(DateTime.Today),
            Shift = "Day",
            Area = "Press",
            HoursCompleted = 8,
        });
        db.HodDailyAudits.Add(new HodDailyAudit
        {
            Id = 7,
            AuditorName = "HoD",
            AuditDate = DateOnly.FromDateTime(DateTime.Today),
            AuditType = "SixS",
            Department = "Press",
            Area = "Press",
            AnswersJson = "[]",
            EffectivenessJson = "[]",
        });
        await db.SaveChangesAsync();

        var svc = new RecordDeleteService(db);
        // Client forgot isNewHodAudit (defaults false) — must still delete the HoD row, not the TL shift.
        var ok = await svc.DeleteAsync("hod", 7, isNewHodAudit: false);
        Assert.True(ok);
        Assert.Null(await db.HodDailyAudits.FindAsync(7));
        Assert.NotNull(await db.ShiftSubmissions.FindAsync(7));
    }

    [Fact]
    public async Task Hod_delete_prefers_new_table_when_flagged()
    {
        await using var db = new InMemoryDb();
        db.HodDailyAudits.Add(new HodDailyAudit
        {
            Id = 3,
            AuditorName = "HoD",
            AuditDate = DateOnly.FromDateTime(DateTime.Today),
            AuditType = "Tpm",
            Department = "Press",
            Area = "Press",
            AnswersJson = "[]",
            EffectivenessJson = "[]",
        });
        await db.SaveChangesAsync();

        var svc = new RecordDeleteService(db);
        Assert.True(await svc.DeleteAsync("hod", 3, isNewHodAudit: true));
        Assert.Empty(db.HodDailyAudits);
    }

    sealed class InMemoryDb : AppDbContext
    {
        public InMemoryDb()
            : base(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"DeleteTests_{Guid.NewGuid():N}")
                .Options)
        {
            Database.EnsureCreated();
        }
    }
}
