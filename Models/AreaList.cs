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
        ("Sheet Metal", "Zone 1",  "Trumpf Laser, Trubend F35"),
        ("Sheet Metal", "Zone 2",  "T500, T5000, T5000–S10, T5000–S11, T7000"),
        ("Sheet Metal", "Zone 3",  "T5000–S13, T5000–S14"),
        ("Sheet Metal", "Zone 7",  "Mainframe 2, Microsoft"),
        ("Sheet Metal", "Zone 8",  "HP Weld"),
        ("Sheet Metal", "Zone 9",  "German Jig 1"),
        ("Sheet Metal", "Zone 10", "German Jig 2"),
        ("Sheet Metal", "Zone 11", "Panasonic V3 & Nvidia"),
        ("Sheet Metal", "Zone 12", "MGX Galv Weld, Google Small Part Galv Weld"),
        ("Sheet Metal", "Zone 13", "Microsoft Tops and Bottoms"),
        ("Sheet Metal", "Zone 14", "R-cell 22/23/24 Flexi Cell"),
        ("Sheet Metal", "Zone 15", "Small Parts Welding"),
        ("Sheet Metal", "Zone 16", "100T Xact, Training Bay, 250T"),
        ("Sheet Metal", "Zone 17", "Trubend F32, F33, F34"),
        ("Sheet Metal", "Zone 20", "Robot Folding Cell F28, Bystronic F29, F30, F31, F36"),
        ("Sheet Metal", "Zone 22", "F37, EP3"),
        ("Phase 1 Sheetmetal", "Zone 4",  "Heilbronn"),
        ("Phase 1 Sheetmetal", "Zone 5",  "Salv 1"),
        ("Phase 1 Sheetmetal", "Zone 6",  "Salv 3"),
        ("Phase 1 Sheetmetal", "Zone 23", "Galv Grindbay"),
        ("Phase 3 Sheetmetal", "Zone 18", "Grind Master PH1"),
        ("Phase 3 Sheetmetal", "Zone 19", "Automatic TX Stud Weld Cell"),
        ("Phase 3 Sheetmetal", "Zone 21", "Sciaky Spot Weld, Spotwelder E11/2 (Serial 770066)"),
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

    public static List<string> GetMachineList(string? label)
    {
        var machines = GetMachines(label);
        if (string.IsNullOrWhiteSpace(machines))
            return [];
        return machines.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
