using Concilia.Core.Catalog;
using Concilia.Core.Matching;
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
// of the plan. If only one ends well, or neither, the arbiter is not called
// and the job fails: the output of a single model has no cross-check and is
// not the result (decision of 14-10-2026; until then it was, "sin
// verificación cruzada").
public class JobRunner(RunExecutor executor, CatalogOptions options, PhaseExecutor? phaseExecutor = null, MockupExecutor? mockupExecutor = null)
{
    // A mockup job (prompts v0.8): the design (its first run, already
    // created), one run per screen of the map it wrote (up to
    // MaxParallelScreens at once), and then the platform joins and checks the
    // prototype (MockupTask.Finish). If some parts have problems between them,
    // one run fixes each of those parts, and the platform checks again. Each
    // step works on what the earlier ones left in the job's folder
    // (MockupTask.Keep); a step already there (a job that resumes a failed
    // one) is not run again. addRuns creates runs once the map says how many
    // screens there are (fixes: false) and for the fixes (fixes: true); they
    // are lots of the slot MOCK, so the web shows them as steps.
    public async Task<MockupJobResult> RunMockupAsync(
        Run design,
        MockupPlan plan,
        string jobFolder,
        Func<int, bool, Task<IReadOnlyList<Run>>> addRuns,
        Func<Run, Task> onRunStarting,
        Func<RunOutcome, Task> onRunFinished,
        CancellationToken cancellationToken)
    {
        var mockups = mockupExecutor ?? throw new InvalidOperationException("JobRunner without MockupExecutor");

        // A step: reused if the job's folder already has it, run otherwise.
        async Task<bool> Step(Run run, MockupStep step)
        {
            if (MockupTask.IsDone(step, jobFolder))
            {
                await onRunFinished(new RunOutcome(run, null, null, PhaseExecutor.PromptVersion,
                    "Reutilizado: este paso ya terminó bien en el trabajo que se reanuda."));
                return true;
            }
            var outcome = await RunOneAsync(run, () => mockups.ExecuteAsync(run, plan, step, cancellationToken), onRunStarting, onRunFinished, cancellationToken);
            if (outcome.Succeeded)
                MockupTask.Keep(step, outcome.Attempts![^1].Request.WorkDirectory, jobFolder);
            return outcome.Succeeded;
        }

        // Several steps, up to MaxParallelScreens at once, like the lots of the
        // catalog: the screens share the shell, not each other.
        async Task<IReadOnlyList<string>> Failed(IReadOnlyList<(Run Run, MockupStep Step, string Name)> steps)
        {
            using var places = new SemaphoreSlim(mockups.MaxParallelScreens);
            var done = await Task.WhenAll(steps.Select(async s =>
            {
                await places.WaitAsync(cancellationToken);
                try
                {
                    return await Step(s.Run, s.Step);
                }
                finally
                {
                    places.Release();
                }
            }));
            return steps.Where((_, i) => !done[i]).Select(s => s.Name).ToList();
        }

        if (!await Step(design, new MockupStep(MockupStage.Design)))
            return new MockupJobResult(false, null, 0, "El diseño no terminó con una salida válida.");

        var map = MockupMap.Load(File.ReadAllText(Path.Combine(MockupTask.JobPrototypeFolder(jobFolder), "mapa.json")), plan.StoryIds);
        var screenRuns = await addRuns(map.Screens.Count, false);
        var failed = await Failed(map.Screens.Select((screen, i) => (screenRuns[i], new MockupStep(MockupStage.Screen, screen), screen.Id)).ToList());
        if (failed.Count > 0)
            return new MockupJobResult(false, null, map.Screens.Count, $"No terminaron con una salida válida: {string.Join(", ", failed)}.");

        var finish = MockupTask.Finish(jobFolder, plan, map);
        if (finish.Errors.Count == 0 && finish.Problems.Count > 0)
        {
            // One fix per part with problems, the shell first.
            var targets = finish.Problems.GroupBy(p => p.Target).ToList();
            var fixRuns = await addRuns(targets.Count, true);
            var fixes = targets.Select((group, i) =>
            {
                var screen = map.Screens.FirstOrDefault(s => s.Id == group.Key);
                return (fixRuns[i], new MockupStep(MockupStage.Fix, screen, group.Select(p => p.Message).ToList()), group.Key);
            }).ToList();
            failed = await Failed(fixes);
            if (failed.Count > 0)
                return new MockupJobResult(false, null, map.Screens.Count, $"Las correcciones de {string.Join(", ", failed)} no terminaron con una salida válida.");
            finish = MockupTask.Finish(jobFolder, plan, map);
        }

        if (!finish.Valid)
        {
            var errors = finish.Errors.Concat(finish.Problems.Select(p => p.Message)).ToList();
            var more = errors.Count > 3 ? $" (y {errors.Count - 3} más)" : "";
            return new MockupJobResult(false, null, map.Screens.Count, $"El prototipo juntado no es válido: {string.Join(" | ", errors.Take(3))}{more}.");
        }
        return new MockupJobResult(true, finish.HtmlPath, map.Screens.Count, $"Mockup generado: {map.Screens.Count} pantallas.");
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

        // Without both outputs there is no cross-check, and the output of a
        // single model is not accepted as the result (decision of the author,
        // 14-10-2026): the job fails and can be launched again.
        if (succeeded.Count < 2)
        {
            var reason = succeeded.Count == 1
                ? $"No se llama al árbitro: solo {succeeded[0]} terminó con una salida válida, y sin las dos propuestas no hay verificación cruzada. El trabajo falla; se puede lanzar de nuevo."
                : "No se llama al árbitro: ni A ni B terminaron con una salida válida. El trabajo falla; se puede lanzar de nuevo.";
            await onRunFinished(new RunOutcome(arbiter, null, null, PhaseExecutor.PromptVersion, reason));
            return new PhaseJobResult(null, false, null, reason);
        }

        return await RunArbiterAsync(arbiter, plan, OutputOf("A"), OutputOf("B"), onRunStarting, onRunFinished, cancellationToken);
    }

