using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Orchestration;

public class OrchestratorSession
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public AgentType AgentType { get; private set; }
    public OrchestratorStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private OrchestratorSession() { }

    public static OrchestratorSession Create(Guid projectId, AgentType agentType) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            AgentType = agentType,
            Status = OrchestratorStatus.Running,
            StartedAt = DateTimeOffset.UtcNow
        };

    public static OrchestratorSession Reconstitute(
        Guid id, Guid projectId, AgentType agentType, OrchestratorStatus status,
        DateTimeOffset startedAt, DateTimeOffset? completedAt) =>
        new()
        {
            Id = id, ProjectId = projectId, AgentType = agentType,
            Status = status, StartedAt = startedAt, CompletedAt = completedAt
        };

    public void Complete()
    {
        if (Status != OrchestratorStatus.Running)
            throw new DomainValidationException($"Cannot complete a session that is in {Status} status.");

        Status = OrchestratorStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail()
    {
        if (Status != OrchestratorStatus.Running)
            throw new DomainValidationException($"Cannot fail a session that is in {Status} status.");

        Status = OrchestratorStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
