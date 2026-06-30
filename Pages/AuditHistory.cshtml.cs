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
    public List<AuditSubmission> Audits { get; set; } = [];

    public async Task OnGetAsync(string? from, string? to, string? area, string? auditor)
    {
        From = from; To = to; AreaFilter = area; AuditorFilter = auditor;

        var q = _db.AuditSubmissions.AsQueryable();
        if (!string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f)) q = q.Where(a => a.AuditDate >= f);
        if (!string.IsNullOrEmpty(to)   && DateOnly.TryParse(to,   out var t)) q = q.Where(a => a.AuditDate <= t);
        if (!string.IsNullOrEmpty(area))    q = q.Where(a => a.Area == area);
        if (!string.IsNullOrEmpty(auditor)) q = q.Where(a => a.AuditorName.Contains(auditor));

        Audits = await q.OrderByDescending(a => a.AuditDate).ThenByDescending(a => a.SubmittedAt).ToListAsync();
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };
}
