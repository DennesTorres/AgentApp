using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonMdFileRepository : IMdFileRepository
{
    private readonly string _storageFolder;
    private const string FileName = "md-files.json";

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonMdFileRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<MdFile?> GetByNameAsync(string name, MdFileScope scope, Guid? projectId)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(f =>
            f.Name == name &&
            f.Scope == scope &&
            f.ProjectId == projectId);
    }

    public async Task<IReadOnlyList<MdFile>> GetAllAsync(MdFileScope scope, Guid? projectId)
    {
        var all = await LoadAllAsync();
        return all.Where(f => f.Scope == scope && f.ProjectId == projectId).ToList();
    }

    public async Task SaveAsync(MdFile file)
    {
        var all = await LoadAllAsync();
        var list = all.ToList();
        var index = list.FindIndex(f => f.Id == file.Id);
        if (index >= 0)
            list[index] = file;
        else
            list.Add(file);
        await PersistAsync(list);
    }

    public async Task DeleteAsync(Guid id)
    {
        var all = await LoadAllAsync();
        var list = all.Where(f => f.Id != id).ToList();
        await PersistAsync(list);
    }

    private async Task<IReadOnlyList<MdFile>> LoadAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path))
            return [];

        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<MdFileDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToMdFile()).ToList();
    }

    private async Task PersistAsync(IEnumerable<MdFile> files)
    {
        var dtos = files.Select(MdFileDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

internal class MdFileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MdFileScope Scope { get; set; }
    public Guid? ProjectId { get; set; }
    public bool IsTechnology { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static MdFileDto From(MdFile f) => new()
    {
        Id = f.Id, Name = f.Name, Content = f.Content, Scope = f.Scope,
        ProjectId = f.ProjectId, IsTechnology = f.IsTechnology,
        CreatedAt = f.CreatedAt, UpdatedAt = f.UpdatedAt
    };

    public MdFile ToMdFile() =>
        MdFile.Reconstitute(Id, Name, Content, Scope, ProjectId, IsTechnology, CreatedAt, UpdatedAt);
}
