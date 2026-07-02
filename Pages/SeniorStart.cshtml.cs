using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Services;

namespace TL.Pages;

public class SeniorStartModel : PageModel
{
    private readonly UserService _users;
    public SeniorStartModel(UserService users) => _users = users;

    public string AuditorName { get; set; } = "";
    public string AuditDate { get; set; } = "";
    public string? Error { get; set; }

    public void OnGet()
    {
        var user = _users.GetCurrentUser();
        AuditorName = user.DisplayName;
        AuditDate = DateTime.Today.ToString("yyyy-MM-dd");
    }

    public IActionResult OnPost(string auditDate, string auditorName, string area)
    {
        if (string.IsNullOrWhiteSpace(auditorName) || string.IsNullOrWhiteSpace(area))
        {
            AuditorName = auditorName ?? "";
            AuditDate = auditDate ?? DateTime.Today.ToString("yyyy-MM-dd");
            Error = "Please fill in all fields.";
            return Page();
        }
        return RedirectToPage("/SeniorAudit", new { date = auditDate, auditor = auditorName, area });
    }
}
