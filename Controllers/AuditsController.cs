using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Services;

namespace TL.Controllers
{
    [ApiController]
    [Route("api/audits")]
    public class AuditsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PdfExportService _pdf;

        public AuditsController(AppDbContext db, PdfExportService pdf)
        {
            _db = db; _pdf = pdf;
        }

        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> Pdf(int id)
        {
            var audit = await _db.AuditSubmissions.FirstOrDefaultAsync(a => a.Id == id);
            if (audit == null) return NotFound();
            var bytes = _pdf.GenerateAudit(audit);
            var filename = $"Audit_{audit.AuditDate:yyyyMMdd}_{audit.Area.Replace(" ", "_")}_{audit.AuditorName.Replace(" ", "_")}.pdf";
            return File(bytes, "application/pdf", filename);
        }
    }
}
