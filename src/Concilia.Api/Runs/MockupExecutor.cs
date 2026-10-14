using Concilia.Api.Files;
using Concilia.Core.Mockup;
using Concilia.Core.Runner;
using Concilia.Core.Runs;
using Concilia.Runner;

namespace Concilia.Api.Runs;

// Executes the run of a mockup job (slot MOCK) with the mockup provider (keyed
// "mockup": the author's subscription, a gateway model or Fake), with one
// repair, without touching the database. Like PhaseExecutor, with the checks
// of MockupTask instead of those of a phase.
public class MockupExecutor(
    [FromKeyedServices(RunnerSetup.MockupKey)] IRunnerProvider provider,
    RunEventStore events,
    FileStorage storage,
    MockupOptions options)
{
    public const int MaxRepairs = 1;

    public async Task<IReadOnlyList<RunAttempt>> ExecuteAsync(Run run, MockupPlan plan, CancellationToken cancellationToken)
    {
        var request = MockupTask.BuildRequest(plan, run.Id, run.Model, storage.RunFolder(run.Id), new MockupRunOptions
        {
            MaxOutputTokens = options.MaxOutputTokens,
            HeartbeatTimeout = options.HeartbeatTimeout,
            TotalTimeout = options.TotalTimeout,
            MaxTurns = options.MaxTurns,
        }, run.JobId is { } jobId ? Deliverables.MockupEndpoints.StyleFolder(storage, jobId) : null);
        var original = request.Prompt;
        var reads = new List<FileRead>();
        return await new RetryRunner(provider, MaxRepairs).RunAsync(
            request,
            attempt => runEvent => events.Publish(
                run.Id,
                attempt.RunId == run.Id ? runEvent : runEvent with { Text = $"[reintento] {runEvent.Text}" }),
            (attempt, attemptsSoFar) =>
            {
                reads.AddRange(attempt.Result.Reads);
                // The model wrote the page by parts: the platform joins them first.
                var parts = MockupTask.Assemble(attempt.Request.WorkDirectory, plan);
                return PhaseExecutor.Review(attempt, MockupTask.OutputFile,
                    (html, unread) => MockupTask.Review(html, plan.StoryIds, original, attemptsSoFar, [.. parts, .. unread]),
                    MockupTask.RequiredReads, reads);
            },
            cancellationToken);
    }

    // The path of the .html of an attempt.
    public static string OutputPath(RunAttempt attempt) => Path.Combine(attempt.Request.WorkDirectory, MockupTask.OutputFile);
}
