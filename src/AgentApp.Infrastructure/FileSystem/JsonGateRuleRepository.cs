using System.Text.Json;
using AgentApp.Domain.Gates;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonGateRuleRepository : IGateRuleRepository
{
    private readonly string _storageFolder;
    private const string FileName = "gate-rules.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonGateRuleRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<GateRule?> GetByNameAsync(string name)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(r => r.Name == name);
    }

    public async Task<IReadOnlyList<GateRule>> GetAllAsync() => await LoadAllAsync();

    public async Task SaveAsync(GateRule rule)
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

    private async Task<IReadOnlyList<GateRule>> LoadAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path))
            return [];

        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<GateRuleDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToGateRule()).ToList();
    }

    private async Task PersistAsync(IEnumerable<GateRule> rules)
    {
        var dtos = rules.Select(GateRuleDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

internal class GateRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RuleText { get; set; } = string.Empty;
    public List<string> RequiredOutputKeys { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }

    public static GateRuleDto From(GateRule r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        RuleText = r.RuleText,
        RequiredOutputKeys = r.RequiredOutputKeys.ToList(),
        CreatedAt = r.CreatedAt
    };

    public GateRule ToGateRule() =>
        GateRule.Reconstitute(Id, Name, RuleText, RequiredOutputKeys, CreatedAt);
}
