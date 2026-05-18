using AgentApp.Domain.Providers;

namespace AgentApp.Application.Tests.Fakes;

internal sealed class FakeProvider : IProvider
{
    private Func<ProviderRequest, CancellationToken, Task<ProviderResponse>>? _handler;

    public ProviderCapability Capability { get; }
    public ProviderRequest? LastRequest { get; private set; }
    public int CallCount { get; private set; }

    public FakeProvider(ProviderCapability capability,
        Func<ProviderRequest, ProviderResponse>? handler = null)
    {
        Capability = capability;
        if (handler != null)
            _handler = (r, _) => Task.FromResult(handler(r));
    }

    public void SetResponse(Func<ProviderRequest, ProviderResponse> handler)
        => _handler = (r, _) => Task.FromResult(handler(r));

    public Task<ProviderResponse> HandleAsync(ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        CallCount++;
        return _handler != null
            ? _handler(request, cancellationToken)
            : Task.FromResult(ProviderResponse.Ok(request.RequestId));
    }
}
