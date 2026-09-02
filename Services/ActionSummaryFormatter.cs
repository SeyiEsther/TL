using System.Text;

namespace TL.Services;

// Splits a stored ActionsRaised value into (a) the human-typed action — the part
// written by a person, for a person — and (b) a one-line muted summary of the
// auto-linked audit findings, so the A3 board sheet shows the intent, not a dump.
public static class ActionSummaryFormatter
{
    public const string Marker = "--- Auto-linked from audit ---";

    public record Result(string Typed, string? Summary);

    public static Result Summarise(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new Result("", null);

        var typed = new List<string>();
        int incomplete = 0, signOff = 0, discrepancy = 0, walkaround = 0;

        foreach (var rawLine in raw.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) { typed.Add(""); continue; }
            if (line.StartsWith("---") && line.Contains("Auto-linked", StringComparison.OrdinalIgnoreCase))
                continue; // drop the marker (and any duplicate of it)

            // Classify auto-generated finding lines out of the human content.
            if (line.Contains("shift form incomplete", StringComparison.OrdinalIgnoreCase)) { incomplete++; continue; }
            if (line.Contains("sign-off missing", StringComparison.OrdinalIgnoreCase)) { signOff++; continue; }
            if (line.Contains("claimed pass but audit failed", StringComparison.OrdinalIgnoreCase)
                || line.Contains("Audit FAIL vs TL claim", StringComparison.OrdinalIgnoreCase)) { discrepancy++; continue; }
            if (line.StartsWith("Audit fail (walkaround)", StringComparison.OrdinalIgnoreCase)) { walkaround++; continue; }

            typed.Add(rawLine.TrimEnd());
        }

        var typedText = string.Join("\n", typed).Trim();

        var parts = new List<string>();
        if (incomplete > 0) parts.Add($"{incomplete} shift form{(incomplete == 1 ? "" : "s")} incomplete");
        if (signOff > 0) parts.Add($"{signOff} sign-off{(signOff == 1 ? "" : "s")} missing");
        // The discrepancy (claimed pass but failed on audit) is the one worth a
        // conversation — keep it distinct from the admin gaps above.
        if (discrepancy > 0) parts.Add($"{discrepancy} team leader{(discrepancy == 1 ? "" : "s")} claimed passes that failed on audit");
        if (walkaround > 0) parts.Add($"{walkaround} walkaround fail{(walkaround == 1 ? "" : "s")}");

        var summary = parts.Count == 0 ? null : Capitalise(string.Join(", ", parts)) + ".";
        return new Result(typedText, summary);
    }

    static string Capitalise(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
