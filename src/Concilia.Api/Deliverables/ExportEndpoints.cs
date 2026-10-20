using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.OpenApi;
using Concilia.Api.Persistence;
using Concilia.Core.Deliverables;
using Concilia.Core.Deliverables.Export;
using Concilia.Core.Phases;
using Concilia.Core.Questions;

namespace Concilia.Api.Deliverables;

// Downloads of the deliverables (hito 19). They only read: the current
// version as it is saved (validated or draft), never a task.
public static class ExportEndpoints
{
    public static void MapExportEndpoints(this WebApplication app)
    {
        // Step 96: the catalog as Word or PDF.
        app.MapGet("/projects/{projectId:int}/catalog.{format}", async (int projectId,
            [Description("`docx` (Word) o `pdf`.")] string format, AppDbContext db, FileStorage storage) =>
        {
            if (format is not ("docx" or "pdf"))
            {
                return Results.NotFound();
            }
            var project = await db.Projects.FindAsync(projectId);
            var catalog = await DeliverableEndpoints.CurrentAsync(db, projectId, DeliverableKind.Catalog);
            if (project is null || catalog is null)
            {
                return Results.NotFound();
            }
            var export = await CatalogAsync(db, storage, project.Name, catalog);
            var name = $"catalogo-{FileName(project.Name)}.{format}";
            return format == "docx"
                ? Results.File(CatalogDocx.Write(export), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", name)
                : Results.File(CatalogPdf.Write(export), "application/pdf", name);
        })
            .WithTags(ApiTags.Deliverables)
            .WithSummary("Exporta el catálogo a Word o PDF")
            .WithDescription("El catálogo guardado, validado o en borrador (con «BORRADOR» en la portada y el pie). Una sección por tipo de requisito, cada uno con sus fuentes y preguntas, y dos anexos con el texto de cada fragmento y cada pregunta citados. Lleva las marcas «Suposición de la IA» y «Discrepancia sin revisar».")
            .Produces<Stream>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/pdf")
            .Produces(StatusCodes.Status404NotFound);
    }

    // The fragments come from the plan of the job that generated the catalog
    // (the documentation it was written from); the questions, from the project.
    private static async Task<CatalogExport> CatalogAsync(AppDbContext db, FileStorage storage, string projectName, Deliverable catalog)
    {
        var plan = PhasePlan.Load(storage.JobFolder(catalog.JobId));
        var questions = (await db.Questions.Where(q => q.ProjectId == catalog.ProjectId).ToListAsync())
            .Select(q => new ExportQuestion(q.Code, q.Text, q.Status == QuestionStatus.Answered ? q.Answer : null, QuestionRules.IsAcceptedGuess(q)))
            .ToList();
        return CatalogExport.Build(
            projectName, catalog.Status == DeliverableStatus.Validated, Spain(DateTimeOffset.UtcNow),
            catalog.ValidatedAt is { } validated ? Spain(validated) : null,
            catalog.Content, plan.Fragments, plan.Input.Documentation, questions);
    }

    // The dates of the document in peninsular time, the author's; UTC if the
    // system does not know the time zone.
    private static DateTimeOffset Spain(DateTimeOffset date) =>
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Madrid", out var zone) ? TimeZoneInfo.ConvertTime(date, zone) : date;

    // "Clínicas Veterinarias" → "clinicas-veterinarias", for the name of the file.
    public static string FileName(string projectName)
    {
        var plain = new StringBuilder();
        foreach (var ch in projectName.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            plain.Append(char.IsAsciiLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');
        }
        var name = string.Join('-', plain.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        return name.Length > 0 ? name : "proyecto";
    }
}
