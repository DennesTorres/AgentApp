using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;

namespace AgentApp.Infrastructure.Persistence;

public class JsonProjectRepository : IProjectRepository
{
    private readonly string _towerRoot;
    private const string FileName = "project.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public JsonProjectRepository(string towerRoot)
    {
        _towerRoot = towerRoot;
        Directory.CreateDirectory(towerRoot);
    }

    public async Task<Project?> GetByIdAsync(Guid projectId)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(p => p.Id == projectId);
    }

    public async Task SaveAsync(Project project)
    {
        var projectFolder = Path.Combine(_towerRoot, project.FolderName);
        Directory.CreateDirectory(projectFolder);

        var dto = ProjectDto.FromProject(project);
        var json = JsonSerializer.Serialize(dto, Options);
        await File.WriteAllTextAsync(Path.Combine(projectFolder, FileName), json);
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync()
    {
        if (!Directory.Exists(_towerRoot))
            return [];

        var projects = new List<Project>();
        foreach (var dir in Directory.GetDirectories(_towerRoot))
        {
            var path = Path.Combine(dir, FileName);
            if (!File.Exists(path))
                continue;

            var json = await File.ReadAllTextAsync(path);
            var dto = JsonSerializer.Deserialize<ProjectDto>(json, Options);
            if (dto is not null)
                projects.Add(dto.ToProject());
        }
        return projects;
    }

    public async Task DeleteAsync(Guid projectId)
    {
        var all = await GetAllAsync();
        var project = all.FirstOrDefault(p => p.Id == projectId);
        if (project is null)
            return;

        var projectFolder = Path.Combine(_towerRoot, project.FolderName);
        var path = Path.Combine(projectFolder, FileName);
        if (File.Exists(path))
            File.Delete(path);
    }
}

// Internal DTO for JSON serialization — Project has private setters
internal class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string ProjectFolderPath { get; set; } = string.Empty;
    public string ControlFolderPath { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public static ProjectDto FromProject(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        FolderName = p.FolderName,
        Description = p.Description,
        Purpose = p.Purpose,
        ProjectFolderPath = p.ProjectFolderPath,
        ControlFolderPath = p.ControlFolderPath,
        CreatedAt = p.CreatedAt
    };

    public Project ToProject() =>
        Project.Reconstitute(Id, Name, FolderName, Description, Purpose,
            ProjectFolderPath, ControlFolderPath, CreatedAt);
}
