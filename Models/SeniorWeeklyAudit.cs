namespace TL.Models;

public class SeniorWeeklyAudit
{
    public int Id { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string SubmittedBy { get; set; } = "";
    public string AuditorName { get; set; } = "";
    public DateOnly AuditDate { get; set; }
    public string Area { get; set; } = "";

    public byte? HandoverStandardsFollowed { get; set; }
    public byte? VisualManagementCurrent { get; set; }
    public byte? EscalationPathsUsed { get; set; }
    public string? GovernanceNotes { get; set; }

    public byte? PpeComplianceFull { get; set; }
    public byte? NearMissesReported { get; set; }
    public string? SafetyNotes { get; set; }

    public byte? FirstOffRecordsComplete { get; set; }
    public byte? NcProcedureFollowed { get; set; }
    public byte? QualityGatesMaintained { get; set; }
    public string? QualityNotes { get; set; }

    public byte? LeaderVisibilityCheck { get; set; }
    public string? LastTeamMeeting { get; set; }
    public byte? TrainingMatrixCurrent { get; set; }
    public string? PeopleNotes { get; set; }

    public byte? SixSStandardMaintained { get; set; }
    public byte? TpmScheduleFollowed { get; set; }
    public byte? StandardWorkMaintained { get; set; }
    public string? StandardsNotes { get; set; }

    public byte? TrackingAgainstWeeklyPlan { get; set; }
    public byte? ImprovementActionsProgressing { get; set; }
    public string? PerformanceNotes { get; set; }

    public byte? SafetyActionLogCurrent { get; set; }
    public byte? NcCaptureTrended { get; set; }
    public byte? AbsenceManagedProactively { get; set; }
    public byte? TlsCoachingTeams { get; set; }
    public byte? StandardWorkVisible { get; set; }
    public byte? MetricsVisibleAndOwned { get; set; }

    public string? GoodPracticeObserved { get; set; }
    public string? AreasForImprovement { get; set; }
    public string? ActionsRaised { get; set; }
    public string? OverallVerdict { get; set; }
    public string? AuditorSignature { get; set; }
}
