using AgentApp.Domain.Chat;
using AgentApp.Domain.Providers;
using AgentApp.Infrastructure.ModelAccess;
using Microsoft.Extensions.AI;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AgentApp.Infrastructure.Tests.ModelAccess;

public class AzureChatClientProviderTests
{
    private static ProviderRequest MakeRequest(List<ChatTurn> history) =>
        ProviderRequest.Create(ProviderCapability.ModelCall,
            new Dictionary<string, object> { ["history"] = history });

    [Fact]
    public void Capability_IsModelCall()
    {
        var provider = new AzureChatClientProvider(Substitute.For<IChatClient>());
        Assert.Equal(ProviderCapability.ModelCall, provider.Capability);
    }

    [Fact]
    public async Task HandleAsync_OnSuccess_ReturnsTextInResult()
    {
        var chatClient = Substitute.For<IChatClient>();
        var provider = new AzureChatClientProvider(chatClient);
        var history = new List<ChatTurn> { new(ChatTurnRole.User, "Hello", DateTimeOffset.UtcNow) };
        var request = MakeRequest(history);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hi there")]));

        var response = await provider.HandleAsync(request);

        Assert.True(response.Success);
        Assert.Equal("Hi there", response.Result["text"]);
        Assert.Equal(request.RequestId, response.RequestId);
    }

    [Fact]
    public async Task HandleAsync_MissingHistoryPayload_ReturnsFail()
    {
        var provider = new AzureChatClientProvider(Substitute.For<IChatClient>());
        var request = ProviderRequest.Create(ProviderCapability.ModelCall);

        var response = await provider.HandleAsync(request);

        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ClientThrows_ReturnsFail()
    {
        var chatClient = Substitute.For<IChatClient>();
        var provider = new AzureChatClientProvider(chatClient);
        var history = new List<ChatTurn> { new(ChatTurnRole.User, "Hello", DateTimeOffset.UtcNow) };
        var request = MakeRequest(history);

        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var response = await provider.HandleAsync(request);

        Assert.False(response.Success);
        Assert.Contains("Connection failed", response.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_MapsUserAndAssistantRoles()
    {
        var chatClient = Substitute.For<IChatClient>();
        var provider = new AzureChatClientProvider(chatClient);
        var history = new List<ChatTurn>
        {
            new(ChatTurnRole.User, "Hello", DateTimeOffset.UtcNow),
            new(ChatTurnRole.Assistant, "Hi", DateTimeOffset.UtcNow),
            new(ChatTurnRole.User, "How are you?", DateTimeOffset.UtcNow)
        };
        var request = MakeRequest(history);

        IEnumerable<ChatMessage>? capturedMessages = null;
        chatClient.GetResponseAsync(
                Arg.Do<IEnumerable<ChatMessage>>(m => capturedMessages = m),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Fine")]));

        await provider.HandleAsync(request);

        var messages = capturedMessages!.ToList();
        Assert.Equal(3, messages.Count);
        Assert.Equal(ChatRole.User, messages[0].Role);
        Assert.Equal(ChatRole.Assistant, messages[1].Role);
        Assert.Equal(ChatRole.User, messages[2].Role);
    }
}
