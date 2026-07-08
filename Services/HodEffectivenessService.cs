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

    public async Task<HodShiftComplianceSummary> GetComplianceSummaryAsync(
        string department, string area, DateOnly auditDate, string? auditType = null)
    {
        var type = auditType ?? HodAuditTypes.SuggestedForDate(auditDate);
        var (weekStart, weekEnd) = WeekRange(auditDate);
        var findings = await GetFindingsAsync(department, area, auditDate, type);
        var shifts = findings.Where(f => f.ShiftSubmissionId.HasValue).ToList();

        return new HodShiftComplianceSummary(
            weekStart,
            weekEnd,
            shifts.Count,
            shifts.Count(f => f.CloseStatus != "Closed correctly"),
            shifts.Count(f => !f.FormComplete),
            shifts.Count(f => !f.OutgoingSignedOff),
            shifts.Count(f => f.OutgoingSignedOff && !f.IncomingHandoverAcknowledged),
            shifts.Count(f => f.CloseStatus == "Closed correctly"),
            findings);
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

        if (shifts.Count == 0)
        {
            return
            [
                new HodEffectivenessFinding
                {
                    Area = area,
                    CloseStatus = "No forms",
                    Issues = ["No TL shift forms submitted for this area/week"],
                    IsAuditFinding = true,
                }
            ];
        }

        return shifts.Select(s => BuildFinding(s, auditType)).ToList();
    }

    HodEffectivenessFinding BuildFinding(ShiftSubmission shift, string auditType)
    {
        var completion = _completion.Evaluate(shift);
        var hours = shift.Hours.OrderBy(h => h.HourNumber).ToList();
        var endHour = hours.FirstOrDefault(h => h.HourNumber == 1) ?? hours.FirstOrDefault();

        var claimedSixS = endHour?.SixSCompleted == true;
        var claimedTpm = endHour?.TPMCompleted == true;
        var allPartsId = hours.Count > 0 && hours.All(h => h.PartsIdCorrect == true);
        var anyPartsIdFail = hours.Any(h => h.PartsIdCorrect == false);
        var allNcStored = hours.Count > 0 && hours.All(h => h.NCPartsStoredCorrectly == true);
        var anyNcFail = hours.Any(h => h.NCPartsStoredCorrectly == false);
        var allQuality = hours.Count > 0 && hours.All(h => h.QualityChecksCompleted == true);
        var anyQualityFail = hours.Any(h => h.QualityChecksCompleted == false);

        var outgoingSigned = completion.SignedOff;
        var incomingAck = !string.IsNullOrWhiteSpace(shift.IncomingTLSignature);
        var closeStatus = ResolveCloseStatus(completion, outgoingSigned, incomingAck);

        var issues = BuildIssues(completion, outgoingSigned, incomingAck, auditType,
            claimedSixS, claimedTpm, allPartsId, anyPartsIdFail, allNcStored, anyNcFail, allQuality, anyQualityFail, endHour);

        return new HodEffectivenessFinding
        {
            ShiftSubmissionId = shift.Id,
            TeamLeader = shift.TeamLeaderDisplay,
            Shift = shift.Shift,
            ShiftDate = shift.ShiftDate,
            Area = shift.Area ?? "",
            FormComplete = completion.IsComplete,
            HoursComplete = completion.HoursComplete,
            HoursTotal = completion.HoursTotal,
            OutgoingSignedOff = outgoingSigned,
            IncomingHandoverAcknowledged = incomingAck,
            CloseStatus = closeStatus,
            TlClaimedSixS = claimedSixS,
            TlClaimedTpm = claimedTpm,
            TlClaimedPartsId = hours.Count > 0 ? allPartsId : null,
            TlClaimedNcStored = hours.Count > 0 ? allNcStored : null,
            TlClaimedQuality = hours.Count > 0 ? allQuality : null,
            Issues = issues.Distinct().ToList(),
            IsAuditFinding = !completion.IsComplete
                || (outgoingSigned && !incomingAck)
                || (auditType == HodAuditTypes.SixS && !claimedSixS)
                || (auditType == HodAuditTypes.Tpm && !claimedTpm),
        };
    }

    static string ResolveCloseStatus(ShiftCompletionResult completion, bool outgoingSigned, bool incomingAck)
    {
        if (completion.IsComplete && outgoingSigned && incomingAck)
            return "Closed correctly";
        if (!outgoingSigned)
            return "Not signed off";
        if (outgoingSigned && !incomingAck)
            return "Handover pending";
        return "Incomplete form";
    }

    static List<string> BuildIssues(
        ShiftCompletionResult completion,
        bool outgoingSigned,
        bool incomingAck,
        string auditType,
        bool claimedSixS,
        bool claimedTpm,
        bool allPartsId,
        bool anyPartsIdFail,
        bool allNcStored,
        bool anyNcFail,
        bool allQuality,
        bool anyQualityFail,
        HourlyCheck? endHour)
    {
        var issues = new List<string>();

        if (!completion.IsComplete)
        {
            var priority = completion.MissingItems
                .Where(m => m.Contains("sign-off", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var rest = completion.MissingItems.Except(priority).Take(3).ToList();
            issues.AddRange(priority);
            issues.AddRange(rest);
            if (completion.MissingItems.Count > priority.Count + rest.Count)
                issues.Add($"+{completion.MissingItems.Count - priority.Count - rest.Count} more missing items");
        }

        if (!outgoingSigned)
            issues.Add("Outgoing TL sign-off missing — shift not closed");
        else if (!incomingAck)
            issues.Add("Incoming TL has not acknowledged handover");

        switch (auditType)
        {
            case HodAuditTypes.SixS:
                if (claimedSixS)
                    issues.Add("TL claimed 6S done — verify on walkaround");
                else if (endHour?.SixSCompleted == false)
                    issues.Add("TL marked 6S as not done");
                else
                    issues.Add("TL has not answered 6S check");
                break;

            case HodAuditTypes.Tpm:
                if (claimedTpm)
                    issues.Add("TL claimed TPM done — verify physical boards");
                else if (endHour?.TPMCompleted == false)
                    issues.Add("TL marked TPM as not done");
                else
                    issues.Add("TL has not answered TPM check");
                break;

            case HodAuditTypes.PartsIdNc:
                if (anyPartsIdFail)
                    issues.Add("TL recorded parts ID failures during shift");
                else if (allPartsId)
                    issues.Add("TL claimed all parts ID'd — verify on floor");
                if (anyNcFail)
                    issues.Add("TL recorded NC storage/ID failures during shift");
                else if (allNcStored)
                    issues.Add("TL claimed NC parts correct — verify red cards");
                break;

            case HodAuditTypes.Quality:
                if (anyQualityFail)
                    issues.Add("TL recorded quality check failures during shift");
                else if (allQuality)
                    issues.Add("TL claimed all quality checks done — verify documentation");
                break;
        }

        return issues;
    }
}
