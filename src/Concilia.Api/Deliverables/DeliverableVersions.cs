using System.ComponentModel;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.OpenApi;
using Concilia.Api.Persistence;
using Concilia.Core.Deliverables;
using Concilia.Core.Phases;
using Concilia.Core.Reports;

namespace Concilia.Api.Deliverables;

// A version of a deliverable: without its content in the list, with it when
// it is read alone. Current: it is the version the deliverable has now (the
// last one with its content and job).
public record DeliverableVersionView(
    int Number, VersionOrigin Origin, DeliverableStatus Status, Guid JobId, DateTimeOffset CreatedAt,
    [property: Description("Requisitos del catálogo, historias del backlog o pantallas del mockup.")] int? Elements,
    [property: Description("Es la versión que tiene ahora el entregable.")] bool Current,
    [property: Description("Algo en lo que se basaba cambió después de generarla.")] bool Outdated,
    [property: Description("En una versión `Restored`, el número de la versión a la que se volvió.")] int? RestoredFrom,
    [property: Description("El contenido, con la forma del entregable; solo al leer una versión sola. Del mockup, su resumen.")] JsonNode? Content = null,
    [property: Description("Las iteraciones del proyecto cuya documentación incluye.")] IReadOnlyList<int>? Iterations = null);

// The history of the deliverables (step 101): each change of a catalog,
// backlog or mockup records a version (DeliverableHistory), and the
// deliverables that were there before the history get one at start-up.
public static class DeliverableVersions
{
    public static void MapDeliverableVersionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("").WithTags(ApiTags.Deliverables);

        group.MapGet("/projects/{projectId:int}/deliverables/{kind}/versions",
            async (int projectId, [Description("`catalog`, `backlog` o `mockup`.")] string kind, AppDbContext db) =>
        {
            if (!Enum.TryParse<DeliverableKind>(kind, ignoreCase: true, out var k)
                || await DeliverableEndpoints.CurrentAsync(db, projectId, k) is not { } d)
            {
                return Results.NotFound();
            }
            var versions = db.DeliverableVersions.Where(v => v.ProjectId == projectId && v.Kind == k);
            // Compared in the database, so the contents are not read.
            var current = await versions.Where(v => v.Content == d.Content && v.JobId == d.JobId).MaxAsync(v => (int?)v.Number);
            var list = await versions
                .OrderByDescending(v => v.Number)
                .Select(v => new DeliverableVersionView(v.Number, v.Origin, v.Status, v.JobId, v.CreatedAt, v.Elements, v.Number == current, v.Outdated, v.RestoredFrom, null, v.Iterations))
                .ToListAsync();
            return Results.Ok(list);
        })
            .WithSummary("Lista las versiones del catálogo, el backlog o el mockup")
            .WithDescription("De la más reciente a la más antigua, sin su contenido. Se guarda una versión cada vez que el entregable se genera (`Generated`), se guardan cambios que lo dejan distinto (`Edited`), se valida (`Validated`) o se vuelve a una versión anterior (`Restored`); al reabrirlo no, porque el contenido no cambia. `Existing` es la que tenía al empezar el historial. `current` marca la versión que tiene ahora.")
            .Produces<List<DeliverableVersionView>>()
            .Produces(StatusCodes.Status404NotFound);

        // Step 102: one version with its content, to read it.
        group.MapGet("/projects/{projectId:int}/deliverables/{kind}/versions/{number:int}",
            async (int projectId, [Description("`catalog`, `backlog` o `mockup`.")] string kind, int number, AppDbContext db) =>
        {
            if (await FindAsync(db, projectId, kind, number) is not ({ } d, { } v))
            {
                return Results.NotFound();
            }
            return Results.Ok(new DeliverableVersionView(v.Number, v.Origin, v.Status, v.JobId, v.CreatedAt, v.Elements,
                v.Content == d.Content && v.JobId == d.JobId, v.Outdated, v.RestoredFrom, JsonNode.Parse(v.Content), v.Iterations));
        })
            .WithSummary("Lee una versión del catálogo, el backlog o el mockup")
            .WithDescription("Con su contenido, para verla. La página de una versión del mockup está en `GET /projects/{projectId}/mockup/versions/{number}`.")
            .Produces<DeliverableVersionView>()
            .Produces(StatusCodes.Status404NotFound);

