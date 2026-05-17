using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.ExecutionStates;

public class ImplementingStateProvider : IExecutionStateProvider
{
    public ExecutionStateName StateName => ExecutionStateName.Implementing;
    public IReadOnlyList<string> GetSystemMdFileNames() => ["workflow-git", "architecture-backend"];
}
