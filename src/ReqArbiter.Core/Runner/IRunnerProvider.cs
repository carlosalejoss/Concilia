namespace ReqArbiter.Core.Runner;

// Runs one task with one model. Implementations: FakeProvider (fixed answer,
// for development and tests) and LiteLlmProvider (claude -p against the
// company's LiteLLM gateway). There is no personal-subscription provider.
public interface IRunnerProvider
{
    string Name { get; }

    // onEvent is called in order, on the running task, for each progress event.
    // Cancelling the token stops the run and throws OperationCanceledException.
    Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken);
}
