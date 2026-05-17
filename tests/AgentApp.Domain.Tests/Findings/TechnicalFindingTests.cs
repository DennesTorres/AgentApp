using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Findings;

namespace AgentApp.Domain.Tests.Findings;

public class TechnicalFindingTests
{
    [Fact]
    public void Create_ValidInputs_SetsAllProperties()
    {
        var finding = TechnicalFinding.Create("dotnet", "Always use async/await over .Result to avoid deadlocks.");

        Assert.Equal("dotnet", finding.Technology);
        Assert.Equal("Always use async/await over .Result to avoid deadlocks.", finding.Content);
        Assert.NotEqual(Guid.Empty, finding.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTechnology_ThrowsDomainValidationException(string technology)
    {
        Assert.Throws<DomainValidationException>(() =>
            TechnicalFinding.Create(technology, "Some content about something important."));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyContent_ThrowsDomainValidationException(string content)
    {
        Assert.Throws<DomainValidationException>(() =>
            TechnicalFinding.Create("dotnet", content));
    }

    [Fact]
    public void Reconstitute_RestoresAllProperties()
    {
        var id = Guid.NewGuid();
        var extractedAt = DateTimeOffset.UtcNow.AddHours(-1);

        var finding = TechnicalFinding.Reconstitute(id, "azure", "Use managed identity over secrets.", extractedAt);

        Assert.Equal(id, finding.Id);
        Assert.Equal("azure", finding.Technology);
        Assert.Equal(extractedAt, finding.ExtractedAt);
    }
}
