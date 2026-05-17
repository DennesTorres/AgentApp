using AgentApp.Domain.Projects;
using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Tests.Projects;

public class ProjectTests
{
    [Fact]
    public void Create_ValidNameAndFolder_ReturnsProject()
    {
        var project = Project.Create("MyProject", @"C:\Projects\MyProject");

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal("MyProject", project.Name);
        Assert.Equal(@"C:\Projects\MyProject", project.ProjectFolderPath);
        Assert.True(project.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrNullName_ThrowsValidationException(string? name)
    {
        Assert.Throws<DomainValidationException>(() => Project.Create(name!, @"C:\Projects"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrNullFolder_ThrowsValidationException(string? folder)
    {
        Assert.Throws<DomainValidationException>(() => Project.Create("MyProject", folder!));
    }

    [Fact]
    public void ControlFolderPath_IsUnderAppUserFolder()
    {
        var project = Project.Create("MyProject", @"C:\Projects\MyProject");
        var appUserFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.StartsWith(appUserFolder, project.ControlFolderPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(project.Id.ToString(), project.ControlFolderPath);
    }

    [Fact]
    public void TwoProjects_WithSameName_HaveDifferentControlFolders()
    {
        var p1 = Project.Create("MyProject", @"C:\Projects\P1");
        var p2 = Project.Create("MyProject", @"C:\Projects\P2");

        Assert.NotEqual(p1.ControlFolderPath, p2.ControlFolderPath);
    }
}
