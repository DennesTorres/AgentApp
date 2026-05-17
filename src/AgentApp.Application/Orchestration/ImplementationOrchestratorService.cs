using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Orchestration;
using AgentApp.Domain.Projects;

namespace AgentApp.Application.Orchestration;

public class ImplementationOrchestratorService
{
    private readonly IKnowledgeRecordRepository _recordRepository;
    private readonly IOrchestratorSessionRepository _sessionRepository;

    public ImplementationOrchestratorService(
        IKnowledgeRecordRepository recordRepository,
        IOrchestratorSessionRepository sessionRepository)
    {
        _recordRepository = recordRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<Guid> BeginImplementationAsync(Guid projectId, Guid recordId)
    {
        var record = await _recordRepository.GetByIdAsync(recordId)
            ?? throw new InvalidOperationException($"Knowledge record {recordId} not found.");

        record.TransitionTo(KnowledgeRecordStatus.InImplementation);
        await _recordRepository.SaveAsync(record);

        var session = OrchestratorSession.Create(projectId, AgentType.Implementation);
        await _sessionRepository.SaveAsync(session);

        return session.Id;
    }

    public async Task CompleteImplementationAsync(Guid projectId, Guid recordId, Guid sessionId)
    {
        var record = await _recordRepository.GetByIdAsync(recordId)
            ?? throw new InvalidOperationException($"Knowledge record {recordId} not found.");

        record.TransitionTo(KnowledgeRecordStatus.Implemented);
        await _recordRepository.SaveAsync(record);

        var session = await _sessionRepository.GetByIdAsync(sessionId)
            ?? throw new InvalidOperationException($"Orchestrator session {sessionId} not found.");

        session.Complete();
        await _sessionRepository.SaveAsync(session);
    }
}
