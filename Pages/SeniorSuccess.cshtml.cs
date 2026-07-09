using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;

namespace TL.Pages;

public class SeniorSuccessModel : PageModel
{
    private readonly AppDbContext _db;
    public SeniorSuccessModel(AppDbContext db) => _db = db;

    public string Area { get; set; } = "";
    public string AuditDate { get; set; } = "";
    public string AuditorName { get; set; } = "";
    public int? AuditId { get; set; }

    public async Task OnGetAsync(int? id)
    {
        if (id.HasValue)
        {
            var sub = await _db.SeniorWeeklyAudits.FirstOrDefaultAsync(s => s.Id == id.Value);
            if (sub != null)
            {
                AuditId = sub.Id;
                Area = sub.Area;
                AuditDate = sub.AuditDate.ToString("dd/MM/yyyy");
                AuditorName = sub.AuditorName;
            }
        }
    }
}
