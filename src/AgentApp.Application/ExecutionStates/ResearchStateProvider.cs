using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.ExecutionStates;

public class ResearchStateProvider : IExecutionStateProvider
{
    public ExecutionStateName StateName => ExecutionStateName.Research;
    public IReadOnlyList<string> GetSystemMdFileNames() => ["workflow-investigation"];
}
