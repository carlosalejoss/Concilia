using Concilia.Core.Runner;
using Concilia.Runner;
using static Concilia.Tests.Runner.FakeClaudeLauncher;

namespace Concilia.Tests.Runner;

public class RetryRunnerTests
{
    // A provider that answers from a list, in order, and records each request.
    private sealed class ScriptedProvider(params RunResult[] answers) : IRunnerProvider
    {
        private int _next;
        public List<RunRequest> Requests { get; } = [];
        public string Name => "Scripted";

        public Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            onEvent(new RunEvent(RunEventKind.Started, request.RunId.ToString()));
            return Task.FromResult(answers[_next++]);
        }
    }

    private static readonly RunResult Done = new() { Status = RunStatus.Done, Verdict = new Verdict(VerdictState.Done, null) };
    private static readonly RunResult NoVerdict = new() { Status = RunStatus.Failed, VerdictError = "No se ha encontrado ninguna línea." };
    private static readonly RunResult Blocked = new() { Status = RunStatus.Blocked, Verdict = new Verdict(VerdictState.Blocked, "x") };
    private static readonly RunResult ApiError = new() { Status = RunStatus.Failed, IsError = true, ErrorMessage = "403" };
    private static readonly RunResult TimedOut = new() { Status = RunStatus.TimedOut };
    private static readonly RunResult HeartbeatLost = new() { Status = RunStatus.HeartbeatLost };

    private static RunRequest FirstRequest(string root)
    {
        var workDirectory = Path.Combine(root, Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(workDirectory, "input"));
        File.WriteAllText(Path.Combine(workDirectory, "input", "doc.md"), "# ReservaSalas");
        return new RunRequest
        {
            RunId = Guid.Parse(Path.GetFileName(workDirectory)),
            ModelId = "SS - GPT-6-SOL",
            Prompt = "Resume los archivos.",
            WorkDirectory = workDirectory,
        };
    }

    [Fact]
    public async Task Retries_after_a_missing_verdict_with_a_new_linked_run()
    {
        using var temp = new TempDirectory();
        var provider = new ScriptedProvider(NoVerdict, Done);
        var first = FirstRequest(temp.Path);

        var attempts = await new RetryRunner(provider).RunAsync(first, _ => _ => { }, CancellationToken.None);

        Assert.Equal(2, attempts.Count);
        Assert.Null(attempts[0].PreviousRunId);
        Assert.Equal(first.RunId, attempts[1].PreviousRunId);
        Assert.NotEqual(first.RunId, attempts[1].Request.RunId);
        Assert.Equal(RunStatus.Done, attempts[1].Result.Status);

        var retry = attempts[1].Request;
        Assert.StartsWith(first.Prompt, retry.Prompt);
        Assert.Contains(NoVerdict.VerdictError!, retry.Prompt);
        Assert.NotEqual(first.WorkDirectory, retry.WorkDirectory);
        Assert.Equal(retry.RunId.ToString(), Path.GetFileName(retry.WorkDirectory));
        Assert.True(File.Exists(Path.Combine(retry.WorkDirectory, "input", "doc.md")));
    }

    // The mockup carries output/mockup/ too, so a repair only rewrites the
    // parts with errors; other tasks carry only input/.
    [Fact]
    public async Task A_retry_gets_a_copy_of_the_folders_the_request_carries()
    {
        using var temp = new TempDirectory();
        var provider = new ScriptedProvider(NoVerdict, Done);
        var first = FirstRequest(temp.Path) with { CarriedFolders = ["input", "output/mockup"] };
        Directory.CreateDirectory(Path.Combine(first.WorkDirectory, "output", "mockup"));
        File.WriteAllText(Path.Combine(first.WorkDirectory, "output", "mockup", "EP-01.html"), "<section></section>");
        File.WriteAllText(Path.Combine(first.WorkDirectory, "output", "otro.json"), "{}");

        var attempts = await new RetryRunner(provider).RunAsync(first, _ => _ => { }, CancellationToken.None);

        var retry = attempts[1].Request.WorkDirectory;
        Assert.True(File.Exists(Path.Combine(retry, "input", "doc.md")));
        Assert.True(File.Exists(Path.Combine(retry, "output", "mockup", "EP-01.html")));
        Assert.False(File.Exists(Path.Combine(retry, "output", "otro.json")));
    }

    [Fact]
    public async Task Stops_after_two_retries()
    {
        using var temp = new TempDirectory();
        var provider = new ScriptedProvider(NoVerdict, NoVerdict, NoVerdict, Done);

        var attempts = await new RetryRunner(provider).RunAsync(FirstRequest(temp.Path), _ => _ => { }, CancellationToken.None);

        Assert.Equal(1 + RetryRunner.DefaultMaxRetries, attempts.Count);
        Assert.Equal(RunStatus.Failed, attempts[^1].Result.Status);
        Assert.Equal(attempts[1].Request.RunId, attempts[2].PreviousRunId);
    }

    [Theory]
    [MemberData(nameof(NotRetried))]
    public async Task Only_verdict_problems_are_retried(RunResult answer)
    {
        using var temp = new TempDirectory();

        var attempts = await new RetryRunner(new ScriptedProvider(answer)).RunAsync(
            FirstRequest(temp.Path), _ => _ => { }, CancellationToken.None);

        Assert.Single(attempts);
    }

    public static TheoryData<RunResult> NotRetried => [Done, Blocked, ApiError, TimedOut];

    [Fact]
    public async Task A_heartbeat_loss_is_repeated_once_with_the_same_prompt()
    {
        using var temp = new TempDirectory();
        var first = FirstRequest(temp.Path);

        var attempts = await new RetryRunner(new ScriptedProvider(HeartbeatLost, Done)).RunAsync(
            first, _ => _ => { }, CancellationToken.None);

        Assert.Equal(2, attempts.Count);
        Assert.Equal(RunStatus.HeartbeatLost, attempts[0].Result.Status);
        Assert.Equal(first.RunId, attempts[1].PreviousRunId);
        Assert.Equal(first.Prompt, attempts[1].Request.Prompt);
        Assert.Equal(RunStatus.Done, attempts[1].Result.Status);
    }

    [Fact]
    public async Task A_second_heartbeat_loss_is_not_repeated()
    {
        using var temp = new TempDirectory();

        var attempts = await new RetryRunner(new ScriptedProvider(HeartbeatLost, NoVerdict, HeartbeatLost, Done)).RunAsync(
            FirstRequest(temp.Path), _ => _ => { }, CancellationToken.None);

        Assert.Equal(3, attempts.Count);
        Assert.Equal(RunStatus.HeartbeatLost, attempts[^1].Result.Status);
    }

    [Fact]
    public async Task The_heartbeat_repetition_does_not_count_as_a_retry()
    {
        using var temp = new TempDirectory();
        var numbers = new List<int>();

        var attempts = await new RetryRunner(new ScriptedProvider(HeartbeatLost, NoVerdict, Done), maxRetries: 1).RunAsync(
            FirstRequest(temp.Path),
            _ => _ => { },
            (attempt, number) =>
            {
                numbers.Add(number);
                return RetryRunner.ReviewVerdict(attempt);
            },
            CancellationToken.None);

        Assert.Equal(3, attempts.Count);
        Assert.Equal(RunStatus.Done, attempts[^1].Result.Status);
        Assert.Equal([1, 1, 2], numbers);
    }

    [Fact]
    public async Task Each_attempt_gets_its_own_event_stream()
    {
        using var temp = new TempDirectory();
        var streams = new Dictionary<Guid, List<RunEvent>>();

        await new RetryRunner(new ScriptedProvider(NoVerdict, Done)).RunAsync(
            FirstRequest(temp.Path),
            request =>
            {
                var stream = new List<RunEvent>();
                streams[request.RunId] = stream;
                return stream.Add;
            },
            CancellationToken.None);

        Assert.Equal(2, streams.Count);
        Assert.All(streams, s => Assert.Equal(s.Key.ToString(), Assert.Single(s.Value).Text));
    }

    [Fact]
    public async Task End_to_end_with_fake_claude_a_bad_verdict_is_retried()
    {
        using var temp = new TempDirectory();
        var first = FirstRequest(temp.Path) with
        {
            // Every attempt replays the same fixture without verdict: 3 attempts, all failed.
            Prompt = Directive("synthetic-sin-veredicto.jsonl") + "Resume los archivos.",
        };

        var attempts = await new RetryRunner(new ClaudeCliRunner(ExecutablePath, FakeGateway))
            .RunAsync(first, _ => _ => { }, CancellationToken.None);

        Assert.Equal(3, attempts.Count);
        Assert.All(attempts, a => Assert.NotNull(a.Result.VerdictError));
        // The retry note reached the (fake) model through stdin.
        var received = Received(new Concilia.Runner.Execution.RunFolders(attempts[^1].Request.WorkDirectory));
        Assert.Contains("veredicto válido", received.GetProperty("prompt").GetString());
    }
}
