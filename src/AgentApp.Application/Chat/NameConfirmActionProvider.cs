using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Chat;

public class NameConfirmActionProvider : IActionProvider
{
    private readonly IAgentContextService _contextService;
    private readonly IProjectRepository _projectRepository;

    public NameConfirmActionProvider(IAgentContextService contextService, IProjectRepository projectRepository)
    {
        _contextService = contextService;
        _projectRepository = projectRepository;
    }

    public bool CanHandle(ChatCommand command) => command is NameConfirmedCommand;

    public async Task HandleAsync(ChatCommand command, CancellationToken cancellationToken = default)
    {
        _contextService.ConfirmProjectName();
        // C-095: persist NameConfirmed on the Project entity so it survives session restarts
        var project = _contextService.GetCurrent().CurrentProject;
        if (project is not null)
        {
            project.ConfirmName();
            await _projectRepository.SaveAsync(project);
        }
    }
}
