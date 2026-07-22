using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Models;

namespace TL.Services;

public class TlShiftComplianceService
{
    private readonly AppDbContext _db;
    private readonly ShiftCompletionService _completion;

    public TlShiftComplianceService(AppDbContext db, ShiftCompletionService completion)
    {
        _db = db;
        _completion = completion;
    }

    public async Task<TlShiftComplianceSnapshot> LoadAsync(
        DateOnly rangeStart,
        DateOnly rangeEnd,
        string? area = null,
        string? department = null,
        string? shift = null,
        string? teamLeader = null)
    {
        var deptAreas = string.IsNullOrEmpty(department)
            ? null
            : AreaList.All
                .Where(a => a.Group == department)
                .Select(a => a.Label)
                .ToHashSet();

        var q = _db.ShiftSubmissions
            .Include(s => s.Hours)
            .ExcludeAudits()
            .Where(s => s.ShiftDate >= rangeStart && s.ShiftDate <= rangeEnd);

        if (!string.IsNullOrEmpty(area))
            q = q.Where(s => s.Area == area);
        else if (deptAreas != null)
            q = q.Where(s => s.Area != null && deptAreas.Contains(s.Area));

        if (!string.IsNullOrEmpty(shift))
            q = q.Where(s => s.Shift == shift);

        if (!string.IsNullOrEmpty(teamLeader))
            q = q.Where(s => s.TeamLeaderDisplay != null && s.TeamLeaderDisplay.Contains(teamLeader));

        var shifts = await q.OrderByDescending(s => s.ShiftDate).ThenBy(s => s.Shift).ToListAsync();

        var issueRows = new List<HodTlShiftIssueRow>();
        var tlStats = new Dictionary<string, (int Total, int Problems, int Incomplete, int Unsigned, HashSet<string> Areas, List<string> Summaries)>(StringComparer.OrdinalIgnoreCase);
        var incomplete = 0;
        var unsigned = 0;

        foreach (var row in shifts)
        {
            var comp = _completion.Evaluate(row);
            var signed = comp.SignedOff;
            var closeStatus = ResolveCloseStatus(comp, signed);
            var hasProblem = !comp.IsComplete || !signed;

            if (!comp.IsComplete) incomplete++;
            if (!signed) unsigned++;

            var tl = string.IsNullOrWhiteSpace(row.TeamLeaderDisplay) ? "Unknown TL" : row.TeamLeaderDisplay.Trim();
            if (!tlStats.TryGetValue(tl, out var stat))
                stat = (0, 0, 0, 0, new HashSet<string>(), new List<string>());
            stat.Total++;
            stat.Areas.Add(row.Area ?? "Unknown");
            if (hasProblem)
            {
                stat.Problems++;
                if (!comp.IsComplete) stat.Incomplete++;
                if (!signed) stat.Unsigned++;

                issueRows.Add(new HodTlShiftIssueRow(
                    row.Id,
                    tl,
                    row.ShiftDate,
                    row.Shift,
                    row.Area ?? "—",
                    closeStatus,
                    comp.HoursComplete,
                    comp.HoursTotal,
                    BuildShiftIssues(comp, signed)));

                var headline = $"{row.ShiftDate:dd/MM} {row.Shift}: {closeStatus}";
                if (!stat.Summaries.Contains(headline))
                    stat.Summaries.Add(headline);
            }
            tlStats[tl] = stat;
        }

        var accountability = tlStats
            .Where(kv => kv.Value.Problems > 0)
            .Select(kv => new HodTlAccountabilityRow(
                kv.Key,
                kv.Value.Areas.Count == 1 ? kv.Value.Areas.First() : $"{kv.Value.Areas.Count} areas",
                kv.Value.Total,
                kv.Value.Problems,
                kv.Value.Incomplete,
                kv.Value.Unsigned,
                kv.Value.Summaries.Take(5).ToList()))
            .OrderByDescending(r => r.ProblemShifts)
            .ThenByDescending(r => r.Incomplete)
            .ThenBy(r => r.TeamLeader)
            .ToList();

        var areaCompliance = shifts
            .GroupBy(s => s.Area ?? "Unknown")
            .Select(g =>
            {
                var areaIssues = issueRows.Where(r => r.Area == (g.Key == "Unknown" ? "—" : g.Key) || r.Area == g.Key).ToList();
                if (areaIssues.Count == 0) return null;
                return new HodAreaComplianceRow(
                    g.Key,
                    g.Count(),
                    areaIssues.Count(x => x.CloseStatus == "Incomplete form"),
                    areaIssues.Count(x => x.CloseStatus == "Not signed off"));
            })
            .Where(r => r != null)
            .Cast<HodAreaComplianceRow>()
            .OrderByDescending(r => r.Problems)
            .ThenBy(r => r.Area)
            .Take(12)
            .ToList();

        return new TlShiftComplianceSnapshot(
            shifts.Count,
            issueRows.Count,
            incomplete,
            unsigned,
            accountability,
            issueRows,
            areaCompliance);
    }

