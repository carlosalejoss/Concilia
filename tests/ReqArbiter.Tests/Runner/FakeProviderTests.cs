using ReqArbiter.Core.Runner;

namespace ReqArbiter.Tests.Runner;

public class FakeProviderTests
{
    private static RunRequest Request(string workDirectory, params string[] expectedOutputs) => new()
    {
        RunId = Guid.NewGuid(),
        ModelId = "SS - GPT-6-SOL",
        Prompt = "Resume los archivos.",
        WorkDirectory = workDirectory,
        ExpectedOutputFiles = expectedOutputs,
    };

    [Fact]
    public void Request_defaults_follow_the_specification()
    {
        var request = Request("/work");

        Assert.Equal(["Read", "Write"], request.AllowedTools);
        Assert.Equal(10, request.MaxTurns);
        Assert.Equal(TimeSpan.FromSeconds(120), request.HeartbeatTimeout);
        Assert.Equal(TimeSpan.FromMinutes(10), request.TotalTimeout);
        Assert.Equal(128_000, request.MaxContextTokens);
        Assert.Equal(80_000, request.InputTokenBudget);
    }

    [Fact]
    public async Task Returns_done_with_verdict_and_writes_expected_outputs()
    {
        using var temp = new TempDirectory();

        var result = await new FakeProvider().RunAsync(
            Request(temp.Path, "output/resumen.md"), _ => { }, CancellationToken.None);

        Assert.Equal(RunStatus.Done, result.Status);
        Assert.Equal(new Verdict(VerdictState.Done, null), result.Verdict);
        Assert.EndsWith(FakeProvider.VerdictLine, result.ResultText);
        Assert.Equal(["output/resumen.md"], result.OutputFiles);
        Assert.False(result.IsError);
        Assert.True(File.Exists(Path.Combine(temp.Path, "output", "resumen.md")));
    }

    [Fact]
    public async Task Publishes_events_in_order()
    {
        using var temp = new TempDirectory();
        var events = new List<RunEvent>();

        await new FakeProvider().RunAsync(Request(temp.Path, "output/resumen.md"), events.Add, CancellationToken.None);

        Assert.Equal(
            [RunEventKind.Started, RunEventKind.ModelInfo, RunEventKind.ToolUse, RunEventKind.AssistantText, RunEventKind.Finished],
            events.Select(e => e.Kind));
    }

    [Fact]
    public async Task Cancellation_stops_the_run()
    {
        using var temp = new TempDirectory();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new FakeProvider().RunAsync(Request(temp.Path), _ => { }, cancellation.Token));
    }

    [Theory]
    [InlineData("../fuera.md")]
    [InlineData("output/../../fuera.md")]
    public async Task Output_paths_outside_the_work_directory_are_refused(string relativePath)
    {
        using var temp = new TempDirectory();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new FakeProvider().RunAsync(Request(temp.Path, relativePath), _ => { }, CancellationToken.None));
    }
}
