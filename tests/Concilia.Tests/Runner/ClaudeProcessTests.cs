using System.Diagnostics;
using Concilia.Core.Runner;
using Concilia.Runner.Execution;
using static Concilia.Tests.Runner.FakeClaudeLauncher;

namespace Concilia.Tests.Runner;

// The timing tests measure real seconds: they run alone, not in parallel
// with the rest of the suite, so the load of other tests cannot delay them.
[CollectionDefinition(nameof(TimingTests), DisableParallelization = true)]
public class TimingTests;

// Launches the real FakeClaude executable: tests the process handling
// (arguments, environment, stdin, stdout, stderr, timeouts) without Claude Code.
[Collection(nameof(TimingTests))]
public class ClaudeProcessTests
{
    private static RunRequest Request(string workDirectory) => new()
    {
        RunId = Guid.NewGuid(),
        ModelId = "SS - GPT-6-SOL",
        Prompt = "Resume los archivos.",
        WorkDirectory = workDirectory,
    };

    private static Task<ProcessOutcome> Run(ProcessLaunch launch, List<string>? lines = null, CancellationToken token = default) =>
        ClaudeProcess.RunAsync(launch, line => lines?.Add(line), token);

    [Fact]
    public async Task Model_id_with_spaces_arrives_as_a_single_argument()
    {
        using var temp = new TempDirectory();
        var folders = RunFolders.Create(temp.Path);
        var arguments = ClaudeArguments.Build(Request(temp.Path));

        await Run(Launch(folders, Directive(), arguments));

        var received = Received(folders).GetProperty("args").EnumerateArray().Select(a => a.GetString()!).ToList();
        Assert.Equal(arguments, received);
        Assert.Equal("SS - GPT-6-SOL", received[received.IndexOf("--model") + 1]);
    }

    [Fact]
    public async Task Child_gets_only_the_explicit_environment()
    {
        using var temp = new TempDirectory();
        var folders = RunFolders.Create(temp.Path);

        await Run(Launch(folders, Directive()));

        var environment = Received(folders).GetProperty("environment");
        Assert.Equal(FakeGateway.AuthToken, environment.GetProperty("ANTHROPIC_AUTH_TOKEN").GetString());
        Assert.Equal(folders.ClaudeConfig, environment.GetProperty("CLAUDE_CONFIG_DIR").GetString());
        Assert.False(environment.TryGetProperty("ANTHROPIC_API_KEY", out _));

        // Nothing beyond what ChildEnvironment builds (Windows may add a couple
        // of variables of its own to every process, so compare case-insensitively
        // and allow only names from our list).
        var allowed = ChildEnvironment.InheritedVariables
            .Concat(["HOME", "USERPROFILE", "CLAUDE_CONFIG_DIR", "CLAUDE_CODE_MAX_CONTEXT_TOKENS", "CLAUDE_CODE_MAX_OUTPUT_TOKENS", "ANTHROPIC_BASE_URL", "ANTHROPIC_AUTH_TOKEN"])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unexpected = environment.EnumerateObject().Select(p => p.Name).Where(n => !allowed.Contains(n)).ToList();
        Assert.Empty(unexpected);
    }

    [Fact]
    public async Task Prompt_goes_through_stdin_as_utf8_without_bom()
    {
        using var temp = new TempDirectory();
        var folders = RunFolders.Create(temp.Path);
        const string prompt = "Resume la especificación de ReservaSalas — con tildes: canción, año, €.";

        await Run(Launch(folders, Directive() + prompt));

        var received = Received(folders);
        Assert.False(received.GetProperty("stdinHasBom").GetBoolean());
        Assert.Equal(prompt, received.GetProperty("prompt").GetString());
    }

    [Fact]
    public async Task Stdout_lines_arrive_in_order_as_utf8()
    {
        using var temp = new TempDirectory();
        var folders = RunFolders.Create(temp.Path);
        var lines = new List<string>();

        var outcome = await Run(Launch(folders, Directive(fixture: "sin-herramientas.jsonl")), lines);

        Assert.Equal(ProcessEnd.Exited, outcome.End);
        Assert.Equal(0, outcome.ExitCode);
        Assert.Equal(3, lines.Count);
        Assert.StartsWith("{\"type\":\"system\"", lines[0]);
        Assert.Contains("reservación de sala — prueba", lines[2]);
    }

    [Fact]
    public async Task Unrecognized_model_notice_on_stderr_is_captured_not_fatal()
    {
        using var temp = new TempDirectory();
        var folders = RunFolders.Create(temp.Path);
        const string notice = "[claude-code:unrecognized_model] SS - GPT-6-SOL is not a known model";

        var outcome = await Run(Launch(folders, Directive(fixture: "tools-sol.jsonl", stderr: [notice])));

        Assert.Equal(ProcessEnd.Exited, outcome.End);
        Assert.Equal(0, outcome.ExitCode);
        Assert.Equal([notice], outcome.StandardError);
    }

    [Fact]
    public async Task Exit_code_is_reported()
    {
        using var temp = new TempDirectory();
        var folders = RunFolders.Create(temp.Path);

        var outcome = await Run(Launch(folders, Directive(exitCode: 3)));

        Assert.Equal(ProcessEnd.Exited, outcome.End);
        Assert.Equal(3, outcome.ExitCode);
    }

    [Fact]
    public async Task Silence_longer_than_the_heartbeat_kills_the_process()
    {
        using var temp = new TempDirectory();
        var folders = RunFolders.Create(temp.Path);
        var clock = Stopwatch.StartNew();

        var outcome = await Run(Launch(
            folders,
            Directive(fixture: "sin-herramientas.jsonl", sleepAfterMs: 30_000),
            heartbeat: TimeSpan.FromMilliseconds(700)));

        Assert.Equal(ProcessEnd.HeartbeatLost, outcome.End);
        Assert.Null(outcome.ExitCode);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(15), $"Tardó {clock.Elapsed}");
    }

    [Fact]
    public async Task Output_keeps_the_heartbeat_alive_but_not_past_the_total_timeout()
    {
        using var temp = new TempDirectory();
        var folders = RunFolders.Create(temp.Path);
        var lines = new List<string>();

        // One line every 500 ms (9 lines, 8 waits: ~4 s): the 3 s heartbeat
        // never expires, but the 3.5 s total time does, always before the
        // ninth line. The heartbeat leaves room for the start of FakeClaude,
        // which under load (all tests in parallel) took more than 1 s and made
        // the old timings (1 s and 1.2 s) fail now and then.
        var outcome = await Run(
            Launch(
                folders,
                Directive(fixture: "tools-luna.jsonl", lineDelayMs: 500),
                heartbeat: TimeSpan.FromSeconds(3),
                total: TimeSpan.FromMilliseconds(3500)),
            lines);

        Assert.Equal(ProcessEnd.TimedOut, outcome.End);
        Assert.InRange(lines.Count, 1, 8);
    }

    [Fact]
    public async Task Cancellation_kills_the_process_and_throws()
    {
        using var temp = new TempDirectory();
        var folders = RunFolders.Create(temp.Path);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var clock = Stopwatch.StartNew();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Run(Launch(folders, Directive(sleepAfterMs: 30_000)), token: cancellation.Token));

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(15), $"Tardó {clock.Elapsed}");
    }
}
