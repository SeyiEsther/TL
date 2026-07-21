using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Services;

namespace TL.Pages;

public class HistoryModel : PageModel
{
    public IActionResult OnGet(string? tab, string? from, string? to, string? area, string? q, string? shift, string? auditor)
    {
        var person = q ?? auditor;
        return (tab ?? "all").ToLowerInvariant() switch
        {
            "hod" => RedirectToPage("/HodHistory", new { from, to, area, q = person }),
            "senior" => RedirectToPage("/SeniorHistory", new { from, to, area, q = person }),
            "shifts" => RedirectToPage("/ShiftHistory", new { from, to, area, shift, q = person }),
            _ => RedirectToPage("/ShiftHistory", new { from, to, area, shift, q = person }),
        };
    }
}
