using AgentApp.Domain.VectorSearch;

namespace AgentApp.Application.VectorSearch;

public class InMemoryVectorIndex : IVectorIndex
{
    private readonly List<(string Id, string Content, EmbeddingVector Vector)> _entries = new();
    private readonly object _lock = new();

    public void Add(string id, string content, EmbeddingVector vector)
    {
        lock (_lock)
        {
            _entries.Add((id, content, vector));
        }
    }

    public IReadOnlyList<VectorSearchResult> Search(EmbeddingVector query, int topK, float minScore)
    {
        lock (_lock)
        {
            return _entries
                .Select(e => VectorSearchResult.Create(e.Id, e.Content, e.Vector.CosineSimilarity(query)))
                .Where(r => r.Score >= minScore)
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .ToList();
        }
    }
}
