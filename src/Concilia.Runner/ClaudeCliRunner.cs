using System.ComponentModel;
using Concilia.Core.Runner;
using Concilia.Runner.Execution;
using Concilia.Runner.Security;
using Concilia.Runner.StreamJson;

namespace Concilia.Runner;

// Runs one attempt of "claude -p" with one way of reaching the model (the
// gateway or the subscription, see IModelAccess): checks the input budget,
// prepares the run folder and the environment, launches the process, parses
// its output and evaluates the result. It does not retry (see RetryRunner).
public class ClaudeCliRunner(string executable, IModelAccess access) : IRunnerProvider
{
    public string Name => "ClaudeCli";

    public async Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken)
    {
        var redactor = access.Redactor();
        onEvent(new RunEvent(RunEventKind.Started, $"Ejecución {request.RunId} con {request.ModelId}"));

        // Refuse before launching anything if the input is too big. The input is
        // the prompt plus, when the request says so, the files in input/. The
        // v0 phases and the mockup leave them out: the model reads them by
        // parts (see RunRequest.InputFilesInBudget).
        // File sizes in bytes stand in for characters: for UTF-8 text that
        // slightly overestimates, which is the safe side.
        var inputFolder = Path.Combine(request.WorkDirectory, "input");
        var inputFilesSize = request.InputFilesInBudget && Directory.Exists(inputFolder)
            ? Directory.EnumerateFiles(inputFolder, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length)
            : 0;
        var budgetError = InputBudget.Check(request.Prompt.Length + inputFilesSize, request.InputTokenBudget);
        if (budgetError is not null)
        {
            return Finish(onEvent, new RunResult { Status = RunStatus.Failed, ErrorMessage = budgetError });
        }

        var folders = RunFolders.Create(request.WorkDirectory);
        var launch = new ProcessLaunch(
            executable,
            ClaudeArguments.Build(request),
            folders.Root,
            ChildEnvironment.Build(access, folders, request.MaxContextTokens, request.MaxOutputTokens, Environment.GetEnvironmentVariables()),
            request.Prompt,
            request.HeartbeatTimeout,
            request.TotalTimeout);

        ResultEvent? result = null;
        string? claudeCodeVersion = null;
        var warnings = new List<string>();

        // Set when going on is useless, with the reason (see TruncatedToolCall).
        using var stop = new CancellationTokenSource();
        string? stopReason = null;

        // The Read calls waiting for their result (by tool call id), those
        // already answered, and the context compactions.
        var pendingReads = new Dictionary<string, FileRead>();
        var reads = new List<FileRead>();
        var compactions = 0;

        // The Write calls waiting for their result, and the files written, to
        // know when every expected output is there (see CompactingAfterOutput).
        var pendingWrites = new Dictionary<string, string>();
        var written = new HashSet<string>();

        void OnLine(string rawLine)
        {
            // Redact first: everything derived from the line is then safe.
            foreach (var streamEvent in StreamJsonParser.ParseLine(redactor.Redact(rawLine)))
            {
                switch (streamEvent)
                {
                    case ToolUseEvent { Name: "Read" } read when ReadOf(read.Input, folders.Root) is { } fileRead:
                        pendingReads[read.ToolUseId] = fileRead;
                        break;
                    case ToolResultEvent answered when pendingReads.Remove(answered.ToolUseId, out var fileRead):
                        reads.Add(fileRead with { Succeeded = !answered.IsError });
                        break;
                    case ToolUseEvent { Name: "Write" } write when ReadOf(write.Input, folders.Root) is { } target:
                        pendingWrites[write.ToolUseId] = target.Path;
                        break;
                    case ToolResultEvent answered when pendingWrites.Remove(answered.ToolUseId, out var path) && !answered.IsError:
                        written.Add(path);
                        break;
                    case SystemEvent { Subtype: CompactBoundary }:
                        compactions++;
                        break;
                }

                switch (streamEvent)
                {
                    case InitEvent init:
                        claudeCodeVersion = init.ClaudeCodeVersion;
                        break;
                    case ResultEvent r:
                        result = r;
                        break;
                    case InvalidLineEvent invalid:
                        warnings.Add(invalid.Line);
                        break;
                    case ToolResultEvent toolResult when stopReason is null && IsTruncatedToolCall(toolResult):
                        stopReason = TruncatedToolCall;
                        stop.Cancel();
                        break;
                    case SystemEvent system when stopReason is null && IsCompacting(system)
                                                 && request.ExpectedOutputFiles.Count > 0
                                                 && request.ExpectedOutputFiles.All(written.Contains):
                        stopReason = CompactingAfterOutput;
                        stop.Cancel();
                        break;
                }

                if (EventSummaries.From(streamEvent) is { } runEvent)
                {
                    onEvent(runEvent);
                }
            }
        }

        ProcessOutcome outcome;
        try
        {
            outcome = await ClaudeProcess.RunAsync(launch, OnLine, cancellationToken, stop.Token);
        }
        catch (Win32Exception e)
        {
            // The executable was not found or could not be started.
            return Finish(onEvent, new RunResult
            {
                Status = RunStatus.Failed,
                ErrorMessage = $"No se ha podido iniciar \"{executable}\": {e.Message}",
            });
        }

        // stderr is only read as warnings: the "unrecognized_model" notice goes
        // there and is not an error.
        foreach (var line in outcome.StandardError.Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var redacted = redactor.Redact(line);
            warnings.Add(redacted);
            onEvent(new RunEvent(RunEventKind.Warning, EventSummaries.Truncate(redacted, EventSummaries.MaxToolTextLength)));
        }

        // Report the context window Claude Code assumed for the model: it shows
        // whether CLAUDE_CODE_MAX_CONTEXT_TOKENS took effect (128000) or not (200000).
        // Its maxOutputTokens is only the model's default (32000 for unknown
        // models), not the cap in use: Claude Code 2.1.283 applies
        // CLAUDE_CODE_MAX_OUTPUT_TOKENS up to 128000 for them, so both are shown.
        foreach (var model in result?.ModelUsage ?? [])
        {
            onEvent(new RunEvent(
                RunEventKind.ModelInfo,
                $"Ventana de contexto asumida para {model.Model}: {model.ContextWindow} tokens " +
                $"(salida máxima por defecto {model.MaxOutputTokens}; el runner pide {request.MaxOutputTokens})"));
        }

        return Finish(onEvent, RunEvaluator.Evaluate(outcome, result, request, warnings, stopReason) with
        {
            ClaudeCodeVersion = claudeCodeVersion,
            Reads = reads,
            Compactions = compactions,
        });
    }

    // The "system" line Claude Code writes when it compacts the context.
    public const string CompactBoundary = "compact_boundary";

    public const string CompactingAfterOutput =
        "Se detuvo la ejecución: la salida ya estaba escrita y Claude Code empezó a compactar el contexto antes de " +
        "contestar con el veredicto. La plataforma revisa la salida igualmente.";

    // {"type":"system","subtype":"status","status":"compacting"}: Claude Code
    // starts to compact the context. A real arbiter of the catalog (14-10-2026)
    // wrote its output and then tried to compact seven times, without ending,
    // until the 30 minutes ran out; all that was left was the verdict line.
    // Once every expected output is written there is nothing to gain by
    // waiting: the run stops and its output is reviewed.
    private static bool IsCompacting(SystemEvent system)
    {
        if (system.Subtype != "status")
            return false;
        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(system.RawJson);
            return json.RootElement.TryGetProperty("status", out var status)
                && status.ValueKind == System.Text.Json.JsonValueKind.String
                && status.GetString() == "compacting";
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    // The arguments of a Read call: {"file_path": "...", "offset": 10, "limit": 200}.
    // The path is made relative to the run folder when it is inside it, so
    // reads of repairs (each in its own folder) compare with each other.
    public static FileRead? ReadOf(string input, string root)
    {
        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(input);
            var args = json.RootElement;
            if (!args.TryGetProperty("file_path", out var pathElement) || pathElement.GetString() is not { } path)
                return null;
            int? Number(string name) => args.TryGetProperty(name, out var n) && n.TryGetInt32(out var value) ? value : null;

            var relative = Path.IsPathRooted(path) ? Path.GetRelativePath(root, path) : path;
            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
                relative = path; // outside the run folder: kept as it came
            return new FileRead(relative.Replace('\\', '/'), Number("offset") ?? 1, Number("limit"), false);
        }
        catch (System.Text.Json.JsonException)
        {
            return null; // arguments that cannot be read: not counted as a read
        }
    }

    public const string TruncatedToolCall =
        "La llamada a Write llegó cortada (JSON incompleto): la respuesta del modelo agotó el tope de salida " +
        "antes de terminar el fichero. Se detuvo la ejecución en lugar de dejar que el modelo lo repitiera igual.";

    // Claude Code answers a tool call whose arguments are not valid JSON with
    // this error. In the first real catalog run (milestone 5) it meant that the
    // answer hit the output cap in the middle of Write: the model then wrote
    // the whole file again, hit the cap again, and used up the 30 minutes.
    // Stopping at the first one saves that time; the caller may retry.
    private static bool IsTruncatedToolCall(ToolResultEvent toolResult) =>
        toolResult.IsError && toolResult.Content.Contains("could not be parsed as JSON", StringComparison.Ordinal);

    // The last line of the console. A run stopped after writing its output is
    // Failed for the runner (it never got to the verdict), but its output is
    // usually good and the platform reviews it next: "Terminada: Failed" made
    // it look like an error (15-10-2026), so the line says what happened.
    private static RunResult Finish(Action<RunEvent> onEvent, RunResult result)
    {
        if (result.ErrorMessage == CompactingAfterOutput)
        {
            onEvent(new RunEvent(RunEventKind.Finished, StoppedAfterOutputLine));
            return result;
        }
        var detail = result.ErrorMessage is null ? "" : $": {EventSummaries.Truncate(result.ErrorMessage, EventSummaries.MaxTextLength)}";
        onEvent(new RunEvent(RunEventKind.Finished, $"Terminada: {result.Status}{detail}"));
        return result;
    }

    public const string StoppedAfterOutputLine =
        "Detenida: la salida ya estaba escrita y Claude Code empezó a compactar el contexto antes del veredicto. " +
        "La plataforma revisa la salida.";
}
