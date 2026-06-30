using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TL.Pages;

public class AuditSuccessModel : PageModel
{
    public int AuditId { get; set; }
    public void OnGet(int id) => AuditId = id;
}
