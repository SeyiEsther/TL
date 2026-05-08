using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TL.Data;
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
        private readonly UserService _users;

        public SubmissionsController(AppDbContext db, PdfExportService pdf, UserService users)
        {
            _db = db; _pdf = pdf; _users = users;
        }

        // POST - create new submission
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShiftSubmissionRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = _users.GetCurrentUser();

            var shift = new ShiftSubmission
            {
                SubmittedBy = user.Username,
                TeamLeaderDisplay = req.TeamLeader ?? user.DisplayName,
                ShiftDate = DateOnly.Parse(req.ShiftDate),
                Shift = req.Shift,
                Area = req.Area,
                HoursCompleted = (byte)req.Hours.Count,
                Escalations = req.Escalations,
                KeyRisks = req.KeyRisks,
                Priorities = req.Priorities,
                OutgoingTLSignature = req.OutgoingSignature,
                IncomingTLSignature = req.IncomingSignature,
                Hours = req.Hours.Select(h => MapHour(h)).ToList()
            };

            _db.ShiftSubmissions.Add(shift);
            await _db.SaveChangesAsync();
            return Ok(new { id = shift.Id });
        }

        // GET find by date/shift/area
        [HttpGet("find")]
        public async Task<IActionResult> Find([FromQuery] string date, [FromQuery] string shift, [FromQuery] string area)
        {
            if (!DateOnly.TryParse(date, out var d)) return BadRequest("Invalid date");
            var existing = await _db.ShiftSubmissions
                .Include(s => s.Hours.OrderBy(h => h.HourNumber))
                .Include(s => s.AuditLogs.OrderByDescending(a => a.ChangedAt))
                .FirstOrDefaultAsync(s => s.ShiftDate == d && s.Shift == shift && s.Area == area);
            if (existing == null) return NotFound();
            return Ok(existing);
        }

        // PUT - update existing submission with audit log
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ShiftSubmissionRequest req)
        {
            var user = _users.GetCurrentUser();
            var editorName = req.TeamLeader ?? user.DisplayName;

            var shift = await _db.ShiftSubmissions
                .Include(s => s.Hours)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (shift == null) return NotFound();

            var logs = new List<AuditLog>();

            void Track(string field, string? oldVal, string? newVal)
            {
                if (oldVal != newVal)
                    logs.Add(new AuditLog
                    {
                        SubmissionId = id,
                        ChangedBy = editorName,
                        FieldName = field,
                        OldValue = oldVal,
                        NewValue = newVal
                    });
            }

            Track("Escalations", shift.Escalations, req.Escalations);
            Track("KeyRisks", shift.KeyRisks, req.KeyRisks);
            Track("Priorities", shift.Priorities, req.Priorities);

            shift.Escalations = req.Escalations;
            shift.KeyRisks = req.KeyRisks;
            shift.Priorities = req.Priorities;
            shift.HoursCompleted = (byte)req.Hours.Count;
            shift.LastEditedBy = editorName;
            shift.LastEditedAt = DateTime.UtcNow;

            foreach (var hReq in req.Hours)
            {
                var existing = shift.Hours.FirstOrDefault(h => h.HourNumber == hReq.Hour);
                if (existing == null)
                {
                    shift.Hours.Add(MapHour(hReq));
                    logs.Add(new AuditLog { SubmissionId = id, ChangedBy = editorName, FieldName = $"Hour{hReq.Hour}", OldValue = null, NewValue = "Created" });
                }
                else
                {
                    void TrackHour(string f, string? o, string? n)
                    {
                        if (o != n) logs.Add(new AuditLog { SubmissionId = id, ChangedBy = editorName, FieldName = $"Hr{hReq.Hour}.{f}", OldValue = o, NewValue = n });
                    }
                    TrackHour("Hazards", existing.HazardsObserved?.ToString(), hReq.Haz?.ToString());
                    TrackHour("Unsafe", existing.UnsafeBehaviours?.ToString(), hReq.Uns?.ToString());
                    TrackHour("Target", existing.HourlyTargetAchieved?.ToString(), hReq.Tgt?.ToString());
                    TrackHour("Maintenance", existing.MaintenanceIssues?.ToString(), hReq.Maint?.ToString());
                    TrackHour("Escalations", existing.EscalationsNeeded?.ToString(), hReq.Escl?.ToString());
                    TrackHour("SafetyStatus", existing.OverallSafetyStatus, hReq.Ss);
                    TrackHour("QualityStatus", existing.OverallQualityStatus, hReq.Qs);
                    TrackHour("PerfStatus", existing.OverallPerfStatus, hReq.Ps);
                    TrackHour("Notes", existing.PerformanceNotes, hReq.Pnote);

                    existing.HazardsObserved = hReq.Haz;
                    existing.UnsafeBehaviours = hReq.Uns;
                    existing.PositiveBehaviours = hReq.Pos;
                    existing.SafetyNotes = hReq.Snote;
                    existing.QualityChecksCompleted = hReq.Qchk;
                    existing.DeviationsEscalated = hReq.Dev;
                    existing.NonComplianceAddressed = hReq.Nc;
                    existing.QualityNotes = hReq.Qnote;
                    existing.HourlyTargetAchieved = hReq.Tgt;
                    existing.MaintenanceIssues = hReq.Maint;
                    existing.MaterialsAvailable = hReq.Mat;
                    existing.ToolsAvailable = hReq.Tools;
                    existing.EscalationsNeeded = hReq.Escl;
                    existing.PartsConfirmed = hReq.Pconf;
                    existing.PartsIdCorrect = hReq.Pid;
                    existing.NCPartsStoredCorrectly = hReq.Ncp;
                    existing.SixSCompleted = hReq.Sixs;
                    existing.TPMCompleted = hReq.Tpm;
                    existing.PerformanceNotes = hReq.Pnote;
                    existing.WellbeingConfirmed = hReq.Wb;
                    existing.SupportRequired = hReq.Sup;
                    existing.MoraleNotes = hReq.Mnote;
                    existing.AccidentsReported = hReq.Acc;
                    existing.OverallSafetyStatus = hReq.Ss;
                    existing.OverallQualityStatus = hReq.Qs;
                    existing.OverallPerfStatus = hReq.Ps;
                }
            }

            _db.AuditLogs.AddRange(logs);
            await _db.SaveChangesAsync();
            return Ok(new { id = shift.Id, auditEntries = logs.Count });
        }

        // GET today's shifts
        [HttpGet("today")]
        public async Task<IActionResult> Today()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var items = await _db.ShiftSubmissions
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

        // GET audit log for a submission
        [HttpGet("{id:int}/audit")]
        public async Task<IActionResult> Audit(int id)
        {
            var logs = await _db.AuditLogs
                .Where(a => a.SubmissionId == id)
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync();
            return Ok(logs);
        }

        // GET list
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? from,
            [FromQuery] string? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _db.ShiftSubmissions.Include(s => s.Hours).AsQueryable();
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

        // GET single submission
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

        // GET PDF
        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> Pdf(int id)
        {
            var shift = await _db.ShiftSubmissions
                .Include(s => s.Hours.OrderBy(h => h.HourNumber))
                .FirstOrDefaultAsync(s => s.Id == id);
            if (shift == null) return NotFound();
            var bytes = _pdf.GenerateShift(shift);
            var filename = $"TLSW_{shift.ShiftDate:yyyyMMdd}_{shift.Shift}_{shift.TeamLeaderDisplay.Replace(" ", "_")}.pdf";
            return File(bytes, "application/pdf", filename);
        }

        // GET CSV export
        [HttpGet("export/csv")]
        public async Task<IActionResult> Csv([FromQuery] string? from, [FromQuery] string? to)
        {
            var query = _db.ShiftSubmissions.Include(s => s.Hours).AsQueryable();
            if (!string.IsNullOrEmpty(from) && DateOnly.TryParse(from, out var f)) query = query.Where(s => s.ShiftDate >= f);
            if (!string.IsNullOrEmpty(to) && DateOnly.TryParse(to, out var t)) query = query.Where(s => s.ShiftDate <= t);

            var rows = await query.OrderByDescending(s => s.ShiftDate).ToListAsync();
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

        static HourlyCheck MapHour(HourRequest h) => new()
        {
            HourNumber = (byte)h.Hour,
            HazardsObserved = h.Haz,
            UnsafeBehaviours = h.Uns,
            PositiveBehaviours = h.Pos,
            SafetyNotes = h.Snote,
            QualityChecksCompleted = h.Qchk,
            DeviationsEscalated = h.Dev,
            NonComplianceAddressed = h.Nc,
            QualityNotes = h.Qnote,
            HourlyTargetAchieved = h.Tgt,
            MaintenanceIssues = h.Maint,
            MaterialsAvailable = h.Mat,
            ToolsAvailable = h.Tools,
            EscalationsNeeded = h.Escl,
            PartsConfirmed = h.Pconf,
            PartsIdCorrect = h.Pid,
            NCPartsStoredCorrectly = h.Ncp,
            SixSCompleted = h.Sixs,
            TPMCompleted = h.Tpm,
            PerformanceNotes = h.Pnote,
            WellbeingConfirmed = h.Wb,
            SupportRequired = h.Sup,
            MoraleNotes = h.Mnote,
            AccidentsReported = h.Acc,
            OverallSafetyStatus = h.Ss,
            OverallQualityStatus = h.Qs,
            OverallPerfStatus = h.Ps,
        };
    }

    public class ShiftSubmissionRequest
    {
        public string ShiftDate { get; set; } = "";
        public string Shift { get; set; } = "";
        public string? Area { get; set; }
        public string? TeamLeader { get; set; }
        public List<HourRequest> Hours { get; set; } = new();
        public string? Escalations { get; set; }
        public string? KeyRisks { get; set; }
        public string? Priorities { get; set; }
        public string? OutgoingSignature { get; set; }
        public string? IncomingSignature { get; set; }
    }

    public class HourRequest
    {
        public int Hour { get; set; }
        public bool? Haz { get; set; }
        public bool? Uns { get; set; }
        public bool? Pos { get; set; }
        public string? Snote { get; set; }
        public bool? Qchk { get; set; }
        public bool? Dev { get; set; }
        public bool? Nc { get; set; }
        public string? Qnote { get; set; }
        public bool? Tgt { get; set; }
        public bool? Maint { get; set; }
        public bool? Mat { get; set; }
        public bool? Tools { get; set; }
        public bool? Escl { get; set; }
        public bool? Pconf { get; set; }
        public bool? Pid { get; set; }
        public bool? Ncp { get; set; }
        public bool? Sixs { get; set; }
        public bool? Tpm { get; set; }
        public string? Pnote { get; set; }
        public bool? Wb { get; set; }
        public bool? Sup { get; set; }
        public string? Mnote { get; set; }
        public bool? Acc { get; set; }
        public string? Ss { get; set; }
        public string? Qs { get; set; }
        public string? Ps { get; set; }
    }
}