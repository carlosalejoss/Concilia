using ReqArbiter.Core.Runner;

namespace ReqArbiter.Runner;

// One attempt of a task. PreviousRunId links a retry to the attempt it repeats.
public record RunAttempt(RunRequest Request, RunResult Result, Guid? PreviousRunId);

// What the caller decides after each attempt: the result to keep for it (the
// caller may turn a Done into a Failed, for example when the file the model
// wrote is not valid) and, when asking again is worth it, the complete prompt
// of the next attempt. A null RetryPrompt means no retry.
public record AttemptReview(RunResult Result, string? RetryPrompt);

// Runs a task and retries it while the review asks for it, at most MaxRetries
// times. Each retry is a new run with its own RunId and folder, linked to the
// previous one. By default (ReviewVerdict) only a missing or invalid verdict
// is retried: an API error, a timeout or a "bloqueada" verdict would not
// change by asking again. The catalog task adds its own review (step 27).
public class RetryRunner(IRunnerProvider provider)
{
    public const int MaxRetries = 2;

    // eventsFor gives the event callback of each attempt, so every run keeps
    // its own event stream.
    public Task<IReadOnlyList<RunAttempt>> RunAsync(
        RunRequest first,
        Func<RunRequest, Action<RunEvent>> eventsFor,
        CancellationToken cancellationToken) =>
        RunAsync(first, eventsFor, (attempt, _) => ReviewVerdict(attempt), cancellationToken);

    // review receives each attempt and how many attempts have run so far (1
    // after the first one), which is also the number of the retry it may ask for.
    public async Task<IReadOnlyList<RunAttempt>> RunAsync(
        RunRequest first,
        Func<RunRequest, Action<RunEvent>> eventsFor,
        Func<RunAttempt, int, AttemptReview> review,
        CancellationToken cancellationToken)
    {
        var attempts = new List<RunAttempt>();
        var request = first;
        Guid? previousRunId = null;

        while (true)
        {
            var result = await provider.RunAsync(request, eventsFor(request), cancellationToken);
            var decision = review(new RunAttempt(request, result, previousRunId), attempts.Count + 1);
            attempts.Add(new RunAttempt(request, decision.Result, previousRunId));

            var retries = attempts.Count - 1;
            if (decision.RetryPrompt is null || retries >= MaxRetries)
            {
                return attempts;
            }

            previousRunId = request.RunId;
            request = NextAttempt(request, decision.RetryPrompt);
        }
    }

    // Retry only a missing or invalid verdict, adding the reason to the prompt.
    public static AttemptReview ReviewVerdict(RunAttempt attempt) => new(
        attempt.Result,
        attempt.Result.VerdictError is { } error ? attempt.Request.Prompt + RetryNote(error) : null);

    // The retry gets a new id and a sibling folder (data/runs/{newId}/) with a
    // copy of the previous input/ folder, and the prompt the review chose.
    private static RunRequest NextAttempt(RunRequest previous, string prompt)
    {
        var runId = Guid.NewGuid();
        var parent = Path.GetDirectoryName(Path.GetFullPath(previous.WorkDirectory))!;
        var workDirectory = Path.Combine(parent, runId.ToString());

        var previousInput = Path.Combine(previous.WorkDirectory, "input");
        if (Directory.Exists(previousInput))
        {
            CopyDirectory(previousInput, Path.Combine(workDirectory, "input"));
        }

        return previous with
        {
            RunId = runId,
            WorkDirectory = workDirectory,
            Prompt = prompt,
        };
    }

    // With $$""" the JSON braces are written as they are and {{...}} interpolates.
    public static string RetryNote(string verdictError) => "\n\n" + $$"""
        ---
        El intento anterior no terminó con un veredicto válido: {{verdictError}}
        Termina tu respuesta con una última línea exactamente en uno de estos formatos:
        VEREDICTO: {"estado":"hecho"}
        VEREDICTO: {"estado":"bloqueada","motivo":"<explica el motivo>"}
        """;

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }
        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}
