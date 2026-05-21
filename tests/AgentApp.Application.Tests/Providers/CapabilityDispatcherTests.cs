using AgentApp.Application.Providers;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Providers;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.ModelAccess;

namespace AgentApp.Application.Tests.Providers;

public class CapabilityDispatcherTests
{
    private static AzureChatClientProvider CreateModelProvider() =>
        new(AzureClientFactory.BuildFromCredentials(new WindowsCredentialManager())!);

    [Fact]
    public async Task SendAsync_DispatchesToMatchingProvider()
    {
        var provider = CreateModelProvider();
        var registry = new ProviderRegistry();
        registry.Register(provider);
        var dispatcher = new CapabilityDispatcher(registry);
        var request = ProviderRequest.Create(ProviderCapability.ModelCall,
            new Dictionary<string, object>
            {
                ["history"] = new List<ChatTurn> { new(ChatTurnRole.User, "Say hello.", DateTimeOffset.UtcNow) }
            });

        var response = await dispatcher.SendAsync(request);

        Assert.NotNull(response);
        Assert.Equal(request.RequestId, response.RequestId);
    }

    [Fact]
    public async Task SendAsync_NoProviderRegistered_ThrowsInvalidOperation()
    {
        var registry = new ProviderRegistry();
        var dispatcher = new CapabilityDispatcher(registry);
        var request = ProviderRequest.Create(ProviderCapability.ModelCall);

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.SendAsync(request));
    }
}
