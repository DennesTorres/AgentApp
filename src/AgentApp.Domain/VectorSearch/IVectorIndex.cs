namespace AgentApp.Domain.VectorSearch;

public interface IVectorIndex
{
    void Add(string id, string content, EmbeddingVector vector);
    IReadOnlyList<VectorSearchResult> Search(EmbeddingVector query, int topK, float minScore);
}
