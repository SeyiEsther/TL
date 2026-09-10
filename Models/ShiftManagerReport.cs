namespace TL.Models;

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

    public string? HseJson { get; set; }
    public string? ProductionJson { get; set; }
    public string? AuditsJson { get; set; }

    public string? ManagerHseComments { get; set; }
    public string? ProductionComments { get; set; }
    public string? LswTeamLeaderComments { get; set; }
    public string? LswHodComments { get; set; }
    public string? Aob { get; set; }
}

public record ShiftMetricRow(string Label, string? Target, string? Actual,
    string? Comments = null, string? Progress = null);

public record ShiftAuditRow(string Type, string Day, string? Completed);

public static class ShiftReportDefs
{
    public static readonly string[] HseRows =
    [
        "Accident", "Near Miss", "Hazards Reported",
        "Safety Walk - Positive conversation", "Safety Walk - NC conversation",
    ];

    public static readonly string[] QualityRows =
    [
        "Hold Reports", "Deviation", "Concession Raised",
    ];

    public static readonly string[] MoraleRows =
    [
        "Absents PH1", "Absents PH3", "Absents Paint", "Absents Assembly",
        "Absents Internal Logistic", "Absence Furnace",
        "New Starters", "Leavers", "Thank you",
    ];

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

    // Declared after ProductionRows so the referenced arrays are initialised first.
    public static readonly (string Section, string[] Labels)[] TargetableSections =
    [
        (SectionNames.Hse, HseRows),
        (SectionNames.Quality, QualityRows),
        (SectionNames.Production, ProductionRows),
    ];
}

public static class SectionNames
{
    public const string Hse = "HSE";
    public const string Quality = "Quality";
    public const string Production = "Production";
}

public class ReportMetricTarget
{
    public int Id { get; set; }
    public string Section { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Target { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
