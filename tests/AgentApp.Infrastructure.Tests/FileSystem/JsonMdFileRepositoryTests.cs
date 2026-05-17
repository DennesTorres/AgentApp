using AgentApp.Domain.Rules;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonMdFileRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonMdFileRepository _sut;

    public JsonMdFileRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonMdFileRepository(_testFolder);
    }

    [Fact]
    public async Task SaveAsync_NewFile_CanBeRetrievedByName()
    {
        var file = MdFile.CreateGlobal("core", "# Core rules");

        await _sut.SaveAsync(file);
        var result = await _sut.GetByNameAsync("core", MdFileScope.Global, null);

        Assert.NotNull(result);
        Assert.Equal("core", result.Name);
        Assert.Equal("# Core rules", result.Content);
    }

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.GetByNameAsync("nonexistent", MdFileScope.Global, null);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_GlobalScope_ReturnsOnlyGlobalFiles()
    {
        var projectId = Guid.NewGuid();
        await _sut.SaveAsync(MdFile.CreateGlobal("global-file", "content"));
        await _sut.SaveAsync(MdFile.CreateForProject("project-file", "content", projectId));

        var result = await _sut.GetAllAsync(MdFileScope.Global, null);

        Assert.Single(result);
        Assert.Equal("global-file", result[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_ProjectScope_ReturnsOnlyProjectFiles()
    {
        var projectId = Guid.NewGuid();
        await _sut.SaveAsync(MdFile.CreateGlobal("global-file", "content"));
        await _sut.SaveAsync(MdFile.CreateForProject("project-file", "content", projectId));

        var result = await _sut.GetAllAsync(MdFileScope.Project, projectId);

        Assert.Single(result);
        Assert.Equal("project-file", result[0].Name);
    }

    [Fact]
    public async Task SaveAsync_ExistingFile_UpdatesContent()
    {
        var file = MdFile.CreateGlobal("core", "original");
        await _sut.SaveAsync(file);
        file.UpdateContent("updated");
        await _sut.SaveAsync(file);

        var result = await _sut.GetByNameAsync("core", MdFileScope.Global, null);

        Assert.Equal("updated", result!.Content);
        var all = await _sut.GetAllAsync(MdFileScope.Global, null);
        Assert.Single(all);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
