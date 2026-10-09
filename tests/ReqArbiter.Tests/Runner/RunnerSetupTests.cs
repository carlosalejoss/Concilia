using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Runner;
using ReqArbiter.Runner;

namespace ReqArbiter.Tests.Runner;

// Provider selection and LiteLLM settings. No test calls the gateway: the
// values below are fake and nothing is run.
public class RunnerSetupTests
{
    private const string FakeUrl = "https://gateway.invalid:4000";
    private const string FakeKey = "fake-token-123456";

    private const string FakeOAuthToken = "fake-oauth-token-abcdef";

    private static IRunnerProvider Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddRunnerProvider(configuration).BuildServiceProvider();
        return services.GetRequiredService<IRunnerProvider>();
    }

    private static IRunnerProvider ResolveMockup(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddRunnerProvider(configuration).BuildServiceProvider();
        return services.GetRequiredKeyedService<IRunnerProvider>(RunnerSetup.MockupKey);
    }

    [Fact]
    public void The_mockup_is_fake_by_default_and_needs_no_token()
    {
        Assert.IsType<FakeProvider>(ResolveMockup([]));
    }

    [Fact]
    public void The_mockup_uses_the_subscription_when_configured_and_the_analysis_roles_do_not_change()
    {
        Dictionary<string, string?> settings = new()
        {
            ["Mockup:Provider"] = "ClaudeSubscription",
            ["CLAUDE_CODE_OAUTH_TOKEN"] = FakeOAuthToken,
        };

        Assert.Equal("ClaudeSubscription", ResolveMockup(settings).Name);
        Assert.IsType<FakeProvider>(Resolve(settings));
    }

    [Fact]
    public void The_subscription_without_token_fails_at_startup_naming_the_variable()
    {
        var error = Assert.Throws<InvalidOperationException>(() => ResolveMockup(new() { ["Mockup:Provider"] = "ClaudeSubscription" }));

        Assert.Contains("CLAUDE_CODE_OAUTH_TOKEN", error.Message);
    }

    [Fact]
    public void The_subscription_is_not_allowed_for_the_analysis_roles()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Resolve(new()
        {
            ["Runner:Provider"] = "ClaudeSubscription",
            ["CLAUDE_CODE_OAUTH_TOKEN"] = FakeOAuthToken,
        }));

        Assert.Contains("Fake y LiteLlm", error.Message);
        Assert.DoesNotContain(FakeOAuthToken, error.Message);
    }

    [Fact]
    public void Fake_is_the_default_and_needs_no_secrets()
    {
        Assert.IsType<FakeProvider>(Resolve([]));
    }

    [Fact]
    public void LiteLlm_with_both_variables_is_selected()
    {
        var provider = Resolve(new()
        {
            ["Runner:Provider"] = "LiteLlm",
            ["LITELLM_BASE_URL"] = FakeUrl + "/v1",
            ["LITELLM_API_KEY"] = FakeKey,
        });

        Assert.Equal("LiteLlm", provider.Name);
    }

    [Fact]
    public void LiteLlm_without_variables_fails_at_startup_naming_them()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Resolve(new() { ["Runner:Provider"] = "LiteLlm" }));

        Assert.Contains("LITELLM_BASE_URL", error.Message);
        Assert.Contains("LITELLM_API_KEY", error.Message);
    }

    [Fact]
    public void Startup_errors_never_show_the_values()
    {
        var missingKey = Assert.Throws<InvalidOperationException>(() => Resolve(new()
        {
            ["Runner:Provider"] = "LiteLlm",
            ["LITELLM_BASE_URL"] = FakeUrl,
        }));
        var invalidUrl = Assert.Throws<InvalidOperationException>(() => Resolve(new()
        {
            ["Runner:Provider"] = "LiteLlm",
            ["LITELLM_BASE_URL"] = "no-es-una-url",
            ["LITELLM_API_KEY"] = FakeKey,
        }));

        Assert.DoesNotContain("gateway.invalid", missingKey.Message);
        Assert.DoesNotContain(FakeKey, invalidUrl.Message);
        Assert.DoesNotContain("no-es-una-url", invalidUrl.Message);
    }

    [Fact]
    public void Unknown_provider_fails_at_startup()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Resolve(new() { ["Runner:Provider"] = "Suscripcion" }));

        Assert.Contains("Fake y LiteLlm", error.Message);
    }

    [Theory]
    [InlineData("https://gateway.invalid:4000", "https://gateway.invalid:4000")]
    [InlineData("https://gateway.invalid:4000/", "https://gateway.invalid:4000")]
    [InlineData("https://gateway.invalid:4000/v1", "https://gateway.invalid:4000")]
    [InlineData("https://gateway.invalid:4000/v1/", "https://gateway.invalid:4000")]
    [InlineData(" https://gateway.invalid/litellm/V1 ", "https://gateway.invalid/litellm")]
    public void Base_url_loses_the_final_v1(string input, string expected)
    {
        Assert.Equal(expected, LiteLlmProvider.NormalizeBaseUrl(input));
    }
}
