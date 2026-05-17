using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Gates;

public class GateRule
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string RuleText { get; private set; } = string.Empty;
    public IReadOnlyList<string> RequiredOutputKeys { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }

    private GateRule() { }

    public static GateRule Create(string name, string ruleText, IEnumerable<string> requiredOutputKeys)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Gate rule name must not be empty.");
        if (string.IsNullOrWhiteSpace(ruleText))
            throw new DomainValidationException("Gate rule text must not be empty.");

        var keys = requiredOutputKeys.ToList();
        if (keys.Count == 0)
            throw new DomainValidationException("Gate rule must specify at least one required output key.");

        return new GateRule
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            RuleText = ruleText,
            RequiredOutputKeys = keys,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static GateRule Reconstitute(Guid id, string name, string ruleText,
        IReadOnlyList<string> requiredOutputKeys, DateTimeOffset createdAt) => new()
    {
        Id = id,
        Name = name,
        RuleText = ruleText,
        RequiredOutputKeys = requiredOutputKeys,
        CreatedAt = createdAt
    };
}
