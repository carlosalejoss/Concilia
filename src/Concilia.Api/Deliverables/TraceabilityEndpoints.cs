using Concilia.Api.Files;
using Concilia.Api.OpenApi;
using Concilia.Api.Persistence;
using Concilia.Core.Deliverables;

namespace Concilia.Api.Deliverables;

// Step 99: the traceability of the saved catalog and backlog, for the panel
// of the web. It only reads, and it uses the same content as the exports.
public static class TraceabilityEndpoints
{
    public static void MapTraceabilityEndpoints(this WebApplication app)
    {
        app.MapGet("/projects/{projectId:int}/traceability", async (int projectId, AppDbContext db, FileStorage storage) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            var catalog = await DeliverableEndpoints.CurrentAsync(db, projectId, DeliverableKind.Catalog);
            if (project is null || catalog is null)
            {
                return Results.NotFound();
            }
            var catalogExport = await ExportEndpoints.CatalogAsync(db, storage, project.Name, catalog);
            var (_, backlogExport) = await ExportEndpoints.BacklogAsync(db, projectId);
            return Results.Ok(Traceability.Build(catalogExport, backlogExport));
        })
            .WithTags(ApiTags.Deliverables)
            .WithSummary("Trazabilidad del catálogo y el backlog")
            .WithDescription("Del catálogo y el backlog guardados (validados o en borrador). Cada requisito con sus fragmentos de la documentación (`sources`), las preguntas en que se apoya y las historias que lo cubren; cada historia con su épica y sus requisitos. El texto de cada fragmento citado está una sola vez en `fragments` (sin `text` si no está en la documentación de la que salió el catálogo) y cada pregunta citada, con su respuesta, en `questions`. `missingQuestions` y `missingRequirements` son las que se citan y no existen. Sin backlog, `stories` va vacío. `404` si el proyecto no tiene catálogo.")
            .Produces<Traceability>()
            .Produces(StatusCodes.Status404NotFound);
    }
}
