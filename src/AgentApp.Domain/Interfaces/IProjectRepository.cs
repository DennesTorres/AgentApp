using AgentApp.Domain.Projects;

namespace AgentApp.Domain.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid projectId);
    Task SaveAsync(Project project);
    Task<IReadOnlyList<Project>> GetAllAsync();
    Task DeleteAsync(Guid projectId);
}
