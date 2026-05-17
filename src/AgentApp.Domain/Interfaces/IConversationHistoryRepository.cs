using AgentApp.Domain.Context;

namespace AgentApp.Domain.Interfaces;

public interface IConversationHistoryRepository
{
    Task<IReadOnlyList<ConversationMessage>> GetBySessionIdAsync(Guid sessionId);
    Task SaveAsync(Guid sessionId, IReadOnlyList<ConversationMessage> messages);
    Task ArchiveAsync(Guid sessionId, IReadOnlyList<ConversationMessage> messages);
}
