using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class SeniorAuditInputModel
{
    public byte? HandoverStandardsFollowed { get; set; }
    public byte? VisualManagementCurrent { get; set; }
    public byte? EscalationPathsUsed { get; set; }
    public string? GovernanceNotes { get; set; }

    public byte? PpeComplianceFull { get; set; }
    public byte? NearMissesReported { get; set; }
    public string? SafetyNotes { get; set; }

    public byte? FirstOffRecordsComplete { get; set; }
    public byte? NcProcedureFollowed { get; set; }
    public byte? QualityGatesMaintained { get; set; }
    public string? QualityNotes { get; set; }

    public byte? LeaderVisibilityCheck { get; set; }
    public string? LastTeamMeeting { get; set; }
    public byte? TrainingMatrixCurrent { get; set; }
    public string? PeopleNotes { get; set; }

    public byte? SixSStandardMaintained { get; set; }
    public byte? TpmScheduleFollowed { get; set; }
    public byte? StandardWorkMaintained { get; set; }
    public string? StandardsNotes { get; set; }

    public byte? TrackingAgainstWeeklyPlan { get; set; }
    public byte? ImprovementActionsProgressing { get; set; }
    public string? PerformanceNotes { get; set; }

    public string? GoodPracticeObserved { get; set; }
    public string? AreasForImprovement { get; set; }
    public string? ActionsRaised { get; set; }
    public string? OverallVerdict { get; set; }
}

