namespace AgentApp.Domain.VectorSearch;

public class EmbeddingVector
{
    public float[] Values { get; private set; }

    private EmbeddingVector(float[] values)
    {
        Values = values;
    }

    public static EmbeddingVector Create(float[] values)
    {
        if (values.Length == 0)
            throw new ArgumentException("Values must not be empty.", nameof(values));
        return new EmbeddingVector(values.ToArray());
    }

    public float CosineSimilarity(EmbeddingVector other)
    {
        if (Values.Length != other.Values.Length)
            throw new ArgumentException("Vectors must have the same dimension.");

        float dot = 0f, magA = 0f, magB = 0f;
        for (int i = 0; i < Values.Length; i++)
        {
            dot += Values[i] * other.Values[i];
            magA += Values[i] * Values[i];
            magB += other.Values[i] * other.Values[i];
        }

        if (magA == 0f || magB == 0f) return 0f;
        return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
    }
}
