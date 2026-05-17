using AgentApp.Application.ExecutionStates;
using AgentApp.Domain.ExecutionStates;
using NSubstitute;

namespace AgentApp.Application.Tests.ExecutionStates;

public class StateAwareSystemMessageBuilderTests
{
    [Fact]
    public void GetContextFileNames_Returns_FilesFromActiveStateProvider()
    {
        var machine = Substitute.For<IExecutionStateMachine>();
        machine.CurrentState.Returns(ExecutionStateName.Research);

        var provider = Substitute.For<IExecutionStateProvider>();
        provider.StateName.Returns(ExecutionStateName.Research);
        provider.GetSystemMdFileNames().Returns(["workflow-investigation"]);

        var registry = Substitute.For<IExecutionStateRegistry>();
        registry.Resolve(ExecutionStateName.Research).Returns(provider);

        var builder = new StateAwareSystemMessageBuilder(machine, registry);
        var files = builder.GetContextFileNames();

        Assert.Contains("workflow-investigation", files);
    }

    [Fact]
    public void GetContextFileNames_WhenNoProviderRegistered_Returns_Empty()
    {
        var machine = Substitute.For<IExecutionStateMachine>();
        machine.CurrentState.Returns(ExecutionStateName.Testing);

        var registry = Substitute.For<IExecutionStateRegistry>();
        registry.Resolve(ExecutionStateName.Testing).Returns((IExecutionStateProvider?)null);

        var builder = new StateAwareSystemMessageBuilder(machine, registry);
        var files = builder.GetContextFileNames();

        Assert.Empty(files);
    }

    [Fact]
    public void GetContextFileNames_ReflectsCurrentMachineState()
    {
        var machine = Substitute.For<IExecutionStateMachine>();

        var chatProvider = Substitute.For<IExecutionStateProvider>();
        chatProvider.StateName.Returns(ExecutionStateName.Chat);
        chatProvider.GetSystemMdFileNames().Returns([]);

        var implementingProvider = Substitute.For<IExecutionStateProvider>();
        implementingProvider.StateName.Returns(ExecutionStateName.Implementing);
        implementingProvider.GetSystemMdFileNames().Returns(["workflow-git", "architecture-backend"]);

        var registry = Substitute.For<IExecutionStateRegistry>();
        registry.Resolve(ExecutionStateName.Chat).Returns(chatProvider);
        registry.Resolve(ExecutionStateName.Implementing).Returns(implementingProvider);

        machine.CurrentState.Returns(ExecutionStateName.Chat);
        var builder = new StateAwareSystemMessageBuilder(machine, registry);
        Assert.Empty(builder.GetContextFileNames());

        machine.CurrentState.Returns(ExecutionStateName.Implementing);
        var implementingFiles = builder.GetContextFileNames();
        Assert.Contains("workflow-git", implementingFiles);
        Assert.Contains("architecture-backend", implementingFiles);
    }

    [Fact]
    public void GetContextFileNames_ChatState_Returns_Empty()
    {
        var machine = Substitute.For<IExecutionStateMachine>();
        machine.CurrentState.Returns(ExecutionStateName.Chat);

        var chatProvider = Substitute.For<IExecutionStateProvider>();
        chatProvider.StateName.Returns(ExecutionStateName.Chat);
        chatProvider.GetSystemMdFileNames().Returns([]);

        var registry = Substitute.For<IExecutionStateRegistry>();
        registry.Resolve(ExecutionStateName.Chat).Returns(chatProvider);

        var builder = new StateAwareSystemMessageBuilder(machine, registry);
        Assert.Empty(builder.GetContextFileNames());
    }
}
