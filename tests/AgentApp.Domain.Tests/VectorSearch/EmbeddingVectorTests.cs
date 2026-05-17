using AgentApp.Domain.VectorSearch;

namespace AgentApp.Domain.Tests.VectorSearch;

public class EmbeddingVectorTests
{
    [Fact]
    public void Create_WithValues_StoresValues()
    {
        var values = new float[] { 1f, 0f, 0f };
        var vector = EmbeddingVector.Create(values);
        Assert.Equal(values, vector.Values);
    }

    [Fact]
    public void Create_WithEmptyArray_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => EmbeddingVector.Create(Array.Empty<float>()));
    }

    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var v = EmbeddingVector.Create(new float[] { 1f, 0f, 0f });
        var similarity = v.CosineSimilarity(EmbeddingVector.Create(new float[] { 1f, 0f, 0f }));
        Assert.Equal(1f, similarity, precision: 5);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        var a = EmbeddingVector.Create(new float[] { 1f, 0f });
        var b = EmbeddingVector.Create(new float[] { 0f, 1f });
        var similarity = a.CosineSimilarity(b);
        Assert.Equal(0f, similarity, precision: 5);
    }

    [Fact]
    public void CosineSimilarity_DifferentDimensions_ThrowsArgumentException()
    {
        var a = EmbeddingVector.Create(new float[] { 1f, 0f });
        var b = EmbeddingVector.Create(new float[] { 1f, 0f, 0f });
        Assert.Throws<ArgumentException>(() => a.CosineSimilarity(b));
    }
}
