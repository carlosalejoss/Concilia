using Concilia.Api.Files;
using Concilia.Core.Phases;
using Concilia.Core.Runner;
using Concilia.Core.Runs;
using Concilia.Runner;

namespace Concilia.Api.Runs;

// Executes one run of a v0 phase job (A, B or the arbiter), with its repair,
// without touching the database: builds the request (PhaseTask), runs it with
// RetryRunner (one repair) and reviews each attempt with PhaseTask.Review.
public class PhaseExecutor(IRunnerProvider provider, RunEventStore events, FileStorage storage, RunnerOptions runnerOptions, CatalogOptions catalogOptions)
{
    // v0.1 (09-10-2026): the inputs are read by parts from input/ (milestone 12b).
    // v0.2 (09-10-2026): the reading plan gives the exact offset and limit of each Read call.
    // v0.3 (09-10-2026): the arbiter sees the ids of each proposal with its number ("1:P-003").
    // v0.4 (13-10-2026): the mockup is written by parts, a base and one file per epic.
    public const string PromptVersion = "v0.4";
    public const int MaxRepairs = 1;

    // proposals: the outputs of the two slots in the order of the plan (only for the arbiter).
    public async Task<IReadOnlyList<RunAttempt>> ExecuteAsync(
        Run run, PhasePlan plan, (string P1, string P2)? proposals, CancellationToken cancellationToken)
    {
        var phase = PhaseDefinition.For(plan.Kind, proposals is null ? PhaseRole.Proposal : PhaseRole.Arbiter);
        var input = proposals is var (p1, p2) ? plan.Input with { Proposal1 = p1, Proposal2 = p2 } : plan.Input;
        var options = new PhaseOptions
        {
            MaxContextTokens = runnerOptions.MaxContextTokens,
            MaxOutputTokens = runnerOptions.MaxOutputTokens,
            HeartbeatTimeout = catalogOptions.HeartbeatTimeout,
            TotalTimeout = catalogOptions.TotalTimeout,
            MaxTurns = runnerOptions.PhaseMaxTurns,
        };
        var built = PhaseTask.BuildRequest(phase, input, plan.Fragments, run.Id, run.Model, storage.RunFolder(run.Id), options,
            plan.Kind == PhaseKind.Suggestions ? plan.PendingQuestionIds : null);
        if (built.Request is null)
        {
            // The input does not fit: the run fails at once, without calling the model.
            var request = new RunRequest { RunId = run.Id, ModelId = run.Model, Prompt = "", WorkDirectory = storage.RunFolder(run.Id) };
            return [new RunAttempt(request, new RunResult { Status = RunStatus.Failed, ErrorMessage = built.Error }, null)];
        }

        var context = plan.Context(input.Proposal1.Length > 0 ? input.Proposal1 : null, input.Proposal2.Length > 0 ? input.Proposal2 : null);
        var original = built.Request.Prompt;
        var reads = new List<FileRead>(); // of every attempt: a repair only has to read what was missing
        return await new RetryRunner(provider, MaxRepairs).RunAsync(
            built.Request,
            attempt => runEvent => events.Publish(
                run.Id,
                attempt.RunId == run.Id ? runEvent : runEvent with { Text = $"[reintento] {runEvent.Text}" }),
            (attempt, attemptsSoFar) =>
            {
                reads.AddRange(attempt.Result.Reads);
                return Review(attempt, phase.OutputFile,
                    (output, unread) => PhaseTask.Review(phase, output, context, original, attemptsSoFar, unread),
                    built.RequiredReads!, reads,
                    phase.IsArbiter ? ProposalIds.Strip : null);
            },
            cancellationToken);
    }

    // The path of the output of an attempt.
    public static string OutputPath(RunAttempt attempt, PhaseKind kind) =>
        Path.Combine(attempt.Request.WorkDirectory, PhaseDefinition.For(kind, PhaseRole.Proposal).OutputFile);

    // A valid output makes the attempt Done (a wrong verdict line is noted, not
    // repeated); an invalid one makes it Failed and asks for the repair. Runs
    // where the model did not get to answer (API error, timeout, blocked) are
    // not repaired: asking again would not change it.
    // For any output file and its checks (the phases and the mockup).
    // reviewOutput gets the output (null if the model did not write it) and
    // the errors of the lines it left unread; requiredReads are the files it
    // must read entirely, and reads those it read in every attempt so far.
    // keepAs: how a valid output is kept in its file (the arbiter's, without
    // the proposal prefixes), so the rest of the flow reads it as before.
    internal static AttemptReview Review(
        RunAttempt attempt, string outputFile, Func<string?, IReadOnlyList<string>, PhaseReview> reviewOutput,
        IReadOnlyList<string> requiredReads, IReadOnlyList<FileRead> reads, Func<string, string>? keepAs = null)
    {
        var result = attempt.Result;
        if (!CatalogReview.ModelAnswered(result))
            return new AttemptReview(result, null);

        var path = Path.Combine(attempt.Request.WorkDirectory, outputFile);
        var output = File.Exists(path) ? File.ReadAllText(path) : null;
        var review = reviewOutput(output, PhaseTask.UnreadErrors(attempt.Request.WorkDirectory, requiredReads, reads));
        if (review.Valid)
        {
            if (keepAs is not null && output is not null)
                File.WriteAllText(path, keepAs(output));
            var notes = new List<string>();
            if (result.Compactions > 0)
                notes.Add($"Claude Code compactó el contexto {result.Compactions} {(result.Compactions == 1 ? "vez" : "veces")}: puede haberse perdido detalle");
            if (result.VerdictError is { } verdictError)
                notes.Add($"Veredicto no válido ({verdictError}); se acepta la salida");
            if (review.Validation.Warnings.Count > 0)
                notes.Add($"Avisos: {string.Join(" | ", review.Validation.Warnings)}");
            return new AttemptReview(result with
            {
                Status = RunStatus.Done,
                Verdict = result.Verdict ?? new Verdict(VerdictState.Done, null),
                ErrorMessage = notes.Count > 0 ? string.Join(". ", notes) + "." : null,
                VerdictError = null,
            }, null);
        }

        var errors = review.Validation.Errors;
        var more = errors.Count > 1 ? $" (y {errors.Count - 1} más)" : "";
        return new AttemptReview(result with { Status = RunStatus.Failed, ErrorMessage = $"Salida no válida: {errors[0]}{more}" }, review.RepairPrompt);
    }
}
