namespace AgentApp.Application.Settings;

public class EffectiveSettings
{
    public int MaxGateRetries { get; }
    public bool RequireUserConfirmationForInternalLearning { get; }
    public bool RequireUserConfirmationForFindingsExtraction { get; }
    public int TokenThresholdForContextReset { get; }

    public EffectiveSettings(
        int maxGateRetries,
        bool requireConfirmationInternal,
        bool requireConfirmationFindings,
        int tokenThreshold)
    {
        MaxGateRetries = maxGateRetries;
        RequireUserConfirmationForInternalLearning = requireConfirmationInternal;
        RequireUserConfirmationForFindingsExtraction = requireConfirmationFindings;
        TokenThresholdForContextReset = tokenThreshold;
    }
}
