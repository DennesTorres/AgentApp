using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;

namespace AgentApp.Application.Tests.Fakes;

internal sealed class FakeProjectSwitchHandler : IProjectSwitchHandler
{
    public Project? LastSwitchedTo { get; private set; }
    public bool StandaloneResetCalled { get; private set; }

    public Task ResetForProjectAsync(Project project)
    {
        LastSwitchedTo = project;
        return Task.CompletedTask;
    }

    public Task ResetForStandaloneAsync()
    {
        StandaloneResetCalled = true;
        return Task.CompletedTask;
    }
}
