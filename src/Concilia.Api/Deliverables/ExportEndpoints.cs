using System.ComponentModel;
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
            var name = $"catalogo-{ExportFiles.Slug(project.Name)}.{format}";
            return format == "docx"
                ? Results.File(CatalogDocx.Write(export), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", name)
                : Results.File(CatalogPdf.Write(export), "application/pdf", name);
        })
            .WithTags(ApiTags.Deliverables)
            .WithSummary("Exporta el catálogo a Word o PDF")
            .WithDescription("El catálogo guardado, validado o en borrador (con «BORRADOR» en la portada y el pie). Una sección por tipo de requisito, cada uno con sus fuentes y preguntas, y dos anexos con el texto de cada fragmento y cada pregunta citados. Lleva las marcas «Suposición de la IA» y «Discrepancia sin revisar».")
            .Produces<Stream>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/pdf")
            .Produces(StatusCodes.Status404NotFound);

        // Step 97: the backlog as Gherkin, a .feature file per epic in a .zip.
        app.MapGet("/projects/{projectId:int}/backlog-gherkin.zip", async (int projectId, AppDbContext db) =>
        {
            var (project, backlog) = await BacklogAsync(db, projectId);
            return backlog is null
                ? Results.NotFound()
                : Results.File(BacklogGherkin.Write(backlog), "application/zip", $"backlog-{ExportFiles.Slug(project!)}-gherkin.zip");
        })
            .WithTags(ApiTags.Deliverables)
            .WithSummary("Exporta el backlog a Gherkin")
            .WithDescription("Un `.zip` con un fichero `.feature` por épica, comprobado con el analizador oficial de Gherkin. Palabras clave en inglés: cada épica es una `Feature`, cada historia una `Rule` (con su «Como… quiero… para…» y sus tareas) y cada criterio de aceptación un `Scenario`. Los ids van como etiquetas (`@EP-01`, `@HU-001 @RF-001`, `@CA-001`), y una historia con una discrepancia sin revisar lleva `@discrepancia-sin-revisar`. Del backlog guardado, validado o en borrador (lo dice la primera línea de cada fichero).")
            .Produces<Stream>(StatusCodes.Status200OK, "application/zip")
            .Produces(StatusCodes.Status404NotFound);

        // Step 98: the backlog as CSV to import into Jira or Azure DevOps.
        app.MapGet("/projects/{projectId:int}/backlog-{tool}.csv", async (int projectId,
            [Description("`jira` o `azure-devops`.")] string tool, AppDbContext db) =>
        {
            if (tool is not ("jira" or "azure-devops"))
            {
                return Results.NotFound();
            }
            var (project, backlog) = await BacklogAsync(db, projectId);
            if (backlog is null)
            {
                return Results.NotFound();
            }
            var csv = tool == "jira" ? BacklogCsv.Jira(backlog) : BacklogCsv.AzureDevOps(backlog);
            return Results.File(csv, "text/csv; charset=utf-8", $"backlog-{ExportFiles.Slug(project!)}-{tool}.csv");
        })
            .WithTags(ApiTags.Deliverables)
            .WithSummary("Exporta el backlog a CSV para Jira o Azure DevOps")
            .WithDescription("Un CSV para importar el backlog guardado en cada herramienta, con una fila por épica, historia y tarea, cada una detrás de su padre. **Jira:** la jerarquía va con `Issue ID` y `Parent ID` (épica → `Story` → `Sub-task`), con `Epic Name` en las épicas y los ids de Concilia en columnas `Labels`; la descripción de cada historia lleva su «Como… quiero… para…», los criterios de aceptación en Gherkin y los requisitos. **Azure DevOps** (proceso Agile): `ID` vacío y el título en `Title 1`, `Title 2` o `Title 3` según el nivel (`Epic` → `User Story` → `Task`), los criterios en `Acceptance Criteria` y los ids en `Tags`. Separado por comas, UTF-8 sin BOM. Una historia con una discrepancia sin revisar lleva también `discrepancia-sin-revisar`.")
            .Produces<Stream>(StatusCodes.Status200OK, "text/csv")
            .Produces(StatusCodes.Status404NotFound);
    }

    // The saved backlog of a project and the name of the project, or nulls.
    private static async Task<(string? Project, BacklogExport? Backlog)> BacklogAsync(AppDbContext db, int projectId)
    {
        var project = await db.Projects.FindAsync(projectId);
        var backlog = await DeliverableEndpoints.CurrentAsync(db, projectId, DeliverableKind.Backlog);
        if (project is null || backlog is null)
        {
            return (null, null);
        }
        return (project.Name, BacklogExport.Build(
            project.Name, backlog.Status == DeliverableStatus.Validated, Spain(DateTimeOffset.UtcNow),
            backlog.ValidatedAt is { } validated ? Spain(validated) : null, backlog.Content));
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
}
