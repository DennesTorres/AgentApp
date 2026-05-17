using AgentApp.Application.StructuredRules;
using AgentApp.Domain.StructuredRules;

namespace AgentApp.Application.Tests.StructuredRules;

public class MultiStepGateTrackerTests
{
    private static StructuredRule TwoStepRule() =>
        StructuredRule.Create("ILH", "ILH", "trigger", ["Step 1", "Step 2"]);

    [Fact]
    public void Initially_IsNotActive()
    {
        var tracker = new MultiStepGateTracker();
        Assert.False(tracker.IsActive);
        Assert.Null(tracker.ActiveRuleId);
    }

    [Fact]
    public void CanCallTool_WhenNotActive_ReturnsTrue()
    {
        var tracker = new MultiStepGateTracker();
        Assert.True(tracker.CanCallTool());
    }

    [Fact]
    public void Activate_SetsActiveRule_AndStep1()
    {
        var tracker = new MultiStepGateTracker();
        tracker.Activate(TwoStepRule());

        Assert.True(tracker.IsActive);
        Assert.Equal("ILH", tracker.ActiveRuleId);
        Assert.Equal(1, tracker.CurrentStep);
    }

    [Fact]
    public void CanCallTool_AfterActivate_BeforeValidation_ReturnsFalse()
    {
        var tracker = new MultiStepGateTracker();
        tracker.Activate(TwoStepRule());
        Assert.False(tracker.CanCallTool());
    }

    [Fact]
    public void ValidateStep_CorrectStep_ReturnsTrue_AllowsToolCall()
    {
        var tracker = new MultiStepGateTracker();
        tracker.Activate(TwoStepRule());

        var output = new GateOutput("ILH", 1);
        Assert.True(tracker.ValidateStep(output));
        Assert.True(tracker.CanCallTool());
    }

    [Fact]
    public void ValidateStep_WrongStep_ReturnsFalse()
    {
        var tracker = new MultiStepGateTracker();
        tracker.Activate(TwoStepRule());

        var wrongOutput = new GateOutput("ILH", 2);
        Assert.False(tracker.ValidateStep(wrongOutput));
    }

    [Fact]
    public void ValidateStep_WrongRule_ReturnsFalse()
    {
        var tracker = new MultiStepGateTracker();
        tracker.Activate(TwoStepRule());

        var output = new GateOutput("SIP", 1);
        Assert.False(tracker.ValidateStep(output));
    }

    [Fact]
    public void AdvanceStep_AfterValidation_RequiresNewValidation()
    {
        var tracker = new MultiStepGateTracker();
        tracker.Activate(TwoStepRule());
        tracker.ValidateStep(new GateOutput("ILH", 1));
        tracker.AdvanceStep();

        Assert.Equal(2, tracker.CurrentStep);
        Assert.False(tracker.CanCallTool());
    }

    [Fact]
    public void IsComplete_AfterAllStepsAdvanced_ReturnsTrue()
    {
        var tracker = new MultiStepGateTracker();
        tracker.Activate(TwoStepRule());

        tracker.ValidateStep(new GateOutput("ILH", 1));
        tracker.AdvanceStep();
        tracker.ValidateStep(new GateOutput("ILH", 2));
        tracker.AdvanceStep();

        Assert.True(tracker.IsComplete);
    }

    [Fact]
    public void Deactivate_ClearsActiveRule()
    {
        var tracker = new MultiStepGateTracker();
        tracker.Activate(TwoStepRule());
        tracker.Deactivate();

        Assert.False(tracker.IsActive);
        Assert.Null(tracker.ActiveRuleId);
        Assert.True(tracker.CanCallTool());
    }
}
