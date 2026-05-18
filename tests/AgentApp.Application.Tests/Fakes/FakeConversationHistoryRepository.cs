using AgentApp.Domain.Context;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Tests.Fakes;

internal sealed class FakeConversationHistoryRepository : IConversationHistoryRepository
{
    private readonly Dictionary<Guid, List<ConversationMessage>> _active = [];
    private readonly List<Guid> _archivedSessions = [];

    public Task<IReadOnlyList<ConversationMessage>> GetBySessionIdAsync(Guid sessionId)
    {
        _active.TryGetValue(sessionId, out var msgs);
        return Task.FromResult<IReadOnlyList<ConversationMessage>>(msgs ?? []);
    }

    public Task SaveAsync(Guid sessionId, IReadOnlyList<ConversationMessage> messages)
    {
        _active[sessionId] = messages.ToList();
        return Task.CompletedTask;
    }

    public Task ArchiveAsync(Guid sessionId, IReadOnlyList<ConversationMessage> messages)
    {
        _archivedSessions.Add(sessionId);
        return Task.CompletedTask;
    }

    public bool WasArchived(Guid sessionId) => _archivedSessions.Contains(sessionId);
    public IReadOnlyList<ConversationMessage> GetSaved(Guid sessionId)
        => _active.GetValueOrDefault(sessionId) ?? [];
}
