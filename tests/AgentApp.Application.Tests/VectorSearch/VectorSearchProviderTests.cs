using AgentApp.Application.VectorSearch;
using AgentApp.Domain.Providers;
using AgentApp.Domain.VectorSearch;
using NSubstitute;

namespace AgentApp.Application.Tests.VectorSearch;

public class VectorSearchProviderTests
{
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IVectorIndex _index = Substitute.For<IVectorIndex>();
    private readonly VectorSearchSettings _settings = new() { SearchSensitivity = 0.7f, TopK = 5 };
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
        var vector = EmbeddingVector.Create(new float[] { 1f, 0f });
        _embeddingService.GetEmbeddingAsync("search term", Arg.Any<CancellationToken>())
            .Returns(vector);
        _index.Search(Arg.Any<EmbeddingVector>(), Arg.Any<int>(), Arg.Any<float>())
            .Returns(new List<VectorSearchResult>());

        var request = ProviderRequest.Create(ProviderCapability.EmbeddingSearch,
            new Dictionary<string, object> { ["query"] = "search term" });

        await _sut.HandleAsync(request);

        await _embeddingService.Received(1)
            .GetEmbeddingAsync("search term", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithResults_ReturnsSuccess()
    {
        var vector = EmbeddingVector.Create(new float[] { 1f, 0f });
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(vector);
        _index.Search(Arg.Any<EmbeddingVector>(), Arg.Any<int>(), Arg.Any<float>())
            .Returns(new List<VectorSearchResult>
            {
                VectorSearchResult.Create("id-1", "content", 0.95f)
            });

        var request = ProviderRequest.Create(ProviderCapability.EmbeddingSearch,
            new Dictionary<string, object> { ["query"] = "anything" });

        var response = await _sut.HandleAsync(request);

        Assert.True(response.Success);
        Assert.True(response.Result.ContainsKey("results"));
    }
}
