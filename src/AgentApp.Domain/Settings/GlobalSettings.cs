namespace AgentApp.Domain.Settings;

public class GlobalSettings
{
    public string RootProjectFolderPath { get; set; } = string.Empty;
    public int MaxGateRetries { get; set; } = 3;
    public bool RequireUserConfirmationForInternalLearning { get; set; } = false;
    public bool RequireUserConfirmationForFindingsExtraction { get; set; } = false;
    public int TokenThresholdForContextReset { get; set; } = 80000;
    public string EmbeddingModelEndpoint { get; set; } = string.Empty;
    public float VectorSearchSensitivity { get; set; } = 0.7f;
}
