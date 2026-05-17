namespace AgentApp.Domain.VectorSearch;

public class VectorSearchResult
{
    public string Id { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public float Score { get; private set; }

    private VectorSearchResult() { }

    public static VectorSearchResult Create(string id, string content, float score) => new()
    {
        Id = id,
        Content = content,
        Score = score
    };
}
