namespace AgentApp.Domain.Settings;

public class GlobalSettings
{
    public string RootProjectFolderPath { get; set; } = string.Empty;
    public int MaxGateRetries { get; set; } = 3;
    public bool RequireUserConfirmationForInternalLearning { get; set; } = false;
}
