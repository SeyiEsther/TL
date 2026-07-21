using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class DashboardModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ShiftCompletionService _completion;

    public DashboardModel(AppDbContext db, ShiftCompletionService completion)
    {
        _db = db;
        _completion = completion;
    }

    public string? From { get; set; }
    public string? To { get; set; }
    public string? ShiftFilter { get; set; }
    public string? AreaFilter { get; set; }
    public string? TlFilter { get; set; }
    public string CsvQuery { get; set; } = "";

    public List<ShiftSummaryDto> Shifts { get; set; } = new();
    public Dictionary<int, ShiftCompletionResult> CompletionById { get; set; } = new();
    public int IncompleteShifts { get; set; }
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

        try
        {
            await LoadDashboardDataAsync(from, to, shift, area, tl);
        }
        catch (Exception ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<DashboardModel>>()
                .LogError(ex, "Dashboard load failed");
            Shifts = [];
        }
    }

    async Task LoadDashboardDataAsync(string? from, string? to, string? shift, string? area, string? tl)
    {
        var q = _db.ShiftSubmissions.ExcludeAudits();
        if (!string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f)) q = q.Where(s => s.ShiftDate >= f);
        if (!string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var t)) q = q.Where(s => s.ShiftDate <= t);
        if (!string.IsNullOrEmpty(shift)) q = q.Where(s => s.Shift == shift);
        if (!string.IsNullOrEmpty(area)) q = q.Where(s => s.Area == area);
        if (!string.IsNullOrEmpty(tl)) q = q.Where(s => s.TeamLeaderDisplay.Contains(tl));

        var raw = await q
            .Include(s => s.Hours)
            .OrderByDescending(s => s.ShiftDate)
            .ThenBy(s => s.Shift)
            .ToListAsync();

        CompletionById = raw.ToDictionary(s => s.Id, s => _completion.Evaluate(s));
        IncompleteShifts = CompletionById.Count(kv => !kv.Value.IsComplete);

        Shifts = raw.Select(s => new ShiftSummaryDto
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
        }).ToList();
        var todayDate = DateOnly.FromDateTime(DateTime.Today);
        var weekAgo = todayDate.AddDays(-7);
        Today = Shifts.Count(s => s.ShiftDate == todayDate);
        ThisWeek = Shifts.Count(s => s.ShiftDate >= weekAgo);
        WithEscalations = Shifts.Count(s => !string.IsNullOrEmpty(s.Escalations));

        SafetyGreen = Shifts.Count(s => s.OverallSafetyStatus == "Green");
        SafetyAmber = Shifts.Count(s => s.OverallSafetyStatus == "Amber");
        SafetyRed = Shifts.Count(s => s.OverallSafetyStatus == "Red");
        QualityGreen = Shifts.Count(s => s.OverallQualityStatus == "Green");
        QualityAmber = Shifts.Count(s => s.OverallQualityStatus == "Amber");
        QualityRed = Shifts.Count(s => s.OverallQualityStatus == "Red");
        PerfGreen = Shifts.Count(s => s.OverallPerfStatus == "Green");
        PerfAmber = Shifts.Count(s => s.OverallPerfStatus == "Amber");
        PerfRed = Shifts.Count(s => s.OverallPerfStatus == "Red");

        DayShifts = Shifts.Count(s => s.Shift == "Day");
        AfternoonShifts = Shifts.Count(s => s.Shift == "Afternoon");
        NightShifts = Shifts.Count(s => s.Shift == "Night");

        var totalStatuses = SafetyGreen + SafetyAmber + SafetyRed + QualityGreen + QualityAmber + QualityRed + PerfGreen + PerfAmber + PerfRed;
        var weighted = (SafetyGreen + QualityGreen + PerfGreen) * 100 + (SafetyAmber + QualityAmber + PerfAmber) * 50;
        HealthScore = totalStatuses > 0 ? weighted / totalStatuses : 0;

        var last14 = Enumerable.Range(0, 14)
            .Select(i => DateOnly.FromDateTime(DateTime.Today.AddDays(-13 + i)))
            .ToList();
        ActivityLabels = last14.Select(d => d.ToString("dd/MM")).ToArray();
        ActivityData = last14.Select(d => Shifts.Count(s => s.ShiftDate == d)).ToArray();

        var areaGroups = Shifts
            .Where(s => !string.IsNullOrEmpty(s.Area))
            .GroupBy(s => s.Area!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToList();
        AreaLabels = areaGroups.Select(g => g.Key).ToArray();
        AreaData = areaGroups.Select(g => g.Count()).ToArray();

        WorstAreas = Shifts
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
