using System.Text.Json;
using TL.Models;

namespace TL.Services;

// Parses the "assign action" rows posted from the audit forms (a hidden JSON
// field, same pattern as the Team Meeting repeatable sections).
public static class ActionSerializer
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public record NewAction(string? Owner, string? Text, string? Due);

    public static List<NewAction> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<NewAction>>(json, Opts) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
