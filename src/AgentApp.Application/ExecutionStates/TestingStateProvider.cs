using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.ExecutionStates;

public class TestingStateProvider : IExecutionStateProvider
{
    public ExecutionStateName StateName => ExecutionStateName.Testing;
    public IReadOnlyList<string> GetSystemMdFileNames() => ["workflow-testing"];
}
