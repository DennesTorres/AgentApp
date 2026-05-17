using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.ExecutionStates;

public class ExecutionStateRegistry : IExecutionStateRegistry
{
    private readonly Dictionary<ExecutionStateName, IExecutionStateProvider> _providers = new();

    public void Register(IExecutionStateProvider provider) =>
        _providers[provider.StateName] = provider;

    public IExecutionStateProvider? Resolve(ExecutionStateName stateName) =>
        _providers.TryGetValue(stateName, out var provider) ? provider : null;

    public IReadOnlyList<IExecutionStateProvider> GetAll() =>
        _providers.Values.ToList();
}
