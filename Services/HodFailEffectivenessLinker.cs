using TL.Models;

namespace TL.Services;

public static class HodFailEffectivenessLinker
{
    public static List<HodEffectivenessFinding> LinkFailures(
        List<HodAuditAnswer> answers,
        List<HodEffectivenessFinding> findings,
        string auditType)
    {
        var failed = answers.Where(a => a.Pass == false).ToList();
        if (failed.Count == 0) return findings;

        foreach (var finding in findings.Where(f => f.ShiftSubmissionId.HasValue))
        {
            finding.Issues ??= [];
            finding.LinkedAuditFailures ??= [];
            var relevantFails = failed.Where(f => MatchesAuditType(f, auditType)).ToList();
            if (relevantFails.Count == 0) continue;

            finding.LinkedAuditFailures = relevantFails.Select(f => f.Label).Distinct().ToList();
            finding.TlClaimMismatch = TlClaimedPass(finding, auditType);

            if (finding.TlClaimMismatch)
            {
                finding.IsAuditFinding = true;
                foreach (var label in finding.LinkedAuditFailures)
                    finding.Issues.Add($"Audit FAIL vs TL claim: {label}");
            }
        }

        return findings;
    }

    public static string MergeActions(
        string? existingActions,
        List<HodAuditAnswer> answers,
        List<HodEffectivenessFinding> findings)
    {
        var lines = new List<string>();

        foreach (var f in findings.Where(x => !string.IsNullOrEmpty(x.TeamLeader)))
        {
            if (!f.FormComplete)
            {
                lines.Add($"{f.TeamLeader} ({f.Shift}, {f.ShiftDate:dd/MM/yyyy}): shift form incomplete — {f.CloseStatus}");
                continue;
            }

            if (!f.OutgoingSignedOff)
                lines.Add($"{f.TeamLeader} ({f.Shift}, {f.ShiftDate:dd/MM/yyyy}): outgoing TL sign-off missing");
        }

        foreach (var f in findings.Where(x => x.TlClaimMismatch))
        {
            foreach (var fail in f.LinkedAuditFailures)
                lines.Add($"{f.TeamLeader} ({f.Shift}, {f.ShiftDate:dd/MM/yyyy}): TL claimed pass but audit failed — {fail}");
        }

        var unlinkedFails = answers
            .Where(a => a.Pass == false)
            .Where(a => !findings.Any(f => f.LinkedAuditFailures.Contains(a.Label)))
            .Select(a => a.Label)
            .Distinct();

        foreach (var label in unlinkedFails)
            lines.Add($"Audit fail (walkaround): {label}");

        var auto = string.Join("\n", lines.Distinct());
        if (string.IsNullOrWhiteSpace(auto)) return existingActions ?? "";
        if (string.IsNullOrWhiteSpace(existingActions)) return auto;

        var newLines = lines
            .Distinct()
            .Where(l => !existingActions.Contains(l, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (newLines.Count == 0) return existingActions;

        const string marker = "--- Auto-linked from audit ---";
        // Append under a single marker — never add a second one if it's already there.
        if (existingActions.Contains(marker, StringComparison.OrdinalIgnoreCase))
            return existingActions.TrimEnd() + "\n" + string.Join("\n", newLines);

        return existingActions.TrimEnd() + "\n\n" + marker + "\n" + string.Join("\n", newLines);
    }

    static bool MatchesAuditType(HodAuditAnswer answer, string auditType) =>
        auditType switch
        {
            HodAuditTypes.SixS => answer.QuestionId.StartsWith("6s_", StringComparison.OrdinalIgnoreCase),
            HodAuditTypes.Tpm => answer.QuestionId.StartsWith("tpm_", StringComparison.OrdinalIgnoreCase),
            HodAuditTypes.PartsIdNc => answer.QuestionId.StartsWith("pid_", StringComparison.OrdinalIgnoreCase)
                || answer.QuestionId.StartsWith("nc_", StringComparison.OrdinalIgnoreCase),
            HodAuditTypes.Quality => answer.QuestionId.StartsWith("qual_", StringComparison.OrdinalIgnoreCase),
            _ => true,
        };

    static bool TlClaimedPass(HodEffectivenessFinding finding, string auditType) =>
        auditType switch
        {
            HodAuditTypes.SixS => finding.TlClaimedSixS,
            HodAuditTypes.Tpm => finding.TlClaimedTpm,
            HodAuditTypes.PartsIdNc => finding.TlClaimedPartsId == true || finding.TlClaimedNcStored == true,
            HodAuditTypes.Quality => finding.TlClaimedQuality == true,
            _ => false,
        };
}
