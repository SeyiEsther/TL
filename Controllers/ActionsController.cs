using Microsoft.AspNetCore.Mvc;
using TL.Models;
using TL.Services;

namespace TL.Controllers;

[ApiController]
[Route("api/actions")]
public class ActionsController : ControllerBase
{
    private readonly ActionService _actions;
    private readonly UserService _users;
    private readonly PortalAccessService _access;
    private readonly AdminService _admin;

    public ActionsController(ActionService actions, UserService users, PortalAccessService access, AdminService admin)
    {
        _actions = actions;
        _users = users;
        _access = access;
        _admin = admin;
    }

    public record CompleteRequest(string? Note);
    public record ReassignRequest(string? Owner);

    // Mark an action complete with a mandatory note. Allowed for the owner, or
    // anyone with management access (completing a shared action on behalf).
    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, [FromBody] CompleteRequest req)
    {
        var user = _users.GetCurrentUser();
        var action = await _actions.FindAsync(id);
        if (action == null) return NotFound();

        var isOwner = PortalNameMatcher.Matches(action.OwnerName, user.DisplayName);
        if (!isOwner && !_access.CanAccessManagement())
            return Forbid();

        var by = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
        var (ok, error) = await _actions.CompleteAsync(id, by, req?.Note);
        if (!ok) return BadRequest(new { error });

        return Ok(new { ok = true, openCount = await OpenCountAsync(user) });
    }

    // Reassign to a different owner — management or admin only.
    [HttpPost("{id:int}/reassign")]
    public async Task<IActionResult> Reassign(int id, [FromBody] ReassignRequest req)
    {
        if (!_access.CanAccessManagement() && !_admin.IsAdmin())
            return Forbid();
        if (string.IsNullOrWhiteSpace(req?.Owner))
            return BadRequest(new { error = "Please choose an owner to reassign to." });

        var user = _users.GetCurrentUser();
        var by = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
        var ok = await _actions.ReassignAsync(id, req.Owner, by);
        if (!ok) return NotFound();
        return Ok(new { ok = true });
    }

    // Reopen a completed action — admin only.
    [HttpPost("{id:int}/reopen")]
    public async Task<IActionResult> Reopen(int id)
    {
        if (!_admin.IsAdmin())
            return Forbid();
        var user = _users.GetCurrentUser();
        var by = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
        var ok = await _actions.ReopenAsync(id, by);
        if (!ok) return NotFound();
        return Ok(new { ok = true });
    }

    Task<int> OpenCountAsync(AppUser user) =>
        _actions.OpenCountForUserAsync(user.DisplayName);
}
