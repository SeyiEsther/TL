using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class SeniorStartModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserService _users;

    public SeniorStartModel(AppDbContext db, UserService users) { _db = db; _users = users; }

    public string AuditorName { get; set; } = "";
    public string AuditDate { get; set; } = "";
    public List<AreaPerformanceSuggestion> SuggestedAreas { get; set; } = [];
    public SeniorRota.RotaWeek? DutyWeek { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(string? auditor, string? date)
    {
        var user = _users.GetCurrentUser();
        AuditorName = string.IsNullOrWhiteSpace(auditor) ? user.DisplayName : auditor;
        AuditDate = string.IsNullOrWhiteSpace(date) ? DateTime.Today.ToString("yyyy-MM-dd") : date;
        var refDate = DateOnly.TryParse(AuditDate, out var ad) ? ad : DateOnly.FromDateTime(DateTime.Today);
        DutyWeek = SeniorRota.WeekForDate(refDate) ?? SeniorRota.CurrentWeek(refDate);
        SuggestedAreas = await LoadSuggestedAreasAsync();
    }

    public async Task<IActionResult> OnPostAsync(string auditDate, string auditorName, string area)
    {
        if (string.IsNullOrWhiteSpace(auditorName) || string.IsNullOrWhiteSpace(area))
        {
            AuditorName = auditorName ?? "";
            AuditDate = auditDate ?? DateTime.Today.ToString("yyyy-MM-dd");
            var refDate = DateOnly.TryParse(AuditDate, out var ad) ? ad : DateOnly.FromDateTime(DateTime.Today);
            DutyWeek = SeniorRota.WeekForDate(refDate) ?? SeniorRota.CurrentWeek(refDate);
            SuggestedAreas = await LoadSuggestedAreasAsync();
            Error = "Please fill in all fields.";
            return Page();
        }
        return RedirectToPage("/SeniorAudit", new { date = auditDate, auditor = auditorName, area });
    }

    async Task<List<AreaPerformanceSuggestion>> LoadSuggestedAreasAsync()
    {
        var today = DateTime.Today;
        var dow = (int)today.DayOfWeek;
        var weekStart = DateOnly.FromDateTime(today.AddDays(dow == 0 ? -6 : 1 - dow));
        var weekEnd = weekStart.AddDays(6);

        var weekData = await _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => s.ShiftDate >= weekStart && s.ShiftDate <= weekEnd)
            .ToListAsync();

        return weekData
            .Where(s => !string.IsNullOrEmpty(s.Area))
            .GroupBy(s => s.Area!)
            .Select(g =>
            {
                var reds = g.Sum(s => CountStatus(s, "Red"));
                var ambers = g.Sum(s => CountStatus(s, "Amber"));
                return new AreaPerformanceSuggestion(g.Key, reds, ambers);
            })
            .Where(a => a.Reds > 0 || a.Ambers > 0)
            .OrderByDescending(a => a.Reds)
            .ThenByDescending(a => a.Ambers)
            .Take(4)
            .ToList();
    }

    static int CountStatus(ShiftSubmission s, string status)
    {
        var latest = s.Hours.OrderByDescending(h => h.HourNumber).FirstOrDefault();
        if (latest == null) return 0;
        var n = 0;
        if (latest.OverallSafetyStatus == status) n++;
        if (latest.OverallQualityStatus == status) n++;
        if (latest.OverallPerfStatus == status) n++;
        return n;
    }
}
