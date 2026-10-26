using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.OpenApi;
using Concilia.Api.Persistence;
using Concilia.Core.Flow;
using Concilia.Core.Projects;

namespace Concilia.Api.Files;

public record IterationRequest(
    [property: Description("Nombre de la iteración. Al crearla, si se deja vacío, «Iteración N».")] string? Name,
    [property: Description("Notas opcionales («Reunión del 15-11»).")] string? Notes);

// The iterations of a project (milestone 22, step 109): the documentation is
// grouped in batches, uploaded to the open one and closed with "Cerrar
// documentación". The rules are in IterationRules.
public static class IterationEndpoints
{
    public static void MapIterationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("").WithTags(ApiTags.Files);

        group.MapGet("/projects/{projectId:int}/iterations", async (int projectId, AppDbContext db) =>
            await db.Projects.AnyAsync(p => p.Id == projectId)
                ? Results.Ok(await db.Iterations.Where(i => i.ProjectId == projectId).OrderBy(i => i.Number).ToListAsync())
                : Results.NotFound())
            .WithSummary("Lista las iteraciones del proyecto")
            .WithDescription("De la primera a la última. Cada archivo dice en `iterationId` a cuál pertenece (nulo en la referencia de estilo `.zip`, que es del proyecto).")
            .Produces<List<Iteration>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/projects/{projectId:int}/iterations", async (int projectId, IterationRequest request, AppDbContext db) =>
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            {
                return Results.NotFound();
            }
            var existing = await db.Iterations.Where(i => i.ProjectId == projectId).ToListAsync();
            var name = string.IsNullOrWhiteSpace(request.Name)
                ? IterationRules.DefaultName(IterationRules.NextNumber(existing))
                : request.Name.Trim();
            var notes = Notes(request.Notes);
            var errors = IterationRules.Validate(name, notes);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }
            var (iteration, error) = IterationRules.Create(projectId, existing, name, notes, DateTimeOffset.UtcNow);
            if (error is not null)
            {
                return Problem(error);
            }
            db.Iterations.Add(iteration!);
            await db.SaveChangesAsync();
            return Results.Created($"/projects/{projectId}/iterations/{iteration!.Number}", iteration);
        })
            .WithSummary("Crea una iteración nueva")
            .WithDescription("Detrás de la última, abierta, para subir su documentación. Solo si las demás están cerradas.")
            .Produces<Iteration>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/projects/{projectId:int}/iterations/{number:int}",
            async (int projectId, int number, IterationRequest request, AppDbContext db) =>
        {
            var iteration = await FindAsync(db, projectId, number);
            if (iteration is null)
            {
                return Results.NotFound();
            }
            var name = request.Name?.Trim();
            var notes = Notes(request.Notes);
            var errors = IterationRules.Validate(name, notes);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }
            iteration.Name = name!;
            iteration.Notes = notes;
            await db.SaveChangesAsync();
            return Results.Ok(iteration);
        })
            .WithSummary("Renombra una iteración y edita sus notas")
            .WithDescription("Abierta o cerrada. No cambia nada del flujo.")
            .Produces<Iteration>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/projects/{projectId:int}/iterations/{number:int}/close",
            async (int projectId, int number, AppDbContext db) =>
        {
            var iteration = await FindAsync(db, projectId, number);
            if (iteration is null)
            {
                return Results.NotFound();
            }
            if (IterationRules.Close(iteration, await DocumentsAsync(db, iteration), DateTimeOffset.UtcNow) is { } error)
            {
                return Problem(error);
            }
            await db.SaveChangesAsync();
            return Results.Ok(iteration);
        })
            .WithSummary("Cierra la documentación de una iteración")
            .WithDescription("Desde entonces no se le suben más documentos y, si no hay otra abierta, se pueden lanzar las rondas de preguntas, las sugerencias, el catálogo y el backlog. Exige al menos un documento.")
            .Produces<Iteration>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/projects/{projectId:int}/iterations/{number:int}/reopen",
            async (int projectId, int number, AppDbContext db) =>
        {
            var iteration = await FindAsync(db, projectId, number);
            if (iteration is null)
            {
                return Results.NotFound();
            }
            var all = await db.Iterations.Where(i => i.ProjectId == projectId).ToListAsync();
            if (IterationRules.Reopen(iteration, all) is { } error)
            {
                return Problem(error);
            }
            // Reopening outdates the same as uploading a file (decision of the author, 26-10-2026).
            await Deliverables.DeliverableEndpoints.ApplyChangeAsync(db, projectId, FlowChange.FileAdded);
            await db.SaveChangesAsync();
            return Results.Ok(iteration);
        })
            .WithSummary("Reabre la última iteración")
            .WithDescription("Solo la última. Vuelve a admitir documentos y, mientras está abierta, no se lanzan las fases que leen la documentación. Deja desactualizados el catálogo, el backlog y el mockup, como subir un archivo.")
            .Produces<Iteration>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/projects/{projectId:int}/iterations/{number:int}",
            async (int projectId, int number, AppDbContext db) =>
        {
            var iteration = await FindAsync(db, projectId, number);
            if (iteration is null)
            {
                return Results.NotFound();
            }
            var all = await db.Iterations.Where(i => i.ProjectId == projectId).ToListAsync();
            if (IterationRules.Delete(iteration, all, await DocumentsAsync(db, iteration)) is { } error)
            {
                return Problem(error);
            }
            db.Iterations.Remove(iteration);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
            .WithSummary("Elimina una iteración creada por error")
            .WithDescription("Solo la última, abierta y sin documentos, y nunca la primera.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);
    }

    // The open iteration of a project, where documents are uploaded, or null.
    public static async Task<Iteration?> OpenAsync(AppDbContext db, int projectId) =>
        await db.Iterations.SingleOrDefaultAsync(i => i.ProjectId == projectId && i.Status == IterationStatus.Open);

    // At start-up: each project from before the iterations gets its
    // "Iteración 1", closed with all its documents (open if it has none;
    // IterationRules.ForExisting). The style .zip stays out of it. Only does
    // something the first time, after the iterations arrive. Only the projects
    // created before this start: a new one gets its iteration when it is
    // created, and the API may already be creating one while this runs.
    public static async Task CreateFirstForExistingAsync(AppDbContext db, DateTimeOffset start)
    {
        var projects = await db.Projects
            .Where(p => p.CreatedAt < start && !db.Iterations.Any(i => i.ProjectId == p.Id))
            .ToListAsync();
        foreach (var project in projects)
        {
            var documents = (await db.ProjectFiles.Where(f => f.ProjectId == project.Id && f.IterationId == null).ToListAsync())
                .Where(f => IterationRules.BelongsToIteration(f.FileName))
                .ToList();
            var iteration = IterationRules.ForExisting(project, documents);
            db.Iterations.Add(iteration);
            // Saved first, so the iteration has its id.
            await db.SaveChangesAsync();
            foreach (var document in documents)
            {
                document.IterationId = iteration.Id;
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task<Iteration?> FindAsync(AppDbContext db, int projectId, int number) =>
        await db.Iterations.SingleOrDefaultAsync(i => i.ProjectId == projectId && i.Number == number);

    private static async Task<int> DocumentsAsync(AppDbContext db, Iteration iteration) =>
        await db.ProjectFiles.CountAsync(f => f.IterationId == iteration.Id);

    private static string? Notes(string? notes) => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    // 400, with the same shape as the other errors so the web shows the message.
    private static IResult Problem(string error) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["iteration"] = [error] });
}
