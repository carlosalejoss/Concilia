namespace Concilia.Core.Projects;

public enum IterationStatus { Open, Closed }

// An iteration of the project (milestone 22, step 109): a batch of
// documentation ("Reunión del 15-11"). Documents are uploaded to the open one,
// and only one is open at a time; the phases that read the documentation wait
// until it is closed. Not to be confused with the "iteración v0" of the
// prompts.
public class Iteration
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    // 1, 2, 3… within the project.
    public int Number { get; set; }

    // "Iteración 1" by default, editable.
    public required string Name { get; set; }

    // Optional notes of the user.
    public string? Notes { get; set; }

    public IterationStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // When its documentation was closed; null while it is open (also after reopening it).
    public DateTimeOffset? ClosedAt { get; set; }
}
