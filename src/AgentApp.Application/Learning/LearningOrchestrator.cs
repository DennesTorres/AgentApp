using System.Text.Json;
using System.Text.RegularExpressions;
using AgentApp.Application.Providers;
using AgentApp.Domain.Learning;
using AgentApp.Domain.Providers;

namespace AgentApp.Application.Learning;

public class LearningOrchestrator
{
    private readonly OrchestratorPipeline _pipeline;

    public LearningOrchestrator(OrchestratorPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task<ProposedRuleChange?> RunLearningLoopAsync(
        LearningSession session,
        string? reasoningTraceContent = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["violation"] = session.ViolationDescription,
            ["trigger"] = session.Trigger.ToString()
        };

        if (!string.IsNullOrEmpty(reasoningTraceContent))
            payload["reasoningTrace"] = reasoningTraceContent;

        var request = ProviderRequest.Create(ProviderCapability.ModelCall, payload);
        var response = await _pipeline.SendAsync(request, cancellationToken);

        if (!response.Success) return null;

        var text = response.Result.TryGetValue("text", out var t) ? t?.ToString() ?? "" : "";
        var proposal = ParseProposal(text);

        if (proposal != null)
            session.ProposeChange(proposal);

        return proposal;
    }

    private static ProposedRuleChange? ParseProposal(string text)
    {
        var match = Regex.Match(text, @"\[RULE_PROPOSAL:(\{.*?\})\]", RegexOptions.Singleline);
        if (!match.Success) return null;

        try
        {
            var json = match.Groups[1].Value;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var fileName = root.GetProperty("fileName").GetString() ?? "";
            var ruleText = root.GetProperty("ruleText").GetString() ?? "";
            var humanSummary = root.GetProperty("humanSummary").GetString() ?? "";
            var action = root.GetProperty("action").GetString() ?? "";
            return ProposedRuleChange.Create(fileName, ruleText, humanSummary, action);
        }
        catch
        {
            return null;
        }
    }
}
