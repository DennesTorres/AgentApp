using AgentApp.Application.Orchestration;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Orchestration;
using AgentApp.Domain.Projects;
using NSubstitute;

namespace AgentApp.Application.Tests.Orchestration;

public class ReviewOrchestratorServiceTests
{
    private readonly IKnowledgeRecordRepository _recordRepository;
    private readonly IOrchestratorSessionRepository _sessionRepository;
    private readonly ReviewOrchestratorService _sut;

    public ReviewOrchestratorServiceTests()
    {
        _recordRepository = Substitute.For<IKnowledgeRecordRepository>();
        _sessionRepository = Substitute.For<IOrchestratorSessionRepository>();
        _sut = new ReviewOrchestratorService(_recordRepository, _sessionRepository);
    }

    [Fact]
    public async Task RunAsync_NewFinding_CreatesNewBacklogRecord()
    {
        var projectId = Guid.NewGuid();
        _recordRepository.GetByProjectIdAsync(projectId).Returns(new List<KnowledgeRecord>());
        var findings = new List<ReviewFinding>
        {
            new("Missing null check in GateValidator", "GateValidator does not handle null model responses.")
        };

        await _sut.RunAsync(projectId, findings);

        await _recordRepository.Received(1).SaveAsync(Arg.Is<KnowledgeRecord>(r =>
            r.Title == "Missing null check in GateValidator" &&
            r.Status == KnowledgeRecordStatus.Backlog));
    }

    [Fact]
    public async Task RunAsync_FindingMatchesImplementedRecord_TransitionsToInFix()
    {
        var projectId = Guid.NewGuid();
        var existingRecord = KnowledgeRecord.Create(projectId, "US-021 Gate validation", "Desc",
            KnowledgeRecordType.Story, null);
        existingRecord.TransitionTo(KnowledgeRecordStatus.InImplementation);
        existingRecord.TransitionTo(KnowledgeRecordStatus.Implemented);
        _recordRepository.GetByProjectIdAsync(projectId).Returns([existingRecord]);
        _recordRepository.GetByIdAsync(existingRecord.Id).Returns(existingRecord);

        var findings = new List<ReviewFinding>
        {
            new("US-021 Gate validation", "Implementation is missing edge case handling.")
        };

        await _sut.RunAsync(projectId, findings);

        await _recordRepository.Received(1).SaveAsync(Arg.Is<KnowledgeRecord>(r =>
            r.Id == existingRecord.Id && r.Status == KnowledgeRecordStatus.InFix));
    }

    [Fact]
    public async Task RunAsync_FindingMatchesInImplementationRecord_DoesNotTransition()
    {
        var projectId = Guid.NewGuid();
        var existingRecord = KnowledgeRecord.Create(projectId, "Active story", "Desc",
            KnowledgeRecordType.Story, null);
        existingRecord.TransitionTo(KnowledgeRecordStatus.InImplementation);
        _recordRepository.GetByProjectIdAsync(projectId).Returns([existingRecord]);

        var findings = new List<ReviewFinding> { new("Active story", "Finding about active story.") };

        await _sut.RunAsync(projectId, findings);

        // Should not save a status transition — only the session save
        await _recordRepository.DidNotReceive().SaveAsync(Arg.Is<KnowledgeRecord>(r =>
            r.Id == existingRecord.Id));
    }

    [Fact]
    public async Task RunAsync_CompletesSession()
    {
        var projectId = Guid.NewGuid();
        _recordRepository.GetByProjectIdAsync(projectId).Returns(new List<KnowledgeRecord>());

        await _sut.RunAsync(projectId, []);

        // Service saves session twice (Running on create, Completed on finish).
        // NSubstitute captures by reference so both appear Completed after mutation — verify count=2.
        await _sessionRepository.Received(2).SaveAsync(Arg.Is<OrchestratorSession>(s =>
            s.AgentType == AgentType.Review));
    }
}
