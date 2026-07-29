using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Helpers;
using TL.Services;

namespace TL.Controllers;

[ApiController]
[Route("api/meetings")]
public class MeetingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PdfExportService _pdf;
    private readonly ILogger<MeetingsController> _log;

    public MeetingsController(AppDbContext db, PdfExportService pdf, ILogger<MeetingsController> log)
    {
        _db = db;
        _pdf = pdf;
        _log = log;
    }

    // Team meetings are viewable by everyone, so the PDF is not role-gated.
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id)
    {
        var m = await _db.TeamMeetings.FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();

        try
        {
            var bytes = _pdf.GenerateTeamMeeting(
                m,
                TeamMeetingSerializer.Problems(m.ProblemsJson),
                TeamMeetingSerializer.Attendees(m.TeamMembersJson),
                TeamMeetingSerializer.Attendees(m.GroupMembersJson),
                TeamMeetingSerializer.Guests(m.GuestsJson));
            var filename = $"TeamMeeting_{m.MeetingDate:yyyyMMdd}_{m.Area.Replace(" ", "_")}_{m.Shift}.pdf";
            return PdfResponse.File(this, bytes, filename);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Team meeting PDF failed for id {Id}", id);
            return new ContentResult
            {
                StatusCode = 500,
                ContentType = "text/plain",
                Content = "PDF generation failed. Please try again or contact support.",
            };
        }
    }
}