        // Step 102: what changed from a version to the current deliverable,
        // element by element, paired by content as in the report of a job.
        group.MapGet("/projects/{projectId:int}/deliverables/{kind}/versions/{number:int}/comparison",
            async (int projectId, [Description("`catalog` o `backlog`.")] string kind, int number, AppDbContext db) =>
        {
            if (await FindAsync(db, projectId, kind, number) is not ({ } d, { } v))
            {
                return Results.NotFound();
            }
            if (d.Kind == DeliverableKind.Mockup)
            {
                return Problem("El mockup no se compara: abre la versión para verla.");
            }
            var phase = d.Kind == DeliverableKind.Catalog ? PhaseKind.Catalog : PhaseKind.Backlog;
            return Results.Ok(JobReports.Compare(phase, v.Content, d.Content));
        })
            .WithSummary("Compara una versión con el catálogo o el backlog actual")
            .WithDescription("De la versión (`original`) a lo que hay ahora (`current`): los requisitos o historias que siguen igual se cuentan (`kept`); los que tienen el mismo id y otro contenido (`modified`) van como eran y como son; `added` son los que hay ahora y no estaban, y `deleted`, los que estaban y ya no. Se emparejan por contenido, sin ids ni campos de la plataforma (las versiones de antes de los ids estables, del paso 110, se renumeraban al guardar). Solo el catálogo y el backlog.")
            .Produces<ElementDiff>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        // Step 102: back to a version. Its content and job come back as a
        // draft, and that is a new version (Restored): what there was before
        // stays in the history, so going back can be undone.
        group.MapPost("/projects/{projectId:int}/deliverables/{kind}/versions/{number:int}/restore",
            async (int projectId, [Description("`catalog`, `backlog` o `mockup`.")] string kind, int number, AppDbContext db, FileStorage storage) =>
        {
            if (await FindAsync(db, projectId, kind, number) is not ({ } d, { } v))
            {
                return Results.NotFound();
            }
            var page = storage.MockupVersionFile(projectId, number);
            if (d.Kind == DeliverableKind.Mockup && !File.Exists(page))
            {
                return Problem("No se guardó la página de esta versión del mockup.");
            }
            var now = DateTimeOffset.UtcNow;
            var change = DeliverableRules.Restore(d, v, now);
            if (!change.Done)
            {
                return Problem(change.Errors[0]);
            }
            if (d.Kind == DeliverableKind.Mockup)
            {
                File.Copy(page, storage.MockupFile(projectId), overwrite: true);
            }
            await RecordAsync(db, storage, d, VersionOrigin.Restored, now, v.Number);
            await DeliverableEndpoints.ApplyChangeAsync(db, projectId, change.Change!.Value);
            await db.SaveChangesAsync();
            return Results.Ok(await DeliverableEndpoints.ViewAsync(db, storage, d));
        })
            .WithSummary("Vuelve a una versión anterior")
            .WithDescription("El entregable recupera el contenido de esa versión y el trabajo del que salió, siempre en borrador (aunque la versión estuviera validada, porque las respuestas pueden haber cambiado) y con su aviso de desactualizado si lo tenía. Es una versión nueva (`Restored`): la que había queda en el historial y se puede volver a ella. Los pasos siguientes quedan desactualizados, como al generarlo. Del mockup se recupera también su página.")
            .Produces<DeliverableView>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);
    }

    // The current deliverable of that kind and one of its versions, if both exist.
    private static async Task<(Deliverable Current, DeliverableVersion Version)?> FindAsync(AppDbContext db, int projectId, string kind, int number)
    {
        if (!Enum.TryParse<DeliverableKind>(kind, ignoreCase: true, out var k)
            || await DeliverableEndpoints.CurrentAsync(db, projectId, k) is not { } d)
        {
            return null;
        }
        var version = await db.DeliverableVersions.SingleOrDefaultAsync(v => v.ProjectId == projectId && v.Kind == k && v.Number == number);
        return version is null ? null : (d, version);
    }

    private static IResult Problem(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["deliverable"] = [message] });

    // Records the version of d after a change, if it is not the same as the
    // last one (the caller saves). Of a mockup, its page is copied too, so it
    // must be in place.
    public static async Task RecordAsync(AppDbContext db, FileStorage storage, Deliverable d, VersionOrigin origin, DateTimeOffset now, int? restoredFrom = null)
    {
        var last = await db.DeliverableVersions
            .Where(v => v.ProjectId == d.ProjectId && v.Kind == d.Kind)
            .OrderByDescending(v => v.Number)
            .FirstOrDefaultAsync();
        if (DeliverableHistory.Next(d, last, origin, now, restoredFrom) is { } version)
        {
            Add(db, storage, version);
        }
    }

    // Every id given in the history of a draft: those of its versions with the
    // same job (a new generation numbers from 1 again). A new element never
    // takes one of them (stable ids, step 110). Step 113: an update keeps the
    // ids of the catalog it updates, so the history goes back, through the
    // updates, to the last version generated from scratch.
    public static async Task<IReadOnlyList<string>> IdsOfJobAsync(AppDbContext db, Deliverable d)
    {
        var versions = await db.DeliverableVersions
            .Where(v => v.ProjectId == d.ProjectId && v.Kind == d.Kind)
            .OrderByDescending(v => v.Number)
            .Select(v => new { v.JobId, v.Origin, v.Content })
            .ToListAsync();
        var jobs = JobChain(versions.Select(v => (v.JobId, v.Origin)).ToList(), d.JobId);
        var contents = versions.Where(v => jobs.Contains(v.JobId)).Select(v => v.Content).ToList();
        return contents
            .SelectMany(c => d.Kind == DeliverableKind.Catalog ? CatalogDraft.Ids(c) : BacklogDraft.Ids(c))
            .Distinct()
            .ToList();
    }

    // Step 124: the job that generated a draft from scratch, the same for all
    // the updates on it. While it does not change, an id means the same thing.
    public static async Task<Guid> GenerationAsync(AppDbContext db, Deliverable d)
    {
        var versions = await db.DeliverableVersions
            .Where(v => v.ProjectId == d.ProjectId && v.Kind == d.Kind)
            .OrderByDescending(v => v.Number)
            .Select(v => new { v.JobId, v.Origin })
            .ToListAsync();
        return JobChain(versions.Select(v => (v.JobId, v.Origin)).ToList(), d.JobId)[^1];
    }

    // The job of a draft followed by those it comes from through its updates;
    // the last one generated it. versions: all of them, newest first. Each
    // version of an update adds the job of the version before it, the deliverable
    // it started from (which may be an update too).
    public static List<Guid> JobChain(IReadOnlyList<(Guid JobId, VersionOrigin Origin)> versions, Guid job)
    {
        var jobs = new List<Guid> { job };
        for (var i = 0; i < versions.Count - 1; i++)
            if (versions[i].Origin == VersionOrigin.Updated && jobs.Contains(versions[i].JobId) && !jobs.Contains(versions[i + 1].JobId))
                jobs.Add(versions[i + 1].JobId);
        return jobs;
    }

    // At start-up: each deliverable without versions gets its first one, as
    // it is now (Existing), with the date of its last change. Only does
    // something the first time, after the history arrives.
    public static async Task RecordExistingAsync(AppDbContext db, FileStorage storage)
    {
        var deliverables = await db.Deliverables
            .Where(d => !db.DeliverableVersions.Any(v => v.ProjectId == d.ProjectId && v.Kind == d.Kind))
            .ToListAsync();
        foreach (var d in deliverables)
        {
            Add(db, storage, DeliverableHistory.Next(d, null, VersionOrigin.Existing, d.UpdatedAt ?? d.CreatedAt)!);
        }
        await db.SaveChangesAsync();
    }

    private static void Add(AppDbContext db, FileStorage storage, DeliverableVersion version)
    {
        db.DeliverableVersions.Add(version);
        if (version.Kind == DeliverableKind.Mockup && File.Exists(storage.MockupFile(version.ProjectId)))
        {
            var copy = storage.MockupVersionFile(version.ProjectId, version.Number);
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(storage.MockupFile(version.ProjectId), copy, overwrite: true);
        }
    }
}
