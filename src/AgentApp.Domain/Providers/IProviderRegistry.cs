namespace AgentApp.Domain.Providers;

public interface IProviderRegistry
{
    void Register(IProvider provider);
    IProvider? Resolve(ProviderCapability capability);
    IReadOnlyList<IProvider> GetAll();
}
