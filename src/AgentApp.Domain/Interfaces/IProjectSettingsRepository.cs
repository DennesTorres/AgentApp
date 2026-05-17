using AgentApp.Domain.Settings;

namespace AgentApp.Domain.Interfaces;

public interface IProjectSettingsRepository
{
    Task<ProjectSettings?> GetByProjectIdAsync(Guid projectId);
    Task SaveAsync(ProjectSettings settings);
}
