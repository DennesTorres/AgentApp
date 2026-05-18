using AgentApp.Application.ExecutionStates;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.Tests.ExecutionStates;

public class ExecutionStateRegistryTests
{
    [Fact]
    public void Register_And_Resolve_Returns_Provider()
    {
        var provider = new FakeExecutionStateProvider(ExecutionStateName.Chat);
        var registry = new ExecutionStateRegistry();
        registry.Register(provider);

        Assert.Same(provider, registry.Resolve(ExecutionStateName.Chat));
    }

    [Fact]
    public void Resolve_UnknownState_Returns_Null()
    {
        var registry = new ExecutionStateRegistry();

        Assert.Null(registry.Resolve(ExecutionStateName.Research));
    }

    [Fact]
    public void Register_SameState_Twice_LastWins()
    {
        var first = new FakeExecutionStateProvider(ExecutionStateName.Implementing);
        var second = new FakeExecutionStateProvider(ExecutionStateName.Implementing);

        var registry = new ExecutionStateRegistry();
        registry.Register(first);
        registry.Register(second);

        Assert.Same(second, registry.Resolve(ExecutionStateName.Implementing));
    }

    [Fact]
    public void GetAll_Returns_All_Registered()
    {
        var chatProvider = new FakeExecutionStateProvider(ExecutionStateName.Chat);
        var researchProvider = new FakeExecutionStateProvider(ExecutionStateName.Research);

        var registry = new ExecutionStateRegistry();
        registry.Register(chatProvider);
        registry.Register(researchProvider);

        var all = registry.GetAll();
        Assert.Equal(2, all.Count);
        Assert.Contains(chatProvider, all);
        Assert.Contains(researchProvider, all);
    }

    [Fact]
    public void GetAll_WhenEmpty_Returns_EmptyList()
    {
        var registry = new ExecutionStateRegistry();
        Assert.Empty(registry.GetAll());
    }
}
