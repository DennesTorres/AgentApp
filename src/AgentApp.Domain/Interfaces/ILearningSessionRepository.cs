using AgentApp.Domain.Learning;

namespace AgentApp.Domain.Interfaces;

public interface ILearningSessionRepository
{
    Task<LearningSession?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<LearningSession>> GetAllPendingAsync();
    Task SaveAsync(LearningSession session);
}
