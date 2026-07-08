namespace TL.Models;

public class SeniorWeeklyAudit
{
    public int Id { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string SubmittedBy { get; set; } = "";
    public string AuditorName { get; set; } = "";
    public DateOnly AuditDate { get; set; }
    public string Area { get; set; } = "";

    // ── Leadership & Governance (0=not met, 1=partial, 2=met) ─────────────────
    public byte? HandoverStandardsFollowed { get; set; }
    public byte? VisualManagementCurrent { get; set; }
    public byte? EscalationPathsUsed { get; set; }
    public string? GovernanceNotes { get; set; }

    // ── Safety Culture ────────────────────────────────────────────────────────
    public byte? PpeComplianceFull { get; set; }
    public byte? NearMissesReported { get; set; }
    public byte? SafetyActionLogCurrent { get; set; }
    public string? SafetyNotes { get; set; }

    // ── Quality Governance ────────────────────────────────────────────────────
    public byte? FirstOffRecordsComplete { get; set; }
    public byte? NcCaptureTrended { get; set; }
    public byte? QualityGatesMaintained { get; set; }
    public string? QualityNotes { get; set; }

    // ── People & Wellbeing ────────────────────────────────────────────────────
    public byte? AbsenceManagedProactively { get; set; }
    public byte? TlsCoachingTeams { get; set; }
    public byte? TrainingMatrixCurrent { get; set; }
    public string? PeopleNotes { get; set; }

    // ── Standards & Housekeeping ──────────────────────────────────────────────
    public byte? SixSStandardMaintained { get; set; }
    public byte? TpmScheduleFollowed { get; set; }
    public byte? StandardWorkVisible { get; set; }
    public string? StandardsNotes { get; set; }

    // ── Performance ───────────────────────────────────────────────────────────
    public byte? TrackingAgainstWeeklyPlan { get; set; }
    public byte? MetricsVisibleAndOwned { get; set; }
    public byte? ImprovementActionsProgressing { get; set; }
    public string? PerformanceNotes { get; set; }

    // ── Findings & sign-off ───────────────────────────────────────────────────
    public string? GoodPracticeObserved { get; set; }
    public string? AreasForImprovement { get; set; }
    public string? ActionsRaised { get; set; }
    public string? OverallVerdict { get; set; }   // Green / Amber / Red
    public string? AuditorSignature { get; set; }
}
