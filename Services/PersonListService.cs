using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TL.Data;
using TL.Models;

namespace TL.Services;

public class PersonListService
{
    private const string TeamLeaderCacheKey = "picker-names-tl";
    private const string HodCacheKey = "picker-names-hod";

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PersonListService> _log;

    public PersonListService(AppDbContext db, IMemoryCache cache, ILogger<PersonListService> log)
    {
        _db = db;
        _cache = cache;
        _log = log;
    }

    public IReadOnlyList<string> TeamLeaders =>
        _cache.Get<IReadOnlyList<string>>(TeamLeaderCacheKey) ?? TeamLeaderList.Names;

    public IReadOnlyList<string> Hods =>
        _cache.Get<IReadOnlyList<string>>(HodCacheKey) ?? HodList.Names;

    public async Task EnsureLoadedAsync()
    {
        if (_cache.TryGetValue(TeamLeaderCacheKey, out _))
            return;
        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        try
        {
            if (!await _db.PickerPersons.AnyAsync())
                await SeedFromDefaultsAsync();

            var rows = await _db.PickerPersons
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();

            _cache.Set(TeamLeaderCacheKey, RowsForKind(rows, PersonListKinds.TeamLeader));
            _cache.Set(HodCacheKey, RowsForKind(rows, PersonListKinds.Hod));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not load picker names from database — using built-in defaults.");
            _cache.Set(TeamLeaderCacheKey, TeamLeaderList.Names);
            _cache.Set(HodCacheKey, HodList.Names);
        }
    }

    public async Task<bool> AddPersonAsync(string listKind, string name)
    {
        var trimmed = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return false;

        await EnsureLoadedAsync();

        var exists = await _db.PickerPersons.AnyAsync(p =>
            p.ListKind == listKind &&
            p.Name.ToLower() == trimmed.ToLower());
        if (exists) return false;

        var maxOrder = await _db.PickerPersons
            .Where(p => p.ListKind == listKind)
            .Select(p => (int?)p.SortOrder)
            .MaxAsync() ?? 0;

        _db.PickerPersons.Add(new PickerPerson
        {
            ListKind = listKind,
            Name = trimmed,
            SortOrder = maxOrder + 1,
        });
        await _db.SaveChangesAsync();
        await ReloadAsync();
        return true;
    }

    public async Task<bool> RemovePersonAsync(int id)
    {
        var person = await _db.PickerPersons.FindAsync(id);
        if (person == null) return false;
        _db.PickerPersons.Remove(person);
        await _db.SaveChangesAsync();
        await ReloadAsync();
        return true;
    }

    public async Task<(IReadOnlyList<PickerPerson> TeamLeaders, IReadOnlyList<PickerPerson> Hods, bool FromDatabase)>
        LoadPickerPeopleAsync()
    {
        try
        {
            if (!await _db.PickerPersons.AnyAsync())
                await SeedFromDefaultsAsync();

            var people = await _db.PickerPersons
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return (
                people.Where(p => p.ListKind == PersonListKinds.TeamLeader).ToList(),
                people.Where(p => p.ListKind == PersonListKinds.Hod).ToList(),
                true);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not load picker people for admin — showing built-in defaults.");
            await ReloadAsync();
            return (FallbackPeople(PersonListKinds.TeamLeader, TeamLeaders),
                FallbackPeople(PersonListKinds.Hod, Hods),
                false);
        }
    }

    static List<PickerPerson> FallbackPeople(string kind, IReadOnlyList<string> names) =>
        names.Select((name, i) => new PickerPerson
        {
            Id = -(i + 1),
            ListKind = kind,
            Name = name,
            SortOrder = i + 1,
        }).ToList();

    async Task SeedFromDefaultsAsync()
    {
        var seed = TeamLeaderList.Names
            .Select((name, i) => new PickerPerson
            {
                ListKind = PersonListKinds.TeamLeader,
                Name = name,
                SortOrder = i + 1,
            })
            .Concat(HodList.Names.Select((name, i) => new PickerPerson
            {
                ListKind = PersonListKinds.Hod,
                Name = name,
                SortOrder = i + 1,
            }))
            .ToList();

        _db.PickerPersons.AddRange(seed);
        await _db.SaveChangesAsync();
    }

    static IReadOnlyList<string> RowsForKind(List<PickerPerson> rows, string kind) =>
        rows.Where(p => p.ListKind == kind).Select(p => p.Name).ToList();
}
