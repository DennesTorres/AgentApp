using AgentApp.Application.VectorSearch;
using AgentApp.Domain.VectorSearch;

namespace AgentApp.Application.Tests.VectorSearch;

public class InMemoryVectorIndexTests
{
    [Fact]
    public void Search_EmptyIndex_ReturnsEmpty()
    {
        var index = new InMemoryVectorIndex();
        var query = EmbeddingVector.Create(new float[] { 1f, 0f });
        var results = index.Search(query, topK: 5, minScore: 0f);
        Assert.Empty(results);
    }

    [Fact]
    public void Add_ThenSearch_ReturnsMatch()
    {
        var index = new InMemoryVectorIndex();
        var vector = EmbeddingVector.Create(new float[] { 1f, 0f, 0f });
        index.Add("id-1", "content one", vector);

        var results = index.Search(EmbeddingVector.Create(new float[] { 1f, 0f, 0f }), topK: 5, minScore: 0.9f);

        Assert.Single(results);
        Assert.Equal("id-1", results[0].Id);
        Assert.Equal("content one", results[0].Content);
    }

    [Fact]
    public void Search_TopK_LimitsResults()
    {
        var index = new InMemoryVectorIndex();
        for (int i = 0; i < 5; i++)
            index.Add($"id-{i}", $"content {i}", EmbeddingVector.Create(new float[] { 1f, 0f }));

        var results = index.Search(EmbeddingVector.Create(new float[] { 1f, 0f }), topK: 3, minScore: 0f);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void Search_MinScore_FiltersLowSimilarity()
    {
        var index = new InMemoryVectorIndex();
        index.Add("similar", "similar content", EmbeddingVector.Create(new float[] { 1f, 0f }));
        index.Add("dissimilar", "dissimilar content", EmbeddingVector.Create(new float[] { 0f, 1f }));

        var results = index.Search(EmbeddingVector.Create(new float[] { 1f, 0f }), topK: 5, minScore: 0.9f);

        Assert.Single(results);
        Assert.Equal("similar", results[0].Id);
    }

    [Fact]
    public void Search_ReturnsResultsInDescendingScoreOrder()
    {
        var index = new InMemoryVectorIndex();
        index.Add("low", "low sim", EmbeddingVector.Create(new float[] { 0.5f, 0.5f }));
        index.Add("high", "high sim", EmbeddingVector.Create(new float[] { 1f, 0f }));

        var results = index.Search(EmbeddingVector.Create(new float[] { 1f, 0f }), topK: 5, minScore: 0f);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].Score >= results[1].Score);
        Assert.Equal("high", results[0].Id);
    }
}
