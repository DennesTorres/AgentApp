using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Providers;

namespace AgentApp.Infrastructure.FileSystem;

public class FileWriteProvider : IProvider
{
    public ProviderCapability Capability => ProviderCapability.FileWrite;

    public async Task<ProviderResponse> HandleAsync(ProviderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var path = (string)request.Payload["path"];
            var content = (string)request.Payload["content"];
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(path, content, cancellationToken);
            return ProviderResponse.Ok(request.RequestId);
        }
        catch (Exception ex)
        {
            return ProviderResponse.Fail(request.RequestId, ex.Message);
        }
    }
}
