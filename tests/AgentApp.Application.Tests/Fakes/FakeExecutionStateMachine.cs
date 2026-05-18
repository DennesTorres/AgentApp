using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.Tests.Fakes;

internal sealed class FakeExecutionStateMachine : IExecutionStateMachine
{
    public ExecutionStateName CurrentState { get; set; } = ExecutionStateName.Chat;

    public bool CanTransitionTo(ExecutionStateName targetState) => true;

    public void TransitionTo(ExecutionStateName targetState) => CurrentState = targetState;

    public event EventHandler<ExecutionStateName>? StateChanged;
}
