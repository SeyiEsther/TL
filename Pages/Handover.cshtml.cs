using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Pages;

public class HandoverModel : PageModel
{
    private readonly AppDbContext _db;
    public HandoverModel(AppDbContext db) => _db = db;

    public string? SelectedArea { get; set; }
    public ShiftSubmission? Latest { get; set; }
    public List<ShiftSummaryDto> Previous { get; set; } = new();

    public async Task OnGetAsync(string? area, int? id)
    {
        SelectedArea = area;
        if (string.IsNullOrEmpty(area)) return;

        var areaShifts = await _db.ShiftSubmissions
            .Where(s => s.Area == area)
            .OrderByDescending(s => s.ShiftDate)
            .ThenByDescending(s => s.SubmittedAt)
            .Select(s => new ShiftSummaryDto
            {
                Id = s.Id,
                ShiftDate = s.ShiftDate,
                Shift = s.Shift,
                SubmittedAt = s.SubmittedAt,
            })
            .ToListAsync();

        if (!areaShifts.Any()) return;

        var targetId = id ?? areaShifts[0].Id;

        Latest = await _db.ShiftSubmissions
            .Include(s => s.Hours.OrderBy(h => h.HourNumber))
            .Include(s => s.AuditLogs.OrderByDescending(a => a.ChangedAt))
            .FirstOrDefaultAsync(s => s.Id == targetId);

        if (Latest != null)
        {
            var lastHour = Latest.Hours.LastOrDefault();
            Latest.Hours = Latest.Hours.ToList();

            Previous = areaShifts
                .Where(s => s.Id != targetId)
                .Take(5)
                .ToList();
        }
    }
}
