using TL.Services;

namespace TL.Tests;

public class ActionSummaryFormatterTests
{
    [Fact]
    public void Typed_action_is_separated_from_the_auto_linked_findings()
    {
        var raw = "Re-brief the team on red-bin process by Friday.\n\n" +
                  "--- Auto-linked from audit ---\n" +
                  "Bob (Day, 01/08/2026): shift form incomplete — Incomplete form\n" +
                  "Sue (Back, 01/08/2026): shift form incomplete — Not signed off\n" +
                  "Amy (Day, 02/08/2026): TL claimed pass but audit failed — TPM board current\n" +
                  "Amy (Day, 02/08/2026): TL claimed pass but audit failed — TPM board filled";

        var r = ActionSummaryFormatter.Summarise(raw);

        Assert.Equal("Re-brief the team on red-bin process by Friday.", r.Typed);
        Assert.Equal("2 shift forms incomplete, 2 team leaders claimed passes that failed on audit.", r.Summary);
        Assert.DoesNotContain("Bob", r.Typed);       // findings not in the typed text
        Assert.DoesNotContain("Auto-linked", r.Typed);
    }

    [Fact]
    public void Works_when_there_is_no_typed_action_only_findings()
    {
        var raw = "Bob (Day, 01/08/2026): shift form incomplete — Incomplete form";
        var r = ActionSummaryFormatter.Summarise(raw);
        Assert.Equal("", r.Typed);
        Assert.Equal("1 shift form incomplete.", r.Summary);
    }

    [Fact]
    public void Distinguishes_admin_gaps_from_genuine_discrepancies()
    {
        var raw = "--- Auto-linked from audit ---\n" +
                  "X (Day, 01/08): shift form incomplete — x\n" +
                  "Y (Day, 01/08): TL claimed pass but audit failed — 6S standard";
        var r = ActionSummaryFormatter.Summarise(raw);
        // Both categories named, the discrepancy kept distinct.
        Assert.Contains("1 shift form incomplete", r.Summary);
        Assert.Contains("1 team leader claimed passes that failed on audit", r.Summary);
    }

    [Fact]
    public void No_summary_when_only_a_typed_action()
    {
        var r = ActionSummaryFormatter.Summarise("Fix the fan.");
        Assert.Equal("Fix the fan.", r.Typed);
        Assert.Null(r.Summary);
    }
}
