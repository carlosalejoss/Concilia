namespace Concilia.Core.Deliverables;

// Why a version was recorded (step 101):
//   Existing   the deliverable was there before the history (recorded once, at start-up)
//   Generated  a job generated it (a new draft)
//   Edited     the user saved changes
//   Validated  the user validated it
public enum VersionOrigin { Existing, Generated, Edited, Validated }

// A copy of a deliverable as it was at one moment (milestone 21): its content,
// its state and the job it came from. The Deliverable stays the working copy
// that the rest of the platform reads; the versions only keep what it was. Of
// a mockup, Content is its summary, and the page is copied apart
// (FileStorage.MockupVersionFile).
public class DeliverableVersion
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public DeliverableKind Kind { get; set; }

    // 1, 2, 3... for each kind and project.
    public int Number { get; set; }

    public VersionOrigin Origin { get; set; }

    // The job that generated the content. The checks of a draft need it: they
    // use the plan of that job (the backlog's has the catalog it came from).
    public Guid JobId { get; set; }

    public DeliverableStatus Status { get; set; }

    public required string Content { get; set; }

    // Its requirements, stories or screens (DeliverableHistory.Elements),
    // counted once so the list does not read every content.
    public int? Elements { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
