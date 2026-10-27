namespace Concilia.Api.Questions;

// Step 116: a round centred on some iterations that reads everything (option A
// of the design of milestone 22), to measure it against the reading of option
// C on the same project. The same prompts v0.13 and inputs as a centred round,
// except that A and B read entirely all the earlier documentation instead of
// only the fragments related to the new one (FocusedRoundInput, readAll).
//
// It is an experiment, like a new arbitration: its own kind of job, so the
// flow, the rounds and the checks of an update do not see it, and its
// questions are not added to the project. It leaves its result and its report.
public static class ReadAllRound
{
    public const string JobKind = "questions-read-all";
}
