using System.Text.Json;
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
    public string Area { get; set; } = "";
    public string AuditType { get; set; } = "";
    public string AuditTypeLabel { get; set; } = "";
    public int? EditingId { get; set; }

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
        string? date, string? auditor, string? department, string? area, string? type, int? id)
    {
        if (id.HasValue)
        {
            var audit = await _db.HodDailyAudits.FirstOrDefaultAsync(a => a.Id == id.Value);
            if (audit == null) return RedirectToPage("/AuditStart");

            EditingId = audit.Id;
            AuditDate = audit.AuditDate.ToString("yyyy-MM-dd");
            AuditorName = audit.AuditorName;
            Department = audit.Department;
            Area = audit.Area;
            AuditType = audit.AuditType;
            Actions = audit.ActionsRaised;
            GoodPractice = audit.GoodPractice;
            AuditorSignature = audit.AuditorSignature;
            TeamLeaderSignature = audit.TeamLeaderSignature;
            Answers = HodAuditSerializer.ParseAnswers(audit.AnswersJson);
            EffectivenessFindings = HodAuditSerializer.ParseEffectiveness(audit.EffectivenessJson);
            TotalScore = audit.TotalScore;
            MaxScore = audit.MaxScore;
        }
        else
        {
            AuditDate = date ?? DateTime.Today.ToString("yyyy-MM-dd");
            AuditorName = auditor ?? "";
            Department = department ?? AreaList.GetDepartment(area);
            Area = area ?? "";
            AuditType = type ?? HodAuditTypes.SuggestedForDay(DateTime.Today.DayOfWeek);

            if (string.IsNullOrWhiteSpace(Area) || string.IsNullOrWhiteSpace(AuditorName))
                return RedirectToPage("/AuditStart");

            if (DateOnly.TryParse(AuditDate, out var ad))
                EffectivenessFindings = await _effectiveness.GetFindingsAsync(Department, Area, ad, AuditType);
        }

        AuditTypeLabel = HodAuditTypes.LabelFor(AuditType);
        Questions = HodAuditDefinitions.GetQuestions(AuditType, Area);
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

    public async Task<IActionResult> OnPostAsync(
        string auditDate, string auditorName, string department, string area, string auditType,
        int? editingId, string? auditorSignature, string? teamLeaderSignature)
    {
        AuditDate = auditDate;
        AuditorName = auditorName;
        Department = department;
        Area = area;
        AuditType = auditType;
        EditingId = editingId;
        AuditorSignature = auditorSignature;
        TeamLeaderSignature = teamLeaderSignature;
        AuditTypeLabel = HodAuditTypes.LabelFor(AuditType);
        Questions = HodAuditDefinitions.GetQuestions(AuditType, Area);

        var answers = BuildAnswers();
        var missing = answers.Count(a => !a.Pass.HasValue);
        if (missing > 0)
        {
            Answers = answers;
            A = answers.Select(a => new HodAnswerInput
            {
                QuestionId = a.QuestionId,
                Pass = a.Pass,
                Evidence = a.Evidence,
            }).ToList();
            if (DateOnly.TryParse(AuditDate, out var ad))
                EffectivenessFindings = await _effectiveness.GetFindingsAsync(Department, Area, ad, AuditType);
            ModelState.AddModelError("", $"Answer all {missing} remaining question(s) — each must be Pass (1) or Fail (0).");
            return Page();
        }

        (TotalScore, MaxScore) = HodAuditScoring.Score(answers);
        var user = _users.GetCurrentUser();

        List<HodEffectivenessFinding> effectiveness;
        if (DateOnly.TryParse(AuditDate, out var auditD))
            effectiveness = await _effectiveness.GetFindingsAsync(Department, Area, auditD, AuditType);
        else
            effectiveness = [];

        if (editingId.HasValue)
        {
            var existing = await _db.HodDailyAudits.FirstOrDefaultAsync(a => a.Id == editingId.Value);
            if (existing == null) return RedirectToPage("/AuditStart");

            existing.AuditorName = auditorName ?? user.DisplayName;
            existing.AuditDate = auditD;
            existing.Department = department;
            existing.Area = area;
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
            Area = area,
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

    List<HodAuditAnswer> BuildAnswers()
    {
        var inputById = A.ToDictionary(a => a.QuestionId, a => a);
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
