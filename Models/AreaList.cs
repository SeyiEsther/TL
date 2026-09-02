namespace TL.Models;

public static class AreaList
{
    public static readonly (string Group, string Label, string Machines)[] All =
    [
        ("Assembly", "1 — HP",                    ""),
        ("Assembly", "2 — OZEKI",                 ""),
        ("Assembly", "3 — META",                  ""),
        ("Assembly", "4 — SELL",                  ""),
        ("Assembly", "5 — MICROSOFT",             ""),
        ("Assembly", "6 — DELL GAINS / RAINWATER",""),
        ("Assembly", "7 — MOR",                   ""),
        ("Assembly", "8 — SPECIALS",              ""),
        ("Assembly", "9 — TX",                    ""),
        ("Assembly", "10 — E40001",               ""),
        ("Assembly", "11 — FLAT PARTS",           ""),
        ("Assembly", "12 — ACCESSORIES",          ""),
        ("Paint", "13 — Black Line",  ""),
        ("Paint", "14 — White Line",  ""),
        ("Paint", "15 — Back Booth",  ""),
        ("Paint", "16 — Re-Work",     ""),
        ("Phase 1 Weld", "Zone 7",  "Mainframe 2, Microsoft"),
        ("Phase 1 Weld", "Zone 8",  "HP Weld"),
        ("Phase 1 Weld", "Zone 9",  "German Jig 1"),
        ("Phase 1 Weld", "Zone 10", "German Jig 2"),
        ("Phase 1 Weld", "Zone 11", "Panasonic V3 & Nvidia"),
        ("Phase 1 Weld", "Zone 12", "MGX Galv Weld, Google Small Part Galv Weld"),
        ("Phase 1 Weld", "Zone 13", "Microsoft Tops and Bottoms"),
        ("Phase 1 Weld", "Zone 14", "R-cell 22/23/24 Flexi Cell"),
        ("Phase 1 Weld", "Zone 15", "Small Parts Welding"),
        ("Phase 3 Pierce and Fold", "Zone 1",  "Trumpf Laser, Trubend F35"),
        ("Phase 3 Pierce and Fold", "Zone 2",  "T500, T5000, T5000–S10, T5000–S11, T7000"),
        ("Phase 3 Pierce and Fold", "Zone 3",  "T5000–S13, T5000–S14"),
        ("Phase 3 Pierce and Fold", "Zone 16", "100T Xact, Training Bay, 250T"),
        ("Phase 3 Pierce and Fold", "Zone 17", "Trubend F32, F33, F34"),
        ("Phase 3 Pierce and Fold", "Zone 20", "Robot Folding Cell F28, Bystronic F29, F30, F31, F36"),
        ("Phase 3 Pierce and Fold", "Zone 22", "F37, EP3"),
        ("Phase 3 Pierce and Fold", "Zone 4",  "Heilbronn"),
        ("Phase 3 Pierce and Fold", "Zone 5",  "Salv 1"),
        ("Phase 3 Pierce and Fold", "Zone 6",  "Salv 3"),
        ("Phase 3 Pierce and Fold", "Zone 23", "Galv Grindbay"),
        ("Phase 3 Pierce and Fold", "Zone 18", "Grind Master PH1"),
        ("Phase 3 Pierce and Fold", "Zone 19", "Automatic TX Stud Weld Cell"),
        ("Phase 3 Pierce and Fold", "Zone 21", "Sciaky Spot Weld, Spotwelder E11/2 (Serial 770066)"),
        ("Dispatch", "Loading Bays",    ""),
        ("Dispatch", "Mez Floor Above", ""),
        ("Dispatch", "Mez Floor Below", ""),
        ("Stores", "DP1", ""),
        ("Stores", "DP3", ""),
    ];

    public static string GetMachines(string? label) =>
        All.FirstOrDefault(a => a.Label == label).Machines ?? "";

    public static string GetDepartment(string? label) =>
        All.FirstOrDefault(a => a.Label == label).Group ?? "";

    public static IReadOnlyList<string> Departments =>
        All.Select(a => a.Group).Distinct().ToList();

    public static IReadOnlyList<string> GetLabelsForDepartment(string? department) =>
        string.IsNullOrWhiteSpace(department)
            ? []
            : All.Where(a => a.Group == department).Select(a => a.Label).ToList();

    public static bool IsInDepartment(string? label, string? department) =>
        !string.IsNullOrWhiteSpace(label)
        && !string.IsNullOrWhiteSpace(department)
        && GetDepartment(label) == department;

    // The two sheetmetal groups. Sheetmetal runs a 2-hourly check cadence
    // (4 checks per shift) rather than the hourly cadence used elsewhere.
    public static readonly string[] SheetmetalGroups =
    [
        "Phase 1 Weld",
        "Phase 3 Pierce and Fold",
    ];

    public static bool IsSheetmetal(string? label) =>
        SheetmetalGroups.Contains(GetDepartment(label));

    // Number of checks a shift in this area records.
    public const int SheetmetalChecks = 4;
    public static int DefaultChecksFor(string? label) =>
        IsSheetmetal(label) ? SheetmetalChecks : 8;

    // TPM-only board groupings for Assembly. Several assembly lines share one TPM
    // board, so on a TPM audit they are collapsed into a single board answered
    // once. This applies ONLY to TPM question generation — every other audit type
    // and the TL daily form keep the individual lines.
    public static readonly (string Board, string[] Members)[] TpmLineGroups =
    [
        ("MSFT / E4000 / MOR", ["5 — MICROSOFT", "10 — E40001", "7 — MOR"]),
        ("HP / Ozeki / SPC / TX", ["1 — HP", "2 — OZEKI", "8 — SPECIALS", "9 — TX"]),
    ];

    // The TPM board a line belongs to, or null if it isn't in a group.
    public static string? TpmBoardFor(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        foreach (var g in TpmLineGroups)
            if (g.Members.Contains(label, StringComparer.OrdinalIgnoreCase))
                return g.Board;
        return null;
    }

    // The member line labels for a TPM board name (empty if not a board).
    public static IReadOnlyList<string> TpmBoardMembers(string? board)
    {
        foreach (var g in TpmLineGroups)
            if (string.Equals(g.Board, board, StringComparison.OrdinalIgnoreCase))
                return g.Members;
        return [];
    }

    public static bool IsTpmBoard(string? label) =>
        !string.IsNullOrWhiteSpace(label) && TpmLineGroups.Any(g => string.Equals(g.Board, label, StringComparison.OrdinalIgnoreCase));

    // For TPM audits, a grouped line is stored/matched as its shared board so
    // everyone lands on one record. Every other audit type keeps the raw area.
    public static string CanonicalAreaForAudit(string? auditType, string label) =>
        auditType == HodAuditTypes.Tpm ? (TpmBoardFor(label) ?? label) : label;

    public static List<string> GetMachineList(string? label)
    {
        var machines = GetMachines(label);
        if (string.IsNullOrWhiteSpace(machines))
            return [];
        return machines.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
