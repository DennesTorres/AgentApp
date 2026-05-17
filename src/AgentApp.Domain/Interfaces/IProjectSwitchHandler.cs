using AgentApp.Domain.Projects;

namespace AgentApp.Domain.Interfaces;

public interface IProjectSwitchHandler
{
    Task ResetForProjectAsync(Project project);
    Task ResetForStandaloneAsync();
}
