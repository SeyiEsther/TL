using System.Text.Json;
using TL.Models;

namespace TL.Services;

public static class HodAuditSerializer
{
    static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static List<HodAuditAnswer> ParseAnswers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<HodAuditAnswer>>(json, Options) ?? []; }
        catch { return []; }
    }

    public static string ToJson(IEnumerable<HodAuditAnswer> answers) =>
        JsonSerializer.Serialize(answers, Options);

    public static List<HodEffectivenessFinding> ParseEffectiveness(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var list = JsonSerializer.Deserialize<List<HodEffectivenessFinding>>(json, Options) ?? [];
            foreach (var f in list)
            {
                f.Issues ??= [];
                f.LinkedAuditFailures ??= [];
            }
            return list;
        }
        catch { return []; }
    }

    public static string EffectivenessToJson(IEnumerable<HodEffectivenessFinding> findings) =>
        JsonSerializer.Serialize(findings, Options);
}
