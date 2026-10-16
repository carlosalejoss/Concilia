using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Api.Runs;
using Concilia.Core.Deliverables;
using Concilia.Core.Phases;
using Concilia.Core.Questions;
using Concilia.Core.Reports;
using Concilia.Core.Runs;

namespace Concilia.Api.Reports;

// The report of each job (step 64), the base of the evaluation, and all the
// jobs of a project as a CSV to open in a spreadsheet.
public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapGet("/jobs/{jobId:guid}/report", async (Guid jobId, AppDbContext db, FileStorage storage) =>
        {
            var job = await db.Jobs.FindAsync(jobId);
            return job is null ? Results.NotFound() : Results.Ok(await BuildAsync(db, storage, job));
        });

        // One row per job, newest first, separated by ";" and with a BOM so
        // Excel in Spanish opens it with its columns and accents.
        app.MapGet("/projects/{projectId:int}/report.csv", async (int projectId, AppDbContext db, FileStorage storage) =>
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            {
                return Results.NotFound();
            }
            var jobs = await db.Jobs.Where(j => j.ProjectId == projectId).OrderByDescending(j => j.CreatedAt).ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine(string.Join(';', Columns));
            foreach (var job in jobs)
                csv.AppendLine(string.Join(';', Row(await BuildAsync(db, storage, job)).Select(Cell)));
            return Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
                "text/csv; charset=utf-8", $"informe-proyecto-{projectId}.csv");
        });
    }

    private static readonly string[] Columns =
    [
        "fecha", "trabajo", "tipo", "resultado", "tokens_entrada", "tokens_salida", "ejecuciones_sin_consumo", "segundos", "reparaciones",
        "elementos_A", "elementos_B", "elementos_union", "de_ambas", "solo_A", "solo_B", "discrepancias", "descartados",
        "usuario_conservados", "usuario_modificados", "usuario_anadidos", "usuario_borrados",
        "preguntas", "respondidas", "descartadas", "pendientes", "respuestas_usuario", "respuestas_ia", "respuestas_ia_editadas",
        "fase", "modelo_arbitro", "arbitraje_de",
        "mensaje",
    ];

    private static IEnumerable<object?> Row(JobReport r) =>
    [
        r.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), r.JobId, r.Kind, r.Succeeded ? "ok" : "fallo",
        r.InputTokens, r.OutputTokens, r.RunsWithoutUsage, r.Seconds is { } s ? Math.Round(s) : null, r.Repairs,
        r.Elements?.A, r.Elements?.B, r.Elements?.Merged, r.Elements?.Both, r.Elements?.OnlyA, r.Elements?.OnlyB,
        r.Elements?.Discrepancies, r.Elements?.Discarded,
        r.Changes?.Kept, r.Changes?.Modified, r.Changes?.Added, r.Changes?.Deleted,
        r.Questions?.Total, r.Questions?.Answered, r.Questions?.Discarded, r.Questions?.Pending,
        r.Questions?.ByUser, r.Questions?.ByAi, r.Questions?.ByAiEdited,
        r.Phase, r.ArbiterModel, r.RearbitrationOf,
        r.Message,
    ];

    private static string Cell(object? value)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        return text.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
    }

    public static async Task<JobReport> BuildAsync(AppDbContext db, FileStorage storage, Job job)
    {
        var runs = await db.Runs.Where(r => r.JobId == job.Id)
            .OrderBy(r => r.Slot).ThenBy(r => r.LotNumber).ThenBy(r => r.Attempt).ToListAsync();
        var lines = runs.Select(r => new RunLine(r.Slot ?? "", r.LotNumber, r.Attempt, r.Model, r.State.ToString(),
            r.InputTokens, r.OutputTokens, r.NumTurns,
            r.StartedAt is { } start && r.FinishedAt is { } end ? (end - start).TotalSeconds : null,
            r.ErrorMessage)).ToList();

        var folder = storage.JobFolder(job.Id);
        var result = ReadResult(folder);
        var succeeded = result?["fuente"] is JsonValue;
        var output = result?["salida"] is JsonNode salida ? salida.ToJsonString() : null;

        ElementCounts? elements = null;
        UserChanges? changes = null;
        string? changesNote = null;
        QuestionCounts? questions = null;
        // A new arbitration takes its phase from its plan, and its proposals
        // from the copies in its folder (it has no runs of A and B).
        var rearbitration = job.Kind == Rearbitration.JobKind;
        var hasPlan = File.Exists(Path.Combine(folder, PhasePlan.FileName));
        var kind = PhasePlan.KindOfJob(job.Kind) ?? (rearbitration && hasPlan ? PhasePlan.Load(folder).Kind : null);
        Guid? rearbitrationOf = null;
        if (kind is { } phase && hasPlan)
        {
            var plan = PhasePlan.Load(folder);
            rearbitrationOf = plan.RearbitrationOf;
            int? Proposal(string slot)
            {
                if (rearbitration)
                {
                    var copy = Path.Combine(folder, Rearbitration.ProposalFile(slot));
                    return File.Exists(copy) ? JobReports.Count(phase, File.ReadAllText(copy)) : null;
                }
                var last = runs.Where(r => r.Slot == slot).OrderBy(r => r.Attempt).LastOrDefault();
                if (last?.State != RunState.Done)
                    return null;
                var path = Path.Combine(storage.RunFolder(last.Id), PhaseDefinition.For(phase, PhaseRole.Proposal).OutputFile);
                return File.Exists(path) ? JobReports.Count(phase, File.ReadAllText(path)) : null;
            }

            if (result?["fuente"]?.GetValue<string>() == "ARB" && output is not null)
                elements = JobReports.Merge(phase, output, plan, Proposal("A"), Proposal("B"));

            if (rearbitration)
            {
                changesNote = "Arbitraje experimental: no cambia las preguntas, el catálogo ni el backlog del proyecto.";
            }
            else if (phase is PhaseKind.Catalog or PhaseKind.Backlog && succeeded && output is not null)
            {
                var deliverableKind = phase == PhaseKind.Catalog ? DeliverableKind.Catalog : DeliverableKind.Backlog;
                var current = await db.Deliverables.SingleOrDefaultAsync(d => d.ProjectId == job.ProjectId && d.Kind == deliverableKind);
                if (current?.JobId == job.Id)
                {
                    // The draft the job left, rebuilt from its frozen result.
                    var frozen = new PhaseJobResult(result!["fuente"]!.GetValue<string>(), result["verificacion_cruzada"]!.GetValue<bool>(), output, "");
                    var original = phase == PhaseKind.Catalog ? CatalogDraft.FromResult(frozen, plan) : BacklogDraft.FromResult(frozen, plan);
                    changes = JobReports.Diff(phase, original, current.Content);
                }
                else
                    changesNote = "El entregable actual viene de otro trabajo: los cambios del usuario se cuentan en ese.";
            }

            if (phase == PhaseKind.Questions && !rearbitration)
            {
                var round = await db.Questions.Where(q => q.JobId == job.Id).ToListAsync();
                questions = new QuestionCounts(round.Count,
                    round.Count(q => q.Status == QuestionStatus.Answered),
                    round.Count(q => q.Status == QuestionStatus.Discarded),
                    round.Count(q => q.Status == QuestionStatus.Pending),
                    round.Count(q => q.Status == QuestionStatus.Answered && q.AnswerOrigin is null or "usuario"),
                    round.Count(q => q.Status == QuestionStatus.Answered && q.AnswerOrigin == "ia"),
                    round.Count(q => q.Status == QuestionStatus.Answered && q.AnswerOrigin == "ia-editada"));
            }
        }

        return new JobReport(job.Id, job.Kind, job.CreatedAt, job.FinishedAt, succeeded, result?["mensaje"]?.GetValue<string>(),
            runs.Sum(r => r.InputTokens ?? 0), runs.Sum(r => r.OutputTokens ?? 0),
            runs.Count(r => r.StartedAt is not null && r.InputTokens is null), runs.Count(r => r.Attempt > 1),
            job.FinishedAt is { } finished ? (finished - job.CreatedAt).TotalSeconds : null,
            lines, elements, changes, changesNote, questions,
            kind?.ToString(), runs.FirstOrDefault(r => r.Slot == "ARB")?.Model, rearbitrationOf);
    }

    private static JsonNode? ReadResult(string jobFolder)
    {
        var path = Path.Combine(jobFolder, PhaseJobResult.FileName);
        return File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) : null;
    }
}
