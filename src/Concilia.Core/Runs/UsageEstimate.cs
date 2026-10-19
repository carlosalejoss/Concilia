using System.Text.RegularExpressions;
using Concilia.Core.Runner;

namespace Concilia.Core.Runs;

// Step 95 (decision of the author, 19-10-2026): an estimate of the tokens of
// the runs that have no usage. Claude Code only reports it in its last event,
// so a run the platform stops (it compacts with its output written, its tool
// call comes truncated) or that runs out of time has none, and through the
// gateway the usage of each message comes as 0.
//
// How: the platform knows what went into the context of the model, turn by
// turn, from the saved console (each Read with its exact range, each Write)
// and the files of the run folder. Each call to the model sends the whole
// context again, so the input of a run grows with the sum of the context of
// every turn; a compaction leaves only a summary. The tokens come from that
// simulation with coefficients fitted on the runs that do have usage
// (scripts/evaluacion/ajustar-estimador.cs, which also measures the error).
public record ContextSimulation(int Turns, long InputChars, long WrittenChars, int Compactions);

// Tokens = PerTurn × turns + PerChar × characters (input: the context sent in
// every turn; output: what the model wrote). Fitted by least squares.
public record UsageCoefficients(double InputPerTurn, double InputPerChar, double OutputPerTurn, double OutputPerChar);

public record UsageEstimate(long InputTokens, long OutputTokens);

public static partial class UsageEstimator
{
    // Fitted on 19-10-2026 with the 125 attempts that had both usage and a
    // saved console (scripts/evaluacion/ajustar-estimador.cs). One fit for
    // the mockup, whose runs read little and write a lot, and another for
    // the phases (A, B, the arbiter and the suggestions). Error of the fit,
    // estimating each run without it: docs/costes/estimador.md.
    public static readonly UsageCoefficients Mockup = new(1_636, 1 / 3.30, 1_395, 0.387);
    public static readonly UsageCoefficients Phases = new(8_587, 1 / 4.42, 871, 0.320);

    public static UsageCoefficients For(string jobKind) => jobKind == "mockup" ? Mockup : Phases;

    public static UsageEstimate Estimate(ContextSimulation simulation, UsageCoefficients c) => new(
        (long)Math.Round(c.InputPerTurn * simulation.Turns + c.InputPerChar * simulation.InputChars),
        (long)Math.Round(c.OutputPerTurn * simulation.Turns + c.OutputPerChar * simulation.WrittenChars));

    // What each call adds to the context besides its content: the name, the
    // arguments and the markup of the call and of its result.
    private const int CallOverhead = 120;

    // Read shows each line with its number and a tab, and cuts lines longer than 2,000 characters.
    private const int LineOverhead = 8;
    private const int MaxLineLength = 2_000;
    private const int DefaultReadLimit = 2_000;

    [GeneratedRegex("\"file_path\"\\s*:\\s*\"([^\"]+)\"")]
    private static partial Regex FilePath();

    [GeneratedRegex("\"offset\"\\s*:\\s*(\\d+)")]
    private static partial Regex Offset();

    [GeneratedRegex("\"limit\"\\s*:\\s*(\\d+)")]
    private static partial Regex Limit();

    [GeneratedRegex(@"Ejecución ([0-9a-fA-F-]{36})")]
    private static partial Regex StartedRun();

    // The events of each attempt of a run. The repairs publish in the
    // console of the first attempt, with "[reintento] " before the text, and
    // each attempt begins with its Started event ("Ejecución <id> con …").
    public static IReadOnlyDictionary<Guid, IReadOnlyList<RunEvent>> SplitAttempts(Guid firstRunId, IEnumerable<RunEvent> events)
    {
        var attempts = new Dictionary<Guid, List<RunEvent>>();
        var current = firstRunId;
        foreach (var e in events)
        {
            var text = e.Text.StartsWith("[reintento] ", StringComparison.Ordinal) ? e.Text["[reintento] ".Length..] : e.Text;
            if (e.Kind == RunEventKind.Started && StartedRun().Match(text) is { Success: true } started && Guid.TryParse(started.Groups[1].Value, out var id))
                current = id;
            if (!attempts.TryGetValue(current, out var list))
                attempts[current] = list = [];
            list.Add(e with { Text = text });
        }
        return attempts.ToDictionary(a => a.Key, a => (IReadOnlyList<RunEvent>)a.Value);
    }

    // The context, turn by turn. readFile gets a path as the model used it
    // (the path inside the container) and returns the file, or null.
    //   A turn: one answer of the model, made of the texts and calls that
    //   come together before the results of its calls.
    //   Before each turn the whole context is sent again.
    //   A Read adds the lines it read; a Write, the file it wrote (its final
    //   size); a text of the model, its length.
    //   A compaction sends the whole context to summarize it, and then only
    //   the summary is left (counted in the turns).
    public static ContextSimulation Simulate(IEnumerable<RunEvent> attempt, Func<string, string?> readFile)
    {
        int turns = 0, compactions = 0;
        long context = 0, input = 0, written = 0;
        var inAnswer = false;
        foreach (var e in attempt)
        {
            switch (e.Kind)
            {
                case RunEventKind.AssistantText or RunEventKind.ToolUse:
                    if (!inAnswer)
                    {
                        turns++;
                        input += context;
                        inAnswer = true;
                    }
                    if (e.Kind == RunEventKind.AssistantText)
                    {
                        context += e.Text.Length;
                        written += e.Text.Length;
                    }
                    else if (e.Text.StartsWith("Read ", StringComparison.Ordinal))
                        context += CallOverhead + ReadChars(e.Text, readFile);
                    else if (e.Text.StartsWith("Write ", StringComparison.Ordinal))
                    {
                        var size = FilePath().Match(e.Text) is { Success: true } m ? readFile(m.Groups[1].Value)?.Length ?? 0 : 0;
                        context += CallOverhead + size;
                        written += size;
                    }
                    else
                        context += CallOverhead;
                    break;
                case RunEventKind.ToolResult:
                    inAnswer = false;
                    break;
                case RunEventKind.Warning when e.Text.Contains("\"status\":\"compacting\"", StringComparison.Ordinal):
                    input += context;
                    compactions++;
                    break;
                case RunEventKind.Warning when e.Text.Contains("compact_boundary", StringComparison.Ordinal):
                    context = 0;
                    break;
            }
        }
        return new ContextSimulation(turns, input, written, compactions);
    }

    // The characters a Read call brought: the lines of its range, as Read shows them.
    private static long ReadChars(string call, Func<string, string?> readFile)
    {
        if (FilePath().Match(call) is not { Success: true } path || readFile(path.Groups[1].Value) is not { } content)
            return 0;
        var offset = Offset().Match(call) is { Success: true } o ? int.Parse(o.Groups[1].Value) : 1;
        var limit = Limit().Match(call) is { Success: true } l ? int.Parse(l.Groups[1].Value) : DefaultReadLimit;
        return content.Split('\n').Skip(Math.Max(offset, 1) - 1).Take(limit).Sum(line => (long)Math.Min(line.Length, MaxLineLength) + LineOverhead);
    }

    // A path of the container ("/data/runs/<id>/input/x.json") as the folder
    // of that run and the path inside it, to find it wherever the data are.
    [GeneratedRegex(@"[/\\]runs[/\\]([0-9a-fA-F-]{36})[/\\](.+)$")]
    private static partial Regex RunPath();

    public static (Guid RunId, string Relative)? InRunFolder(string path) =>
        RunPath().Match(path) is { Success: true } m && Guid.TryParse(m.Groups[1].Value, out var id) ? (id, m.Groups[2].Value) : null;
}
