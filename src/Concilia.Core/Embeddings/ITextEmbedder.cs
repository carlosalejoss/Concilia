namespace Concilia.Core.Embeddings;

// Turns texts into vectors (embeddings) whose closeness measures how close
// their meanings are, so the platform can compare the elements of A and B
// outside the LLM (milestone 17). Two implementations: OnnxTextEmbedder, the
// real local model, and FakeTextEmbedder, for the tests and the Fake provider.
public interface ITextEmbedder
{
    // Length of every vector this embedder returns (384 for the real model).
    int Dimensions { get; }

    // One vector per text, in the same order. Computing many texts in one
    // call is faster than one by one: the model processes them in batches.
    IReadOnlyList<float[]> Embed(IReadOnlyList<string> texts);
}

public static class TextEmbedderExtensions
{
    public static float[] Embed(this ITextEmbedder embedder, string text) =>
        embedder.Embed([text])[0];

    // Cosine similarity of the meanings of two texts: around 1 when they say
    // the same, lower the more they differ.
    public static double Similarity(this ITextEmbedder embedder, string a, string b)
    {
        var vectors = embedder.Embed([a, b]);
        return VectorMath.Cosine(vectors[0], vectors[1]);
    }
}
