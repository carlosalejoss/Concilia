namespace ReqArbiter.Core.Runs;

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
}
