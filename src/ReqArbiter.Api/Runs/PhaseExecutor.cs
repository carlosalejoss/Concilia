using ReqArbiter.Api.Files;
using ReqArbiter.Core.Phases;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;
using ReqArbiter.Runner;

namespace ReqArbiter.Api.Runs;

// Executes one run of a v0 phase job (A, B or the arbiter), with its repair,
// without touching the database: builds the request (PhaseTask), runs it with
// RetryRunner (one repair) and reviews each attempt with PhaseTask.Review.
public class PhaseExecutor(IRunnerProvider provider, RunEventStore events, FileStorage storage, RunnerOptions runnerOptions, CatalogOptions catalogOptions)
{
    public const string PromptVersion = "v0";
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
        return await new RetryRunner(provider, MaxRepairs).RunAsync(
            built.Request,
            attempt => runEvent => events.Publish(
                run.Id,
                attempt.RunId == run.Id ? runEvent : runEvent with { Text = $"[reintento] {runEvent.Text}" }),
            (attempt, attemptsSoFar) => Review(phase, attempt, context, original, attemptsSoFar),
            cancellationToken);
    }

    // The path of the output of an attempt.
    public static string OutputPath(RunAttempt attempt, PhaseKind kind) =>
        Path.Combine(attempt.Request.WorkDirectory, PhaseDefinition.For(kind, PhaseRole.Proposal).OutputFile);

    // A valid output makes the attempt Done (a wrong verdict line is noted, not
    // repeated); an invalid one makes it Failed and asks for the repair. Runs
    // where the model did not get to answer (API error, timeout, blocked) are
    // not repaired: asking again would not change it.
    internal static AttemptReview Review(PhaseDefinition phase, RunAttempt attempt, PhaseContext context, string originalPrompt, int attemptsSoFar)
    {
        var result = attempt.Result;
        if (!CatalogReview.ModelAnswered(result))
            return new AttemptReview(result, null);

        var path = Path.Combine(attempt.Request.WorkDirectory, phase.OutputFile);
        var output = File.Exists(path) ? File.ReadAllText(path) : null;
        var review = PhaseTask.Review(phase, output, context, originalPrompt, attemptsSoFar);
        if (review.Valid)
        {
            var notes = new List<string>();
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
