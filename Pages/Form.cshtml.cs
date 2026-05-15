using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class FormModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserService _users;

    public FormModel(AppDbContext db, UserService users)
    {
        _db = db;
        _users = users;
    }

    public string ShiftDate { get; set; } = "";
    public string Shift { get; set; } = "";
    public string TeamLeader { get; set; } = "";
    public string Area { get; set; } = "";
    public int Hours { get; set; } = 8;
    public int? EditingId { get; set; }

    [BindProperty] public List<HourInput> H { get; set; } = new();
    [BindProperty] public string? Escalations { get; set; }
    [BindProperty] public string? KeyRisks { get; set; }
    [BindProperty] public string? Priorities { get; set; }
    [BindProperty] public string? OutgoingTLSignature { get; set; }

    public List<MissedTargetReason> TargetReasons { get; set; } = new();

    // Previous shift notes (for carry-forward display)
    public string? PrevEscalations { get; set; }
    public string? PrevKeyRisks { get; set; }
    public string? PrevPriorities { get; set; }
    public string? PrevTLName { get; set; }
    public string? PrevShiftLabel { get; set; }

    public async Task<IActionResult> OnGetAsync(
        string? date, string? shift, string? area, string? tl,
        int? id, int hours = 8)
    {
        Hours = Math.Clamp(hours, 1, 8);

        TargetReasons = await _db.MissedTargetReasons
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ToListAsync();

        if (id.HasValue)
        {
            var sub = await _db.ShiftSubmissions
                .Include(s => s.Hours.OrderBy(h => h.HourNumber))
                .Include(s => s.AuditLogs)
                .FirstOrDefaultAsync(s => s.Id == id.Value);

            if (sub == null) return RedirectToPage("/Index");

            EditingId = sub.Id;
            ShiftDate = sub.ShiftDate.ToString("yyyy-MM-dd");
            Shift = sub.Shift;
            TeamLeader = tl ?? sub.TeamLeaderDisplay;
            Area = sub.Area ?? "";
            Hours = sub.HoursCompleted;
            Escalations = sub.Escalations;
            KeyRisks = sub.KeyRisks;
            Priorities = sub.Priorities;
            OutgoingTLSignature = sub.OutgoingTLSignature;

            H = sub.Hours.OrderBy(h => h.HourNumber).Select(h => new HourInput
            {
                Haz = h.HazardsObserved, Uns = h.UnsafeBehaviours, Pos = h.PositiveBehaviours, Snote = h.SafetyNotes,
                Qchk = h.QualityChecksCompleted, Dev = h.DeviationsEscalated, Nc = h.NonComplianceAddressed, Qnote = h.QualityNotes,
                Tgt = h.HourlyTargetAchieved, Maint = h.MaintenanceIssues, Mat = h.MaterialsAvailable, Tools = h.ToolsAvailable,
                Escl = h.EscalationsNeeded, Pconf = h.PartsConfirmed, Pid = h.PartsIdCorrect, Ncp = h.NCPartsStoredCorrectly,
                Sixs = h.SixSCompleted, Tpm = h.TPMCompleted, Pnote = h.PerformanceNotes,
                TgtReason = h.MissedTargetReasonId, TgtNote = h.MissedTargetNote,
                Wb = h.WellbeingConfirmed, Sup = h.SupportRequired, Mnote = h.MoraleNotes,
                Acc = h.AccidentsReported, Ss = h.OverallSafetyStatus, Qs = h.OverallQualityStatus, Ps = h.OverallPerfStatus,
            }).ToList();
        }
        else
        {
            ShiftDate = date ?? DateTime.Today.ToString("yyyy-MM-dd");
            Shift = shift ?? "";
            TeamLeader = tl ?? "";
            Area = area ?? "";

            // Carry forward notes from the most recent previous submission for this area
            if (!string.IsNullOrEmpty(area))
            {
                var prev = await _db.ShiftSubmissions
                    .Where(s => s.Area == area)
                    .OrderByDescending(s => s.ShiftDate)
                    .ThenByDescending(s => s.SubmittedAt)
                    .FirstOrDefaultAsync();

                if (prev != null)
                {
                    PrevEscalations = prev.Escalations;
                    PrevKeyRisks = prev.KeyRisks;
                    PrevPriorities = prev.Priorities;
                    PrevTLName = prev.TeamLeaderDisplay;
                    PrevShiftLabel = $"{prev.Shift} shift — {prev.ShiftDate:d MMM}";
                }
            }
        }

        while (H.Count < Hours) H.Add(new HourInput());

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string shiftDate, string shift, string teamLeader, string area,
        int hoursCount, int? editingId)
    {
        ShiftDate = shiftDate;
        Shift = shift;
        TeamLeader = teamLeader;
        Area = area;
        Hours = Math.Clamp(hoursCount, 1, 8);
        EditingId = editingId;

        while (H.Count < Hours) H.Add(new HourInput());
        var hours = H.Take(Hours).ToList();

        var user = _users.GetCurrentUser();

        if (editingId.HasValue)
        {
            var sub = await _db.ShiftSubmissions
                .Include(s => s.Hours)
                .FirstOrDefaultAsync(s => s.Id == editingId.Value);

            if (sub == null) return RedirectToPage("/Index");

            var editorName = teamLeader ?? user.DisplayName;
            var logs = new List<AuditLog>();

            void Track(string field, string? oldVal, string? newVal)
            {
                if (oldVal != newVal)
                    logs.Add(new AuditLog { SubmissionId = sub.Id, ChangedBy = editorName, FieldName = field, OldValue = oldVal, NewValue = newVal });
            }

            Track("Escalations", sub.Escalations, Escalations);
            Track("KeyRisks", sub.KeyRisks, KeyRisks);
            Track("Priorities", sub.Priorities, Priorities);
            Track("OutgoingTLSignature", sub.OutgoingTLSignature, OutgoingTLSignature);

            sub.Escalations = Escalations;
            sub.KeyRisks = KeyRisks;
            sub.Priorities = Priorities;
            sub.OutgoingTLSignature = OutgoingTLSignature;
            sub.HoursCompleted = (byte)hours.Count;
            sub.LastEditedBy = editorName;
            sub.LastEditedAt = DateTime.UtcNow;

            for (int i = 0; i < hours.Count; i++)
            {
                var inp = hours[i];
                var existing = sub.Hours.FirstOrDefault(h => h.HourNumber == i + 1);
                if (existing == null)
                {
                    sub.Hours.Add(MapHour(inp, i + 1));
                    logs.Add(new AuditLog { SubmissionId = sub.Id, ChangedBy = editorName, FieldName = $"Hour{i + 1}", NewValue = "Created" });
                }
                else
                {
                    void TrackHour(string f, string? o, string? n) { if (o != n) logs.Add(new AuditLog { SubmissionId = sub.Id, ChangedBy = editorName, FieldName = $"Hr{i + 1}.{f}", OldValue = o, NewValue = n }); }
                    TrackHour("Hazards", existing.HazardsObserved?.ToString(), inp.Haz?.ToString());
                    TrackHour("Target", existing.HourlyTargetAchieved?.ToString(), inp.Tgt?.ToString());
                    TrackHour("MissedReason", existing.MissedTargetReasonId?.ToString(), inp.TgtReason?.ToString());
                    TrackHour("SafetyStatus", existing.OverallSafetyStatus, inp.Ss);
                    TrackHour("QualityStatus", existing.OverallQualityStatus, inp.Qs);
                    TrackHour("PerfStatus", existing.OverallPerfStatus, inp.Ps);

                    existing.HazardsObserved = inp.Haz; existing.UnsafeBehaviours = inp.Uns; existing.PositiveBehaviours = inp.Pos; existing.SafetyNotes = inp.Snote;
                    existing.QualityChecksCompleted = inp.Qchk; existing.DeviationsEscalated = inp.Dev; existing.NonComplianceAddressed = inp.Nc; existing.QualityNotes = inp.Qnote;
                    existing.HourlyTargetAchieved = inp.Tgt; existing.MaintenanceIssues = inp.Maint; existing.MaterialsAvailable = inp.Mat; existing.ToolsAvailable = inp.Tools;
                    existing.EscalationsNeeded = inp.Escl; existing.PartsConfirmed = inp.Pconf; existing.PartsIdCorrect = inp.Pid; existing.NCPartsStoredCorrectly = inp.Ncp;
                    existing.SixSCompleted = inp.Sixs; existing.TPMCompleted = inp.Tpm; existing.PerformanceNotes = inp.Pnote;
                    existing.MissedTargetReasonId = inp.Tgt == false ? inp.TgtReason : null;
                    existing.MissedTargetNote = inp.Tgt == false ? inp.TgtNote : null;
                    existing.WellbeingConfirmed = inp.Wb; existing.SupportRequired = inp.Sup; existing.MoraleNotes = inp.Mnote;
                    existing.AccidentsReported = inp.Acc; existing.OverallSafetyStatus = inp.Ss; existing.OverallQualityStatus = inp.Qs; existing.OverallPerfStatus = inp.Ps;
                }
            }

            _db.AuditLogs.AddRange(logs);
            await _db.SaveChangesAsync();
            return RedirectToPage("/Success", new { id = sub.Id });
        }
        else
        {
            if (!DateOnly.TryParse(shiftDate, out var d))
                d = DateOnly.FromDateTime(DateTime.Today);

            var sub = new ShiftSubmission
            {
                SubmittedBy = user.Username,
                TeamLeaderDisplay = teamLeader ?? user.DisplayName,
                ShiftDate = d,
                Shift = shift,
                Area = area,
                HoursCompleted = (byte)hours.Count,
                Escalations = Escalations,
                KeyRisks = KeyRisks,
                Priorities = Priorities,
                OutgoingTLSignature = OutgoingTLSignature,
                Hours = hours.Select((inp, i) => MapHour(inp, i + 1)).ToList()
            };

            _db.ShiftSubmissions.Add(sub);
            await _db.SaveChangesAsync();
            return RedirectToPage("/Success", new { id = sub.Id });
        }
    }

    private static HourlyCheck MapHour(HourInput h, int number) => new()
    {
        HourNumber = (byte)number,
        HazardsObserved = h.Haz, UnsafeBehaviours = h.Uns, PositiveBehaviours = h.Pos, SafetyNotes = h.Snote,
        QualityChecksCompleted = h.Qchk, DeviationsEscalated = h.Dev, NonComplianceAddressed = h.Nc, QualityNotes = h.Qnote,
        HourlyTargetAchieved = h.Tgt, MaintenanceIssues = h.Maint, MaterialsAvailable = h.Mat, ToolsAvailable = h.Tools,
        EscalationsNeeded = h.Escl, PartsConfirmed = h.Pconf, PartsIdCorrect = h.Pid, NCPartsStoredCorrectly = h.Ncp,
        SixSCompleted = h.Sixs, TPMCompleted = h.Tpm, PerformanceNotes = h.Pnote,
        MissedTargetReasonId = h.Tgt == false ? h.TgtReason : null,
        MissedTargetNote = h.Tgt == false ? h.TgtNote : null,
        WellbeingConfirmed = h.Wb, SupportRequired = h.Sup, MoraleNotes = h.Mnote,
        AccidentsReported = h.Acc, OverallSafetyStatus = h.Ss, OverallQualityStatus = h.Qs, OverallPerfStatus = h.Ps,
    };
}
