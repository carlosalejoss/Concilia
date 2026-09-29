using System.ComponentModel;
using ReqArbiter.Core.Runner;
using ReqArbiter.Runner.Execution;
using ReqArbiter.Runner.Security;
using ReqArbiter.Runner.StreamJson;

namespace ReqArbiter.Runner;

// Runs one attempt of "claude -p" against a gateway: checks the input budget,
// prepares the run folder and the environment, launches the process, parses
// its output and evaluates the result. It does not retry (see VerdictRetryRunner).
public class ClaudeCliRunner(string executable, GatewaySettings gateway) : IRunnerProvider
{
    public string Name => "ClaudeCli";

    public async Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken)
    {
        var redactor = SecretRedactor.ForGateway(gateway);
        onEvent(new RunEvent(RunEventKind.Started, $"Ejecución {request.RunId} con {request.ModelId}"));

        // Refuse before launching anything if the input is too big. The input is
        // the prompt plus the files in input/, which the model reads with Read.
        // File sizes in bytes stand in for characters: for UTF-8 text that
        // slightly overestimates, which is the safe side.
        var inputFolder = Path.Combine(request.WorkDirectory, "input");
        var inputFilesSize = Directory.Exists(inputFolder)
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
            ChildEnvironment.Build(gateway, folders, request.MaxContextTokens, Environment.GetEnvironmentVariables()),
            request.Prompt,
            request.HeartbeatTimeout,
            request.TotalTimeout);

        ResultEvent? result = null;
        var warnings = new List<string>();

        void OnLine(string rawLine)
        {
            // Redact first: everything derived from the line is then safe.
            foreach (var streamEvent in StreamJsonParser.ParseLine(redactor.Redact(rawLine)))
            {
                switch (streamEvent)
                {
                    case ResultEvent r:
                        result = r;
                        break;
                    case InvalidLineEvent invalid:
                        warnings.Add(invalid.Line);
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
            outcome = await ClaudeProcess.RunAsync(launch, OnLine, cancellationToken);
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
        foreach (var model in result?.ModelUsage ?? [])
        {
            onEvent(new RunEvent(
                RunEventKind.ModelInfo,
                $"Ventana de contexto asumida para {model.Model}: {model.ContextWindow} tokens (salida máxima {model.MaxOutputTokens})"));
        }

        return Finish(onEvent, RunEvaluator.Evaluate(outcome, result, request, warnings));
    }

    private static RunResult Finish(Action<RunEvent> onEvent, RunResult result)
    {
        var detail = result.ErrorMessage is null ? "" : $": {EventSummaries.Truncate(result.ErrorMessage, EventSummaries.MaxTextLength)}";
        onEvent(new RunEvent(RunEventKind.Finished, $"Terminada: {result.Status}{detail}"));
        return result;
    }
}
