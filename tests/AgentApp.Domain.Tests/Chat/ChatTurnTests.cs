using AgentApp.Domain.Chat;

namespace AgentApp.Domain.Tests.Chat;

public class ChatTurnTests
{
    [Fact]
    public void ChatTurn_StoresRoleAndContent()
    {
        var ts = DateTimeOffset.UtcNow;
        var turn = new ChatTurn(ChatTurnRole.User, "Hello", ts);

        Assert.Equal(ChatTurnRole.User, turn.Role);
        Assert.Equal("Hello", turn.Content);
        Assert.Equal(ts, turn.Timestamp);
    }

    [Fact]
    public void ChatTurn_AssistantRole_ReflectsCorrectly()
    {
        var turn = new ChatTurn(ChatTurnRole.Assistant, "Hi there", DateTimeOffset.UtcNow);

        Assert.Equal(ChatTurnRole.Assistant, turn.Role);
    }
}
