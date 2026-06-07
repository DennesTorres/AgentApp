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

    // C-062: raised after a project is successfully created so the Projects tab can refresh
    public event EventHandler? ProjectCreated;
    // C-092: raised when project settings change (e.g. Always Allow path granted)
    public event EventHandler? ProjectSettingsUpdated;
    public void RaiseSettingsUpdated() => ProjectSettingsUpdated?.Invoke(this, EventArgs.Empty);

    public async Task<Project> CreateProjectAsync(string name, string folderName,
        string projectFolderPath, string description = "", string purpose = "")
    {
        var project = Project.Create(name, folderName, projectFolderPath, description, purpose);
        await _projectRepository.SaveAsync(project);
        ProjectCreated?.Invoke(this, EventArgs.Empty);
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
