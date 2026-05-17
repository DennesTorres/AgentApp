using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Rules;

namespace AgentApp.Domain.Filters;

public class FilterRule
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public FilterTarget Target { get; private set; }
    public string MessageType { get; private set; } = string.Empty;
    public FilterTransformation Transformation { get; private set; }
    public int? MaxLength { get; private set; }
    public MdFileScope Scope { get; private set; }
    public Guid? ProjectId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private FilterRule() { }

    public static FilterRule Create(string name, FilterTarget target, string messageType,
        FilterTransformation transformation, int? maxLength, MdFileScope scope, Guid? projectId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Filter rule name must not be empty.");
        if (string.IsNullOrWhiteSpace(messageType))
            throw new DomainValidationException("Filter rule message type must not be empty.");

        return new FilterRule
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Target = target,
            MessageType = messageType.Trim(),
            Transformation = transformation,
            MaxLength = maxLength,
            Scope = scope,
            ProjectId = projectId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static FilterRule Reconstitute(Guid id, string name, FilterTarget target, string messageType,
        FilterTransformation transformation, int? maxLength, MdFileScope scope, Guid? projectId,
        DateTimeOffset createdAt) => new()
    {
        Id = id,
        Name = name,
        Target = target,
        MessageType = messageType,
        Transformation = transformation,
        MaxLength = maxLength,
        Scope = scope,
        ProjectId = projectId,
        CreatedAt = createdAt
    };
}
