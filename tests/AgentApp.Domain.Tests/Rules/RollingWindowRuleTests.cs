using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Rules;

namespace AgentApp.Domain.Tests.Rules;

public class RollingWindowRuleTests
{
    [Fact]
    public void Create_ValidInputs_SetsAllProperties()
    {
        var rule = RollingWindowRule.Create("session-memory", 50000, 30, MdFileScope.Global, null);

        Assert.NotEqual(Guid.Empty, rule.Id);
        Assert.Equal("session-memory", rule.FileType);
        Assert.Equal(50000, rule.MaxSizeChars);
        Assert.Equal(30, rule.RetentionDays);
        Assert.Equal(MdFileScope.Global, rule.Scope);
        Assert.Null(rule.ProjectId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyFileType_ThrowsDomainValidationException(string fileType)
    {
        Assert.Throws<DomainValidationException>(() =>
            RollingWindowRule.Create(fileType, 50000, 30, MdFileScope.Global, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositiveMaxSizeChars_ThrowsDomainValidationException(int maxSize)
    {
        Assert.Throws<DomainValidationException>(() =>
            RollingWindowRule.Create("session-memory", maxSize, 30, MdFileScope.Global, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositiveRetentionDays_ThrowsDomainValidationException(int days)
    {
        Assert.Throws<DomainValidationException>(() =>
            RollingWindowRule.Create("session-memory", 50000, days, MdFileScope.Global, null));
    }

    [Fact]
    public void Reconstitute_RestoresAllProperties()
    {
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-5);

        var rule = RollingWindowRule.Reconstitute(id, "impl-notes", 10000, 7, MdFileScope.Project, projectId, createdAt);

        Assert.Equal(id, rule.Id);
        Assert.Equal("impl-notes", rule.FileType);
        Assert.Equal(10000, rule.MaxSizeChars);
        Assert.Equal(7, rule.RetentionDays);
        Assert.Equal(MdFileScope.Project, rule.Scope);
        Assert.Equal(projectId, rule.ProjectId);
        Assert.Equal(createdAt, rule.CreatedAt);
    }
}
