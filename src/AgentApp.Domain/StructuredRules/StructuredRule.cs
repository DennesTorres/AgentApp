using System.Text.Json;

namespace AgentApp.Domain.StructuredRules;

public class StructuredRule
{
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Trigger { get; private set; } = string.Empty;
    public IReadOnlyList<string> Steps { get; private set; } = [];
    public int StepCount => Steps.Count;

    private StructuredRule() { }

    public static StructuredRule Create(string id, string name, string trigger, IReadOnlyList<string> steps)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Rule ID must not be empty.", nameof(id));
        if (steps.Count == 0)
            throw new ArgumentException("Rule must have at least one step.", nameof(steps));

        return new StructuredRule
        {
            Id = id.Trim(),
            Name = name,
            Trigger = trigger,
            Steps = steps.ToList()
        };
    }

    public string ToJson() => JsonSerializer.Serialize(new
    {
        id = Id,
        name = Name,
        trigger = Trigger,
        steps = Steps
    });
}
