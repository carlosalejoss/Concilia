using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;
using ReqArbiter.Runner;

namespace ReqArbiter.Api.Runs;

// Executes the queued runs in the background, one at a time.
public class RunWorker(
    RunQueue queue,
    IServiceScopeFactory scopes,
    IRunnerProvider provider,
    RunEventStore events,
    FileStorage storage,
    RunnerOptions options,
    ILogger<RunWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await MarkInterruptedRunsAsync();

        await foreach (var runId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ExecuteRunAsync(runId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                await FailAsync(runId, "Cancelada: la API se estaba deteniendo.");
            }
            catch (Exception e)
            {
                // The exception goes to the API log; the user sees a generic message.
                logger.LogError(e, "Run {RunId} failed unexpectedly", runId);
                await FailAsync(runId, "Error interno al ejecutar. Consulta el log de la API.");
            }
            finally
            {
                events.Complete(runId);
            }
        }
    }

    private async Task ExecuteRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var run = await db.Runs.FindAsync([runId], cancellationToken);
        if (run is null)
        {
            return;
        }

        // Copy the extracted texts of the project into the run's input/ folder.
        var files = await db.ProjectFiles
            .Where(f => f.ProjectId == run.ProjectId && f.TextStoredName != null)
            .OrderBy(f => f.UploadedAt)
            .ToListAsync(cancellationToken);
        var workDirectory = storage.RunFolder(run.Id);
        var inputNames = RunInput.Prepare(
            Path.Combine(workDirectory, "input"),
            files.Select(f => new RunInputFile(f.FileName, Path.Combine(storage.ProjectFolder(f.ProjectId), f.TextStoredName!))));

        var request = new RunRequest
        {
            RunId = run.Id,
            ModelId = run.Model,
            Prompt = SummaryTask.Prompt(workDirectory, inputNames),
            WorkDirectory = workDirectory,
            ExpectedOutputFiles = [SummaryTask.OutputFile],
            MaxContextTokens = options.MaxContextTokens,
        };

        run.State = RunState.Running;
        run.StartedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        // Retries publish into the same event stream, marked, so the console
        // in the web shows the whole task.
        var attempts = await new VerdictRetryRunner(provider).RunAsync(
            request,
            attempt => runEvent => events.Publish(
                run.Id,
                attempt.RunId == run.Id ? runEvent : runEvent with { Text = $"[reintento] {runEvent.Text}" }),
            cancellationToken);

        // The first attempt updates this run; each retry becomes a new linked run.
        for (var i = 0; i < attempts.Count; i++)
        {
            var target = i == 0 ? run : new Run
            {
                Id = attempts[i].Request.RunId,
                ProjectId = run.ProjectId,
                RetryOfRunId = attempts[i].PreviousRunId,
                Attempt = i + 1,
                Provider = run.Provider,
                Model = run.Model,
                CreatedAt = DateTimeOffset.UtcNow,
                StartedAt = run.StartedAt,
            };
            Apply(target, attempts[i].Result);
            if (i > 0)
            {
                db.Runs.Add(target);
            }
        }

        await db.SaveChangesAsync(CancellationToken.None);
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
    }

    private async Task FailAsync(Guid runId, string message)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var run = await db.Runs.FindAsync(runId);
            if (run is not null && run.State is RunState.Queued or RunState.Running)
            {
                run.State = RunState.Failed;
                run.FinishedAt = DateTimeOffset.UtcNow;
                run.ErrorMessage = message;
                await db.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Could not mark run {RunId} as failed", runId);
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
