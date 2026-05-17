using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Learning;

public class LearningSession
{
    public Guid Id { get; private set; }
    public LearningTrigger Trigger { get; private set; }
    public string ViolationDescription { get; private set; } = string.Empty;
    public Guid? ReasoningTraceId { get; private set; }
    public LearningOutcome Outcome { get; private set; }
    public ProposedRuleChange? ProposedChange { get; private set; }
    public int IterationCount { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private LearningSession() { }

    public static LearningSession Initiate(LearningTrigger trigger, string violationDescription,
        Guid? reasoningTraceId = null) => new()
    {
        Id = Guid.NewGuid(),
        Trigger = trigger,
        ViolationDescription = violationDescription,
        ReasoningTraceId = reasoningTraceId,
        Outcome = LearningOutcome.Pending,
        IterationCount = 0,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    public static LearningSession Reconstitute(Guid id, LearningTrigger trigger, string violationDescription,
        Guid? reasoningTraceId, LearningOutcome outcome, ProposedRuleChange? proposedChange,
        int iterationCount, string? rejectionReason, DateTimeOffset createdAt, DateTimeOffset updatedAt) => new()
    {
        Id = id,
        Trigger = trigger,
        ViolationDescription = violationDescription,
        ReasoningTraceId = reasoningTraceId,
        Outcome = outcome,
        ProposedChange = proposedChange,
        IterationCount = iterationCount,
        RejectionReason = rejectionReason,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };

    public void ProposeChange(ProposedRuleChange change)
    {
        ProposedChange = change;
        IterationCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Approve()
    {
        if (ProposedChange == null)
            throw new DomainValidationException("Cannot approve a learning session without a proposed rule change.");
        if (Outcome != LearningOutcome.Pending)
            throw new DomainValidationException($"Cannot approve a session with outcome {Outcome}.");
        Outcome = LearningOutcome.Approved;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string? reason = null)
    {
        Outcome = LearningOutcome.Rejected;
        RejectionReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Outcome = LearningOutcome.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
