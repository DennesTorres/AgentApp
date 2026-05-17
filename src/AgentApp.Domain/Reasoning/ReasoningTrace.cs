using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Reasoning;

public class ReasoningTrace
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid MessageId { get; private set; }
    public string Content { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ReasoningTrace() { Content = string.Empty; }

    public static ReasoningTrace Capture(Guid sessionId, Guid messageId, string content)
    {
        if (sessionId == Guid.Empty) throw new DomainValidationException("SessionId must not be empty.");
        if (messageId == Guid.Empty) throw new DomainValidationException("MessageId must not be empty.");
        if (string.IsNullOrWhiteSpace(content)) throw new DomainValidationException("Content must not be empty.");

        return new ReasoningTrace
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            MessageId = messageId,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static ReasoningTrace Reconstitute(
        Guid id, Guid sessionId, Guid messageId, string content, DateTimeOffset createdAt) =>
        new() { Id = id, SessionId = sessionId, MessageId = messageId, Content = content, CreatedAt = createdAt };
}
