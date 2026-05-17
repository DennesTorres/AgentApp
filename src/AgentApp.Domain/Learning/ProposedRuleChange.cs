namespace AgentApp.Domain.Learning;

public class ProposedRuleChange
{
    public string FileName { get; }
    public string RuleText { get; }
    public string HumanSummary { get; }
    public string Action { get; }

    private ProposedRuleChange(string fileName, string ruleText, string humanSummary, string action)
    {
        FileName = fileName;
        RuleText = ruleText;
        HumanSummary = humanSummary;
        Action = action;
    }

    public static ProposedRuleChange Create(string fileName, string ruleText, string humanSummary, string action) =>
        new(fileName, ruleText, humanSummary, action);
}
