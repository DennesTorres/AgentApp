using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Projects;

public class ProjectSwitchService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectSwitchHandler _switchHandler;

    public ProjectSwitchService(IProjectRepository projectRepository, IProjectSwitchHandler switchHandler)
    {
        _projectRepository = projectRepository;
        _switchHandler = switchHandler;
    }

    public async Task SwitchToProjectAsync(Guid projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId)
            ?? throw new DomainNotFoundException($"Project '{projectId}' not found.");

        await _switchHandler.ResetForProjectAsync(project);
    }

    public async Task SwitchToNoneAsync()
    {
        await _switchHandler.ResetForStandaloneAsync();
    }
}
