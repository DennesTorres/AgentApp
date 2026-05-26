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

    // US-189: AlwaysAllowedPaths
    [Fact]
    public void Create_AlwaysAllowedPaths_IsEmpty()
    {
        var settings = ProjectSettings.Create(Guid.NewGuid());

        Assert.Empty(settings.AlwaysAllowedPaths);
    }

    [Fact]
    public void AddAlwaysAllowedPath_AddsPathToList()
    {
        var settings = ProjectSettings.Create(Guid.NewGuid());

        settings.AddAlwaysAllowedPath(@"C:\MyProject");

        Assert.Single(settings.AlwaysAllowedPaths);
        Assert.Equal(@"C:\MyProject", settings.AlwaysAllowedPaths[0]);
    }

    [Fact]
    public void AddAlwaysAllowedPath_DuplicateCaseInsensitive_NotAdded()
    {
        var settings = ProjectSettings.Create(Guid.NewGuid());
        settings.AddAlwaysAllowedPath(@"C:\MyProject");

        settings.AddAlwaysAllowedPath(@"c:\myproject");

        Assert.Single(settings.AlwaysAllowedPaths);
    }

    [Fact]
    public void Reconstitute_WithAlwaysAllowedPaths_RestoresPaths()
    {
        var paths = new List<string> { @"C:\Folder1", @"C:\Folder2" };

        var settings = ProjectSettings.Reconstitute(
            Guid.NewGuid(), null, null, null, null, DateTimeOffset.UtcNow, paths);

        Assert.Equal(2, settings.AlwaysAllowedPaths.Count);
        Assert.Contains(@"C:\Folder1", settings.AlwaysAllowedPaths);
        Assert.Contains(@"C:\Folder2", settings.AlwaysAllowedPaths);
    }
}
