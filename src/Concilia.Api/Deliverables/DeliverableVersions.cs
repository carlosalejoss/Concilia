using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.OpenApi;
using Concilia.Api.Persistence;
using Concilia.Core.Deliverables;

namespace Concilia.Api.Deliverables;

// A version in the list of a deliverable, without its content. Current: it is
// the version the deliverable has now (the last one with its content and job).
public record DeliverableVersionView(
    int Number, VersionOrigin Origin, DeliverableStatus Status, Guid JobId, DateTimeOffset CreatedAt,
    [property: Description("Requisitos del catálogo, historias del backlog o pantallas del mockup.")] int? Elements,
    [property: Description("Es la versión que tiene ahora el entregable.")] bool Current);

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
                .Select(v => new DeliverableVersionView(v.Number, v.Origin, v.Status, v.JobId, v.CreatedAt, v.Elements, v.Number == current))
                .ToListAsync();
            return Results.Ok(list);
        })
            .WithSummary("Lista las versiones del catálogo, el backlog o el mockup")
            .WithDescription("De la más reciente a la más antigua, sin su contenido. Se guarda una versión cada vez que el entregable se genera (`Generated`), se guardan cambios que lo dejan distinto (`Edited`) o se valida (`Validated`); al reabrirlo no, porque el contenido no cambia. `Existing` es la que tenía al empezar el historial. `current` marca la versión que tiene ahora.")
            .Produces<List<DeliverableVersionView>>()
            .Produces(StatusCodes.Status404NotFound);
    }

    // Records the version of d after a change, if it is not the same as the
    // last one (the caller saves). Of a mockup, its page is copied too, so it
    // must be in place.
    public static async Task RecordAsync(AppDbContext db, FileStorage storage, Deliverable d, VersionOrigin origin, DateTimeOffset now)
    {
        var last = await db.DeliverableVersions
            .Where(v => v.ProjectId == d.ProjectId && v.Kind == d.Kind)
            .OrderByDescending(v => v.Number)
            .FirstOrDefaultAsync();
        if (DeliverableHistory.Next(d, last, origin, now) is { } version)
        {
            Add(db, storage, version);
        }
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
