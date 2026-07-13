namespace TL.Models;

public class HodDailyAudit
{
    public int Id { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string SubmittedBy { get; set; } = "";
    public string? LastEditedBy { get; set; }
    public DateTime? LastEditedAt { get; set; }

    public string AuditorName { get; set; } = "";
    public DateOnly AuditDate { get; set; }
    public string Department { get; set; } = "";
    public string EffectivenessArea { get; set; } = "";
    public string Area { get; set; } = "";
    public string AuditType { get; set; } = "";

    public string AnswersJson { get; set; } = "[]";
    public int TotalScore { get; set; }
    public int MaxScore { get; set; }

    public string? EffectivenessJson { get; set; }

    public string? ActionsRaised { get; set; }
    public string? GoodPractice { get; set; }
    public string? AuditorSignature { get; set; }
    public string? TeamLeaderSignature { get; set; }

    public string ResolveEffectivenessArea() =>
        !string.IsNullOrWhiteSpace(EffectivenessArea) ? EffectivenessArea : Area;
}

public class HodAuditAnswer
{
    public string QuestionId { get; set; } = "";
    public string Section { get; set; } = "";
    public string Label { get; set; } = "";
    public string? MachineName { get; set; }
    public bool? Pass { get; set; }
    public string? Evidence { get; set; }
}

public class HodEffectivenessFinding
{
    public int? ShiftSubmissionId { get; set; }
    public string TeamLeader { get; set; } = "";
    public string Shift { get; set; } = "";
    public DateOnly ShiftDate { get; set; }
    public string Area { get; set; } = "";
    public bool FormComplete { get; set; }
    public int HoursComplete { get; set; }
    public int HoursTotal { get; set; }
    public bool OutgoingSignedOff { get; set; }
    public bool IncomingHandoverAcknowledged { get; set; }
    public string CloseStatus { get; set; } = "";
    public List<string> Issues { get; set; } = new();
    public bool TlClaimedSixS { get; set; }
    public bool TlClaimedTpm { get; set; }
    public bool? TlClaimedPartsId { get; set; }
    public bool? TlClaimedNcStored { get; set; }
    public bool? TlClaimedQuality { get; set; }
    public bool IsAuditFinding { get; set; }
    public bool TlClaimMismatch { get; set; }
    public List<string> LinkedAuditFailures { get; set; } = new();
}

public record HodShiftComplianceSummary(
    DateOnly WeekStart,
    DateOnly WeekEnd,
    int TotalShifts,
    int NotClosedCorrectly,
    int IncompleteForms,
    int Unsigned,
    int ClosedCorrectly,
    List<HodEffectivenessFinding> Findings);

public record HodAuditQuestion(string Id, string Section, string Label, string? MachineName = null);

public static class HodAuditTypes
{
    public const string SixS = "SixS";
    public const string Tpm = "Tpm";
    public const string PartsIdNc = "PartsIdNc";
    public const string Quality = "Quality";

    public static readonly (string Value, string Label)[] All =
    [
        (SixS, "6S Audit"),
        (Tpm, "TPM Board Audit"),
        (PartsIdNc, "Parts ID & Non-Conformance"),
        (Quality, "Quality Audit"),
    ];

    public static string LabelFor(string type) =>
        All.FirstOrDefault(t => t.Value == type).Label ?? type;

    public static string SuggestedForDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => SixS,
        DayOfWeek.Tuesday => Tpm,
        DayOfWeek.Wednesday => PartsIdNc,
        DayOfWeek.Thursday => Quality,
        DayOfWeek.Friday => SixS,
        _ => SixS,
    };

    public static string SuggestedForDate(DateOnly date) => SuggestedForDay(date.DayOfWeek);
}
