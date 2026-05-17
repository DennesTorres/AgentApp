using AgentApp.Domain.Providers;
using AgentApp.Domain.VectorSearch;

namespace AgentApp.Application.VectorSearch;

public class VectorSearchProvider : IProvider
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorIndex _index;
    private readonly VectorSearchSettings _settings;

    public VectorSearchProvider(IEmbeddingService embeddingService, IVectorIndex index,
        VectorSearchSettings settings)
    {
        _embeddingService = embeddingService;
        _index = index;
        _settings = settings;
    }

    public ProviderCapability Capability => ProviderCapability.EmbeddingSearch;

    public async Task<ProviderResponse> HandleAsync(ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Payload.TryGetValue("query", out var queryObj) || queryObj is not string query)
            return ProviderResponse.Fail(request.RequestId, "Missing or invalid 'query' payload.");

        EmbeddingVector queryVector;
        try
        {
            queryVector = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            return ProviderResponse.Fail(request.RequestId, $"Embedding failed: {ex.Message}");
        }

        var results = _index.Search(queryVector, _settings.TopK, _settings.SearchSensitivity);
        var serialized = results.Select(r => new { r.Id, r.Content, r.Score }).ToList();

        return ProviderResponse.Ok(request.RequestId,
            new Dictionary<string, object> { ["results"] = serialized });
    }
}
