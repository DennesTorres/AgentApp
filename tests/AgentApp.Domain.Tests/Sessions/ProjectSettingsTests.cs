using AgentApp.Domain.Settings;

namespace AgentApp.Domain.Tests.Sessions;

public class ProjectSettingsTests
{
    [Fact]
    public void Create_WithNullOverrides_AllOverridesAreNull()
    {
        var projectId = Guid.NewGuid();

        var settings = ProjectSettings.Create(projectId);

        Assert.Equal(projectId, settings.ProjectId);
        Assert.Null(settings.MaxGateRetries);
        Assert.Null(settings.RequireUserConfirmationForInternalLearning);
        Assert.Null(settings.RequireUserConfirmationForFindingsExtraction);
        Assert.Null(settings.TokenThresholdForContextReset);
    }

    [Fact]
    public void SetMaxGateRetries_UpdatesValue()
    {
        var settings = ProjectSettings.Create(Guid.NewGuid());

        settings.SetMaxGateRetries(5);

        Assert.Equal(5, settings.MaxGateRetries);
    }

    [Fact]
    public void SetTokenThreshold_UpdatesValue()
    {
        var settings = ProjectSettings.Create(Guid.NewGuid());

        settings.SetTokenThreshold(50000);

        Assert.Equal(50000, settings.TokenThresholdForContextReset);
    }

    [Fact]
    public void Reconstitute_RestoresAllProperties()
    {
        var projectId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);

        var settings = ProjectSettings.Reconstitute(projectId, 5, true, false, 50000, createdAt);

        Assert.Equal(5, settings.MaxGateRetries);
        Assert.True(settings.RequireUserConfirmationForInternalLearning);
        Assert.False(settings.RequireUserConfirmationForFindingsExtraction);
        Assert.Equal(50000, settings.TokenThresholdForContextReset);
    }
}
