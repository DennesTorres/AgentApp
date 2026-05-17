using AgentApp.Domain.Providers;

namespace AgentApp.Application.Providers;

public class OrchestratorPipeline
{
    private readonly IProviderRegistry _registry;

    public OrchestratorPipeline(IProviderRegistry registry)
    {
        _registry = registry;
    }

    public Task<ProviderResponse> SendAsync(ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var provider = _registry.Resolve(request.Capability)
            ?? throw new InvalidOperationException(
                $"No provider registered for capability '{request.Capability}'.");

        return provider.HandleAsync(request, cancellationToken);
    }
}
