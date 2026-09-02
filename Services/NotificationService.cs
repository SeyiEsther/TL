using TL.Models;

namespace TL.Services;

// Assembles the per-user "outstanding items" shown against the user's name in
// the header: their own unfinished audits (so they resume the existing record
// rather than starting a duplicate) and the actions assigned to them.
public class NotificationService
{
    private readonly HistoryListService _history;
    private readonly ActionService _actions;
    private readonly UserService _users;

    public NotificationService(HistoryListService history, ActionService actions, UserService users)
    {
        _history = history;
        _actions = actions;
        _users = users;
    }

    // A single unfinished audit belonging to the current user, with a resume
    // link that reopens the EXISTING record (never starts a new one).
    public record UnfinishedItem(
        string Kind, string Label, string Area, DateOnly Date,
        string ResumeUrl, int Answered, int Total, DateTime LastActivity);

    public record AssignedActionItem(
        int Id, string Text, string? SourceLabel, DateOnly? DueDate, bool Overdue);

    public record UserNotifications(
        IReadOnlyList<UnfinishedItem> Unfinished,
        IReadOnlyList<AssignedActionItem> Actions)
    {
        public int Count => Unfinished.Count + Actions.Count;
        public bool Any => Count > 0;
    }

    public async Task<UserNotifications> ForCurrentUserAsync()
    {
        var user = _users.GetCurrentUser();
        var name = user.DisplayName;
        if (string.IsNullOrWhiteSpace(name))
            return new UserNotifications([], []);

        // (a) Own unfinished audits. Only HOD daily audits are pre-created and
        // autosaved as signature-less drafts — senior audits only persist on a
        // signed submit, so there is no senior "draft" to resume. Filter the
        // recent draft list down to this user by name.
        var unfinished = new List<UnfinishedItem>();
        try
        {
            var hod = await _history.LoadUnfinishedHodAsync();
            unfinished.AddRange(hod
                .Where(a => PortalNameMatcher.Matches(a.AuditorName, name))
                .Select(a => new UnfinishedItem(
                    "HoD", a.AuditTypeLabel, a.Area, a.AuditDate,
                    $"/Audit?id={a.Id}", a.Answered, a.Total, a.LastActivity)));
        }
        catch { /* never let the header fail over a draft lookup */ }

        // (b) Open actions assigned to this user, overdue first (OpenForUserAsync
        // already orders by due date ascending, nulls last).
        var actions = new List<AssignedActionItem>();
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var open = await _actions.OpenForUserAsync(name);
            actions.AddRange(open.Select(a => new AssignedActionItem(
                a.Id, a.Text, a.SourceLabel ?? ActionSourceTypes.Label(a.SourceType),
                a.DueDate, a.DueDate.HasValue && a.DueDate.Value < today)));
        }
        catch { }

        return new UserNotifications(unfinished, actions);
    }
}
