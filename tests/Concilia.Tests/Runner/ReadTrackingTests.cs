using Concilia.Core.Runner;
using Concilia.Runner;
using static Concilia.Tests.Runner.FakeClaudeLauncher;

namespace Concilia.Tests.Runner;

// Reading by parts (09-10-2026): the runner records the Read calls of the
// model and the context compactions, and ReadCoverage says which lines of a
// file were not read.
public class ReadTrackingTests
{
    [Fact]
    public void A_read_inside_the_run_folder_is_kept_relative_to_it()
    {
        using var temp = new TempDirectory();
        var file = Path.Combine(temp.Path, "input", "documentacion.txt").Replace("\\", "\\\\");

        var read = ClaudeCliRunner.ReadOf($$"""{"file_path":"{{file}}","offset":201,"limit":200}""", temp.Path)!;

        Assert.Equal(new FileRead("input/documentacion.txt", 201, 200, false), read);
        Assert.Equal((201, 400), (read.FirstLine, read.LastLine));
    }

    [Fact]
    public void Without_offset_or_limit_a_read_takes_claude_codes_default()
    {
        var read = ClaudeCliRunner.ReadOf("""{"file_path":"input/documentacion.txt"}""", "/work")!;

        Assert.Equal((1, FileRead.DefaultLimit), (read.FirstLine, read.LastLine));
        Assert.Null(ClaudeCliRunner.ReadOf("""{"otro":1}""", "/work"));
        Assert.Null(ClaudeCliRunner.ReadOf("no es json", "/work"));
    }

    [Fact]
    public void Coverage_lists_the_lines_no_successful_read_covered()
    {
        FileRead[] reads =
        [
            new("input/documentacion.txt", 1, 100, true),
            new("input/documentacion.txt", 151, 50, true),
            new("input/documentacion.txt", 201, 100, false), // failed: does not count
            new("input/otro.txt", 1, 1000, true),            // another file
        ];

        var missing = ReadCoverage.Missing("input/documentacion.txt", 250, reads);

        Assert.Equal([(101, 150), (201, 250)], missing);
        Assert.Equal("101–150, 201–250", ReadCoverage.Describe(missing));
        Assert.Empty(ReadCoverage.Missing("input/documentacion.txt", 100, reads));
    }

    [Fact]
    public void Line_count_ignores_a_final_newline()
    {
        Assert.Equal(0, ReadCoverage.LineCount(""));
        Assert.Equal(2, ReadCoverage.LineCount("a\nb\n"));
        Assert.Equal(2, ReadCoverage.LineCount("a\nb"));
    }

    // End to end with FakeClaude: two Read calls (one fails) and a compaction.
    [Fact]
    public async Task The_runner_records_the_reads_and_the_compactions()
    {
        using var temp = new TempDirectory();
        var request = new RunRequest
        {
            RunId = Guid.NewGuid(),
            ModelId = "SS - GPT-6-SOL",
            Prompt = Directive("synthetic-lectura-por-partes.jsonl", writeFiles: ["output/resumen.md"]) + "Lee input/documentacion.txt.",
            WorkDirectory = temp.Path,
            ExpectedOutputFiles = ["output/resumen.md"],
        };
        var events = new List<RunEvent>();

        var result = await new ClaudeCliRunner(ExecutablePath, FakeGateway).RunAsync(request, events.Add, CancellationToken.None);

        Assert.Equal(RunStatus.Done, result.Status);
        // The fixture's folder is /work, outside this run's folder: the path stays as it came.
        Assert.Equal(
            [new FileRead("/work/input/documentacion.txt", 1, 2, true), new FileRead("/work/input/documentacion.txt", 3, 2, false)],
            result.Reads);
        Assert.Equal(1, result.Compactions);
        Assert.Contains(events, e => e.Kind == RunEventKind.Warning && e.Text.StartsWith("Claude Code compactó el contexto"));
    }
}
