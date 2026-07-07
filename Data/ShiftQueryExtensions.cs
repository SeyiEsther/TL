using TL.Models;

namespace TL.Data;

public static class ShiftQueryExtensions
{
    // Senior-management walkaround audits are persisted as ShiftSubmissions with
    // Shift == "Audit" (see Audit.cshtml.cs). They are not real production shifts,
    // so they must be excluded from shift dashboards, history, handover tracking
    // and exports — otherwise they distort KPIs and can masquerade as pending
    // handovers. Keep this in one place so every shift-centric query stays consistent.
    public const string AuditPseudoShift = "Audit";

    public static IQueryable<ShiftSubmission> ExcludeAudits(this IQueryable<ShiftSubmission> query) =>
        query.Where(s => s.Shift != AuditPseudoShift);
}
