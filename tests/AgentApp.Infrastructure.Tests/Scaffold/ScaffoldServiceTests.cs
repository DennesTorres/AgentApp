using AgentApp.Infrastructure.Scaffold;

namespace AgentApp.Infrastructure.Tests.Scaffold;

public class ScaffoldServiceTests : IDisposable
{
    private readonly string _tempSourceRoot;

    public ScaffoldServiceTests()
    {
        _tempSourceRoot = Path.Combine(Path.GetTempPath(), $"ScaffoldTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempSourceRoot);
    }

    private static ScaffoldService BuildService() => new();

    [Fact]
    public async Task CreateScaffold_CreatesCodeFolder()
    {
        var service = BuildService();
        await service.CreateScaffoldAsync("MyProject", _tempSourceRoot);

        var codeFolder = Path.Combine(_tempSourceRoot, "MyProject");
        Assert.True(Directory.Exists(codeFolder));
    }

    [Fact]
    public async Task CreateScaffold_CreatesAgentFolder()
    {
        var service = BuildService();
        await service.CreateScaffoldAsync("MyProject", _tempSourceRoot);

        var agentFolder = service.GetAgentFolderPath("MyProject");
        Assert.True(Directory.Exists(agentFolder));
    }

    [Fact]
    public void GetAgentFolderPath_ReturnsTowerAgentSubfolder()
    {
        var service = BuildService();
        var path = service.GetAgentFolderPath("TestApp");

        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".TowerAgent", "TestApp");
        Assert.Equal(expected, path);
    }

    [Fact]
    public void GetCodeFolderPath_ReturnsSourceRootSubfolder()
    {
        var service = BuildService();
        var path = service.GetCodeFolderPath("TestApp", @"C:\Code");
        Assert.Equal(@"C:\Code\TestApp", path);
    }

    [Fact]
    public async Task CreateScaffold_IsIdempotent()
    {
        var service = BuildService();
        await service.CreateScaffoldAsync("MyProject", _tempSourceRoot);
        await service.CreateScaffoldAsync("MyProject", _tempSourceRoot); // should not throw
        Assert.True(Directory.Exists(Path.Combine(_tempSourceRoot, "MyProject")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempSourceRoot))
            Directory.Delete(_tempSourceRoot, recursive: true);

        // Clean up agent folder if created
        var agentFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".TowerAgent", "MyProject");
        if (Directory.Exists(agentFolder))
            Directory.Delete(agentFolder, recursive: true);
    }
}
