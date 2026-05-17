namespace AgentApp.Application.VectorSearch;

public class VectorSearchSettings
{
    public string EmbeddingModelEndpoint { get; set; } = string.Empty;
    public float SearchSensitivity { get; set; } = 0.7f;
    public int TopK { get; set; } = 5;
}
