using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.ExecutionStates;

public class ExecutionStateMachine : IExecutionStateMachine
{
    private static readonly IReadOnlyDictionary<ExecutionStateName, IReadOnlyList<ExecutionStateName>> ValidTransitions =
        new Dictionary<ExecutionStateName, IReadOnlyList<ExecutionStateName>>
        {
            [ExecutionStateName.Chat]        = [ExecutionStateName.Research, ExecutionStateName.Implementing, ExecutionStateName.Testing],
            [ExecutionStateName.Research]    = [ExecutionStateName.Chat, ExecutionStateName.Implementing],
            [ExecutionStateName.Implementing]= [ExecutionStateName.Testing, ExecutionStateName.Chat],
            [ExecutionStateName.Testing]     = [ExecutionStateName.Implementing, ExecutionStateName.Chat]
        };

    public ExecutionStateName CurrentState { get; private set; } = ExecutionStateName.Chat;

    public event EventHandler<ExecutionStateName>? StateChanged;

    public bool CanTransitionTo(ExecutionStateName targetState) =>
        ValidTransitions.TryGetValue(CurrentState, out var allowed) &&
        allowed.Contains(targetState);

    public void TransitionTo(ExecutionStateName targetState)
    {
        if (!CanTransitionTo(targetState))
            throw new InvalidOperationException(
                $"Cannot transition from {CurrentState} to {targetState}.");

        CurrentState = targetState;
        StateChanged?.Invoke(this, CurrentState);
    }
}
