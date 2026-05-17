namespace AgentApp.Domain.Settings;

public class ProjectSettings
{
    public Guid ProjectId { get; private set; }
    public int? MaxGateRetries { get; private set; }
    public bool? RequireUserConfirmationForInternalLearning { get; private set; }
    public bool? RequireUserConfirmationForFindingsExtraction { get; private set; }
    public int? TokenThresholdForContextReset { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ProjectSettings() { }

    public static ProjectSettings Create(Guid projectId) =>
        new() { ProjectId = projectId, CreatedAt = DateTimeOffset.UtcNow };

    public static ProjectSettings Reconstitute(
        Guid projectId, int? maxGateRetries, bool? requireConfirmationInternal,
        bool? requireConfirmationFindings, int? tokenThreshold, DateTimeOffset createdAt) =>
        new()
        {
            ProjectId = projectId,
            MaxGateRetries = maxGateRetries,
            RequireUserConfirmationForInternalLearning = requireConfirmationInternal,
            RequireUserConfirmationForFindingsExtraction = requireConfirmationFindings,
            TokenThresholdForContextReset = tokenThreshold,
            CreatedAt = createdAt
        };

    public void SetMaxGateRetries(int? value) => MaxGateRetries = value;
    public void SetRequireConfirmationInternal(bool? value) => RequireUserConfirmationForInternalLearning = value;
    public void SetRequireConfirmationFindings(bool? value) => RequireUserConfirmationForFindingsExtraction = value;
    public void SetTokenThreshold(int? value) => TokenThresholdForContextReset = value;
}
