namespace AgentApp.Domain.Providers;

public interface IProvider
{
    ProviderCapability Capability { get; }
    Task<ProviderResponse> HandleAsync(ProviderRequest request, CancellationToken cancellationToken = default);
}
