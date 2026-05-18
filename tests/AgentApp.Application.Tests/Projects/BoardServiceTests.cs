using AgentApp.Application.Projects;
using AgentApp.Domain.Projects;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Projects;

public class BoardServiceTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonKnowledgeRecordRepository _repository;
    private readonly BoardService _sut;

    public BoardServiceTests()
    {
        _repository = new JsonKnowledgeRecordRepository(_tempFolder);
        _sut = new BoardService(_repository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task CreateRecordAsync_ValidInputs_SavesAndReturnsId()
    {
        var projectId = Guid.NewGuid();

        var id = await _sut.CreateRecordAsync(
            projectId, "US-001 Start session", "User can start chat session.", KnowledgeRecordType.Story, null);

        Assert.NotEqual(Guid.Empty, id);
        var saved = await _repository.GetByIdAsync(id);
        Assert.NotNull(saved);
        Assert.Equal(projectId, saved.ProjectId);
        Assert.Equal(KnowledgeRecordStatus.Backlog, saved.Status);
    }

    [Fact]
    public async Task TransitionStatusAsync_ValidTransition_SavesUpdatedRecord()
    {
        var record = KnowledgeRecord.Create(Guid.NewGuid(), "Story", "Desc", KnowledgeRecordType.Story, null);
        await _repository.SaveAsync(record);

        await _sut.TransitionStatusAsync(record.Id, KnowledgeRecordStatus.InImplementation);

        var saved = await _repository.GetByIdAsync(record.Id);
        Assert.NotNull(saved);
        Assert.Equal(KnowledgeRecordStatus.InImplementation, saved.Status);
    }

    [Fact]
    public async Task TransitionStatusAsync_RecordNotFound_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.TransitionStatusAsync(Guid.NewGuid(), KnowledgeRecordStatus.InImplementation));
    }

    [Fact]
    public async Task GetBoardAsync_ReturnsAllProjectRecords()
    {
        var projectId = Guid.NewGuid();
        await _repository.SaveAsync(KnowledgeRecord.Create(projectId, "Epic 1", "Desc", KnowledgeRecordType.Epic, null));
        await _repository.SaveAsync(KnowledgeRecord.Create(projectId, "Story 1", "Desc", KnowledgeRecordType.Story, null));

        var result = await _sut.GetBoardAsync(projectId);

        Assert.Equal(2, result.Count);
    }
}
