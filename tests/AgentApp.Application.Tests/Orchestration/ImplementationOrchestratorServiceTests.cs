using AgentApp.Application.Orchestration;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Orchestration;
using AgentApp.Domain.Projects;
using NSubstitute;

namespace AgentApp.Application.Tests.Orchestration;

public class ImplementationOrchestratorServiceTests
{
    private readonly IKnowledgeRecordRepository _recordRepository;
    private readonly IOrchestratorSessionRepository _sessionRepository;
    private readonly ImplementationOrchestratorService _sut;

    public ImplementationOrchestratorServiceTests()
    {
        _recordRepository = Substitute.For<IKnowledgeRecordRepository>();
        _sessionRepository = Substitute.For<IOrchestratorSessionRepository>();
        _sut = new ImplementationOrchestratorService(_recordRepository, _sessionRepository);
    }

    [Fact]
    public async Task BeginImplementationAsync_TransitionsRecordAndCreatesSession()
    {
        var projectId = Guid.NewGuid();
        var record = KnowledgeRecord.Create(projectId, "US-001 Story", "Desc", KnowledgeRecordType.Story, null);
        _recordRepository.GetByIdAsync(record.Id).Returns(record);

        var sessionId = await _sut.BeginImplementationAsync(projectId, record.Id);

        Assert.NotEqual(Guid.Empty, sessionId);
        await _recordRepository.Received(1).SaveAsync(Arg.Is<KnowledgeRecord>(r =>
            r.Id == record.Id && r.Status == KnowledgeRecordStatus.InImplementation));
        await _sessionRepository.Received(1).SaveAsync(Arg.Is<OrchestratorSession>(s =>
            s.ProjectId == projectId && s.AgentType == AgentType.Implementation));
    }

    [Fact]
    public async Task CompleteImplementationAsync_TransitionsRecordToImplementedAndCompletesSession()
    {
        var projectId = Guid.NewGuid();
        var record = KnowledgeRecord.Create(projectId, "Story", "Desc", KnowledgeRecordType.Story, null);
        record.TransitionTo(KnowledgeRecordStatus.InImplementation);
        _recordRepository.GetByIdAsync(record.Id).Returns(record);

        var session = OrchestratorSession.Create(projectId, AgentType.Implementation);
        _sessionRepository.GetByIdAsync(session.Id).Returns(session);

        await _sut.CompleteImplementationAsync(projectId, record.Id, session.Id);

        await _recordRepository.Received(1).SaveAsync(Arg.Is<KnowledgeRecord>(r =>
            r.Status == KnowledgeRecordStatus.Implemented));
        await _sessionRepository.Received(1).SaveAsync(Arg.Is<OrchestratorSession>(s =>
            s.Status == OrchestratorStatus.Completed));
    }

    [Fact]
    public async Task BeginImplementationAsync_RecordNotFound_ThrowsInvalidOperationException()
    {
        _recordRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((KnowledgeRecord?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.BeginImplementationAsync(Guid.NewGuid(), Guid.NewGuid()));
    }
}
