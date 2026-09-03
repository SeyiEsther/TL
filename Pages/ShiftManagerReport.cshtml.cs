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
    private readonly TargetService _targets;

    public ShiftManagerReportModel(AppDbContext db, UserService users, PersonListService people, TargetService targets)
    {
        _db = db;
        _users = users;
        _people = people;
        _targets = targets;
    }

    [BindProperty] public int? EditingId { get; set; }
    [BindProperty] public string ReportDate { get; set; } = "";
    [BindProperty] public string Shift { get; set; } = "";
    [BindProperty] public string ManagerName { get; set; } = "";

    // One indexed set of lists per section, each carrying the spreadsheet's
    // Target / Actual / Comments-Actions / Progress(O/C) columns. Morale has no
    // target (single count + comment).
    [BindProperty] public List<string?> HseTarget { get; set; } = [];
    [BindProperty] public List<string?> HseActual { get; set; } = [];
    [BindProperty] public List<string?> HseComments { get; set; } = [];
    [BindProperty] public List<string?> HseProgress { get; set; } = [];

    [BindProperty] public List<string?> QualTarget { get; set; } = [];
    [BindProperty] public List<string?> QualActual { get; set; } = [];
    [BindProperty] public List<string?> QualComments { get; set; } = [];
    [BindProperty] public List<string?> QualProgress { get; set; } = [];

    [BindProperty] public List<string?> MoraleActual { get; set; } = [];
    [BindProperty] public List<string?> MoraleComments { get; set; } = [];
    [BindProperty] public List<string?> MoraleProgress { get; set; } = [];

    [BindProperty] public List<string?> ProdTarget { get; set; } = [];
    [BindProperty] public List<string?> ProdActual { get; set; } = [];
    [BindProperty] public List<string?> ProdComments { get; set; } = [];
    [BindProperty] public List<string?> ProdProgress { get; set; } = [];

    [BindProperty] public List<string?> AuditDone { get; set; } = [];

    [BindProperty] public string? LswTeamLeaderComments { get; set; }
    [BindProperty] public string? LswHodComments { get; set; }
    [BindProperty] public string? Aob { get; set; }
    // Legacy section-level comment fields, preserved (round-tripped) now that
    // comments live per row.
    [BindProperty] public string? ManagerHseComments { get; set; }
    [BindProperty] public string? ProductionComments { get; set; }

    // For rendering (labels + any saved values).
    public List<ShiftMetricRow> HseRows { get; set; } = [];
    public List<ShiftMetricRow> QualityRows { get; set; } = [];
    public List<ShiftMetricRow> MoraleRows { get; set; } = [];
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
            QualityRows = ShiftReportSerializer.QualityRows(r.HseJson);
            MoraleRows = ShiftReportSerializer.MoraleRows(r.HseJson);
            ProductionRows = ShiftReportSerializer.ProductionRows(r.ProductionJson);
            Audits = ShiftReportSerializer.AuditRows(r.AuditsJson);
        }
        else
        {
            ReportDate = DateTime.Today.ToString("yyyy-MM-dd");
            ManagerName = user.DisplayName;
            HseRows = ShiftReportSerializer.HseRows(null);
            QualityRows = ShiftReportSerializer.QualityRows(null);
            MoraleRows = ShiftReportSerializer.MoraleRows(null);
            ProductionRows = ShiftReportSerializer.ProductionRows(null);
            Audits = ShiftReportSerializer.AuditRows(null);
        }
        // Targets are admin-set and read-only here — always show the current
        // admin value (item 3: pull through automatically wherever displayed).
        OverlayTargets();
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
        // HSE + Quality + Morale all persist to the single metrics store, in the
        // canonical MetricRows order, so old readers/records stay compatible.
        // Target is not posted (read-only); snapshot the current admin value so
        // the saved record is self-contained for history.
        var metrics = new List<ShiftMetricRow>();
        metrics.AddRange(ZipT(SectionNames.Hse, ShiftReportDefs.HseRows, HseActual, HseComments, HseProgress));
        metrics.AddRange(ZipT(SectionNames.Quality, ShiftReportDefs.QualityRows, QualActual, QualComments, QualProgress));
        metrics.AddRange(Zip(ShiftReportDefs.MoraleRows, null, MoraleActual, MoraleComments, MoraleProgress));
        r.HseJson = ShiftReportSerializer.Metrics(metrics);
        r.ProductionJson = ShiftReportSerializer.Metrics(
            ZipT(SectionNames.Production, ShiftReportDefs.ProductionRows, ProdActual, ProdComments, ProdProgress));
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

    static List<ShiftMetricRow> Zip(string[] labels, List<string?>? targets, List<string?> actuals,
        List<string?> comments, List<string?> progress) =>
        labels.Select((l, i) => new ShiftMetricRow(
            l,
            targets != null && i < targets.Count ? targets[i] : null,
            i < actuals.Count ? actuals[i] : null,
            i < comments.Count ? comments[i] : null,
            i < progress.Count ? progress[i] : null)).ToList();

    // Like Zip, but the Target comes from the admin-set value for this section
    // (read-only), not from a posted field.
    List<ShiftMetricRow> ZipT(string section, string[] labels, List<string?> actuals,
        List<string?> comments, List<string?> progress) =>
        labels.Select((l, i) => new ShiftMetricRow(
            l,
            _targets.ReportTarget(section, l),
            i < actuals.Count ? actuals[i] : null,
            i < comments.Count ? comments[i] : null,
            i < progress.Count ? progress[i] : null)).ToList();

    // Replace each displayed row's Target with the current admin value.
    void OverlayTargets()
    {
        HseRows = WithTargets(SectionNames.Hse, HseRows);
        QualityRows = WithTargets(SectionNames.Quality, QualityRows);
        ProductionRows = WithTargets(SectionNames.Production, ProductionRows);
    }

    List<ShiftMetricRow> WithTargets(string section, List<ShiftMetricRow> rows) =>
        rows.Select(r => r with { Target = _targets.ReportTarget(section, r.Label) }).ToList();

    static List<ShiftAuditRow> ZipAudits(List<string?> done) =>
        ShiftReportDefs.AuditRows.Select((a, i) => new ShiftAuditRow(
            a.Type, a.Day, i < done.Count ? done[i] : null)).ToList();

    void Rehydrate()
    {
        HseRows = Zip(ShiftReportDefs.HseRows, HseTarget, HseActual, HseComments, HseProgress);
        QualityRows = Zip(ShiftReportDefs.QualityRows, QualTarget, QualActual, QualComments, QualProgress);
        MoraleRows = Zip(ShiftReportDefs.MoraleRows, null, MoraleActual, MoraleComments, MoraleProgress);
        ProductionRows = Zip(ShiftReportDefs.ProductionRows, ProdTarget, ProdActual, ProdComments, ProdProgress);
        Audits = ZipAudits(AuditDone);
        OverlayTargets();
    }
}
