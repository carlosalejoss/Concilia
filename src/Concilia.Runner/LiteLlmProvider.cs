using Concilia.Core.Runner;

namespace Concilia.Runner;

// The real provider: "claude -p" against the company's LiteLLM gateway.
// All the work is done by ClaudeCliRunner; this class only brings the gateway
// settings, read from LITELLM_BASE_URL and LITELLM_API_KEY.
public class LiteLlmProvider(string claudeExecutable, GatewaySettings gateway) : IRunnerProvider
{
    public const string BaseUrlVariable = "LITELLM_BASE_URL";
    public const string ApiKeyVariable = "LITELLM_API_KEY";

    private readonly ClaudeCliRunner _runner = new(claudeExecutable, gateway);

    public string Name => "LiteLlm";

    public Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken) =>
        _runner.RunAsync(request, onEvent, cancellationToken);

    // Validates the two variables. Error messages name the variables but never
    // show their values.
    public static GatewaySettings SettingsFrom(string? baseUrl, string? apiKey)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(baseUrl)) missing.Add(BaseUrlVariable);
        if (string.IsNullOrWhiteSpace(apiKey)) missing.Add(ApiKeyVariable);
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"El proveedor LiteLlm necesita las variables de entorno {string.Join(" y ", missing)} (en .env).");
        }

        var normalized = NormalizeBaseUrl(baseUrl!);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            throw new InvalidOperationException($"{BaseUrlVariable} no es una dirección http(s) válida.");
        }

        return new GatewaySettings(normalized, apiKey!.Trim());
    }

    // Claude Code adds "/v1/messages" itself, so ANTHROPIC_BASE_URL must be the
    // gateway address WITHOUT the final "/v1". Accept it with or without.
    public static string NormalizeBaseUrl(string baseUrl)
    {
        var url = baseUrl.Trim().TrimEnd('/');
        if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            url = url[..^"/v1".Length].TrimEnd('/');
        }
        return url;
    }
}