    public static TlShiftComplianceSnapshot FilterSnapshot(
        TlShiftComplianceSnapshot snapshot, string? shift, string? teamLeader)
    {
        if (string.IsNullOrEmpty(shift) && string.IsNullOrEmpty(teamLeader))
            return snapshot;

        var issues = snapshot.ShiftIssues.AsEnumerable();
        if (!string.IsNullOrEmpty(shift))
            issues = issues.Where(i => string.Equals(i.Shift, shift, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(teamLeader))
            issues = issues.Where(i => i.TeamLeader.Contains(teamLeader, StringComparison.OrdinalIgnoreCase));
        var issueList = issues.ToList();

        // Rebuild TL accountability from filtered issue rows so ProblemShifts / totals match.
        var accountability = issueList
            .GroupBy(i => i.TeamLeader, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var areas = g.Select(x => x.Area).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var incomplete = g.Count(x => x.CloseStatus == "Incomplete form");
                var unsigned = g.Count(x => x.CloseStatus == "Not signed off");
                return new HodTlAccountabilityRow(
                    g.First().TeamLeader,
                    areas.Count == 1 ? areas[0] : $"{areas.Count} areas",
                    g.Count(),
                    g.Count(),
                    incomplete,
                    unsigned,
                    g.Select(x => $"{x.ShiftDate:dd/MM} {x.Shift}: {x.CloseStatus}").Take(5).ToList());
            })
            .OrderByDescending(r => r.ProblemShifts)
            .ThenBy(r => r.TeamLeader)
            .ToList();

        var areaCompliance = issueList
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Area) || i.Area == "—" ? "Unknown" : i.Area, StringComparer.OrdinalIgnoreCase)
            .Select(g => new HodAreaComplianceRow(
                g.Key,
                g.Count(),
                g.Count(x => x.CloseStatus == "Incomplete form"),
                g.Count(x => x.CloseStatus == "Not signed off")))
            .OrderByDescending(r => r.Problems)
            .ThenBy(r => r.Area)
            .ToList();

        var incomplete = issueList.Count(i => i.CloseStatus == "Incomplete form");
        var unsigned = issueList.Count(i => i.CloseStatus == "Not signed off");

        return snapshot with
        {
            // Filtered view only sees problem rows — don't keep the unfiltered total
            // next to a narrowed NotClosed count (would read as "3 of 40" incorrectly).
            ShiftCount = issueList.Count,
            NotClosed = issueList.Count,
            Incomplete = incomplete,
            Unsigned = unsigned,
            Accountability = accountability,
            ShiftIssues = issueList,
            AreaCompliance = areaCompliance,
        };
    }

    static string ResolveCloseStatus(ShiftCompletionResult comp, bool signed)
    {
        if (comp.IsComplete && signed) return "Closed correctly";
        if (!signed) return "Not signed off";
        return "Incomplete form";
    }

    static List<string> BuildShiftIssues(ShiftCompletionResult comp, bool signed)
    {
        var issues = new List<string>();
        if (!comp.IsComplete)
        {
            issues.AddRange(comp.MissingItems.Take(4));
            if (comp.MissingItems.Count > 4)
                issues.Add($"+{comp.MissingItems.Count - 4} more");
        }
        if (!signed)
            issues.Add("Outgoing TL sign-off missing");
        return issues;
    }
}

public record TlShiftComplianceSnapshot(
    int ShiftCount,
    int NotClosed,
    int Incomplete,
    int Unsigned,
    List<HodTlAccountabilityRow> Accountability,
    List<HodTlShiftIssueRow> ShiftIssues,
    List<HodAreaComplianceRow> AreaCompliance);
