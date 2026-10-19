using Concilia.Core.Embeddings;

namespace Concilia.Tests.Embeddings;

public class FakeTextEmbedderTests
{
    private readonly FakeTextEmbedder _embedder = new();

    [Fact]
    public void Same_words_give_similarity_one_whatever_the_case_and_punctuation()
    {
        Assert.Equal(1, _embedder.Similarity("Reservar una sala.", "reservar UNA sala"), precision: 6);
    }

    [Fact]
    public void Texts_without_words_in_common_give_similarity_zero()
    {
        Assert.Equal(0, _embedder.Similarity("reservar una sala", "book a room"), precision: 6);
    }

    [Fact]
    public void Shared_words_give_a_similarity_between_zero_and_one()
    {
        var similarity = _embedder.Similarity("reservar una sala", "cancelar una sala");

        Assert.InRange(similarity, 0.1, 0.9);
    }

    [Fact]
    public void Vectors_have_the_configured_length_and_keep_the_order()
    {
        var vectors = new FakeTextEmbedder(dimensions: 16).Embed(["uno", "dos", "uno"]);

        Assert.All(vectors, v => Assert.Equal(16, v.Length));
        Assert.Equal(vectors[0], vectors[2]);
        Assert.NotEqual(vectors[0], vectors[1]);
    }
}
