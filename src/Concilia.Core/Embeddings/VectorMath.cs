namespace Concilia.Core.Embeddings;

// The two calculations around the model, kept apart so they can be tested
// without it.
public static class VectorMath
{
    // Cosine of the angle between two vectors: dot(a, b) / (|a| |b|). It goes
    // from -1 to 1 and ignores the length of the vectors, only their direction.
    // A zero vector has no direction: its similarity with anything is 0.
    public static double Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException($"Los vectores tienen longitudes distintas ({a.Length} y {b.Length}).");

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return normA == 0 || normB == 0 ? 0 : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    // Mean pooling, the way sentence-transformers turns the model output (one
    // vector per token) into one vector per text with this model
    // (1_Pooling/config.json: pooling_mode_mean_tokens). The padding added to
    // fill a batch has mask 0 and does not count, so a text gets the same
    // vector alone or in a batch with longer ones.
    //   tokenVectors: [tokens][dimensions] of one text; mask: 1 real token, 0 padding.
    public static float[] MeanPooling(ReadOnlySpan<float> tokenVectors, ReadOnlySpan<long> mask, int dimensions)
    {
        if (tokenVectors.Length != mask.Length * dimensions)
            throw new ArgumentException("El número de vectores de token no coincide con la máscara.");

        var sum = new double[dimensions];
        var count = 0;
        for (var token = 0; token < mask.Length; token++)
        {
            if (mask[token] == 0) continue;
            count++;
            var vector = tokenVectors.Slice(token * dimensions, dimensions);
            for (var d = 0; d < dimensions; d++) sum[d] += vector[d];
        }

        // sentence-transformers divides by max(count, 1e-9): with no real
        // token the result is the zero vector.
        var mean = new float[dimensions];
        if (count == 0) return mean;
        for (var d = 0; d < dimensions; d++) mean[d] = (float)(sum[d] / count);
        return mean;
    }
}
