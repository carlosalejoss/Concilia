using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Phases;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;
using ReqArbiter.Runner;

namespace ReqArbiter.Api.Runs;

// How one run of a job ended: its attempts, or the unexpected error that
// stopped it (a bug or an I/O problem, not a model failure: those come back
// as attempts with a failed status), or why it was not run at all (SkipReason:
// the arbiter when A or B did not finish well). PromptVersion is stored with
// each attempt.
public record RunOutcome(
    Run Run, IReadOnlyList<RunAttempt>? Attempts, Exception? Error,
    string PromptVersion = PromptBuilder.Version, string? SkipReason = null)
{
    // The run ended with a valid output (its last attempt is Done).
    public bool Succeeded => Attempts is { Count: > 0 } attempts && attempts[^1].Result.Status == RunStatus.Done;
}

// The result of a v0 phase job: the output that the next steps use, and where
// it comes from ("ARB", or "A"/"B" when only that model finished well: no
// cross-check). Source is null when the job failed (both models, or the arbiter).
public record PhaseJobResult(string? Source, bool CrossChecked, string? Output, string Message)
{
    public const string FileName = "resultado-arbitro.json";
}

// Runs the runs of a job: the two slots (A and B) at the same time, and within
// each slot up to CatalogOptions.MaxParallelLots lots at once (the lots are
// independent: each one only sees its own fragments). Each run is independent:
// if one throws, the others still run. onRunStarting is called just before each
// run starts, and onRunFinished as soon as it ends, so its state can be stored
// and shown without waiting for the rest.
//
// A v0 phase job (RunPhaseAsync) runs A and B at the same time and, when both
// end with a valid output, the arbiter with the two outputs in the random order
// of the plan. If only one ends well, the arbiter is not called and that output
// is the result, "sin verificación cruzada"; if neither does, the job fails
// (decisions of the v0 iteration, docs/plan-de-construccion.md).
public class JobRunner(RunExecutor executor, CatalogOptions options, PhaseExecutor? phaseExecutor = null)
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

    public async Task<PhaseJobResult> RunPhaseAsync(
        IReadOnlyList<Run> runs,
        PhasePlan plan,
        Func<Run, Task> onRunStarting,
        Func<RunOutcome, Task> onRunFinished,
        CancellationToken cancellationToken)
    {
        var phases = phaseExecutor ?? throw new InvalidOperationException("JobRunner without PhaseExecutor");
        var bySlot = runs.ToDictionary(r => r.Slot!);
        var arbiter = bySlot["ARB"];

        // A and B at the same time.
        var outcomes = await Task.WhenAll(new[] { "A", "B" }.Select(slot =>
            RunOneAsync(bySlot[slot], () => phases.ExecuteAsync(bySlot[slot], plan, null, cancellationToken), onRunStarting, onRunFinished, cancellationToken)));
        var succeeded = outcomes.Where(o => o.Succeeded).Select(o => o.Run.Slot!).ToList();
        string OutputOf(string slot) =>
            File.ReadAllText(PhaseExecutor.OutputPath(outcomes.Single(o => o.Run.Slot == slot).Attempts![^1], plan.Kind));

        if (succeeded.Count < 2)
        {
            var reason = succeeded.Count == 1
                ? $"No se llama al árbitro: solo {succeeded[0]} terminó con una salida válida. Su salida es el resultado, sin verificación cruzada."
                : "No se llama al árbitro: ni A ni B terminaron con una salida válida. El trabajo falla.";
            await onRunFinished(new RunOutcome(arbiter, null, null, PhaseExecutor.PromptVersion, reason));
            return succeeded.Count == 1
                ? new PhaseJobResult(succeeded[0], false, OutputOf(succeeded[0]), reason)
                : new PhaseJobResult(null, false, null, reason);
        }

        // The arbiter, with the two outputs as "Propuesta 1" and "Propuesta 2" in the order of the plan.
        var proposals = (OutputOf(plan.ProposalOrder[0]), OutputOf(plan.ProposalOrder[1]));
        var final = await RunOneAsync(arbiter, () => phases.ExecuteAsync(arbiter, plan, proposals, cancellationToken), onRunStarting, onRunFinished, cancellationToken);
        return final.Succeeded
            ? new PhaseJobResult("ARB", true, File.ReadAllText(PhaseExecutor.OutputPath(final.Attempts![^1], plan.Kind)), "Unión del árbitro.")
            : new PhaseJobResult(null, false, null, "El árbitro no terminó con una salida válida. El trabajo falla; se puede lanzar de nuevo.");
    }

    // One run of a phase job: like RunLotAsync, without lots.
    private static async Task<RunOutcome> RunOneAsync(
        Run run,
        Func<Task<IReadOnlyList<RunAttempt>>> execute,
        Func<Run, Task> onRunStarting,
        Func<RunOutcome, Task> onRunFinished,
        CancellationToken cancellationToken)
    {
        await onRunStarting(run);
        RunOutcome outcome;
        try
        {
            outcome = new RunOutcome(run, await execute(), null, PhaseExecutor.PromptVersion);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // The API is stopping: handled by the worker.
        }
        catch (Exception e)
        {
            outcome = new RunOutcome(run, null, e, PhaseExecutor.PromptVersion);
        }
        await onRunFinished(outcome);
        return outcome;
    }

    private async Task RunSlotAsync(
        IReadOnlyList<Run> lots,
        JobContext context,
        Func<Run, Task> onRunStarting,
        Func<RunOutcome, Task> onRunFinished,
        CancellationToken cancellationToken)
    {
        // The semaphore holds one place per lot that may run at the same time.
        // The loop takes a place before starting each lot, in lot order, so the
        // lots start in order; with MaxParallelLots = 1 they run one by one.
        using var places = new SemaphoreSlim(Math.Max(1, options.MaxParallelLots));
        var running = new List<Task>();

        foreach (var run in lots)
        {
            await places.WaitAsync(cancellationToken);
            running.Add(RunLotAsync(run, context, onRunStarting, onRunFinished, places, cancellationToken));
        }

        await Task.WhenAll(running);
    }

    private async Task RunLotAsync(
        Run run,
        JobContext context,
        Func<Run, Task> onRunStarting,
        Func<RunOutcome, Task> onRunFinished,
        SemaphoreSlim places,
        CancellationToken cancellationToken)
    {
        try
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
        finally
        {
            places.Release(); // The next lot of the slot can start.
        }
    }
}
