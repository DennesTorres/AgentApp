using System.Text.Json;
using AgentApp.Domain.Filters;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonFilterRuleRepository : IFilterRuleRepository
{
    private readonly string _storageFolder;
    private const string FileName = "filter-rules.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonFilterRuleRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<IReadOnlyList<FilterRule>> GetAllAsync(MdFileScope scope, Guid? projectId)
    {
        var all = await LoadAllAsync();
        return all.Where(r => r.Scope == scope && r.ProjectId == projectId).ToList();
    }

    public async Task SaveAsync(FilterRule rule)
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

    private async Task<IReadOnlyList<FilterRule>> LoadAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path))
            return [];

        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<FilterRuleDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToFilterRule()).ToList();
    }

    private async Task PersistAsync(IEnumerable<FilterRule> rules)
    {
        var dtos = rules.Select(FilterRuleDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

internal class FilterRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public FilterTarget Target { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public FilterTransformation Transformation { get; set; }
    public int? MaxLength { get; set; }
    public MdFileScope Scope { get; set; }
    public Guid? ProjectId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static FilterRuleDto From(FilterRule r) => new()
    {
        Id = r.Id, Name = r.Name, Target = r.Target, MessageType = r.MessageType,
        Transformation = r.Transformation, MaxLength = r.MaxLength,
        Scope = r.Scope, ProjectId = r.ProjectId, CreatedAt = r.CreatedAt
    };

    public FilterRule ToFilterRule() =>
        FilterRule.Reconstitute(Id, Name, Target, MessageType, Transformation, MaxLength, Scope, ProjectId, CreatedAt);
}
