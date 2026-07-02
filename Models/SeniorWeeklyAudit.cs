namespace TL.Models;

public class SeniorWeeklyAudit
{
    public int Id { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string SubmittedBy { get; set; } = "";
    public string AuditorName { get; set; } = "";
    public DateOnly AuditDate { get; set; }
    public string Area { get; set; } = "";

    // ── Leadership & Governance ───────────────────────────────────────────────
    public bool? HandoverStandardsFollowed { get; set; }
    public bool? VisualManagementCurrent { get; set; }
    public bool? EscalationPathsUsed { get; set; }
    public string? GovernanceNotes { get; set; }

    // ── Safety Culture ────────────────────────────────────────────────────────
    public bool? PpeComplianceFull { get; set; }
    public bool? NearMissesReported { get; set; }
    public bool? SafetyActionLogCurrent { get; set; }
    public string? SafetyNotes { get; set; }

    // ── Quality Governance ────────────────────────────────────────────────────
    public bool? FirstOffRecordsComplete { get; set; }
    public bool? NcCaptureTrended { get; set; }
    public bool? QualityGatesMaintained { get; set; }
    public string? QualityNotes { get; set; }

    // ── People & Wellbeing ────────────────────────────────────────────────────
    public bool? AbsenceManagedProactively { get; set; }
    public bool? TlsCoachingTeams { get; set; }
    public bool? TrainingMatrixCurrent { get; set; }
    public string? PeopleNotes { get; set; }

    // ── Standards & Housekeeping ──────────────────────────────────────────────
    public bool? SixSStandardMaintained { get; set; }
    public bool? TpmScheduleFollowed { get; set; }
    public bool? StandardWorkVisible { get; set; }
    public string? StandardsNotes { get; set; }

    // ── Performance ───────────────────────────────────────────────────────────
    public bool? TrackingAgainstWeeklyPlan { get; set; }
    public bool? MetricsVisibleAndOwned { get; set; }
    public bool? ImprovementActionsProgressing { get; set; }
    public string? PerformanceNotes { get; set; }

    // ── Findings & sign-off ───────────────────────────────────────────────────
    public string? GoodPracticeObserved { get; set; }
    public string? AreasForImprovement { get; set; }
    public string? ActionsRaised { get; set; }
    public string? OverallVerdict { get; set; }   // Green / Amber / Red
    public string? AuditorSignature { get; set; }
}
