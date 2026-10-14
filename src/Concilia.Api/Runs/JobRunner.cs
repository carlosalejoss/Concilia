using Concilia.Core.Catalog;
using Concilia.Core.Mockup;
using Concilia.Core.Phases;
using Concilia.Core.Runner;
using Concilia.Core.Runs;
using Concilia.Runner;

namespace Concilia.Api.Runs;

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
public class JobRunner(RunExecutor executor, CatalogOptions options, PhaseExecutor? phaseExecutor = null, MockupExecutor? mockupExecutor = null)
{
    // A mockup job (prompts v0.6): the design (its first run, already
    // created), then one run per screen of the map it wrote (up to
    // MaxParallelScreens at once) and the integration, each with one repair.
    // Each step works on what the earlier ones left in the job's folder
    // (MockupTask.Keep). addRuns creates the runs of the screens and of the
    // integration once the map says how many screens there are; they are
    // lots of the slot MOCK, so the web shows them as "n of m". Returns the
    // outcome of the integration, whose .html is the mockup, or that of the
    // step that stopped the job.
    public async Task<RunOutcome> RunMockupAsync(
        Run design,
        MockupPlan plan,
        string jobFolder,
        Func<int, Task<IReadOnlyList<Run>>> addRuns,
        Func<Run, Task> onRunStarting,
        Func<RunOutcome, Task> onRunFinished,
        CancellationToken cancellationToken)
    {
        var mockups = mockupExecutor ?? throw new InvalidOperationException("JobRunner without MockupExecutor");

        async Task<RunOutcome> Step(Run run, MockupStep step)
        {
            var outcome = await RunOneAsync(run, () => mockups.ExecuteAsync(run, plan, step, cancellationToken), onRunStarting, onRunFinished, cancellationToken);
            if (outcome.Succeeded)
                MockupTask.Keep(step, outcome.Attempts![^1].Request.WorkDirectory, jobFolder);
            return outcome;
        }

        var designed = await Step(design, new MockupStep(MockupStage.Design));
        if (!designed.Succeeded)
            return designed;

        var map = MockupMap.Load(File.ReadAllText(Path.Combine(MockupTask.JobPrototypeFolder(jobFolder), "mapa.json")), plan.StoryIds);
        var runs = await addRuns(map.Screens.Count + 1); // the screens, in order, and the integration
        var integration = runs[^1];

        // The screens are independent (they share the shell, not each other):
        // up to MaxParallelScreens at once, like the lots of the catalog.
        using var places = new SemaphoreSlim(mockups.MaxParallelScreens);
        var screens = await Task.WhenAll(map.Screens.Select(async (screen, i) =>
        {
            await places.WaitAsync(cancellationToken);
            try
            {
                return await Step(runs[i], new MockupStep(MockupStage.Screen, screen));
            }
            finally
            {
                places.Release();
            }
        }));

        var failed = map.Screens.Where((_, i) => !screens[i].Succeeded).Select(s => s.Id).ToList();
        if (failed.Count > 0)
        {
            var reason = failed.Count == 1
                ? $"No se revisa el conjunto: la pantalla {failed[0]} no terminó con una salida válida."
                : $"No se revisa el conjunto: las pantallas {string.Join(", ", failed)} no terminaron con una salida válida.";
            var skipped = new RunOutcome(integration, null, null, PhaseExecutor.PromptVersion, reason);
            await onRunFinished(skipped);
            return skipped;
        }

        return await Step(integration, new MockupStep(MockupStage.Integration));
    }

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

    // A job of a single run (the suggestions of answers, slot SUG): its output
    // is the result, without cross-check.
    public async Task<PhaseJobResult> RunSingleAsync(
        Run run,
        PhasePlan plan,
        Func<Run, Task> onRunStarting,
        Func<RunOutcome, Task> onRunFinished,
        CancellationToken cancellationToken)
    {
        var phases = phaseExecutor ?? throw new InvalidOperationException("JobRunner without PhaseExecutor");
        var outcome = await RunOneAsync(run, () => phases.ExecuteAsync(run, plan, null, cancellationToken), onRunStarting, onRunFinished, cancellationToken);
        return outcome.Succeeded
            ? new PhaseJobResult(run.Slot, false, File.ReadAllText(PhaseExecutor.OutputPath(outcome.Attempts![^1], plan.Kind)), "Salida de un solo modelo.")
            : new PhaseJobResult(null, false, null, "El modelo no terminó con una salida válida; se puede pedir de nuevo.");
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
