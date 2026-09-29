using ReqArbiter.Api.Files;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;
using ReqArbiter.Runner;

namespace ReqArbiter.Api.Runs;

// Executes one run of the summary task, with its verdict retries, without
// touching the database: prepares input/, builds the request, runs it and
// publishes the progress events. The caller stores the result.
public class RunExecutor(IRunnerProvider provider, RunEventStore events, FileStorage storage, RunnerOptions options)
{
    public Task<IReadOnlyList<RunAttempt>> ExecuteAsync(
        Run run,
        IReadOnlyList<RunInputFile> inputs,
        CancellationToken cancellationToken)
    {
        var workDirectory = storage.RunFolder(run.Id);
        var inputNames = RunInput.Prepare(Path.Combine(workDirectory, "input"), inputs);

        var request = new RunRequest
        {
            RunId = run.Id,
            ModelId = run.Model,
            Prompt = SummaryTask.Prompt(workDirectory, inputNames),
            WorkDirectory = workDirectory,
            ExpectedOutputFiles = [SummaryTask.OutputFile],
            MaxContextTokens = options.MaxContextTokens,
        };

        // Retries publish into the same event stream, marked, so the console
        // in the web shows the whole task of this slot.
        return new VerdictRetryRunner(provider).RunAsync(
            request,
            attempt => runEvent => events.Publish(
                run.Id,
                attempt.RunId == run.Id ? runEvent : runEvent with { Text = $"[reintento] {runEvent.Text}" }),
            cancellationToken);
    }
}
