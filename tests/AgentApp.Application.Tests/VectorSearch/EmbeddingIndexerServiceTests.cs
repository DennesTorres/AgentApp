using AgentApp.Application.VectorSearch;
using AgentApp.Domain.VectorSearch;
using NSubstitute;

namespace AgentApp.Application.Tests.VectorSearch;

public class EmbeddingIndexerServiceTests
{
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IVectorIndex _index = Substitute.For<IVectorIndex>();
    private readonly EmbeddingIndexerService _sut;

    public EmbeddingIndexerServiceTests()
    {
        _sut = new EmbeddingIndexerService(_embeddingService, _index);
    }

    [Fact]
    public async Task IndexAsync_CallsEmbeddingServiceWithContent()
    {
        var vector = EmbeddingVector.Create(new float[] { 1f, 0f });
        _embeddingService.GetEmbeddingAsync("session content", Arg.Any<CancellationToken>())
            .Returns(vector);

        await _sut.IndexAsync("sess-1", "session content");

        await _embeddingService.Received(1)
            .GetEmbeddingAsync("session content", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IndexAsync_AddsResultToVectorIndex()
    {
        var vector = EmbeddingVector.Create(new float[] { 0.5f, 0.5f });
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(vector);

        await _sut.IndexAsync("sess-1", "some content");

        _index.Received(1).Add("sess-1", "some content", vector);
    }
}
