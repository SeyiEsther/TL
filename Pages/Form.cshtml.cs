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

    // Display properties (from query string or loaded submission)
    public string ShiftDate { get; set; } = "";
    public string Shift { get; set; } = "";
    public string TeamLeader { get; set; } = "";
    public string Area { get; set; } = "";
    public int Hours { get; set; } = 8;
    public int? EditingId { get; set; }

    // Form bound data
    [BindProperty] public List<HourInput> H { get; set; } = new();
    [BindProperty] public string? Escalations { get; set; }
    [BindProperty] public string? KeyRisks { get; set; }
    [BindProperty] public string? Priorities { get; set; }
    public string? OutgoingTLSignature { get; set; }

    public async Task<IActionResult> OnGetAsync(
        string? date, string? shift, string? area, string? tl,
        int? id, int hours = 8)
    {
        Hours = Math.Clamp(hours, 1, 8);

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
        }

        // Pad H to required length
        while (H.Count < Hours) H.Add(new HourInput());

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string shiftDate, string shift, string teamLeader, string area,
        int hoursCount, int? editingId, string? outgoingTLSignature)
    {
        ShiftDate = shiftDate;
        Shift = shift;
        TeamLeader = teamLeader;
        Area = area;
        Hours = Math.Clamp(hoursCount, 1, 8);
        EditingId = editingId;
        OutgoingTLSignature = outgoingTLSignature;

        // Trim H to the declared hours
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
            Track("OutgoingTLSignature", sub.OutgoingTLSignature, outgoingTLSignature);

            sub.Escalations = Escalations;
            sub.KeyRisks = KeyRisks;
            sub.Priorities = Priorities;
            sub.OutgoingTLSignature = outgoingTLSignature;
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
                    TrackHour("SafetyStatus", existing.OverallSafetyStatus, inp.Ss);
                    TrackHour("QualityStatus", existing.OverallQualityStatus, inp.Qs);
                    TrackHour("PerfStatus", existing.OverallPerfStatus, inp.Ps);

                    existing.HazardsObserved = inp.Haz; existing.UnsafeBehaviours = inp.Uns; existing.PositiveBehaviours = inp.Pos; existing.SafetyNotes = inp.Snote;
                    existing.QualityChecksCompleted = inp.Qchk; existing.DeviationsEscalated = inp.Dev; existing.NonComplianceAddressed = inp.Nc; existing.QualityNotes = inp.Qnote;
                    existing.HourlyTargetAchieved = inp.Tgt; existing.MaintenanceIssues = inp.Maint; existing.MaterialsAvailable = inp.Mat; existing.ToolsAvailable = inp.Tools;
                    existing.EscalationsNeeded = inp.Escl; existing.PartsConfirmed = inp.Pconf; existing.PartsIdCorrect = inp.Pid; existing.NCPartsStoredCorrectly = inp.Ncp;
                    existing.SixSCompleted = inp.Sixs; existing.TPMCompleted = inp.Tpm; existing.PerformanceNotes = inp.Pnote;
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
                OutgoingTLSignature = outgoingTLSignature,
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
        WellbeingConfirmed = h.Wb, SupportRequired = h.Sup, MoraleNotes = h.Mnote,
        AccidentsReported = h.Acc, OverallSafetyStatus = h.Ss, OverallQualityStatus = h.Qs, OverallPerfStatus = h.Ps,
    };
}
