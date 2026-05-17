using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Settings;

namespace AgentApp.Infrastructure.Persistence;

public class JsonSettingsRepository : ISettingsRepository
{
    private readonly string _storageFolder;
    private const string FileName = "global-settings.json";

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonSettingsRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<GlobalSettings> GetGlobalSettingsAsync()
    {
        var path = FilePath();
        if (!File.Exists(path))
            return new GlobalSettings();

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<GlobalSettings>(json, Options) ?? new GlobalSettings();
    }

    public async Task SaveGlobalSettingsAsync(GlobalSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, Options);
        await File.WriteAllTextAsync(FilePath(), json);
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}
