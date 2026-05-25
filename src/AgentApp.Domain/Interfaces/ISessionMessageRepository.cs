using AgentApp.Domain.Sessions;

namespace AgentApp.Domain.Interfaces;

public interface ISessionMessageRepository
{
    Task<IReadOnlyList<SessionMessage>> GetBySessionIdAsync(Guid sessionId);
    Task SaveAsync(SessionMessage message);
}
