namespace ReqArbiter.Core.Questions;

public enum QuestionStatus { Pending, Answered, Discarded }

// A question to the client, from a round of questions (a "questions" job).
// The answers are information of the project: the catalog and the backlog
// receive them.
public class Question
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    // The round (job) where it was asked.
    public Guid JobId { get; set; }

    // P-001, P-002… numbered by project, in the order the rounds give them.
    public required string Code { get; set; }

    public required string Text { get; set; }

    // Which requirement cannot be written without the answer.
    public required string Reason { get; set; }

    // The fragments the doubt comes from ("DOC-01#F007").
    public List<string> Sources { get; set; } = [];

    // Who asked it: "A", "B" or "ambas" (from the arbiter's `de`).
    public required string Origin { get; set; }

    // The arbiter's explanation, or why there was no cross-check.
    public string? ArbiterNote { get; set; }

    public QuestionStatus Status { get; set; }

    public string? Answer { get; set; }

    // Optional: why it was discarded (unnecessary, out of scope…).
    public string? DiscardReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
