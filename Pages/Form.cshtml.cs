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
    private readonly ShiftCompletionService _completion;

    public FormModel(AppDbContext db, UserService users, ShiftCompletionService completion)
    {
        _db = db;
        _users = users;
        _completion = completion;
    }

    public string ShiftDate { get; set; } = "";
    public string Shift { get; set; } = "";
    public string TeamLeader { get; set; } = "";
    public string Area { get; set; } = "";
    public int Hours { get; set; } = 8;
    public int? EditingId { get; set; }
    public string? SaveMessage { get; set; }

    [BindProperty] public List<HourInput> H { get; set; } = new();
    [BindProperty] public string? Escalations { get; set; }
    [BindProperty] public string? KeyRisks { get; set; }
    [BindProperty] public string? Priorities { get; set; }
    [BindProperty] public string? OutgoingTLSignature { get; set; }
    public string? ValidationError { get; set; }

    public async Task<IActionResult> OnGetAsync(
        string? date, string? shift, string? area, string? tl,
        int? id, int hours = 8, string? saved = null)
    {
        Hours = Math.Clamp(hours, 1, 8);
        SaveMessage = saved switch
        {
            "progress" => "Progress saved to server.",
            "hour" => "Hour saved to server.",
            _ => null,
        };

        if (id.HasValue)
        {
            var sub = await LoadSubmissionAsync(id.Value);
            if (sub == null) return RedirectToPage("/Index");
            PopulateFromSubmission(sub, tl);
            PadHours();
            return Page();
        }

        ShiftDate = date ?? DateTime.Today.ToString("yyyy-MM-dd");
        Shift = shift ?? "";
        TeamLeader = tl ?? "";
        Area = area ?? "";

        if (DateOnly.TryParse(ShiftDate, out var d) && !string.IsNullOrWhiteSpace(Shift) && !string.IsNullOrWhiteSpace(Area))
        {
            var user = _users.GetCurrentUser();
            var tlName = string.IsNullOrWhiteSpace(TeamLeader) ? user.DisplayName : TeamLeader;

            var existing = await _db.ShiftSubmissions
                .ExcludeAudits()
                .FirstOrDefaultAsync(s =>
                    s.ShiftDate == d && s.Shift == Shift && s.Area == Area && s.TeamLeaderDisplay == tlName);

            if (existing != null)
                return RedirectToPage("/Form", new { id = existing.Id, hours = Hours, tl = tlName });

            var stub = new ShiftSubmission
            {
                SubmittedBy = user.Username,
                TeamLeaderDisplay = tlName,
                ShiftDate = d,
                Shift = Shift,
                Area = Area,
                HoursCompleted = (byte)Hours,
            };
            _db.ShiftSubmissions.Add(stub);
            await _db.SaveChangesAsync();
            return RedirectToPage("/Form", new { id = stub.Id, hours = Hours, tl = tlName });
        }

        PadHours();
        return Page();
    }

    public Task<IActionResult> OnPostSaveProgressAsync(
        string shiftDate, string shift, string teamLeader, string area,
        int hoursCount, int? editingId, string? outgoingTLSignature)
        => SaveAsync(shiftDate, shift, teamLeader, area, hoursCount, editingId, outgoingTLSignature, finalSubmit: false);

    public Task<IActionResult> OnPostAsync(
        string shiftDate, string shift, string teamLeader, string area,
        int hoursCount, int? editingId, string? outgoingTLSignature)
        => SaveAsync(shiftDate, shift, teamLeader, area, hoursCount, editingId, outgoingTLSignature, finalSubmit: true);

    async Task<IActionResult> SaveAsync(
        string shiftDate, string shift, string teamLeader, string area,
        int hoursCount, int? editingId, string? outgoingTLSignature, bool finalSubmit)
    {
        ShiftDate = shiftDate;
        Shift = shift;
        TeamLeader = teamLeader;
        Area = area;
        Hours = Math.Clamp(hoursCount, 1, 8);
        EditingId = editingId;
        OutgoingTLSignature = outgoingTLSignature;
        PadHours();

        var user = _users.GetCurrentUser();
        var editorName = teamLeader ?? user.DisplayName;
        var hours = H.Take(Hours).ToList();

        ShiftSubmission sub;
        if (editingId.HasValue)
        {
            sub = await _db.ShiftSubmissions
                .Include(s => s.Hours)
                .FirstOrDefaultAsync(s => s.Id == editingId.Value)
                ?? await CreateSubmissionAsync(shiftDate, shift, teamLeader, area, user);
            EditingId = sub.Id;
        }
        else
        {
            sub = await CreateSubmissionAsync(shiftDate, shift, teamLeader, area, user);
            EditingId = sub.Id;
        }

        var logs = new List<AuditLog>();
        ApplyShiftFields(sub, outgoingTLSignature, editorName, logs);
        MergeHours(sub, hours, editorName, logs);

        if (finalSubmit)
        {
            var check = _completion.Evaluate(sub);
            if (!check.IsComplete)
            {
                ValidationError = "Cannot submit — complete all hours, 6S/TPM checks, and sign-off first: "
                    + string.Join("; ", check.MissingItems.Take(5))
                    + (check.MissingItems.Count > 5 ? $" (+{check.MissingItems.Count - 5} more)" : "");
                RepopulateFromSubmission(sub, teamLeader);
                return Page();
            }
        }

        sub.LastEditedBy = editorName;
        sub.LastEditedAt = DateTime.UtcNow;
        _db.AuditLogs.AddRange(logs);
        await _db.SaveChangesAsync();

        if (finalSubmit)
            return RedirectToPage("/Success", new { id = sub.Id });

        if (Request.Headers.Accept.Any(h => h.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
        {
            var progress = _completion.Evaluate(sub);
            return new JsonResult(new
            {
                id = sub.Id,
                savedAt = DateTime.UtcNow,
                hoursComplete = progress.HoursComplete,
                hoursTotal = progress.HoursTotal,
                message = "Progress saved",
            });
        }

        return RedirectToPage("/Form", new { id = sub.Id, hours = Hours, saved = "progress" });
    }

    async Task<ShiftSubmission> CreateSubmissionAsync(string shiftDate, string shift, string teamLeader, string area, AppUser user)
    {
        if (!DateOnly.TryParse(shiftDate, out var d))
            d = DateOnly.FromDateTime(DateTime.Today);

        var tlName = teamLeader ?? user.DisplayName;
        var existing = await _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .FirstOrDefaultAsync(s =>
                s.ShiftDate == d && s.Shift == shift && s.Area == area && s.TeamLeaderDisplay == tlName);

        if (existing != null) return existing;

        var sub = new ShiftSubmission
        {
            SubmittedBy = user.Username,
            TeamLeaderDisplay = tlName,
            ShiftDate = d,
            Shift = shift,
            Area = area,
            HoursCompleted = (byte)Hours,
        };
        _db.ShiftSubmissions.Add(sub);
        await _db.SaveChangesAsync();
        return sub;
    }

    void ApplyShiftFields(ShiftSubmission sub, string? outgoingTLSignature, string editorName, List<AuditLog> logs)
    {
        void Track(string field, string? oldVal, string? newVal)
        {
            if (oldVal != newVal)
                logs.Add(new AuditLog { SubmissionId = sub.Id, ChangedBy = editorName, FieldName = field, OldValue = oldVal, NewValue = newVal });
        }

        Track("Escalations", sub.Escalations, Escalations);
        Track("KeyRisks", sub.KeyRisks, KeyRisks);
        Track("Priorities", sub.Priorities, Priorities);
        if (!string.IsNullOrWhiteSpace(outgoingTLSignature))
            Track("OutgoingTLSignature", sub.OutgoingTLSignature, outgoingTLSignature);

        sub.Escalations = Escalations;
        sub.KeyRisks = KeyRisks;
        sub.Priorities = Priorities;
        if (!string.IsNullOrWhiteSpace(outgoingTLSignature))
            sub.OutgoingTLSignature = outgoingTLSignature;
        sub.HoursCompleted = (byte)Hours;
    }

    void MergeHours(ShiftSubmission sub, List<HourInput> hours, string editorName, List<AuditLog> logs)
    {
        for (int i = 0; i < hours.Count; i++)
        {
            var inp = hours[i];
            if (!inp.HasAnyData()) continue;

            var hourNum = i + 1;
            var existing = sub.Hours.FirstOrDefault(h => h.HourNumber == hourNum);
            if (existing == null)
            {
                sub.Hours.Add(MapHour(inp, hourNum));
                logs.Add(new AuditLog { SubmissionId = sub.Id, ChangedBy = editorName, FieldName = $"Hour{hourNum}", NewValue = "Created" });
                continue;
            }

            void TrackHour(string f, string? o, string? n)
            {
                if (o != n) logs.Add(new AuditLog { SubmissionId = sub.Id, ChangedBy = editorName, FieldName = $"Hr{hourNum}.{f}", OldValue = o, NewValue = n });
            }

            TrackHour("Hazards", existing.HazardsObserved?.ToString(), inp.Haz?.ToString());
            TrackHour("Target", existing.HourlyTargetAchieved?.ToString(), inp.Tgt?.ToString());
            TrackHour("SafetyStatus", existing.OverallSafetyStatus, inp.Ss);
            TrackHour("QualityStatus", existing.OverallQualityStatus, inp.Qs);
            TrackHour("PerfStatus", existing.OverallPerfStatus, inp.Ps);

            existing.HazardsObserved = inp.Haz; existing.UnsafeBehaviours = inp.Uns; existing.PositiveBehaviours = inp.Pos;
            existing.SafetyNotes = inp.Snote;
            existing.QualityChecksCompleted = inp.Qchk; existing.DeviationsEscalated = inp.Dev; existing.NonComplianceAddressed = inp.Nc;
            existing.QualityNotes = inp.Qnote;
            existing.HourlyTargetAchieved = inp.Tgt; existing.MaintenanceIssues = inp.Maint; existing.MaterialsAvailable = inp.Mat;
            existing.ToolsAvailable = inp.Tools;
            existing.EscalationsNeeded = inp.Escl; existing.PartsConfirmed = inp.Pconf; existing.PartsIdCorrect = inp.Pid;
            existing.NCPartsStoredCorrectly = inp.Ncp;
            existing.SixSCompleted = inp.Sixs; existing.TPMCompleted = inp.Tpm; existing.PerformanceNotes = inp.Pnote;
            existing.WellbeingConfirmed = inp.Wb; existing.SupportRequired = inp.Sup; existing.MoraleNotes = inp.Mnote;
            existing.AccidentsReported = inp.Acc; existing.OverallSafetyStatus = inp.Ss; existing.OverallQualityStatus = inp.Qs;
            existing.OverallPerfStatus = inp.Ps;
        }
    }

    async Task<ShiftSubmission?> LoadSubmissionAsync(int id) =>
        await _db.ShiftSubmissions
            .Include(s => s.Hours.OrderBy(h => h.HourNumber))
            .Include(s => s.AuditLogs)
            .FirstOrDefaultAsync(s => s.Id == id);

    void PopulateFromSubmission(ShiftSubmission sub, string? tl)
    {
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

    void RepopulateFromSubmission(ShiftSubmission sub, string? tl)
    {
        PopulateFromSubmission(sub, tl);
        PadHours();
    }

    void PadHours()
    {
        while (H.Count < Hours) H.Add(new HourInput());
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
