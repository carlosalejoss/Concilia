using Concilia.Core.Catalog;
using Concilia.Core.Runner;
using Concilia.Runner;

namespace Concilia.Api.Runs;

// Steps 27 and 31: after each attempt of a lot, validate the catalog the model
// wrote (the report goes to output/informe.json and the accepted catalog to
// output/catalogo_aceptado.json) and decide what to keep and whether to ask
// for a repair. A repair is asked for only for the three reasons of the
// validator (CatalogValidationReport.NeedsRepair): errors of the lot, more
// than 20 % of the requirements rejected, or a capability of the inventory
// without a requirement (C20). How many repairs there can be is
// Catalog:MaxRepairs (RetryRunner stops there).
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

        // A usable catalog makes the attempt Done, even if a repair is asked for
        // (if the repair goes wrong, the job keeps this catalog) or the verdict
        // line is wrong (the catalog is what matters, and repeating the whole
        // lot only for that line costs a full answer).
        if (report.Valid)
        {
            var notes = new List<string>();
            if (result.VerdictError is { } verdictError)
                notes.Add($"Veredicto no válido ({verdictError}); se acepta el catálogo");
            if (report.NeedsRepair)
                notes.Add($"Se pide una reparación: {Summary(report.RepairReasons)}");
            var kept = result with
            {
                Status = RunStatus.Done,
                Verdict = result.Verdict ?? new Verdict(VerdictState.Done, null),
                ErrorMessage = notes.Count > 0 ? string.Join(". ", notes) + "." : null,
                VerdictError = null,
            };
            return new AttemptReview(kept, report.NeedsRepair ? RepairPrompt(originalPrompt, attemptsSoFar, workDirectory, report.RepairReasons) : null);
        }

        // The catalog cannot be used (missing, not JSON, broken structure, too
        // many fragments untreated, or a Write cut by the output cap): the lot
        // fails unless a repair fixes it.
        var truncated = result.ErrorMessage == ClaudeCliRunner.TruncatedToolCall;
        var reasons = new List<string>();
        if (truncated)
            reasons.Add($"LOTE {result.ErrorMessage}");
        reasons.AddRange(report.RepairReasons);

        var failed = result with
        {
            Status = RunStatus.Failed,
            ErrorMessage = truncated ? result.ErrorMessage : $"Catálogo no válido: {Summary(report.Errors)}",
        };
        return new AttemptReview(failed, RepairPrompt(originalPrompt, attemptsSoFar, workDirectory, reasons));
    }

    // The repair is the ORIGINAL prompt plus the annex with the previous catalog
    // and the reasons, one per line (not the previous prompt, so annexes do
    // not pile up).
    private static string RepairPrompt(string originalPrompt, int attemptsSoFar, string workDirectory, IEnumerable<string> reasons) =>
        originalPrompt + "\n" + PromptBuilder.BuildRepairAnnex(attemptsSoFar, PreviousCatalog(workDirectory), reasons);

    // True when the process and the API worked and what failed, if anything, is
    // what the model wrote: its catalog, its verdict line, or a Write cut by
    // the output cap.
    internal static bool ModelAnswered(RunResult result) =>
        result.Status == RunStatus.Done
        || result.VerdictError is not null
        || result.Verdict?.State == VerdictState.Done // "hecho" but the file is missing
        || result.ErrorMessage == ClaudeCliRunner.TruncatedToolCall;

    private static string Summary(IReadOnlyList<string> lines)
    {
        var more = lines.Count > 1 ? $" (y {lines.Count - 1} más; ver {CatalogReport.ReportFile})" : "";
        return $"{lines.FirstOrDefault() ?? "sin detalle"}{more}";
    }

    // The file as the model left it (the annex asks for it "tal cual"), or
    // nothing when it did not write one.
    private static string PreviousCatalog(string workDirectory)
    {
        var path = Path.Combine(workDirectory, CatalogTask.OutputFile);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }
}
