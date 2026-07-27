using Microsoft.AspNetCore.Mvc.RazorPages;
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
    public bool NotFoundRecord { get; set; }
    public bool IsProgressSave { get; set; }
    public string? HodAuditTypeLabel { get; set; }

    public async Task OnGetAsync(int? id, int? hodAuditId, bool? audit, bool? saved)
    {
        IsProgressSave = saved == true;
        if (hodAuditId.HasValue)
        {
            var hod = await _db.HodDailyAudits.FindAsync(hodAuditId.Value);
            SubmissionId = hodAuditId.Value;
            IsHodDailyAudit = true;
            IsAudit = true;
            if (hod == null)
            {
                NotFoundRecord = true;
                return;
            }
            HodAuditTypeLabel = HodAuditTypes.LabelFor(hod.AuditType);
            return;
        }

        SubmissionId = id ?? 0;
        IsAudit = audit == true;
        if (SubmissionId == 0)
            NotFoundRecord = true;
    }
}
