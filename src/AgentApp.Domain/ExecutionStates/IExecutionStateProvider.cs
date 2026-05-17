namespace AgentApp.Domain.ExecutionStates;

public interface IExecutionStateProvider
{
    ExecutionStateName StateName { get; }
    IReadOnlyList<string> GetSystemMdFileNames();
}
