using AgentApp.Application.Settings;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Settings;
using AgentApp.Infrastructure.Persistence;

namespace AgentApp.Application.Tests.Sessions;

public class SettingsResolverServiceTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IProjectSettingsRepository _projectSettingsRepository;
    private readonly SettingsResolverService _sut;

    public SettingsResolverServiceTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempFolder);
        _settingsRepository = new JsonSettingsRepository(_tempFolder);
        _projectSettingsRepository = new JsonProjectSettingsRepository(_tempFolder);
        _sut = new SettingsResolverService(_settingsRepository, _projectSettingsRepository);

        _settingsRepository.SaveGlobalSettingsAsync(new GlobalSettings
        {
            MaxGateRetries = 3,
            RequireUserConfirmationForInternalLearning = false,
            RequireUserConfirmationForFindingsExtraction = false,
            TokenThresholdForContextReset = 80000
        }).GetAwaiter().GetResult();
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task ResolveAsync_NoProjectId_ReturnsGlobalDefaults()
    {
        var result = await _sut.ResolveAsync(null);

        Assert.Equal(3, result.MaxGateRetries);
        Assert.Equal(80000, result.TokenThresholdForContextReset);
        Assert.False(result.RequireUserConfirmationForInternalLearning);
    }

    [Fact]
    public async Task ResolveAsync_ProjectOverridesMaxGateRetries_UsesProjectValue()
    {
        var projectId = Guid.NewGuid();
        var projectSettings = ProjectSettings.Create(projectId);
        projectSettings.SetMaxGateRetries(7);
        await _projectSettingsRepository.SaveAsync(projectSettings);

        var result = await _sut.ResolveAsync(projectId);

        Assert.Equal(7, result.MaxGateRetries);
        Assert.Equal(80000, result.TokenThresholdForContextReset);
    }

    [Fact]
    public async Task ResolveAsync_ProjectHasNoOverrides_FallsBackToGlobal()
    {
        var projectId = Guid.NewGuid();
        var projectSettings = ProjectSettings.Create(projectId);
        await _projectSettingsRepository.SaveAsync(projectSettings);

        var result = await _sut.ResolveAsync(projectId);

        Assert.Equal(3, result.MaxGateRetries);
        Assert.Equal(80000, result.TokenThresholdForContextReset);
    }
}
