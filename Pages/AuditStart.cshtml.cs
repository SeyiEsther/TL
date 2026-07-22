using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class AuditStartModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserService _users;
    private readonly HistoryListService _history;

    public AuditStartModel(AppDbContext db, UserService users, HistoryListService history)
    {
        _db = db;
        _users = users;
        _history = history;
    }

    public string AuditorName { get; set; } = "";
    public string AuditDate { get; set; } = "";
    public string SuggestedType { get; set; } = "";
    public string SuggestedTypeLabel { get; set; } = "";
    public string? Error { get; set; }
    public List<UnfinishedHodAudit> Unfinished { get; set; } = [];

    public async Task OnGetAsync()
    {
        var user = _users.GetCurrentUser();
        AuditorName = user.DisplayName;
        AuditDate = DateTime.Today.ToString("yyyy-MM-dd");
        SuggestedType = HodAuditTypes.SuggestedForDay(DateTime.Today.DayOfWeek);
        SuggestedTypeLabel = HodAuditTypes.LabelFor(SuggestedType);
        await LoadUnfinishedAsync(user.DisplayName);
    }

    public async Task<IActionResult> OnPostAsync(
        string auditDate, string auditorName, string department, string effectivenessArea, string auditType)
    {
        AuditorName = auditorName ?? "";
        AuditDate = auditDate ?? DateTime.Today.ToString("yyyy-MM-dd");
        if (DateOnly.TryParse(AuditDate, out var d))
        {
            SuggestedType = HodAuditTypes.SuggestedForDate(d);
            SuggestedTypeLabel = HodAuditTypes.LabelFor(SuggestedType);
        }

        if (string.IsNullOrWhiteSpace(auditorName)
            || string.IsNullOrWhiteSpace(department)
            || string.IsNullOrWhiteSpace(effectivenessArea))
        {
            Error = "Please fill in all required fields.";
            await LoadUnfinishedAsync(_users.GetCurrentUser().DisplayName);
            return Page();
        }

        if (!AreaList.IsInDepartment(effectivenessArea, department))
        {
            Error = "The effectiveness check zone must belong to the selected area.";
            await LoadUnfinishedAsync(_users.GetCurrentUser().DisplayName);
            return Page();
        }

        var type = string.IsNullOrWhiteSpace(auditType)
            ? (DateOnly.TryParse(auditDate, out var ad) ? HodAuditTypes.SuggestedForDate(ad) : HodAuditTypes.SuggestedForDay(DateTime.Today.DayOfWeek))
            : auditType;

        if (!DateOnly.TryParse(auditDate, out var auditD))
            auditD = DateOnly.FromDateTime(DateTime.Today);

        try
        {
            // One shared record per zone per day per type: if an audit already
            // exists for this natural key (AuditDate + Department +
            // EffectivenessArea + AuditType — no auditor), open it so everyone
            // continues the same record instead of starting a duplicate.
            var existing = await _db.HodDailyAudits
                .Where(a => a.AuditDate == auditD
                    && a.Department == department
                    && a.AuditType == type
                    && (a.EffectivenessArea == effectivenessArea
                        || ((a.EffectivenessArea == null || a.EffectivenessArea == "") && a.Area == effectivenessArea)))
                .OrderByDescending(a => a.LastEditedAt ?? a.SubmittedAt)
                .FirstOrDefaultAsync();
            if (existing != null)
                return RedirectToPage("/Audit", new { id = existing.Id });

            var user = _users.GetCurrentUser();
            var questions = HodAuditDefinitions.GetQuestions(type, department);
            var blankAnswers = questions.Select(q => new HodAuditAnswer
            {
                QuestionId = q.Id,
                Section = q.Section,
                Label = q.Label,
                MachineName = q.MachineName,
            }).ToList();

            var draft = new HodDailyAudit
            {
                SubmittedBy = user.Username,
                AuditorName = auditorName.Trim(),
                AuditDate = auditD,
                Department = department,
                EffectivenessArea = effectivenessArea,
                Area = effectivenessArea,
                AuditType = type,
                AnswersJson = HodAuditSerializer.ToJson(blankAnswers),
                TotalScore = 0,
                MaxScore = questions.Count,
            };

            _db.HodDailyAudits.Add(draft);
            await _db.SaveChangesAsync();
            return RedirectToPage("/Audit", new { id = draft.Id });
        }
        catch (Exception ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<AuditStartModel>>()
                .LogError(ex, "Could not create HoD audit draft for {Department}/{Area}", department, effectivenessArea);
            Error = "Could not start this audit — please try again.";
            await LoadUnfinishedAsync(_users.GetCurrentUser().DisplayName);
            return Page();
        }
    }

    async Task LoadUnfinishedAsync(string? displayName)
    {
        var unfinished = await _history.LoadUnfinishedHodAsync();
        // Show the current user's own unfinished audits first, then everyone else's.
        Unfinished = unfinished
            .OrderByDescending(u => PortalNameMatcher.Matches(u.AuditorName, displayName))
            .ThenByDescending(u => u.LastActivity)
            .ToList();
    }
}
