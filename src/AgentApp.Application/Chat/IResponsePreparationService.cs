using AgentApp.Domain.Chat;

namespace AgentApp.Application.Chat;

public interface IResponsePreparationService
{
    (string DisplayText, IReadOnlyList<ChatCommand> Commands) Prepare(string rawText);
}
