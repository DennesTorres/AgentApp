namespace AgentApp.Domain.StructuredRules;

public class GateViolation
{
    public string RuleId { get; private set; } = string.Empty;
    public int ExpectedStep { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private GateViolation() { }

    public static GateViolation Create(string ruleId, int expectedStep) => new()
    {
        RuleId = ruleId,
        ExpectedStep = expectedStep,
        OccurredAt = DateTimeOffset.UtcNow
    };
}
