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
            // resolved in code, matching the audit pattern.
            mb.Entity<TeamMeeting>(e =>
            {
                e.HasIndex(m => new { m.MeetingDate, m.Area, m.Shift });
            });
        }
    }
}