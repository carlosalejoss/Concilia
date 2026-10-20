using System.ComponentModel;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Embeddings;
using Concilia.Api.Files;
using Concilia.Api.OpenApi;
using Concilia.Api.Persistence;
using Concilia.Api.Runs;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;
using Concilia.Core.Runs;

namespace Concilia.Api.Reports;

// The matching of A and B of a job by meaning (step 87), computed with
// embeddings and without any LLM, next to what the arbiter did. Nothing is
// stored: it is computed on each call (a few seconds with the real model).
public static class MatchingEndpoints
{
    // Phase: the phase of the job. RearbitrationOf: for a new arbitration, the
    // job whose proposals it arbitrated again. HasArbiter: whether the job has
    // a valid arbiter output to compare with. Deviations (step 88): where that
    // output goes against this matching, also for the jobs from before the
    // arbiter got it (what the platform would have marked); DeviationElements,
    // the elements of the arbiter they mark.
    // Time (step 95): what this computation took the platform.
    public record MatchingView(Guid JobId, string Phase, Guid? RearbitrationOf, bool HasArbiter, string? ArbiterModel, MatchingResult Matching,
        IReadOnlyList<ArbiterDeviation>? Deviations, int? DeviationElements, ComputeTime Time);

    public static void MapMatchingEndpoints(this WebApplication app)
    {
        // ?same=0.85&similar=0.6 to try other thresholds than the configured ones.
        app.MapGet("/jobs/{jobId:guid}/matching", async (Guid jobId,
            [Description("Umbral de «lo mismo» (0,90 por defecto).")] double? same,
            [Description("Umbral de «parecido» (0,70 por defecto).")] double? similar,
            AppDbContext db, FileStorage storage, ITextEmbedder embedder, EmbeddingOptions options) =>
        {
            var job = await db.Jobs.FindAsync(jobId);
            if (job is null)
                return Results.NotFound();

            var thresholds = new MatchThresholds(same ?? options.SameThreshold, similar ?? options.SimilarThreshold);
            if (thresholds.Similar < 0 || thresholds.Same > 1 || thresholds.Similar >= thresholds.Same)
                return Refuse("Los umbrales deben cumplir 0 ≤ similar < same ≤ 1.");

            var folder = storage.JobFolder(jobId);
            if (!File.Exists(Path.Combine(folder, PhasePlan.FileName)))
                return Refuse("El trabajo no es de preguntas, catálogo o backlog.");
            var plan = PhasePlan.Load(folder);
            if (plan.Kind is not (PhaseKind.Questions or PhaseKind.Catalog or PhaseKind.Backlog))
                return Refuse("Solo se emparejan las propuestas de preguntas, catálogo o backlog.");

            var runs = await db.Runs.Where(r => r.JobId == jobId).ToListAsync();
            var a = JobProposals.Read(storage, job, runs, plan.Kind, "A");
            var b = JobProposals.Read(storage, job, runs, plan.Kind, "B");
            if (a is null || b is null)
                return Refuse("El trabajo no tiene una propuesta válida de A y de B.");

            // The arbiter's output, only if it is really the arbiter's ("fuente": "ARB").
            var result = ReportEndpoints.ReadResult(folder);
            var arbiterOutput = result?["fuente"]?.GetValue<string>() == "ARB" && result["salida"] is JsonNode salida
                ? salida.ToJsonString() : null;
            var arbiterPairs = arbiterOutput is null ? null : ProposalMatcher.ArbiterPairs(plan.Kind, arbiterOutput, plan.ProposalOrder);

            var ((matching, deviations), time) = ComputeTime.Measure(() =>
            {
                var matching = ProposalMatcher.Match(ProposalMatcher.ElementsOf(plan.Kind, a), ProposalMatcher.ElementsOf(plan.Kind, b),
                    embedder, thresholds, arbiterPairs);
                return (matching, arbiterOutput is null ? null : ArbiterDeviations.Find(plan.Kind, arbiterOutput, matching, plan.ProposalOrder));
            });
            return Results.Ok(new MatchingView(jobId, plan.Kind.ToString(), plan.RearbitrationOf, arbiterPairs is not null,
                runs.FirstOrDefault(r => r.Slot == "ARB")?.Model, matching,
                deviations, deviations is null ? null : ArbiterDeviations.ElementCount(deviations), time));
        })
            .WithTags(ApiTags.Reports)
            .WithSummary("Emparejamiento de A y B por significado")
            .WithDescription("Calculado con embeddings en local, sin ningún LLM, junto a lo que hizo el árbitro: los pares «mismo» y «parecido», lo que quedó solo en A o en B y dónde se apartó el árbitro del emparejamiento. No se guarda: se calcula en cada llamada (unos segundos con el modelo real). `?same=0.85&similar=0.6` prueba otros umbrales.")
            .Produces<MatchingView>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static IResult Refuse(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["matching"] = [message] });
}
