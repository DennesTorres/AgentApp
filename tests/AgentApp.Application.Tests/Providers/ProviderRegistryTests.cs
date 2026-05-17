using AgentApp.Application.Providers;
using AgentApp.Domain.Providers;
using NSubstitute;

namespace AgentApp.Application.Tests.Providers;

public class ProviderRegistryTests
{
    [Fact]
    public void Register_AndResolve_ReturnsProvider()
    {
        var registry = new ProviderRegistry();
        var provider = Substitute.For<IProvider>();
        provider.Capability.Returns(ProviderCapability.ModelCall);

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
        var first = Substitute.For<IProvider>();
        var second = Substitute.For<IProvider>();
        first.Capability.Returns(ProviderCapability.ModelCall);
        second.Capability.Returns(ProviderCapability.ModelCall);

        registry.Register(first);
        registry.Register(second);

        Assert.Same(second, registry.Resolve(ProviderCapability.ModelCall));
    }

    [Fact]
    public void GetAll_ReturnsAllRegisteredProviders()
    {
        var registry = new ProviderRegistry();
        var modelProvider = Substitute.For<IProvider>();
        var credentialProvider = Substitute.For<IProvider>();
        modelProvider.Capability.Returns(ProviderCapability.ModelCall);
        credentialProvider.Capability.Returns(ProviderCapability.CredentialAccess);

        registry.Register(modelProvider);
        registry.Register(credentialProvider);

        Assert.Equal(2, registry.GetAll().Count);
    }

    [Fact]
    public void GetAll_EmptyRegistry_ReturnsEmpty()
    {
        var registry = new ProviderRegistry();

        Assert.Empty(registry.GetAll());
    }
}
