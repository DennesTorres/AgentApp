using AgentApp.Application.Tests.Fakes;
using AgentApp.Application.VectorSearch;
using AgentApp.Domain.VectorSearch;

namespace AgentApp.Application.Tests.VectorSearch;

public class EmbeddingIndexerServiceTests
{
    private readonly FakeEmbeddingService _embeddingService = new();
    private readonly FakeVectorIndex _index = new();
    private readonly EmbeddingIndexerService _sut;

    public EmbeddingIndexerServiceTests()
    {
        _sut = new EmbeddingIndexerService(_embeddingService, _index);
    }

    [Fact]
    public async Task IndexAsync_CallsEmbeddingServiceWithContent()
    {
        _embeddingService.SetDefaultResponse(EmbeddingVector.Create(new float[] { 1f, 0f }));

        await _sut.IndexAsync("sess-1", "session content");

        Assert.Equal("session content", _embeddingService.LastContent);
    }

    [Fact]
    public async Task IndexAsync_AddsResultToVectorIndex()
    {
        _embeddingService.SetDefaultResponse(EmbeddingVector.Create(new float[] { 0.5f, 0.5f }));

        await _sut.IndexAsync("sess-1", "some content");

        Assert.Equal("sess-1", _index.LastId);
        Assert.Equal("some content", _index.LastContent);
    }
}
