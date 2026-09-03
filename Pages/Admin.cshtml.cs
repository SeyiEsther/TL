using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Services;

namespace TL.Pages;

public class AdminModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AdminService _admin;
    private readonly RecordDeleteService _delete;
    private readonly DocumentNumberService _docs;
    private readonly TargetService _targets;
    private readonly UserService _users;

    public AdminModel(AppDbContext db, AdminService admin, RecordDeleteService delete,
        DocumentNumberService docs, TargetService targets, UserService users)
    {
        _db = db;
        _admin = admin;
        _delete = delete;
        _docs = docs;
        _targets = targets;
        _users = users;
    }

    public string? StatusMessage { get; set; }
    public List<InProgressShiftRow> InProgressShifts { get; set; } = [];
    public List<TL.Models.DocumentNumber> DocumentNumbers { get; set; } = [];
    public List<TargetService.TargetRow> Targets { get; set; } = [];
    public List<TargetService.ReportTargetRow> ReportTargets { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string? saved)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        StatusMessage = saved switch
        {
            "deleted" => "Record deleted.",
            "docnum" => "Document number updated.",
            "target" => "Target saved.",
            "targetfail" => "Target not saved — enter a whole number of 0 or more.",
            "rtarget" => "Report target saved.",
            "rtargetfail" => "Report target not saved.",
            _ => null,
        };

        DocumentNumbers = await _docs.AllAsync();
        Targets = await _targets.AllAsync();
        ReportTargets = await _targets.AllReportTargetsAsync();

        InProgressShifts = await _db.ShiftSubmissions
            .ExcludeAudits()
            .Where(s => string.IsNullOrEmpty(s.OutgoingTLSignature))
            .OrderByDescending(s => s.LastEditedAt ?? s.SubmittedAt)
            .Select(s => new InProgressShiftRow(
                s.Id,
                s.ShiftDate,
                s.Shift,
                s.Area ?? "—",
                s.TeamLeaderDisplay,
                s.CoveringFor,
                s.Hours.Count,
                s.HoursCompleted,
                s.LastEditedAt ?? s.SubmittedAt))
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteSessionAsync(int id)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        var ok = await _delete.DeleteShiftSubmissionAsync(id);
        return RedirectToPage(new { saved = ok ? "deleted" : null });
    }

    public async Task<IActionResult> OnPostUpdateDocNumberAsync(int id, string? number)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        await _docs.UpdateAsync(id, number);
        return RedirectToPage(new { saved = "docnum" });
    }

    public async Task<IActionResult> OnPostUpdateTargetAsync(string key, string? value)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        var by = _users.GetCurrentUser();
        var byName = string.IsNullOrWhiteSpace(by.DisplayName) ? by.Username : by.DisplayName;

        // Confirmed-write: only report success once the server has stored it.
        var ok = int.TryParse(value, out var v)
                 && await _targets.UpdateAsync(key ?? "", v, byName);
        return RedirectToPage(new { saved = ok ? "target" : "targetfail" });
    }

    public async Task<IActionResult> OnPostUpdateReportTargetAsync(string section, string label, string? value)
    {
        if (!_admin.IsAdmin())
            return RedirectToPage("/Index");

        var by = _users.GetCurrentUser();
        var byName = string.IsNullOrWhiteSpace(by.DisplayName) ? by.Username : by.DisplayName;

        var ok = await _targets.UpdateReportTargetAsync(section ?? "", label ?? "", value, byName);
        return RedirectToPage(new { saved = ok ? "rtarget" : "rtargetfail" });
    }

    public static string PersonLabel(string teamLeader, string? coveringFor) =>
        string.IsNullOrWhiteSpace(coveringFor) ? teamLeader : $"{teamLeader} (covering for {coveringFor})";
}

public record InProgressShiftRow(
    int Id,
    DateOnly Date,
    string Shift,
    string Area,
    string TeamLeader,
    string? CoveringFor,
    int HoursFilled,
    byte HoursTotal,
    DateTime LastActivity);
