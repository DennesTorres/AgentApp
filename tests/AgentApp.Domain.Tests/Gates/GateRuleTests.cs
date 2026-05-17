using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Gates;

namespace AgentApp.Domain.Tests.Gates;

public class GateRuleTests
{
    [Fact]
    public void Create_ValidInputs_SetsAllProperties()
    {
        var rule = GateRule.Create("phase1-check", "Return JSON with action and files", ["action", "files"]);

        Assert.Equal("phase1-check", rule.Name);
        Assert.Equal("Return JSON with action and files", rule.RuleText);
        Assert.Contains("action", rule.RequiredOutputKeys);
        Assert.Contains("files", rule.RequiredOutputKeys);
        Assert.NotEqual(Guid.Empty, rule.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ThrowsDomainValidationException(string name)
    {
        Assert.Throws<DomainValidationException>(() =>
            GateRule.Create(name, "rule text", ["key"]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyRuleText_ThrowsDomainValidationException(string ruleText)
    {
        Assert.Throws<DomainValidationException>(() =>
            GateRule.Create("name", ruleText, ["key"]));
    }

    [Fact]
    public void Create_EmptyRequiredOutputKeys_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            GateRule.Create("name", "rule text", []));
    }

    [Fact]
    public void Reconstitute_RestoresAllProperties()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);

        var rule = GateRule.Reconstitute(id, "test-rule", "rule text", ["key1", "key2"], createdAt);

        Assert.Equal(id, rule.Id);
        Assert.Equal("test-rule", rule.Name);
        Assert.Equal("rule text", rule.RuleText);
        Assert.Equal(["key1", "key2"], rule.RequiredOutputKeys);
        Assert.Equal(createdAt, rule.CreatedAt);
    }
}
