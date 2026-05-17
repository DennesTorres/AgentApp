using AgentApp.Domain.Rules;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonTriggersIndexRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonTriggersIndexRepository _sut;

    public JsonTriggersIndexRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonTriggersIndexRepository(_testFolder);
    }

    [Fact]
    public async Task GetGlobalAsync_EmptyStore_ReturnsEmptyIndex()
    {
        var index = await _sut.GetGlobalAsync();

        Assert.NotNull(index);
        Assert.Equal(MdFileScope.Global, index.Scope);
        Assert.Null(index.ProjectId);
    }

    [Fact]
    public async Task SaveAsync_GlobalIndex_CanBeReloaded()
    {
        var index = TriggersIndex.CreateGlobal();
        index.AddEntry("coding-standards", ["refactor", "review"]);

        await _sut.SaveAsync(index);
        var reloaded = await _sut.GetGlobalAsync();

        Assert.True(reloaded.HasTrigger("refactor"));
        Assert.Equal("coding-standards", reloaded.GetFileNameForTrigger("refactor"));
    }

    [Fact]
    public async Task GetForProjectAsync_EmptyStore_ReturnsEmptyProjectIndex()
    {
        var projectId = Guid.NewGuid();

        var index = await _sut.GetForProjectAsync(projectId);

        Assert.Equal(MdFileScope.Project, index.Scope);
        Assert.Equal(projectId, index.ProjectId);
    }

    [Fact]
    public async Task SaveAsync_ProjectIndex_CanBeReloaded()
    {
        var projectId = Guid.NewGuid();
        var index = TriggersIndex.CreateForProject(projectId);
        index.AddEntry("api-rules", ["endpoint", "api"]);

        await _sut.SaveAsync(index);
        var reloaded = await _sut.GetForProjectAsync(projectId);

        Assert.True(reloaded.HasTrigger("endpoint"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
