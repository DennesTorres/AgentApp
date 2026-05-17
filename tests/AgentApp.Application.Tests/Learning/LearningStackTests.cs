using AgentApp.Application.Learning;
using AgentApp.Domain.Learning;

namespace AgentApp.Application.Tests.Learning;

public class LearningStackTests
{
    [Fact]
    public void Initially_IsEmpty()
    {
        var stack = new LearningStack();
        Assert.True(stack.IsEmpty);
        Assert.Equal(0, stack.Count);
    }

    [Fact]
    public void Push_Increments_Count()
    {
        var stack = new LearningStack();
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation");
        stack.Push(session);
        Assert.Equal(1, stack.Count);
        Assert.False(stack.IsEmpty);
    }

    [Fact]
    public void TryPop_WhenEmpty_ReturnsNull()
    {
        var stack = new LearningStack();
        Assert.Null(stack.TryPop());
    }

    [Fact]
    public void TryPop_ReturnsLastPushed_Lifo()
    {
        var stack = new LearningStack();
        var first = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "first");
        var second = LearningSession.Initiate(LearningTrigger.ExternalUserError, "second");

        stack.Push(first);
        stack.Push(second);

        var popped = stack.TryPop();
        Assert.Same(second, popped);
    }

    [Fact]
    public void TryPop_Decrements_Count()
    {
        var stack = new LearningStack();
        stack.Push(LearningSession.Initiate(LearningTrigger.InternalGateFailure, "v"));
        stack.TryPop();
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void Push_MultipleSessions_AllInStack()
    {
        var stack = new LearningStack();
        for (int i = 0; i < 3; i++)
            stack.Push(LearningSession.Initiate(LearningTrigger.InternalGateFailure, $"violation {i}"));
        Assert.Equal(3, stack.Count);
    }
}
