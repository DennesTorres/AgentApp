using AgentApp.Domain.Sessions;

namespace AgentApp.Domain.Interfaces;

public interface ISessionRepository
{
    Task<ChatSession?> GetByIdAsync(Guid sessionId);
    Task SaveAsync(ChatSession session);
    Task<IReadOnlyList<ChatSession>> GetAllAsync();
    Task DeleteAsync(Guid sessionId);
}
