namespace AgentApp.Domain.Providers;

public class ProviderResponse
{
    public string RequestId { get; private set; } = string.Empty;
    public bool Success { get; private set; }
    public IReadOnlyDictionary<string, object> Result { get; private set; } =
        new Dictionary<string, object>();
    public string? ErrorMessage { get; private set; }

    private ProviderResponse() { }

    public static ProviderResponse Ok(string requestId, Dictionary<string, object>? result = null) => new()
    {
        RequestId = requestId,
        Success = true,
        Result = result is not null
            ? new Dictionary<string, object>(result)
            : new Dictionary<string, object>()
    };

    public static ProviderResponse Fail(string requestId, string errorMessage) => new()
    {
        RequestId = requestId,
        Success = false,
        Result = new Dictionary<string, object>(),
        ErrorMessage = errorMessage
    };
}
