using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Services;

public class HodEffectivenessService
{
    private readonly AppDbContext _db;
    private readonly ShiftCompletionService _completion;

    public HodEffectivenessService(AppDbContext db, ShiftCompletionService completion)
    {
        _db = db;
        _completion = completion;
    }

    /// <summary>Week containing auditDate (Mon–Sun).</summary>
    public static (DateOnly Start, DateOnly End) WeekRange(DateOnly auditDate)
    {
        var dow = auditDate.DayOfWeek;
        var daysFromMonday = dow == DayOfWeek.Sunday ? 6 : (int)dow - 1;
        var start = auditDate.AddDays(-daysFromMonday);
        return (start, start.AddDays(6));
    }

    public async Task<List<HodEffectivenessFinding>> GetFindingsAsync(
        string department, string area, DateOnly auditDate, string auditType)
    {
        var (weekStart, weekEnd) = WeekRange(auditDate);
        var deptAreas = AreaList.All
            .Where(a => a.Group == department)
            .Select(a => a.Label)
            .ToHashSet();

        var q = _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => s.ShiftDate >= weekStart && s.ShiftDate <= weekEnd);

        if (!string.IsNullOrEmpty(area))
            q = q.Where(s => s.Area == area);
        else if (!string.IsNullOrEmpty(department))
            q = q.Where(s => s.Area != null && deptAreas.Contains(s.Area));

        var shifts = await q.OrderBy(s => s.ShiftDate).ThenBy(s => s.Shift).ToListAsync();
        var findings = new List<HodEffectivenessFinding>();

        foreach (var shift in shifts)
        {
            var completion = _completion.Evaluate(shift);
            var issues = new List<string>(completion.MissingItems);

            var endHour = shift.Hours.FirstOrDefault(h => h.HourNumber == 1) ?? shift.Hours.FirstOrDefault();
            var claimedSixS = endHour?.SixSCompleted == true;
            var claimedTpm = endHour?.TPMCompleted == true;

            if (auditType == HodAuditTypes.SixS && claimedSixS && !completion.IsComplete)
                issues.Add("TL claimed 6S done but shift form is incomplete");
            if (auditType == HodAuditTypes.Tpm && claimedTpm && !completion.IsComplete)
                issues.Add("TL claimed TPM done but shift form is incomplete");

            if (!completion.IsComplete || issues.Count > completion.MissingItems.Count)
            {
                findings.Add(new HodEffectivenessFinding
                {
                    TeamLeader = shift.TeamLeaderDisplay,
                    Shift = shift.Shift,
                    ShiftDate = shift.ShiftDate,
                    Area = shift.Area ?? "",
                    Issues = issues.Distinct().ToList(),
                    TlClaimedSixS = claimedSixS,
                    TlClaimedTpm = claimedTpm,
                });
            }
        }

        if (findings.Count == 0 && shifts.Count == 0)
        {
            findings.Add(new HodEffectivenessFinding
            {
                Area = area,
                Issues = ["No TL shift forms submitted for this area/week — incomplete compliance"],
            });
        }

        return findings;
    }
}