public class SeniorAuditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserService _users;

    public SeniorAuditModel(AppDbContext db, UserService users) { _db = db; _users = users; }

    public string AuditDate { get; set; } = "";
    public string AuditorName { get; set; } = "";
    public string Area { get; set; } = "";
    public int? EditingId { get; set; }
    public string? AuditorSignature { get; set; }

    [BindProperty] public SeniorAuditInputModel A { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? date, string? auditor, string? area, int? id)
    {
        if (id.HasValue)
        {
            var sub = await _db.SeniorWeeklyAudits.FirstOrDefaultAsync(s => s.Id == id.Value);
            if (sub == null) return RedirectToPage("/SeniorStart");

            EditingId = sub.Id;
            AuditDate = sub.AuditDate.ToString("yyyy-MM-dd");
            AuditorName = sub.AuditorName;
            Area = sub.Area;
            AuditorSignature = sub.AuditorSignature;
            A = MapToInput(sub);
        }
        else
        {
            AuditDate = date ?? DateTime.Today.ToString("yyyy-MM-dd");
            AuditorName = auditor ?? "";
            Area = area ?? "";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string auditDate, string auditorName, string area, int? editingId, string? auditorSignature)
    {
        AuditDate = auditDate;
        AuditorName = auditorName;
        Area = area;
        EditingId = editingId;
        AuditorSignature = auditorSignature;

        var missing = CountMissingScores(A);
        if (missing > 0)
        {
            ModelState.AddModelError("", $"Answer all {missing} question(s) using the 0–2 scale.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(A.LastTeamMeeting))
        {
            ModelState.AddModelError("", "Please record when the last team meeting was held.");
            return Page();
        }

        var user = _users.GetCurrentUser();

        if (editingId.HasValue)
        {
            var sub = await _db.SeniorWeeklyAudits.FirstOrDefaultAsync(s => s.Id == editingId.Value);
            if (sub == null) return RedirectToPage("/SeniorStart");

            ApplyInput(sub, A);
            sub.AuditorSignature = auditorSignature;
            await _db.SaveChangesAsync();
            return RedirectToPage("/SeniorSuccess", new { id = editingId });
        }

        if (!DateOnly.TryParse(auditDate, out var d)) d = DateOnly.FromDateTime(DateTime.Today);

        var audit = new SeniorWeeklyAudit
        {
            SubmittedBy = user.Username,
            AuditorName = auditorName ?? user.DisplayName,
            AuditDate = d,
            Area = area,
            AuditorSignature = auditorSignature,
        };
        ApplyInput(audit, A);

        _db.SeniorWeeklyAudits.Add(audit);
        await _db.SaveChangesAsync();
        return RedirectToPage("/SeniorSuccess", new { id = audit.Id });
    }

    static int CountMissingScores(SeniorAuditInputModel a) =>
        new byte?[]
        {
            a.HandoverStandardsFollowed, a.VisualManagementCurrent, a.EscalationPathsUsed,
            a.PpeComplianceFull, a.NearMissesReported,
            a.FirstOffRecordsComplete, a.NcProcedureFollowed, a.QualityGatesMaintained,
            a.LeaderVisibilityCheck, a.TrainingMatrixCurrent,
            a.SixSStandardMaintained, a.TpmScheduleFollowed, a.StandardWorkMaintained,
            a.TrackingAgainstWeeklyPlan, a.ImprovementActionsProgressing,
        }.Count(v => v is null or > 2);

    private static SeniorAuditInputModel MapToInput(SeniorWeeklyAudit s) => new()
    {
        HandoverStandardsFollowed = s.HandoverStandardsFollowed,
        VisualManagementCurrent = s.VisualManagementCurrent,
        EscalationPathsUsed = s.EscalationPathsUsed,
        GovernanceNotes = s.GovernanceNotes,
        PpeComplianceFull = s.PpeComplianceFull,
        NearMissesReported = s.NearMissesReported,
        SafetyNotes = s.SafetyNotes,
        FirstOffRecordsComplete = s.FirstOffRecordsComplete,
        NcProcedureFollowed = s.NcProcedureFollowed ?? s.NcCaptureTrended,
        QualityGatesMaintained = s.QualityGatesMaintained,
        QualityNotes = s.QualityNotes,
        LeaderVisibilityCheck = s.LeaderVisibilityCheck ?? s.AbsenceManagedProactively,
        LastTeamMeeting = s.LastTeamMeeting,
        TrainingMatrixCurrent = s.TrainingMatrixCurrent,
        PeopleNotes = s.PeopleNotes,
        SixSStandardMaintained = s.SixSStandardMaintained,
        TpmScheduleFollowed = s.TpmScheduleFollowed,
        StandardWorkMaintained = s.StandardWorkMaintained ?? s.StandardWorkVisible,
        StandardsNotes = s.StandardsNotes,
        TrackingAgainstWeeklyPlan = s.TrackingAgainstWeeklyPlan,
        ImprovementActionsProgressing = s.ImprovementActionsProgressing,
        PerformanceNotes = s.PerformanceNotes,
        GoodPracticeObserved = s.GoodPracticeObserved,
        AreasForImprovement = s.AreasForImprovement,
        ActionsRaised = s.ActionsRaised,
        OverallVerdict = s.OverallVerdict,
    };

    private static void ApplyInput(SeniorWeeklyAudit s, SeniorAuditInputModel a)
    {
        s.HandoverStandardsFollowed = a.HandoverStandardsFollowed;
        s.VisualManagementCurrent = a.VisualManagementCurrent;
        s.EscalationPathsUsed = a.EscalationPathsUsed;
        s.GovernanceNotes = a.GovernanceNotes;
        s.PpeComplianceFull = a.PpeComplianceFull;
        s.NearMissesReported = a.NearMissesReported;
        s.SafetyNotes = a.SafetyNotes;
        s.FirstOffRecordsComplete = a.FirstOffRecordsComplete;
        s.NcProcedureFollowed = a.NcProcedureFollowed;
        s.QualityGatesMaintained = a.QualityGatesMaintained;
        s.QualityNotes = a.QualityNotes;
        s.LeaderVisibilityCheck = a.LeaderVisibilityCheck;
        s.LastTeamMeeting = a.LastTeamMeeting?.Trim();
        s.TrainingMatrixCurrent = a.TrainingMatrixCurrent;
        s.PeopleNotes = a.PeopleNotes;
        s.SixSStandardMaintained = a.SixSStandardMaintained;
        s.TpmScheduleFollowed = a.TpmScheduleFollowed;
        s.StandardWorkMaintained = a.StandardWorkMaintained;
        s.StandardsNotes = a.StandardsNotes;
        s.TrackingAgainstWeeklyPlan = a.TrackingAgainstWeeklyPlan;
        s.ImprovementActionsProgressing = a.ImprovementActionsProgressing;
        s.PerformanceNotes = a.PerformanceNotes;
        s.GoodPracticeObserved = a.GoodPracticeObserved;
        s.AreasForImprovement = a.AreasForImprovement;
        s.ActionsRaised = a.ActionsRaised;
        s.OverallVerdict = a.OverallVerdict;
    }
}
