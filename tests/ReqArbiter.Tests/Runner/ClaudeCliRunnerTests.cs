using ReqArbiter.Core.Runner;
using ReqArbiter.Runner;
using static ReqArbiter.Tests.Runner.FakeClaudeLauncher;

namespace ReqArbiter.Tests.Runner;

// End to end with the FakeClaude executable replaying the fixtures:
// process + parser + verdict + evaluation, without Claude Code or network.
public class ClaudeCliRunnerTests
{
    private const string Output = "output/resumen.md";

    private static ClaudeCliRunner Runner() => new(ExecutablePath, FakeGateway);

    private static RunRequest Request(string workDirectory, string directive, params string[] expected) => new()
    {
        RunId = Guid.NewGuid(),
        ModelId = "SS - GPT-6-SOL",
        Prompt = directive + "Resume los archivos de input/ en output/resumen.md.",
        WorkDirectory = workDirectory,
        ExpectedOutputFiles = expected,
        HeartbeatTimeout = TimeSpan.FromSeconds(30),
        TotalTimeout = TimeSpan.FromSeconds(60),
    };

    private static async Task<(RunResult Result, List<RunEvent> Events)> Run(RunRequest request)
    {
        var events = new List<RunEvent>();
        var result = await Runner().RunAsync(request, events.Add, CancellationToken.None);
        return (result, events);
    }

    [Theory]
    [InlineData("tools-sol.jsonl", 3)]
    [InlineData("tools-luna.jsonl", 4)]
    public async Task Correct_run_is_done(string fixture, int turns)
    {
        using var temp = new TempDirectory();

        var (result, events) = await Run(Request(temp.Path, Directive(fixture, writeFiles: [Output]), Output));

        Assert.Equal(RunStatus.Done, result.Status);
        Assert.Equal(VerdictState.Done, result.Verdict?.State);
        Assert.Equal([Output], result.OutputFiles);
        Assert.Equal(turns, result.NumTurns);
        Assert.Equal("completed", result.TerminalReason);
        Assert.False(result.IsError);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(RunEventKind.Started, events[0].Kind);
        Assert.Equal(RunEventKind.Finished, events[^1].Kind);
        Assert.Contains(events, e => e.Kind == RunEventKind.ToolUse && e.Text.StartsWith("Write"));
    }

    [Fact]
    public async Task Usage_includes_cached_input_tokens()
    {
        using var temp = new TempDirectory();

        var (result, _) = await Run(Request(temp.Path, Directive("tools-sol.jsonl", writeFiles: [Output]), Output));

        Assert.Equal(new TokenUsage(1772 + 15442 + 28904, 148), result.Usage);
    }

    [Fact]
    public async Task Reports_the_context_window_assumed_by_claude_code()
    {
        using var temp = new TempDirectory();

        var (_, events) = await Run(Request(temp.Path, Directive("tools-sol.jsonl", writeFiles: [Output]), Output));

        // The recordings were made without CLAUDE_CODE_MAX_CONTEXT_TOKENS: 200000.
        Assert.Contains(events, e => e.Kind == RunEventKind.ModelInfo
            && e.Text == "Ventana de contexto asumida para SS - GPT-6-SOL: 200000 tokens (salida máxima 32000)");
    }

    [Fact]
    public async Task Missing_output_file_fails_even_with_verdict_done()
    {
        using var temp = new TempDirectory();

        var (result, _) = await Run(Request(temp.Path, Directive("tools-sol.jsonl"), Output));

        Assert.Equal(RunStatus.Failed, result.Status);
        Assert.Contains("Faltan archivos de salida", result.ErrorMessage);
        Assert.Null(result.VerdictError);
    }

    [Theory]
    [InlineData("synthetic-sin-veredicto.jsonl", "No se ha encontrado")]
    [InlineData("synthetic-veredicto-invalido.jsonl", "no es válido")]
    public async Task Missing_or_invalid_verdict_fails_and_is_marked_for_retry(string fixture, string expectedError)
    {
        using var temp = new TempDirectory();

        var (result, _) = await Run(Request(temp.Path, Directive(fixture, writeFiles: [Output]), Output));

        Assert.Equal(RunStatus.Failed, result.Status);
        Assert.Null(result.Verdict);
        Assert.Contains(expectedError, result.VerdictError);
    }

