using AgentApp.Domain.Projects;
using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Tests.Projects;

public class ProjectTests
{
    [Fact]
    public void Create_ValidInput_ReturnsProject()
    {
        var project = Project.Create("MyProject", "my-project", @"C:\Projects\MyProject");

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal("MyProject", project.Name);
        Assert.Equal("my-project", project.FolderName);
        Assert.Equal(@"C:\Projects\MyProject", project.ProjectFolderPath);
        Assert.True(project.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrNullName_ThrowsValidationException(string? name)
    {
        Assert.Throws<DomainValidationException>(() => Project.Create(name!, "my-project", @"C:\Projects"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrNullFolderName_ThrowsValidationException(string? folder)
    {
        Assert.Throws<DomainValidationException>(() => Project.Create("MyProject", folder!, @"C:\Projects"));
    }

    [Fact]
    public void ControlFolderPath_IsUnderTowerFolder()
    {
        var project = Project.Create("MyProject", "my-project", @"C:\Projects\MyProject");
        var towerRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tower");

        Assert.StartsWith(towerRoot, project.ControlFolderPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("my-project", project.ControlFolderPath);
    }

    [Fact]
    public void TwoProjects_WithDifferentFolderNames_HaveDifferentControlFolders()
    {
        var p1 = Project.Create("ProjectA", "project-a", @"C:\Projects\P1");
        var p2 = Project.Create("ProjectB", "project-b", @"C:\Projects\P2");

        Assert.NotEqual(p1.ControlFolderPath, p2.ControlFolderPath);
    }

    [Fact]
    public void ToFolderName_ConvertsToLowercaseHyphens()
    {
        Assert.Equal("my-project", Project.ToFolderName("My Project"));
        Assert.Equal("hello-world", Project.ToFolderName("Hello_World"));
        Assert.Equal("simple", Project.ToFolderName("  Simple  "));
    }
}
