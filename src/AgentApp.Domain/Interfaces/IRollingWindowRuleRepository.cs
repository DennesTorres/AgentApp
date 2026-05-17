using AgentApp.Domain.Rules;

namespace AgentApp.Domain.Interfaces;

public interface IRollingWindowRuleRepository
{
    Task<RollingWindowRule?> GetByFileTypeAsync(string fileType, MdFileScope scope, Guid? projectId);
    Task SaveAsync(RollingWindowRule rule);
    Task DeleteAsync(Guid id);
}
