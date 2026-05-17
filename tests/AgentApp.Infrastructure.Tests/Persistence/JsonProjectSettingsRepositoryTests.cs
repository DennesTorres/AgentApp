using AgentApp.Domain.Settings;
using AgentApp.Infrastructure.Persistence;

namespace AgentApp.Infrastructure.Tests.Persistence;

public class JsonProjectSettingsRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonProjectSettingsRepository _sut;

    public JsonProjectSettingsRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonProjectSettingsRepository(_testFolder);
    }

    [Fact]
    public async Task GetByProjectIdAsync_NoneStored_ReturnsNull()
    {
        var result = await _sut.GetByProjectIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_Settings_CanBeRetrievedByProjectId()
    {
        var projectId = Guid.NewGuid();
        var settings = ProjectSettings.Create(projectId);
        settings.SetMaxGateRetries(5);

        await _sut.SaveAsync(settings);
        var result = await _sut.GetByProjectIdAsync(projectId);

        Assert.NotNull(result);
        Assert.Equal(5, result.MaxGateRetries);
    }

    [Fact]
    public async Task SaveAsync_UpdatedSettings_OverwritesExisting()
    {
        var projectId = Guid.NewGuid();
        var settings = ProjectSettings.Create(projectId);
        await _sut.SaveAsync(settings);

        settings.SetTokenThreshold(40000);
        await _sut.SaveAsync(settings);

        var result = await _sut.GetByProjectIdAsync(projectId);
        Assert.Equal(40000, result!.TokenThresholdForContextReset);
    }

    [Fact]
    public async Task GetByProjectIdAsync_DifferentProjects_ReturnsCorrectOne()
    {
        var project1 = Guid.NewGuid();
        var project2 = Guid.NewGuid();
        var s1 = ProjectSettings.Create(project1);
        s1.SetMaxGateRetries(5);
        var s2 = ProjectSettings.Create(project2);
        s2.SetMaxGateRetries(10);

        await _sut.SaveAsync(s1);
        await _sut.SaveAsync(s2);

        var result = await _sut.GetByProjectIdAsync(project1);
        Assert.Equal(5, result!.MaxGateRetries);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
