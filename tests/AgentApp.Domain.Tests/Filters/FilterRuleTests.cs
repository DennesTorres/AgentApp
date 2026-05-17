using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Filters;
using AgentApp.Domain.Rules;

namespace AgentApp.Domain.Tests.Filters;

public class FilterRuleTests
{
    [Fact]
    public void Create_ValidInputs_SetsAllProperties()
    {
        var rule = FilterRule.Create("truncate-bash", FilterTarget.Filter1, "bash-output",
            FilterTransformation.Truncate, maxLength: 1000, MdFileScope.Global, projectId: null);

        Assert.Equal("truncate-bash", rule.Name);
        Assert.Equal(FilterTarget.Filter1, rule.Target);
        Assert.Equal("bash-output", rule.MessageType);
        Assert.Equal(FilterTransformation.Truncate, rule.Transformation);
        Assert.Equal(1000, rule.MaxLength);
        Assert.Equal(MdFileScope.Global, rule.Scope);
        Assert.Null(rule.ProjectId);
        Assert.NotEqual(Guid.Empty, rule.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ThrowsDomainValidationException(string name)
    {
        Assert.Throws<DomainValidationException>(() =>
            FilterRule.Create(name, FilterTarget.Filter1, "bash-output",
                FilterTransformation.Passthrough, null, MdFileScope.Global, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyMessageType_ThrowsDomainValidationException(string messageType)
    {
        Assert.Throws<DomainValidationException>(() =>
            FilterRule.Create("rule", FilterTarget.Filter1, messageType,
                FilterTransformation.Passthrough, null, MdFileScope.Global, null));
    }

    [Fact]
    public void Reconstitute_RestoresAllProperties()
    {
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);

        var rule = FilterRule.Reconstitute(id, "strip-gate", FilterTarget.Filter2, "model-response",
            FilterTransformation.StripGateBlocks, null, MdFileScope.Project, projectId, createdAt);

        Assert.Equal(id, rule.Id);
        Assert.Equal("strip-gate", rule.Name);
        Assert.Equal(FilterTarget.Filter2, rule.Target);
        Assert.Equal(MdFileScope.Project, rule.Scope);
        Assert.Equal(projectId, rule.ProjectId);
        Assert.Equal(createdAt, rule.CreatedAt);
    }
}
