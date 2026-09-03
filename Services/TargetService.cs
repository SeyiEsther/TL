using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TL.Data;
using TL.Models;

namespace TL.Services;

// Reads and writes the editable production targets. Values are cached briefly
// so every page that shows a target pulls the current admin-set number without
// hammering the database, and a change made by an admin converges everywhere
// within the TTL (mirrors PersonListService).
public class TargetService
{
    private const string CacheKey = "production-targets";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TargetService> _log;

    public TargetService(AppDbContext db, IMemoryCache cache, ILogger<TargetService> log)
    {
        _db = db;
        _cache = cache;
        _log = log;
    }

    public record TargetRow(string Key, string Label, int Value, string? UpdatedBy, DateTime? UpdatedAt);

    // Non-throwing accessors used by display pages. Fall back to the built-in
    // default if the row or the whole table isn't available yet.
    public int Shift => Get(TargetKeys.Shift);
    public int Day => Get(TargetKeys.Day);
    public int Week => Get(TargetKeys.Week);

    public int Get(string key)
    {
        var map = LoadCached();
        if (map.TryGetValue(key, out var v)) return v;
        return TargetKeys.Definitions.TryGetValue(key, out var def) ? def.Default : 0;
    }

    Dictionary<string, int> LoadCached()
    {
        if (_cache.TryGetValue<Dictionary<string, int>>(CacheKey, out var cached) && cached != null)
            return cached;

        var map = TargetKeys.Definitions.ToDictionary(d => d.Key, d => d.Value.Default);
        try
        {
            foreach (var row in _db.TargetSettings.AsNoTracking().ToList())
                map[row.Key] = row.Value;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not load targets — using built-in defaults.");
        }
        _cache.Set(CacheKey, map, CacheTtl);
        return map;
    }

    // Ensures every defined target has a row (seeds defaults on first run). Safe
    // to call on startup; add-only, never overwrites an admin-set value.
    public async Task EnsureSeededAsync()
    {
        try
        {
            var have = await _db.TargetSettings.Select(t => t.Key).ToListAsync();
            var haveSet = new HashSet<string>(have, StringComparer.OrdinalIgnoreCase);
            var added = false;
            foreach (var (key, def) in TargetKeys.Definitions)
            {
                if (haveSet.Contains(key)) continue;
                _db.TargetSettings.Add(new TargetSetting { Key = key, Value = def.Default });
                added = true;
            }
            if (added)
            {
                await _db.SaveChangesAsync();
                _cache.Remove(CacheKey);
                _log.LogInformation("Seeded default production targets.");
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not seed production targets.");
        }
    }

    public async Task<List<TargetRow>> AllAsync()
    {
        await EnsureSeededAsync();
        var rows = await _db.TargetSettings.AsNoTracking().ToListAsync();
        var byKey = rows.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);
        return TargetKeys.Definitions.Select(d =>
        {
            byKey.TryGetValue(d.Key, out var r);
            return new TargetRow(d.Key, d.Value.Label,
                r?.Value ?? d.Value.Default, r?.UpdatedBy, r?.UpdatedAt);
        }).ToList();
    }

    // ---- Daily Report per-row metric targets (admin-set, read-only to SMs) ----

    private const string ReportCacheKey = "report-metric-targets";

    public record ReportTargetRow(string Section, string Label, string? Target, string? UpdatedBy, DateTime? UpdatedAt);

    // Current target text for one metric row, or null if the admin hasn't set one.
    public string? ReportTarget(string section, string label)
        => LoadReportCached().GetValueOrDefault(ReportKey(section, label));

    static string ReportKey(string section, string label) => section + "" + label;

    Dictionary<string, string?> LoadReportCached()
    {
        if (_cache.TryGetValue<Dictionary<string, string?>>(ReportCacheKey, out var cached) && cached != null)
            return cached;

        var map = new Dictionary<string, string?>();
        try
        {
            foreach (var r in _db.ReportMetricTargets.AsNoTracking().ToList())
                map[ReportKey(r.Section, r.Label)] = r.Target;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not load Daily Report targets.");
        }
        _cache.Set(ReportCacheKey, map, CacheTtl);
        return map;
    }

    // Every targetable row (from the fixed defs) with its saved value, for the
    // admin editor — grouped in section/label order.
    public async Task<List<ReportTargetRow>> AllReportTargetsAsync()
    {
        var saved = new Dictionary<string, ReportMetricTarget>();
        try
        {
            foreach (var r in await _db.ReportMetricTargets.AsNoTracking().ToListAsync())
                saved[ReportKey(r.Section, r.Label)] = r;
        }
        catch (Exception ex) { _log.LogWarning(ex, "Could not load Daily Report targets."); }

        var rows = new List<ReportTargetRow>();
        foreach (var (section, labels) in ShiftReportDefs.TargetableSections)
            foreach (var label in labels)
            {
                saved.TryGetValue(ReportKey(section, label), out var r);
                rows.Add(new ReportTargetRow(section, label, r?.Target, r?.UpdatedBy, r?.UpdatedAt));
            }
        return rows;
    }

    public async Task<bool> UpdateReportTargetAsync(string section, string label, string? value, string byName)
    {
        // Only accept rows that are part of a targetable section.
        var known = ShiftReportDefs.TargetableSections
            .Any(s => s.Section == section && s.Labels.Contains(label));
        if (!known) return false;

        var row = await _db.ReportMetricTargets
            .FirstOrDefaultAsync(t => t.Section == section && t.Label == label);
        if (row == null)
        {
            row = new ReportMetricTarget { Section = section, Label = label };
            _db.ReportMetricTargets.Add(row);
        }
        row.Target = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        row.UpdatedBy = string.IsNullOrWhiteSpace(byName) ? "Unknown" : byName;
        row.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _cache.Remove(ReportCacheKey);
        return true;
    }

    // Confirmed-write: persists then returns true only after SaveChanges succeeds.
    // Records who changed the value and when.
    public async Task<bool> UpdateAsync(string key, int value, string byName)
    {
        if (!TargetKeys.Definitions.ContainsKey(key) || value < 0) return false;

        var row = await _db.TargetSettings.FirstOrDefaultAsync(t => t.Key == key);
        if (row == null)
        {
            row = new TargetSetting { Key = key };
            _db.TargetSettings.Add(row);
        }
        row.Value = value;
        row.UpdatedBy = string.IsNullOrWhiteSpace(byName) ? "Unknown" : byName;
        row.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
        return true;
    }
}
