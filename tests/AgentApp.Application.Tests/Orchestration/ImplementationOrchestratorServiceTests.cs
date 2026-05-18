using AgentApp.Application.Orchestration;
using AgentApp.Domain.Orchestration;
using AgentApp.Domain.Projects;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Orchestration;

public class ImplementationOrchestratorServiceTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonKnowledgeRecordRepository _recordRepository;
    private readonly JsonOrchestratorSessionRepository _sessionRepository;
    private readonly ImplementationOrchestratorService _sut;

    public ImplementationOrchestratorServiceTests()
    {
        _recordRepository = new JsonKnowledgeRecordRepository(_tempFolder);
        _sessionRepository = new JsonOrchestratorSessionRepository(_tempFolder);
        _sut = new ImplementationOrchestratorService(_recordRepository, _sessionRepository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task BeginImplementationAsync_TransitionsRecordAndCreatesSession()
    {
        var projectId = Guid.NewGuid();
        var record = KnowledgeRecord.Create(projectId, "US-001 Story", "Desc", KnowledgeRecordType.Story, null);
        await _recordRepository.SaveAsync(record);

        var sessionId = await _sut.BeginImplementationAsync(projectId, record.Id);

        Assert.NotEqual(Guid.Empty, sessionId);
        var savedRecord = await _recordRepository.GetByIdAsync(record.Id);
        Assert.NotNull(savedRecord);
        Assert.Equal(KnowledgeRecordStatus.InImplementation, savedRecord.Status);
        var sessions = await _sessionRepository.GetByProjectIdAsync(projectId);
        Assert.Contains(sessions, s => s.AgentType == AgentType.Implementation);
    }

    [Fact]
    public async Task CompleteImplementationAsync_TransitionsRecordToImplementedAndCompletesSession()
    {
        var projectId = Guid.NewGuid();
        var record = KnowledgeRecord.Create(projectId, "Story", "Desc", KnowledgeRecordType.Story, null);
        record.TransitionTo(KnowledgeRecordStatus.InImplementation);
        await _recordRepository.SaveAsync(record);

        var session = OrchestratorSession.Create(projectId, AgentType.Implementation);
        await _sessionRepository.SaveAsync(session);

        await _sut.CompleteImplementationAsync(projectId, record.Id, session.Id);

        var savedRecord = await _recordRepository.GetByIdAsync(record.Id);
        Assert.NotNull(savedRecord);
        Assert.Equal(KnowledgeRecordStatus.Implemented, savedRecord.Status);
        var savedSession = await _sessionRepository.GetByIdAsync(session.Id);
        Assert.NotNull(savedSession);
        Assert.Equal(OrchestratorStatus.Completed, savedSession.Status);
    }

    [Fact]
    public async Task BeginImplementationAsync_RecordNotFound_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.BeginImplementationAsync(Guid.NewGuid(), Guid.NewGuid()));
    }
}
