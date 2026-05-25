using AgentApp.Domain.Chat;

namespace AgentApp.Application.Chat;

public interface IActionProviderRegistry
{
    void Register(IActionProvider provider);
    Task<IReadOnlyList<ChatCommand>> DispatchAsync(IReadOnlyList<ChatCommand> commands, CancellationToken cancellationToken = default);
}
