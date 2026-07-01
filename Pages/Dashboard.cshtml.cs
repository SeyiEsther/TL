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

    public int SafetyGreen { get; set; }
    public int SafetyAmber { get; set; }
    public int SafetyRed { get; set; }
    public int QualityGreen { get; set; }
    public int QualityAmber { get; set; }
    public int QualityRed { get; set; }
    public int PerfGreen { get; set; }
    public int PerfAmber { get; set; }
    public int PerfRed { get; set; }
    public int DayShifts { get; set; }
    public int AfternoonShifts { get; set; }
    public int NightShifts { get; set; }
    public int HealthScore { get; set; }
    public string[] ActivityLabels { get; set; } = [];
    public int[] ActivityData { get; set; } = [];
    public string[] AreaLabels { get; set; } = [];
    public int[] AreaData { get; set; } = [];
    public List<WorstAreaDto> WorstAreas { get; set; } = [];

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

        SafetyGreen = raw.Count(s => s.OverallSafetyStatus == "Green");
        SafetyAmber = raw.Count(s => s.OverallSafetyStatus == "Amber");
        SafetyRed = raw.Count(s => s.OverallSafetyStatus == "Red");
        QualityGreen = raw.Count(s => s.OverallQualityStatus == "Green");
        QualityAmber = raw.Count(s => s.OverallQualityStatus == "Amber");
        QualityRed = raw.Count(s => s.OverallQualityStatus == "Red");
        PerfGreen = raw.Count(s => s.OverallPerfStatus == "Green");
        PerfAmber = raw.Count(s => s.OverallPerfStatus == "Amber");
        PerfRed = raw.Count(s => s.OverallPerfStatus == "Red");

        DayShifts = raw.Count(s => s.Shift == "Day");
        AfternoonShifts = raw.Count(s => s.Shift == "Afternoon");
        NightShifts = raw.Count(s => s.Shift == "Night");

        var totalStatuses = SafetyGreen + SafetyAmber + SafetyRed + QualityGreen + QualityAmber + QualityRed + PerfGreen + PerfAmber + PerfRed;
        var weighted = (SafetyGreen + QualityGreen + PerfGreen) * 100 + (SafetyAmber + QualityAmber + PerfAmber) * 50;
        HealthScore = totalStatuses > 0 ? weighted / totalStatuses : 0;

        var last14 = Enumerable.Range(0, 14)
            .Select(i => DateOnly.FromDateTime(DateTime.Today.AddDays(-13 + i)))
            .ToList();
        ActivityLabels = last14.Select(d => d.ToString("dd/MM")).ToArray();
        ActivityData = last14.Select(d => raw.Count(s => s.ShiftDate == d)).ToArray();

        var areaGroups = raw
            .Where(s => !string.IsNullOrEmpty(s.Area))
            .GroupBy(s => s.Area!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToList();
        AreaLabels = areaGroups.Select(g => g.Key).ToArray();
        AreaData = areaGroups.Select(g => g.Count()).ToArray();

        WorstAreas = raw
            .Where(s => !string.IsNullOrEmpty(s.Area))
            .GroupBy(s => s.Area!)
            .Select(g => new WorstAreaDto(
                Area: g.Key,
                Reds: g.Sum(s =>
                    (s.OverallSafetyStatus  == "Red"   ? 1 : 0) +
                    (s.OverallQualityStatus == "Red"   ? 1 : 0) +
                    (s.OverallPerfStatus    == "Red"   ? 1 : 0)),
                Ambers: g.Sum(s =>
                    (s.OverallSafetyStatus  == "Amber" ? 1 : 0) +
                    (s.OverallQualityStatus == "Amber" ? 1 : 0) +
                    (s.OverallPerfStatus    == "Amber" ? 1 : 0)),
                Greens: g.Sum(s =>
                    (s.OverallSafetyStatus  == "Green" ? 1 : 0) +
                    (s.OverallQualityStatus == "Green" ? 1 : 0) +
                    (s.OverallPerfStatus    == "Green" ? 1 : 0)),
                TotalShifts: g.Count()
            ))
            .OrderByDescending(a => a.Reds)
            .ThenByDescending(a => a.Ambers)
            .Take(10)
            .ToList();

        var csvParams = new List<string>();
        if (!string.IsNullOrEmpty(from)) csvParams.Add("from=" + from);
        if (!string.IsNullOrEmpty(to)) csvParams.Add("to=" + to);
        CsvQuery = csvParams.Any() ? "?" + string.Join("&", csvParams) : "";
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };

    public string J(object o) => System.Text.Json.JsonSerializer.Serialize(o);
}

public record WorstAreaDto(string Area, int Reds, int Ambers, int Greens, int TotalShifts)
{
    public int Total => Reds + Ambers + Greens;
    public int RedPct  => Total > 0 ? Reds   * 100 / Total : 0;
    public int AmberPct => Total > 0 ? Ambers * 100 / Total : 0;
    public int GreenPct => Total > 0 ? Greens * 100 / Total : 0;
}
