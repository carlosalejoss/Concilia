using Concilia.Api.Files;
using Concilia.Core.Projects;
using Concilia.Core.Runner;
using Concilia.Core.Runs;
using Concilia.Runner;

namespace Concilia.Api.Runs;

// What every run of a job shares: the project settings and the job's plan.
public record JobContext(Project Project, CatalogPlan Plan);

// Executes one run (one lot for one model), with its retries, without touching
// the database: prepares the run folder, builds the request, runs it, validates
// and repairs its catalog (CatalogReview) and publishes the progress events.
// The caller stores the result.
public class RunExecutor(
    IRunnerProvider provider,
    RunEventStore events,
    FileStorage storage,
    RunnerOptions runnerOptions,
    CatalogOptions catalogOptions)
{
    public async Task<IReadOnlyList<RunAttempt>> ExecuteAsync(Run run, JobContext context, CancellationToken cancellationToken)
    {
        var request = CatalogTask.BuildRequest(
            run, context.Project, context.Plan, storage.RunFolder(run.Id), catalogOptions, runnerOptions);

        // Retries publish into the same event stream, marked, so the console
        // in the web shows the whole task of this lot.
        return await new RetryRunner(provider, catalogOptions.MaxRepairs).RunAsync(
            request,
            attempt => runEvent => events.Publish(
                run.Id,
                attempt.RunId == run.Id ? runEvent : runEvent with { Text = $"[reintento] {runEvent.Text}" }),
            (attempt, attemptsSoFar) => CatalogReview.Review(attempt, attemptsSoFar, request.Prompt, context.Project.SystemName),
            cancellationToken);
    }
}
