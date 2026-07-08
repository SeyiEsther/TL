using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class SeniorDashboardModel : PageModel
{
    private readonly AppDbContext _db;
    public SeniorDashboardModel(AppDbContext db) => _db = db;

    public string? From { get; set; }
    public string? To { get; set; }
    public string? AreaFilter { get; set; }

    public List<SeniorWeeklyAudit> Audits { get; set; } = [];
    public int TotalAudits { get; set; }
    public int AvgScore { get; set; }
    public int GovScore { get; set; }
    public int SafetyScore { get; set; }
    public int QualityScore { get; set; }
    public int PeopleScore { get; set; }
    public int StandardsScore { get; set; }
    public int PerfScore { get; set; }
    public int VerdictGreen { get; set; }
    public int VerdictAmber { get; set; }
    public int VerdictRed { get; set; }
    public SeniorWeeklyAudit? Latest { get; set; }

    public async Task OnGetAsync(string? from, string? to, string? area)
    {
        From = from;
        To = to;
        AreaFilter = area;

        try
        {
            var q = _db.SeniorWeeklyAudits.AsQueryable();
            if (!string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f)) q = q.Where(a => a.AuditDate >= f);
            if (!string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var t)) q = q.Where(a => a.AuditDate <= t);
            if (!string.IsNullOrEmpty(area)) q = q.Where(a => a.Area == area);

            Audits = await q.OrderByDescending(a => a.AuditDate).ThenByDescending(a => a.SubmittedAt).ToListAsync();
        }
        catch
        {
            // If the database is unreachable, degrade gracefully by showing an
            // empty dashboard instead of a 500 error.
            Audits = new List<SeniorWeeklyAudit>();
        }

        TotalAudits = Audits.Count;
        Latest = Audits.FirstOrDefault();

        if (TotalAudits == 0) return;

        AvgScore = (int)Audits.Average(SeniorAuditScoring.OverallScore);
        GovScore = (int)Audits.Average(SeniorAuditScoring.GovernanceScore);
        SafetyScore = (int)Audits.Average(SeniorAuditScoring.SafetyScore);
        QualityScore = (int)Audits.Average(SeniorAuditScoring.QualityScore);
        PeopleScore = (int)Audits.Average(SeniorAuditScoring.PeopleScore);
        StandardsScore = (int)Audits.Average(SeniorAuditScoring.StandardsScore);
        PerfScore = (int)Audits.Average(SeniorAuditScoring.PerformanceScore);

        VerdictGreen = Audits.Count(a => a.OverallVerdict == "Green");
        VerdictAmber = Audits.Count(a => a.OverallVerdict == "Amber");
        VerdictRed = Audits.Count(a => a.OverallVerdict == "Red");
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };
    public static string GaugeColor(int pct) => SeniorAuditScoring.GaugeColor(pct);
}
