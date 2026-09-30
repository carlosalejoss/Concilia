using ReqArbiter.Core.Runner;
using ReqArbiter.Runner.Execution;
using ReqArbiter.Runner.StreamJson;

namespace ReqArbiter.Runner;

// Decides the final status of a run. A run is correct (Done) only if ALL hold:
// the process exited with code 0, is_error is false, terminal_reason is
// "completed", the verdict is "hecho" and every expected output file exists.
// The checks go from the most basic to the most specific, and the first one
// that fails gives the message.
public static class RunEvaluator
{
    public static RunResult Evaluate(
        ProcessOutcome outcome,
        ResultEvent? result,
        RunRequest request,
        IReadOnlyList<string> warnings,
        string? stopReason = null)
    {
        var found = request.ExpectedOutputFiles
            .Where(f => File.Exists(RunPaths.ResolveInside(request.WorkDirectory, f)))
            .ToList();

        var baseResult = new RunResult
        {
            Status = RunStatus.Failed,
            ResultText = result?.Result,
            OutputFiles = found,
            NumTurns = result?.NumTurns,
            DurationMs = result?.DurationMs,
            TerminalReason = result?.TerminalReason,
            IsError = result?.IsError,
            Usage = result?.Usage is { } u ? new TokenUsage(u.TotalInputTokens, u.OutputTokens) : null,
            Warnings = warnings,
        };

        RunResult Fail(string message) => baseResult with { Status = RunStatus.Failed, ErrorMessage = message };

        switch (outcome.End)
        {
            case ProcessEnd.TimedOut:
                return baseResult with { Status = RunStatus.TimedOut, ErrorMessage = $"Se agotó el tiempo máximo ({request.TotalTimeout})." };
            case ProcessEnd.HeartbeatLost:
                return baseResult with { Status = RunStatus.HeartbeatLost, ErrorMessage = $"Sin actividad durante {request.HeartbeatTimeout}: se detuvo la ejecución." };
            case ProcessEnd.Stopped:
                return Fail(stopReason ?? "Se detuvo la ejecución.");
        }

        if (result is null)
        {
            return Fail($"La salida terminó sin evento \"result\" (código de salida {outcome.ExitCode}).");
        }

        // Checked before the exit code: an API error also ends with a non-zero
        // code, and its message is the more useful one.
        if (result.IsError)
        {
            var status = result.ApiErrorStatus is { } code ? $" {code}" : "";
            return Fail($"Error de la API{status} ({result.TerminalReason}): {result.Result}");
        }

        if (outcome.ExitCode != 0)
        {
            return Fail($"claude terminó con el código de salida {outcome.ExitCode}.");
        }

        if (result.TerminalReason != "completed")
        {
            return Fail($"La ejecución no se completó: terminal_reason = \"{result.TerminalReason}\".");
        }

        var parsed = VerdictParser.Parse(result.Result);
        if (parsed.Verdict is null)
        {
            return Fail($"Veredicto no válido: {parsed.Error}") with { VerdictError = parsed.Error };
        }

        if (parsed.Verdict.State == VerdictState.Blocked)
        {
            return baseResult with
            {
                Status = RunStatus.Blocked,
                Verdict = parsed.Verdict,
                ErrorMessage = $"El modelo se declaró bloqueado: {parsed.Verdict.Reason ?? "sin motivo"}",
            };
        }

        var missing = request.ExpectedOutputFiles.Except(found).ToList();
        if (missing.Count > 0)
        {
            return Fail($"Faltan archivos de salida: {string.Join(", ", missing)}.") with { Verdict = parsed.Verdict };
        }

        return baseResult with { Status = RunStatus.Done, Verdict = parsed.Verdict };
    }
}
