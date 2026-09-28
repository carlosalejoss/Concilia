namespace ReqArbiter.Core.Runner;

// A provider that calls no model: it emits a few fixed events, writes the
// expected output files with placeholder content and answers "hecho".
// Used in development without secrets and in tests.
public class FakeProvider(TimeSpan stepDelay = default) : IRunnerProvider
{
    public const string VerdictLine = """VEREDICTO: {"estado":"hecho"}""";

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

        foreach (var relativePath in request.ExpectedOutputFiles)
        {
            var path = RunPaths.ResolveInside(request.WorkDirectory, relativePath);
            await Step(new RunEvent(RunEventKind.ToolUse, $"Write {relativePath}"));

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(
                path,
                "# Resultado simulado\n\nEscrito por FakeProvider; ningún modelo ha intervenido.\n",
                cancellationToken);
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
        };
    }
}
