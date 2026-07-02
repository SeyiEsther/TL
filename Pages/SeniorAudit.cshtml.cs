using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class SeniorAuditInputModel
{
    public bool? HandoverStandardsFollowed { get; set; }
    public bool? VisualManagementCurrent { get; set; }
    public bool? EscalationPathsUsed { get; set; }
    public string? GovernanceNotes { get; set; }

    public bool? PpeComplianceFull { get; set; }
    public bool? NearMissesReported { get; set; }
    public bool? SafetyActionLogCurrent { get; set; }
    public string? SafetyNotes { get; set; }

    public bool? FirstOffRecordsComplete { get; set; }
    public bool? NcCaptureTrended { get; set; }
    public bool? QualityGatesMaintained { get; set; }
    public string? QualityNotes { get; set; }

    public bool? AbsenceManagedProactively { get; set; }
    public bool? TlsCoachingTeams { get; set; }
    public bool? TrainingMatrixCurrent { get; set; }
    public string? PeopleNotes { get; set; }

    public bool? SixSStandardMaintained { get; set; }
    public bool? TpmScheduleFollowed { get; set; }
    public bool? StandardWorkVisible { get; set; }
    public string? StandardsNotes { get; set; }

    public bool? TrackingAgainstWeeklyPlan { get; set; }
    public bool? MetricsVisibleAndOwned { get; set; }
    public bool? ImprovementActionsProgressing { get; set; }
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
        else
        {
            if (!DateOnly.TryParse(auditDate, out var d)) d = DateOnly.FromDateTime(DateTime.Today);

            var sub = new SeniorWeeklyAudit
            {
                SubmittedBy = user.Username,
                AuditorName = auditorName ?? user.DisplayName,
                AuditDate = d,
                Area = area,
                AuditorSignature = auditorSignature,
            };
            ApplyInput(sub, A);

            _db.SeniorWeeklyAudits.Add(sub);
            await _db.SaveChangesAsync();
            return RedirectToPage("/SeniorSuccess", new { id = sub.Id });
        }
    }

    private static SeniorAuditInputModel MapToInput(SeniorWeeklyAudit s) => new()
    {
        HandoverStandardsFollowed = s.HandoverStandardsFollowed,
        VisualManagementCurrent   = s.VisualManagementCurrent,
        EscalationPathsUsed       = s.EscalationPathsUsed,
        GovernanceNotes           = s.GovernanceNotes,
        PpeComplianceFull         = s.PpeComplianceFull,
        NearMissesReported        = s.NearMissesReported,
        SafetyActionLogCurrent    = s.SafetyActionLogCurrent,
        SafetyNotes               = s.SafetyNotes,
        FirstOffRecordsComplete   = s.FirstOffRecordsComplete,
        NcCaptureTrended          = s.NcCaptureTrended,
        QualityGatesMaintained    = s.QualityGatesMaintained,
        QualityNotes              = s.QualityNotes,
        AbsenceManagedProactively = s.AbsenceManagedProactively,
        TlsCoachingTeams          = s.TlsCoachingTeams,
        TrainingMatrixCurrent     = s.TrainingMatrixCurrent,
        PeopleNotes               = s.PeopleNotes,
        SixSStandardMaintained    = s.SixSStandardMaintained,
        TpmScheduleFollowed       = s.TpmScheduleFollowed,
        StandardWorkVisible       = s.StandardWorkVisible,
        StandardsNotes            = s.StandardsNotes,
        TrackingAgainstWeeklyPlan     = s.TrackingAgainstWeeklyPlan,
        MetricsVisibleAndOwned        = s.MetricsVisibleAndOwned,
        ImprovementActionsProgressing = s.ImprovementActionsProgressing,
        PerformanceNotes          = s.PerformanceNotes,
        GoodPracticeObserved      = s.GoodPracticeObserved,
        AreasForImprovement       = s.AreasForImprovement,
        ActionsRaised             = s.ActionsRaised,
        OverallVerdict            = s.OverallVerdict,
    };

    private static void ApplyInput(SeniorWeeklyAudit s, SeniorAuditInputModel a)
    {
        s.HandoverStandardsFollowed   = a.HandoverStandardsFollowed;
        s.VisualManagementCurrent     = a.VisualManagementCurrent;
        s.EscalationPathsUsed         = a.EscalationPathsUsed;
        s.GovernanceNotes             = a.GovernanceNotes;
        s.PpeComplianceFull           = a.PpeComplianceFull;
        s.NearMissesReported          = a.NearMissesReported;
        s.SafetyActionLogCurrent      = a.SafetyActionLogCurrent;
        s.SafetyNotes                 = a.SafetyNotes;
        s.FirstOffRecordsComplete     = a.FirstOffRecordsComplete;
        s.NcCaptureTrended            = a.NcCaptureTrended;
        s.QualityGatesMaintained      = a.QualityGatesMaintained;
        s.QualityNotes                = a.QualityNotes;
        s.AbsenceManagedProactively   = a.AbsenceManagedProactively;
        s.TlsCoachingTeams            = a.TlsCoachingTeams;
        s.TrainingMatrixCurrent       = a.TrainingMatrixCurrent;
        s.PeopleNotes                 = a.PeopleNotes;
        s.SixSStandardMaintained      = a.SixSStandardMaintained;
        s.TpmScheduleFollowed         = a.TpmScheduleFollowed;
        s.StandardWorkVisible         = a.StandardWorkVisible;
        s.StandardsNotes              = a.StandardsNotes;
        s.TrackingAgainstWeeklyPlan       = a.TrackingAgainstWeeklyPlan;
        s.MetricsVisibleAndOwned          = a.MetricsVisibleAndOwned;
        s.ImprovementActionsProgressing   = a.ImprovementActionsProgressing;
        s.PerformanceNotes            = a.PerformanceNotes;
        s.GoodPracticeObserved        = a.GoodPracticeObserved;
        s.AreasForImprovement         = a.AreasForImprovement;
        s.ActionsRaised               = a.ActionsRaised;
        s.OverallVerdict              = a.OverallVerdict;
    }
}
