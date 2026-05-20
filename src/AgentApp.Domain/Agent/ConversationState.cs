namespace AgentApp.Domain.Agent;

public record ConversationState(string Mode)
{
    public static readonly ConversationState Initial = new("chat");
    public static readonly ConversationState Implementing = new("implementing");
    public static readonly ConversationState Testing = new("testing");
    public static readonly ConversationState Planning = new("planning");

    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;

    public static ConversationState FromMode(string mode) => new(mode.ToLowerInvariant());
}
