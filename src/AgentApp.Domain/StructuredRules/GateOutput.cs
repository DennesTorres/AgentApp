using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgentApp.Domain.StructuredRules;

public class GateOutput
{
    public string RuleId { get; }
    public int StepNumber { get; }

    public GateOutput(string ruleId, int stepNumber)
    {
        RuleId = ruleId;
        StepNumber = stepNumber;
    }

    public static GateOutput? TryParse(string text)
    {
        var match = Regex.Match(text, @"\[GATE:(\{[^}]+\})\]");
        if (!match.Success) return null;

        try
        {
            var json = match.Groups[1].Value;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var rule = root.GetProperty("rule").GetString();
            var step = root.GetProperty("step").GetInt32();
            if (string.IsNullOrEmpty(rule)) return null;
            return new GateOutput(rule, step);
        }
        catch
        {
            return null;
        }
    }
}
