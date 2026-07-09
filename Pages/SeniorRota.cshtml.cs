using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;

namespace TL.Pages;

public class SeniorRotaModel : PageModel
{
    public int Year { get; set; }
    public SeniorRota.RotaWeek? ThisWeek { get; set; }
    public List<IGrouping<string, SeniorRota.RotaWeek>> Months { get; set; } = new();

    public void OnGet()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        Year = today.Year;

        var weeks = SeniorRota.WeeksThisYear(today).Where(w => !w.IsPast).ToList();
        ThisWeek = weeks.FirstOrDefault(w => w.IsCurrent);
        Months = weeks.GroupBy(w => w.WeekStart.ToString("MMMM yyyy")).ToList();
    }
}
