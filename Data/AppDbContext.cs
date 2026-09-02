using Microsoft.EntityFrameworkCore;
using TL.Models;
namespace TL.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<ShiftSubmission> ShiftSubmissions => Set<ShiftSubmission>();
        public DbSet<HourlyCheck> HourlyChecks => Set<HourlyCheck>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<AuditSubmission> AuditSubmissions => Set<AuditSubmission>();
        public DbSet<SeniorWeeklyAudit> SeniorWeeklyAudits => Set<SeniorWeeklyAudit>();
        public DbSet<HodDailyAudit> HodDailyAudits => Set<HodDailyAudit>();
        public DbSet<PickerPerson> PickerPersons => Set<PickerPerson>();
        public DbSet<TeamMeeting> TeamMeetings => Set<TeamMeeting>();
        public DbSet<DocumentNumber> DocumentNumbers => Set<DocumentNumber>();
        public DbSet<AuditAction> AuditActions => Set<AuditAction>();
        public DbSet<ShiftManagerReport> ShiftManagerReports => Set<ShiftManagerReport>();
        public DbSet<TargetSetting> TargetSettings => Set<TargetSetting>();
        protected override void OnModelCreating(ModelBuilder mb)
        {
            
            mb.Entity<ShiftSubmission>(e =>
            {
                e.HasMany(s => s.Hours)
                 .WithOne()
                 .HasForeignKey(h => h.ShiftSubmissionId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasMany(s => s.AuditLogs)
                 .WithOne(a => a.Submission)
                 .HasForeignKey(a => a.SubmissionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
            mb.Entity<PickerPerson>(e =>
            {
                e.HasIndex(p => new { p.ListKind, p.Name }).IsUnique();
            });
            // Non-unique index for the natural-key lookup (date + area + shift).
            // Not unique so a concurrent insert can't hard-fail; continuity is
            // resolved in code, matching the audit pattern. Area/Shift are bounded
            // well under the index key limit (default nvarchar(450) x2 + date would
            // be 1803 bytes, over the 1700-byte nonclustered index cap) while still
            // far larger than any real area/shift name. All the free-text/notes
            // columns stay nvarchar(max).
            mb.Entity<TeamMeeting>(e =>
            {
                e.Property(m => m.Area).HasMaxLength(200);
                e.Property(m => m.Shift).HasMaxLength(30);
                e.HasIndex(m => new { m.MeetingDate, m.Area, m.Shift });
            });
            mb.Entity<HodDailyAudit>(e =>
            {
                e.Property(a => a.Shift).HasMaxLength(40);
            });
            mb.Entity<AuditAction>(e =>
            {
                // Free text stays unbounded (this project has a history of
                // truncation crashes); names/keys are generously sized.
                e.Property(a => a.Text).HasColumnType("nvarchar(max)");
                e.Property(a => a.CompletionNote).HasColumnType("nvarchar(max)");
                e.Property(a => a.SourceLabel).HasColumnType("nvarchar(max)");
                e.Property(a => a.SourceType).HasMaxLength(40);
                e.Property(a => a.AuditType).HasMaxLength(120);
                e.Property(a => a.Area).HasMaxLength(200);
                e.Property(a => a.RaisedByName).HasMaxLength(256);
                e.Property(a => a.RaisedByUsername).HasMaxLength(256);
                e.Property(a => a.OwnerName).HasMaxLength(256);
                e.Property(a => a.OwnerKey).HasMaxLength(256);
                e.Property(a => a.Status).HasMaxLength(20);
                e.Property(a => a.CompletedByName).HasMaxLength(256);
                e.HasIndex(a => new { a.Status, a.OwnerKey });
                e.HasIndex(a => new { a.SourceType, a.SourceId });
            });
            mb.Entity<ShiftManagerReport>(e =>
            {
                // Free text stays unbounded to avoid truncation.
                foreach (var prop in new[] { "HseJson", "ProductionJson", "AuditsJson",
                    "ManagerHseComments", "ProductionComments", "LswTeamLeaderComments",
                    "LswHodComments", "Aob" })
                    e.Property(prop).HasColumnType("nvarchar(max)");
                e.Property(x => x.Shift).HasMaxLength(30);
                e.Property(x => x.ManagerName).HasMaxLength(256);
                e.Property(x => x.SubmittedBy).HasMaxLength(256);
                e.Property(x => x.LastEditedBy).HasMaxLength(256);
                e.HasIndex(x => new { x.ReportDate, x.Shift });
            });
            mb.Entity<TargetSetting>(e =>
            {
                e.Property(t => t.Key).HasMaxLength(60);
                e.Property(t => t.UpdatedBy).HasMaxLength(256);
                e.HasIndex(t => t.Key).IsUnique();
            });
            mb.Entity<DocumentNumber>(e =>
            {
                e.Property(d => d.FormType).HasMaxLength(60);
                e.Property(d => d.Label).HasMaxLength(120);
                e.Property(d => d.Number).HasMaxLength(120);
                e.HasIndex(d => d.FormType).IsUnique();
            });
        }
    }
}