using AgentApp.Domain.Filters;
using AgentApp.Domain.Rules;

namespace AgentApp.Domain.Interfaces;

public interface IFilterRuleRepository
{
    Task<IReadOnlyList<FilterRule>> GetAllAsync(MdFileScope scope, Guid? projectId);
    Task SaveAsync(FilterRule rule);
    Task DeleteAsync(Guid id);
}
