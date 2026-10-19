using Concilia.Core.Embeddings;

namespace Concilia.Api.Embeddings;

// "Embeddings" section of the configuration (in Docker: Embeddings__Provider
// and Embeddings__ModelPath; milestone 17).
public class EmbeddingOptions
{
    // "Fake" (default: no model, counts shared words) or "Onnx" (the local
    // model). docker-compose.yml sets Onnx, because the image brings the model.
    public string Provider { get; set; } = "Fake";

    // Onnx only: the folder with model.onnx and sentencepiece.bpe.model.
    public string ModelPath { get; set; } = "";
}

public static class EmbeddingSetup
{
    // Registers the ITextEmbedder chosen in the configuration. With Onnx and a
    // missing model file it throws here, so the API fails at start-up with a
    // clear message instead of on the first comparison.
    public static IServiceCollection AddTextEmbedder(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Embeddings").Get<EmbeddingOptions>() ?? new EmbeddingOptions();
        ITextEmbedder embedder = options.Provider.ToLowerInvariant() switch
        {
            "fake" => new FakeTextEmbedder(),
            "onnx" => new OnnxTextEmbedder(options.ModelPath),
            _ => throw new InvalidOperationException(
                $"Embeddings:Provider desconocido: \"{options.Provider}\". Los valores válidos son Fake y Onnx."),
        };
        services.AddSingleton(embedder);
        return services;
    }

    // Similarity between two texts, to try the model by hand (for example,
    // the same requirement in Spanish and in English). Nothing is stored.
    public static void MapEmbeddingEndpoints(this WebApplication app)
    {
        app.MapPost("/embeddings/similarity", (SimilarityRequest request, ITextEmbedder embedder) =>
            request.A is null || request.B is null
                ? Results.BadRequest(new { error = "Faltan los textos \"a\" y \"b\"." })
                : Results.Ok(new { similarity = embedder.Similarity(request.A, request.B) }));
    }

    public record SimilarityRequest(string? A, string? B);
}
