namespace Concilia.Core.Runs;

// One task launched to model A and model B at the same time, over the same
// input. Its two runs (and their retries) point to it through Run.JobId.
public class Job
{
    public Guid Id { get; set; }

    public int ProjectId { get; set; }

    // Which task: "catalog" (v1.4, by lots) or a v0 phase: "questions",
    // "catalog-v0" or "backlog" (PhasePlan.JobKind).
    public required string Kind { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Set when both runs have finished.
    public DateTimeOffset? FinishedAt { get; set; }

    // A round of questions centred on some iterations (step 112): the numbers
    // of those iterations. Null in every other job, also in a round with all of them.
    public List<int>? Focus { get; set; }
}
