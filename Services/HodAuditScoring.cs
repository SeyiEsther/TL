using TL.Models;

namespace TL.Services;

public static class HodAuditScoring
{
    public static (int Total, int Max) Score(IEnumerable<HodAuditAnswer> answers)
    {
        var list = answers.ToList();
        var max = list.Count;
        var total = list.Count(a => a.Pass == true);
        return (total, max);
    }

    public static string RatingBand(int total, int max)
    {
        if (max == 0) return "—";
        var pct = total * 100.0 / max;
        return pct switch
        {
            >= 92 => "Excellent",
            >= 72 => "Good",
            >= 40 => "Needs Improvement",
            _ => "Poor",
        };
    }

    public static string RatingDetail(int total, int max) => RatingBand(total, max) switch
    {
        "Excellent" => "Best practice standard maintained. Continue to sustain.",
        "Good" => "Minor improvements required. Follow up within one week.",
        "Needs Improvement" => "Significant gaps identified. Action plan required within 48 hours.",
        "Poor" => "Immediate corrective action required. Area must not be signed off.",
        _ => "",
    };

    public static string BandColor(int total, int max)
    {
        if (max == 0) return "#94a3b8";
        var pct = total * 100.0 / max;
        return pct switch
        {
            >= 92 => "#22c55e",
            >= 72 => "#3b82f6",
            >= 40 => "#f59e0b",
            _ => "#ef4444",
        };
    }
}
