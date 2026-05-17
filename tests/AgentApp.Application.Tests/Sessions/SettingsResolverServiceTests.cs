using AgentApp.Application.Settings;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Settings;
using NSubstitute;

namespace AgentApp.Application.Tests.Sessions;

public class SettingsResolverServiceTests
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IProjectSettingsRepository _projectSettingsRepository;
    private readonly SettingsResolverService _sut;

    public SettingsResolverServiceTests()
    {
        _settingsRepository = Substitute.For<ISettingsRepository>();
        _projectSettingsRepository = Substitute.For<IProjectSettingsRepository>();
        _sut = new SettingsResolverService(_settingsRepository, _projectSettingsRepository);

        _settingsRepository.GetGlobalSettingsAsync().Returns(new GlobalSettings
        {
            MaxGateRetries = 3,
            RequireUserConfirmationForInternalLearning = false,
            RequireUserConfirmationForFindingsExtraction = false,
            TokenThresholdForContextReset = 80000
        });
        _projectSettingsRepository.GetByProjectIdAsync(Arg.Any<Guid>())
            .Returns((ProjectSettings?)null);
    }

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
        _projectSettingsRepository.GetByProjectIdAsync(projectId).Returns(projectSettings);

        var result = await _sut.ResolveAsync(projectId);

        Assert.Equal(7, result.MaxGateRetries);
        Assert.Equal(80000, result.TokenThresholdForContextReset); // still global
    }

    [Fact]
    public async Task ResolveAsync_ProjectHasNoOverrides_FallsBackToGlobal()
    {
        var projectId = Guid.NewGuid();
        var projectSettings = ProjectSettings.Create(projectId); // all nulls
        _projectSettingsRepository.GetByProjectIdAsync(projectId).Returns(projectSettings);

        var result = await _sut.ResolveAsync(projectId);

        Assert.Equal(3, result.MaxGateRetries);
        Assert.Equal(80000, result.TokenThresholdForContextReset);
    }
}
