using Microsoft.AspNetCore.Mvc.RazorPages;
using TL.Models;
using TL.Services;

namespace TL.Pages;

// A user's personal action worklist. Reachable by ANY authenticated user
// (owners may be TLs), so it is not gated to HOD/Senior/Management.
public class MyActionsModel : PageModel
{
    private readonly ActionService _actions;
    private readonly UserService _users;

    public MyActionsModel(ActionService actions, UserService users)
    {
        _actions = actions;
        _users = users;
    }

    public string DisplayName { get; set; } = "";
    public List<AuditAction> AssignedOpen { get; set; } = [];
    public List<AuditAction> Raised { get; set; } = [];

    public async Task OnGetAsync()
    {
        var user = _users.GetCurrentUser();
        DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
        AssignedOpen = await _actions.OpenForUserAsync(user.DisplayName);
        Raised = await _actions.RaisedByUserAsync(user.Username, user.DisplayName);
    }
}
