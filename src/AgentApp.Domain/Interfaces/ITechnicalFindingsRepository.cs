using AgentApp.Domain.Findings;

namespace AgentApp.Domain.Interfaces;

public interface ITechnicalFindingsRepository
{
    Task<IReadOnlyList<TechnicalFinding>> GetAllAsync();
    Task SaveAsync(TechnicalFinding finding);
}
