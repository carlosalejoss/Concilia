using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;
using Concilia.Core.Runs;

namespace Concilia.Api.Reports;

// What the matching, the comparison and the report need from a job of
// questions, catalog or backlog: its plan, the proposals of A and B and the
// arbiter's output (null if the arbiter did not end well).
public record JobMatchingInput(Job Job, PhasePlan Plan, IReadOnlyList<Run> Runs, string ProposalA, string ProposalB, string? ArbiterOutput)
{
    // The pairs the arbiter joined, or null without arbiter output.
    public HashSet<(string A, string B)>? ArbiterPairs =>
        ArbiterOutput is null ? null : ProposalMatcher.ArbiterPairs(Plan.Kind, ArbiterOutput, Plan.ProposalOrder);
}

// Similarities: the slow part of the matching. FromCache: it was already
// saved (MatchingCache), so this use cost no embeddings.
public record JobSimilarities(ProposalSimilarities Similarities, bool FromCache);

public static class JobMatching
{
    // The input of a job, or why it has none. Job: null if it does not exist.
    public static async Task<(Job? Job, JobMatchingInput? Input, string? Error)> LoadAsync(AppDbContext db, FileStorage storage, Guid jobId)
    {
        var job = await db.Jobs.FindAsync(jobId);
        if (job is null)
            return (null, null, null);
        var runs = await db.Runs.Where(r => r.JobId == jobId).ToListAsync();
        var (input, error) = Load(storage, job, runs);
        return (job, input, error);
    }

    public static (JobMatchingInput? Input, string? Error) Load(FileStorage storage, Job job, IReadOnlyList<Run> runs)
    {
        var folder = storage.JobFolder(job.Id);
        if (!File.Exists(Path.Combine(folder, PhasePlan.FileName)))
            return (null, "El trabajo no es de preguntas, catálogo o backlog.");
        var plan = PhasePlan.Load(folder);
        if (plan.Kind is not (PhaseKind.Questions or PhaseKind.Catalog or PhaseKind.Backlog))
            return (null, "Solo se emparejan las propuestas de preguntas, catálogo o backlog.");

        var a = JobProposals.Read(storage, job, runs, plan.Kind, "A");
        var b = JobProposals.Read(storage, job, runs, plan.Kind, "B");
        if (a is null || b is null)
            return (null, "El trabajo no tiene una propuesta válida de A y de B.");

        // The arbiter's output, only if it is really the arbiter's ("fuente": "ARB").
        var result = ReportEndpoints.ReadResult(folder);
        var arbiterOutput = result?["fuente"]?.GetValue<string>() == "ARB" && result["salida"] is JsonNode salida
            ? salida.ToJsonString() : null;
        return (new JobMatchingInput(job, plan, runs, a, b, arbiterOutput), null);
    }

    // The saved similarities of the job or, the first time, computed with
    // the embeddings and saved (step 100).
    public static JobSimilarities Similarities(FileStorage storage, JobMatchingInput input, ITextEmbedder embedder)
    {
        var folder = storage.JobFolder(input.Job.Id);
        if (MatchingCache.Read(folder, embedder.Name) is { } saved)
            return new JobSimilarities(saved, FromCache: true);
        var kind = input.Plan.Kind;
        var computed = ProposalMatcher.Similarities(
            ProposalMatcher.ElementsOf(kind, input.ProposalA), ProposalMatcher.ElementsOf(kind, input.ProposalB), embedder);
        MatchingCache.Write(folder, computed);
        return new JobSimilarities(computed, FromCache: false);
    }
}
