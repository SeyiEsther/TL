using TL.Models;

namespace TL.Services;

public static class HodAuditDefinitions
{
    public static List<HodAuditQuestion> GetQuestions(string auditType, string area)
    {
        return auditType switch
        {
            HodAuditTypes.SixS => SixSQuestions(),
            HodAuditTypes.Tpm => TpmQuestions(area),
            HodAuditTypes.PartsIdNc => PartsIdNcQuestions(),
            HodAuditTypes.Quality => QualityQuestions(),
            _ => SixSQuestions(),
        };
    }

    static List<HodAuditQuestion> SixSQuestions() =>
    [
        new("6s_sort_1", "1S — Sort", "Are unnecessary items, tools and materials removed from the work area?"),
        new("6s_sort_2", "1S — Sort", "Are only required components, equipment and WIP present at the workstation?"),
        new("6s_order_1", "2S — Set in Order", "Are all tools, materials and equipment clearly labelled and stored in their designated locations?"),
        new("6s_order_2", "2S — Set in Order", "Are walkways, storage areas and workstation boundaries clearly marked and kept clear?"),
        new("6s_shine_1", "3S — Shine", "Is the work area, equipment and machinery clean and free from dirt, swarf and debris?"),
        new("6s_shine_2", "3S — Shine", "Are cleaning responsibilities and schedules defined, visible and being followed?"),
        new("6s_std_1", "4S — Standardize", "Are visual standards, work instructions and 6S expectations displayed and up to date?"),
        new("6s_std_2", "4S — Standardize", "Are 6S standards consistently applied across the entire area with no local variations?"),
        new("6s_sustain_1", "5S — Sustain", "Is there clear evidence that 6S practices are being maintained between formal audits?"),
        new("6s_safety_1", "6S — Safety", "Are PPE requirements, safety hazards, emergency procedures and escape routes clearly marked and followed?"),
    ];

    static List<HodAuditQuestion> TpmQuestions(string area)
    {
        var machines = AreaList.GetMachineList(area);
        if (machines.Count == 0)
            machines = [area];

        var questions = new List<HodAuditQuestion>();
        foreach (var machine in machines)
        {
            var section = $"TPM Board — {machine}";
            questions.Add(new($"tpm_{Slug(machine)}_content", section, "Does the TPM board have the required content?", machine));
            questions.Add(new($"tpm_{Slug(machine)}_current", section, "Is the TPM board up to date?", machine));
            questions.Add(new($"tpm_{Slug(machine)}_filled", section, "Is the TPM board being filled out?", machine));
            questions.Add(new($"tpm_{Slug(machine)}_visual", section, "Does the TPM board meet the visual standard?", machine));
        }
        return questions;
    }

    static List<HodAuditQuestion> PartsIdNcQuestions() =>
    [
        new("pid_1", "Part Identification", "Is every live product on the floor clearly labelled and identified?"),
        new("pid_2", "Part Identification", "Are all bins, WIP and stock in the area correctly identified — not just the part on the machine?"),
        new("nc_1", "Non-Conformance", "Is all non-conforming product in its designated NC location?"),
        new("nc_2", "Non-Conformance", "Does every red card explain what the part is and what is wrong with it (not blank)?"),
        new("nc_3", "Non-Conformance", "Are NC parts clearly segregated from good product with no mixing?"),
    ];

    static List<HodAuditQuestion> QualityQuestions() =>
    [
        new("qual_1", "Tools & Calibration", "Are the tools being used calibrated and within their calibration date?"),
        new("qual_2", "Documentation", "Is quality documentation being filled out as required?"),
        new("qual_3", "Documentation", "Is quality documentation clean, in order and stored correctly (not scattered)?"),
        new("qual_4", "Documentation", "Can you trust the quality records are complete based on how they are maintained?"),
    ];

    static string Slug(string name) =>
        string.Concat(name.ToLowerInvariant().Select(c =>
            char.IsLetterOrDigit(c) ? c : '_')).Trim('_');
}
