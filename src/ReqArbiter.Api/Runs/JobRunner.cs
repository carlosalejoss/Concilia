using ReqArbiter.Core.Runs;
using ReqArbiter.Runner;

namespace ReqArbiter.Api.Runs;

// How one run of a job ended: its attempts, or the unexpected error that
// stopped it (a bug or an I/O problem, not a model failure: those come back
// as attempts with a failed status).
public record RunOutcome(Run Run, IReadOnlyList<RunAttempt>? Attempts, Exception? Error);

// Runs the runs of a job: the two slots (A and B) at the same time, and within
// each slot its lots one after another. Each run is independent: if one throws,
// the others still run. onRunStarting is called just before each run starts,
// and onRunFinished as soon as it ends, so its state can be stored and shown
// without waiting for the rest.
public class JobRunner(RunExecutor executor)
{
    public async Task RunAsync(
        IReadOnlyList<Run> runs,
        JobContext context,
        Func<Run, Task> onRunStarting,
        Func<RunOutcome, Task> onRunFinished,
        CancellationToken cancellationToken)
    {
        var slots = runs
            .GroupBy(r => r.Slot)
            .Select(slot => RunSlotAsync(slot.OrderBy(r => r.LotNumber).ToList(), context, onRunStarting, onRunFinished, cancellationToken));

        // ToList() starts every slot (Select is lazy until enumerated), so they
        // run at the same time; Task.WhenAll then waits for all of them.
        await Task.WhenAll(slots.ToList());
    }

    private async Task RunSlotAsync(
        IReadOnlyList<Run> lots,
        JobContext context,
        Func<Run, Task> onRunStarting,
        Func<RunOutcome, Task> onRunFinished,
        CancellationToken cancellationToken)
    {
        foreach (var run in lots)
        {
            await onRunStarting(run);
            RunOutcome outcome;
            try
            {
                outcome = new RunOutcome(run, await executor.ExecuteAsync(run, context, cancellationToken), null);
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
        }
    }
}
