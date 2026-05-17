using AgentApp.Domain.Orchestration;

namespace AgentApp.Domain.Interfaces;

public interface IOrchestratorSessionRepository
{
    Task SaveAsync(OrchestratorSession session);
    Task<OrchestratorSession?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<OrchestratorSession>> GetByProjectIdAsync(Guid projectId);
}
