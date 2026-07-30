using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TL.Data;
using TL.Models;

namespace TL.Services;

// Provides the controlled-document number for each form type (shown in PDF
// footers) and lets an admin maintain them. Values are cached; reads never
// throw so a PDF still renders if the lookup fails.
public class DocumentNumberService
{
    private const string CacheKey = "document-numbers";
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DocumentNumberService> _log;

    public DocumentNumberService(AppDbContext db, IMemoryCache cache, ILogger<DocumentNumberService> log)
    {
        _db = db;
        _cache = cache;
        _log = log;
    }

    // Seed any missing form types (idempotent) and warm the cache.
    public async Task EnsureSeededAsync()
    {
        try
        {
            var existing = await _db.DocumentNumbers.Select(d => d.FormType).ToListAsync();
            var missing = DocumentFormTypes.Defaults
                .Where(d => !existing.Contains(d.Type))
                .Select(d => new DocumentNumber { FormType = d.Type, Label = d.Label, Number = d.Number, SortOrder = d.Order })
                .ToList();
            if (missing.Count > 0)
            {
                _db.DocumentNumbers.AddRange(missing);
                await _db.SaveChangesAsync();
                _log.LogInformation("Seeded {Count} document numbers.", missing.Count);
            }
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not seed document numbers — using built-in defaults.");
        }
    }

    async Task ReloadAsync()
    {
        var rows = await _db.DocumentNumbers.ToListAsync();
        _cache.Set(CacheKey, rows.ToDictionary(r => r.FormType, r => r.Number ?? ""));
    }

    // The number for a form type, or "" if unknown. Never throws.
    public string Get(string formType)
    {
        var map = _cache.Get<Dictionary<string, string>>(CacheKey);
        if (map == null)
        {
            var def = DocumentFormTypes.Defaults.FirstOrDefault(d => d.Type == formType);
            return def.Number ?? "";
        }
        return map.TryGetValue(formType, out var n) ? n : "";
    }

    public async Task<List<DocumentNumber>> AllAsync()
    {
        try
        {
            var rows = await _db.DocumentNumbers.OrderBy(d => d.SortOrder).ThenBy(d => d.Label).ToListAsync();
            if (rows.Count == 0)
                return DocumentFormTypes.Defaults
                    .Select(d => new DocumentNumber { FormType = d.Type, Label = d.Label, Number = d.Number, SortOrder = d.Order })
                    .ToList();
            return rows;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not load document numbers.");
            return [];
        }
    }

    public async Task<bool> UpdateAsync(int id, string? number)
    {
        var row = await _db.DocumentNumbers.FindAsync(id);
        if (row == null) return false;
        row.Number = (number ?? "").Trim();
        await _db.SaveChangesAsync();
        await ReloadAsync();
        return true;
    }
}
