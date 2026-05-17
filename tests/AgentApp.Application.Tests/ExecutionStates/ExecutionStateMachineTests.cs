using AgentApp.Application.ExecutionStates;
using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.Tests.ExecutionStates;

public class ExecutionStateMachineTests
{
    [Fact]
    public void InitialState_Is_Chat()
    {
        var machine = new ExecutionStateMachine();
        Assert.Equal(ExecutionStateName.Chat, machine.CurrentState);
    }

    [Theory]
    [InlineData(ExecutionStateName.Chat, ExecutionStateName.Research)]
    [InlineData(ExecutionStateName.Chat, ExecutionStateName.Implementing)]
    [InlineData(ExecutionStateName.Chat, ExecutionStateName.Testing)]
    [InlineData(ExecutionStateName.Research, ExecutionStateName.Chat)]
    [InlineData(ExecutionStateName.Research, ExecutionStateName.Implementing)]
    [InlineData(ExecutionStateName.Implementing, ExecutionStateName.Testing)]
    [InlineData(ExecutionStateName.Implementing, ExecutionStateName.Chat)]
    [InlineData(ExecutionStateName.Testing, ExecutionStateName.Implementing)]
    [InlineData(ExecutionStateName.Testing, ExecutionStateName.Chat)]
    public void CanTransitionTo_ValidTransition_Returns_True(ExecutionStateName from, ExecutionStateName to)
    {
        var machine = new ExecutionStateMachine();
        if (from != ExecutionStateName.Chat)
            machine.TransitionTo(from);

        Assert.True(machine.CanTransitionTo(to));
    }

    [Theory]
    [InlineData(ExecutionStateName.Research, ExecutionStateName.Testing)]
    [InlineData(ExecutionStateName.Implementing, ExecutionStateName.Research)]
    [InlineData(ExecutionStateName.Testing, ExecutionStateName.Research)]
    public void CanTransitionTo_InvalidTransition_Returns_False(ExecutionStateName from, ExecutionStateName to)
    {
        var machine = new ExecutionStateMachine();
        if (from != ExecutionStateName.Chat)
            machine.TransitionTo(from);

        Assert.False(machine.CanTransitionTo(to));
    }

    [Theory]
    [InlineData(ExecutionStateName.Chat, ExecutionStateName.Chat)]
    [InlineData(ExecutionStateName.Research, ExecutionStateName.Research)]
    [InlineData(ExecutionStateName.Implementing, ExecutionStateName.Implementing)]
    [InlineData(ExecutionStateName.Testing, ExecutionStateName.Testing)]
    public void CanTransitionTo_SameState_Returns_False(ExecutionStateName from, ExecutionStateName to)
    {
        var machine = new ExecutionStateMachine();
        if (from != ExecutionStateName.Chat)
            machine.TransitionTo(from);

        Assert.False(machine.CanTransitionTo(to));
    }

    [Fact]
    public void TransitionTo_ValidTransition_Changes_CurrentState()
    {
        var machine = new ExecutionStateMachine();
        machine.TransitionTo(ExecutionStateName.Research);
        Assert.Equal(ExecutionStateName.Research, machine.CurrentState);
    }

    [Fact]
    public void TransitionTo_InvalidTransition_Throws_InvalidOperationException()
    {
        var machine = new ExecutionStateMachine();
        machine.TransitionTo(ExecutionStateName.Research);

        Assert.Throws<InvalidOperationException>(() =>
            machine.TransitionTo(ExecutionStateName.Testing));
    }

    [Fact]
    public void TransitionTo_SameState_Throws_InvalidOperationException()
    {
        var machine = new ExecutionStateMachine();
        Assert.Throws<InvalidOperationException>(() =>
            machine.TransitionTo(ExecutionStateName.Chat));
    }

    [Fact]
    public void TransitionTo_ValidTransition_Raises_StateChanged_Event()
    {
        var machine = new ExecutionStateMachine();
        ExecutionStateName? receivedState = null;
        machine.StateChanged += (_, state) => receivedState = state;

        machine.TransitionTo(ExecutionStateName.Implementing);

        Assert.Equal(ExecutionStateName.Implementing, receivedState);
    }

    [Fact]
    public void TransitionTo_MultiStep_UpdatesCurrentState()
    {
        var machine = new ExecutionStateMachine();
        machine.TransitionTo(ExecutionStateName.Implementing);
        machine.TransitionTo(ExecutionStateName.Testing);
        machine.TransitionTo(ExecutionStateName.Implementing);
        machine.TransitionTo(ExecutionStateName.Chat);

        Assert.Equal(ExecutionStateName.Chat, machine.CurrentState);
    }
}
