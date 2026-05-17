using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonKnowledgeRecordRepository : IKnowledgeRecordRepository
{
    private readonly string _storageFolder;
    private const string FileName = "knowledge-records.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonKnowledgeRecordRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<IReadOnlyList<KnowledgeRecord>> GetByProjectIdAsync(Guid projectId)
    {
        var all = await LoadAllAsync();
        return all.Where(r => r.ProjectId == projectId).ToList();
    }

    public async Task<KnowledgeRecord?> GetByIdAsync(Guid id)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(r => r.Id == id);
    }

    public async Task SaveAsync(KnowledgeRecord record)
    {
        var all = await LoadAllAsync();
        var list = all.ToList();
        var index = list.FindIndex(r => r.Id == record.Id);
        if (index >= 0)
            list[index] = record;
        else
            list.Add(record);
        await PersistAsync(list);
    }

    public async Task DeleteAsync(Guid id)
    {
        var all = await LoadAllAsync();
        await PersistAsync(all.Where(r => r.Id != id));
    }

    private async Task<IReadOnlyList<KnowledgeRecord>> LoadAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path)) return [];
        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<KnowledgeRecordDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToRecord()).ToList();
    }

    private async Task PersistAsync(IEnumerable<KnowledgeRecord> records)
    {
        var dtos = records.Select(KnowledgeRecordDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

internal class KnowledgeRecordDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public KnowledgeRecordStatus Status { get; set; }
    public KnowledgeRecordType RecordType { get; set; }
    public Guid? ParentId { get; set; }
    public int FormatVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static KnowledgeRecordDto From(KnowledgeRecord r) => new()
    {
        Id = r.Id, ProjectId = r.ProjectId, Title = r.Title, Description = r.Description,
        Status = r.Status, RecordType = r.RecordType, ParentId = r.ParentId,
        FormatVersion = r.FormatVersion, CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt
    };

    public KnowledgeRecord ToRecord()
    {
        // Format migration: currently version 1 is the only version
        return KnowledgeRecord.Reconstitute(
            Id, ProjectId, Title, Description, Status, RecordType, ParentId,
            FormatVersion == 0 ? KnowledgeRecord.CurrentFormatVersion : FormatVersion,
            CreatedAt, UpdatedAt);
    }
}
