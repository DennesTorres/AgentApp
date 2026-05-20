using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Providers;

namespace AgentApp.Infrastructure.FileSystem;

public class DirectoryListProvider : IProvider
{
    public ProviderCapability Capability => ProviderCapability.DirectoryList;

    public Task<ProviderResponse> HandleAsync(ProviderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var path = (string)request.Payload["path"];
            if (!Directory.Exists(path))
                return Task.FromResult(ProviderResponse.Fail(request.RequestId, $"Directory not found: {path}"));

            var entries = Directory.EnumerateFileSystemEntries(path)
                .Select(Path.GetFileName)
                .Where(n => n is not null)
                .Cast<string>()
                .ToList();

            return Task.FromResult(ProviderResponse.Ok(request.RequestId,
                new Dictionary<string, object> { ["entries"] = (IReadOnlyList<string>)entries }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ProviderResponse.Fail(request.RequestId, ex.Message));
        }
    }
}
