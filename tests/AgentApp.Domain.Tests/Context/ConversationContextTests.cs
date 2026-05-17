using AgentApp.Domain.Context;

namespace AgentApp.Domain.Tests.Context;

public class ConversationContextTests
{
    [Fact]
    public void AddMessage_IncreasesTokenEstimate()
    {
        var ctx = new ConversationContext();
        var msg = ConversationMessage.Create(MessageRole.User, "A".PadRight(40, 'A'));

        ctx.AddMessage(msg);

        Assert.True(ctx.TokenEstimate > 0);
        Assert.Single(ctx.Messages);
    }

    [Fact]
    public void TokenEstimate_SumsAllMessages()
    {
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "A".PadRight(40, 'A')));    // 10 tokens
        ctx.AddMessage(ConversationMessage.Create(MessageRole.Assistant, "B".PadRight(80, 'B'))); // 20 tokens

        Assert.Equal(30, ctx.TokenEstimate);
    }

    [Fact]
    public void IsNearLimit_TokensBelowThreshold_ReturnsFalse()
    {
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "short"));

        Assert.False(ctx.IsNearLimit(1000));
    }

    [Fact]
    public void IsNearLimit_TokensAtOrAboveThreshold_ReturnsTrue()
    {
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "A".PadRight(400, 'A'))); // 100 tokens

        Assert.True(ctx.IsNearLimit(50));
    }

    [Fact]
    public void Reset_ClearsAllMessages()
    {
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "message"));

        ctx.Reset();

        Assert.Empty(ctx.Messages);
        Assert.Equal(0, ctx.TokenEstimate);
    }

    [Fact]
    public void Reset_WithSeedMessages_StartsWithSeed()
    {
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "old message"));

        var seed = ConversationMessage.Create(MessageRole.System, "Summary of conversation");
        ctx.Reset([seed]);

        Assert.Single(ctx.Messages);
        Assert.Equal(MessageRole.System, ctx.Messages[0].Role);
    }
}
