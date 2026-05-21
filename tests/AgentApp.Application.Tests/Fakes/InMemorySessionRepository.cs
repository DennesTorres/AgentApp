using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Sessions;

namespace AgentApp.Application.Tests.Fakes;

internal sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly Dictionary<Guid, ChatSession> _store = [];

    public Task<ChatSession?> GetByIdAsync(Guid sessionId)
        => Task.FromResult(_store.GetValueOrDefault(sessionId));

    public Task SaveAsync(ChatSession session)
    {
        _store[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChatSession>> GetAllAsync()
        => Task.FromResult<IReadOnlyList<ChatSession>>(_store.Values.ToList());

    public Task DeleteAsync(Guid sessionId)
    {
        _store.Remove(sessionId);
        return Task.CompletedTask;
    }
}
