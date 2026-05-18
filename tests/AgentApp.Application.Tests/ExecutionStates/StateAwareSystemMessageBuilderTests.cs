using AgentApp.Application.ExecutionStates;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.Tests.ExecutionStates;

public class StateAwareSystemMessageBuilderTests
{
    [Fact]
    public void GetContextFileNames_Returns_FilesFromActiveStateProvider()
    {
        var machine = new FakeExecutionStateMachine { CurrentState = ExecutionStateName.Research };
        var provider = new FakeExecutionStateProvider(ExecutionStateName.Research, ["workflow-investigation"]);
        var registry = new ExecutionStateRegistry();
        registry.Register(provider);

        var builder = new StateAwareSystemMessageBuilder(machine, registry);
        var files = builder.GetContextFileNames();

        Assert.Contains("workflow-investigation", files);
    }

    [Fact]
    public void GetContextFileNames_WhenNoProviderRegistered_Returns_Empty()
    {
        var machine = new FakeExecutionStateMachine { CurrentState = ExecutionStateName.Testing };
        var registry = new ExecutionStateRegistry();

        var builder = new StateAwareSystemMessageBuilder(machine, registry);
        var files = builder.GetContextFileNames();

        Assert.Empty(files);
    }

    [Fact]
    public void GetContextFileNames_ReflectsCurrentMachineState()
    {
        var machine = new FakeExecutionStateMachine { CurrentState = ExecutionStateName.Chat };

        var chatProvider = new FakeExecutionStateProvider(ExecutionStateName.Chat, []);
        var implementingProvider = new FakeExecutionStateProvider(ExecutionStateName.Implementing,
            ["workflow-git", "architecture-backend"]);

        var registry = new ExecutionStateRegistry();
        registry.Register(chatProvider);
        registry.Register(implementingProvider);

        var builder = new StateAwareSystemMessageBuilder(machine, registry);
        Assert.Empty(builder.GetContextFileNames());

        machine.CurrentState = ExecutionStateName.Implementing;
        var implementingFiles = builder.GetContextFileNames();
        Assert.Contains("workflow-git", implementingFiles);
        Assert.Contains("architecture-backend", implementingFiles);
    }

    [Fact]
    public void GetContextFileNames_ChatState_Returns_Empty()
    {
        var machine = new FakeExecutionStateMachine { CurrentState = ExecutionStateName.Chat };
        var chatProvider = new FakeExecutionStateProvider(ExecutionStateName.Chat, []);
        var registry = new ExecutionStateRegistry();
        registry.Register(chatProvider);

        var builder = new StateAwareSystemMessageBuilder(machine, registry);
        Assert.Empty(builder.GetContextFileNames());
    }
}
