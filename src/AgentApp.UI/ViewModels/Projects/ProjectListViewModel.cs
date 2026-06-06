using System.Collections.ObjectModel;
using AgentApp.Application.Projects;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels.Projects;

public partial class ProjectListViewModel : ObservableObject
{
    private readonly ProjectService _projectService;
    private readonly IProjectSettingsRepository _projectSettingsRepo;

    // C-086: fired when user clicks Open on a project row
    public event Action<Guid>? OpenProjectRequested;

    public ObservableCollection<ProjectItemViewModel> Projects { get; } = [];

    public ProjectListViewModel(ProjectService projectService, IProjectSettingsRepository projectSettingsRepo)
    {
        _projectService = projectService;
        _projectSettingsRepo = projectSettingsRepo;
        // C-062: refresh when a project is created via chat
        _projectService.ProjectCreated += (_, _) => _ = LoadProjectsAsync();
        _ = LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        Projects.Clear();
        foreach (var p in projects.OrderByDescending(x => x.CreatedAt))
        {
            var settings = await _projectSettingsRepo.GetByProjectIdAsync(p.Id);
            var allowedPaths = settings?.AlwaysAllowedPaths ?? [];
            Projects.Add(new ProjectItemViewModel(p, allowedPaths));
        }
    }

    // C-086: OPEN button — navigate to most recent session for this project (or create one)
    [RelayCommand]
    private void OpenProject(ProjectItemViewModel item) => OpenProjectRequested?.Invoke(item.Id);
}

public partial class ProjectItemViewModel : ObservableObject
{
    public Guid Id { get; }
    public string Name { get; }
    public string LastAccessed { get; }
    public string Purpose { get; }
    public string AgentFolder { get; }
    public string CodeFolder { get; }
    public IReadOnlyList<string> AlwaysAllowedPaths { get; }

    [ObservableProperty]
    private bool _isExpanded;

    public ProjectItemViewModel(Project project, IReadOnlyList<string> allowedPaths)
    {
        Id = project.Id;
        Name = project.Name;
        LastAccessed = project.CreatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        Purpose = string.IsNullOrWhiteSpace(project.Purpose) ? "(not set)" : project.Purpose;
        AgentFolder = project.ControlFolderPath;
        CodeFolder = string.IsNullOrWhiteSpace(project.ProjectFolderPath) ? "(not set)" : project.ProjectFolderPath;
        AlwaysAllowedPaths = allowedPaths;
    }

    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;
}
