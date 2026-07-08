using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Pages;

public class AuditResultModel : PageModel
{
    private readonly AppDbContext _db;
    public AuditResultModel(AppDbContext db) => _db = db;

    public ShiftSubmission? Audit { get; set; }
    public HourlyCheck? Check { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Audit = await _db.ShiftSubmissions
            .Include(s => s.Hours.OrderBy(h => h.HourNumber))
            .FirstOrDefaultAsync(s => s.Id == id && s.Shift == ShiftQueryExtensions.AuditPseudoShift);

        if (Audit == null) return RedirectToPage("/History", new { tab = "hod" });

        Check = Audit.Hours.FirstOrDefault();
        return Page();
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };
    public static string Bn(bool? v) => v == true ? "Yes" : v == false ? "No" : "—";
}
