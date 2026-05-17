using AgentApp.Domain.Context;

namespace AgentApp.Domain.Tests.Context;

public class ConversationMessageTests
{
    [Fact]
    public void Create_SetsRoleAndContent()
    {
        var msg = ConversationMessage.Create(MessageRole.User, "Hello, can you help me?");

        Assert.Equal(MessageRole.User, msg.Role);
        Assert.Equal("Hello, can you help me?", msg.Content);
        Assert.NotEqual(Guid.Empty, msg.Id);
    }

    [Fact]
    public void Create_TokenEstimateIsContentLengthDividedByFour()
    {
        var content = "A".PadRight(40, 'A'); // 40 chars
        var msg = ConversationMessage.Create(MessageRole.Assistant, content);

        Assert.Equal(10, msg.TokenEstimate); // 40 / 4
    }

    [Fact]
    public void Create_ShortContent_HasAtLeastOneToken()
    {
        var msg = ConversationMessage.Create(MessageRole.System, "Hi");

        Assert.True(msg.TokenEstimate >= 1);
    }

    [Fact]
    public void Reconstitute_RestoresAllProperties()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var msg = ConversationMessage.Reconstitute(id, MessageRole.Assistant, "content", 5, createdAt);

        Assert.Equal(id, msg.Id);
        Assert.Equal(MessageRole.Assistant, msg.Role);
        Assert.Equal("content", msg.Content);
        Assert.Equal(5, msg.TokenEstimate);
        Assert.Equal(createdAt, msg.CreatedAt);
    }
}
