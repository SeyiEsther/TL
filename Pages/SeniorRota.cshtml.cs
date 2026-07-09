using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;

namespace TL.Pages;

public class SeniorRotaModel : PageModel
{
    public int Year { get; set; }
    public List<SeniorRota.RotaWeek> Weeks { get; set; } = new();

    public void OnGet()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        Year = today.Year;
        Weeks = SeniorRota.RemainingWeeksThisYear(today);
    }
}
