using AgentApp.Domain.Projects;
using AgentApp.Infrastructure.Persistence;

namespace AgentApp.Infrastructure.Tests.Persistence;

public class JsonProjectRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonProjectRepository _sut;

    public JsonProjectRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonProjectRepository(_testFolder);
    }

    [Fact]
    public async Task SaveAsync_NewProject_CanBeRetrievedById()
    {
        var project = Project.Create("TestProject", "test-project", @"C:\Projects\Test");

        await _sut.SaveAsync(project);
        var result = await _sut.GetByIdAsync(project.Id);

        Assert.NotNull(result);
        Assert.Equal(project.Id, result.Id);
        Assert.Equal("TestProject", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_MultipleProjects_ReturnsAll()
    {
        await _sut.SaveAsync(Project.Create("A", "a", @"C:\A"));
        await _sut.SaveAsync(Project.Create("B", "b", @"C:\B"));

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task DeleteAsync_ExistingProject_RemovesIt()
    {
        var project = Project.Create("ToDelete", "to-delete", @"C:\Delete");
        await _sut.SaveAsync(project);

        await _sut.DeleteAsync(project.Id);
        var result = await _sut.GetByIdAsync(project.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_ExistingProject_UpdatesIt()
    {
        var project = Project.Create("Original", "original", @"C:\Original");
        await _sut.SaveAsync(project);
        // Save same project again (simulate update path)
        await _sut.SaveAsync(project);

        var all = await _sut.GetAllAsync();
        Assert.Single(all);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