    [Fact]
    public async Task Blocked_verdict_gives_blocked_with_reason()
    {
        using var temp = new TempDirectory();

        var (result, _) = await Run(Request(temp.Path, Directive("synthetic-bloqueada.jsonl"), Output));

        Assert.Equal(RunStatus.Blocked, result.Status);
        Assert.Equal("El archivo de entrada no describe ningún requisito.", result.Verdict?.Reason);
        Assert.Null(result.VerdictError);
    }

    [Fact]
    public async Task Api_error_fails_and_is_not_marked_for_retry()
    {
        using var temp = new TempDirectory();

        var (result, _) = await Run(Request(temp.Path, Directive("error-modelo-inexistente.jsonl", exitCode: 1), Output));

        Assert.Equal(RunStatus.Failed, result.Status);
        Assert.True(result.IsError);
        Assert.Contains("403", result.ErrorMessage);
        Assert.Null(result.VerdictError);
    }

    [Fact]
    public async Task Output_cut_without_result_fails()
    {
        using var temp = new TempDirectory();

        var (result, _) = await Run(Request(temp.Path, Directive("synthetic-cortada.jsonl"), Output));

        Assert.Equal(RunStatus.Failed, result.Status);
        Assert.Contains("sin evento \"result\"", result.ErrorMessage);
    }

    [Fact]
    public async Task Non_zero_exit_code_fails_even_with_a_good_result()
    {
        using var temp = new TempDirectory();

        var (result, _) = await Run(Request(temp.Path, Directive("tools-sol.jsonl", exitCode: 2, writeFiles: [Output]), Output));

        Assert.Equal(RunStatus.Failed, result.Status);
        Assert.Contains("código de salida 2", result.ErrorMessage);
    }

    [Fact]
    public async Task Api_retry_is_progress_and_the_run_still_succeeds()
    {
        using var temp = new TempDirectory();

        var (result, events) = await Run(Request(temp.Path, Directive("synthetic-reintento-api.jsonl", writeFiles: [Output]), Output));

        Assert.Equal(RunStatus.Done, result.Status);
        Assert.Contains(events, e => e.Kind == RunEventKind.ApiRetry);
    }

    [Fact]
    public async Task Heartbeat_loss_is_reported()
    {
        using var temp = new TempDirectory();
        var request = Request(temp.Path, Directive("sin-herramientas.jsonl", sleepAfterMs: 30_000)) with
        {
            HeartbeatTimeout = TimeSpan.FromMilliseconds(700),
        };

        var (result, _) = await Run(request);

        Assert.Equal(RunStatus.HeartbeatLost, result.Status);
    }

    [Fact]
    public async Task Stderr_is_kept_as_redacted_warnings()
    {
        using var temp = new TempDirectory();
        string[] stderr =
        [
            "[claude-code:unrecognized_model] SS - GPT-6-SOL is not a known model",
            $"conectando a {FakeGateway.BaseUrl} con {FakeGateway.AuthToken}",
        ];

        var (result, events) = await Run(Request(temp.Path, Directive("tools-sol.jsonl", stderr: stderr, writeFiles: [Output]), Output));

        Assert.Equal(RunStatus.Done, result.Status);
        Assert.Equal(stderr[0], result.Warnings[0]);
        Assert.Equal("conectando a *** con ***", result.Warnings[1]);
        Assert.All(events, e => Assert.DoesNotContain(FakeGateway.AuthToken, e.Text));
    }

    [Fact]
    public async Task Over_budget_input_is_refused_without_launching()
    {
        using var temp = new TempDirectory();
        var request = Request(temp.Path, Directive("tools-sol.jsonl"), Output) with { InputTokenBudget = 10 };

        var (result, _) = await Run(request);

        Assert.Equal(RunStatus.Failed, result.Status);
        Assert.Contains("demasiado grande", result.ErrorMessage);
        Assert.False(File.Exists(Path.Combine(temp.Path, "fake-received.json")));
    }

    [Fact]
    public async Task Missing_executable_fails_with_a_clear_message()
    {
        using var temp = new TempDirectory();
        var runner = new ClaudeCliRunner(Path.Combine(temp.Path, "no-existe"), FakeGateway);

        var result = await runner.RunAsync(Request(temp.Path, "", Output), _ => { }, CancellationToken.None);

        Assert.Equal(RunStatus.Failed, result.Status);
        Assert.Contains("No se ha podido iniciar", result.ErrorMessage);
    }
}
