using AgentApp.Domain.Rules;

namespace AgentApp.Domain.Tests.Rules;

public class MdFileTests
{
    [Fact]
    public void CreateGlobal_ValidNameAndContent_CreatesGlobalFile()
    {
        var file = MdFile.CreateGlobal("coding-standards", "# Coding Standards\n...");

        Assert.Equal("coding-standards", file.Name);
        Assert.Equal(MdFileScope.Global, file.Scope);
        Assert.Equal("# Coding Standards\n...", file.Content);
        Assert.Null(file.ProjectId);
        Assert.False(file.IsTechnology);
    }

    [Fact]
    public void CreateForProject_ValidInput_CreatesProjectScopedFile()
    {
        var projectId = Guid.NewGuid();
        var file = MdFile.CreateForProject("api-guidelines", "# API Guidelines", projectId);

        Assert.Equal("api-guidelines", file.Name);
        Assert.Equal(MdFileScope.Project, file.Scope);
        Assert.Equal(projectId, file.ProjectId);
        Assert.False(file.IsTechnology);
    }

    [Fact]
    public void CreateTechnology_ValidInput_CreatesTechnologyFile()
    {
        var file = MdFile.CreateTechnology("dotnet-patterns", "# .NET Patterns");

        Assert.Equal("dotnet-patterns", file.Name);
        Assert.Equal(MdFileScope.Global, file.Scope);
        Assert.True(file.IsTechnology);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateGlobal_EmptyName_ThrowsValidationException(string? name)
    {
        Assert.Throws<AgentApp.Domain.Exceptions.DomainValidationException>(
            () => MdFile.CreateGlobal(name!, "content"));
    }

    [Fact]
    public void UpdateContent_ChangesContent()
    {
        var file = MdFile.CreateGlobal("test", "original");

        file.UpdateContent("updated content");

        Assert.Equal("updated content", file.Content);
    }

    [Fact]
    public void IsCore_GlobalFileNamedCore_ReturnsTrue()
    {
        var file = MdFile.CreateGlobal("core", "# Core rules");

        Assert.True(file.IsCore);
    }

    [Fact]
    public void IsCore_NonCoreFile_ReturnsFalse()
    {
        var file = MdFile.CreateGlobal("other-rules", "content");

        Assert.False(file.IsCore);
    }
}
