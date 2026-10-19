using System.Text.Json;
using Concilia.Core.Embeddings;

namespace Concilia.Tests.Embeddings;

// The model is loaded once for all the tests of the class (about a second).
public sealed class OnnxEmbedderFixture : IDisposable
{
    public OnnxTextEmbedder? Embedder { get; } =
        EmbeddingModel.Folder is null ? null : new OnnxTextEmbedder(EmbeddingModel.Folder);

    public void Dispose() => Embedder?.Dispose();
}

// The C# service against the original Python libraries: the token ids of the
// Hugging Face tokenizer and the vectors of sentence-transformers, saved in
// fixtures/embeddings/referencia.json by generar_referencia.py.
public class OnnxTextEmbedderTests(OnnxEmbedderFixture fixture) : IClassFixture<OnnxEmbedderFixture>
{
    private OnnxTextEmbedder Embedder => fixture.Embedder!;

    private static readonly List<Case> Reference = LoadReference();

    [ModelFact]
    public void Token_ids_are_the_same_as_the_hugging_face_tokenizer()
    {
        var tokenizer = XlmRobertaTokenizer.FromFile(Path.Combine(EmbeddingModel.Folder!, OnnxTextEmbedder.TokenizerFileName));

        Assert.All(Reference, c => Assert.Equal(c.Ids, tokenizer.Encode(c.Texto, OnnxTextEmbedder.MaxTokens)));
    }

    [ModelFact]
    public void A_text_longer_than_the_model_keeps_its_first_128_tokens()
    {
        var tokenizer = XlmRobertaTokenizer.FromFile(Path.Combine(EmbeddingModel.Folder!, OnnxTextEmbedder.TokenizerFileName));

        var ids = tokenizer.Encode(Reference[^1].Texto, OnnxTextEmbedder.MaxTokens);

        Assert.Equal(128, ids.Length);
        Assert.Equal(XlmRobertaTokenizer.BeginId, ids[0]);
        Assert.Equal(XlmRobertaTokenizer.EndId, ids[^1]);
    }

    [ModelFact]
    public void Vectors_are_the_same_as_sentence_transformers()
    {
        var vectors = Embedder.Embed(Reference.Select(c => c.Texto).ToList());

        for (var i = 0; i < Reference.Count; i++)
        {
            Assert.Equal(384, vectors[i].Length);
            // Rounding to 6 decimals and the float arithmetic of each library
            // leave differences far below what changes a comparison.
            Assert.True(VectorMath.Cosine(vectors[i], Reference[i].Vector) > 0.99999, $"Caso {i}: «{Reference[i].Texto}»");
            Assert.All(vectors[i].Zip(Reference[i].Vector), pair => Assert.Equal(pair.Second, pair.First, 0.00001f));
        }
    }

    [ModelFact]
    public void A_text_gets_the_same_vector_alone_or_in_a_batch_with_padding()
    {
        var shortText = "Reservar una sala.";
        var alone = Embedder.Embed(shortText);
        var inBatch = Embedder.Embed([Reference[^1].Texto, shortText])[1];

        Assert.All(alone.Zip(inBatch), pair => Assert.Equal(pair.First, pair.Second, 0.00001f));
    }

    [ModelFact]
    public void More_texts_than_one_batch_keep_their_order()
    {
        var texts = Enumerable.Range(0, 40).Select(i => $"Requisito número {i} del catálogo.").ToList();

        var vectors = Embedder.Embed(texts);

        Assert.Equal(40, vectors.Count);
        Assert.Equal(1, VectorMath.Cosine(vectors[37], Embedder.Embed(texts[37])), precision: 5);
    }

    [ModelFact]
    public void The_same_requirement_in_spanish_and_english_is_closer_than_a_different_one()
    {
        var sameInEnglish = Embedder.Similarity(
            "El sistema permitirá reservar una sala de reuniones.",
            "The system shall allow users to book a meeting room.");
        var different = Embedder.Similarity(
            "El sistema permitirá reservar una sala de reuniones.",
            "El informe mensual de ocupación se exporta en PDF.");

        Assert.True(sameInEnglish > 0.8, $"Mismo requisito en dos idiomas: {sameInEnglish:F3}");
        Assert.True(different < 0.5, $"Requisitos distintos: {different:F3}");
    }

    [Fact]
    public void Missing_model_files_fail_with_a_clear_message()
    {
        using var temp = new TempDirectory();

        var error = Assert.Throws<FileNotFoundException>(() => new OnnxTextEmbedder(temp.Path));

        Assert.Contains("model.onnx", error.Message);
        Assert.Contains("descargar-modelo-embeddings.ps1", error.Message);
    }

    private sealed record Case(string Texto, long[] Ids, float[] Vector);

    private sealed record ReferenceFile(List<Case> Casos);

    private static List<Case> LoadReference()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "embeddings", "referencia.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<ReferenceFile>(File.ReadAllText(path), options)!.Casos;
    }
}
