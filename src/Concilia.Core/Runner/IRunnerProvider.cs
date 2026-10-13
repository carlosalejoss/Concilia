namespace Concilia.Core.Runner;

// Runs one task with one model. Implementations: FakeProvider (fixed answer,
// for development and tests), LiteLlmProvider (claude -p against the
// company's LiteLLM gateway) and ClaudeSubscriptionProvider (claude -p with
// the author's subscription; only the mockup, decision of 08-10-2026).
public interface IRunnerProvider
{
    string Name { get; }

    // onEvent is called in order, on the running task, for each progress event.
    // Cancelling the token stops the run and throws OperationCanceledException.
    Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken);
}
