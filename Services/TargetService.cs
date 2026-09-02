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
