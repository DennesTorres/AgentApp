using AgentApp.Application.Onboarding;
using AgentApp.Domain.Settings;
using AgentApp.Infrastructure.Persistence;

namespace AgentApp.Application.Tests.Onboarding;

public class OnboardingServiceTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly JsonProjectRepository _projectRepo;
    private readonly JsonSettingsRepository _settingsRepo;

    public OnboardingServiceTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), $"OnboardingTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempFolder);
        _projectRepo = new JsonProjectRepository(_tempFolder);
        _settingsRepo = new JsonSettingsRepository(_tempFolder);
    }

    private OnboardingService BuildService() => new(_projectRepo, _settingsRepo);

    [Fact]
    public async Task IsOnboardingRequired_WhenNoProjects_ReturnsTrue()
    {
        var service = BuildService();
        Assert.True(await service.IsOnboardingRequiredAsync());
    }

    [Fact]
    public async Task IsOnboardingRequired_WhenProjectExists_ReturnsFalse()
    {
        var project = Domain.Projects.Project.Create("TestProject", "/code/TestProject");
        await _projectRepo.SaveAsync(project);

        var service = BuildService();
        Assert.False(await service.IsOnboardingRequiredAsync());
    }

    [Fact]
    public async Task IsSourceControlRootSet_WhenEmpty_ReturnsFalse()
    {
        var service = BuildService();
        Assert.False(await service.IsSourceControlRootSetAsync());
    }

    [Fact]
    public async Task IsSourceControlRootSet_WhenSet_ReturnsTrue()
    {
        var settings = new GlobalSettings { SourceControlRoot = @"C:\Code" };
        await _settingsRepo.SaveGlobalSettingsAsync(settings);

        var service = BuildService();
        Assert.True(await service.IsSourceControlRootSetAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }
}
