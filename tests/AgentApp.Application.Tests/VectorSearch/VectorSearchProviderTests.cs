using AgentApp.Application.Tests.Fakes;
using AgentApp.Application.VectorSearch;
using AgentApp.Domain.Providers;
using AgentApp.Domain.VectorSearch;

namespace AgentApp.Application.Tests.VectorSearch;

public class VectorSearchProviderTests
{
    private readonly FakeEmbeddingService _embeddingService = new();
    private readonly InMemoryVectorIndex _index = new();
    private readonly VectorSearchSettings _settings = new() { SearchSensitivity = 0.0f, TopK = 5 };
    private readonly VectorSearchProvider _sut;

    public VectorSearchProviderTests()
    {
        _sut = new VectorSearchProvider(_embeddingService, _index, _settings);
    }

    [Fact]
    public void Capability_IsEmbeddingSearch()
    {
        Assert.Equal(ProviderCapability.EmbeddingSearch, _sut.Capability);
    }

    [Fact]
    public async Task HandleAsync_MissingQuery_ReturnsFail()
    {
        var request = ProviderRequest.Create(ProviderCapability.EmbeddingSearch);

        var response = await _sut.HandleAsync(request);

        Assert.False(response.Success);
        Assert.Contains("query", response.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_WithQuery_CallsEmbeddingService()
    {
        _embeddingService.SetDefaultResponse(EmbeddingVector.Create(new float[] { 1f, 0f }));
        var request = ProviderRequest.Create(ProviderCapability.EmbeddingSearch,
            new Dictionary<string, object> { ["query"] = "search term" });

        await _sut.HandleAsync(request);

        Assert.Equal("search term", _embeddingService.LastContent);
    }

    [Fact]
    public async Task HandleAsync_WithResults_ReturnsSuccess()
    {
        var vector = EmbeddingVector.Create(new float[] { 1f, 0f });
        _embeddingService.SetDefaultResponse(vector);
        _index.Add("id-1", "content", vector);

        var request = ProviderRequest.Create(ProviderCapability.EmbeddingSearch,
            new Dictionary<string, object> { ["query"] = "anything" });

        var response = await _sut.HandleAsync(request);

        Assert.True(response.Success);
        Assert.True(response.Result.ContainsKey("results"));
    }
}
