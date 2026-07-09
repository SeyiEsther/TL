namespace TL.Helpers;

public static class UkTime
{
    static readonly TimeZoneInfo Zone = ResolveUkZone();

    static TimeZoneInfo ResolveUkZone()
    {
        foreach (var id in new[] { "GMT Standard Time", "Europe/London" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }

    public static DateTime NowFromUtc() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
}
