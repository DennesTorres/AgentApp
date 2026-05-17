using AgentApp.Domain.Reasoning;

namespace AgentApp.Domain.Interfaces;

public interface IReasoningTraceRepository
{
    Task SaveAsync(ReasoningTrace trace);
    Task<ReasoningTrace?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<ReasoningTrace>> GetBySessionIdAsync(Guid sessionId);
}
