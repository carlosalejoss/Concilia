using ReqArbiter.Core.Runner;

namespace ReqArbiter.Runner;

// "claude -p" with the author's Claude subscription instead of the gateway
// (the mockup, with Sonnet). Like LiteLlmProvider, all the work is done by
// ClaudeCliRunner; this class only brings the token.
public class ClaudeSubscriptionProvider(string claudeExecutable, SubscriptionSettings subscription) : IRunnerProvider
{
    private readonly ClaudeCliRunner _runner = new(claudeExecutable, subscription);

    public string Name => "ClaudeSubscription";

    public Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken) =>
        _runner.RunAsync(request, onEvent, cancellationToken);

    // Validates the token. The error names the variable but never shows a value.
    public static SubscriptionSettings SettingsFrom(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                $"El proveedor ClaudeSubscription necesita la variable de entorno {SubscriptionSettings.TokenVariable} " +
                "(en .env; se obtiene con \"claude setup-token\").");
        }
        return new SubscriptionSettings(token.Trim());
    }
}