    // The arbiter alone, with the outputs of A and B as "Propuesta 1" and
    // "Propuesta 2" in the order of the plan. Also used to arbitrate again the
    // proposals of a finished job (Rearbitration).
    // Step 88: first the platform matches A and B by meaning and gives the
    // matching to the arbiter as a guide; afterwards it looks for where the
    // arbiter went against it (ArbiterDeviations), kept with the result.
    public async Task<PhaseJobResult> RunArbiterAsync(
        Run arbiter,
        PhasePlan plan,
        string outputA,
        string outputB,
        Func<Run, Task> onRunStarting,
        Func<RunOutcome, Task> onRunFinished,
        CancellationToken cancellationToken)
    {
        var phases = phaseExecutor ?? throw new InvalidOperationException("JobRunner without PhaseExecutor");
        string Of(string slot) => slot == "A" ? outputA : outputB;
        var (matching, before) = ComputeTime.Measure(() => phases.Matching.Compute(plan.Kind, outputA, outputB));
        var input = (Of(plan.ProposalOrder[0]), Of(plan.ProposalOrder[1]), MatchingFile.Write(matching, plan.ProposalOrder));
        var final = await RunOneAsync(arbiter, () => phases.ExecuteAsync(arbiter, plan, input, cancellationToken), onRunStarting, onRunFinished, cancellationToken);
        if (!final.Succeeded)
            return new PhaseJobResult(null, false, null, "El árbitro no terminó con una salida válida. El trabajo falla; se puede lanzar de nuevo.");
        var output = File.ReadAllText(PhaseExecutor.OutputPath(final.Attempts![^1], plan.Kind));
        var (deviations, after) = ComputeTime.Measure(() => ArbiterDeviations.Find(plan.Kind, output, matching, plan.ProposalOrder));
        return new PhaseJobResult("ARB", true, output, "Unión del árbitro.")
        {
            Deviations = deviations,
            MatchingTime = new ComputeTime(before.Seconds + after.Seconds, before.CpuSeconds + after.CpuSeconds),
        };
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
