using AgentApp.Domain.Settings;
using AgentApp.Infrastructure.Persistence;

namespace AgentApp.Infrastructure.Tests.Persistence;

public class JsonSettingsRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonSettingsRepository _sut;

    public JsonSettingsRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonSettingsRepository(_testFolder);
    }

    [Fact]
    public async Task GetGlobalSettingsAsync_FirstLoad_ReturnsDefaultSettings()
    {
        var settings = await _sut.GetGlobalSettingsAsync();

        Assert.NotNull(settings);
        Assert.Equal(string.Empty, settings.RootProjectFolderPath);
    }

    [Fact]
    public async Task SaveGlobalSettingsAsync_Persists_AndCanBeReloaded()
    {
        var settings = new GlobalSettings { RootProjectFolderPath = @"C:\MyProjects" };

        await _sut.SaveGlobalSettingsAsync(settings);
        var reloaded = await _sut.GetGlobalSettingsAsync();

        Assert.Equal(@"C:\MyProjects", reloaded.RootProjectFolderPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
