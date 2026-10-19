using System.Text;

namespace Concilia.Core.Embeddings;

// An embedder without a model, for the tests and the Fake provider: it counts
// words, it does not understand them. Each word (in lower case) adds 1 to one
// position of the vector, chosen by a hash of the word. So two texts with the
// same words get the same vector, and texts with no word in common get
// similarity 0; "reservar una sala" and "book a room" get 0 too, unlike with
// the real model. Always the same result for the same text.
public sealed class FakeTextEmbedder(int dimensions = 384) : ITextEmbedder
{
    public int Dimensions => dimensions;

    public IReadOnlyList<float[]> Embed(IReadOnlyList<string> texts) => texts.Select(EmbedOne).ToList();

    private float[] EmbedOne(string text)
    {
        var vector = new float[dimensions];
        foreach (var word in Words(text))
            vector[Fnv1a(word) % (uint)dimensions] += 1;
        return vector;
    }

    private static IEnumerable<string> Words(string text)
    {
        var word = new StringBuilder();
        foreach (var c in text.ToLowerInvariant() + " ")
        {
            if (char.IsLetterOrDigit(c)) { word.Append(c); continue; }
            if (word.Length == 0) continue;
            yield return word.ToString();
            word.Clear();
        }
    }

    // FNV-1a, a simple hash that is the same in every run (string.GetHashCode
    // changes from one start of the process to the next).
    private static uint Fnv1a(string word)
    {
        var hash = 2166136261u;
        foreach (var b in Encoding.UTF8.GetBytes(word))
            hash = (hash ^ b) * 16777619u;
        return hash;
    }
}
