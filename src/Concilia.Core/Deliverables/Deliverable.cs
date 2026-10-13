namespace Concilia.Core.Deliverables;

public enum DeliverableKind { Catalog, Backlog, Mockup }

public enum DeliverableStatus { Draft, Validated }

// The current catalog, backlog or mockup of a project: the working copy the
// user edits and validates (one per kind and project). Saving replaces its
// content: what is replaced no longer exists for the rest of the flow. The
// original output of the arbiter stays frozen in data/jobs/{JobId}/, only for
// the metrics.
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

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ValidatedAt { get; set; }
}
