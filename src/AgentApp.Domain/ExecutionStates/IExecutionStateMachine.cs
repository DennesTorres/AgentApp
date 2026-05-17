namespace AgentApp.Domain.ExecutionStates;

public interface IExecutionStateMachine
{
    ExecutionStateName CurrentState { get; }
    bool CanTransitionTo(ExecutionStateName targetState);
    void TransitionTo(ExecutionStateName targetState);
    event EventHandler<ExecutionStateName>? StateChanged;
}
