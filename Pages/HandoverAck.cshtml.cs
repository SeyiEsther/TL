using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Pages;

public class HandoverAckModel : PageModel
{
    private readonly AppDbContext _db;

    public HandoverAckModel(AppDbContext db) => _db = db;

    // Previous shift data to display
    public ShiftSubmission? PreviousShift { get; set; }
    public string? SafetyStatus { get; set; }
    public string? QualityStatus { get; set; }
    public string? PerfStatus { get; set; }

    // Parameters for the new shift being started
    public int PrevId { get; set; }
    public string NewDate { get; set; } = "";
    public string NewShift { get; set; } = "";
    public string NewArea { get; set; } = "";
    public string NewTL { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int prevId, string date, string shift, string area, string tl)
    {
        PrevId = prevId;
        NewDate = date;
        NewShift = shift;
        NewArea = area;
        NewTL = tl;

        PreviousShift = await _db.ShiftSubmissions
            .Include(s => s.Hours.OrderBy(h => h.HourNumber))
            .FirstOrDefaultAsync(s => s.Id == prevId);

        if (PreviousShift == null) return RedirectToPage("/Index");

        // Extract overall statuses from the last hour that has them
        SafetyStatus = PreviousShift.Hours
            .Where(h => h.OverallSafetyStatus != null)
            .OrderByDescending(h => h.HourNumber)
            .Select(h => h.OverallSafetyStatus)
            .FirstOrDefault();

        QualityStatus = PreviousShift.Hours
            .Where(h => h.OverallQualityStatus != null)
            .OrderByDescending(h => h.HourNumber)
            .Select(h => h.OverallQualityStatus)
            .FirstOrDefault();

        PerfStatus = PreviousShift.Hours
            .Where(h => h.OverallPerfStatus != null)
            .OrderByDescending(h => h.HourNumber)
            .Select(h => h.OverallPerfStatus)
            .FirstOrDefault();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        int prevId, string date, string shift, string area, string tl,
        string incomingSignature)
    {
        if (string.IsNullOrWhiteSpace(incomingSignature))
            return RedirectToPage("/HandoverAck", new { prevId, date, shift, area, tl });

        var sub = await _db.ShiftSubmissions.FindAsync(prevId);
        if (sub != null && string.IsNullOrEmpty(sub.IncomingTLSignature))
        {
            sub.IncomingTLSignature = incomingSignature.Trim();
            sub.IncomingTLSignedAt = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
            await _db.SaveChangesAsync();
        }

        // Now redirect to the new shift form
        return RedirectToPage("/Form", new { date, shift, area, tl });
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };
}
