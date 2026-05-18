using AgentApp.Domain.Chat;
using AgentApp.Domain.Providers;
using AgentApp.Infrastructure.ModelAccess;
using AgentApp.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.AI;

namespace AgentApp.Infrastructure.Tests.ModelAccess;

public class AzureChatClientProviderTests
{
    private static ProviderRequest MakeRequest(List<ChatTurn> history) =>
        ProviderRequest.Create(ProviderCapability.ModelCall,
            new Dictionary<string, object> { ["history"] = history });

    [Fact]
    public void Capability_IsModelCall()
    {
        var provider = new AzureChatClientProvider(new FakeChatClient());
        Assert.Equal(ProviderCapability.ModelCall, provider.Capability);
    }

    [Fact]
    public async Task HandleAsync_OnSuccess_ReturnsTextInResult()
    {
        var chatClient = new FakeChatClient();
        chatClient.SetResponse(_ => new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hi there")]));
        var provider = new AzureChatClientProvider(chatClient);
        var history = new List<ChatTurn> { new(ChatTurnRole.User, "Hello", DateTimeOffset.UtcNow) };
        var request = MakeRequest(history);

        var response = await provider.HandleAsync(request);

        Assert.True(response.Success);
        Assert.Equal("Hi there", response.Result["text"]);
        Assert.Equal(request.RequestId, response.RequestId);
    }

    [Fact]
    public async Task HandleAsync_MissingHistoryPayload_ReturnsFail()
    {
        var provider = new AzureChatClientProvider(new FakeChatClient());
        var request = ProviderRequest.Create(ProviderCapability.ModelCall);

        var response = await provider.HandleAsync(request);

        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ClientThrows_ReturnsFail()
    {
        var chatClient = new FakeChatClient();
        chatClient.SetException(new HttpRequestException("Connection failed"));
        var provider = new AzureChatClientProvider(chatClient);
        var history = new List<ChatTurn> { new(ChatTurnRole.User, "Hello", DateTimeOffset.UtcNow) };
        var request = MakeRequest(history);

        var response = await provider.HandleAsync(request);

        Assert.False(response.Success);
        Assert.Contains("Connection failed", response.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_MapsUserAndAssistantRoles()
    {
        var chatClient = new FakeChatClient();
        chatClient.SetResponse(_ => new ChatResponse([new ChatMessage(ChatRole.Assistant, "Fine")]));
        var provider = new AzureChatClientProvider(chatClient);
        var history = new List<ChatTurn>
        {
            new(ChatTurnRole.User, "Hello", DateTimeOffset.UtcNow),
            new(ChatTurnRole.Assistant, "Hi", DateTimeOffset.UtcNow),
            new(ChatTurnRole.User, "How are you?", DateTimeOffset.UtcNow)
        };
        var request = MakeRequest(history);

        await provider.HandleAsync(request);

        var messages = chatClient.CapturedMessages!.ToList();
        Assert.Equal(3, messages.Count);
        Assert.Equal(ChatRole.User, messages[0].Role);
        Assert.Equal(ChatRole.Assistant, messages[1].Role);
        Assert.Equal(ChatRole.User, messages[2].Role);
    }
}
