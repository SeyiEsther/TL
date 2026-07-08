using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Pages;

public class SuccessModel : PageModel
{
    private readonly AppDbContext _db;
    public SuccessModel(AppDbContext db) => _db = db;

    public int SubmissionId { get; set; }
    public bool IsAudit { get; set; }
    public bool IsHodDailyAudit { get; set; }
    public string? HodAuditTypeLabel { get; set; }

    public async Task OnGetAsync(int? id, int? hodAuditId, bool? audit)
    {
        if (hodAuditId.HasValue)
        {
            var hod = await _db.HodDailyAudits.FindAsync(hodAuditId.Value);
            IsHodDailyAudit = true;
            IsAudit = true;
            SubmissionId = hodAuditId.Value;
            HodAuditTypeLabel = hod != null ? HodAuditTypes.LabelFor(hod.AuditType) : null;
            return;
        }

        SubmissionId = id ?? 0;
        IsAudit = audit == true;
    }
}
