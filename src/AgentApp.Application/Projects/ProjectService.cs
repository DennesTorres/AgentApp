using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;

namespace AgentApp.Application.Projects;

public class ProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISettingsRepository _settingsRepository;

    public ProjectService(IProjectRepository projectRepository, ISettingsRepository settingsRepository)
    {
        _projectRepository = projectRepository;
        _settingsRepository = settingsRepository;
    }

    public async Task<Project> CreateProjectAsync(string name, string folderName,
        string projectFolderPath, string description = "", string purpose = "")
    {
        var project = Project.Create(name, folderName, projectFolderPath, description, purpose);
        await _projectRepository.SaveAsync(project);
        return project;
    }

    public async Task<IReadOnlyList<Project>> GetAllProjectsAsync()
    {
        return await _projectRepository.GetAllAsync();
    }

    public async Task<Project> GetProjectByIdAsync(Guid projectId)
    {
        return await _projectRepository.GetByIdAsync(projectId)
            ?? throw new DomainNotFoundException($"Project '{projectId}' not found.");
    }
}
