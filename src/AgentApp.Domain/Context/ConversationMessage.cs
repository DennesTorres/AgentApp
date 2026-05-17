namespace AgentApp.Domain.Context;

public class ConversationMessage
{
    public Guid Id { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int TokenEstimate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ConversationMessage() { }

    public static ConversationMessage Create(MessageRole role, string content)
    {
        var tokens = Math.Max(1, content.Length / 4);
        return new ConversationMessage
        {
            Id = Guid.NewGuid(),
            Role = role,
            Content = content,
            TokenEstimate = tokens,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static ConversationMessage Reconstitute(Guid id, MessageRole role, string content,
        int tokenEstimate, DateTimeOffset createdAt) => new()
    {
        Id = id,
        Role = role,
        Content = content,
        TokenEstimate = tokenEstimate,
        CreatedAt = createdAt
    };
}
