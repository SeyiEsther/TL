namespace TL.Models;

public record HodAreaComplianceRow(string Area, int TotalShifts, int Incomplete, int Unsigned)
{
    public int Problems => Incomplete + Unsigned;
}

public record HodTlAccountabilityRow(
    string TeamLeader,
    string Area,
    int TotalShifts,
    int ProblemShifts,
    int Incomplete,
    int Unsigned,
    List<string> IssueSummary);

public record HodTlShiftIssueRow(
    int ShiftId,
    string TeamLeader,
    DateOnly ShiftDate,
    string Shift,
    string Area,
    string CloseStatus,
    int HoursComplete,
    int HoursTotal,
    List<string> Issues);

public record HodTlAuditCatchRow(
    string TeamLeader,
    DateOnly ShiftDate,
    string Shift,
    string Area,
    DateOnly AuditDate,
    string AuditType,
    List<string> Issues);
