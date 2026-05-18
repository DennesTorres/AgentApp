using AgentApp.Domain.VectorSearch;

namespace AgentApp.Application.Tests.Fakes;

internal sealed class FakeVectorIndex : IVectorIndex
{
    private readonly List<(string Id, string Content, EmbeddingVector Vector)> _entries = new();

    public string? LastId { get; private set; }
    public string? LastContent { get; private set; }
    public EmbeddingVector? LastVector { get; private set; }

    public void Add(string id, string content, EmbeddingVector vector)
    {
        LastId = id;
        LastContent = content;
        LastVector = vector;
        _entries.Add((id, content, vector));
    }

    public IReadOnlyList<VectorSearchResult> Search(EmbeddingVector query, int topK, float minScore)
        => _entries.Select(e => VectorSearchResult.Create(e.Id, e.Content, 1.0f)).ToList();
}
