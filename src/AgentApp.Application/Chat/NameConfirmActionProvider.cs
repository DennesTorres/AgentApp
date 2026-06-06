using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Chat;

public class NameConfirmActionProvider : IActionProvider
{
    private readonly IAgentContextService _contextService;

    public NameConfirmActionProvider(IAgentContextService contextService)
    {
        _contextService = contextService;
    }

    public bool CanHandle(ChatCommand command) => command is NameConfirmedCommand;

    public Task HandleAsync(ChatCommand command, CancellationToken cancellationToken = default)
    {
        _contextService.ConfirmProjectName();
        return Task.CompletedTask;
    }
}
