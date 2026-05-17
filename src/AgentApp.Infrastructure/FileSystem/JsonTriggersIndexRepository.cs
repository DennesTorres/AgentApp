using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonTriggersIndexRepository : ITriggersIndexRepository
{
    private readonly string _storageFolder;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonTriggersIndexRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<TriggersIndex> GetGlobalAsync()
    {
        var path = GlobalFilePath();
        if (!File.Exists(path))
            return TriggersIndex.CreateGlobal();

        var json = await File.ReadAllTextAsync(path);
        var dto = JsonSerializer.Deserialize<TriggersIndexDto>(json, Options);
        return dto?.ToTriggersIndex() ?? TriggersIndex.CreateGlobal();
    }

    public async Task<TriggersIndex> GetForProjectAsync(Guid projectId)
    {
        var path = ProjectFilePath(projectId);
        if (!File.Exists(path))
            return TriggersIndex.CreateForProject(projectId);

        var json = await File.ReadAllTextAsync(path);
        var dto = JsonSerializer.Deserialize<TriggersIndexDto>(json, Options);
        return dto?.ToTriggersIndex() ?? TriggersIndex.CreateForProject(projectId);
    }

    public async Task SaveAsync(TriggersIndex index)
    {
        var dto = TriggersIndexDto.From(index);
        var path = index.ProjectId.HasValue
            ? ProjectFilePath(index.ProjectId.Value)
            : GlobalFilePath();
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(dto, Options));
    }

    private string GlobalFilePath() => Path.Combine(_storageFolder, "global-triggers.json");
    private string ProjectFilePath(Guid projectId) => Path.Combine(_storageFolder, $"project-{projectId}-triggers.json");
}

internal class TriggersIndexDto
{
    public MdFileScope Scope { get; set; }
    public Guid? ProjectId { get; set; }
    public Dictionary<string, string> Entries { get; set; } = new();

    public static TriggersIndexDto From(TriggersIndex index) => new()
    {
        Scope = index.Scope,
        ProjectId = index.ProjectId,
        Entries = index.Entries.ToDictionary(kv => kv.Key, kv => kv.Value)
    };

    public TriggersIndex ToTriggersIndex()
    {
        var index = Scope == MdFileScope.Global
            ? TriggersIndex.CreateGlobal()
            : TriggersIndex.CreateForProject(ProjectId!.Value);

        // Group by file name so each AddEntry call handles one file's triggers
        foreach (var group in Entries.GroupBy(kv => kv.Value))
            index.AddEntry(group.Key, group.Select(kv => kv.Key));

        return index;
    }
}
