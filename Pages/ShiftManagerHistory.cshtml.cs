using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;

namespace TL.Pages;

public class ShiftManagerHistoryModel : PageModel
{
    private readonly AppDbContext _db;
    public ShiftManagerHistoryModel(AppDbContext db) => _db = db;

    public List<Row> Reports { get; set; } = [];
    public record Row(int Id, DateOnly Date, string Shift, string Manager, DateTime When);

    public async Task OnGetAsync()
    {
        Reports = await _db.ShiftManagerReports
            .OrderByDescending(r => r.ReportDate).ThenByDescending(r => r.SubmittedAt)
            .Select(r => new Row(r.Id, r.ReportDate, r.Shift, r.ManagerName, r.LastEditedAt ?? r.SubmittedAt))
            .ToListAsync();
    }
}
