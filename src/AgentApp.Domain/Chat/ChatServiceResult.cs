namespace AgentApp.Domain.Chat;

public record ChatServiceResult(string DisplayText, IReadOnlyList<ChatCommand> Commands);
