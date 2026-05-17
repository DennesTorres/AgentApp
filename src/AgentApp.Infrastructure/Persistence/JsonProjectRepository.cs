using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;

namespace AgentApp.Infrastructure.Persistence;

public class JsonProjectRepository : IProjectRepository
{
    private readonly string _storageFolder;
    private const string FileName = "projects.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public JsonProjectRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<Project?> GetByIdAsync(Guid projectId)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(p => p.Id == projectId);
    }

    public async Task SaveAsync(Project project)
    {
        var all = await LoadAllAsync();
        var list = all.ToList();
        var index = list.FindIndex(p => p.Id == project.Id);
        if (index >= 0)
            list[index] = project;
        else
            list.Add(project);

        await PersistAsync(list);
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync()
    {
        return await LoadAllAsync();
    }

    public async Task DeleteAsync(Guid projectId)
    {
        var all = await LoadAllAsync();
        var list = all.Where(p => p.Id != projectId).ToList();
        await PersistAsync(list);
    }

    private async Task<IReadOnlyList<Project>> LoadAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path))
            return [];

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<ProjectDto>>(json, Options)
            ?.Select(dto => dto.ToProject())
            .ToList()
            ?? [];
    }

    private async Task PersistAsync(IEnumerable<Project> projects)
    {
        var dtos = projects.Select(ProjectDto.FromProject).ToList();
        var json = JsonSerializer.Serialize(dtos, Options);
        await File.WriteAllTextAsync(FilePath(), json);
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

// Internal DTO for JSON serialization — Project has private setters
internal class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProjectFolderPath { get; set; } = string.Empty;
    public string ControlFolderPath { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public static ProjectDto FromProject(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        ProjectFolderPath = p.ProjectFolderPath,
        ControlFolderPath = p.ControlFolderPath,
        CreatedAt = p.CreatedAt
    };

    public Project ToProject()
    {
        // Reconstruct using reflection-free approach: use internal factory
        return Project.Reconstitute(Id, Name, ProjectFolderPath, ControlFolderPath, CreatedAt);
    }
}
