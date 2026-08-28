using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class ShiftManagerReportModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserService _users;
    private readonly PersonListService _people;

    public ShiftManagerReportModel(AppDbContext db, UserService users, PersonListService people)
    {
        _db = db;
        _users = users;
        _people = people;
    }

    [BindProperty] public int? EditingId { get; set; }
    [BindProperty] public string ReportDate { get; set; } = "";
    [BindProperty] public string Shift { get; set; } = "";
    [BindProperty] public string ManagerName { get; set; } = "";

    [BindProperty] public List<string?> HseTarget { get; set; } = [];
    [BindProperty] public List<string?> HseActual { get; set; } = [];
    [BindProperty] public List<string?> ProdTarget { get; set; } = [];
    [BindProperty] public List<string?> ProdActual { get; set; } = [];
    [BindProperty] public List<string?> AuditDone { get; set; } = [];

    [BindProperty] public string? ManagerHseComments { get; set; }
    [BindProperty] public string? ProductionComments { get; set; }
    [BindProperty] public string? LswTeamLeaderComments { get; set; }
    [BindProperty] public string? LswHodComments { get; set; }
    [BindProperty] public string? Aob { get; set; }

    // For rendering (labels + any saved values).
    public List<ShiftMetricRow> HseRows { get; set; } = [];
    public List<ShiftMetricRow> ProductionRows { get; set; } = [];
    public List<ShiftAuditRow> Audits { get; set; } = [];
    public IReadOnlyList<string> Managers => _people.ShiftManagers;
    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        var user = _users.GetCurrentUser();
        ShiftManagerReport? r = id.HasValue
            ? await _db.ShiftManagerReports.FirstOrDefaultAsync(x => x.Id == id.Value)
            : null;

        if (r != null)
        {
            EditingId = r.Id;
            ReportDate = r.ReportDate.ToString("yyyy-MM-dd");
            Shift = r.Shift;
            ManagerName = r.ManagerName;
            ManagerHseComments = r.ManagerHseComments;
            ProductionComments = r.ProductionComments;
            LswTeamLeaderComments = r.LswTeamLeaderComments;
            LswHodComments = r.LswHodComments;
            Aob = r.Aob;
            HseRows = ShiftReportSerializer.HseRows(r.HseJson);
            ProductionRows = ShiftReportSerializer.ProductionRows(r.ProductionJson);
            Audits = ShiftReportSerializer.AuditRows(r.AuditsJson);
        }
        else
        {
            ReportDate = DateTime.Today.ToString("yyyy-MM-dd");
            ManagerName = user.DisplayName;
            HseRows = ShiftReportSerializer.HseRows(null);
            ProductionRows = ShiftReportSerializer.ProductionRows(null);
            Audits = ShiftReportSerializer.AuditRows(null);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(ManagerName) || string.IsNullOrWhiteSpace(Shift)
            || !DateOnly.TryParse(ReportDate, out var d))
        {
            Error = "Please fill in the date, shift and manager.";
            Rehydrate();
            return Page();
        }

        var user = _users.GetCurrentUser();
        var r = EditingId.HasValue
            ? await _db.ShiftManagerReports.FirstOrDefaultAsync(x => x.Id == EditingId.Value)
            : null;
        var isNew = r == null;
        r ??= new ShiftManagerReport { SubmittedBy = user.Username };

        r.ReportDate = d;
        r.Shift = Shift;
        r.ManagerName = ManagerName.Trim();
        r.HseJson = ShiftReportSerializer.Metrics(Zip(ShiftReportDefs.HseRows, HseTarget, HseActual));
        r.ProductionJson = ShiftReportSerializer.Metrics(Zip(ShiftReportDefs.ProductionRows, ProdTarget, ProdActual));
        r.AuditsJson = ShiftReportSerializer.Audits(ZipAudits(AuditDone));
        r.ManagerHseComments = ManagerHseComments;
        r.ProductionComments = ProductionComments;
        r.LswTeamLeaderComments = LswTeamLeaderComments;
        r.LswHodComments = LswHodComments;
        r.Aob = Aob;
        if (isNew) { _db.ShiftManagerReports.Add(r); }
        else { r.LastEditedBy = user.DisplayName; r.LastEditedAt = DateTime.UtcNow; }

        await _db.SaveChangesAsync();
        return RedirectToPage("/ShiftManagerReportSuccess", new { id = r.Id });
    }

    static List<ShiftMetricRow> Zip(string[] labels, List<string?> targets, List<string?> actuals) =>
        labels.Select((l, i) => new ShiftMetricRow(
            l,
            i < targets.Count ? targets[i] : null,
            i < actuals.Count ? actuals[i] : null)).ToList();

    static List<ShiftAuditRow> ZipAudits(List<string?> done) =>
        ShiftReportDefs.AuditRows.Select((a, i) => new ShiftAuditRow(
            a.Type, a.Day, i < done.Count ? done[i] : null)).ToList();

    void Rehydrate()
    {
        HseRows = Zip(ShiftReportDefs.HseRows, HseTarget, HseActual);
        ProductionRows = Zip(ShiftReportDefs.ProductionRows, ProdTarget, ProdActual);
        Audits = ZipAudits(AuditDone);
    }
}
