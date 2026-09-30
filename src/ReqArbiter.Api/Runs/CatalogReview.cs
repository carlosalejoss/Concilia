using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Runner;
using ReqArbiter.Runner;

namespace ReqArbiter.Api.Runs;

// Step 27: after each attempt of a lot, validate the catalog the model wrote
// (the report is saved in output/informe.json) and decide what to keep and
// whether to ask again. Retries count towards RetryRunner.MaxRetries, the
// verdict ones included.
public static class CatalogReview
{
    public static AttemptReview Review(RunAttempt attempt, int attemptsSoFar, string originalPrompt, string systemName)
    {
        var result = attempt.Result;
        var workDirectory = attempt.Request.WorkDirectory;
        var report = CatalogReport.ValidateRunFolder(workDirectory, systemName);

        // The model did not get to answer (API error, timeout...) or declared
        // itself blocked: asking again would not change it.
        if (!ModelAnswered(result))
        {
            return new AttemptReview(result, null);
        }

        if (report.Valid)
        {
            // Warnings do not cause a retry. A valid catalog with a bad verdict
            // line only needs that line: the verdict note, as in milestone 3.
            return result.VerdictError is { } verdictError
                ? new AttemptReview(result, originalPrompt + RetryRunner.RetryNote(verdictError))
                : new AttemptReview(result, null);
        }

        // The catalog is missing, is not JSON or has errors: the lot fails
        // unless a repair fixes it. The repair is the ORIGINAL prompt plus the
        // annex with the previous catalog and one error per line (not the
        // previous prompt, so annexes do not pile up).
        var truncated = result.ErrorMessage == ClaudeCliRunner.TruncatedToolCall;
        var errors = new List<string>();
        if (truncated)
        {
            errors.Add(result.ErrorMessage!);
        }
        errors.AddRange(report.Errors);
        if (result.VerdictError is { } error)
        {
            errors.Add($"Línea de veredicto no válida: {error}");
        }

        var failed = result with
        {
            Status = RunStatus.Failed,
            ErrorMessage = truncated ? result.ErrorMessage : InvalidCatalogMessage(report),
        };
        var annex = PromptBuilder.BuildRepairAnnex(attemptsSoFar, PreviousCatalog(workDirectory), errors);
        return new AttemptReview(failed, originalPrompt + "\n" + annex);
    }

    // True when the process and the API worked and what failed, if anything, is
    // what the model wrote: its catalog, its verdict line, or a Write cut by
    // the output cap.
    private static bool ModelAnswered(RunResult result) =>
        result.Status == RunStatus.Done
        || result.VerdictError is not null
        || result.Verdict?.State == VerdictState.Done // "hecho" but the file is missing
        || result.ErrorMessage == ClaudeCliRunner.TruncatedToolCall;

    private static string InvalidCatalogMessage(CatalogValidationReport report)
    {
        var more = report.Errors.Count > 1 ? $" (y {report.Errors.Count - 1} errores más; ver output/informe.json)" : "";
        return $"Catálogo no válido: {report.Errors.FirstOrDefault() ?? "sin detalle"}{more}";
    }

    // The file as the model left it (the annex asks for it "tal cual"), or
    // nothing when it did not write one.
    private static string PreviousCatalog(string workDirectory)
    {
        var path = Path.Combine(workDirectory, CatalogTask.OutputFile);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }
}
