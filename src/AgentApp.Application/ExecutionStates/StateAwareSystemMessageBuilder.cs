using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.ExecutionStates;

public class StateAwareSystemMessageBuilder
{
    private readonly IExecutionStateMachine _machine;
    private readonly IExecutionStateRegistry _registry;

    public StateAwareSystemMessageBuilder(IExecutionStateMachine machine, IExecutionStateRegistry registry)
    {
        _machine = machine;
        _registry = registry;
    }

    public IReadOnlyList<string> GetContextFileNames()
    {
        var provider = _registry.Resolve(_machine.CurrentState);
        return provider?.GetSystemMdFileNames() ?? [];
    }
}
