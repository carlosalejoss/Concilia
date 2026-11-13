using Concilia.Core.Runner;
using Concilia.Runner;

namespace Concilia.Api.Runs;

// "Runner" section of the configuration (in Docker: Runner__Provider, etc.).
public class RunnerOptions
{
    // "Fake" (default: no model, no secrets) or "LiteLlm" (the real gateway).
    public string Provider { get; set; } = "Fake";

    // The provider of each role (step 128): empty means Provider, except for B
    // (see RunnerSetup.ProviderOfB). Besides Fake and LiteLlm, "OpenCodeGo"
    // (model B since 10-11-2026). RUNNER_PROVIDER_A, _B, _ARBITER and _SUGGESTIONS.
    public string? ProviderA { get; set; }
    public string? ProviderB { get; set; }
    public string? ProviderArbiter { get; set; }
    public string? ProviderSuggestions { get; set; }

    // Fake only: the pause between its events, so the web shows them arriving
    // one by one (0 in the test of the whole flow).
    public int FakeStepDelayMs { get; set; } = 300;

    // "claude" in Linux and Docker; "claude.cmd" when the API runs on Windows.
    public string ClaudeExecutable { get; set; } = "claude";

    // The two models that run every job in parallel, and the arbiter that joins
    // their outputs in the v0 phases (RUNNER_MODEL_ARBITER). Which model plays
    // each role is configuration. B is Kimi K3.1 Pro on OpenCode Go since the
    // author chose it (step 130, 12-11-2026); LUNA before (step 132 changed the default).
    public string ModelA { get; set; } = "SS - GPT-6-SOL";
    public string ModelB { get; set; } = "kimi-k3.1-pro";
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
    // RunnerProviders for the analysis roles (A, B, arbiter, suggestions), which
    // sends each run to the provider of its role, and the mockup one (keyed
    // "mockup"). With missing gateway variables it throws here, so the API fails
    // at start-up with a clear message instead of failing later on the first
    // run. A missing OpenCode Go key does not stop the API: only the tasks of
    // its roles are refused (RunnerProviders.ErrorFor).
    public static IServiceCollection AddRunnerProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Runner").Get<RunnerOptions>() ?? new RunnerOptions();
        services.AddSingleton(options);
        var mockup = configuration.GetSection("Mockup").Get<MockupOptions>() ?? new MockupOptions();
        services.AddSingleton(mockup);

        var providers = CreateRoleProviders(options, configuration);
        services.AddSingleton(providers);
        services.AddSingleton<IRunnerProvider>(providers);
        services.AddKeyedSingleton(MockupKey, CreateProvider("Mockup:Provider", mockup.Provider, options, configuration, allowSubscription: true));
        return services;
    }

    // One provider per role. Roles with the same provider name share one
    // instance (the default case: the four on Runner:Provider).
    private static RunnerProviders CreateRoleProviders(RunnerOptions options, IConfiguration configuration)
    {
        var created = new Dictionary<string, RunnerProviders.Entry>(StringComparer.OrdinalIgnoreCase);
        RunnerProviders.Entry EntryFor(string role, string? name)
        {
            var chosen = string.IsNullOrWhiteSpace(name) ? options.Provider : name.Trim();
            if (created.TryGetValue(chosen, out var existing)) return existing;
            var setting = string.IsNullOrWhiteSpace(name) ? "Runner:Provider" : $"Runner:Provider{role}";
            var entry = chosen.Equals("OpenCodeGo", StringComparison.OrdinalIgnoreCase)
                ? OpenCodeGoEntry(options, configuration)
                : new RunnerProviders.Entry("", CreateProvider(setting, chosen, options, configuration, allowSubscription: false), null);
            entry = entry with { ProviderName = entry.Provider?.Name ?? "OpenCodeGo" };
            created[chosen] = entry;
            return entry;
        }

        var roles = new List<(string Role, string Model, RunnerProviders.Entry Entry)>
        {
            ("A", options.ModelA, EntryFor("A", options.ProviderA)),
            ("B", options.ModelB, EntryFor("B", ProviderOfB(options))),
            ("Arbiter", options.ModelArbiter, EntryFor("Arbiter", options.ProviderArbiter)),
            ("Suggestions", options.ModelSuggestions, EntryFor("Suggestions", options.ProviderSuggestions)),
        };
        var defaultEntry = EntryFor("", options.Provider);
        // The default provider runs the models of no role (none in the flow). If it
        // cannot run (OpenCode Go without key), the first role provider that can,
        // and if none can, Fake: every role task is refused anyway (ErrorFor).
        var fallback = defaultEntry.Provider
            ?? roles.Select(r => r.Entry.Provider).FirstOrDefault(p => p is not null)
            ?? new FakeProvider();
        return new RunnerProviders(fallback, roles);
    }

    // The provider of model B (step 132, author, 13-11-2026). RUNNER_PROVIDER_B
    // if it is set. If not, OpenCode Go, where the default B (Kimi) runs, except
    // when the general provider is Fake: then B is Fake too, so the start without
    // secrets stays as it was. To run B on the gateway (LUNA, or SOL against SOL),
    // RUNNER_PROVIDER_B=LiteLlm.
    public static string ProviderOfB(RunnerOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ProviderB))
            return options.ProviderB.Trim();
        return options.Provider.Equals("Fake", StringComparison.OrdinalIgnoreCase) ? options.Provider : "OpenCodeGo";
    }

    // OpenCode Go (model B since 10-11-2026). Without its key the role has no
    // provider, only the reason, and its tasks are refused when launched.
    private static RunnerProviders.Entry OpenCodeGoEntry(RunnerOptions options, IConfiguration configuration)
    {
        var key = configuration[OpenCodeGoProvider.ApiKeyVariable];
        var error = OpenCodeGoProvider.MissingKeyError(key);
        if (error is not null) return new RunnerProviders.Entry("OpenCodeGo", null, error);
        var settings = OpenCodeGoProvider.SettingsFrom(configuration[OpenCodeGoProvider.BaseUrlVariable], key!);
        return new RunnerProviders.Entry("OpenCodeGo", new OpenCodeGoProvider(options.ClaudeExecutable, settings), null);
    }

    // The subscription is only allowed for the mockup: it is the author's own
    // account (decision of 08-10-2026), never for the analysis roles.
    // setting: the configuration key the name comes from, for the error message.
    private static IRunnerProvider CreateProvider(
        string setting, string name, RunnerOptions options, IConfiguration configuration, bool allowSubscription) =>
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
                $"{setting} desconocido: \"{name}\". Los valores válidos son " +
                (allowSubscription ? "Fake, ClaudeSubscription y LiteLlm." : "Fake, LiteLlm y OpenCodeGo.")),
        };
}
