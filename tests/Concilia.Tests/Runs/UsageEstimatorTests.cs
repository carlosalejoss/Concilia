using Concilia.Core.Runner;
using Concilia.Core.Runs;

namespace Concilia.Tests.Runs;

// Step 95: the estimate of the tokens of a run without usage, from its console.
public class UsageEstimatorTests
{
    private static readonly Guid First = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Repair = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // The files of the run, as the model saw them.
    private static readonly Dictionary<string, string> Files = new()
    {
        [$"/data/runs/{First}/input/doc.txt"] = "uno\ndos\ntres",   // lines of 3, 3 and 4 characters
        [$"/data/runs/{First}/output/salida.json"] = new string('x', 50),
    };

    private static string? Read(string path) => Files.GetValueOrDefault(path);

    private static RunEvent Use(string text) => new(RunEventKind.ToolUse, text);
    private static RunEvent Result() => new(RunEventKind.ToolResult, "…");

    [Fact]
    public void The_events_of_each_attempt_are_split_by_their_start()
    {
        RunEvent[] events =
        [
            new(RunEventKind.Started, $"Ejecución {First} con SS - GPT-6-SOL"),
            Use("Read {}"),
            new(RunEventKind.Started, $"[reintento] Ejecución {Repair} con SS - GPT-6-SOL"),
            new(RunEventKind.ToolUse, "[reintento] Write {}"),
        ];

        var attempts = UsageEstimator.SplitAttempts(First, events);

        Assert.Equal(2, attempts[First].Count);
        Assert.Equal("Write {}", attempts[Repair][1].Text); // without the "[reintento] " mark
    }

    [Fact]
    public void Each_turn_sends_the_whole_context_again()
    {
        RunEvent[] attempt =
        [
            Use($$"""Read {"file_path":"/data/runs/{{First}}/input/doc.txt","offset":1,"limit":2}"""), // turn 1: 2 lines
            Result(),
            Use($$"""Write {"file_path":"/data/runs/{{First}}/output/salida.json","content":"xx…"""),    // turn 2
            Result(),
            new(RunEventKind.AssistantText, "VEREDICTO"),                                             // turn 3
        ];

        var simulation = UsageEstimator.Simulate(attempt, Read);

        // Context: after turn 1, the call and its 2 lines (3 + 8 and 3 + 8);
        // after turn 2, also the call and the 50 characters written.
        const long afterRead = 120 + 11 + 11;
        const long afterWrite = afterRead + 120 + 50;
        Assert.Equal(new ContextSimulation(3, 0 + afterRead + afterWrite, 50 + "VEREDICTO".Length, 0), simulation);
    }

    [Fact]
    public void A_compaction_sends_the_context_once_and_leaves_only_a_summary()
    {
        RunEvent[] attempt =
        [
            Use($$"""Read {"file_path":"/data/runs/{{First}}/input/doc.txt"}"""), // the whole file: 3 lines
            Result(),
            new(RunEventKind.Warning, """Aviso del sistema (status): {"type":"system","subtype":"status","status":"compacting"}"""),
            new(RunEventKind.Warning, """Claude Code compactó el contexto: {"type":"system","subtype":"compact_boundary"}"""),
            new(RunEventKind.AssistantText, "fin"),
        ];

        var simulation = UsageEstimator.Simulate(attempt, Read);

        const long afterRead = 120 + 11 + 11 + 12;
        Assert.Equal(2, simulation.Turns);
        Assert.Equal(1, simulation.Compactions);
        Assert.Equal(afterRead, simulation.InputChars); // the compaction; the last turn starts from 0
    }

    [Fact]
    public void A_file_that_is_not_there_adds_only_the_call()
    {
        var simulation = UsageEstimator.Simulate([Use("""Read {"file_path":"/data/runs/otro/x.txt"}"""), Result(), Use("Read {}")], Read);

        Assert.Equal(120, simulation.InputChars);
    }

    [Fact]
    public void The_tokens_come_from_the_simulation_and_the_coefficients()
    {
        var estimate = UsageEstimator.Estimate(new ContextSimulation(2, 1_000, 100, 0), new UsageCoefficients(10, 0.5, 3, 0.25));

        Assert.Equal(new UsageEstimate(2 * 10 + 500, 2 * 3 + 25), estimate);
    }

    [Theory]
    [InlineData("/data/runs/11111111-1111-1111-1111-111111111111/input/doc.txt", "input/doc.txt")]
    [InlineData(@"C:\TFG\data\runs\11111111-1111-1111-1111-111111111111\output\x.json", @"output\x.json")]
    public void A_path_of_a_run_folder_is_found_wherever_the_data_are(string path, string relative)
    {
        Assert.Equal((First, relative), UsageEstimator.InRunFolder(path));
        Assert.Null(UsageEstimator.InRunFolder("/tmp/otro.txt"));
    }
}
