using TL.Models;

namespace TL.Services;

public class ShiftCompletionService
{
    public ShiftCompletionResult Evaluate(ShiftSubmission sub)
    {
        var hours = sub.Hours.OrderBy(h => h.HourNumber).ToList();
        var result = new ShiftCompletionResult
        {
            HoursTotal = sub.HoursCompleted,
            HoursComplete = 0,
        };

        if (hours.Count == 0)
        {
            result.MissingItems.Add("No hourly checks recorded");
            return result;
        }

        for (int i = 0; i < sub.HoursCompleted; i++)
        {
            var hourNum = i + 1;
            var h = hours.FirstOrDefault(x => x.HourNumber == hourNum);
            if (h == null)
            {
                result.MissingItems.Add($"Hour {hourNum}: not started");
                continue;
            }

            var hourMissing = GetHourMissingFields(h);
            if (hourMissing.Count == 0)
                result.HoursComplete++;
            else
                result.MissingItems.AddRange(hourMissing.Select(f => $"Hour {hourNum}: {f}"));
        }

        var endHour = hours.FirstOrDefault(h => h.HourNumber == 1) ?? hours.First();
        result.SixSAnswered = endHour.SixSCompleted.HasValue;
        result.TpmAnswered = endHour.TPMCompleted.HasValue;
        result.SixSDone = endHour.SixSCompleted == true;
        result.TpmDone = endHour.TPMCompleted == true;

        if (!result.SixSAnswered) result.MissingItems.Add("6S end-of-shift check not answered");
        if (!result.TpmAnswered) result.MissingItems.Add("TPM end-of-shift check not answered");

        result.SignedOff = !string.IsNullOrWhiteSpace(sub.OutgoingTLSignature);
        if (!result.SignedOff) result.MissingItems.Add("Outgoing TL sign-off missing");

        result.IsComplete = result.HoursComplete == sub.HoursCompleted
            && result.SixSAnswered && result.TpmAnswered && result.SignedOff;

        return result;
    }

    public static List<string> GetHourMissingFields(HourlyCheck h)
    {
        var missing = new List<string>();
        void ReqBool(string label, bool? val) { if (!val.HasValue) missing.Add(label); }
        void ReqStatus(string label, string? val) { if (string.IsNullOrEmpty(val)) missing.Add(label); }

        ReqBool("hazards check", h.HazardsObserved);
        ReqBool("unsafe behaviours check", h.UnsafeBehaviours);
        ReqBool("positive behaviours check", h.PositiveBehaviours);
        ReqBool("quality checks", h.QualityChecksCompleted);
        ReqBool("deviations check", h.DeviationsEscalated);
        ReqBool("non-compliance check", h.NonComplianceAddressed);
        ReqBool("tools match inspection plan", h.ToolsMatchInspectionPlan);
        ReqBool("hourly target", h.HourlyTargetAchieved);
        ReqBool("maintenance check", h.MaintenanceIssues);
        ReqBool("materials check", h.MaterialsAvailable);
        ReqBool("tools check", h.ToolsAvailable);
        ReqBool("escalations check", h.EscalationsNeeded);
        ReqBool("parts confirmed", h.PartsConfirmed);
        ReqBool("parts ID check", h.PartsIdCorrect);
        ReqBool("NC parts check", h.NCPartsStoredCorrectly);
        ReqBool("wellbeing check", h.WellbeingConfirmed);
        ReqBool("support check", h.SupportRequired);
        ReqBool("accidents check", h.AccidentsReported);
        ReqStatus("safety status", h.OverallSafetyStatus);
        ReqStatus("quality status", h.OverallQualityStatus);
        ReqStatus("performance status", h.OverallPerfStatus);

        return missing;
    }
}

public class ShiftCompletionResult
{
    public bool IsComplete { get; set; }
    public int HoursComplete { get; set; }
    public int HoursTotal { get; set; }
    public bool SixSAnswered { get; set; }
    public bool TpmAnswered { get; set; }
    public bool SixSDone { get; set; }
    public bool TpmDone { get; set; }
    public bool SignedOff { get; set; }
    public List<string> MissingItems { get; set; } = new();
}
