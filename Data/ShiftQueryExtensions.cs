using TL.Models;

namespace TL.Data;

public static class ShiftQueryExtensions
{
    public const string AuditPseudoShift = "Audit";

    public static IQueryable<ShiftSubmission> ExcludeAudits(this IQueryable<ShiftSubmission> query) =>
        query.Where(s => s.Shift != AuditPseudoShift);
}
