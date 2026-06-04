using System.Collections.ObjectModel;
using AgentApp.Application.Projects;
using AgentApp.Domain.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels.Projects;

public partial class ProjectListViewModel : ObservableObject
{
    private readonly ProjectService _projectService;
    private readonly IFilePermissionGate _fileGate;
    private readonly IScaffoldService _scaffoldService;

    [ObservableProperty]
    private string _activeProjectName = string.Empty;

    public ObservableCollection<ProjectItemViewModel> Projects { get; } = [];

    public ProjectListViewModel(ProjectService projectService, IFilePermissionGate fileGate, IScaffoldService scaffoldService)
    {
        _projectService = projectService;
        _fileGate = fileGate;
        _scaffoldService = scaffoldService;
        // C-062: refresh when a project is created via chat
        _projectService.ProjectCreated += (_, _) => _ = LoadProjectsAsync();
        _ = LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        Projects.Clear();
        foreach (var p in projects.OrderByDescending(x => x.CreatedAt))
            Projects.Add(new ProjectItemViewModel(p.Id, p.Name, p.CreatedAt));
    }

    [RelayCommand]
    private void SelectProject(ProjectItemViewModel item)
    {
        ActiveProjectName = item.Name;
        _fileGate.SetProjectRoots(
            _scaffoldService.GetAgentFolderPath(item.Name),
            item.Name); // code root stored on project; simplified here
    }
}

public class ProjectItemViewModel
{
    public Guid Id { get; }
    public string Name { get; }
    public string LastAccessed { get; }

    public ProjectItemViewModel(Guid id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        LastAccessed = createdAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
    }
}
