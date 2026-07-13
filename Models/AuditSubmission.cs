namespace TL.Models;

public class AuditSubmission
{
    public int Id { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string SubmittedBy { get; set; } = "";
    public string? LastEditedBy { get; set; }
    public DateTime? LastEditedAt { get; set; }

    public string AuditorName { get; set; } = "";
    public DateOnly AuditDate { get; set; }
    public string Area { get; set; } = "";
    public string? TLOnShift { get; set; }
    public string? ShiftObserved { get; set; }

    public bool? HazardsObserved { get; set; }
    public bool? UnsafeBehavioursObserved { get; set; }
    public bool? PositiveBehavioursPraised { get; set; }
    public string? SafetyNotes { get; set; }

    public bool? QualityChecksCompleted { get; set; }
    public bool? DeviationsEscalated { get; set; }
    public bool? NonComplianceAddressed { get; set; }
    public string? QualityNotes { get; set; }

    public bool? HourlyTargetAchieved { get; set; }
    public bool? MaintenanceIssues { get; set; }
    public bool? MaterialsAvailable { get; set; }
    public bool? ToolsAvailable { get; set; }
    public bool? EscalationsNeeded { get; set; }
    public bool? PartsConfirmed { get; set; }
    public bool? PartsIdCorrect { get; set; }
    public bool? NCPartsStoredCorrectly { get; set; }
    public bool? SixSCompleted { get; set; }
    public bool? TPMCompleted { get; set; }
    public string? PerformanceNotes { get; set; }

    public bool? WellbeingConfirmed { get; set; }
    public bool? SupportRequired { get; set; }
    public string? MoraleNotes { get; set; }

    public bool? AccidentsObserved { get; set; }
    public string? OverallSafetyStatus { get; set; }
    public string? OverallQualityStatus { get; set; }
    public string? OverallPerfStatus { get; set; }

    public string? ActionsRaised { get; set; }
    public string? GoodPracticeObserved { get; set; }
    public string? FollowUpRequired { get; set; }
    public string? ActionOwner { get; set; }
    public DateOnly? ActionDueDate { get; set; }

    public string? AuditorSignature { get; set; }
}
