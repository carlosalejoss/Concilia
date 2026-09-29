using ReqArbiter.Core.Runs;
using ReqArbiter.Runner;

namespace ReqArbiter.Api.Runs;

// How one run of a job ended: its attempts, or the unexpected error that
// stopped it (a bug or an I/O problem, not a model failure: those come back
// as attempts with a failed status).
public record RunOutcome(Run Run, IReadOnlyList<RunAttempt>? Attempts, Exception? Error);

// Runs the runs of a job (A and B) at the same time. Each one is independent:
// if one throws, the other still finishes. onRunFinished is called as soon as
// each run ends, so its result can be stored and shown without waiting for
// the other one.
public class JobRunner(RunExecutor executor)
{
    public async Task RunAsync(
        IReadOnlyList<Run> runs,
        IReadOnlyList<RunInputFile> inputs,
        Func<RunOutcome, Task> onRunFinished,
        CancellationToken cancellationToken)
    {
        var running = runs.Select(async run =>
        {
            RunOutcome outcome;
            try
            {
                outcome = new RunOutcome(run, await executor.ExecuteAsync(run, inputs, cancellationToken), null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // The API is stopping: handled by the worker.
            }
            catch (Exception e)
            {
                outcome = new RunOutcome(run, null, e);
            }

            await onRunFinished(outcome);
        });

        // ToList() starts every run (Select is lazy until enumerated), so they
        // all run at the same time; Task.WhenAll then waits for all of them.
        await Task.WhenAll(running.ToList());
    }
}
