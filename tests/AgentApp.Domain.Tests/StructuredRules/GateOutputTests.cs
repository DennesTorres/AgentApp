using AgentApp.Domain.StructuredRules;

namespace AgentApp.Domain.Tests.StructuredRules;

public class GateOutputTests
{
    [Fact]
    public void TryParse_ValidGateBlock_ReturnsGateOutput()
    {
        var text = "Some response text [GATE:{\"rule\":\"ILH\",\"step\":1}] more text";
        var output = GateOutput.TryParse(text);
        Assert.NotNull(output);
    }

    [Fact]
    public void TryParse_NoGateBlock_ReturnsNull()
    {
        var text = "Response with no gate block at all";
        Assert.Null(GateOutput.TryParse(text));
    }

    [Fact]
    public void TryParse_ExtractsRuleId()
    {
        var text = "[GATE:{\"rule\":\"SIP\",\"step\":2}]";
        var output = GateOutput.TryParse(text);
        Assert.Equal("SIP", output!.RuleId);
    }

    [Fact]
    public void TryParse_ExtractsStepNumber()
    {
        var text = "[GATE:{\"rule\":\"ILH\",\"step\":3}]";
        var output = GateOutput.TryParse(text);
        Assert.Equal(3, output!.StepNumber);
    }

    [Fact]
    public void TryParse_MalformedJson_ReturnsNull()
    {
        var text = "[GATE:{not-valid-json}]";
        Assert.Null(GateOutput.TryParse(text));
    }
}
