using ReqArbiter.Core.Runner;
using ReqArbiter.Runner;

namespace ReqArbiter.Api.Runs;

// "Runner" section of the configuration (in Docker: Runner__Provider, etc.).
public class RunnerOptions
{
    // "Fake" (default: no model, no secrets) or "LiteLlm" (the real gateway).
    public string Provider { get; set; } = "Fake";

    // "claude" in Linux and Docker; "claude.cmd" when the API runs on Windows.
    public string ClaudeExecutable { get; set; } = "claude";

    // One configurable model is enough for milestone 3.
    public string Model { get; set; } = "SS - GPT-6-SOL";

    // Input window of the models, confirmed by the team leader.
    public int MaxContextTokens { get; set; } = 128_000;
}

public static class RunnerSetup
{
    // Registers the IRunnerProvider chosen in the configuration. With LiteLlm
    // and missing variables it throws here, so the API fails at start-up with
    // a clear message instead of failing later on the first run.
    public static IServiceCollection AddRunnerProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Runner").Get<RunnerOptions>() ?? new RunnerOptions();
        services.AddSingleton(options);

        IRunnerProvider provider = options.Provider.ToLowerInvariant() switch
        {
            // The small delay lets the web show the fake events arriving one by one.
            "fake" => new FakeProvider(stepDelay: TimeSpan.FromMilliseconds(300)),
            "litellm" => new LiteLlmProvider(
                options.ClaudeExecutable,
                LiteLlmProvider.SettingsFrom(
                    configuration[LiteLlmProvider.BaseUrlVariable],
                    configuration[LiteLlmProvider.ApiKeyVariable])),
            _ => throw new InvalidOperationException(
                $"Runner:Provider desconocido: \"{options.Provider}\". Los valores válidos son Fake y LiteLlm."),
        };

        services.AddSingleton(provider);
        return services;
    }
}
