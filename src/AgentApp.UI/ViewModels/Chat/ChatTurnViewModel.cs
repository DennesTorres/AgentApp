namespace AgentApp.UI.ViewModels.Chat;

public class ChatTurnViewModel
{
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Timestamp { get; init; } = string.Empty;
    public bool IsUser => Role == "User";
    public bool IsAgent => Role == "Tower";
}
