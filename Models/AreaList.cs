namespace TL.Models;

public static class AreaList
{
    public static readonly (string Group, string Label)[] All =
    [
        // ── Assembly (unchanged) ──────────────────────────────────────────────
        ("Assembly", "1 — HP"),
        ("Assembly", "2 — OZEKI"),
        ("Assembly", "3 — META"),
        ("Assembly", "4 — SELL"),
        ("Assembly", "5 — MICROSOFT"),
        ("Assembly", "6 — DELL GAINS / RAINWATER"),
        ("Assembly", "7 — MOR"),
        ("Assembly", "8 — SPECIALS"),
        ("Assembly", "9 — TX"),
        ("Assembly", "10 — E40001"),
        ("Assembly", "11 — FLAT PARTS"),
        ("Assembly", "12 — ACCESSORIES"),

        // ── Paint (unchanged) ────────────────────────────────────────────────
        ("Paint", "13 — Black Line"),
        ("Paint", "14 — White Line"),
        ("Paint", "15 — Back Booth"),
        ("Paint", "16 — Re-Work"),

        // ── Sheet Metal (rebuilt from zone sheet) ────────────────────────────
        ("Sheet Metal", "Zone 1 — Trumpf Laser"),
        ("Sheet Metal", "Zone 1 — Trubend F35"),
        ("Sheet Metal", "Zone 2 — T500"),
        ("Sheet Metal", "Zone 2 — T5000"),
        ("Sheet Metal", "Zone 2 — T5000 – S10"),
        ("Sheet Metal", "Zone 2 — T5000 – S11"),
        ("Sheet Metal", "Zone 2 — T7000"),
        ("Sheet Metal", "Zone 3 — T5000 – S13"),
        ("Sheet Metal", "Zone 3 — T5000 – S14"),
        ("Sheet Metal", "Zone 4 — Heilbronn"),
        ("Sheet Metal", "Zone 5 — Salv 1"),
        ("Sheet Metal", "Zone 6 — Salv 3"),
        ("Sheet Metal", "Zone 7 — Mainframe 2"),
        ("Sheet Metal", "Zone 7 — Microsoft"),
        ("Sheet Metal", "Zone 8 — HP Weld"),
        ("Sheet Metal", "Zone 9 — German Jig 1"),
        ("Sheet Metal", "Zone 10 — German Jig 2"),
        ("Sheet Metal", "Zone 11 — Panasonic V3 & Nvidia"),
        ("Sheet Metal", "Zone 12 — MGX Galv Weld"),
        ("Sheet Metal", "Zone 12 — Google Small Part Galv Weld"),
        ("Sheet Metal", "Zone 13 — Microsoft Tops and Bottoms"),
        ("Sheet Metal", "Zone 14 — R-cell 22/23/24 Flexi Cell"),
        ("Sheet Metal", "Zone 15 — Small Parts Welding"),
        ("Sheet Metal", "Zone 16 — 100T Xact"),
        ("Sheet Metal", "Zone 16 — Training Bay"),
        ("Sheet Metal", "Zone 16 — 250T"),
        ("Sheet Metal", "Zone 17 — Trubend F32"),
        ("Sheet Metal", "Zone 17 — Trubend F33"),
        ("Sheet Metal", "Zone 17 — Trubend F34"),
        ("Sheet Metal", "Zone 18 — Grind Master PH1"),
        ("Sheet Metal", "Zone 19 — Automatic TX Stud Weld Cell"),
        ("Sheet Metal", "Zone 20 — Robot Folding Cell 200T – F28"),
        ("Sheet Metal", "Zone 20 — Bystronic 200T – F29"),
        ("Sheet Metal", "Zone 20 — Bystronic 200T Machine 3 – F30"),
        ("Sheet Metal", "Zone 20 — Bystronic 200T 4 Robot – F31"),
        ("Sheet Metal", "Zone 20 — F36"),
        ("Sheet Metal", "Zone 21 — Skiaky Spot Weld"),
        ("Sheet Metal", "Zone 22 — F37"),
        ("Sheet Metal", "Zone 22 — EP3"),

        // ── Dispatch ─────────────────────────────────────────────────────────
        ("Dispatch", "Loading Bays"),
        ("Dispatch", "Mez Floor Above"),
        ("Dispatch", "Mez Floor Below"),

        // ── Stores ────────────────────────────────────────────────────────────
        ("Stores", "DP1"),
        ("Stores", "DP3"),
    ];
}
