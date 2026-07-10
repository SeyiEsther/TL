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
    private readonly HodEffectivenessService _effectiveness;

    public AuditModel(AppDbContext db, UserService users, HodEffectivenessService effectiveness)
    {
        _db = db;
        _users = users;
        _effectiveness = effectiveness;
    }

    public string AuditDate { get; set; } = "";
    public string AuditorName { get; set; } = "";
    public string Department { get; set; } = "";
    public string EffectivenessArea { get; set; } = "";
    public string AuditType { get; set; } = "";
    public string AuditTypeLabel { get; set; } = "";
    public int? EditingId { get; set; }
    public string? SaveMessage { get; set; }

    public List<HodAuditQuestion> Questions { get; set; } = [];
    public List<HodAuditAnswer> Answers { get; set; } = [];
    public List<HodEffectivenessFinding> EffectivenessFindings { get; set; } = [];
    public int TotalScore { get; set; }
    public int MaxScore { get; set; }
    public string RatingBand { get; set; } = "";
    public string RatingDetail { get; set; } = "";

    [BindProperty] public List<HodAnswerInput> A { get; set; } = [];
    [BindProperty] public string? Actions { get; set; }
    [BindProperty] public string? GoodPractice { get; set; }
    [BindProperty] public string? AuditorSignature { get; set; }
    [BindProperty] public string? TeamLeaderSignature { get; set; }

    public async Task<IActionResult> OnGetAsync(
        string? date, string? auditor, string? department, string? effectivenessArea, string? area, string? type, int? id,
        string? saved = null)
    {
        SaveMessage = saved switch
        {
            "progress" => "Changes saved.",
            _ => null,
        };

        if (id.HasValue)
        {
            var audit = await _db.HodDailyAudits.FirstOrDefaultAsync(a => a.Id == id.Value);
            if (audit == null) return RedirectToPage("/AuditStart");

            EditingId = audit.Id;
            AuditDate = audit.AuditDate.ToString("yyyy-MM-dd");
            AuditorName = audit.AuditorName;
            Department = audit.Department;
            EffectivenessArea = audit.ResolveEffectivenessArea();
            AuditType = audit.AuditType;
            Actions = audit.ActionsRaised;
            GoodPractice = audit.GoodPractice;
            AuditorSignature = audit.AuditorSignature;
            TeamLeaderSignature = audit.TeamLeaderSignature;
            Answers = HodAuditSerializer.ParseAnswers(audit.AnswersJson);
            if (DateOnly.TryParse(AuditDate, out var editDate))
                EffectivenessFindings = await _effectiveness.GetFindingsAsync(
                    Department, EffectivenessArea, editDate, AuditType);
            else
                EffectivenessFindings = HodAuditSerializer.ParseEffectiveness(audit.EffectivenessJson);
            TotalScore = audit.TotalScore;
            MaxScore = audit.MaxScore;
        }
        else
        {
            AuditDate = date ?? DateTime.Today.ToString("yyyy-MM-dd");
            AuditorName = auditor ?? "";
            Department = department ?? "";
            EffectivenessArea = effectivenessArea ?? area ?? "";
            AuditType = type ?? HodAuditTypes.SuggestedForDay(DateTime.Today.DayOfWeek);

            if (string.IsNullOrWhiteSpace(Department)
                || string.IsNullOrWhiteSpace(EffectivenessArea)
                || string.IsNullOrWhiteSpace(AuditorName))
                return RedirectToPage("/AuditStart");

            if (!AreaList.IsInDepartment(EffectivenessArea, Department))
                return RedirectToPage("/AuditStart");

            if (DateOnly.TryParse(AuditDate, out var ad))
                EffectivenessFindings = await _effectiveness.GetFindingsAsync(
                    Department, EffectivenessArea, ad, AuditType);
        }

        AuditTypeLabel = HodAuditTypes.LabelFor(AuditType);
        Questions = HodAuditDefinitions.GetQuestions(AuditType, Department);
        MaxScore = Questions.Count;

        if (Answers.Count == 0)
            Answers = Questions.Select(q => new HodAuditAnswer
            {
                QuestionId = q.Id,
                Section = q.Section,
                Label = q.Label,
                MachineName = q.MachineName,
            }).ToList();

        if (A.Count == 0)
            A = Answers.Select(a => new HodAnswerInput
            {
                QuestionId = a.QuestionId,
                Pass = a.Pass,
                Evidence = a.Evidence,
            }).ToList();

        if (TotalScore == 0 && MaxScore > 0 && Answers.Any(a => a.Pass.HasValue))
            (TotalScore, MaxScore) = HodAuditScoring.Score(Answers);

        RatingBand = HodAuditScoring.RatingBand(TotalScore, MaxScore);
        RatingDetail = HodAuditScoring.RatingDetail(TotalScore, MaxScore);

        return Page();
    }

    public async Task<IActionResult> OnPostSaveProgressAsync(
        string auditDate, string auditorName, string department, string effectivenessArea, string auditType,
        int? editingId, string? auditorSignature, string? teamLeaderSignature)
    {
        if (!editingId.HasValue)
            return RedirectToPage("/AuditStart");

        AuditDate = auditDate;
        AuditorName = auditorName;
        Department = department;
        EffectivenessArea = effectivenessArea;
        AuditType = auditType;
        EditingId = editingId;
        AuditorSignature = auditorSignature;
        TeamLeaderSignature = teamLeaderSignature;
        AuditTypeLabel = HodAuditTypes.LabelFor(AuditType);
        Questions = HodAuditDefinitions.GetQuestions(AuditType, Department);

        try
        {
            var existing = await _db.HodDailyAudits.FirstOrDefaultAsync(a => a.Id == editingId.Value);
            if (existing == null)
                return RedirectToPage("/AuditStart");

            if (!DateOnly.TryParse(auditDate, out var auditD))
            {
                ModelState.AddModelError("", "Invalid audit date.");
                await RepopulateForErrorAsync(MergeAnswersWithExisting(existing.AnswersJson));
                return Page();
            }

            var answers = MergeAnswersWithExisting(existing.AnswersJson);
            (TotalScore, MaxScore) = HodAuditScoring.Score(answers);
            var user = _users.GetCurrentUser();
            var effectiveness = await _effectiveness.GetFindingsAsync(
                Department, EffectivenessArea, auditD, AuditType);
            effectiveness = HodFailEffectivenessLinker.LinkFailures(answers, effectiveness, AuditType);
            Actions = HodFailEffectivenessLinker.MergeActions(Actions, answers, effectiveness);

            existing.AuditorName = auditorName ?? user.DisplayName;
            existing.AuditDate = auditD;
            existing.Department = department;
            existing.EffectivenessArea = effectivenessArea;
            existing.Area = effectivenessArea;
            existing.AuditType = auditType;
            existing.AnswersJson = HodAuditSerializer.ToJson(answers);
            existing.TotalScore = TotalScore;
            existing.MaxScore = MaxScore;
            existing.EffectivenessJson = HodAuditSerializer.EffectivenessToJson(effectiveness);
            existing.ActionsRaised = Actions;
            existing.GoodPractice = GoodPractice;
            if (!string.IsNullOrWhiteSpace(auditorSignature))
                existing.AuditorSignature = auditorSignature;
            if (!string.IsNullOrWhiteSpace(teamLeaderSignature))
                existing.TeamLeaderSignature = teamLeaderSignature;
            existing.LastEditedBy = auditorName ?? user.DisplayName;
            existing.LastEditedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToPage("/Audit", new { id = editingId, saved = "progress" });
        }
        catch (Exception ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<AuditModel>>()
                .LogError(ex, "Audit save progress failed for id {EditingId}", editingId);
            ModelState.AddModelError("", "Could not save changes — please try again.");
            var fallback = await _db.HodDailyAudits.FirstOrDefaultAsync(a => a.Id == editingId.Value);
            await RepopulateForErrorAsync(MergeAnswersWithExisting(fallback?.AnswersJson));
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync(
        string auditDate, string auditorName, string department, string effectivenessArea, string auditType,
        int? editingId, string? auditorSignature, string? teamLeaderSignature)
    {
        AuditDate = auditDate;
        AuditorName = auditorName;
        Department = department;
        EffectivenessArea = effectivenessArea;
        AuditType = auditType;
        EditingId = editingId;
        AuditorSignature = auditorSignature;
        TeamLeaderSignature = teamLeaderSignature;
        AuditTypeLabel = HodAuditTypes.LabelFor(AuditType);
        Questions = HodAuditDefinitions.GetQuestions(AuditType, Department);

        var answers = BuildAnswers();
        var missing = answers.Count(a => !a.Pass.HasValue);
        if (missing > 0)
        {
            await RepopulateForErrorAsync(answers);
            ModelState.AddModelError("", $"Answer all {missing} remaining question(s) — each must be Pass (1) or Fail (0).");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(auditorSignature))
        {
            await RepopulateForErrorAsync(answers);
            ModelState.AddModelError("", "Auditor signature is required before submitting.");
            return Page();
        }

        if (!DateOnly.TryParse(auditDate, out var auditD))
        {
            await RepopulateForErrorAsync(answers);
            ModelState.AddModelError("", "Invalid audit date.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Department) || string.IsNullOrWhiteSpace(EffectivenessArea))
        {
            await RepopulateForErrorAsync(answers);
            ModelState.AddModelError("", "Department and effectiveness zone are required.");
            return Page();
        }

        (TotalScore, MaxScore) = HodAuditScoring.Score(answers);
        var user = _users.GetCurrentUser();
        var effectiveness = await _effectiveness.GetFindingsAsync(
            Department, EffectivenessArea, auditD, AuditType);
        effectiveness = HodFailEffectivenessLinker.LinkFailures(answers, effectiveness, AuditType);
        Actions = HodFailEffectivenessLinker.MergeActions(Actions, answers, effectiveness);

        try
        {
            if (editingId.HasValue)
            {
                var existing = await _db.HodDailyAudits.FirstOrDefaultAsync(a => a.Id == editingId.Value);
                if (existing == null) return RedirectToPage("/AuditStart");

                existing.AuditorName = auditorName ?? user.DisplayName;
                existing.AuditDate = auditD;
                existing.Department = department;
                existing.EffectivenessArea = effectivenessArea;
                existing.Area = effectivenessArea;
                existing.AuditType = auditType;
                existing.AnswersJson = HodAuditSerializer.ToJson(answers);
                existing.TotalScore = TotalScore;
                existing.MaxScore = MaxScore;
                existing.EffectivenessJson = HodAuditSerializer.EffectivenessToJson(effectiveness);
                existing.ActionsRaised = Actions;
                existing.GoodPractice = GoodPractice;
                existing.AuditorSignature = auditorSignature;
                existing.TeamLeaderSignature = teamLeaderSignature;
                existing.LastEditedBy = auditorName ?? user.DisplayName;
                existing.LastEditedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return RedirectToPage("/Success", new { hodAuditId = editingId });
            }

            var audit = new HodDailyAudit
            {
                SubmittedBy = user.Username,
                AuditorName = auditorName ?? user.DisplayName,
                AuditDate = auditD,
                Department = department,
                EffectivenessArea = effectivenessArea,
                Area = effectivenessArea,
                AuditType = auditType,
                AnswersJson = HodAuditSerializer.ToJson(answers),
                TotalScore = TotalScore,
                MaxScore = MaxScore,
                EffectivenessJson = HodAuditSerializer.EffectivenessToJson(effectiveness),
                ActionsRaised = Actions,
                GoodPractice = GoodPractice,
                AuditorSignature = auditorSignature,
                TeamLeaderSignature = teamLeaderSignature,
            };

            _db.HodDailyAudits.Add(audit);
            await _db.SaveChangesAsync();
            return RedirectToPage("/Success", new { hodAuditId = audit.Id });
        }
        catch (Exception ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<AuditModel>>()
                .LogError(ex, "Audit submit failed for id {EditingId}", editingId);
            ModelState.AddModelError("", "Could not save audit — please try again.");
            await RepopulateForErrorAsync(answers);
            return Page();
        }
    }

    async Task RepopulateForErrorAsync(List<HodAuditAnswer> answers)
    {
        Answers = answers;
        A = answers.Select(a => new HodAnswerInput
        {
            QuestionId = a.QuestionId,
            Pass = a.Pass,
            Evidence = a.Evidence,
        }).ToList();
        MaxScore = Questions.Count;
        RatingBand = HodAuditScoring.RatingBand(TotalScore, MaxScore);
        RatingDetail = HodAuditScoring.RatingDetail(TotalScore, MaxScore);
        if (DateOnly.TryParse(AuditDate, out var ad))
            EffectivenessFindings = await _effectiveness.GetFindingsAsync(
                Department, EffectivenessArea, ad, AuditType);
    }

    List<HodAuditAnswer> MergeAnswersWithExisting(string? existingJson)
    {
        var stored = HodAuditSerializer.ParseAnswers(existingJson);
        var storedById = stored.ToDictionary(a => a.QuestionId);
        var inputById = A
            .Where(a => !string.IsNullOrEmpty(a.QuestionId))
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last());

        return Questions.Select(q =>
        {
            inputById.TryGetValue(q.Id, out var inp);
            storedById.TryGetValue(q.Id, out var prev);
            var pass = inp?.Pass ?? prev?.Pass;
            var evidence = !string.IsNullOrWhiteSpace(inp?.Evidence) ? inp.Evidence : prev?.Evidence;
            return new HodAuditAnswer
            {
                QuestionId = q.Id,
                Section = q.Section,
                Label = q.Label,
                MachineName = q.MachineName,
                Pass = pass,
                Evidence = evidence,
            };
        }).ToList();
    }

    List<HodAuditAnswer> BuildAnswers()
    {
        var inputById = A
            .Where(a => !string.IsNullOrEmpty(a.QuestionId))
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last());
        return Questions.Select(q =>
        {
            inputById.TryGetValue(q.Id, out var inp);
            return new HodAuditAnswer
            {
                QuestionId = q.Id,
                Section = q.Section,
                Label = q.Label,
                MachineName = q.MachineName,
                Pass = inp?.Pass,
                Evidence = inp?.Evidence,
            };
        }).ToList();
    }
}

public class HodAnswerInput
{
    public string QuestionId { get; set; } = "";
    public bool? Pass { get; set; }
    public string? Evidence { get; set; }
}
