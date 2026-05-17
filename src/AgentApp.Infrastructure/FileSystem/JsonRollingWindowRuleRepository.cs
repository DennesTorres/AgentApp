using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonRollingWindowRuleRepository : IRollingWindowRuleRepository
{
    private readonly string _storageFolder;
    private const string FileName = "rolling-window-rules.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonRollingWindowRuleRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<RollingWindowRule?> GetByFileTypeAsync(string fileType, MdFileScope scope, Guid? projectId)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(r =>
            r.FileType == fileType && r.Scope == scope && r.ProjectId == projectId);
    }

    public async Task SaveAsync(RollingWindowRule rule)
    {
        var all = await LoadAllAsync();
        var list = all.ToList();
        var index = list.FindIndex(r => r.Id == rule.Id);
        if (index >= 0)
            list[index] = rule;
        else
            list.Add(rule);
        await PersistAsync(list);
    }

    public async Task DeleteAsync(Guid id)
    {
        var all = await LoadAllAsync();
        await PersistAsync(all.Where(r => r.Id != id));
    }

    private async Task<IReadOnlyList<RollingWindowRule>> LoadAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path)) return [];
        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<RollingWindowRuleDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToRule()).ToList();
    }

    private async Task PersistAsync(IEnumerable<RollingWindowRule> rules)
    {
        var dtos = rules.Select(RollingWindowRuleDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

internal class RollingWindowRuleDto
{
    public Guid Id { get; set; }
    public string FileType { get; set; } = string.Empty;
    public int MaxSizeChars { get; set; }
    public int RetentionDays { get; set; }
    public MdFileScope Scope { get; set; }
    public Guid? ProjectId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static RollingWindowRuleDto From(RollingWindowRule r) => new()
    {
        Id = r.Id, FileType = r.FileType, MaxSizeChars = r.MaxSizeChars,
        RetentionDays = r.RetentionDays, Scope = r.Scope, ProjectId = r.ProjectId, CreatedAt = r.CreatedAt
    };

    public RollingWindowRule ToRule() =>
        RollingWindowRule.Reconstitute(Id, FileType, MaxSizeChars, RetentionDays, Scope, ProjectId, CreatedAt);
}
