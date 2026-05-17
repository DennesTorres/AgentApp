using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Orchestration;
using AgentApp.Domain.Projects;

namespace AgentApp.Application.Orchestration;

public class ReviewOrchestratorService
{
    private readonly IKnowledgeRecordRepository _recordRepository;
    private readonly IOrchestratorSessionRepository _sessionRepository;

    public ReviewOrchestratorService(
        IKnowledgeRecordRepository recordRepository,
        IOrchestratorSessionRepository sessionRepository)
    {
        _recordRepository = recordRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<Guid> RunAsync(Guid projectId, IReadOnlyList<ReviewFinding> findings)
    {
        var session = OrchestratorSession.Create(projectId, AgentType.Review);
        await _sessionRepository.SaveAsync(session);

        var existingRecords = await _recordRepository.GetByProjectIdAsync(projectId);

        foreach (var finding in findings)
        {
            var existing = existingRecords.FirstOrDefault(r =>
                r.Title.Equals(finding.Title, StringComparison.OrdinalIgnoreCase) &&
                r.Status != KnowledgeRecordStatus.Done);

            if (existing != null)
            {
                // US-105: check state before acting — only act when record is in a reviewable state
                if (existing.Status == KnowledgeRecordStatus.Implemented)
                {
                    // Review agent reviewed and found an issue: Implemented → ReviewedAgent → InFix
                    existing.TransitionTo(KnowledgeRecordStatus.ReviewedAgent);
                    existing.TransitionTo(KnowledgeRecordStatus.InFix);
                    await _recordRepository.SaveAsync(existing);
                }
                else if (existing.Status is KnowledgeRecordStatus.ReviewedUser or KnowledgeRecordStatus.ReviewedAgent)
                {
                    existing.TransitionTo(KnowledgeRecordStatus.InFix);
                    await _recordRepository.SaveAsync(existing);
                }
                // InImplementation, Backlog, InFix: no action — record is already being worked on
            }
            else
            {
                // US-104: route to new story when no matching record exists
                var newRecord = KnowledgeRecord.Create(
                    projectId, finding.Title, finding.Description, KnowledgeRecordType.Story, null);
                await _recordRepository.SaveAsync(newRecord);
            }
        }

        session.Complete();
        await _sessionRepository.SaveAsync(session);

        return session.Id;
    }
}
