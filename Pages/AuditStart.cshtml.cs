using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class AuditStartModel : PageModel
{
    private readonly UserService _users;
    private readonly HodEffectivenessService _effectiveness;

    public AuditStartModel(UserService users, HodEffectivenessService effectiveness)
    {
        _users = users;
        _effectiveness = effectiveness;
    }

    public string AuditorName { get; set; } = "";
    public string AuditDate { get; set; } = "";
    public string SuggestedType { get; set; } = "";
    public string SuggestedTypeLabel { get; set; } = "";
    public string? Error { get; set; }

    public void OnGet()
    {
        var user = _users.GetCurrentUser();
        AuditorName = user.DisplayName;
        AuditDate = DateTime.Today.ToString("yyyy-MM-dd");
        SuggestedType = HodAuditTypes.SuggestedForDay(DateTime.Today.DayOfWeek);
        SuggestedTypeLabel = HodAuditTypes.LabelFor(SuggestedType);
    }

    public async Task<IActionResult> OnGetComplianceAsync(string effectivenessArea, string date, string? department)
    {
        if (string.IsNullOrWhiteSpace(effectivenessArea) || !DateOnly.TryParse(date, out var auditDate))
            return new JsonResult(new { error = "Select a zone and audit date." });

        var dept = department ?? AreaList.GetDepartment(effectivenessArea) ?? "";
        var summary = await _effectiveness.GetComplianceSummaryAsync(dept, effectivenessArea, auditDate);

        return new JsonResult(new
        {
            weekStart = summary.WeekStart.ToString("dd/MM/yyyy"),
            weekEnd = summary.WeekEnd.ToString("dd/MM/yyyy"),
            summary.TotalShifts,
            summary.NotClosedCorrectly,
            summary.IncompleteForms,
            summary.Unsigned,
            summary.HandoverPending,
            summary.ClosedCorrectly,
            findings = summary.Findings.Select(f => new
            {
                f.TeamLeader,
                f.Shift,
                shiftDate = f.ShiftDate == default ? "" : f.ShiftDate.ToString("dd/MM/yyyy"),
                f.FormComplete,
                f.HoursComplete,
                f.HoursTotal,
                f.OutgoingSignedOff,
                f.IncomingHandoverAcknowledged,
                f.CloseStatus,
                f.IsAuditFinding,
                issues = f.Issues,
            }),
        });
    }

    public IActionResult OnPost(
        string auditDate, string auditorName, string department, string effectivenessArea, string auditType)
    {
        AuditorName = auditorName ?? "";
        AuditDate = auditDate ?? DateTime.Today.ToString("yyyy-MM-dd");
        if (DateOnly.TryParse(AuditDate, out var d))
        {
            SuggestedType = HodAuditTypes.SuggestedForDate(d);
            SuggestedTypeLabel = HodAuditTypes.LabelFor(SuggestedType);
        }

        if (string.IsNullOrWhiteSpace(auditorName)
            || string.IsNullOrWhiteSpace(department)
            || string.IsNullOrWhiteSpace(effectivenessArea))
        {
            Error = "Please fill in all required fields.";
            return Page();
        }

        if (!AreaList.IsInDepartment(effectivenessArea, department))
        {
            Error = "The effectiveness check zone must belong to the selected area.";
            return Page();
        }

        var type = string.IsNullOrWhiteSpace(auditType)
            ? (DateOnly.TryParse(auditDate, out var ad) ? HodAuditTypes.SuggestedForDate(ad) : HodAuditTypes.SuggestedForDay(DateTime.Today.DayOfWeek))
            : auditType;

        return RedirectToPage("/Audit", new
        {
            date = auditDate,
            auditor = auditorName,
            department,
            effectivenessArea,
            type,
        });
    }
}
