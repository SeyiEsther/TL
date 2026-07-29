using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TL.Pages;

// Read-only reference page. The entire schedule is computed client-side from the
// cycle start date — no model state, no database, no persistence.
public class MeetingRotaModel : PageModel
{
    public void OnGet() { }
}
