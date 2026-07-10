using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TL.Data;
using TL.Models;

namespace TL.Services;

public class PersonListService
{
    private const string TeamLeaderCacheKey = "picker-names-tl";
    private const string HodCacheKey = "picker-names-hod";
    private const string SeniorCacheKey = "picker-names-senior";

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

    public IReadOnlyList<string> Seniors =>
        _cache.Get<IReadOnlyList<string>>(SeniorCacheKey) ?? SeniorManagementList.Names;

    public async Task EnsureLoadedAsync()
    {
        await SyncSeniorRosterFromDefaultsAsync();

        if (_cache.TryGetValue(TeamLeaderCacheKey, out _))
        {
            await RefreshSeniorCacheAsync();
            return;
        }

        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        try
        {
            if (!await _db.PickerPersons.AnyAsync())
                await SeedFromDefaultsAsync();
            else
                await SyncSeniorRosterFromDefaultsAsync();

            var rows = await _db.PickerPersons
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();

            _cache.Set(TeamLeaderCacheKey, RowsForKind(rows, PersonListKinds.TeamLeader));
            _cache.Set(HodCacheKey, RowsForKind(rows, PersonListKinds.Hod));
            _cache.Set(SeniorCacheKey, RowsForKind(rows, PersonListKinds.Senior));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not load picker names from database — using built-in defaults.");
            _cache.Set(TeamLeaderCacheKey, TeamLeaderList.Names);
            _cache.Set(HodCacheKey, HodList.Names);
            _cache.Set(SeniorCacheKey, SeniorManagementList.Names);
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

    public async Task<(IReadOnlyList<PickerPerson> People, bool FromDatabase)> LoadPeopleByKindAsync(string listKind)
    {
        try
        {
            if (!await _db.PickerPersons.AnyAsync())
                await SeedFromDefaultsAsync();

            var people = await _db.PickerPersons
                .Where(p => p.ListKind == listKind)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();

            if (people.Count == 0)
                return (FallbackPeople(listKind, FallbackNames(listKind)), true);

            return (people, true);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not load {Kind} picker people for admin — showing built-in defaults.", listKind);
            await ReloadAsync();
            return (FallbackPeople(listKind, FallbackNames(listKind)), false);
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

    static IReadOnlyList<string> FallbackNames(string kind) => kind switch
    {
        PersonListKinds.Hod => HodList.Names,
        PersonListKinds.Senior => SeniorManagementList.Names,
        _ => TeamLeaderList.Names,
    };

    async Task SeedFromDefaultsAsync()
    {
        var seed = BuildSeedRows(PersonListKinds.TeamLeader, TeamLeaderList.Names)
            .Concat(BuildSeedRows(PersonListKinds.Hod, HodList.Names))
            .Concat(BuildSeedRows(PersonListKinds.Senior, SeniorManagementList.Names))
            .ToList();

        _db.PickerPersons.AddRange(seed);
        await _db.SaveChangesAsync();
    }

    async Task SyncSeniorRosterFromDefaultsAsync()
    {
        try
        {
            var existing = await _db.PickerPersons
                .Where(p => p.ListKind == PersonListKinds.Senior)
                .ToListAsync();
            var byName = existing.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            var changed = false;

            for (var i = 0; i < SeniorManagementList.Names.Length; i++)
            {
                var name = SeniorManagementList.Names[i];
                var order = i + 1;
                if (byName.TryGetValue(name, out var person))
                {
                    if (person.SortOrder != order)
                    {
                        person.SortOrder = order;
                        changed = true;
                    }
                }
                else
                {
                    _db.PickerPersons.Add(new PickerPerson
                    {
                        ListKind = PersonListKinds.Senior,
                        Name = name,
                        SortOrder = order,
                    });
                    changed = true;
                }
            }

            if (changed)
            {
                await _db.SaveChangesAsync();
                _log.LogInformation("Synced senior rota to {Count} names.", SeniorManagementList.Names.Length);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not sync senior rota names to database.");
        }
    }

    async Task RefreshSeniorCacheAsync()
    {
        try
        {
            var names = await _db.PickerPersons
                .Where(p => p.ListKind == PersonListKinds.Senior)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .Select(p => p.Name)
                .ToListAsync();
            if (names.Count > 0)
                _cache.Set(SeniorCacheKey, names);
        }
        catch
        {
            _cache.Set(SeniorCacheKey, SeniorManagementList.Names);
        }
    }

    static IEnumerable<PickerPerson> BuildSeedRows(string kind, IReadOnlyList<string> names) =>
        names.Select((name, i) => new PickerPerson
        {
            ListKind = kind,
            Name = name,
            SortOrder = i + 1,
        });

    static IReadOnlyList<string> RowsForKind(List<PickerPerson> rows, string kind) =>
        rows.Where(p => p.ListKind == kind).Select(p => p.Name).ToList();
}
