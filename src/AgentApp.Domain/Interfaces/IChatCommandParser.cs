using AgentApp.Domain.Chat;

namespace AgentApp.Domain.Interfaces;

public interface IChatCommandParser
{
    (string CleanText, IReadOnlyList<ChatCommand> Commands) Parse(string text);
}
