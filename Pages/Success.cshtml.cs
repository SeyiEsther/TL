using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TL.Pages;

public class SuccessModel : PageModel
{
    public int SubmissionId { get; set; }
    public bool IsAudit { get; set; }

    public void OnGet(int id, bool? audit)
    {
        SubmissionId = id;
        IsAudit = audit == true;
    }
}
