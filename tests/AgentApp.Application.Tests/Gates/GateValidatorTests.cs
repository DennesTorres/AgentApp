using AgentApp.Application.Gates;
using AgentApp.Domain.Gates;

namespace AgentApp.Application.Tests.Gates;

public class GateValidatorTests
{
    private readonly GateValidator _sut = new();
    private readonly GateRule _rule = GateRule.Create("phase1", "Produce gate output", ["action", "files"]);

    [Fact]
    public void Validate_ResponseWithAllRequiredKeys_ReturnsPass()
    {
        var response = "Some text\n```gate-output\n{\"action\": \"load\", \"files\": [\"a.md\"]}\n```\nMore text";

        var result = _sut.Validate(response, _rule);

        Assert.True(result.IsValid);
        Assert.Empty(result.MissingKeys);
    }

    [Fact]
    public void Validate_ResponseMissingKeys_ReturnsFail()
    {
        var response = "```gate-output\n{\"action\": \"load\"}\n```";

        var result = _sut.Validate(response, _rule);

        Assert.False(result.IsValid);
        Assert.Contains("files", result.MissingKeys);
        Assert.DoesNotContain("action", result.MissingKeys);
    }

    [Fact]
    public void Validate_ResponseWithNoGateBlock_ReturnsFailWithAllKeysMissing()
    {
        var response = "Just a plain text response";

        var result = _sut.Validate(response, _rule);

        Assert.False(result.IsValid);
        Assert.Contains("action", result.MissingKeys);
        Assert.Contains("files", result.MissingKeys);
    }

    [Fact]
    public void Validate_ResponseWithInvalidJson_ReturnsFailWithAllKeysMissing()
    {
        var response = "```gate-output\nnot valid json\n```";

        var result = _sut.Validate(response, _rule);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.MissingKeys.Count);
    }

    [Fact]
    public void StripGateOutput_ResponseWithGateBlock_RemovesBlock()
    {
        var response = "User text\n```gate-output\n{\"action\": \"load\"}\n```\nMore text";

        var stripped = _sut.StripGateOutput(response);

        Assert.DoesNotContain("gate-output", stripped);
        Assert.DoesNotContain("action", stripped);
        Assert.Contains("User text", stripped);
        Assert.Contains("More text", stripped);
    }

    [Fact]
    public void StripGateOutput_ResponseWithNoGateBlock_ReturnsUnchanged()
    {
        var response = "Just plain text";

        var stripped = _sut.StripGateOutput(response);

        Assert.Equal("Just plain text", stripped);
    }
}
