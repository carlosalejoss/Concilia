using System.ComponentModel;
using Concilia.Api.Embeddings;
using Concilia.Api.Files;
using Concilia.Api.OpenApi;
using Concilia.Api.Persistence;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Runs;

namespace Concilia.Api.Reports;

// The matching of A and B of a job by meaning (step 87), computed with
// embeddings and without any LLM, next to what the arbiter did. Step 100: the
// embeddings are computed once per job and saved (MatchingCache); the
// thresholds and the arbiter are applied on each call.
public static class MatchingEndpoints
{
    // Phase: the phase of the job. RearbitrationOf: for a new arbitration, the
    // job whose proposals it arbitrated again. HasArbiter: whether the job has
    // a valid arbiter output to compare with. Deviations (step 88): where that
    // output goes against this matching, also for the jobs from before the
    // arbiter got it (what the platform would have marked); DeviationElements,
    // the elements of the arbiter they mark.
    // Time (step 95): what this call took the platform. FromCache (step 100):
    // the embeddings were already saved, so it took only milliseconds.
    public record MatchingView(Guid JobId, string Phase, Guid? RearbitrationOf, bool HasArbiter, string? ArbiterModel, MatchingResult Matching,
        IReadOnlyList<ArbiterDeviation>? Deviations, int? DeviationElements, ComputeTime Time, bool FromCache);

    public static void MapMatchingEndpoints(this WebApplication app)
    {
        // ?same=0.85&similar=0.6 to try other thresholds than the configured ones.
        app.MapGet("/jobs/{jobId:guid}/matching", async (Guid jobId,
            [Description("Umbral de «lo mismo» (0,90 por defecto).")] double? same,
            [Description("Umbral de «parecido» (0,70 por defecto).")] double? similar,
            AppDbContext db, FileStorage storage, ITextEmbedder embedder, EmbeddingOptions options) =>
        {
            var (job, input, error) = await JobMatching.LoadAsync(db, storage, jobId);
            if (job is null)
                return Results.NotFound();
            var thresholds = new MatchThresholds(same ?? options.SameThreshold, similar ?? options.SimilarThreshold);
            if (thresholds.Similar < 0 || thresholds.Same > 1 || thresholds.Similar >= thresholds.Same)
                return Refuse("Los umbrales deben cumplir 0 ≤ similar < same ≤ 1.");
            if (input is null)
                return Refuse(error!);

            var arbiterPairs = input.ArbiterPairs;
            var ((saved, matching, deviations), time) = ComputeTime.Measure(() =>
            {
                var saved = JobMatching.Similarities(storage, input, embedder);
                var matching = ProposalMatcher.Classify(saved.Similarities, thresholds, arbiterPairs);
                return (saved, matching, input.ArbiterOutput is null ? null
                    : ArbiterDeviations.Find(input.Plan.Kind, input.ArbiterOutput, matching, input.Plan.ProposalOrder));
            });
            return Results.Ok(new MatchingView(jobId, input.Plan.Kind.ToString(), input.Plan.RearbitrationOf, arbiterPairs is not null,
                input.Runs.FirstOrDefault(r => r.Slot == "ARB")?.Model, matching,
                deviations, deviations is null ? null : ArbiterDeviations.ElementCount(deviations), time, saved.FromCache));
        })
            .WithTags(ApiTags.Reports)
            .WithSummary("Emparejamiento de A y B por significado")
            .WithDescription("Calculado con embeddings en local, sin ningún LLM, junto a lo que hizo el árbitro: los pares «mismo» y «parecido», lo que quedó solo en A o en B y dónde se apartó el árbitro del emparejamiento. Los embeddings se calculan una vez por trabajo y se guardan (unos segundos con el modelo real la primera vez; `fromCache` dice si ya estaban), y los umbrales y el árbitro se aplican en cada llamada: `?same=0.85&similar=0.6` prueba otros umbrales sin recalcularlos.")
            .Produces<MatchingView>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);
    }

    // Step 100: A, B and the arbiter side by side, a row per element of the
    // merge and per discarded element, with the thresholds of the configuration.
    public static void MapComparisonEndpoints(this WebApplication app)
    {
        app.MapGet("/jobs/{jobId:guid}/comparison", async (Guid jobId, AppDbContext db, FileStorage storage, ITextEmbedder embedder, EmbeddingOptions options) =>
        {
            var (job, input, error) = await JobMatching.LoadAsync(db, storage, jobId);
            if (job is null)
                return Results.NotFound();
            if (input is null)
                return Refuse(error!);
            if (input.ArbiterOutput is null)
                return Refuse("El árbitro no terminó con una salida válida: no hay nada que comparar con A y B.");

            var similarities = JobMatching.Similarities(storage, input, embedder).Similarities;
            var matching = ProposalMatcher.Classify(similarities,
                new MatchThresholds(options.SameThreshold, options.SimilarThreshold), input.ArbiterPairs);
            var deviations = ArbiterDeviations.Find(input.Plan.Kind, input.ArbiterOutput, matching, input.Plan.ProposalOrder);
            return Results.Ok(JobComparison.Build(input.Plan.Kind, input.ArbiterOutput, input.Plan.ProposalOrder, matching, deviations));
        })
            .WithTags(ApiTags.Reports)
            .WithSummary("Comparación de A, B y el árbitro")
            .WithDescription("Lo que propuso cada modelo y lo que hizo el árbitro con cada elemento, lado a lado: una fila por elemento de la unión (`Merged`, con los elementos de A y de B que junta, su origen, su discrepancia y las desviaciones respecto al emparejamiento), una por cada elemento descartado (`Discarded`, con el motivo del árbitro) y una por cada elemento que el árbitro no clasificó (`Unclassified`). Cada elemento de A y de B lleva su par más parecido en la otra propuesta, su nivel («mismo», «parecido» o «distinto») y si el árbitro lo juntó con algo de la otra; `agreesWithEmbeddings` dice si los dos coinciden. Con los umbrales de la configuración; los embeddings se calculan una vez por trabajo y se guardan. Los ids del árbitro son los de su salida, antes de que la plataforma renumere el borrador.")
            .Produces<JobComparison>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static IResult Refuse(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["matching"] = [message] });
}
