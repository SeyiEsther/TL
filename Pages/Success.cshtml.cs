using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TL.Pages;

public class SuccessModel : PageModel
{
    public int SubmissionId { get; set; }

    public void OnGet(int id)
    {
        SubmissionId = id;
    }
}
