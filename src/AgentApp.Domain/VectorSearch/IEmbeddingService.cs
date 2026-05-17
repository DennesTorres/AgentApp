namespace AgentApp.Domain.VectorSearch;

public interface IEmbeddingService
{
    Task<EmbeddingVector> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
