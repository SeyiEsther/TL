using TL.Models;

namespace TL.Services;

public static class HodAuditDefinitions
{
    public static List<HodAuditQuestion> GetQuestions(string auditType, string department)
    {
        return auditType switch
        {
            HodAuditTypes.SixS => SixSQuestions(department),
            HodAuditTypes.Tpm => TpmQuestionsForDepartment(department),
            HodAuditTypes.PartsIdNc => PartsIdNcQuestions(department),
            HodAuditTypes.Quality => QualityQuestions(department),
            _ => SixSQuestions(department),
        };
    }

    static List<HodAuditQuestion> SixSQuestions(string department)
    {
        var scope = string.IsNullOrWhiteSpace(department) ? "the department" : department;
        return
        [
            new("6s_sort_1", "1S — Sort", $"Are unnecessary items, tools and materials removed from work areas across {scope}?"),
            new("6s_sort_2", "1S — Sort", $"Are only required components, equipment and WIP present at workstations across {scope}?"),
            new("6s_order_1", "2S — Set in Order", "Are all tools, materials and equipment clearly labelled and stored in their designated locations?"),
            new("6s_order_2", "2S — Set in Order", "Are walkways, storage areas and workstation boundaries clearly marked and kept clear?"),
            new("6s_shine_1", "3S — Shine", "Is the work area, equipment and machinery clean and free from dirt, swarf and debris?"),
            new("6s_shine_2", "3S — Shine", "Are cleaning responsibilities and schedules defined, visible and being followed?"),
            new("6s_std_1", "4S — Standardize", "Are visual standards, work instructions and 6S expectations displayed and up to date?"),
            new("6s_std_2", "4S — Standardize", $"Are 6S standards consistently applied across the entire {scope} with no local variations?"),
            new("6s_sustain_1", "5S — Sustain", "Is there clear evidence that 6S practices are being maintained between formal audits?"),
            new("6s_safety_1", "6S — Safety", "Are PPE requirements, safety hazards, emergency procedures and escape routes clearly marked and followed?"),
        ];
    }

    static List<HodAuditQuestion> TpmQuestionsForDepartment(string department)
    {
        var zones = TpmZonesForDepartment(department);

        if (zones.Count == 0)
            return [];

        var questions = new List<HodAuditQuestion>();
        var machineIndex = 0;
        foreach (var zoneLabel in zones)
        {
            var machines = AreaList.GetMachineList(zoneLabel);
            if (machines.Count == 0)
                machines = [zoneLabel];

            foreach (var machine in machines)
            {
                var idBase = $"tpm_{machineIndex}_{Slug(machine)}";
                var section = $"TPM — {zoneLabel} — {machine}";
                questions.Add(new($"{idBase}_content", section, "Does the TPM board have the required content?", machine));
                questions.Add(new($"{idBase}_current", section, "Is the TPM board up to date?", machine));
                questions.Add(new($"{idBase}_filled", section, "Is the TPM board being filled out?", machine));
                questions.Add(new($"{idBase}_visual", section, "Does the TPM board meet the visual standard?", machine));
                machineIndex++;
            }
        }
        return questions;
    }

    // TPM-only: the zones/boards to audit for a department, collapsing the
    // Assembly line groups into a single shared board each (answered once).
    static List<string> TpmZonesForDepartment(string department)
    {
        var labels = AreaList.All
            .Where(a => string.IsNullOrEmpty(department) || a.Group == department)
            .Select(a => a.Label)
            .ToList();

        foreach (var g in AreaList.TpmLineGroups)
        {
            var idx = labels.FindIndex(l => g.Members.Contains(l, StringComparer.OrdinalIgnoreCase));
            if (idx < 0) continue; // none of this group's lines are in scope
            labels.RemoveAll(l => g.Members.Contains(l, StringComparer.OrdinalIgnoreCase));
            labels.Insert(Math.Min(idx, labels.Count), g.Board);
        }
        return labels;
    }

    static List<HodAuditQuestion> PartsIdNcQuestions(string department)
    {
        var scope = string.IsNullOrWhiteSpace(department) ? "the department" : department;
        return
        [
            new("pid_1", "Part Identification", $"Is every live product on the floor in {scope} clearly labelled and identified?"),
            new("pid_2", "Part Identification", $"Are all bins, WIP and stock in {scope} correctly identified — not just the part on the machine?"),
            new("nc_1", "Non-Conformance", $"Is all non-conforming product in its designated NC location across {scope}?"),
            new("nc_2", "Non-Conformance", "Does every red card explain what the part is and what is wrong with it (not blank)?"),
            new("nc_3", "Non-Conformance", "Are NC parts clearly segregated from good product with no mixing?"),
        ];
    }

    static List<HodAuditQuestion> QualityQuestions(string department)
    {
        var scope = string.IsNullOrWhiteSpace(department) ? "the department" : department;
        return
        [
            new("qual_1", "Tools & Calibration", $"Are the tools being used in {scope} calibrated and within their calibration date?"),
            new("qual_2", "Documentation", "Is quality documentation being filled out as required?"),
            new("qual_3", "Documentation", "Is quality documentation clean, in order and stored correctly (not scattered)?"),
            new("qual_4", "Documentation", "Can you trust the quality records are complete based on how they are maintained?"),
        ];
    }

    static string Slug(string name) =>
        string.Concat(name.ToLowerInvariant().Select(c =>
            char.IsLetterOrDigit(c) ? c : '_')).Trim('_');
}
