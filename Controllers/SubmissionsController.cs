using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Helpers;
using TL.Models;
using TL.Services;

namespace TL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PdfExportService _pdf;
        private readonly ILogger<SubmissionsController> _log;

        public SubmissionsController(AppDbContext db, PdfExportService pdf, ILogger<SubmissionsController> log)
        {
            _db = db; _pdf = pdf; _log = log;
        }

        [HttpGet("today")]
        public async Task<IActionResult> Today()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var items = await _db.ShiftSubmissions
                .ExcludeAudits()
                .Include(s => s.Hours)
                .Where(s => s.ShiftDate == today)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            var result = items.Select(s => new
            {
                id = s.Id,
                shiftDate = s.ShiftDate.ToString("dd/MM/yyyy"),
                shift = s.Shift,
                area = s.Area,
                teamLeaderDisplay = s.TeamLeaderDisplay,
                submittedBy = s.SubmittedBy,
                hoursCompleted = s.HoursCompleted,
                overallSafetyStatus = s.Hours.Where(h => h.OverallSafetyStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallSafetyStatus).FirstOrDefault(),
                overallQualityStatus = s.Hours.Where(h => h.OverallQualityStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallQualityStatus).FirstOrDefault(),
                overallPerfStatus = s.Hours.Where(h => h.OverallPerfStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallPerfStatus).FirstOrDefault(),
                submittedAt = s.SubmittedAt,
                lastEditedBy = s.LastEditedBy,
                lastEditedAt = s.LastEditedAt,
                escalations = s.Escalations,
            });

            return Ok(result);
        }

        [HttpGet("{id:int}/audit")]
        public async Task<IActionResult> Audit(int id)
        {
            var logs = await _db.AuditLogs
                .Where(a => a.SubmissionId == id)
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync();
            return Ok(logs);
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? from,
            [FromQuery] string? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _db.ShiftSubmissions.ExcludeAudits();
            if (!string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f)) query = query.Where(s => s.ShiftDate >= f);
            if (!string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var t)) query = query.Where(s => s.ShiftDate <= t);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.ShiftDate)
                .ThenBy(s => s.Shift)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new ShiftSummaryDto
                {
                    Id = s.Id,
                    ShiftDate = s.ShiftDate,
                    Shift = s.Shift,
                    Area = s.Area,
                    TeamLeaderDisplay = s.TeamLeaderDisplay,
                    SubmittedBy = s.SubmittedBy,
                    HoursCompleted = s.HoursCompleted,
                    OverallSafetyStatus = s.Hours.Where(h => h.OverallSafetyStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallSafetyStatus).FirstOrDefault(),
                    OverallQualityStatus = s.Hours.Where(h => h.OverallQualityStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallQualityStatus).FirstOrDefault(),
                    OverallPerfStatus = s.Hours.Where(h => h.OverallPerfStatus != null).OrderByDescending(h => h.HourNumber).Select(h => h.OverallPerfStatus).FirstOrDefault(),
                    SubmittedAt = s.SubmittedAt,
                    LastEditedBy = s.LastEditedBy,
                    LastEditedAt = s.LastEditedAt,
                    Escalations = s.Escalations,
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var shift = await _db.ShiftSubmissions
                .Include(s => s.Hours.OrderBy(h => h.HourNumber))
                .Include(s => s.AuditLogs.OrderByDescending(a => a.ChangedAt))
                .FirstOrDefaultAsync(s => s.Id == id);
            if (shift == null) return NotFound();
            return Ok(shift);
        }

        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> Pdf(int id)
        {
            var shift = await _db.ShiftSubmissions
                .Include(s => s.Hours.OrderBy(h => h.HourNumber))
                .FirstOrDefaultAsync(s => s.Id == id);
            if (shift == null) return NotFound();

            try
            {
                var bytes = _pdf.GenerateShift(shift);
                var filename = $"TLSW_{shift.ShiftDate:yyyyMMdd}_{shift.Shift}_{shift.TeamLeaderDisplay.Replace(" ", "_")}.pdf";
                return PdfResponse.File(this, bytes, filename);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Shift PDF failed for id {Id}", id);
                return new ContentResult
                {
                    StatusCode = 500,
                    ContentType = "text/plain",
                    Content = "PDF generation failed. Please try again or contact support.",
                };
            }
        }

        [HttpGet("export/csv")]
        public async Task<IActionResult> Csv([FromQuery] string? from, [FromQuery] string? to)
        {
            var query = _db.ShiftSubmissions.ExcludeAudits();
            if (!string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f)) query = query.Where(s => s.ShiftDate >= f);
            if (!string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var t)) query = query.Where(s => s.ShiftDate <= t);

            var rows = await query.Include(s => s.Hours).OrderByDescending(s => s.ShiftDate).ToListAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ShiftId,Date,Shift,Area,TeamLeader,Hour,Hazards,UnsafeBehaviours,TargetHit,Maintenance,Escalations,SafetyStatus,QualityStatus,PerfStatus");

            foreach (var s in rows)
                foreach (var h in s.Hours.OrderBy(x => x.HourNumber))
                    sb.AppendLine(string.Join(",",
                        s.Id, s.ShiftDate, Q(s.Shift), Q(s.Area), Q(s.TeamLeaderDisplay), h.HourNumber,
                        h.HazardsObserved, h.UnsafeBehaviours, h.HourlyTargetAchieved, h.MaintenanceIssues,
                        h.EscalationsNeeded, Q(h.OverallSafetyStatus), Q(h.OverallQualityStatus), Q(h.OverallPerfStatus)));

            return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"TLSW_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        static string Q(string? s) => $"\"{(s ?? "").Replace("\"", "\"\"")}\"";
    }
}
