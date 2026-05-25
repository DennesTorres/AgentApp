using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Sessions;

namespace AgentApp.Application.Tests.Fakes;

public class InMemorySessionMessageRepository : ISessionMessageRepository
{
    private readonly List<SessionMessage> _messages = [];

    public Task<IReadOnlyList<SessionMessage>> GetBySessionIdAsync(Guid sessionId)
    {
        IReadOnlyList<SessionMessage> result = _messages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveAsync(SessionMessage message)
    {
        _messages.Add(message);
        return Task.CompletedTask;
    }
}
