using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Settings;

namespace AgentApp.Infrastructure.Persistence;

public class JsonProjectSettingsRepository : IProjectSettingsRepository
{
    private readonly string _storageFolder;
    private const string FileName = "project-settings.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonProjectSettingsRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<ProjectSettings?> GetByProjectIdAsync(Guid projectId)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(s => s.ProjectId == projectId);
    }

    public async Task SaveAsync(ProjectSettings settings)
    {
        var all = await LoadAllAsync();
        var list = all.ToList();
        var index = list.FindIndex(s => s.ProjectId == settings.ProjectId);
        if (index >= 0)
            list[index] = settings;
        else
            list.Add(settings);
        await PersistAsync(list);
    }

    private async Task<IReadOnlyList<ProjectSettings>> LoadAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path)) return [];
        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<ProjectSettingsDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToSettings()).ToList();
    }

    private async Task PersistAsync(IEnumerable<ProjectSettings> settings)
    {
        var dtos = settings.Select(ProjectSettingsDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

internal class ProjectSettingsDto
{
    public Guid ProjectId { get; set; }
    public int? MaxGateRetries { get; set; }
    public bool? RequireUserConfirmationForInternalLearning { get; set; }
    public bool? RequireUserConfirmationForFindingsExtraction { get; set; }
    public int? TokenThresholdForContextReset { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<string> AlwaysAllowedPaths { get; set; } = [];

    public static ProjectSettingsDto From(ProjectSettings s) => new()
    {
        ProjectId = s.ProjectId,
        MaxGateRetries = s.MaxGateRetries,
        RequireUserConfirmationForInternalLearning = s.RequireUserConfirmationForInternalLearning,
        RequireUserConfirmationForFindingsExtraction = s.RequireUserConfirmationForFindingsExtraction,
        TokenThresholdForContextReset = s.TokenThresholdForContextReset,
        CreatedAt = s.CreatedAt,
        AlwaysAllowedPaths = s.AlwaysAllowedPaths.ToList()
    };

    public ProjectSettings ToSettings() =>
        ProjectSettings.Reconstitute(ProjectId, MaxGateRetries,
            RequireUserConfirmationForInternalLearning, RequireUserConfirmationForFindingsExtraction,
            TokenThresholdForContextReset, CreatedAt, AlwaysAllowedPaths);
}
