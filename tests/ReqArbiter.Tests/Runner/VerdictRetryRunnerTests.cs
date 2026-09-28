using ReqArbiter.Core.Runner;
using ReqArbiter.Runner;
using static ReqArbiter.Tests.Runner.FakeClaudeLauncher;

namespace ReqArbiter.Tests.Runner;

public class VerdictRetryRunnerTests
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

        var attempts = await new VerdictRetryRunner(provider).RunAsync(first, _ => _ => { }, CancellationToken.None);

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

    [Fact]
    public async Task Stops_after_two_retries()
    {
        using var temp = new TempDirectory();
        var provider = new ScriptedProvider(NoVerdict, NoVerdict, NoVerdict, Done);

        var attempts = await new VerdictRetryRunner(provider).RunAsync(FirstRequest(temp.Path), _ => _ => { }, CancellationToken.None);

        Assert.Equal(1 + VerdictRetryRunner.MaxRetries, attempts.Count);
        Assert.Equal(RunStatus.Failed, attempts[^1].Result.Status);
        Assert.Equal(attempts[1].Request.RunId, attempts[2].PreviousRunId);
    }

    [Theory]
    [MemberData(nameof(NotRetried))]
    public async Task Only_verdict_problems_are_retried(RunResult answer)
    {
        using var temp = new TempDirectory();

        var attempts = await new VerdictRetryRunner(new ScriptedProvider(answer)).RunAsync(
            FirstRequest(temp.Path), _ => _ => { }, CancellationToken.None);

        Assert.Single(attempts);
    }

    public static TheoryData<RunResult> NotRetried => [Done, Blocked, ApiError];

    [Fact]
    public async Task Each_attempt_gets_its_own_event_stream()
    {
        using var temp = new TempDirectory();
        var streams = new Dictionary<Guid, List<RunEvent>>();

        await new VerdictRetryRunner(new ScriptedProvider(NoVerdict, Done)).RunAsync(
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

        var attempts = await new VerdictRetryRunner(new ClaudeCliRunner(ExecutablePath, FakeGateway))
            .RunAsync(first, _ => _ => { }, CancellationToken.None);

        Assert.Equal(3, attempts.Count);
        Assert.All(attempts, a => Assert.NotNull(a.Result.VerdictError));
        // The retry note reached the (fake) model through stdin.
        var received = Received(new ReqArbiter.Runner.Execution.RunFolders(attempts[^1].Request.WorkDirectory));
        Assert.Contains("veredicto válido", received.GetProperty("prompt").GetString());
    }
}
