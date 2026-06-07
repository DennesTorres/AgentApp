using AgentApp.Domain.Providers;

namespace AgentApp.Application.Tests.Fakes;

public class FakeModelProvider : IProvider
{
    private readonly string _response;

    public FakeModelProvider(string response) => _response = response;

    public string LastSystemMessage { get; private set; } = string.Empty;

    public ProviderCapability Capability => ProviderCapability.ModelCall;

    public Task<ProviderResponse> HandleAsync(ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Payload.TryGetValue("systemMessage", out var sm))
            LastSystemMessage = sm as string ?? string.Empty;

        return Task.FromResult(ProviderResponse.Ok(request.RequestId,
            new Dictionary<string, object> { ["text"] = _response }));
    }
}
