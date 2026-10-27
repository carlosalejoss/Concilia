using Microsoft.EntityFrameworkCore;
using Concilia.Api.Embeddings;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Core.Deliverables;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;
using Concilia.Core.Runs;

namespace Concilia.Api.Reports;

// Comparison: null when there is nothing to compare, and Unavailable says why.
// FromCache: the embeddings were already saved.
public record PreviousVersionView(Guid JobId, string? Unavailable, PreviousVersionComparison? Comparison, bool FromCache);

// Step 115: a job of catalog or backlog from scratch against the version the
// deliverable had just before (PreviousVersion). Both are in the history and
// never change, so the embeddings are computed once per job and saved.
public static class JobPreviousVersion
{
    public static async Task<PreviousVersionView> LoadAsync(AppDbContext db, FileStorage storage, ITextEmbedder embedder, EmbeddingOptions options, Job job)
    {
        PreviousVersionView None(string why) => new(job.Id, why, null, false);

        // Only a generation from scratch: an update brings its own changes,
        // and a new arbitration does not change the deliverable.
        var phase = PhasePlan.KindOfJob(job.Kind);
        if (phase is not (PhaseKind.Catalog or PhaseKind.Backlog))
            return None("Solo se compara con la versión anterior un catálogo o un backlog generado desde cero.");
        var kind = phase == PhaseKind.Catalog ? DeliverableKind.Catalog : DeliverableKind.Backlog;

        var versions = db.DeliverableVersions.Where(v => v.ProjectId == job.ProjectId && v.Kind == kind);
        var generated = await versions
            .Where(v => v.JobId == job.Id && v.Origin == VersionOrigin.Generated)
            .OrderBy(v => v.Number)
            .FirstOrDefaultAsync();
        if (generated is null)
            return None("El trabajo no generó ninguna versión.");
        var previous = await versions
            .Where(v => v.Number < generated.Number)
            .OrderByDescending(v => v.Number)
            .FirstOrDefaultAsync();
        if (previous is null)
            return None("Es la primera versión: no hay nada antes con qué compararla.");

        var folder = storage.JobFolder(job.Id);
        var saved = MatchingCache.Read(folder, embedder.Name, PreviousVersion.FileName);
        var similarities = saved ?? PreviousVersion.Similarities(phase.Value, generated.Content, previous.Content, embedder);
        if (saved is null)
            MatchingCache.Write(folder, similarities, PreviousVersion.FileName);
        var comparison = PreviousVersion.Classify(previous.Number, previous.Status, similarities,
            new MatchThresholds(options.SameThreshold, options.SimilarThreshold));
        return new PreviousVersionView(job.Id, null, comparison, saved is not null);
    }
}
