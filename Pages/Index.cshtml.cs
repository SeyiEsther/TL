using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class IndexModel : PageModel
{
    public const string OtherValue = "__other__";

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
    public string? OtherName { get; set; }
    public string? CoveringFor { get; set; }
    public string? Error { get; set; }

    public void OnGet()
    {
        var displayName = _users.GetCurrentUser().DisplayName;
        UserName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
    }

    public async Task<IActionResult> OnPostAsync(
        string shiftDate, string shift, string teamLeader, string area,
        string? otherName, string? coveringFor)
    {
        // Resolve who is actually filling this in, and who (if anyone) they cover for.
        string actualName;
        string? covering = null;
        if (teamLeader == OtherValue)
        {
            actualName = ShiftResumeService.NormalizeTl(otherName ?? "");
            covering = string.IsNullOrWhiteSpace(coveringFor) ? null : coveringFor.Trim();
        }
        else
        {
            actualName = ShiftResumeService.NormalizeTl(teamLeader ?? "");
        }

        if (string.IsNullOrWhiteSpace(shiftDate) || string.IsNullOrWhiteSpace(shift) ||
            string.IsNullOrWhiteSpace(actualName) || string.IsNullOrWhiteSpace(area))
        {
            ShiftDate = shiftDate ?? DateTime.Today.ToString("yyyy-MM-dd");
            Shift = shift;
            TeamLeader = teamLeader;
            Area = area;
            OtherName = otherName;
            CoveringFor = covering;
            Error = teamLeader == OtherValue && string.IsNullOrWhiteSpace(actualName)
                ? "Please enter your name."
                : "Please fill in all fields.";
            var displayName = _users.GetCurrentUser().DisplayName;
            UserName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
            return Page();
        }

        if (!DateOnly.TryParse(shiftDate, out var d))
            d = DateOnly.FromDateTime(DateTime.Today);

        // Record identity is date + shift + area; match/resume on the real filler's name.
        var existing = await _resume.FindForResumeAsync(d, shift, area, actualName);
        if (existing != null && ShiftResumeService.IsInProgress(existing))
            return RedirectToPage("/Form", new { id = existing.Id, tl = actualName });

        return RedirectToPage("/Form", new
        {
            date = shiftDate,
            shift,
            area,
            tl = actualName,
            coveringFor = covering
        });
    }
}
