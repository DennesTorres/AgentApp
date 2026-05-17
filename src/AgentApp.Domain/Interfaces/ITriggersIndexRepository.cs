using AgentApp.Domain.Rules;

namespace AgentApp.Domain.Interfaces;

public interface ITriggersIndexRepository
{
    Task<TriggersIndex> GetGlobalAsync();
    Task<TriggersIndex> GetForProjectAsync(Guid projectId);
    Task SaveAsync(TriggersIndex index);
}
