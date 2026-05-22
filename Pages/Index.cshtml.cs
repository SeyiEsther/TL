using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserService _users;

    public IndexModel(AppDbContext db, UserService users)
    {
        _db = db;
        _users = users;
    }

    public string? UserName { get; set; }
    public string ShiftDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public string? Shift { get; set; }
    public string? TeamLeader { get; set; }
    public string? Area { get; set; }
    public string? Error { get; set; }

    public void OnGet()
    {
        var user = _users.GetCurrentUser();
        UserName = user.DisplayName;
    }

    public async Task<IActionResult> OnPostAsync(string shiftDate, string shift, string teamLeader, string area)
    {
        if (string.IsNullOrWhiteSpace(shiftDate) || string.IsNullOrWhiteSpace(shift) ||
            string.IsNullOrWhiteSpace(teamLeader) || string.IsNullOrWhiteSpace(area))
        {
            ShiftDate = shiftDate ?? DateTime.Today.ToString("yyyy-MM-dd");
            Shift = shift;
            TeamLeader = teamLeader;
            Area = area;
            Error = "Please fill in all fields.";
            var u = _users.GetCurrentUser();
            UserName = u.DisplayName;
            return Page();
        }

        // Check if a submission already exists for this date/shift/area
        if (DateOnly.TryParse(shiftDate, out var d))
        {
            var existing = await _db.ShiftSubmissions
                .FirstOrDefaultAsync(s => s.ShiftDate == d && s.Shift == shift && s.Area == area);
            if (existing != null)
                return RedirectToPage("/Form", new { id = existing.Id, tl = teamLeader });
        }

        // Trigger condition: check if the most recent shift for this area
        // has an outgoing TL signature but no incoming TL signature yet.
        // If so, the incoming TL must acknowledge before starting a new shift.
        var previousShift = await _db.ShiftSubmissions
            .Where(s => s.Area == area
                     && !string.IsNullOrEmpty(s.OutgoingTLSignature)
                     && string.IsNullOrEmpty(s.IncomingTLSignature))
            .OrderByDescending(s => s.ShiftDate)
            .ThenByDescending(s => s.SubmittedAt)
            .FirstOrDefaultAsync();

        if (previousShift != null)
        {
            return RedirectToPage("/HandoverAck", new
            {
                prevId = previousShift.Id,
                date = shiftDate,
                shift,
                area,
                tl = teamLeader
            });
        }

        return RedirectToPage("/Form", new
        {
            date = shiftDate,
            shift,
            area,
            tl = teamLeader
        });
    }
}
