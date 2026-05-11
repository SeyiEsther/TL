using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Pages;

public class DashboardModel : PageModel
{
    private readonly AppDbContext _db;
    public DashboardModel(AppDbContext db) => _db = db;

    public string? From { get; set; }
    public string? To { get; set; }
    public string? ShiftFilter { get; set; }
    public string? AreaFilter { get; set; }
    public string? TlFilter { get; set; }
    public string CsvQuery { get; set; } = "";

    public List<ShiftSummaryDto> Shifts { get; set; } = new();
    public int ThisWeek { get; set; }
    public int Today { get; set; }
    public int WithEscalations { get; set; }

    public async Task OnGetAsync(string? from, string? to, string? shift, string? area, string? tl)
    {
        From = from;
        To = to;
        ShiftFilter = shift;
        AreaFilter = area;
        TlFilter = tl;

        var q = _db.ShiftSubmissions.Include(s => s.Hours).AsQueryable();
        if (!string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f)) q = q.Where(s => s.ShiftDate >= f);
        if (!string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var t)) q = q.Where(s => s.ShiftDate <= t);
        if (!string.IsNullOrEmpty(shift)) q = q.Where(s => s.Shift == shift);
        if (!string.IsNullOrEmpty(area)) q = q.Where(s => s.Area == area);
        if (!string.IsNullOrEmpty(tl)) q = q.Where(s => s.TeamLeaderDisplay.Contains(tl));

        var raw = await q
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

        Shifts = raw;
        var todayDate = DateOnly.FromDateTime(DateTime.Today);
        var weekAgo = todayDate.AddDays(-7);
        Today = raw.Count(s => s.ShiftDate == todayDate);
        ThisWeek = raw.Count(s => s.ShiftDate >= weekAgo);
        WithEscalations = raw.Count(s => !string.IsNullOrEmpty(s.Escalations));

        var csvParams = new List<string>();
        if (!string.IsNullOrEmpty(from)) csvParams.Add("from=" + from);
        if (!string.IsNullOrEmpty(to)) csvParams.Add("to=" + to);
        CsvQuery = csvParams.Any() ? "?" + string.Join("&", csvParams) : "";
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };
}
