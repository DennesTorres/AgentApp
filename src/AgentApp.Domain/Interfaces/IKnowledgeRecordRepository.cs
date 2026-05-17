using AgentApp.Domain.Projects;

namespace AgentApp.Domain.Interfaces;

public interface IKnowledgeRecordRepository
{
    Task<IReadOnlyList<KnowledgeRecord>> GetByProjectIdAsync(Guid projectId);
    Task<KnowledgeRecord?> GetByIdAsync(Guid id);
    Task SaveAsync(KnowledgeRecord record);
    Task DeleteAsync(Guid id);
}
