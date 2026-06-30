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

    // This-week panels — always Mon–Sun of the current week, unaffected by main filters
    public DateOnly WeekStart { get; set; }
    public DateOnly WeekEnd { get; set; }
    public List<WorstAreaDto> WorstAreasThisWeek { get; set; } = [];
    public List<CompletionRowDto> WeekCompletion { get; set; } = [];
    public int WeekDayTotal { get; set; }
    public int WeekAfternoonTotal { get; set; }
    public int WeekNightTotal { get; set; }

    public async Task OnGetAsync(string? from, string? to, string? shift, string? area, string? tl)
    {
        From = from; To = to; ShiftFilter = shift; AreaFilter = area; TlFilter = tl;

        // ── This week (unfiltered) ──────────────────────────────────────────────
        var today = DateTime.Today;
        var dow = (int)today.DayOfWeek; // 0 = Sunday
        WeekStart = DateOnly.FromDateTime(today.AddDays(dow == 0 ? -6 : 1 - dow));
        WeekEnd = WeekStart.AddDays(6);

        var weekEntities = await _db.ShiftSubmissions
            .Include(s => s.Hours)
            .Where(s => s.ShiftDate >= WeekStart && s.ShiftDate <= WeekEnd)
            .ToListAsync();

        WorstAreasThisWeek = weekEntities
            .Where(s => !string.IsNullOrEmpty(s.Area))
            .GroupBy(s => s.Area!)
            .Select(g => MakeWorstArea(g.Key, g.ToList()))
            .OrderByDescending(a => a.Reds).ThenByDescending(a => a.Ambers)
            .Take(8).ToList();

        WeekCompletion = weekEntities
            .GroupBy(s => s.TeamLeaderDisplay)
            .Select(g => new CompletionRowDto(
                g.Key,
                g.Count(s => s.Shift == "Day"),
                g.Count(s => s.Shift == "Afternoon"),
                g.Count(s => s.Shift == "Night")
            ))
            .OrderByDescending(c => c.Total).ToList();

        WeekDayTotal      = weekEntities.Count(s => s.Shift == "Day");
        WeekAfternoonTotal = weekEntities.Count(s => s.Shift == "Afternoon");
        WeekNightTotal    = weekEntities.Count(s => s.Shift == "Night");

        // ── Main filtered data ──────────────────────────────────────────────────
        var q = _db.ShiftSubmissions.Include(s => s.Hours).AsQueryable();
        if (!string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f)) q = q.Where(s => s.ShiftDate >= f);
        if (!string.IsNullOrEmpty(to)   && DateOnly.TryParse(to,   out var t)) q = q.Where(s => s.ShiftDate <= t);
        if (!string.IsNullOrEmpty(shift)) q = q.Where(s => s.Shift == shift);
        if (!string.IsNullOrEmpty(area))  q = q.Where(s => s.Area == area);
        if (!string.IsNullOrEmpty(tl))    q = q.Where(s => s.TeamLeaderDisplay.Contains(tl));

        var rawEntities = await q
            .OrderByDescending(s => s.ShiftDate)
            .ThenBy(s => s.Shift)
            .ToListAsync();

        var raw = rawEntities.Select(MapShift).ToList();
        Shifts = raw;

        var todayDate = DateOnly.FromDateTime(DateTime.Today);
        var weekAgo = todayDate.AddDays(-7);
        Today = raw.Count(s => s.ShiftDate == todayDate);
        ThisWeek = raw.Count(s => s.ShiftDate >= weekAgo);
        WithEscalations = raw.Count(s => !string.IsNullOrEmpty(s.Escalations));

        SafetyGreen = raw.Count(s => s.OverallSafetyStatus == "Green");
        SafetyAmber = raw.Count(s => s.OverallSafetyStatus == "Amber");
        SafetyRed   = raw.Count(s => s.OverallSafetyStatus == "Red");
        QualityGreen = raw.Count(s => s.OverallQualityStatus == "Green");
        QualityAmber = raw.Count(s => s.OverallQualityStatus == "Amber");
        QualityRed   = raw.Count(s => s.OverallQualityStatus == "Red");
        PerfGreen = raw.Count(s => s.OverallPerfStatus == "Green");
        PerfAmber = raw.Count(s => s.OverallPerfStatus == "Amber");
        PerfRed   = raw.Count(s => s.OverallPerfStatus == "Red");

        DayShifts       = raw.Count(s => s.Shift == "Day");
        AfternoonShifts = raw.Count(s => s.Shift == "Afternoon");
        NightShifts     = raw.Count(s => s.Shift == "Night");

        var totalStatuses = SafetyGreen + SafetyAmber + SafetyRed + QualityGreen + QualityAmber + QualityRed + PerfGreen + PerfAmber + PerfRed;
        var weighted = (SafetyGreen + QualityGreen + PerfGreen) * 100 + (SafetyAmber + QualityAmber + PerfAmber) * 50;
        HealthScore = totalStatuses > 0 ? weighted / totalStatuses : 0;

        var last14 = Enumerable.Range(0, 14)
            .Select(i => DateOnly.FromDateTime(DateTime.Today.AddDays(-13 + i))).ToList();
        ActivityLabels = last14.Select(d => d.ToString("dd/MM")).ToArray();
        ActivityData   = last14.Select(d => raw.Count(s => s.ShiftDate == d)).ToArray();

        var csvParams = new List<string>();
        if (!string.IsNullOrEmpty(from)) csvParams.Add("from=" + from);
        if (!string.IsNullOrEmpty(to))   csvParams.Add("to=" + to);
        CsvQuery = csvParams.Any() ? "?" + string.Join("&", csvParams) : "";
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    static string? LastStatus(ShiftSubmission s, Func<HourlyCheck, string?> sel) =>
        s.Hours.Where(h => sel(h) != null).OrderByDescending(h => h.HourNumber).Select(sel).FirstOrDefault();

    static WorstAreaDto MakeWorstArea(string area, List<ShiftSubmission> shifts)
    {
        int reds = 0, ambers = 0, greens = 0;
        foreach (var s in shifts)
        {
            foreach (var st in new[] {
                LastStatus(s, h => h.OverallSafetyStatus),
                LastStatus(s, h => h.OverallQualityStatus),
                LastStatus(s, h => h.OverallPerfStatus) })
            {
                if (st == "Red") reds++;
                else if (st == "Amber") ambers++;
                else if (st == "Green") greens++;
            }
        }
        return new WorstAreaDto(area, reds, ambers, greens, shifts.Count);
    }

    static ShiftSummaryDto MapShift(ShiftSubmission s)
    {
        var sf = new List<string>();
        var qf = new List<string>();
        var pf = new List<string>();

        foreach (var h in s.Hours)
        {
            if (h.HazardsObserved == true   && !sf.Contains("Hazards"))              sf.Add("Hazards");
            if (h.UnsafeBehaviours == true  && !sf.Contains("Unsafe behaviour"))     sf.Add("Unsafe behaviour");
            if (h.AccidentsReported == true && !sf.Contains("Accident/near-miss"))   sf.Add("Accident/near-miss");
            if (h.QualityChecksCompleted == false && !qf.Contains("Checks missed"))  qf.Add("Checks missed");
            if (h.DeviationsEscalated == false    && !qf.Contains("Dev. not escalated")) qf.Add("Dev. not escalated");
            if (h.NonComplianceAddressed == false && !qf.Contains("Non-compliance")) qf.Add("Non-compliance");
            if (h.HourlyTargetAchieved == false   && !pf.Contains("Target missed"))  pf.Add("Target missed");
            if (h.MaintenanceIssues == true       && !pf.Contains("Maintenance"))    pf.Add("Maintenance");
            if (h.MaterialsAvailable == false     && !pf.Contains("Materials"))      pf.Add("Materials");
            if (h.ToolsAvailable == false         && !pf.Contains("Tools"))          pf.Add("Tools");
            if (h.EscalationsNeeded == true       && !pf.Contains("Escalation"))     pf.Add("Escalation");
        }

        return new ShiftSummaryDto
        {
            Id = s.Id,
            ShiftDate = s.ShiftDate,
            Shift = s.Shift,
            Area = s.Area,
            TeamLeaderDisplay = s.TeamLeaderDisplay,
            HoursCompleted = s.HoursCompleted,
            OverallSafetyStatus  = LastStatus(s, h => h.OverallSafetyStatus),
            OverallQualityStatus = LastStatus(s, h => h.OverallQualityStatus),
            OverallPerfStatus    = LastStatus(s, h => h.OverallPerfStatus),
            SubmittedAt  = s.SubmittedAt,
            Escalations  = s.Escalations,
            SafetyFlags  = sf,
            QualityFlags = qf,
            PerfFlags    = pf,
        };
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };
    public string J(object o) => System.Text.Json.JsonSerializer.Serialize(o);
}

public record WorstAreaDto(string Area, int Reds, int Ambers, int Greens, int TotalShifts)
{
    public int Total    => Reds + Ambers + Greens;
    public int RedPct   => Total > 0 ? Reds   * 100 / Total : 0;
    public int AmberPct => Total > 0 ? Ambers * 100 / Total : 0;
    public int GreenPct => Total > 0 ? Greens * 100 / Total : 0;
}

public record CompletionRowDto(string TL, int Day, int Afternoon, int Night)
{
    public int Total => Day + Afternoon + Night;
}
