namespace Concilia.Core.Deliverables;

public enum DeliverableKind { Catalog, Backlog, Mockup }

public enum DeliverableStatus { Draft, Validated }

// The current catalog, backlog or mockup of a project: the working copy the
// user edits and validates (one per kind and project). Saving replaces its
// content: what is replaced no longer exists for the rest of the flow, but a
// copy stays in its history (DeliverableVersion, step 101). The original
// output of the arbiter stays frozen in data/jobs/{JobId}/, only for the metrics.
public class Deliverable
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public DeliverableKind Kind { get; set; }

    // The job that generated it.
    public Guid JobId { get; set; }

    public DeliverableStatus Status { get; set; }

    // Something it was built on changed after it was generated (a warning:
    // nothing is deleted, and it can be generated again).
    public bool Outdated { get; set; }

    // The working copy as JSON (CatalogDraft, BacklogDraft).
    public required string Content { get; set; }

    // The iterations of the project whose documentation it includes (step
    // 111): those its job read (the mockup, those of its backlog). Null only
    // until the start-up gives it to those from before.
    public List<int>? Iterations { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ValidatedAt { get; set; }
}
