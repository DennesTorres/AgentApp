using AgentApp.Application.Projects;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;
using NSubstitute;

namespace AgentApp.Application.Tests.Projects;

public class BoardServiceTests
{
    private readonly IKnowledgeRecordRepository _repository;
    private readonly BoardService _sut;

    public BoardServiceTests()
    {
        _repository = Substitute.For<IKnowledgeRecordRepository>();
        _sut = new BoardService(_repository);
    }

    [Fact]
    public async Task CreateRecordAsync_ValidInputs_SavesAndReturnsId()
    {
        var projectId = Guid.NewGuid();

        var id = await _sut.CreateRecordAsync(
            projectId, "US-001 Start session", "User can start chat session.", KnowledgeRecordType.Story, null);

        Assert.NotEqual(Guid.Empty, id);
        await _repository.Received(1).SaveAsync(Arg.Is<KnowledgeRecord>(r =>
            r.ProjectId == projectId &&
            r.Id == id &&
            r.Status == KnowledgeRecordStatus.Backlog));
    }

    [Fact]
    public async Task TransitionStatusAsync_ValidTransition_SavesUpdatedRecord()
    {
        var record = KnowledgeRecord.Create(Guid.NewGuid(), "Story", "Desc", KnowledgeRecordType.Story, null);
        _repository.GetByIdAsync(record.Id).Returns(record);

        await _sut.TransitionStatusAsync(record.Id, KnowledgeRecordStatus.InImplementation);

        await _repository.Received(1).SaveAsync(Arg.Is<KnowledgeRecord>(r =>
            r.Id == record.Id &&
            r.Status == KnowledgeRecordStatus.InImplementation));
    }

    [Fact]
    public async Task TransitionStatusAsync_RecordNotFound_ThrowsInvalidOperationException()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((KnowledgeRecord?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.TransitionStatusAsync(Guid.NewGuid(), KnowledgeRecordStatus.InImplementation));
    }

    [Fact]
    public async Task GetBoardAsync_ReturnsAllProjectRecords()
    {
        var projectId = Guid.NewGuid();
        var records = new List<KnowledgeRecord>
        {
            KnowledgeRecord.Create(projectId, "Epic 1", "Desc", KnowledgeRecordType.Epic, null),
            KnowledgeRecord.Create(projectId, "Story 1", "Desc", KnowledgeRecordType.Story, null)
        };
        _repository.GetByProjectIdAsync(projectId).Returns(records);

        var result = await _sut.GetBoardAsync(projectId);

        Assert.Equal(2, result.Count);
    }
}
