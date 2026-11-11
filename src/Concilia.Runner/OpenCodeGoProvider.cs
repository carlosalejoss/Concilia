using Concilia.Core.Runner;

namespace Concilia.Runner;

// "claude -p" against OpenCode Go, the subscription that serves model B since
// 10-11-2026 (milestone 25). OpenCode Go accepts Claude Code as a client, so
// the runner is the same: only the address and the key change. Like
// LiteLlmProvider, all the work is done by ClaudeCliRunner, and the access is a
// GatewaySettings, so the child receives this address and this key and nothing
// else (never the LiteLLM key), and the redactor masks both.
public class OpenCodeGoProvider(string claudeExecutable, GatewaySettings gateway) : IRunnerProvider
{
    public const string ApiKeyVariable = "OPENCODE_GO_API_KEY";

    // Optional: the public address of OpenCode Go for Claude Code is the default.
    public const string BaseUrlVariable = "OPENCODE_GO_BASE_URL";
    public const string DefaultBaseUrl = "https://opencode.ai/inference/go/anthropic";

    private readonly ClaudeCliRunner _runner = new(claudeExecutable, gateway);

    public string Name => "OpenCodeGo";

    public Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken) =>
        _runner.RunAsync(request, onEvent, cancellationToken);

    // The error when the key is missing. It names the variable, never a value.
    // Unlike the gateway, a missing key does not stop the API: only the tasks of
    // the roles that use OpenCode Go are refused, with this message (step 128).
    public static string? MissingKeyError(string? apiKey) =>
        string.IsNullOrWhiteSpace(apiKey)
            ? $"El proveedor OpenCodeGo necesita la variable de entorno {ApiKeyVariable} (en .env), con la clave de un espacio de trabajo con Go."
            : null;

    // Validates the settings: the key is required and the address, if given, must
    // be http(s). Claude Code adds "/v1/messages" itself, so a final "/v1" is removed.
    public static GatewaySettings SettingsFrom(string? baseUrl, string apiKey)
    {
        var url = LiteLlmProvider.NormalizeBaseUrl(string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            throw new InvalidOperationException($"{BaseUrlVariable} no es una dirección http(s) válida.");
        }
        return new GatewaySettings(url, apiKey.Trim());
    }
}
