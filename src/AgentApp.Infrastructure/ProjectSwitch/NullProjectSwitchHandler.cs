using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;

namespace AgentApp.Infrastructure.ProjectSwitch;

// Placeholder implementation — replaced in Epic 11 (Dual-Agent Pipeline)
// when board, context, and agent state reset are fully implemented.
public class NullProjectSwitchHandler : IProjectSwitchHandler
{
    public Task ResetForProjectAsync(Project project) => Task.CompletedTask;
    public Task ResetForStandaloneAsync() => Task.CompletedTask;
}
