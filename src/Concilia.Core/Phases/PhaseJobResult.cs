using Concilia.Core.Matching;

namespace Concilia.Core.Phases;

// The result of a v0 phase job: the output that the next steps use, and where
// it comes from ("ARB", or "A"/"B" when only that model finished well: no
// cross-check). Source is null when the job failed (both models, or the arbiter).
public record PhaseJobResult(string? Source, bool CrossChecked, string? Output, string Message)
{
    // data/jobs/{id}/resultado-arbitro.json: the result, frozen.
    public const string FileName = "resultado-arbitro.json";

    // Where the arbiter went against the matching of the platform (step 88).
    // Kept apart from Output, which stays the arbiter's output as it wrote it:
    // the drafts of the catalog and the backlog mark these elements as a discrepancy.
    public IReadOnlyList<ArbiterDeviation> Deviations { get; init; } = [];
}
