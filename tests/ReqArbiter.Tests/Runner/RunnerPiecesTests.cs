using System.Collections;
using ReqArbiter.Core.Runner;
using ReqArbiter.Runner;
using ReqArbiter.Runner.Execution;
using ReqArbiter.Runner.Security;

namespace ReqArbiter.Tests.Runner;

// The small pure pieces of step 18: arguments, environment, folders,
// secret redaction and input budget.
public class RunnerPiecesTests
{
    private static readonly GatewaySettings Gateway = FakeClaudeLauncher.FakeGateway;

    [Fact]
    public void Arguments_follow_the_specification()
    {
        var request = new RunRequest
        {
            RunId = Guid.NewGuid(),
            ModelId = "SS - GPT-6-LUNA",
            Prompt = "no va en los argumentos",
            WorkDirectory = "/work",
        };

        Assert.Equal(
            [
                "-p", "--model", "SS - GPT-6-LUNA", "--output-format", "stream-json", "--verbose",
                "--tools", "Read,Write", "--allowedTools", "Read,Write", "--disable-slash-commands",
                "--max-turns", "10",
            ],
            ClaudeArguments.Build(request));
    }

    [Fact]
    public void Only_api_retry_system_lines_are_retries()
    {
        const string denied = """{"type":"system","subtype":"permission_denied","tool_name":"Skill"}""";
        const string retry = """{"type":"system","subtype":"api_retry","attempt":1}""";

        var deniedEvent = EventSummaries.From(ReqArbiter.Runner.StreamJson.StreamJsonParser.ParseLine(denied).Single());
        var retryEvent = EventSummaries.From(ReqArbiter.Runner.StreamJson.StreamJsonParser.ParseLine(retry).Single());

        Assert.Equal(RunEventKind.Warning, deniedEvent?.Kind);
        Assert.Contains("permission_denied", deniedEvent?.Text);
        Assert.Equal(RunEventKind.ApiRetry, retryEvent?.Kind);
    }

    [Fact]
    public void Environment_is_built_explicitly_without_user_credentials()
    {
        using var temp = new TempDirectory();
        var folders = RunFolders.Create(temp.Path);
        IDictionary parent = new Hashtable
        {
            ["PATH"] = "/usr/bin",
            ["ANTHROPIC_API_KEY"] = "clave-personal-del-usuario",
            ["ANTHROPIC_BASE_URL"] = "https://otra-direccion.invalid",
            ["AWS_SECRET_ACCESS_KEY"] = "otra-credencial",
            ["HOME"] = "/home/usuario",
        };

        var environment = ChildEnvironment.Build(Gateway, folders, 128_000, 64_000, parent);

        Assert.Equal("/usr/bin", environment["PATH"]);
        Assert.False(environment.ContainsKey("ANTHROPIC_API_KEY"));
        Assert.False(environment.ContainsKey("AWS_SECRET_ACCESS_KEY"));
        Assert.Equal(Gateway.BaseUrl, environment["ANTHROPIC_BASE_URL"]);
        Assert.Equal(Gateway.AuthToken, environment["ANTHROPIC_AUTH_TOKEN"]);
        Assert.Equal(folders.Home, environment["HOME"]);
        Assert.Equal(folders.ClaudeConfig, environment["CLAUDE_CONFIG_DIR"]);
        Assert.Equal("128000", environment["CLAUDE_CODE_MAX_CONTEXT_TOKENS"]);
        Assert.Equal("64000", environment["CLAUDE_CODE_MAX_OUTPUT_TOKENS"]);
    }

    [Fact]
    public void Run_folders_are_created_inside_the_work_directory()
    {
        using var temp = new TempDirectory();

        var folders = RunFolders.Create(temp.Path);

        Assert.All(
            [folders.Input, folders.Output, folders.ClaudeConfig, folders.Home],
            folder => Assert.True(Directory.Exists(folder)));
        Assert.Empty(Directory.GetFiles(folders.ClaudeConfig));
    }

    [Theory]
    [InlineData("error con la clave fake-token-123456 en medio", "error con la clave *** en medio")]
    [InlineData("POST https://gateway.invalid:4000/v1/messages falló", "POST ***/v1/messages falló")]
    [InlineData("no se resuelve GATEWAY.invalid", "no se resuelve ***")]
    [InlineData("{\"url\":\"https:\\/\\/gateway.invalid:4000\"}", "{\"url\":\"***\"}")]
    [InlineData("texto sin secretos", "texto sin secretos")]
    public void Redactor_masks_the_gateway_key_and_address(string input, string expected)
    {
        Assert.Equal(expected, SecretRedactor.ForGateway(Gateway).Redact(input));
    }

    [Fact]
    public void Gateway_settings_do_not_print_their_values()
    {
        var printed = Gateway.ToString();

        Assert.DoesNotContain(Gateway.AuthToken, printed);
        Assert.DoesNotContain("gateway.invalid", printed);
    }

    [Fact]
    public void Input_within_budget_is_accepted()
    {
        var input = new string('a', 35_000); // ~10,000 tokens

        Assert.Equal(10_000, InputBudget.EstimateTokens(input));
        Assert.Null(InputBudget.Check(input, budgetTokens: 80_000));
    }

    [Fact]
    public void Input_over_budget_is_refused_with_a_clear_message()
    {
        var input = new string('a', 350_000); // ~100,000 tokens

        var error = InputBudget.Check(input, budgetTokens: 80_000);

        Assert.NotNull(error);
        Assert.Contains("100000", error);
        Assert.Contains("80000", error);
    }
}
