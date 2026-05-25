using AgentApp.Domain.Chat;

namespace AgentApp.Application.Chat;

public interface IActionProvider
{
    bool CanHandle(ChatCommand command);
    Task HandleAsync(ChatCommand command, CancellationToken cancellationToken = default);
}
