using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;
using TL.Services;

namespace TL.Pages;

public class SeniorRotaModel : PageModel
{
    private readonly PersonListService _people;

    public SeniorRotaModel(PersonListService people) => _people = people;

    public int Year { get; set; }
    public SeniorRota.RotaWeek? ThisWeek { get; set; }
    public List<IGrouping<string, SeniorRota.RotaWeek>> Months { get; set; } = new();
    public IReadOnlyList<string> SeniorNames { get; set; } = SeniorManagementList.Names;
    public IReadOnlyList<string> Group1Names { get; set; } = [];
    public IReadOnlyList<string> DirectorNames { get; set; } = [];
    public string[][] YearTeams { get; set; } = [];

    public async Task OnGetAsync()
    {
        await _people.EnsureLoadedAsync();
        SeniorNames = _people.Seniors;
        Group1Names = SeniorNames.Take(SeniorManagementList.Group1Count).ToList();
        DirectorNames = SeniorNames.Skip(SeniorManagementList.Group1Count).ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        Year = today.Year;
        YearTeams = SeniorRota.TeamsForYear(Year, SeniorNames).ToArray();

        ThisWeek = SeniorRota.CurrentWeek(today, SeniorNames);
        var weeks = SeniorRota.WeeksThisYear(today, SeniorNames).Where(w => !w.IsPast).ToList();
        Months = weeks.GroupBy(w => w.WeekStart.ToString("MMMM yyyy")).ToList();
    }
}
