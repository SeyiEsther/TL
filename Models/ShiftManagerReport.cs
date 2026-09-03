namespace TL.Models;

// Shift Manager Daily Report — a new daily form for shift managers, in addition
// to the audits they already do. Metric rows are stored as JSON (same pattern as
// the audits); comment sections are free text. Nothing here is shift-key unique;
// each submission is its own row.
public class ShiftManagerReport
{
    public int Id { get; set; }

    public DateOnly ReportDate { get; set; }
    public string Shift { get; set; } = "";
    public string ManagerName { get; set; } = "";

    public string SubmittedBy { get; set; } = "";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string? LastEditedBy { get; set; }
    public DateTime? LastEditedAt { get; set; }

    // JSON arrays of MetricRow / AuditRow (see ShiftReportDefs).
    public string? HseJson { get; set; }
    public string? ProductionJson { get; set; }
    public string? AuditsJson { get; set; }

    // Comments / Actions sections — free text.
    public string? ManagerHseComments { get; set; }
    public string? ProductionComments { get; set; }
    public string? LswTeamLeaderComments { get; set; }
    public string? LswHodComments { get; set; }
    public string? Aob { get; set; }
}

// One metric line: a label with a target and an actual (both free text so "12",
// "95%" or a short note all fit), a per-row Comments/Actions note and an
// Open/Closed progress flag — matching the emailed Daily Report spreadsheet.
// Comments/Progress are optional so older saved rows (which lack them)
// deserialize cleanly.
public record ShiftMetricRow(string Label, string? Target, string? Actual,
    string? Comments = null, string? Progress = null);

// Audit-completion line: which audit type, its scheduled day, and Y/N done.
public record ShiftAuditRow(string Type, string Day, string? Completed);

// Fixed row definitions transcribed from the Shift Manager Daily Report form.
public static class ShiftReportDefs
{
    // Sections mirror the emailed Daily Report spreadsheet exactly.
    public static readonly string[] HseRows =
    [
        "Accident", "Near Miss", "Hazards Reported",
        "Safety Walk - Positive conversation", "Safety Walk - NC conversation",
    ];

    public static readonly string[] QualityRows =
    [
        "Hold Reports", "Deviation", "Concession Raised",
    ];

    // Morale is a single count + comment per row (no target/actual split).
    public static readonly string[] MoraleRows =
    [
        "Absents PH1", "Absents PH3", "Absents Paint", "Absents Assembly",
        "Absents Internal Logistic", "Absence Furnace",
        "New Starters", "Leavers", "Thank you",
    ];

    // All non-production metric rows share one JSON store (keyed by label), so
    // the section split above is presentation only — no schema change, and older
    // reports (which stored every row together) still read back correctly.
    public static readonly string[] MetricRows =
        HseRows.Concat(QualityRows).Concat(MoraleRows).ToArray();

    public static readonly string[] ProductionRows =
    [
        "PH1 recovery", "PH3 Recovery", "Paint Efficiency White", "Paint Efficiency Black",
        "MS gen 6", "MS MOR", "MS E400", "Meta Standard", "Meta HPR", "Meta Network",
        "ORW", "Dell", "Google", "TX", "HP", "Special", "Accessory",
    ];

    public static readonly (string Type, string Day)[] AuditRows =
    [
        ("6S", "Monday"), ("TPM", "Tuesday"),
        ("Parts ID/Confirmation", "Wednesday"), ("Quality", "Thursday"),
    ];

    public static readonly string[] Shifts = ["Days", "Backs", "Nights"];
}
