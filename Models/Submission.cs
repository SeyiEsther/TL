using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TL.Models
{
    public class ShiftSubmission
    {
        public int Id { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string SubmittedBy { get; set; } = "";
        public string TeamLeaderDisplay { get; set; } = "";
        public DateOnly ShiftDate { get; set; }
        public string Shift { get; set; } = "";
        public string? Area { get; set; }
        public byte HoursCompleted { get; set; }
        public string? Escalations { get; set; }
        public string? KeyRisks { get; set; }
        public string? Priorities { get; set; }
        public string? OutgoingTLSignature { get; set; }
        public string? IncomingTLSignature { get; set; }
        public DateTime? IncomingTLSignedAt { get; set; }
        public string? LastEditedBy { get; set; }
        public DateTime? LastEditedAt { get; set; }
        public List<HourlyCheck> Hours { get; set; } = new();
        public List<AuditLog> AuditLogs { get; set; } = new();
    }

    public class HourlyCheck
    {
        public int Id { get; set; }
        public int ShiftSubmissionId { get; set; }
        public byte HourNumber { get; set; }
        public bool? HazardsObserved { get; set; }
        public bool? UnsafeBehaviours { get; set; }
        public bool? PositiveBehaviours { get; set; }
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
        public bool? AccidentsReported { get; set; }
        public string? OverallSafetyStatus { get; set; }
        public string? OverallQualityStatus { get; set; }
        public string? OverallPerfStatus { get; set; }
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public string ChangedBy { get; set; } = "";
        public DateTime ChangedAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
        public string FieldName { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public ShiftSubmission? Submission { get; set; }
    }

    public class ShiftSummaryDto
    {
        public int Id { get; set; }
        public DateOnly ShiftDate { get; set; }
        public string Shift { get; set; } = "";
        public string? Area { get; set; }
        public string TeamLeaderDisplay { get; set; } = "";
        public string SubmittedBy { get; set; } = "";
        public byte HoursCompleted { get; set; }
        public string? OverallSafetyStatus { get; set; }
        public string? OverallQualityStatus { get; set; }
        public string? OverallPerfStatus { get; set; }
        public DateTime SubmittedAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
        public string? LastEditedBy { get; set; }
        public DateTime? LastEditedAt { get; set; }
        public string? Escalations { get; set; }
        public string? OutgoingTLSignature { get; set; }
        public string? IncomingTLSignature { get; set; }
        public DateTime? IncomingTLSignedAt { get; set; }
        public List<string> SafetyFlags { get; set; } = new();
        public List<string> QualityFlags { get; set; } = new();
        public List<string> PerfFlags { get; set; } = new();
    }
}