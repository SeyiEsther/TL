using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class AuditStartModel : PageModel
{
    private readonly UserService _users;
    public AuditStartModel(UserService users) => _users = users;

    public string AuditorName { get; set; } = "";
    public string AuditDate { get; set; } = "";
    public string SuggestedType { get; set; } = "";
    public string SuggestedTypeLabel { get; set; } = "";
    public string? Error { get; set; }

    public void OnGet()
    {
        var user = _users.GetCurrentUser();
        AuditorName = user.DisplayName;
        AuditDate = DateTime.Today.ToString("yyyy-MM-dd");
        SuggestedType = HodAuditTypes.SuggestedForDay(DateTime.Today.DayOfWeek);
        SuggestedTypeLabel = HodAuditTypes.LabelFor(SuggestedType);
    }

    public IActionResult OnPost(string auditDate, string auditorName, string department, string area, string auditType)
    {
        if (string.IsNullOrWhiteSpace(auditorName) || string.IsNullOrWhiteSpace(department) || string.IsNullOrWhiteSpace(area))
        {
            AuditorName = auditorName ?? "";
            AuditDate = auditDate ?? DateTime.Today.ToString("yyyy-MM-dd");
            SuggestedType = string.IsNullOrEmpty(auditType)
                ? HodAuditTypes.SuggestedForDay(DateTime.Today.DayOfWeek)
                : auditType;
            SuggestedTypeLabel = HodAuditTypes.LabelFor(SuggestedType);
            Error = "Please fill in all required fields.";
            return Page();
        }

        var type = string.IsNullOrWhiteSpace(auditType)
            ? HodAuditTypes.SuggestedForDay(DateTime.Today.DayOfWeek)
            : auditType;

        return RedirectToPage("/Audit", new { date = auditDate, auditor = auditorName, department, area, type });
    }
}
