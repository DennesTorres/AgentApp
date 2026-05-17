using AgentApp.Domain.Providers;

namespace AgentApp.Application.Providers;

public class ProviderRegistry : IProviderRegistry
{
    private readonly Dictionary<ProviderCapability, IProvider> _providers = new();

    public void Register(IProvider provider) =>
        _providers[provider.Capability] = provider;

    public IProvider? Resolve(ProviderCapability capability) =>
        _providers.TryGetValue(capability, out var provider) ? provider : null;

    public IReadOnlyList<IProvider> GetAll() =>
        _providers.Values.ToList();
}
