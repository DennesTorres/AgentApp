using System.Text.Json;
using AgentApp.Domain.Findings;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonTechnicalFindingsRepository : ITechnicalFindingsRepository
{
    private readonly string _storageFolder;
    private const string FileName = "technical-findings.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonTechnicalFindingsRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<IReadOnlyList<TechnicalFinding>> GetAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path))
            return [];

        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<TechnicalFindingDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToTechnicalFinding()).ToList();
    }

    public async Task SaveAsync(TechnicalFinding finding)
    {
        var all = (await GetAllAsync()).ToList();
        all.Add(finding);
        var dtos = all.Select(TechnicalFindingDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

internal class TechnicalFindingDto
{
    public Guid Id { get; set; }
    public string Technology { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset ExtractedAt { get; set; }

    public static TechnicalFindingDto From(TechnicalFinding f) => new()
    { Id = f.Id, Technology = f.Technology, Content = f.Content, ExtractedAt = f.ExtractedAt };

    public TechnicalFinding ToTechnicalFinding() =>
        TechnicalFinding.Reconstitute(Id, Technology, Content, ExtractedAt);
}
