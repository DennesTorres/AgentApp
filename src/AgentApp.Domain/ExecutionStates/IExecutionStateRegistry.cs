namespace AgentApp.Domain.ExecutionStates;

public interface IExecutionStateRegistry
{
    void Register(IExecutionStateProvider provider);
    IExecutionStateProvider? Resolve(ExecutionStateName stateName);
    IReadOnlyList<IExecutionStateProvider> GetAll();
}
