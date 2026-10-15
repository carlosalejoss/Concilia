using Concilia.Api.Files;
using Concilia.Core.Mockup;
using Concilia.Core.Phases;
using Concilia.Core.Runner;
using Concilia.Core.Runs;
using Concilia.Runner;

namespace Concilia.Api.Runs;

// Executes one run of a mockup job (the design, a screen or a fix;
// slot MOCK) with the mockup provider (keyed "mockup": the author's
// subscription, a gateway model or Fake), with one repair, without touching
// the database. Like PhaseExecutor, with the checks of MockupTask.
public class MockupExecutor(
    [FromKeyedServices(RunnerSetup.MockupKey)] IRunnerProvider provider,
    RunEventStore events,
    FileStorage storage,
    MockupOptions options)
{
    public const int MaxRepairs = 1;

    // How many screens are built at the same time.
    public int MaxParallelScreens => Math.Max(1, options.MaxParallelScreens);

    public async Task<IReadOnlyList<RunAttempt>> ExecuteAsync(Run run, MockupPlan plan, MockupStep step, CancellationToken cancellationToken)
    {
        var built = MockupTask.BuildRequest(plan, step, storage.JobFolder(run.JobId!.Value), run.Id, run.Model, storage.RunFolder(run.Id),
            new MockupRunOptions
            {
                MaxOutputTokens = options.MaxOutputTokens,
                HeartbeatTimeout = options.HeartbeatTimeout,
                TotalTimeout = options.TotalTimeout,
                MaxTurns = options.MaxTurns,
            });
        var original = built.Request.Prompt;
        var reads = new List<FileRead>();
        return await new RetryRunner(provider, MaxRepairs).RunAsync(
            built.Request,
            attempt => runEvent => events.Publish(
                run.Id,
                attempt.RunId == run.Id ? runEvent : runEvent with { Text = $"[reintento] {runEvent.Text}" }),
            (attempt, attemptsSoFar) =>
            {
                reads.AddRange(attempt.Result.Reads);
                return PhaseExecutor.Review(attempt, MockupTask.TargetOf(step),
                    (_, unread) => MockupTask.Review(step, plan, attempt.Request.WorkDirectory, original, attemptsSoFar, unread),
                    built.RequiredReads, reads);
            },
            cancellationToken);
    }
}
