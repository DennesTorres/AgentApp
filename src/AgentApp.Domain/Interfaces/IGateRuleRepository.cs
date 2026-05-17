using AgentApp.Domain.Gates;

namespace AgentApp.Domain.Interfaces;

public interface IGateRuleRepository
{
    Task<GateRule?> GetByNameAsync(string name);
    Task<IReadOnlyList<GateRule>> GetAllAsync();
    Task SaveAsync(GateRule rule);
    Task DeleteAsync(Guid id);
}
