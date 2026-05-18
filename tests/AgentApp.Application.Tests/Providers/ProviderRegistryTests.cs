using AgentApp.Application.Providers;
using AgentApp.Domain.Providers;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.ModelAccess;

namespace AgentApp.Application.Tests.Providers;

public class ProviderRegistryTests
{
    private static AzureChatClientProvider CreateProvider() =>
        new(AzureClientFactory.BuildFromCredentials(new WindowsCredentialManager())!);

    [Fact]
    public void Register_AndResolve_ReturnsProvider()
    {
        var registry = new ProviderRegistry();
        var provider = CreateProvider();

        registry.Register(provider);
        var resolved = registry.Resolve(ProviderCapability.ModelCall);

        Assert.Same(provider, resolved);
    }

    [Fact]
    public void Resolve_UnregisteredCapability_ReturnsNull()
    {
        var registry = new ProviderRegistry();

        var result = registry.Resolve(ProviderCapability.ModelCall);

        Assert.Null(result);
    }

    [Fact]
    public void Register_SameCapabilityTwice_LastOneWins()
    {
        var registry = new ProviderRegistry();
        var first = CreateProvider();
        var second = CreateProvider();

        registry.Register(first);
        registry.Register(second);

        Assert.Same(second, registry.Resolve(ProviderCapability.ModelCall));
    }

    [Fact]
    public void GetAll_ReturnsRegisteredProviders()
    {
        var registry = new ProviderRegistry();
        var provider = CreateProvider();

        registry.Register(provider);

        Assert.Single(registry.GetAll());
    }

    [Fact]
    public void GetAll_EmptyRegistry_ReturnsEmpty()
    {
        var registry = new ProviderRegistry();

        Assert.Empty(registry.GetAll());
    }
}
