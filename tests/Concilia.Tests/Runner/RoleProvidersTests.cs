using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concilia.Api.Runs;
using Concilia.Core.Runner;
using Concilia.Runner;
using Concilia.Runner.Execution;
using Concilia.Runner.StreamJson;

namespace Concilia.Tests.Runner;

// Step 128: one provider per role, with OpenCode Go for model B. No test calls
// any service: the keys and addresses below are fake and nothing is run
// against them (the routing tests use providers that only record the call).
public class RoleProvidersTests
{
    private const string GatewayUrl = "https://gateway.invalid:4000";
    private const string GatewayKey = "clave-falsa-de-la-pasarela";
    private const string OpenCodeKey = "oc_sk_clave-falsa-de-opencode";
    private const string ModelB = "kimi-k3.1-pro";

    private static RunnerProviders Providers(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddRunnerProvider(configuration).BuildServiceProvider();
        return services.GetRequiredService<RunnerProviders>();
    }

    // The configuration of milestone 25: everything on the gateway but model B.
    private static Dictionary<string, string?> GatewayWithOpenCodeB(bool withKey = true)
    {
        Dictionary<string, string?> settings = new()
        {
            ["Runner:Provider"] = "LiteLlm",
            ["Runner:ProviderB"] = "OpenCodeGo",
            ["Runner:ModelB"] = ModelB,
            ["LITELLM_BASE_URL"] = GatewayUrl,
            ["LITELLM_API_KEY"] = GatewayKey,
        };
        if (withKey) settings["OPENCODE_GO_API_KEY"] = OpenCodeKey;
        return settings;
    }

    [Fact]
    public void Without_configuring_the_roles_every_role_uses_the_general_provider()
    {
        var options = new RunnerOptions();
        var providers = Providers(new()
        {
            ["Runner:Provider"] = "LiteLlm",
            ["LITELLM_BASE_URL"] = GatewayUrl,
            ["LITELLM_API_KEY"] = GatewayKey,
        });

        Assert.All([options.ModelA, options.ModelB, options.ModelArbiter, options.ModelSuggestions],
            model => Assert.Equal("LiteLlm", providers.NameFor(model)));
        Assert.Null(providers.ErrorFor([options.ModelA, options.ModelB, options.ModelArbiter]));
    }

    [Fact]
    public void The_default_is_still_fake_for_every_role_and_needs_no_secrets()
    {
        var options = new RunnerOptions();
        var providers = Providers([]);

        Assert.Equal("Fake", providers.Name);
        Assert.All([options.ModelA, options.ModelB, options.ModelArbiter, options.ModelSuggestions],
            model => Assert.Equal("Fake", providers.NameFor(model)));
    }

    [Fact]
    public void Model_b_goes_through_opencode_and_the_rest_through_the_gateway()
    {
        var options = new RunnerOptions();
        var providers = Providers(GatewayWithOpenCodeB());

        Assert.Equal("OpenCodeGo", providers.NameFor(ModelB));
        Assert.Equal("LiteLlm", providers.NameFor(options.ModelA));
        Assert.Equal("LiteLlm", providers.NameFor(options.ModelArbiter));
        Assert.Equal("LiteLlm", providers.NameFor(options.ModelSuggestions));
        Assert.Null(providers.ErrorFor(ModelB));
    }

    [Fact]
    public void Without_the_opencode_key_the_api_starts_and_only_the_tasks_of_b_are_refused()
    {
        var options = new RunnerOptions();
        var providers = Providers(GatewayWithOpenCodeB(withKey: false));

        var error = providers.ErrorFor([options.ModelA, ModelB, options.ModelArbiter]);

        Assert.NotNull(error);
        Assert.Contains("OPENCODE_GO_API_KEY", error);
        Assert.Null(providers.ErrorFor(options.ModelSuggestions));
        Assert.Equal("OpenCodeGo", providers.NameFor(ModelB));
    }

