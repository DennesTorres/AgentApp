namespace AgentApp.Domain.Sessions;

public class SessionMessage
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset Timestamp { get; private set; }

    private SessionMessage() { }

    public static SessionMessage Create(Guid sessionId, string role, string content) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = sessionId,
        Role = role,
        Content = content,
        Timestamp = DateTimeOffset.UtcNow
    };

    public static SessionMessage Reconstitute(Guid id, Guid sessionId, string role, string content, DateTimeOffset timestamp) => new()
    {
        Id = id,
        SessionId = sessionId,
        Role = role,
        Content = content,
        Timestamp = timestamp
    };
}
