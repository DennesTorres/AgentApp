using AgentApp.Domain.Chat;
using AgentApp.Domain.Providers;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.ModelAccess;

namespace AgentApp.Infrastructure.Tests.ModelAccess;

public class AzureChatClientProviderTests
{
    private static AzureChatClientProvider CreateProvider() =>
        new(AzureClientFactory.BuildFromCredentials(new WindowsCredentialManager())!);

    private static ProviderRequest MakeRequest(List<ChatTurn> history) =>
        ProviderRequest.Create(ProviderCapability.ModelCall,
            new Dictionary<string, object> { ["history"] = history });

    [Fact]
    public void Capability_IsModelCall()
    {
        var provider = CreateProvider();
        Assert.Equal(ProviderCapability.ModelCall, provider.Capability);
    }

    [Fact]
    public async Task HandleAsync_OnSuccess_ReturnsTextInResult()
    {
        var provider = CreateProvider();
        var history = new List<ChatTurn> { new(ChatTurnRole.User, "Say hello in one word.", DateTimeOffset.UtcNow) };
        var request = MakeRequest(history);

        var response = await provider.HandleAsync(request);

        Assert.True(response.Success);
        Assert.True(response.Result.ContainsKey("text"));
        Assert.False(string.IsNullOrWhiteSpace(response.Result["text"].ToString()));
    }

    [Fact]
    public async Task HandleAsync_MissingHistoryPayload_ReturnsFail()
    {
        var provider = CreateProvider();
        var request = ProviderRequest.Create(ProviderCapability.ModelCall);

        var response = await provider.HandleAsync(request);

        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
    }
}
