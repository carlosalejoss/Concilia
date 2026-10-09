using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Mockup;
using ReqArbiter.Core.Phases;

namespace ReqArbiter.Core.Runner;

// A provider that calls no model: it emits a few fixed events, writes the
// expected output files and answers "hecho". A run of a v0 phase (input/fase.json)
// gets a valid output of its phase from FakePhaseOutput, a mockup run a plain
// .html with a view per story from FakeMockup; a catalog v1.3
// (output/catalogo.json, with the lot's fragments in input/) gets a real one
// from FakeCatalog; any other file, placeholder content. With simulateRepair,
// the first attempt of each run writes an invalid output, so the repair retry
// can be seen working; the repair (its prompt carries the previous output)
// writes it right.
// Used in development without secrets and in tests.
public class FakeProvider(TimeSpan stepDelay = default, bool simulateRepair = false) : IRunnerProvider
{
    public const string VerdictLine = """VEREDICTO: {"estado":"hecho"}""";

    // The repair annex wraps the previous catalog in this tag.
    private const string RepairMarker = "<catalogo_previo>";

    public string Name => "Fake";

    public async Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken)
    {
        // stepDelay lets the web show the events arriving one by one.
        async Task Step(RunEvent runEvent)
        {
            await Task.Delay(stepDelay, cancellationToken);
            onEvent(runEvent);
        }

        await Step(new RunEvent(RunEventKind.Started, $"Ejecución {request.RunId} (proveedor Fake)"));
        await Step(new RunEvent(RunEventKind.ModelInfo, $"Modelo {request.ModelId} (simulado)"));

        // Like a model that reads by parts, but in one call per file: every
        // file of input/ entirely, so the reading check passes.
        var reads = new List<FileRead>();
        if (request.AllowedTools.Contains("Read") && Directory.Exists(Path.Combine(request.WorkDirectory, "input")))
        {
            foreach (var file in Directory.GetFiles(Path.Combine(request.WorkDirectory, "input")).Order(StringComparer.Ordinal))
            {
                var relative = "input/" + Path.GetFileName(file);
                await Step(new RunEvent(RunEventKind.ToolUse, $"Read {relative}"));
                reads.Add(new FileRead(relative, 1, Math.Max(1, ReadCoverage.LineCount(await File.ReadAllTextAsync(file, cancellationToken))), true));
            }
        }

        foreach (var relativePath in request.ExpectedOutputFiles)
        {
            var path = RunPaths.ResolveInside(request.WorkDirectory, relativePath);
            await Step(new RunEvent(RunEventKind.ToolUse, $"Write {relativePath}"));

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, Content(request, relativePath), cancellationToken);
        }

        var resultText = "Tarea simulada completada.\n" + VerdictLine;
        await Step(new RunEvent(RunEventKind.AssistantText, resultText));
        await Step(new RunEvent(RunEventKind.Finished, "Terminada: Done"));

        return new RunResult
        {
            Status = RunStatus.Done,
            Verdict = new Verdict(VerdictState.Done, null),
            ResultText = resultText,
            OutputFiles = request.ExpectedOutputFiles,
            NumTurns = 1,
            DurationMs = 0,
            TerminalReason = "completed",
            IsError = false,
            Usage = new TokenUsage(0, 0),
            Reads = reads,
        };
    }

    private string Content(RunRequest request, string relativePath)
    {
        if (relativePath == MockupTask.OutputFile)
        {
            var first = !request.Prompt.Contains(FakePhaseOutput.RepairMarker, StringComparison.Ordinal);
            return FakeMockup.For(request.WorkDirectory, broken: simulateRepair && first);
        }
        if (FakePhaseOutput.IsPhaseRun(request.WorkDirectory))
        {
            var first = !request.Prompt.Contains(FakePhaseOutput.RepairMarker, StringComparison.Ordinal);
            return FakePhaseOutput.For(request.WorkDirectory, broken: simulateRepair && first);
        }

        var isCatalog = Path.GetFileName(relativePath) == "catalogo.json"
            && File.Exists(Path.Combine(request.WorkDirectory, FakeCatalog.FragmentsFile));
        if (!isCatalog)
        {
            return "# Resultado simulado\n\nEscrito por FakeProvider; ningún modelo ha intervenido.\n";
        }

        var firstAttempt = !request.Prompt.Contains(RepairMarker, StringComparison.Ordinal);
        return FakeCatalog.For(request.WorkDirectory, mutated: simulateRepair && firstAttempt);
    }
}
