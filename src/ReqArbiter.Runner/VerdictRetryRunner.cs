using ReqArbiter.Core.Runner;

namespace ReqArbiter.Runner;

// One attempt of a task. PreviousRunId links a retry to the attempt it repeats.
public record RunAttempt(RunRequest Request, RunResult Result, Guid? PreviousRunId);

// Retries a run when its verdict is missing or invalid (at most MaxRetries
// times), adding the reason to the prompt. Each retry is a new run with its
// own RunId and folder, linked to the previous one. Nothing else is retried:
// an API error, a timeout or a "bloqueada" verdict would not change by asking again.
public class VerdictRetryRunner(IRunnerProvider provider)
{
    public const int MaxRetries = 2;

    // eventsFor gives the event callback of each attempt, so every run keeps
    // its own event stream.
    public async Task<IReadOnlyList<RunAttempt>> RunAsync(
        RunRequest first,
        Func<RunRequest, Action<RunEvent>> eventsFor,
        CancellationToken cancellationToken)
    {
        var attempts = new List<RunAttempt>();
        var request = first;
        Guid? previousRunId = null;

        while (true)
        {
            var result = await provider.RunAsync(request, eventsFor(request), cancellationToken);
            attempts.Add(new RunAttempt(request, result, previousRunId));

            var retries = attempts.Count - 1;
            if (result.VerdictError is null || retries >= MaxRetries)
            {
                return attempts;
            }

            previousRunId = request.RunId;
            request = NextAttempt(request, result.VerdictError);
        }
    }

    // The retry gets a new id and a sibling folder (data/runs/{newId}/) with a
    // copy of the previous input/ folder, and the prompt explains what failed.
    private static RunRequest NextAttempt(RunRequest previous, string verdictError)
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
            Prompt = previous.Prompt + RetryNote(verdictError),
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
