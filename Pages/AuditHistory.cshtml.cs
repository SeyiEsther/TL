using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TL.Pages;

public class AuditHistoryModel : PageModel
{
    public IActionResult OnGet(string? from, string? to, string? area, string? auditor) =>
        RedirectToPage("/HodHistory", new { from, to, area, q = auditor });
}
