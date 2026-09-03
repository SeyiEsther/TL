using System.Text.Json;
using TL.Models;

namespace TL.Services;

public static class ShiftReportSerializer
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static string Metrics(IEnumerable<ShiftMetricRow> rows) => JsonSerializer.Serialize(rows, Opts);
    public static string Audits(IEnumerable<ShiftAuditRow> rows) => JsonSerializer.Serialize(rows, Opts);

    public static List<ShiftMetricRow> Metrics(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<ShiftMetricRow>>(json, Opts) ?? []; }
        catch { return []; }
    }

    public static List<ShiftAuditRow> Audits(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<ShiftAuditRow>>(json, Opts) ?? []; }
        catch { return []; }
    }

    // Build the fixed row sets, filling values from a saved report if present.
    // HSE / Quality / Morale all draw from the same metrics JSON (matched by
    // label), so the section split is display-only and old data still resolves.
    public static List<ShiftMetricRow> HseRows(string? savedJson)
        => Fill(ShiftReportDefs.HseRows, savedJson);

    public static List<ShiftMetricRow> QualityRows(string? savedJson)
        => Fill(ShiftReportDefs.QualityRows, savedJson);

    public static List<ShiftMetricRow> MoraleRows(string? savedJson)
        => Fill(ShiftReportDefs.MoraleRows, savedJson);

    public static List<ShiftMetricRow> ProductionRows(string? savedJson)
        => Fill(ShiftReportDefs.ProductionRows, savedJson);

    static List<ShiftMetricRow> Fill(string[] labels, string? savedJson)
    {
        var saved = Metrics(savedJson).ToDictionary(r => r.Label, r => r, StringComparer.OrdinalIgnoreCase);
        return labels.Select(l => saved.TryGetValue(l, out var r) ? r : new ShiftMetricRow(l, null, null, null, null)).ToList();
    }

    public static List<ShiftAuditRow> AuditRows(string? savedJson)
    {
        var saved = Audits(savedJson).ToDictionary(r => r.Type, r => r, StringComparer.OrdinalIgnoreCase);
        return ShiftReportDefs.AuditRows
            .Select(a => saved.TryGetValue(a.Type, out var r) ? r : new ShiftAuditRow(a.Type, a.Day, null))
            .ToList();
    }
}
