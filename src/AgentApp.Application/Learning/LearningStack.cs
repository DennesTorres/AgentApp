using AgentApp.Domain.Learning;

namespace AgentApp.Application.Learning;

public class LearningStack
{
    private readonly Stack<LearningSession> _stack = new();

    public bool IsEmpty => _stack.Count == 0;
    public int Count => _stack.Count;

    public void Push(LearningSession session) => _stack.Push(session);

    public LearningSession? TryPop() =>
        _stack.TryPop(out var session) ? session : null;
}
