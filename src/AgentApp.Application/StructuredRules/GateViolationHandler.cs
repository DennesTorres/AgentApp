using AgentApp.Domain.Learning;
using AgentApp.Domain.StructuredRules;

namespace AgentApp.Application.StructuredRules;

public class GateViolationHandler
{
    public GateViolation? CreateViolation(MultiStepGateTracker tracker)
    {
        if (!tracker.IsActive) return null;
        if (tracker.CanCallTool()) return null;
        return GateViolation.Create(tracker.ActiveRuleId!, tracker.CurrentStep);
    }

    public LearningSession CreateLearningTrigger(GateViolation violation) =>
        LearningSession.Initiate(
            LearningTrigger.InternalGateFailure,
            $"Gate violation: rule '{violation.RuleId}' step {violation.ExpectedStep} was skipped without producing required gate output.");
}
