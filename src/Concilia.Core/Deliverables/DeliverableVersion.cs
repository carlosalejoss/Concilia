namespace Concilia.Core.Deliverables;

// Why a version was recorded (step 101):
//   Existing   the deliverable was there before the history (recorded once, at start-up)
//   Generated  a job generated it (a new draft)
//   Edited     the user saved changes
//   Validated  the user validated it
//   Restored   the user went back to an earlier version (step 102)
public enum VersionOrigin { Existing, Generated, Edited, Validated, Restored }

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

    // Something it was built on changed after it was generated (step 102):
    // when a change of the flow outdates a deliverable, all its versions are
    // outdated too, since they are all from before it. Going back to a version
    // brings back its warning.
    public bool Outdated { get; set; }

    // Restored: the number of the version it went back to.
    public int? RestoredFrom { get; set; }

    public required string Content { get; set; }

    // The iterations whose documentation it includes, as the deliverable had them (step 111).
    public List<int>? Iterations { get; set; }

    // Its requirements, stories or screens (DeliverableHistory.Elements),
    // counted once so the list does not read every content.
    public int? Elements { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
