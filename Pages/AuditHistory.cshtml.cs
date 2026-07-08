using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Pages;

public class AuditHistoryModel : PageModel
{
    private readonly AppDbContext _db;
    public AuditHistoryModel(AppDbContext db) => _db = db;

    public string? From { get; set; }
    public string? To { get; set; }
    public string? AreaFilter { get; set; }
    public string? AuditorFilter { get; set; }
    public List<HodAuditRow> Audits { get; set; } = [];

    public async Task OnGetAsync(string? from, string? to, string? area, string? auditor)
    {
        From = from;
        To = to;
        AreaFilter = area;
        AuditorFilter = auditor;

        var q = _db.ShiftSubmissions
            .Include(s => s.Hours)
            .Where(s => s.Shift == ShiftQueryExtensions.AuditPseudoShift);

        if (!string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f)) q = q.Where(s => s.ShiftDate >= f);
        if (!string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var t)) q = q.Where(s => s.ShiftDate <= t);
        if (!string.IsNullOrEmpty(area)) q = q.Where(s => s.Area == area);
        if (!string.IsNullOrEmpty(auditor)) q = q.Where(s => s.TeamLeaderDisplay.Contains(auditor));

        var rows = await q.OrderByDescending(s => s.ShiftDate).ThenByDescending(s => s.SubmittedAt).ToListAsync();

        Audits = rows.Select(s =>
        {
            var h = s.Hours.OrderByDescending(x => x.HourNumber).FirstOrDefault();
            return new HodAuditRow(
                s.Id,
                s.ShiftDate,
                s.Area ?? "—",
                s.TeamLeaderDisplay,
                h?.OverallSafetyStatus,
                h?.OverallQualityStatus,
                h?.OverallPerfStatus,
                !string.IsNullOrEmpty(s.Escalations),
                s.SubmittedAt);
        }).ToList();
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };
}

public record HodAuditRow(
    int Id,
    DateOnly AuditDate,
    string Area,
    string AuditorName,
    string? SafetyStatus,
    string? QualityStatus,
    string? PerfStatus,
    bool HasActions,
    DateTime SubmittedAt);
