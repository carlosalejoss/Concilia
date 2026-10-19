using Concilia.Core.Embeddings;

namespace Concilia.Tests.Embeddings;

public class VectorMathTests
{
    [Fact]
    public void Cosine_is_one_for_the_same_direction_whatever_the_length()
    {
        Assert.Equal(1, VectorMath.Cosine([1, 2, 3], [2, 4, 6]), precision: 6);
    }

    [Fact]
    public void Cosine_is_zero_for_perpendicular_vectors_and_minus_one_for_opposite_ones()
    {
        Assert.Equal(0, VectorMath.Cosine([1, 0], [0, 1]), precision: 6);
        Assert.Equal(-1, VectorMath.Cosine([1, 2], [-1, -2]), precision: 6);
    }

    [Fact]
    public void Cosine_with_a_zero_vector_is_zero()
    {
        Assert.Equal(0, VectorMath.Cosine([0, 0, 0], [1, 2, 3]));
    }

    [Fact]
    public void Cosine_rejects_vectors_of_different_lengths()
    {
        Assert.Throws<ArgumentException>(() => VectorMath.Cosine([1, 2], [1, 2, 3]));
    }

    [Fact]
    public void Mean_pooling_averages_the_real_tokens_and_ignores_the_padding()
    {
        // Three tokens of two dimensions; the last one is padding (mask 0).
        float[] tokens = [1, 2, 3, 4, 100, 100];
        long[] mask = [1, 1, 0];

        Assert.Equal([2f, 3f], VectorMath.MeanPooling(tokens, mask, dimensions: 2));
    }

    [Fact]
    public void Mean_pooling_without_real_tokens_is_the_zero_vector()
    {
        Assert.Equal([0f, 0f], VectorMath.MeanPooling([5, 5], [0], dimensions: 2));
    }
}
