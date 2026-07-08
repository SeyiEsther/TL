using TL.Models;

namespace TL.Services;

public static class SeniorAuditScoring
{
    public const byte NotMet = 0;
    public const byte Partial = 1;
    public const byte Met = 2;

    public static int CategoryScore(params byte?[] fields)
    {
        var answered = fields.Where(f => f is >= 0 and <= 2).ToArray();
        if (answered.Length == 0) return 0;
        var total = answered.Sum(f => f!.Value);
        var max = answered.Length * Met;
        return total * 100 / max;
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

    public static int QuestionAvgPct(IEnumerable<SeniorWeeklyAudit> audits, Func<SeniorWeeklyAudit, byte?> field)
    {
        var answered = audits.Where(a => field(a) is >= 0 and <= 2).ToList();
        if (answered.Count == 0) return 0;
        return (int)answered.Average(a => field(a)!.Value * 50.0);
    }

    public static string ScoreLabel(byte? score) => score switch
    {
        2 => "Met (2)",
        1 => "Partial (1)",
        0 => "Not met (0)",
        _ => "—",
    };

    public static string GaugeColor(int pct) => pct >= 80 ? "#22c55e" : pct >= 50 ? "#f59e0b" : "#ef4444";
}
