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
    private readonly ShiftResumeService _resume;

    public FormModel(AppDbContext db, UserService users, ShiftCompletionService completion, ShiftResumeService resume)
    {
        _db = db;
        _users = users;
        _completion = completion;
        _resume = resume;
    }

    [BindProperty] public string ShiftDate { get; set; } = "";
    [BindProperty] public string Shift { get; set; } = "";
    [BindProperty] public string TeamLeader { get; set; } = "";
    [BindProperty] public string Area { get; set; } = "";
    [BindProperty(Name = "HoursCount")]
    public int Hours { get; set; } = 8;
    [BindProperty]
    public int? EditingId { get; set; }
    public string? SaveMessage { get; set; }

    [BindProperty] public List<HourInput> H { get; set; } = new();
    [BindProperty] public string? Escalations { get; set; }
    [BindProperty] public string? KeyRisks { get; set; }
    [BindProperty] public string? Priorities { get; set; }
    [BindProperty] public string? OutgoingTLSignature { get; set; }
    [BindProperty] public string? CoveringFor { get; set; }
    public string? ValidationError { get; set; }

    public async Task<IActionResult> OnGetAsync(
        string? date, string? shift, string? area, string? tl,
        int? id, int hours = 8, string? saved = null, string? coveringFor = null)
    {
        Hours = Math.Clamp(hours, 1, 8);
        SaveMessage = saved switch
        {
            "progress" => "Saved.",
            "hour" => "Saved.",
            _ => null,
        };

        if (id.HasValue)
        {
            var sub = await LoadSubmissionAsync(id.Value);
            if (sub == null) return RedirectToPage("/Index");
            PopulateFromSubmission(sub, tl);
            Hours = Math.Clamp(hours, 1, 8);
            // Sheetmetal uses a 2-hourly cadence (4 checks) for NEW shifts, but an
            // existing record must never lose data: show whatever it already has
            // (e.g. a legacy 8-hour shift), so nothing already captured is hidden.
            if (AreaList.IsSheetmetal(Area))
            {
                var maxHour = sub.Hours.Count == 0 ? 0 : sub.Hours.Max(h => (int)h.HourNumber);
                var existing = Math.Max((int)sub.HoursCompleted, maxHour);
                Hours = Math.Clamp(Math.Max(AreaList.SheetmetalChecks, existing), 1, 8);
            }
            if (!string.IsNullOrWhiteSpace(coveringFor))
                CoveringFor = coveringFor.Trim();
            PadHours();
            return Page();
        }

        ShiftDate = date ?? DateTime.Today.ToString("yyyy-MM-dd");
        Shift = shift ?? "";
        TeamLeader = tl ?? "";
        Area = area ?? "";
        if (AreaList.IsSheetmetal(Area)) Hours = AreaList.SheetmetalChecks;

        if (DateOnly.TryParse(ShiftDate, out var d) && !string.IsNullOrWhiteSpace(Shift) && !string.IsNullOrWhiteSpace(Area))
        {
            var tlName = ShiftResumeService.NormalizeTl(TeamLeader);
            if (string.IsNullOrWhiteSpace(tlName))
                return RedirectToPage("/Index");

            var user = _users.GetCurrentUser();

            CoveringFor = string.IsNullOrWhiteSpace(coveringFor) ? null : coveringFor.Trim();

            SlotResolution resolution;
            try
            {
                resolution = await _resume.ResolveOrCreateOpenAsync(d, Shift, Area, () => new ShiftSubmission
                {
                    SubmittedBy = user.Username,
                    TeamLeaderDisplay = tlName,
                    CoveringFor = CoveringFor,
                    ShiftDate = d,
                    Shift = Shift,
                    Area = Area,
                    HoursCompleted = (byte)Hours,
                });
            }
            catch (Exception ex)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<FormModel>>()
                    .LogError(ex, "Could not start shift for {Area} {Shift} {Date}", Area, Shift, ShiftDate);
                ValidationError = "Could not start this shift — please try again.";
                PadHours();
                return Page();
            }

            if (resolution.Closed)
            {
                TempData["Error"] = "This shift is already closed. Open it from History if you need to view or edit.";
                return RedirectToPage("/Index");
            }

            return RedirectToPage("/Form", new { id = resolution.Shift!.Id, hours = Hours, tl = tlName, coveringFor = CoveringFor });
        }

        PadHours();
        return Page();
    }

    public Task<IActionResult> OnPostSaveProgressAsync(int? id)
    {
        ApplyRouteId(id);
        return SaveAsync(finalSubmit: false);
    }

    public Task<IActionResult> OnPostAsync(int? id)
    {
        ApplyRouteId(id);
        return SaveAsync(finalSubmit: true);
    }

    void ApplyRouteId(int? id)
    {
        if (!EditingId.HasValue && id.HasValue)
            EditingId = id;
    }

    async Task<IActionResult> SaveAsync(bool finalSubmit)
    {
        // A new sheetmetal shift defaults to 4 (2-hourly) checks, but an existing
        // record keeps whatever count it was opened with — the posted HoursCount —
        // so a legacy hourly shift never has hours 5–8 truncated on save.
        if (AreaList.IsSheetmetal(Area) && !EditingId.HasValue)
            Hours = Math.Max(Hours, AreaList.SheetmetalChecks);
        PadHours();

        var user = _users.GetCurrentUser();
        var editorName = ShiftResumeService.NormalizeTl(TeamLeader);
        if (string.IsNullOrWhiteSpace(editorName))
        {
            ValidationError = "Team leader name is required — go back to Home and enter your name.";
            PadHours();
            return await SaveErrorResultAsync(finalSubmit, "Team leader name is required.");
        }
        TeamLeader = editorName;
        var hours = H.Take(Hours).ToList();

        if (finalSubmit && string.IsNullOrWhiteSpace(OutgoingTLSignature))
        {
            ValidationError = "Outgoing TL sign-off is required to close the shift.";
            PadHours();
            return await SaveErrorResultAsync(finalSubmit, ValidationError);
        }

        try
        {
            var sub = await ResolveSubmissionAsync(user, editorName);
            EditingId = sub.Id;

            var logs = new List<AuditLog>();
            ApplyShiftFields(sub, OutgoingTLSignature, editorName, logs, finalSubmit);
            var hoursMerged = MergeHours(sub, hours, editorName, logs);

            sub.TeamLeaderDisplay = editorName;
            sub.LastEditedBy = editorName;
            sub.LastEditedAt = DateTime.UtcNow;
            _db.AuditLogs.AddRange(logs);
            await _db.SaveChangesAsync();

            if (finalSubmit)
            {
                if (WantsJsonResponse())
                    return new JsonResult(new { redirect = Url.Page("/Success", new { id = sub.Id }) });
                return RedirectToPage("/Success", new { id = sub.Id });
            }

            if (WantsJsonResponse())
            {
                var progress = _completion.Evaluate(sub);
                return new JsonResult(new
                {
                    id = sub.Id,
                    savedAt = DateTime.UtcNow,
                    hoursComplete = progress.HoursComplete,
                    hoursTotal = progress.HoursTotal,
                    hoursMerged,
                    message = hoursMerged > 0 ? "Progress saved" : "Shift record saved (no hour data in request)",
                });
            }

            return RedirectToPage("/Form", new { id = sub.Id, hours = Hours, saved = "progress" });
        }
        catch (Exception ex)
        {
            return await SaveErrorResultAsync(
                finalSubmit,
                "Could not save — please try again. If this keeps happening, contact IT.",
                ex);
        }
    }

    bool WantsJsonResponse() =>
        Request.Headers.Accept.Any(h =>
            !string.IsNullOrEmpty(h) && h.Contains("application/json", StringComparison.OrdinalIgnoreCase));

    Task<IActionResult> SaveErrorResultAsync(bool finalSubmit, string message, Exception? ex = null)
    {
        if (ex != null)
            HttpContext.RequestServices.GetRequiredService<ILogger<FormModel>>()
                .LogError(ex, "Form save failed (finalSubmit={FinalSubmit}, editingId={EditingId})", finalSubmit, EditingId);

        if (WantsJsonResponse())
            return Task.FromResult<IActionResult>(new JsonResult(new { error = message }) { StatusCode = 422 });

        ValidationError = message;
        PadHours();
        return Task.FromResult<IActionResult>(Page());
    }

    async Task<ShiftSubmission> ResolveSubmissionAsync(AppUser user, string editorName)
    {
        DateOnly slotDate = default;
        var hasSlot = DateOnly.TryParse(ShiftDate, out slotDate)
            && !string.IsNullOrWhiteSpace(Shift)
            && !string.IsNullOrWhiteSpace(Area);

        if (EditingId.HasValue)
        {
            var byId = await _db.ShiftSubmissions
                .ExcludeAudits()
                .Include(s => s.Hours)
                .FirstOrDefaultAsync(s => s.Id == EditingId.Value);
            if (byId != null)
                return byId;

            throw new InvalidOperationException(
                "This shift session no longer exists. Go Home and open the shift again.");
        }

        if (hasSlot)
        {
            var existing = await _resume.FindForResumeAsync(slotDate, Shift, Area);
            if (existing != null)
            {
                if (!_db.Entry(existing).Collection(s => s.Hours).IsLoaded)
                    await _db.Entry(existing).Collection(s => s.Hours).LoadAsync();
                return existing;
            }
        }

        return await CreateSubmissionAsync(ShiftDate, Shift, editorName, Area, user);
    }

    async Task<ShiftSubmission> CreateSubmissionAsync(string shiftDate, string shift, string teamLeader, string area, AppUser user)
    {
        if (!DateOnly.TryParse(shiftDate, out var d))
            d = DateOnly.FromDateTime(DateTime.Today);

        var tlName = ShiftResumeService.NormalizeTl(teamLeader);
        if (string.IsNullOrEmpty(tlName))
            throw new InvalidOperationException("Team leader name is required.");

        var resolution = await _resume.ResolveOrCreateOpenAsync(d, shift, area, () => new ShiftSubmission
        {
            SubmittedBy = user.Username,
            TeamLeaderDisplay = tlName,
            ShiftDate = d,
            Shift = shift,
            Area = area,
            HoursCompleted = (byte)Hours,
        });

        if (resolution.Closed)
            throw new InvalidOperationException("This shift is already closed.");

        return resolution.Shift!;
    }

    void ApplyShiftFields(
        ShiftSubmission sub, string? outgoingTLSignature, string editorName, List<AuditLog> logs, bool finalSubmit)
    {
        void Track(string field, string? oldVal, string? newVal)
        {
            if (oldVal != newVal)
                logs.Add(new AuditLog { SubmissionId = sub.Id, ChangedBy = editorName, FieldName = field, OldValue = oldVal, NewValue = newVal });
        }

        var covering = string.IsNullOrWhiteSpace(CoveringFor) ? null : CoveringFor.Trim();

        Track("Escalations", sub.Escalations, Escalations);
        Track("KeyRisks", sub.KeyRisks, KeyRisks);
        Track("Priorities", sub.Priorities, Priorities);
        Track("CoveringFor", sub.CoveringFor, covering);

        sub.Escalations = Escalations;
        sub.KeyRisks = KeyRisks;
        sub.Priorities = Priorities;
        sub.CoveringFor = covering;
        sub.HoursCompleted = (byte)Hours;

        // Signature closes the slot for resume — only persist on Submit & close, never on Save progress / autosave.
        if (finalSubmit)
        {
            var signature = string.IsNullOrWhiteSpace(outgoingTLSignature) ? null : outgoingTLSignature.Trim();
            Track("OutgoingTLSignature", sub.OutgoingTLSignature, signature);
            sub.OutgoingTLSignature = signature;
        }
    }

    int MergeHours(ShiftSubmission sub, List<HourInput> hours, string editorName, List<AuditLog> logs)
    {
        var merged = 0;
        for (int i = 0; i < hours.Count; i++)
        {
            var inp = hours[i];
            if (!inp.HasAnyData()) continue;

            var hourNum = i + 1;
            var existing = sub.Hours.FirstOrDefault(h => h.HourNumber == hourNum);
            var replaceAll = HourMergeHelper.IsHourComplete(inp);

            if (existing == null)
            {
                sub.Hours.Add(MapHour(inp, hourNum));
                logs.Add(new AuditLog { SubmissionId = sub.Id, ChangedBy = editorName, FieldName = $"Hour{hourNum}", NewValue = "Created" });
                merged++;
                continue;
            }

            var before = SnapshotHour(existing);
            HourMergeHelper.MergeInto(existing, inp, replaceAll);
            if (!HourSnapshotsEqual(before, existing))
            {
                merged++;
                LogHourChanges(sub.Id, hourNum, editorName, before, existing, logs);
            }
        }
        return merged;
    }

    static (bool? Haz, bool? Tgt, string? Ss, string? Qs, string? Ps) SnapshotHour(HourlyCheck h) =>
        (h.HazardsObserved, h.HourlyTargetAchieved, h.OverallSafetyStatus, h.OverallQualityStatus, h.OverallPerfStatus);

    static bool HourSnapshotsEqual(
        (bool? Haz, bool? Tgt, string? Ss, string? Qs, string? Ps) before,
        HourlyCheck after) =>
        before.Haz == after.HazardsObserved
        && before.Tgt == after.HourlyTargetAchieved
        && before.Ss == after.OverallSafetyStatus
        && before.Qs == after.OverallQualityStatus
        && before.Ps == after.OverallPerfStatus;

    static void LogHourChanges(
        int submissionId, int hourNum, string editorName,
        (bool? Haz, bool? Tgt, string? Ss, string? Qs, string? Ps) before,
        HourlyCheck after, List<AuditLog> logs)
    {
        void Track(string f, string? o, string? n)
        {
            if (o != n)
                logs.Add(new AuditLog { SubmissionId = submissionId, ChangedBy = editorName, FieldName = $"Hr{hourNum}.{f}", OldValue = o, NewValue = n });
        }
        Track("Hazards", before.Haz?.ToString(), after.HazardsObserved?.ToString());
        Track("Target", before.Tgt?.ToString(), after.HourlyTargetAchieved?.ToString());
        Track("SafetyStatus", before.Ss, after.OverallSafetyStatus);
        Track("QualityStatus", before.Qs, after.OverallQualityStatus);
        Track("PerfStatus", before.Ps, after.OverallPerfStatus);
    }

    async Task<ShiftSubmission?> LoadSubmissionAsync(int id) =>
        await _db.ShiftSubmissions
            .ExcludeAudits()
            .Include(s => s.Hours.OrderBy(h => h.HourNumber))
            .Include(s => s.AuditLogs)
            .FirstOrDefaultAsync(s => s.Id == id);

    void PopulateFromSubmission(ShiftSubmission sub, string? tl)
    {
        EditingId = sub.Id;
        ShiftDate = sub.ShiftDate.ToString("yyyy-MM-dd");
        Shift = sub.Shift;
        TeamLeader = ShiftResumeService.NormalizeTl(tl ?? sub.TeamLeaderDisplay);
        Area = sub.Area ?? "";
        // Never show fewer rows than the record actually has data for.
        var maxHour = sub.Hours.Count == 0 ? 0 : sub.Hours.Max(h => (int)h.HourNumber);
        Hours = Math.Clamp(Math.Max((int)sub.HoursCompleted, maxHour), 1, 8);
        Escalations = sub.Escalations;
        KeyRisks = sub.KeyRisks;
        Priorities = sub.Priorities;
        OutgoingTLSignature = sub.OutgoingTLSignature;
        CoveringFor = sub.CoveringFor;

        H = new List<HourInput>();
        for (int hourNum = 1; hourNum <= Hours; hourNum++)
        {
            var dbHour = sub.Hours.FirstOrDefault(h => h.HourNumber == hourNum);
            H.Add(dbHour != null ? MapHourInput(dbHour) : new HourInput());
        }
    }

    static HourInput MapHourInput(HourlyCheck h) => new()
    {
        Haz = h.HazardsObserved, Uns = h.UnsafeBehaviours, Pos = h.PositiveBehaviours, Snote = h.SafetyNotes,
        Qchk = h.QualityChecksCompleted, Dev = h.DeviationsEscalated, Nc = h.NonComplianceAddressed,
        Qiplan = h.ToolsMatchInspectionPlan, Pkb = h.PreKitBreakInFollowed, Qnote = h.QualityNotes,
        Tgt = h.HourlyTargetAchieved, Maint = h.MaintenanceIssues, Mat = h.MaterialsAvailable, Tools = h.ToolsAvailable,
        Escl = h.EscalationsNeeded, Pconf = h.PartsConfirmed, Pid = h.PartsIdCorrect, Ncp = h.NCPartsStoredCorrectly,
        Sixs = h.SixSCompleted, Tpm = h.TPMCompleted, Pnote = h.PerformanceNotes,
        Wb = h.WellbeingConfirmed, Sup = h.SupportRequired, Mnote = h.MoraleNotes,
        Acc = h.AccidentsReported, Ss = h.OverallSafetyStatus, Qs = h.OverallQualityStatus, Ps = h.OverallPerfStatus,
    };

    void PadHours()
    {
        while (H.Count < Hours) H.Add(new HourInput());
    }

    private static HourlyCheck MapHour(HourInput h, int number) => new()
    {
        HourNumber = (byte)number,
        HazardsObserved = h.Haz, UnsafeBehaviours = h.Uns, PositiveBehaviours = h.Pos, SafetyNotes = h.Snote,
        QualityChecksCompleted = h.Qchk, DeviationsEscalated = h.Dev, NonComplianceAddressed = h.Nc,
        ToolsMatchInspectionPlan = h.Qiplan, PreKitBreakInFollowed = h.Pkb, QualityNotes = h.Qnote,
        HourlyTargetAchieved = h.Tgt, MaintenanceIssues = h.Maint, MaterialsAvailable = h.Mat, ToolsAvailable = h.Tools,
        EscalationsNeeded = h.Escl, PartsConfirmed = h.Pconf, PartsIdCorrect = h.Pid, NCPartsStoredCorrectly = h.Ncp,
        SixSCompleted = h.Sixs, TPMCompleted = h.Tpm, PerformanceNotes = h.Pnote,
        WellbeingConfirmed = h.Wb, SupportRequired = h.Sup, MoraleNotes = h.Mnote,
        AccidentsReported = h.Acc, OverallSafetyStatus = h.Ss, OverallQualityStatus = h.Qs, OverallPerfStatus = h.Ps,
    };
}
