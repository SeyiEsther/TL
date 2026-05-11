using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Pages;

public class HistoryModel : PageModel
{
    private readonly AppDbContext _db;
    public HistoryModel(AppDbContext db) => _db = db;

    public string? From { get; set; }
    public string? To { get; set; }
    public List<ShiftSummaryDto> Shifts { get; set; } = new();

    public async Task OnGetAsync(string? from, string? to)
    {
        From = from;
        To = to;

        var q = _db.ShiftSubmissions.Include(s => s.Hours).AsQueryable();
        if (!string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f)) q = q.Where(s => s.ShiftDate >= f);
        if (!string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var t)) q = q.Where(s => s.ShiftDate <= t);

        Shifts = await q
            .OrderByDescending(s => s.ShiftDate)
            .ThenBy(s => s.Shift)
            .Select(s => new ShiftSummaryDto
            {
                Id = s.Id,
                ShiftDate = s.ShiftDate,
                Shift = s.Shift,
                Area = s.Area,
                TeamLeaderDisplay = s.TeamLeaderDisplay,
                HoursCompleted = s.HoursCompleted,
                OverallSafetyStatus = s.Hours.Where(h => h.OverallSafetyStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallSafetyStatus).FirstOrDefault(),
                OverallQualityStatus = s.Hours.Where(h => h.OverallQualityStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallQualityStatus).FirstOrDefault(),
                OverallPerfStatus = s.Hours.Where(h => h.OverallPerfStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallPerfStatus).FirstOrDefault(),
                SubmittedAt = s.SubmittedAt,
                Escalations = s.Escalations,
            })
            .ToListAsync();
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };
}
