using AgentApp.Domain.Projects;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonKnowledgeRecordRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonKnowledgeRecordRepository _sut;

    public JsonKnowledgeRecordRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonKnowledgeRecordRepository(_testFolder);
    }

    [Fact]
    public async Task GetByProjectIdAsync_EmptyStore_ReturnsEmpty()
    {
        var result = await _sut.GetByProjectIdAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveAsync_Record_CanBeRetrievedById()
    {
        var projectId = Guid.NewGuid();
        var record = KnowledgeRecord.Create(projectId, "US-001 Test story", "Description", KnowledgeRecordType.Story, null);

        await _sut.SaveAsync(record);
        var result = await _sut.GetByIdAsync(record.Id);

        Assert.NotNull(result);
        Assert.Equal(record.Id, result.Id);
        Assert.Equal("US-001 Test story", result.Title);
        Assert.Equal(KnowledgeRecordStatus.Backlog, result.Status);
    }

    [Fact]
    public async Task GetByProjectIdAsync_MultipleProjects_ReturnsOnlyMatchingProject()
    {
        var project1 = Guid.NewGuid();
        var project2 = Guid.NewGuid();
        await _sut.SaveAsync(KnowledgeRecord.Create(project1, "P1 Story", "Desc", KnowledgeRecordType.Story, null));
        await _sut.SaveAsync(KnowledgeRecord.Create(project2, "P2 Story", "Desc", KnowledgeRecordType.Story, null));

        var result = await _sut.GetByProjectIdAsync(project1);

        Assert.Single(result);
        Assert.Equal("P1 Story", result[0].Title);
    }

    [Fact]
    public async Task SaveAsync_UpdatedRecord_OverwritesExisting()
    {
        var projectId = Guid.NewGuid();
        var record = KnowledgeRecord.Create(projectId, "Story", "Desc", KnowledgeRecordType.Story, null);
        await _sut.SaveAsync(record);

        record.TransitionTo(KnowledgeRecordStatus.InImplementation);
        await _sut.SaveAsync(record);

        var result = await _sut.GetByIdAsync(record.Id);
        Assert.Equal(KnowledgeRecordStatus.InImplementation, result!.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord()
    {
        var projectId = Guid.NewGuid();
        var record = KnowledgeRecord.Create(projectId, "Story to delete", "Desc", KnowledgeRecordType.Story, null);
        await _sut.SaveAsync(record);

        await _sut.DeleteAsync(record.Id);

        var result = await _sut.GetByIdAsync(record.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
