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
    private readonly TargetService _targets;

    public DashboardModel(AppDbContext db, ShiftCompletionService completion, TargetService targets)
    {
        _db = db;
        _completion = completion;
        _targets = targets;
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

    // Weekly report scope (ISO Monday–Sunday). The dashboard defaults to the
    // current week and can page back to any past week; nothing is deleted.
    // Targets are admin-editable and pulled from the database (item 3); they are
    // display-only here — shift managers cannot change them.
    public int ShiftTarget { get; private set; } = TargetKeys.Definitions[TargetKeys.Shift].Default;
    public int DayTarget { get; private set; } = TargetKeys.Definitions[TargetKeys.Day].Default;
    public int WeekTarget { get; private set; } = TargetKeys.Definitions[TargetKeys.Week].Default;

    public bool CustomRange { get; set; }
    public bool IsCurrentWeek { get; set; }
    public DateOnly WeekStart { get; set; }
    public DateOnly WeekEnd { get; set; }
    public int WeekNumber { get; set; }
    public int WeekYear { get; set; }
    public string WeekLabel { get; set; } = "";
    public DateOnly PrevWeekStart { get; set; }
    public DateOnly? NextWeekStart { get; set; }
    public List<(DateOnly Start, string Label)> RecentWeeks { get; set; } = [];

    public int WeekAchieved { get; set; }
    public int WeekAchievedDay { get; set; }
    public int WeekAchievedAfternoon { get; set; }
    public int WeekAchievedNight { get; set; }
    public List<ShiftTargetRow> ShiftTargets { get; set; } = [];
    public List<DayTargetRow> DayTargets { get; set; } = [];
    public int UnderperformingShiftCount { get; set; }

    public async Task OnGetAsync(string? from, string? to, string? shift, string? area, string? tl, string? week)
    {
        ShiftFilter = shift;
        AreaFilter = area;
        TlFilter = tl;

        // Pull current admin-set targets (read-only on this page).
        ShiftTarget = _targets.Shift;
        DayTarget = _targets.Day;
        WeekTarget = _targets.Week;

        // A custom from/to range overrides the weekly framing; otherwise scope to
        // the selected ISO week (default = current week).
        CustomRange = !string.IsNullOrEmpty(from) || !string.IsNullOrEmpty(to);
        var refDate = DateOnly.FromDateTime(DateTime.Today);
        if (!CustomRange && !string.IsNullOrEmpty(week) && DateOnly.TryParse(week, out var wk))
            refDate = wk;

        var (weekStart, weekEnd) = WeekMath.Bounds(refDate);
        WeekStart = weekStart;
        WeekEnd = weekEnd;
        WeekNumber = WeekMath.IsoWeekNumber(weekStart);
        WeekYear = WeekMath.IsoYear(weekStart);
        WeekLabel = WeekMath.Label(weekStart);

        var currentMonday = WeekMath.Bounds(DateOnly.FromDateTime(DateTime.Today)).Start;
        IsCurrentWeek = weekStart == currentMonday;
        PrevWeekStart = weekStart.AddDays(-7);
        NextWeekStart = weekStart < currentMonday ? weekStart.AddDays(7) : null;
        RecentWeeks = Enumerable.Range(0, 12)
            .Select(i => currentMonday.AddDays(-7 * i))
            .Select(s => (s, WeekMath.Label(s)))
            .ToList();

        if (CustomRange) { From = from; To = to; }
        else { From = weekStart.ToString("yyyy-MM-dd"); To = weekEnd.ToString("yyyy-MM-dd"); }

        try
        {
            await LoadDashboardDataAsync(From, To, shift, area, tl);
            if (!CustomRange)
                await LoadWeeklyTargetsAsync(weekStart, weekEnd);
        }
        catch (Exception ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<DashboardModel>>()
                .LogError(ex, "Dashboard load failed");
            Shifts = [];
        }
    }

    // Target vs actual for the week — always operation-wide (ignores the
    // shift/area/TL filters) so the numbers line up with the 35/105/315 targets.
    // "Achieved" = a shift form that is fully complete AND signed off.
    async Task LoadWeeklyTargetsAsync(DateOnly weekStart, DateOnly weekEnd)
    {
        var weekShifts = await _db.ShiftSubmissions
            .ExcludeAudits()
            .Include(s => s.Hours)
            .Where(s => s.ShiftDate >= weekStart && s.ShiftDate <= weekEnd)
            .ToListAsync();

        bool Achieved(ShiftSubmission s) =>
            !string.IsNullOrWhiteSpace(s.OutgoingTLSignature) && _completion.Evaluate(s).IsComplete;

        var achieved = weekShifts.Where(Achieved).ToList();
        WeekAchieved = achieved.Count;
        WeekAchievedDay = achieved.Count(s => s.Shift == "Day");
        WeekAchievedAfternoon = achieved.Count(s => s.Shift == "Afternoon");
        WeekAchievedNight = achieved.Count(s => s.Shift == "Night");

        // One row per shift that actually ran (had activity), so an underperforming
        // shift is one that ran but closed fewer than 35 forms.
        ShiftTargets = weekShifts
            .GroupBy(s => new { s.ShiftDate, s.Shift })
            .Select(g => new ShiftTargetRow(g.Key.ShiftDate, g.Key.Shift ?? "", g.Count(Achieved), ShiftTarget))
            .OrderBy(r => r.Date).ThenBy(r => ShiftOrder(r.Shift))
            .ToList();
        UnderperformingShiftCount = ShiftTargets.Count(r => r.Under);

        DayTargets = weekShifts
            .GroupBy(s => s.ShiftDate)
            .Select(g => new DayTargetRow(g.Key, g.Count(Achieved), DayTarget))
            .OrderBy(r => r.Date)
            .ToList();
    }

    static int ShiftOrder(string shift) => shift switch
    {
        "Day" => 0,
        "Afternoon" => 1,
        "Night" => 2,
        _ => 3,
    };

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

public record ShiftTargetRow(DateOnly Date, string Shift, int Achieved, int Target)
{
    public bool Under => Achieved < Target;
    public int Pct => Target > 0 ? Math.Min(100, Achieved * 100 / Target) : 0;
}

public record DayTargetRow(DateOnly Date, int Achieved, int Target)
{
    public bool Under => Achieved < Target;
    public int Pct => Target > 0 ? Math.Min(100, Achieved * 100 / Target) : 0;
}

public record WorstAreaDto(string Area, int Reds, int Ambers, int Greens, int TotalShifts)
{
    public int Total => Reds + Ambers + Greens;
    public int RedPct  => Total > 0 ? Reds   * 100 / Total : 0;
    public int AmberPct => Total > 0 ? Ambers * 100 / Total : 0;
    public int GreenPct => Total > 0 ? Greens * 100 / Total : 0;
}
