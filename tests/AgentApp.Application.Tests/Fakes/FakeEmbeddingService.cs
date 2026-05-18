using AgentApp.Domain.VectorSearch;

namespace AgentApp.Application.Tests.Fakes;

internal sealed class FakeEmbeddingService : IEmbeddingService
{
    private readonly Dictionary<string, EmbeddingVector> _responses = new();
    private EmbeddingVector _defaultVector = EmbeddingVector.Create(new float[] { 1f, 0f });

    public string? LastContent { get; private set; }

    public void SetResponse(string content, EmbeddingVector vector) => _responses[content] = vector;
    public void SetDefaultResponse(EmbeddingVector vector) => _defaultVector = vector;

    public Task<EmbeddingVector> GetEmbeddingAsync(string content, CancellationToken cancellationToken = default)
    {
        LastContent = content;
        return Task.FromResult(_responses.TryGetValue(content, out var v) ? v : _defaultVector);
    }
}
