using TL.Models;

namespace TL.Services;

public static class SeniorAuditScoring
{
    public static int CategoryScore(params bool?[] fields)
    {
        var answered = fields.Where(f => f.HasValue).ToArray();
        if (answered.Length == 0) return 0;
        return answered.Count(f => f == true) * 100 / answered.Length;
    }

    public static int GovernanceScore(SeniorWeeklyAudit a) =>
        CategoryScore(a.HandoverStandardsFollowed, a.VisualManagementCurrent, a.EscalationPathsUsed);

    public static int SafetyScore(SeniorWeeklyAudit a) =>
        CategoryScore(a.PpeComplianceFull, a.NearMissesReported, a.SafetyActionLogCurrent);

    public static int QualityScore(SeniorWeeklyAudit a) =>
        CategoryScore(a.FirstOffRecordsComplete, a.NcCaptureTrended, a.QualityGatesMaintained);

    public static int PeopleScore(SeniorWeeklyAudit a) =>
        CategoryScore(a.AbsenceManagedProactively, a.TlsCoachingTeams, a.TrainingMatrixCurrent);

    public static int StandardsScore(SeniorWeeklyAudit a) =>
        CategoryScore(a.SixSStandardMaintained, a.TpmScheduleFollowed, a.StandardWorkVisible);

    public static int PerformanceScore(SeniorWeeklyAudit a) =>
        CategoryScore(a.TrackingAgainstWeeklyPlan, a.MetricsVisibleAndOwned, a.ImprovementActionsProgressing);

    public static int OverallScore(SeniorWeeklyAudit a)
    {
        var scores = new[]
        {
            GovernanceScore(a), SafetyScore(a), QualityScore(a),
            PeopleScore(a), StandardsScore(a), PerformanceScore(a)
        }.Where(s => s > 0).ToArray();
        return scores.Length > 0 ? (int)scores.Average() : 0;
    }

    public static string GaugeColor(int pct) => pct >= 80 ? "#22c55e" : pct >= 50 ? "#f59e0b" : "#ef4444";
}
