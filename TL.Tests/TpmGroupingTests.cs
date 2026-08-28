using TL.Models;
using TL.Services;

namespace TL.Tests;

public class TpmGroupingTests
{
    static string AssemblyDept => AreaList.All.First(a => a.Group == "Assembly").Group;

    [Fact]
    public void Tpm_collapses_grouped_assembly_lines_into_one_board_each()
    {
        var q = HodAuditDefinitions.GetQuestions(HodAuditTypes.Tpm, "Assembly");
        var sections = q.Select(x => x.Section).Distinct().ToList();

        // Grouped boards appear...
        Assert.Contains(sections, s => s.Contains("MSFT / E4000 / MOR"));
        Assert.Contains(sections, s => s.Contains("HP / Ozeki / SPC / TX"));
        // ...and the individual grouped lines no longer generate their own boards.
        Assert.DoesNotContain(sections, s => s.Contains("5 — MICROSOFT"));
        Assert.DoesNotContain(sections, s => s.Contains("1 — HP"));
        // Ungrouped assembly lines remain individual.
        Assert.Contains(sections, s => s.Contains("3 — META"));
    }

    [Fact]
    public void Non_tpm_audits_keep_individual_lines()
    {
        // 6S for Assembly still references the individual lines (no grouping).
        var q6s = HodAuditDefinitions.GetQuestions(HodAuditTypes.SixS, "Assembly");
        var text = string.Join(" ", q6s.Select(x => x.Section + " " + x.Label));
        Assert.DoesNotContain("MSFT / E4000 / MOR", text);

        // And the individual lines are still selectable areas app-wide.
        Assert.Contains(AreaList.GetLabelsForDepartment("Assembly"), l => l == "5 — MICROSOFT");
        Assert.Contains(AreaList.GetLabelsForDepartment("Assembly"), l => l == "1 — HP");
    }
}
