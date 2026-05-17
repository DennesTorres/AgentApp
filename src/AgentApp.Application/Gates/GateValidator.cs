using System.Text.Json;
using System.Text.RegularExpressions;
using AgentApp.Domain.Gates;

namespace AgentApp.Application.Gates;

public class GateValidator
{
    private static readonly Regex GateBlockPattern = new(
        @"```gate-output\s*\n([\s\S]*?)\n```",
        RegexOptions.Compiled);

    public GateValidationResult Validate(string modelResponse, GateRule rule)
    {
        var match = GateBlockPattern.Match(modelResponse);
        if (!match.Success)
            return GateValidationResult.Fail(rule.RequiredOutputKeys.ToList());

        Dictionary<string, object?>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(match.Groups[1].Value);
        }
        catch
        {
            return GateValidationResult.Fail(rule.RequiredOutputKeys.ToList());
        }

        if (parsed == null)
            return GateValidationResult.Fail(rule.RequiredOutputKeys.ToList());

        var missingKeys = rule.RequiredOutputKeys
            .Where(k => !parsed.ContainsKey(k))
            .ToList();

        return missingKeys.Count == 0
            ? GateValidationResult.Pass(parsed)
            : GateValidationResult.Fail(missingKeys);
    }

    public string StripGateOutput(string modelResponse) =>
        GateBlockPattern.Replace(modelResponse, string.Empty).Trim();
}
