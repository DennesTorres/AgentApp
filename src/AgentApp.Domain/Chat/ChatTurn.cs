namespace AgentApp.Domain.Chat;

public record ChatTurn(ChatTurnRole Role, string Content, DateTimeOffset Timestamp);
