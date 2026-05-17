namespace AgentApp.Domain.Providers;

public class ProviderRequest
{
    public string RequestId { get; private set; } = string.Empty;
    public ProviderCapability Capability { get; private set; }
    public IReadOnlyDictionary<string, object> Payload { get; private set; } =
        new Dictionary<string, object>();

    private ProviderRequest() { }

    public static ProviderRequest Create(ProviderCapability capability,
        Dictionary<string, object>? payload = null) => new()
    {
        RequestId = Guid.NewGuid().ToString(),
        Capability = capability,
        Payload = payload is not null
            ? new Dictionary<string, object>(payload)
            : new Dictionary<string, object>()
    };
}
