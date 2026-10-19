using Concilia.Core.Runner;

namespace Concilia.Runner;

// One attempt of a task. PreviousRunId links a retry to the attempt it repeats.
public record RunAttempt(RunRequest Request, RunResult Result, Guid? PreviousRunId);

// What the caller decides after each attempt: the result to keep for it (the
// caller may turn a Done into a Failed, for example when the file the model
// wrote is not valid) and, when asking again is worth it, the complete prompt
// of the next attempt. A null RetryPrompt means no retry.
public record AttemptReview(RunResult Result, string? RetryPrompt);

// Runs a task and retries it while the review asks for it, at most maxRetries
// times (DefaultMaxRetries unless the caller says otherwise; the catalog task
// uses Catalog:MaxRepairs). Each retry is a new run with its own RunId and
// folder, linked to the previous one. By default (ReviewVerdict) only a
// missing or invalid verdict is retried: an API error, a timeout or a
// "bloqueada" verdict would not change by asking again. The catalog task adds
// its own review (CatalogReview).
//
// Apart from those retries, an attempt lost by inactivity (HeartbeatLost: no
// output at all during HeartbeatTimeout) or stopped because it compacted
// twice without writing its output (ClaudeCliRunner.CompactingBeforeOutput,
// step 92) is repeated ONCE per task with the same prompt. In the v1.3 test, three lots of one model died at the same
// moment with no output while other attempts of the same model finished: the
// gateway, not the prompt, was the cause. The repetition is a new linked run,
// but it is not a retry: it does not count for maxRetries and the review gets
// the same attempt number again (so the repair annex keeps its numbering).
public class RetryRunner(IRunnerProvider provider, int maxRetries = RetryRunner.DefaultMaxRetries)
{
    public const int DefaultMaxRetries = 2;

    public int MaxRetries => maxRetries;

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
        var heartbeatRepeated = false;

        while (true)
        {
            var result = await provider.RunAsync(request, eventsFor(request), cancellationToken);
            var repeats = heartbeatRepeated ? 1 : 0;
            var decision = review(new RunAttempt(request, result, previousRunId), attempts.Count + 1 - repeats);
            attempts.Add(new RunAttempt(request, decision.Result, previousRunId));

            if (IsLost(result) && !heartbeatRepeated)
            {
                heartbeatRepeated = true;
                previousRunId = request.RunId;
                request = NextAttempt(request, request.Prompt);
                continue;
            }

            var retries = attempts.Count - 1 - (heartbeatRepeated ? 1 : 0);
            if (decision.RetryPrompt is null || retries >= MaxRetries)
            {
                return attempts;
            }

            previousRunId = request.RunId;
            request = NextAttempt(request, decision.RetryPrompt);
        }
    }

    // An attempt that got nowhere for a reason the same prompt may not repeat.
    private static bool IsLost(RunResult result) =>
        result.Status == RunStatus.HeartbeatLost || result.ErrorMessage == ClaudeCliRunner.CompactingBeforeOutput;

    // Retry only a missing or invalid verdict, adding the reason to the prompt.
    public static AttemptReview ReviewVerdict(RunAttempt attempt) => new(
        attempt.Result,
        attempt.Result.VerdictError is { } error ? attempt.Request.Prompt + RetryNote(error) : null);

    // The retry gets a new id and a sibling folder (data/runs/{newId}/) with a
    // copy of the folders the request carries (input/, and output/mockup/ in
    // the mockup), and the prompt the review chose.
    private static RunRequest NextAttempt(RunRequest previous, string prompt)
    {
        var runId = Guid.NewGuid();
        var parent = Path.GetDirectoryName(Path.GetFullPath(previous.WorkDirectory))!;
        var workDirectory = Path.Combine(parent, runId.ToString());

        foreach (var folder in previous.CarriedFolders)
        {
            var source = Path.Combine(previous.WorkDirectory, folder);
            if (Directory.Exists(source))
            {
                CopyDirectory(source, Path.Combine(workDirectory, folder));
            }
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
