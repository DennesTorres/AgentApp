using AgentApp.Application.Agent;
using AgentApp.Application.Chat;
using AgentApp.Application.FileSystem;
using AgentApp.Application.Projects;
using AgentApp.Domain.Chat;
using AgentApp.Infrastructure.Persistence;
using AgentApp.Infrastructure.Scaffold;

namespace AgentApp.Application.Tests.Chat;

public class StartProjectActionProviderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonSettingsRepository _settingsRepo;
    private readonly JsonProjectRepository _projectRepo;
    private readonly ScaffoldService _scaffoldService;
    private readonly ProjectService _projectService;
    private readonly AgentContextService _contextService;
    private readonly FileSessionGate _fileGate;

    public StartProjectActionProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);

        _settingsRepo = new JsonSettingsRepository(_tempDir);
        _projectRepo = new JsonProjectRepository(_tempDir);
        _scaffoldService = new ScaffoldService();
        _projectService = new ProjectService(_projectRepo, _settingsRepo);
        _contextService = new AgentContextService();
        _fileGate = new FileSessionGate();
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private StartProjectActionProvider Build() =>
        new(_projectService, _scaffoldService, _settingsRepo, _contextService, _fileGate);

    [Fact]
    public void CanHandle_StartProjectCommand_ReturnsTrue()
    {
        var provider = Build();
        Assert.True(provider.CanHandle(new StartProjectCommand("App", "app", "An app")));
    }

    [Fact]
    public void CanHandle_OtherCommand_ReturnsFalse()
    {
        var provider = Build();
        Assert.False(provider.CanHandle(new FolderSelectCommand("reason")));
    }

    [Fact]
    public async Task HandleAsync_CreatesProjectInRepository()
    {
        var provider = Build();

        await provider.HandleAsync(new StartProjectCommand("MyApp", "my-app", "A test app"));

        var projects = await _projectRepo.GetAllAsync();
        Assert.Single(projects);
        Assert.Equal("MyApp", projects[0].Name);
        Assert.Equal("my-app", projects[0].FolderName);
    }

    [Fact]
    public async Task HandleAsync_SetsProjectOnContext()
    {
        var provider = Build();

        await provider.HandleAsync(new StartProjectCommand("MyApp", "my-app", "A test app"));

        var context = _contextService.GetCurrent();
        Assert.True(context.HasProject);
        Assert.Equal("MyApp", context.CurrentProject!.Name);
    }

    [Fact]
    public async Task HandleAsync_NoSourceControlRoot_CodeFolderIsEmpty()
    {
        var provider = Build();

        await provider.HandleAsync(new StartProjectCommand("MyApp", "my-app", "intent"));

        var context = _contextService.GetCurrent();
        Assert.False(context.IsFullyInitialized);
        Assert.True(string.IsNullOrEmpty(context.CodeFolderPath));
    }

    [Fact]
    public async Task HandleAsync_WithSourceControlRoot_CodeFolderSet()
    {
        var codeRoot = Path.Combine(_tempDir, "code");
        Directory.CreateDirectory(codeRoot);
        var settings = await _settingsRepo.GetGlobalSettingsAsync();
        settings.SourceControlRoot = codeRoot;
        await _settingsRepo.SaveGlobalSettingsAsync(settings);

        var provider = Build();
        await provider.HandleAsync(new StartProjectCommand("MyApp", "my-app", "intent"));

        var context = _contextService.GetCurrent();
        Assert.True(context.IsFullyInitialized);
        Assert.False(string.IsNullOrEmpty(context.CodeFolderPath));
        Assert.Contains("my-app", context.CodeFolderPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_StoresIntentAsPurpose()
    {
        var provider = Build();

        await provider.HandleAsync(new StartProjectCommand("MyApp", "my-app", "Build a task manager"));

        var projects = await _projectRepo.GetAllAsync();
        Assert.Equal("Build a task manager", projects[0].Purpose);
    }
}
