using Concilia.Core.Runner;
using Concilia.Runner;

namespace Concilia.Api.Runs;

// "Runner" section of the configuration (in Docker: Runner__Provider, etc.).
public class RunnerOptions
{
    // "Fake" (default: no model, no secrets) or "LiteLlm" (the real gateway).
    public string Provider { get; set; } = "Fake";

    // Fake only: the pause between its events, so the web shows them arriving
    // one by one (0 in the test of the whole flow).
    public int FakeStepDelayMs { get; set; } = 300;

    // "claude" in Linux and Docker; "claude.cmd" when the API runs on Windows.
    public string ClaudeExecutable { get; set; } = "claude";

    // The two models that run every job in parallel, and the arbiter that joins
    // their outputs in the v0 phases (RUNNER_MODEL_ARBITER). Which model plays
    // each role is configuration.
    public string ModelA { get; set; } = "SS - GPT-6-SOL";
    public string ModelB { get; set; } = "SS - GPT-6-LUNA";
    public string ModelArbiter { get; set; } = "SS - GPT-6-SOL";

    // The model that suggests answers to the pending questions of a round
    // (RUNNER_MODEL_SUGGESTIONS). SOL by default: in the measurements it is the
    // faster model and the one that loses fewer runs.
    public string ModelSuggestions { get; set; } = "SS - GPT-6-SOL";

    // Input window of the models, confirmed by the team leader.
    public int MaxContextTokens { get; set; } = 128_000;

    // Fake only: the first attempt of each lot writes an invalid catalog, to
    // see the repair retry working ("Runner:SimulateRepair"; RUNNER_SIMULATE_REPAIR).
    public bool SimulateRepair { get; set; }

    // Output cap per response. The models accept up to 1M; Claude Code
    // assumes 32k unless told otherwise (see RunRequest.MaxOutputTokens).
    public int MaxOutputTokens { get; set; } = 64_000;

    // The v0 phases read the documentation by parts (09-10-2026): one turn
    // per range, so up to 30 turns per run (RUNNER_PHASE_MAX_TURNS).
    public int PhaseMaxTurns { get; set; } = 30;

    // Estimated tokens of all the documentation of a project above which a
    // phase is not launched (RUNNER_MAX_DOCUMENTATION_TOKENS). Reading by
    // parts has no hard limit, but past this size the model runs out of turns
    // or time, or compacts so often that it loses too much.
    public int MaxDocumentationTokens { get; set; } = 300_000;
}

// "Mockup" section of the configuration (in Docker: Mockup__Provider, etc.):
// the mockup is a role of its own, with one model and its own provider.
public class MockupOptions
{
    // "Fake" (default: no model, no secrets), "ClaudeSubscription" (the
    // author's subscription, CLAUDE_CODE_OAUTH_TOKEN) or "LiteLlm" (a gateway
    // model, if the application is ever used by more people). MOCKUP_PROVIDER.
    public string Provider { get; set; } = "Fake";

    // MOCKUP_MODEL. Claude Code knows this model, unlike the gateway ones.
    public string Model { get; set; } = "claude-sonnet-5-5";

    // Limits of EACH run of a mockup job (the design, each screen and the
    // integration; prompts v0.6). A whole file goes in one Write call, so it
    // needs room.
    public int MaxOutputTokens { get; set; } = 64_000;

    // Claude Code writes no line while the model writes a file: with 37
    // stories in a single page a real run passed the first limit of 15
    // minutes (09-10-2026). Raised to 30 without output and 45 in all.
    public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromMinutes(45);

    // The reads by parts, the writes, the edits of the integration and the verdict.
    public int MaxTurns { get; set; } = 40;

    // How many screens are built at the same time.
    public int MaxParallelScreens { get; set; } = 3;
}

public static class RunnerSetup
{
    // The key of the mockup provider in the services ([FromKeyedServices]).
    public const string MockupKey = "mockup";

    // Registers the IRunnerProvider of each role chosen in the configuration:
    // the default one (A, B, arbiter, suggestions) and the mockup one (keyed
    // "mockup"). With missing variables it throws here, so the API fails at
    // start-up with a clear message instead of failing later on the first run.
    public static IServiceCollection AddRunnerProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Runner").Get<RunnerOptions>() ?? new RunnerOptions();
        services.AddSingleton(options);
        var mockup = configuration.GetSection("Mockup").Get<MockupOptions>() ?? new MockupOptions();
        services.AddSingleton(mockup);

        services.AddSingleton(CreateProvider("Runner", options.Provider, options, configuration, allowSubscription: false));
        services.AddKeyedSingleton(MockupKey, CreateProvider("Mockup", mockup.Provider, options, configuration, allowSubscription: true));
        return services;
    }

    // The subscription is only allowed for the mockup: it is the author's own
    // account (decision of 08-10-2026), never for the analysis roles.
    private static IRunnerProvider CreateProvider(
        string section, string name, RunnerOptions options, IConfiguration configuration, bool allowSubscription) =>
        name.ToLowerInvariant() switch
        {
            // The small delay lets the web show the fake events arriving one by one.
            "fake" => new FakeProvider(stepDelay: TimeSpan.FromMilliseconds(options.FakeStepDelayMs), simulateRepair: options.SimulateRepair),
            "litellm" => new LiteLlmProvider(
                options.ClaudeExecutable,
                LiteLlmProvider.SettingsFrom(
                    configuration[LiteLlmProvider.BaseUrlVariable],
                    configuration[LiteLlmProvider.ApiKeyVariable])),
            "claudesubscription" when allowSubscription => new ClaudeSubscriptionProvider(
                options.ClaudeExecutable,
                ClaudeSubscriptionProvider.SettingsFrom(configuration[SubscriptionSettings.TokenVariable])),
            _ => throw new InvalidOperationException(
                $"{section}:Provider desconocido: \"{name}\". Los valores válidos son " +
                (allowSubscription ? "Fake, ClaudeSubscription y LiteLlm." : "Fake y LiteLlm.")),
        };
}
