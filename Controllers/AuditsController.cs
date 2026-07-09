using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Helpers;
using TL.Models;
using TL.Services;

namespace TL.Controllers;

[ApiController]
[Route("api/audits")]
public class AuditsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PdfExportService _pdf;

    public AuditsController(AppDbContext db, PdfExportService pdf)
    {
        _db = db;
        _pdf = pdf;
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id)
    {
        var hod = await _db.HodDailyAudits.FirstOrDefaultAsync(a => a.Id == id);
        if (hod != null)
        {
            try
            {
                var answers = HodAuditSerializer.ParseAnswers(hod.AnswersJson);
                var effectiveness = HodAuditSerializer.ParseEffectiveness(hod.EffectivenessJson);
                var bytes = _pdf.GenerateHodDaily(hod, answers, effectiveness);
                var type = HodAuditTypes.LabelFor(hod.AuditType).Replace(" ", "_");
                var filename = $"HoD_{type}_{hod.AuditDate:yyyyMMdd}_{hod.ResolveEffectivenessArea().Replace(" ", "_")}.pdf";
                return PdfResponse.File(this, bytes, filename);
            }
            catch (Exception)
            {
                return PdfError();
            }
        }

        var senior = await _db.SeniorWeeklyAudits.FirstOrDefaultAsync(a => a.Id == id);
        if (senior != null)
        {
            try
            {
                var bytes = _pdf.GenerateSeniorWeekly(senior);
                var filename = $"Senior_{senior.AuditDate:yyyyMMdd}_{senior.Area.Replace(" ", "_")}.pdf";
                return PdfResponse.File(this, bytes, filename);
            }
            catch (Exception)
            {
                return PdfError();
            }
        }

        var audit = await _db.AuditSubmissions.FirstOrDefaultAsync(a => a.Id == id);
        if (audit == null) return NotFound();

        try
        {
            var bytes = _pdf.GenerateAudit(audit);
            var filename = $"Audit_{audit.AuditDate:yyyyMMdd}_{audit.Area.Replace(" ", "_")}_{audit.AuditorName.Replace(" ", "_")}.pdf";
            return PdfResponse.File(this, bytes, filename);
        }
        catch (Exception)
        {
            return PdfError();
        }
    }

    static ContentResult PdfError() => new()
    {
        StatusCode = 500,
        ContentType = "text/plain",
        Content = "PDF generation failed. Please try again or contact support.",
    };
}
