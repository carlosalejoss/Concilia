namespace Concilia.Core.Questions;

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

    // Who wrote the answer: "usuario", "ia" (the suggestion accepted as it was)
    // or "ia-editada" (the suggestion edited by the user).
    public string? AnswerOrigin { get; set; }

    // The last answer suggested by the AI (not an answer until the user saves
    // it): its text, whether it comes from the documentation or is a guess
    // ("documentacion" or "suposicion"), the fragments it cites and why.
    public string? SuggestedAnswer { get; set; }
    public string? SuggestionBasis { get; set; }
    public List<string> SuggestionSources { get; set; } = [];
    public string? SuggestionNote { get; set; }
    public DateTimeOffset? SuggestedAt { get; set; }

    // Optional: why it was discarded (unnecessary, out of scope…).
    public string? DiscardReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
