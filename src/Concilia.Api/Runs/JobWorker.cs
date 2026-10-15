using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Core.Catalog;
using Concilia.Core.Mockup;
using Concilia.Core.Phases;
using Concilia.Core.Runner;
using Concilia.Core.Runs;

namespace Concilia.Api.Runs;

// Executes the queued jobs in the background, one job at a time. Within a job,
// JobRunner runs slots A and B in parallel (and, in a v0 phase, the arbiter
// after them; in the catalog v1.4, the lots of each slot).
public class JobWorker(
    JobQueue queue,
    IServiceScopeFactory scopes,
    JobRunner jobRunner,
    RunEventStore events,
    FileStorage storage,
    ILogger<JobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await MarkInterruptedRunsAsync();

        await foreach (var jobId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ExecuteJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                await FailUnfinishedRunsAsync(jobId, "Cancelada: la API se estaba deteniendo.");
            }
            catch (Exception e)
            {
                // The exception goes to the API log; the user sees a generic message.
                logger.LogError(e, "Job {JobId} failed unexpectedly", jobId);
                await FailUnfinishedRunsAsync(jobId, "Error interno al ejecutar. Consulta el log de la API.");
            }
        }
    }

    private async Task ExecuteJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var job = await db.Jobs.FindAsync([jobId], cancellationToken);
        if (job is null)
        {
            return;
        }

        var project = await db.Projects.FindAsync([job.ProjectId], cancellationToken)
            ?? throw new InvalidOperationException($"Project {job.ProjectId} of job {jobId} not found");

        var runs = await db.Runs
            .Where(r => r.JobId == jobId && r.State == RunState.Queued)
            .OrderBy(r => r.Slot).ThenBy(r => r.LotNumber)
            .ToListAsync(cancellationToken);

        // The mockup: the design, the screens and the fixes, and the .html
        // copied to the project.
        if (job.Kind == Deliverables.MockupEndpoints.JobKind)
        {
            var mockupPlan = MockupPlan.Load(storage.JobFolder(jobId));
            var design = runs.Single();
            var result = await jobRunner.RunMockupAsync(design, mockupPlan, storage.JobFolder(jobId),
                (count, fixes) => AddMockupRunsAsync(design, count, fixes), MarkRunningAsync, SaveOutcomeAsync, cancellationToken);
            await Deliverables.MockupEndpoints.StoreResultAsync(db, storage, job, mockupPlan, result);
            job.FinishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            return;
        }

        // A v0 phase: A ‖ B → arbiter, and the result of the job in its folder.
        if (PhasePlan.KindOfJob(job.Kind) is not null)
        {
            var phasePlan = PhasePlan.Load(storage.JobFolder(jobId));
            var result = phasePlan.Kind == PhaseKind.Suggestions
                ? await jobRunner.RunSingleAsync(runs.Single(), phasePlan, MarkRunningAsync, SaveOutcomeAsync, cancellationToken)
                : await jobRunner.RunPhaseAsync(runs, phasePlan, MarkRunningAsync, SaveOutcomeAsync, cancellationToken);
            PhaseJobs.WriteResult(storage.JobFolder(jobId), result);
            if (phasePlan.Kind == PhaseKind.Suggestions)
            {
                await Questions.QuestionEndpoints.StoreSuggestionsAsync(db, job.ProjectId, result);
            }
            else if (phasePlan.Kind == PhaseKind.Questions)
            {
                await Questions.QuestionEndpoints.StoreRoundAsync(db, jobId, job.ProjectId, phasePlan, result);
            }
            else
            {
                await Deliverables.DeliverableEndpoints.StoreResultAsync(db, jobId, job.ProjectId, phasePlan, result);
            }
            job.FinishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            return;
        }

        // Every run of the job reads the plan saved when the job was created.
        var plan = CatalogTask.LoadPlan(Path.Combine(storage.JobFolder(jobId), CatalogTask.PlanFile));

        await jobRunner.RunAsync(runs, new JobContext(project, plan), MarkRunningAsync, SaveOutcomeAsync, cancellationToken);

        job.FinishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);
    }

    // Runs of a mockup job, queued after the ones it has. The screens are
    // lots 2 to n + 1, after the design (lot 1), and all of them learn the
    // total n + 1, so the web shows "screen 3 of 19". The fixes go after them
    // and keep that total: a lot beyond it is a fix.
    private async Task<IReadOnlyList<Run>> AddMockupRunsAsync(Run design, int count, bool fixes)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.Runs.Where(r => r.JobId == design.JobId && r.Slot == design.Slot).ToListAsync();
        var last = existing.Max(r => r.LotNumber ?? 1);
        var total = fixes ? existing.Max(r => r.LotTotal ?? last) : last + count;
        if (!fixes)
        {
            foreach (var run in existing)
                run.LotTotal = total;
            design.LotTotal = total;
        }

        var runs = Enumerable.Range(last + 1, count).Select(lot => new Run
        {
            Id = Guid.NewGuid(),
            ProjectId = design.ProjectId,
            JobId = design.JobId,
            Slot = design.Slot,
            LotNumber = lot,
            LotTotal = total,
            Provider = design.Provider,
            Model = design.Model,
            State = RunState.Queued,
            CreatedAt = DateTimeOffset.UtcNow,
            PromptVersion = design.PromptVersion,
        }).ToList();
        db.Runs.AddRange(runs);
        await db.SaveChangesAsync();
        return runs;
    }

    // Called by JobRunner just before each run starts.
    private async Task MarkRunningAsync(Run started)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var run = await db.Runs.FindAsync(started.Id);
        if (run is not null)
        {
            run.State = RunState.Running;
            run.StartedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    // Called by JobRunner as soon as each run ends. It uses its own DbContext
    // (a DbContext must not be shared between runs finishing at the same time)
    // and closes the run's event stream only after saving, so the web reads
    // the final state when the stream ends.
    private async Task SaveOutcomeAsync(RunOutcome outcome)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var run = await db.Runs.FindAsync(outcome.Run.Id);
            if (run is null)
            {
                return;
            }

            if (outcome.SkipReason is not null)
            {
                run.State = RunState.Skipped;
                run.FinishedAt = DateTimeOffset.UtcNow;
                run.ErrorMessage = outcome.SkipReason;
            }
            else if (outcome.Attempts is null)
            {
                logger.LogError(outcome.Error, "Run {RunId} failed unexpectedly", run.Id);
                MarkFailed(run, "Error interno al ejecutar. Consulta el log de la API.");
            }
            else
            {
                // The first attempt updates this run; each retry becomes a new
                // linked run in the same job and slot.
                var attempts = outcome.Attempts;
                for (var i = 0; i < attempts.Count; i++)
                {
                    var target = i == 0 ? run : new Run
                    {
                        Id = attempts[i].Request.RunId,
                        ProjectId = run.ProjectId,
                        JobId = run.JobId,
                        Slot = run.Slot,
                        LotNumber = run.LotNumber,
                        LotTotal = run.LotTotal,
                        RetryOfRunId = attempts[i].PreviousRunId,
                        Attempt = i + 1,
                        Provider = run.Provider,
                        Model = run.Model,
                        CreatedAt = DateTimeOffset.UtcNow,
                        StartedAt = run.StartedAt,
                    };
                    Apply(target, attempts[i].Result);
                    target.PromptVersion = outcome.PromptVersion;
                    target.PromptSha256 = attempts[i].Request.Prompt.Length > 0 ? PromptBuilder.Sha256(attempts[i].Request.Prompt) : null;
                    if (i > 0)
                    {
                        db.Runs.Add(target);
                    }
                }
            }

            await db.SaveChangesAsync();
        }
        finally
        {
            events.Complete(outcome.Run.Id);
        }
    }

    private static void Apply(Run run, RunResult result)
    {
        run.State = result.Status switch
        {
            RunStatus.Done => RunState.Done,
            RunStatus.Blocked => RunState.Blocked,
            RunStatus.TimedOut => RunState.TimedOut,
            RunStatus.HeartbeatLost => RunState.HeartbeatLost,
            _ => RunState.Failed,
        };
        run.FinishedAt = DateTimeOffset.UtcNow;
        run.VerdictState = result.Verdict?.State switch
        {
            VerdictState.Done => "hecho",
            VerdictState.Blocked => "bloqueada",
            _ => null,
        };
        run.VerdictReason = result.Verdict?.Reason;
        run.ResultText = result.ResultText;
        run.InputTokens = result.Usage?.InputTokens;
        run.OutputTokens = result.Usage?.OutputTokens;
        run.NumTurns = result.NumTurns;
        run.ErrorMessage = result.ErrorMessage;
        run.ClaudeCodeVersion = result.ClaudeCodeVersion;
    }

    private static void MarkFailed(Run run, string message)
    {
        run.State = RunState.Failed;
        run.FinishedAt = DateTimeOffset.UtcNow;
        run.ErrorMessage = message;
    }

    // After an unexpected error or a shutdown: fail the runs of the job that
    // did not finish and close their event streams.
    private async Task FailUnfinishedRunsAsync(Guid jobId, string message)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var unfinished = await db.Runs
                .Where(r => r.JobId == jobId && (r.State == RunState.Queued || r.State == RunState.Running))
                .ToListAsync();
            foreach (var run in unfinished)
            {
                MarkFailed(run, message);
                events.Complete(run.Id);
            }
            await db.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Could not mark the runs of job {JobId} as failed", jobId);
        }
    }

    // Runs that were queued or running when the API stopped will never finish.
    private async Task MarkInterruptedRunsAsync()
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Runs
                .Where(r => r.State == RunState.Queued || r.State == RunState.Running)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(r => r.State, RunState.Failed)
                    .SetProperty(r => r.FinishedAt, DateTimeOffset.UtcNow)
                    .SetProperty(r => r.ErrorMessage, "Interrumpida: la API se reinició."));
        }
        catch (Exception e)
        {
            // Without a database (e.g. in the /health test) there is nothing to mark.
            logger.LogWarning(e, "Could not check for interrupted runs");
        }
    }
}
