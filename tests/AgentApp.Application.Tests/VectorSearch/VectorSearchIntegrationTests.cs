using AgentApp.Application.Tests.Fakes;
using AgentApp.Application.VectorSearch;
using AgentApp.Domain.Providers;
using AgentApp.Domain.VectorSearch;
using System.Text.Json;

namespace AgentApp.Application.Tests.VectorSearch;

/// <summary>
/// US-149: Full simulation — session archived -> model requests out-of-scope data ->
/// vector search retrieves and injects correct session summary.
/// </summary>
public class VectorSearchIntegrationTests
{
    [Fact]
    public async Task FullScenario_SessionArchived_ModelRequestsContext_VectorSearchRetrievesCorrectSummary()
    {
        var embeddingService = new FakeEmbeddingService();
        embeddingService.SetResponse("React hooks best practices session summary",
            EmbeddingVector.Create(new float[] { 1f, 0f, 0f }));
        embeddingService.SetResponse("Azure deployment pipeline session summary",
            EmbeddingVector.Create(new float[] { 0f, 1f, 0f }));
        embeddingService.SetResponse("CSS grid layout tutorial session summary",
            EmbeddingVector.Create(new float[] { 0f, 0f, 1f }));
        embeddingService.SetResponse("React component performance optimization",
            EmbeddingVector.Create(new float[] { 0.98f, 0.1f, 0.05f }));

        var index = new InMemoryVectorIndex();
        var indexer = new EmbeddingIndexerService(embeddingService, index);

        await indexer.IndexAsync("session-react", "React hooks best practices session summary");
        await indexer.IndexAsync("session-azure", "Azure deployment pipeline session summary");
        await indexer.IndexAsync("session-css", "CSS grid layout tutorial session summary");

        var settings = new VectorSearchSettings { SearchSensitivity = 0.5f, TopK = 1 };
        var provider = new VectorSearchProvider(embeddingService, index, settings);
        var request = ProviderRequest.Create(
            ProviderCapability.EmbeddingSearch,
            new Dictionary<string, object> { ["query"] = "React component performance optimization" });

        var response = await provider.HandleAsync(request);

        Assert.True(response.Success);
        Assert.True(response.Result.ContainsKey("results"));

        var resultsJson = JsonSerializer.Serialize(response.Result["results"]);
        Assert.Contains("session-react", resultsJson);
        Assert.DoesNotContain("session-azure", resultsJson);
        Assert.DoesNotContain("session-css", resultsJson);
    }
}
