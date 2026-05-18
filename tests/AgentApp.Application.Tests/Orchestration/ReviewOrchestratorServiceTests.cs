using AgentApp.Application.Orchestration;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Orchestration;
using AgentApp.Domain.Projects;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Orchestration;

public class ReviewOrchestratorServiceTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly IKnowledgeRecordRepository _recordRepository;
    private readonly IOrchestratorSessionRepository _sessionRepository;
    private readonly ReviewOrchestratorService _sut;

    public ReviewOrchestratorServiceTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempFolder);
        _recordRepository = new JsonKnowledgeRecordRepository(_tempFolder);
        _sessionRepository = new JsonOrchestratorSessionRepository(_tempFolder);
        _sut = new ReviewOrchestratorService(_recordRepository, _sessionRepository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task RunAsync_NewFinding_CreatesNewBacklogRecord()
    {
        var projectId = Guid.NewGuid();
        var findings = new List<ReviewFinding>
        {
            new("Missing null check in GateValidator", "GateValidator does not handle null model responses.")
        };

        await _sut.RunAsync(projectId, findings);

        var records = await _recordRepository.GetByProjectIdAsync(projectId);
        Assert.Single(records);
        Assert.Equal("Missing null check in GateValidator", records[0].Title);
        Assert.Equal(KnowledgeRecordStatus.Backlog, records[0].Status);
    }

    [Fact]
    public async Task RunAsync_FindingMatchesImplementedRecord_TransitionsToInFix()
    {
        var projectId = Guid.NewGuid();
        var existingRecord = KnowledgeRecord.Create(projectId, "US-021 Gate validation", "Desc",
            KnowledgeRecordType.Story, null);
        existingRecord.TransitionTo(KnowledgeRecordStatus.InImplementation);
        existingRecord.TransitionTo(KnowledgeRecordStatus.Implemented);
        await _recordRepository.SaveAsync(existingRecord);

        var findings = new List<ReviewFinding>
        {
            new("US-021 Gate validation", "Implementation is missing edge case handling.")
        };

        await _sut.RunAsync(projectId, findings);

        var saved = await _recordRepository.GetByIdAsync(existingRecord.Id);
        Assert.NotNull(saved);
        Assert.Equal(KnowledgeRecordStatus.InFix, saved.Status);
    }

    [Fact]
    public async Task RunAsync_FindingMatchesInImplementationRecord_DoesNotTransition()
    {
        var projectId = Guid.NewGuid();
        var existingRecord = KnowledgeRecord.Create(projectId, "Active story", "Desc",
            KnowledgeRecordType.Story, null);
        existingRecord.TransitionTo(KnowledgeRecordStatus.InImplementation);
        await _recordRepository.SaveAsync(existingRecord);

        var findings = new List<ReviewFinding> { new("Active story", "Finding about active story.") };

        await _sut.RunAsync(projectId, findings);

        var saved = await _recordRepository.GetByIdAsync(existingRecord.Id);
        Assert.NotNull(saved);
        Assert.Equal(KnowledgeRecordStatus.InImplementation, saved.Status);
    }

    [Fact]
    public async Task RunAsync_CompletesSession()
    {
        var projectId = Guid.NewGuid();

        await _sut.RunAsync(projectId, []);

        var sessions = await _sessionRepository.GetByProjectIdAsync(projectId);
        Assert.Single(sessions);
        Assert.Equal(OrchestratorStatus.Completed, sessions[0].Status);
    }
}
