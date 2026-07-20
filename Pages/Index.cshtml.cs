using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class IndexModel : PageModel
{
    public const string OtherValue = "__other__";

    private readonly AppDbContext _db;
    private readonly UserService _users;
    private readonly ShiftResumeService _resume;

    public IndexModel(AppDbContext db, UserService users, ShiftResumeService resume)
    {
        _db = db;
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
        if (TempData["Error"] is string tempError && !string.IsNullOrWhiteSpace(tempError))
            Error = tempError;
    }

    public async Task<IActionResult> OnPostAsync(
        string shiftDate, string shift, string teamLeader, string area,
        string? otherName, string? coveringFor)
    {
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

        var existing = await _resume.FindForResumeAsync(d, shift, area);
        if (existing != null)
        {
            if (!string.IsNullOrWhiteSpace(covering) && string.IsNullOrWhiteSpace(existing.CoveringFor))
            {
                try
                {
                    existing.CoveringFor = covering;
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    HttpContext.RequestServices.GetRequiredService<ILogger<IndexModel>>()
                        .LogWarning(ex, "Could not save covering-for on resume for shift {Id}", existing.Id);
                }
            }
            return RedirectToPage("/Form", new { id = existing.Id, tl = actualName, coveringFor = covering });
        }

        // Past-date back-shift: allow a late submission even if the slot has a closed record.
        // Only block re-opening today's already-closed shift to prevent accidental duplicates.
        var isToday = d == DateOnly.FromDateTime(DateTime.Today);
        if (isToday && await _resume.SlotHasClosedAsync(d, shift, area))
        {
            ShiftDate = shiftDate;
            Shift = shift;
            TeamLeader = teamLeader;
            Area = area;
            OtherName = otherName;
            CoveringFor = covering;
            Error = "This shift is already closed. Open it from History if you need to view or edit.";
            var displayName = _users.GetCurrentUser().DisplayName;
            UserName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
            return Page();
        }

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
