using AgentApp.Domain.VectorSearch;

namespace AgentApp.Application.VectorSearch;

public class EmbeddingIndexerService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorIndex _index;

    public EmbeddingIndexerService(IEmbeddingService embeddingService, IVectorIndex index)
    {
        _embeddingService = embeddingService;
        _index = index;
    }

    public async Task IndexAsync(string sessionId, string content,
        CancellationToken cancellationToken = default)
    {
        var vector = await _embeddingService.GetEmbeddingAsync(content, cancellationToken);
        _index.Add(sessionId, content, vector);
    }
}
