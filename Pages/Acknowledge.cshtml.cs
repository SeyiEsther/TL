using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Pages;

public class AcknowledgeModel : PageModel
{
    private readonly AppDbContext _db;
    public AcknowledgeModel(AppDbContext db) => _db = db;

    public ShiftSubmission? PrevShift { get; set; }
    public Dictionary<int, string> ReasonTexts { get; set; } = new();

    // The NEW shift being started
    public string Date { get; set; } = "";
    public string Shift { get; set; } = "";
    public string Area { get; set; } = "";
    public string Tl { get; set; } = "";

    [BindProperty]
    public string? IncomingName { get; set; }

    public async Task<IActionResult> OnGetAsync(int? prevId, string? date, string? shift, string? area, string? tl)
    {
        Date = date ?? "";
        Shift = shift ?? "";
        Area = area ?? "";
        Tl = tl ?? "";

        if (prevId == null)
            return RedirectToPage("/Form", new { date = Date, shift = Shift, area = Area, tl = Tl });

        PrevShift = await _db.ShiftSubmissions
            .Include(s => s.Hours.OrderBy(h => h.HourNumber))
            .FirstOrDefaultAsync(s => s.Id == prevId.Value);

        if (PrevShift == null || string.IsNullOrEmpty(PrevShift.OutgoingTLSignature))
            return RedirectToPage("/Form", new { date = Date, shift = Shift, area = Area, tl = Tl });

        var reasons = await _db.MissedTargetReasons.ToListAsync();
        ReasonTexts = reasons.ToDictionary(r => r.Id, r => r.ReasonText);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int prevId, string date, string shift, string area, string tl)
    {
        var sub = await _db.ShiftSubmissions.FindAsync(prevId);
        if (sub != null && !string.IsNullOrWhiteSpace(IncomingName))
        {
            sub.IncomingTLSignature = IncomingName.Trim();
            sub.IncomingTLSignedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Form", new { date, shift, area, tl });
    }
}
