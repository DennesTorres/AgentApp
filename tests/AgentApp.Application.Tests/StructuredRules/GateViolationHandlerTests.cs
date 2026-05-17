using AgentApp.Application.StructuredRules;
using AgentApp.Domain.Learning;
using AgentApp.Domain.StructuredRules;

namespace AgentApp.Application.Tests.StructuredRules;

public class GateViolationHandlerTests
{
    private static StructuredRule ThreeStepRule() =>
        StructuredRule.Create("ILH", "ILH", "trigger", ["Step 1", "Step 2", "Step 3"]);

    [Fact]
    public void CreateViolation_WhenRuleActiveAndNotValidated_ReturnsViolation()
    {
        var tracker = new MultiStepGateTracker();
        tracker.Activate(ThreeStepRule());

        var handler = new GateViolationHandler();
        var violation = handler.CreateViolation(tracker);

        Assert.NotNull(violation);
        Assert.Equal("ILH", violation!.RuleId);
        Assert.Equal(1, violation.ExpectedStep);
    }

    [Fact]
    public void CreateViolation_WhenNoActiveRule_ReturnsNull()
    {
        var tracker = new MultiStepGateTracker();
        var handler = new GateViolationHandler();

        Assert.Null(handler.CreateViolation(tracker));
    }

    [Fact]
    public void CreateViolation_WhenStepAlreadyValidated_ReturnsNull()
    {
        var tracker = new MultiStepGateTracker();
        tracker.Activate(ThreeStepRule());
        tracker.ValidateStep(new GateOutput("ILH", 1));

        var handler = new GateViolationHandler();
        Assert.Null(handler.CreateViolation(tracker));
    }

    [Fact]
    public void CreateViolation_HasTimestamp()
    {
        var before = DateTimeOffset.UtcNow;
        var tracker = new MultiStepGateTracker();
        tracker.Activate(ThreeStepRule());

        var handler = new GateViolationHandler();
        var violation = handler.CreateViolation(tracker);

        Assert.True(violation!.OccurredAt >= before);
    }

    [Fact]
    public void CreateLearningTrigger_FromViolation_ReturnsLearningSession()
    {
        var violation = GateViolation.Create("ILH", 2);
        var handler = new GateViolationHandler();

        var session = handler.CreateLearningTrigger(violation);
        Assert.NotNull(session);
    }

    [Fact]
    public void CreateLearningTrigger_HasInternalGateFailureTrigger()
    {
        var violation = GateViolation.Create("ILH", 2);
        var handler = new GateViolationHandler();

        var session = handler.CreateLearningTrigger(violation);
        Assert.Equal(LearningTrigger.InternalGateFailure, session.Trigger);
    }

    [Fact]
    public void CreateLearningTrigger_DescriptionContainsRuleId()
    {
        var violation = GateViolation.Create("ILH", 2);
        var handler = new GateViolationHandler();

        var session = handler.CreateLearningTrigger(violation);
        Assert.Contains("ILH", session.ViolationDescription);
    }
}
