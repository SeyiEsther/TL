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
    public string? SuggestedArea { get; set; }
    public string? SuggestedReason { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        var user = _users.GetCurrentUser();
        AuditorName = user.DisplayName;
        AuditDate = DateTime.Today.ToString("yyyy-MM-dd");

        // Find the worst-performing area this week (Mon–Sun) by red count
        var today = DateTime.Today;
        var dow = (int)today.DayOfWeek;
        var weekStart = DateOnly.FromDateTime(today.AddDays(dow == 0 ? -6 : 1 - dow));
        var weekEnd = weekStart.AddDays(6);

        var weekData = await _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => s.ShiftDate >= weekStart && s.ShiftDate <= weekEnd)
            .ToListAsync();

        var worst = weekData
            .Where(s => !string.IsNullOrEmpty(s.Area))
            .GroupBy(s => s.Area!)
            .Select(g =>
            {
                var reds = g.Sum(s =>
                    (s.Hours.OrderByDescending(h => h.HourNumber).Select(h => h.OverallSafetyStatus).FirstOrDefault() == "Red" ? 1 : 0) +
                    (s.Hours.OrderByDescending(h => h.HourNumber).Select(h => h.OverallQualityStatus).FirstOrDefault() == "Red" ? 1 : 0) +
                    (s.Hours.OrderByDescending(h => h.HourNumber).Select(h => h.OverallPerfStatus).FirstOrDefault() == "Red" ? 1 : 0));
                var ambers = g.Sum(s =>
                    (s.Hours.OrderByDescending(h => h.HourNumber).Select(h => h.OverallSafetyStatus).FirstOrDefault() == "Amber" ? 1 : 0) +
                    (s.Hours.OrderByDescending(h => h.HourNumber).Select(h => h.OverallQualityStatus).FirstOrDefault() == "Amber" ? 1 : 0) +
                    (s.Hours.OrderByDescending(h => h.HourNumber).Select(h => h.OverallPerfStatus).FirstOrDefault() == "Amber" ? 1 : 0));
                return new { Area = g.Key, Reds = reds, Ambers = ambers };
            })
            .OrderByDescending(a => a.Reds)
            .ThenByDescending(a => a.Ambers)
            .FirstOrDefault();

        if (worst != null)
        {
            SuggestedArea = worst.Area;
            SuggestedReason = worst.Reds > 0
                ? $"{worst.Reds} red status{(worst.Reds > 1 ? "es" : "")} this week"
                : $"{worst.Ambers} amber status{(worst.Ambers > 1 ? "es" : "")} this week";
        }
    }

    public IActionResult OnPost(string auditDate, string auditorName, string area)
    {
        if (string.IsNullOrWhiteSpace(auditorName) || string.IsNullOrWhiteSpace(area))
        {
            AuditorName = auditorName ?? "";
            AuditDate = auditDate ?? DateTime.Today.ToString("yyyy-MM-dd");
            Error = "Please fill in all fields.";
            return Page();
        }
        return RedirectToPage("/SeniorAudit", new { date = auditDate, auditor = auditorName, area });
    }
}
