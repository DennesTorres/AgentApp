using AgentApp.Domain.Chat;

namespace AgentApp.Application.Chat;

public class ActionProviderRegistry : IActionProviderRegistry
{
    private readonly List<IActionProvider> _providers = [];

    public void Register(IActionProvider provider) => _providers.Add(provider);

    public async Task<IReadOnlyList<ChatCommand>> DispatchAsync(
        IReadOnlyList<ChatCommand> commands,
        CancellationToken cancellationToken = default)
    {
        var unhandled = new List<ChatCommand>();
        foreach (var command in commands)
        {
            var handler = _providers.FirstOrDefault(p => p.CanHandle(command));
            if (handler is not null)
                await handler.HandleAsync(command, cancellationToken);
            else
                unhandled.Add(command);
        }
        return unhandled;
    }
}
