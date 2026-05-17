using AgentApp.Domain.StructuredRules;

namespace AgentApp.Application.StructuredRules;

public class MultiStepGateTracker
{
    private int _totalSteps;
    private bool _currentStepValidated;

    public string? ActiveRuleId { get; private set; }
    public int CurrentStep { get; private set; }
    public bool IsActive => ActiveRuleId != null;
    public bool IsComplete => IsActive && CurrentStep > _totalSteps;

    public void Activate(StructuredRule rule)
    {
        ActiveRuleId = rule.Id;
        _totalSteps = rule.StepCount;
        CurrentStep = 1;
        _currentStepValidated = false;
    }

    public bool CanCallTool() => !IsActive || _currentStepValidated;

    public bool ValidateStep(GateOutput output)
    {
        if (!IsActive) return false;
        if (output.RuleId != ActiveRuleId) return false;
        if (output.StepNumber != CurrentStep) return false;

        _currentStepValidated = true;
        return true;
    }

    public void AdvanceStep()
    {
        if (!IsActive) return;
        CurrentStep++;
        _currentStepValidated = false;
    }

    public void Deactivate()
    {
        ActiveRuleId = null;
        CurrentStep = 0;
        _totalSteps = 0;
        _currentStepValidated = false;
    }
}
