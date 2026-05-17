using AgentApp.Application.VectorSearch;
using AgentApp.Domain.Providers;
using AgentApp.Domain.VectorSearch;
using NSubstitute;
using System.Text.Json;

namespace AgentApp.Application.Tests.VectorSearch;

/// <summary>
/// US-149: Full simulation — session archived → model requests out-of-scope data →
/// vector search retrieves and injects correct session summary.
/// </summary>
public class VectorSearchIntegrationTests
{
    [Fact]
    public async Task FullScenario_SessionArchived_ModelRequestsContext_VectorSearchRetrievesCorrectSummary()
    {
        // Arrange — fake embedding service maps specific strings to known unit vectors
        var embeddingService = Substitute.For<IEmbeddingService>();

        embeddingService
            .GetEmbeddingAsync("React hooks best practices session summary", Arg.Any<CancellationToken>())
            .Returns(EmbeddingVector.Create(new float[] { 1f, 0f, 0f }));

        embeddingService
            .GetEmbeddingAsync("Azure deployment pipeline session summary", Arg.Any<CancellationToken>())
            .Returns(EmbeddingVector.Create(new float[] { 0f, 1f, 0f }));

        embeddingService
            .GetEmbeddingAsync("CSS grid layout tutorial session summary", Arg.Any<CancellationToken>())
            .Returns(EmbeddingVector.Create(new float[] { 0f, 0f, 1f }));

        // Query vector is close to the React vector
        embeddingService
            .GetEmbeddingAsync("React component performance optimization", Arg.Any<CancellationToken>())
            .Returns(EmbeddingVector.Create(new float[] { 0.98f, 0.1f, 0.05f }));

        var index = new InMemoryVectorIndex();
        var indexer = new EmbeddingIndexerService(embeddingService, index);

        // Act — index three archived session summaries (US-146: archived summaries indexed)
        await indexer.IndexAsync("session-react", "React hooks best practices session summary");
        await indexer.IndexAsync("session-azure", "Azure deployment pipeline session summary");
        await indexer.IndexAsync("session-css", "CSS grid layout tutorial session summary");

        // Model requests context via provider protocol (US-148: context-request protocol)
        var settings = new VectorSearchSettings { SearchSensitivity = 0.5f, TopK = 1 };
        var provider = new VectorSearchProvider(embeddingService, index, settings);
        var request = ProviderRequest.Create(
            ProviderCapability.EmbeddingSearch,
            new Dictionary<string, object> { ["query"] = "React component performance optimization" });

        var response = await provider.HandleAsync(request);

        // Assert — correct session summary retrieved and injected (US-147, US-149)
        Assert.True(response.Success);
        Assert.True(response.Result.ContainsKey("results"));

        var resultsJson = JsonSerializer.Serialize(response.Result["results"]);
        Assert.Contains("session-react", resultsJson);
        Assert.DoesNotContain("session-azure", resultsJson);
        Assert.DoesNotContain("session-css", resultsJson);
    }
}
