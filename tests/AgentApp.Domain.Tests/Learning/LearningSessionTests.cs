using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Learning;

namespace AgentApp.Domain.Tests.Learning;

public class LearningSessionTests
{
    private static readonly ProposedRuleChange SampleChange =
        ProposedRuleChange.Create("coding-standards", "## Rule\nAlways verify before acting.",
            "Add verification rule to coding-standards.md", "add");

    [Fact]
    public void Initiate_SetsAllPropertiesWithPendingOutcome()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure,
            "Gate check failed on phase1-check", null);

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(LearningTrigger.InternalGateFailure, session.Trigger);
        Assert.Equal("Gate check failed on phase1-check", session.ViolationDescription);
        Assert.Null(session.ReasoningTraceId);
        Assert.Equal(LearningOutcome.Pending, session.Outcome);
        Assert.Null(session.ProposedChange);
        Assert.Equal(0, session.IterationCount);
    }

    [Fact]
    public void ProposeChange_UpdatesProposedChangeAndIncrementsIteration()
    {
        var session = LearningSession.Initiate(LearningTrigger.ExternalUserError, "User pointed out error", null);

        session.ProposeChange(SampleChange);

        Assert.NotNull(session.ProposedChange);
        Assert.Equal("coding-standards", session.ProposedChange.FileName);
        Assert.Equal(1, session.IterationCount);
    }

    [Fact]
    public void ProposeChange_CalledTwice_IncrementsIterationToTwo()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        session.ProposeChange(SampleChange);
        session.ProposeChange(SampleChange);

        Assert.Equal(2, session.IterationCount);
    }

    [Fact]
    public void Approve_WithProposedChange_SetsApprovedOutcome()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        session.ProposeChange(SampleChange);

        session.Approve();

        Assert.Equal(LearningOutcome.Approved, session.Outcome);
    }

    [Fact]
    public void Approve_WithoutProposedChange_ThrowsDomainValidationException()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);

        Assert.Throws<DomainValidationException>(() => session.Approve());
    }

    [Fact]
    public void Reject_WithReason_SetsRejectedOutcomeAndReason()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        session.ProposeChange(SampleChange);

        session.Reject("Not strong enough");

        Assert.Equal(LearningOutcome.Rejected, session.Outcome);
        Assert.Equal("Not strong enough", session.RejectionReason);
    }

    [Fact]
    public void Reject_WithNullReason_SetsRejectedWithNullReason()
    {
        var session = LearningSession.Initiate(LearningTrigger.ExternalUserError, "error", null);
        session.ProposeChange(SampleChange);

        session.Reject(null);

        Assert.Equal(LearningOutcome.Rejected, session.Outcome);
        Assert.Null(session.RejectionReason);
    }

    [Fact]
    public void Cancel_SetsCancelledOutcome()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);

        session.Cancel();

        Assert.Equal(LearningOutcome.Cancelled, session.Outcome);
    }

    [Fact]
    public void Approve_AfterCancelled_ThrowsDomainValidationException()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        session.ProposeChange(SampleChange);
        session.Cancel();

        Assert.Throws<DomainValidationException>(() => session.Approve());
    }

    [Fact]
    public void Reconstitute_RestoresAllProperties()
    {
        var id = Guid.NewGuid();
        var traceId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-1);

        var session = LearningSession.Reconstitute(id, LearningTrigger.ExternalUserError,
            "description", traceId, LearningOutcome.Approved, SampleChange, 2, "reason", createdAt, createdAt);

        Assert.Equal(id, session.Id);
        Assert.Equal(LearningTrigger.ExternalUserError, session.Trigger);
        Assert.Equal(traceId, session.ReasoningTraceId);
        Assert.Equal(LearningOutcome.Approved, session.Outcome);
        Assert.Equal(2, session.IterationCount);
    }
}
