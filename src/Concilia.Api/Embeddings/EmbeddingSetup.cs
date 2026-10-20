using System.ComponentModel;
using Concilia.Api.OpenApi;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;

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

    // The two thresholds of the matching of A and B (step 87): from SameThreshold
    // up, two elements say the same; from SimilarThreshold up, they are close but
    // not the same. Fixed by the author on 19-10-2026 after the calibration with
    // 48 pairs labelled by hand by the author (docs/calibracion-embeddings/): the best
    // intervals were 0.89–0.92 and 0.70–0.72. /jobs/{id}/matching accepts others.
    public double SameThreshold { get; set; } = 0.90;
    public double SimilarThreshold { get; set; } = 0.70;
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
        services.AddSingleton(options);
        // The matching that the arbiter of every job gets (step 88).
        services.AddSingleton(new ArbiterMatching(embedder, new MatchThresholds(options.SameThreshold, options.SimilarThreshold)));
        return services;
    }

    // Similarity between two texts, to try the model by hand (for example,
    // the same requirement in Spanish and in English). Nothing is stored.
    public static void MapEmbeddingEndpoints(this WebApplication app)
    {
        app.MapPost("/embeddings/similarity", (SimilarityRequest request, ITextEmbedder embedder) =>
            request.A is null || request.B is null
                ? Results.BadRequest(new { error = "Faltan los textos \"a\" y \"b\"." })
                : Results.Ok(new SimilarityView(embedder.Similarity(request.A, request.B))))
            .WithTags(ApiTags.Reports)
            .WithSummary("Similitud entre dos textos")
            .WithDescription("El coseno de sus embeddings, calculados en local (por ejemplo, el mismo requisito en español y en inglés). No se guarda nada.")
            .Produces<SimilarityView>()
            .Produces(StatusCodes.Status400BadRequest);
    }

    public record SimilarityRequest(
        [property: Description("El primer texto.")] string? A,
        [property: Description("El segundo texto.")] string? B);

    // Similarity: from -1 to 1; from 0.90 up, the two texts say the same (step 87).
    public record SimilarityView(double Similarity);
}
