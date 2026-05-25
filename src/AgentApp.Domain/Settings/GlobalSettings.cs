namespace AgentApp.Domain.Settings;

public class GlobalSettings
{
    public string RootProjectFolderPath { get; set; } = string.Empty;
    public string ModelUrl { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int MaxGateRetries { get; set; } = 3;
    public bool RequireUserConfirmationForInternalLearning { get; set; } = false;
    public bool RequireUserConfirmationForFindingsExtraction { get; set; } = false;
    public int TokenThresholdForContextReset { get; set; } = 80000;
    public string SourceControlRoot { get; set; } = string.Empty;
    public string AgentAvatarLetter { get; set; } = "T";
    public string UserAvatarLetter { get; set; } = "U";
    public string AgentAvatarImagePath { get; set; } = string.Empty;
    public string UserAvatarImagePath { get; set; } = string.Empty;
    // C-031: built-in color preset ("blue"|"purple"|"teal"|"amber")
    public string AgentAvatarPreset { get; set; } = "blue";
    public string UserAvatarPreset { get; set; } = "teal";
}
