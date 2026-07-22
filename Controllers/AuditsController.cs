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
    private readonly PortalAccessService _access;
    private readonly ILogger<AuditsController> _log;

    public AuditsController(
        AppDbContext db, PdfExportService pdf, PortalAccessService access, ILogger<AuditsController> log)
    {
        _db = db;
        _pdf = pdf;
        _access = access;
        _log = log;
    }

    [HttpGet("hod/{id:int}/pdf")]
    public Task<IActionResult> HodPdf(int id) => GenerateHodPdfAsync(id);

    [HttpGet("senior/{id:int}/pdf")]
    public Task<IActionResult> SeniorPdf(int id) => GenerateSeniorPdfAsync(id);

    [HttpGet("walkaround/{id:int}/pdf")]
    public Task<IActionResult> WalkaroundPdf(int id) => GenerateWalkaroundPdfAsync(id);

    /// <summary>Legacy HoD rows stored as ShiftSubmission with Shift == "Audit".</summary>
    [HttpGet("legacy/{id:int}/pdf")]
    public Task<IActionResult> LegacyPdf(int id) => GenerateLegacyHodPdfAsync(id);

    /// <summary>
    /// Back-compat. Prefer typed routes. Without ?type=, only succeeds when exactly one
    /// table owns the id, and the caller must have that table's portal role.
    /// </summary>
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id, [FromQuery] string? type = null)
    {
        var kind = (type ?? "").Trim().ToLowerInvariant();
        if (kind is "hod" or "hoddaily")
            return _access.CanAccessHod() ? await GenerateHodPdfAsync(id) : Forbid();
        if (kind is "senior" or "weekly")
            return _access.CanAccessSenior() ? await GenerateSeniorPdfAsync(id) : Forbid();
        if (kind is "walkaround")
            return _access.CanAccessHod() ? await GenerateWalkaroundPdfAsync(id) : Forbid();
        if (kind is "legacy" or "old")
            return _access.CanAccessHod() ? await GenerateLegacyHodPdfAsync(id) : Forbid();

        var hod = await _db.HodDailyAudits.AsNoTracking().AnyAsync(a => a.Id == id);
        var senior = await _db.SeniorWeeklyAudits.AsNoTracking().AnyAsync(a => a.Id == id);
        var walkaround = await _db.AuditSubmissions.AsNoTracking().AnyAsync(a => a.Id == id);
        var legacy = await _db.ShiftSubmissions.AsNoTracking()
            .AnyAsync(s => s.Id == id && s.Shift == ShiftQueryExtensions.AuditPseudoShift);

        var matches = (hod ? 1 : 0) + (senior ? 1 : 0) + (walkaround ? 1 : 0) + (legacy ? 1 : 0);
        if (matches > 1)
        {
            return BadRequest(new
            {
                error = "Ambiguous audit id — use /api/audits/hod/{id}/pdf, /api/audits/senior/{id}/pdf, /api/audits/legacy/{id}/pdf, or ?type=hod|senior|walkaround|legacy."
            });
        }

        if (hod)
            return _access.CanAccessHod() ? await GenerateHodPdfAsync(id) : Forbid();
        if (senior)
            return _access.CanAccessSenior() ? await GenerateSeniorPdfAsync(id) : Forbid();
        if (walkaround)
            return _access.CanAccessHod() ? await GenerateWalkaroundPdfAsync(id) : Forbid();
        if (legacy)
            return _access.CanAccessHod() ? await GenerateLegacyHodPdfAsync(id) : Forbid();
        return NotFound();
    }

    async Task<IActionResult> GenerateHodPdfAsync(int id)
    {
        var hod = await _db.HodDailyAudits.FirstOrDefaultAsync(a => a.Id == id);
        if (hod == null) return NotFound();

        try
        {
            var answers = HodAuditSerializer.ParseAnswers(hod.AnswersJson);
            var effectiveness = HodAuditSerializer.ParseEffectiveness(hod.EffectivenessJson);
            var bytes = _pdf.GenerateHodDaily(hod, answers, effectiveness);
            var type = HodAuditTypes.LabelFor(hod.AuditType).Replace(" ", "_");
            var filename = $"HoD_{type}_{hod.AuditDate:yyyyMMdd}_{hod.ResolveEffectivenessArea().Replace(" ", "_")}.pdf";
            return PdfResponse.File(this, bytes, filename);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "HoD PDF failed for id {Id}", id);
            return PdfError();
        }
    }

    async Task<IActionResult> GenerateSeniorPdfAsync(int id)
    {
        var senior = await _db.SeniorWeeklyAudits.FirstOrDefaultAsync(a => a.Id == id);
        if (senior == null) return NotFound();

        try
        {
            var bytes = _pdf.GenerateSeniorWeekly(senior);
            var filename = $"Senior_{senior.AuditDate:yyyyMMdd}_{senior.Area.Replace(" ", "_")}.pdf";
            return PdfResponse.File(this, bytes, filename);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Senior PDF failed for id {Id}", id);
            return PdfError();
        }
    }

    async Task<IActionResult> GenerateWalkaroundPdfAsync(int id)
    {
        var audit = await _db.AuditSubmissions.FirstOrDefaultAsync(a => a.Id == id);
        if (audit == null) return NotFound();

        try
        {
            var bytes = _pdf.GenerateAudit(audit);
            var filename = $"Audit_{audit.AuditDate:yyyyMMdd}_{audit.Area.Replace(" ", "_")}_{audit.AuditorName.Replace(" ", "_")}.pdf";
            return PdfResponse.File(this, bytes, filename);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Walkaround PDF failed for id {Id}", id);
            return PdfError();
        }
    }

    async Task<IActionResult> GenerateLegacyHodPdfAsync(int id)
    {
        var shift = await _db.ShiftSubmissions
            .Include(s => s.Hours.OrderBy(h => h.HourNumber))
            .FirstOrDefaultAsync(s => s.Id == id && s.Shift == ShiftQueryExtensions.AuditPseudoShift);
        if (shift == null) return NotFound();

        try
        {
            var bytes = _pdf.GenerateShift(shift);
            var area = (shift.Area ?? "Audit").Replace(" ", "_");
            var filename = $"HoD_Legacy_{shift.ShiftDate:yyyyMMdd}_{area}.pdf";
            return PdfResponse.File(this, bytes, filename);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Legacy HoD PDF failed for id {Id}", id);
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
