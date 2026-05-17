using AgentApp.Domain.StructuredRules;

namespace AgentApp.Domain.Tests.StructuredRules;

public class StructuredRuleTests
{
    [Fact]
    public void Create_WithSteps_SetsProperties()
    {
        var rule = StructuredRule.Create("ILH", "Investigation Lookup Hierarchy",
            "Before reading any file", ["Read test-cycles.md", "Read branch-records.md"]);

        Assert.Equal("ILH", rule.Id);
        Assert.Equal("Investigation Lookup Hierarchy", rule.Name);
        Assert.Equal(2, rule.Steps.Count);
        Assert.Equal("Read test-cycles.md", rule.Steps[0]);
        Assert.Equal("Read branch-records.md", rule.Steps[1]);
    }

    [Fact]
    public void Create_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            StructuredRule.Create("", "name", "trigger", ["step1"]));
    }

    [Fact]
    public void Create_EmptySteps_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            StructuredRule.Create("ILH", "name", "trigger", []));
    }

    [Fact]
    public void ToJson_ContainsRuleId()
    {
        var rule = StructuredRule.Create("ILH", "ILH Rule", "trigger", ["Step 1", "Step 2"]);
        var json = rule.ToJson();
        Assert.Contains("ILH", json);
    }

    [Fact]
    public void ToJson_ContainsSteps()
    {
        var rule = StructuredRule.Create("ILH", "ILH Rule", "trigger", ["Read test-cycles.md"]);
        var json = rule.ToJson();
        Assert.Contains("Read test-cycles.md", json);
    }

    [Fact]
    public void StepCount_Matches_Steps_Count()
    {
        var rule = StructuredRule.Create("SIP", "SIP", "trigger", ["Step A", "Step B", "Step C"]);
        Assert.Equal(3, rule.StepCount);
    }
}
