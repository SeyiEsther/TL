using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class AuditResultModel : PageModel
{
    private readonly AppDbContext _db;
    public AuditResultModel(AppDbContext db) => _db = db;

    public bool IsHodDaily { get; set; }
    public ShiftSubmission? Audit { get; set; }
    public HourlyCheck? Check { get; set; }
    public HodDailyAudit? HodAudit { get; set; }
    public List<HodAuditAnswer> Answers { get; set; } = [];
    public List<HodEffectivenessFinding> Effectiveness { get; set; } = [];
    public string RatingBand { get; set; } = "";
    public string RatingDetail { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        HodAudit = await _db.HodDailyAudits.FirstOrDefaultAsync(a => a.Id == id);
        if (HodAudit != null)
        {
            IsHodDaily = true;
            Answers = HodAuditSerializer.ParseAnswers(HodAudit.AnswersJson);
            Effectiveness = HodAuditSerializer.ParseEffectiveness(HodAudit.EffectivenessJson);
            RatingBand = HodAuditScoring.RatingBand(HodAudit.TotalScore, HodAudit.MaxScore);
            RatingDetail = HodAuditScoring.RatingDetail(HodAudit.TotalScore, HodAudit.MaxScore);
            return Page();
        }

        Audit = await _db.ShiftSubmissions
            .Include(s => s.Hours.OrderBy(h => h.HourNumber))
            .FirstOrDefaultAsync(s => s.Id == id && s.Shift == ShiftQueryExtensions.AuditPseudoShift);

        if (Audit == null) return RedirectToPage("/History", new { tab = "hod" });

        Check = Audit.Hours.FirstOrDefault();
        return Page();
    }

    public static string Rc(string? v) => v switch { "Green" => "g", "Amber" => "a", "Red" => "r", _ => "u" };
    public static string Bn(bool? v) => v == true ? "Yes" : v == false ? "No" : "—";
    public static string PassFail(bool? v) => v == true ? "Pass (1)" : v == false ? "Fail (0)" : "—";
}
