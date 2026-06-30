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
        }
    }
}