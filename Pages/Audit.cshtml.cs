using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class AuditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserService _users;

    public AuditModel(AppDbContext db, UserService users) { _db = db; _users = users; }

    public string AuditDate { get; set; } = "";
    public string AuditorName { get; set; } = "";
    public string Area { get; set; } = "";
    public int? EditingId { get; set; }

    [BindProperty] public HourInput A { get; set; } = new();
    [BindProperty] public string? Actions { get; set; }
    [BindProperty] public string? GoodPractice { get; set; }
    [BindProperty] public string? FollowUp { get; set; }
    public string? AuditorSignature { get; set; }

    public async Task<IActionResult> OnGetAsync(string? date, string? auditor, string? area, int? id)
    {
        if (id.HasValue)
        {
            var sub = await _db.ShiftSubmissions
                .Include(s => s.Hours.OrderBy(h => h.HourNumber))
                .FirstOrDefaultAsync(s => s.Id == id.Value && s.Shift == "Audit");
            if (sub == null) return RedirectToPage("/AuditStart");

            EditingId = sub.Id;
            AuditDate = sub.ShiftDate.ToString("yyyy-MM-dd");
            AuditorName = sub.TeamLeaderDisplay;
            Area = sub.Area ?? "";
            Actions = sub.Escalations;
            GoodPractice = sub.KeyRisks;
            FollowUp = sub.Priorities;
            AuditorSignature = sub.OutgoingTLSignature;

            var h = sub.Hours.FirstOrDefault();
            if (h != null) A = MapFromHour(h);
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
            var sub = await _db.ShiftSubmissions
                .Include(s => s.Hours)
                .FirstOrDefaultAsync(s => s.Id == editingId.Value);
            if (sub == null) return RedirectToPage("/AuditStart");

            sub.Escalations = Actions;
            sub.KeyRisks = GoodPractice;
            sub.Priorities = FollowUp;
            sub.OutgoingTLSignature = auditorSignature;
            sub.LastEditedBy = auditorName ?? user.DisplayName;
            sub.LastEditedAt = DateTime.UtcNow;

            var existing = sub.Hours.FirstOrDefault();
            if (existing != null)
                ApplyToHour(existing, A);
            else
                sub.Hours.Add(MakeHour(A));

            await _db.SaveChangesAsync();
            return RedirectToPage("/Success", new { id = editingId, audit = true });
        }
        else
        {
            if (!DateOnly.TryParse(auditDate, out var d)) d = DateOnly.FromDateTime(DateTime.Today);

            var sub = new ShiftSubmission
            {
                SubmittedBy = user.Username,
                TeamLeaderDisplay = auditorName ?? user.DisplayName,
                ShiftDate = d,
                Shift = "Audit",
                Area = area,
                HoursCompleted = 1,
                Escalations = Actions,
                KeyRisks = GoodPractice,
                Priorities = FollowUp,
                OutgoingTLSignature = auditorSignature,
                Hours = [MakeHour(A)]
            };

            _db.ShiftSubmissions.Add(sub);
            await _db.SaveChangesAsync();
            return RedirectToPage("/Success", new { id = sub.Id, audit = true });
        }
    }

    private static HourInput MapFromHour(HourlyCheck h) => new()
    {
        Haz = h.HazardsObserved, Uns = h.UnsafeBehaviours, Pos = h.PositiveBehaviours, Snote = h.SafetyNotes,
        Qchk = h.QualityChecksCompleted, Dev = h.DeviationsEscalated, Nc = h.NonComplianceAddressed, Qnote = h.QualityNotes,
        Tgt = h.HourlyTargetAchieved, Maint = h.MaintenanceIssues, Mat = h.MaterialsAvailable, Tools = h.ToolsAvailable,
        Escl = h.EscalationsNeeded, Pconf = h.PartsConfirmed, Pid = h.PartsIdCorrect, Ncp = h.NCPartsStoredCorrectly,
        Sixs = h.SixSCompleted, Tpm = h.TPMCompleted, Pnote = h.PerformanceNotes,
        Wb = h.WellbeingConfirmed, Sup = h.SupportRequired, Mnote = h.MoraleNotes,
        Acc = h.AccidentsReported, Ss = h.OverallSafetyStatus, Qs = h.OverallQualityStatus, Ps = h.OverallPerfStatus,
    };

    private static HourlyCheck MakeHour(HourInput a) => new()
    {
        HourNumber = 1,
        HazardsObserved = a.Haz, UnsafeBehaviours = a.Uns, PositiveBehaviours = a.Pos, SafetyNotes = a.Snote,
        QualityChecksCompleted = a.Qchk, DeviationsEscalated = a.Dev, NonComplianceAddressed = a.Nc, QualityNotes = a.Qnote,
        HourlyTargetAchieved = a.Tgt, MaintenanceIssues = a.Maint, MaterialsAvailable = a.Mat, ToolsAvailable = a.Tools,
        EscalationsNeeded = a.Escl, PartsConfirmed = a.Pconf, PartsIdCorrect = a.Pid, NCPartsStoredCorrectly = a.Ncp,
        SixSCompleted = a.Sixs, TPMCompleted = a.Tpm, PerformanceNotes = a.Pnote,
        WellbeingConfirmed = a.Wb, SupportRequired = a.Sup, MoraleNotes = a.Mnote,
        AccidentsReported = a.Acc, OverallSafetyStatus = a.Ss, OverallQualityStatus = a.Qs, OverallPerfStatus = a.Ps,
    };

    private static void ApplyToHour(HourlyCheck h, HourInput a)
    {
        h.HazardsObserved = a.Haz; h.UnsafeBehaviours = a.Uns; h.PositiveBehaviours = a.Pos; h.SafetyNotes = a.Snote;
        h.QualityChecksCompleted = a.Qchk; h.DeviationsEscalated = a.Dev; h.NonComplianceAddressed = a.Nc; h.QualityNotes = a.Qnote;
        h.HourlyTargetAchieved = a.Tgt; h.MaintenanceIssues = a.Maint; h.MaterialsAvailable = a.Mat; h.ToolsAvailable = a.Tools;
        h.EscalationsNeeded = a.Escl; h.PartsConfirmed = a.Pconf; h.PartsIdCorrect = a.Pid; h.NCPartsStoredCorrectly = a.Ncp;
        h.SixSCompleted = a.Sixs; h.TPMCompleted = a.Tpm; h.PerformanceNotes = a.Pnote;
        h.WellbeingConfirmed = a.Wb; h.SupportRequired = a.Sup; h.MoraleNotes = a.Mnote;
        h.AccidentsReported = a.Acc; h.OverallSafetyStatus = a.Ss; h.OverallQualityStatus = a.Qs; h.OverallPerfStatus = a.Ps;
    }
}
