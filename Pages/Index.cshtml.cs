using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class IndexModel : PageModel
{
    private readonly UserService _users;
    private readonly ShiftResumeService _resume;

    public IndexModel(UserService users, ShiftResumeService resume)
    {
        _users = users;
        _resume = resume;
    }

    public string? UserName { get; set; }
    public string ShiftDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public string? Shift { get; set; }
    public string? TeamLeader { get; set; }
    public string? Area { get; set; }
    public string? Error { get; set; }

    public void OnGet()
    {
        var displayName = _users.GetCurrentUser().DisplayName;
        UserName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
    }

    public async Task<IActionResult> OnPostAsync(string shiftDate, string shift, string teamLeader, string area)
    {
        teamLeader = ShiftResumeService.NormalizeTl(teamLeader);

        if (string.IsNullOrWhiteSpace(shiftDate) || string.IsNullOrWhiteSpace(shift) ||
            string.IsNullOrWhiteSpace(teamLeader) || string.IsNullOrWhiteSpace(area))
        {
            ShiftDate = shiftDate ?? DateTime.Today.ToString("yyyy-MM-dd");
            Shift = shift;
            TeamLeader = teamLeader;
            Area = area;
            Error = "Please fill in all fields.";
            var displayName = _users.GetCurrentUser().DisplayName;
            UserName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
            return Page();
        }

        if (!DateOnly.TryParse(shiftDate, out var d))
            d = DateOnly.FromDateTime(DateTime.Today);

        var existing = await _resume.FindForResumeAsync(d, shift, area, teamLeader);
        if (existing != null && ShiftResumeService.IsInProgress(existing))
            return RedirectToPage("/Form", new { id = existing.Id, tl = teamLeader });

        var previousShift = await _resume.FindPendingHandoverForAreaAsync(area, d, shift);
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
