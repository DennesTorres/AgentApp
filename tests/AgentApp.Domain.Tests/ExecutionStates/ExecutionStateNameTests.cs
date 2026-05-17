using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Domain.Tests.ExecutionStates;

public class ExecutionStateNameTests
{
    [Fact]
    public void AllFourStates_AreDefined()
    {
        var names = Enum.GetValues<ExecutionStateName>();
        Assert.Contains(ExecutionStateName.Chat, names);
        Assert.Contains(ExecutionStateName.Research, names);
        Assert.Contains(ExecutionStateName.Implementing, names);
        Assert.Contains(ExecutionStateName.Testing, names);
    }
}
