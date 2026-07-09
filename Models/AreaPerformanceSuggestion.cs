namespace TL.Models;

public record AreaPerformanceSuggestion(string Area, int Reds, int Ambers)
{
    public string Reason => Reds > 0
        ? $"{Reds} red status{(Reds > 1 ? "es" : "")}"
        : Ambers > 0
            ? $"{Ambers} amber status{(Ambers > 1 ? "es" : "")}"
            : "No red/amber flags";

    public string ShortLabel
    {
        get
        {
            var dash = Area.IndexOf(" — ", StringComparison.Ordinal);
            return dash >= 0 ? Area[(dash + 3)..].Trim() : Area;
        }
    }
}