    [Fact]
    public void The_same_model_with_two_providers_fails_at_startup()
    {
        var settings = GatewayWithOpenCodeB();
        settings["Runner:ModelB"] = new RunnerOptions().ModelA; // SOL on the gateway as A and on OpenCode as B

        var error = Assert.Throws<InvalidOperationException>(() => Providers(settings));

        Assert.Contains("dos papeles", error.Message);
        Assert.DoesNotContain(OpenCodeKey, error.Message);
    }

    [Fact]
    public void An_unknown_role_provider_names_its_setting()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Providers(new() { ["Runner:ProviderB"] = "OpenCode" }));

        Assert.Contains("Runner:ProviderB", error.Message);
        Assert.Contains("OpenCodeGo", error.Message);
    }

    [Fact]
    public async Task Each_run_goes_to_the_provider_of_its_model()
    {
        var gateway = new RecordingProvider("LiteLlm");
        var openCode = new RecordingProvider("OpenCodeGo");
        var providers = new RunnerProviders(gateway,
        [
            ("A", "SS - GPT-6-SOL", new RunnerProviders.Entry("LiteLlm", gateway, null)),
            ("B", ModelB, new RunnerProviders.Entry("OpenCodeGo", openCode, null)),
            ("Arbiter", "SS - GPT-6-SOL", new RunnerProviders.Entry("LiteLlm", gateway, null)),
        ]);

        await providers.RunAsync(Request(ModelB), _ => { }, CancellationToken.None);
        await providers.RunAsync(Request("SS - GPT-6-SOL"), _ => { }, CancellationToken.None);

        Assert.Equal([ModelB], openCode.Models);
        Assert.Equal(["SS - GPT-6-SOL"], gateway.Models);
    }

    [Fact]
    public async Task A_run_of_a_role_without_provider_fails_with_the_reason()
    {
        var gateway = new RecordingProvider("LiteLlm");
        var providers = new RunnerProviders(gateway,
            [("B", ModelB, new RunnerProviders.Entry("OpenCodeGo", null, "falta OPENCODE_GO_API_KEY"))]);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => providers.RunAsync(Request(ModelB), _ => { }, CancellationToken.None));

        Assert.Contains("OPENCODE_GO_API_KEY", error.Message);
        Assert.Empty(gateway.Models);
    }

    [Fact]
    public void Opencode_settings_use_the_public_address_by_default()
    {
        var settings = OpenCodeGoProvider.SettingsFrom(null, $" {OpenCodeKey} ");

        Assert.Equal(OpenCodeGoProvider.DefaultBaseUrl, settings.BaseUrl);
        Assert.Equal(OpenCodeKey, settings.AuthToken);
        Assert.Equal("https://otra.invalid/anthropic", OpenCodeGoProvider.SettingsFrom("https://otra.invalid/anthropic/v1/", OpenCodeKey).BaseUrl);
    }

    [Fact]
    public void Opencode_errors_never_show_the_values()
    {
        var invalid = Assert.Throws<InvalidOperationException>(() => OpenCodeGoProvider.SettingsFrom("no-es-una-url", OpenCodeKey));

        Assert.Contains("OPENCODE_GO_BASE_URL", invalid.Message);
        Assert.DoesNotContain("no-es-una-url", invalid.Message);
        Assert.DoesNotContain(OpenCodeKey, invalid.Message);
        Assert.Contains("OPENCODE_GO_API_KEY", OpenCodeGoProvider.MissingKeyError("  "));
        Assert.Null(OpenCodeGoProvider.MissingKeyError(OpenCodeKey));
    }

    // The API process has both keys side by side; each child gets only its own.
    private static IDictionary ParentWithBothKeys() => new Hashtable
    {
        ["PATH"] = "/usr/bin",
        ["LITELLM_BASE_URL"] = GatewayUrl,
        ["LITELLM_API_KEY"] = GatewayKey,
        ["OPENCODE_GO_API_KEY"] = OpenCodeKey,
        ["ANTHROPIC_API_KEY"] = "clave-personal-del-usuario",
    };

    [Fact]
    public void The_child_of_b_gets_the_opencode_key_and_never_the_gateway_one()
    {
        using var temp = new TempDirectory();
        var openCode = OpenCodeGoProvider.SettingsFrom(null, OpenCodeKey);

        var environment = ChildEnvironment.Build(openCode, RunFolders.Create(temp.Path), 128_000, 64_000, ParentWithBothKeys());

        Assert.Equal(OpenCodeGoProvider.DefaultBaseUrl, environment["ANTHROPIC_BASE_URL"]);
        Assert.Equal(OpenCodeKey, environment["ANTHROPIC_AUTH_TOKEN"]);
        Assert.DoesNotContain(environment.Values, v => v.Contains(GatewayKey) || v.Contains("gateway.invalid"));
        // Same conditions as SOL: the 128k window and the output cap.
        Assert.Equal("128000", environment["CLAUDE_CODE_MAX_CONTEXT_TOKENS"]);
        Assert.Equal("64000", environment["CLAUDE_CODE_MAX_OUTPUT_TOKENS"]);
        // Step 130: Claude Code waits up to 15 minutes for a silent response instead of 5.
        Assert.Equal("900000", environment["CLAUDE_STREAM_IDLE_TIMEOUT_MS"]);
        Assert.Equal("0", environment["API_FORCE_IDLE_TIMEOUT"]);
    }

    [Fact]
    public void A_gateway_child_never_gets_the_opencode_key()
    {
        using var temp = new TempDirectory();
        var gateway = LiteLlmProvider.SettingsFrom(GatewayUrl, GatewayKey);

        var environment = ChildEnvironment.Build(gateway, RunFolders.Create(temp.Path), 128_000, 64_000, ParentWithBothKeys());

        Assert.Equal(GatewayKey, environment["ANTHROPIC_AUTH_TOKEN"]);
        Assert.DoesNotContain(environment.Values, v => v.Contains(OpenCodeKey));
        Assert.False(environment.ContainsKey("OPENCODE_GO_API_KEY"));
        // The gateway children keep Claude Code's default waits (step 130).
        Assert.False(environment.ContainsKey("CLAUDE_STREAM_IDLE_TIMEOUT_MS"));
        Assert.False(environment.ContainsKey("API_FORCE_IDLE_TIMEOUT"));
    }

    [Fact]
    public void The_redactor_of_b_masks_the_opencode_key()
    {
        var openCode = OpenCodeGoProvider.SettingsFrom(null, OpenCodeKey);

        Assert.Equal("error 401 con *** en la cabecera", openCode.Redactor().Redact($"error 401 con {OpenCodeKey} en la cabecera"));
        Assert.DoesNotContain(OpenCodeKey, openCode.ToString());
    }

    [Fact]
    public void The_cache_tokens_are_kept_apart_from_the_total_input()
    {
        using var temp = new TempDirectory();
        const string line = """{"type":"result","subtype":"success","is_error":false,"terminal_reason":"completed","num_turns":3,"result":"VEREDICTO: {\"estado\":\"hecho\"}","usage":{"input_tokens":1000,"cache_creation_input_tokens":2000,"cache_read_input_tokens":30000,"output_tokens":500}}""";
        var result = StreamJsonParser.ParseLine(line).OfType<ResultEvent>().Single();

        var evaluated = RunEvaluator.Evaluate(new ProcessOutcome(ProcessEnd.Exited, 0, []), result, Request("SS - GPT-6-SOL", temp.Path), []);

        Assert.Equal(new TokenUsage(33_000, 500, 30_000, 2_000), evaluated.Usage);
    }

    private static RunRequest Request(string model, string? folder = null) => new()
    {
        RunId = Guid.NewGuid(),
        ModelId = model,
        Prompt = "prueba",
        WorkDirectory = folder ?? Path.GetTempPath(),
    };

    // Only records which models it was asked to run.
    private class RecordingProvider(string name) : IRunnerProvider
    {
        public List<string> Models { get; } = [];
        public string Name => name;

        public Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken)
        {
            Models.Add(request.ModelId);
            return Task.FromResult(new RunResult { Status = RunStatus.Done });
        }
    }
}
