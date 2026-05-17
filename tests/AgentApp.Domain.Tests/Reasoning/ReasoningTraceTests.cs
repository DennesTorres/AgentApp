using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Reasoning;

namespace AgentApp.Domain.Tests.Reasoning;

public class ReasoningTraceTests
{
    [Fact]
    public void Capture_ValidInputs_SetsAllProperties()
    {
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var trace = ReasoningTrace.Capture(sessionId, messageId, "The model considered option A over B because...");

        Assert.NotEqual(Guid.Empty, trace.Id);
        Assert.Equal(sessionId, trace.SessionId);
        Assert.Equal(messageId, trace.MessageId);
        Assert.Equal("The model considered option A over B because...", trace.Content);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Capture_EmptyContent_ThrowsDomainValidationException(string content)
    {
        Assert.Throws<DomainValidationException>(() =>
            ReasoningTrace.Capture(Guid.NewGuid(), Guid.NewGuid(), content));
    }

    [Fact]
    public void Capture_EmptySessionId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            ReasoningTrace.Capture(Guid.Empty, Guid.NewGuid(), "Some reasoning content"));
    }

    [Fact]
    public void Capture_EmptyMessageId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            ReasoningTrace.Capture(Guid.NewGuid(), Guid.Empty, "Some reasoning content"));
    }

    [Fact]
    public void Reconstitute_RestoresAllProperties()
    {
        var id = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2);

        var trace = ReasoningTrace.Reconstitute(id, sessionId, messageId, "Stored reasoning content", createdAt);

        Assert.Equal(id, trace.Id);
        Assert.Equal(sessionId, trace.SessionId);
        Assert.Equal(messageId, trace.MessageId);
        Assert.Equal(createdAt, trace.CreatedAt);
    }
}
