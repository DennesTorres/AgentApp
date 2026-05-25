using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Providers;

namespace AgentApp.Infrastructure.FileSystem;

public class FileReadProvider : IProvider
{
    public ProviderCapability Capability => ProviderCapability.FileRead;

    public async Task<ProviderResponse> HandleAsync(ProviderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var path = (string)request.Payload["path"];
            var content = await File.ReadAllTextAsync(path, cancellationToken);
            return ProviderResponse.Ok(request.RequestId, new Dictionary<string, object> { ["content"] = content });
        }
        catch (Exception ex)
        {
            return ProviderResponse.Fail(request.RequestId, ex.Message);
        }
    }
}
