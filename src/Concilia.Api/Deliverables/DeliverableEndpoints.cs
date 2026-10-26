using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.OpenApi;
using Concilia.Api.Persistence;
using Concilia.Core.Deliverables;
using Concilia.Core.Flow;
using Concilia.Core.Phases;
using Concilia.Core.Questions;

namespace Concilia.Api.Deliverables;

// A deliverable as the web sees it: its content and, while it is a draft,
// what keeps it from being validated (Errors) and the warnings.
// AssumedQuestionIds (step 93, catalog only): the questions answered with a
// guess of the AI taken as it was; the requirements that cite them are marked.
public record DeliverableView(
    int Id, DeliverableKind Kind, DeliverableStatus Status, bool Outdated, Guid JobId,
    JsonNode Content, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, DateTimeOffset? ValidatedAt,
    IReadOnlyList<string>? AssumedQuestionIds = null,
    [property: Description("Las iteraciones del proyecto cuya documentación incluye (las que leyó su trabajo; el mockup, las de su backlog).")] IReadOnlyList<int>? Iterations = null,
    [property: Description("Las iteraciones cerradas con documentos que no incluye.")] IReadOnlyList<int>? MissingIterations = null);

public record SaveDeliverableRequest([property: Description("El catálogo o el backlog entero, con la forma de su esquema.")] JsonElement Content);

public static class DeliverableEndpoints
{
    public static void MapDeliverableEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("").WithTags(ApiTags.Deliverables);

        group.MapGet("/projects/{projectId:int}/deliverables/{kind}", async (int projectId, [Description("`catalog`, `backlog` o `mockup`.")] string kind, AppDbContext db, FileStorage storage) =>
        {
            var d = await FindAsync(db, projectId, kind);
            return d is null ? Results.NotFound() : Results.Ok(await ViewAsync(db, storage, d));
        })
            .WithSummary("Lee el catálogo, el backlog o el mockup del proyecto")
            .WithDescription("La versión actual, validada o en borrador. Si es un borrador, `errors` dice lo que impide validarlo y `warnings` los avisos. En el catálogo, `assumedQuestionIds` son las preguntas respondidas con una suposición de la IA aceptada tal cual: los requisitos que las citan se marcan. El contenido del mockup es su resumen (historias, pantallas, tamaño y avisos); la página está en `GET /projects/{projectId}/mockup`.")
            .Produces<DeliverableView>()
            .Produces(StatusCodes.Status404NotFound);

        // The user's changes replace the content, with stable ids (step 110).
        group.MapPut("/projects/{projectId:int}/deliverables/{kind}",
            async (int projectId, [Description("`catalog`, `backlog` o `mockup`.")] string kind, SaveDeliverableRequest body, AppDbContext db, FileStorage storage) =>
        {
            var d = await FindAsync(db, projectId, kind);
            if (d is null)
            {
                return Results.NotFound();
            }
            if (d.Kind == DeliverableKind.Mockup)
            {
                return Problem(MockupIsNotEdited);
            }
            string content;
            try
            {
                content = Normalize(d, body.Content.GetRawText(), await DeliverableVersions.IdsOfJobAsync(db, d));
            }
            catch (Exception e) when (e is JsonException or InvalidOperationException or NullReferenceException)
            {
                return Problem($"El contenido no tiene la forma de un {Name(d.Kind)}: {e.Message}");
            }
            var now = DateTimeOffset.UtcNow;
            var change = DeliverableRules.Save(d, content, now);
            if (!change.Done)
            {
                return Problem(change.Errors[0]);
            }
            await DeliverableVersions.RecordAsync(db, storage, d, VersionOrigin.Edited, now);
            await db.SaveChangesAsync();
            return Results.Ok(await ViewAsync(db, storage, d));
        })
            .WithSummary("Guarda los cambios del usuario")
            .WithDescription("El contenido nuevo sustituye al anterior. Los ids son estables: cada elemento conserva el que tenía; uno nuevo (o un requisito que cambia de tipo) toma el siguiente número de su tipo que no se haya dado antes en el historial del borrador, y borrar deja un hueco. Solo el catálogo y el backlog, y solo en borrador: el mockup no se edita.")
            .ChangesDeliverable();

        // Validates it if it has no errors; the later steps become outdated.
        group.MapPost("/projects/{projectId:int}/deliverables/{kind}/validate",
            async (int projectId, [Description("`catalog`, `backlog` o `mockup`.")] string kind, AppDbContext db, FileStorage storage) =>
        {
            var d = await FindAsync(db, projectId, kind);
            if (d is null)
            {
                return Results.NotFound();
            }
            if (d.Kind == DeliverableKind.Mockup)
            {
                return Problem(MockupIsNotEdited);
            }
            var now = DateTimeOffset.UtcNow;
            var change = DeliverableRules.Validate(d, await CheckAsync(db, storage, d), now);
            if (!change.Done)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["deliverable"] = [.. change.Errors] });
            }
            await DeliverableVersions.RecordAsync(db, storage, d, VersionOrigin.Validated, now);
            await ApplyChangeAsync(db, projectId, change.Change!.Value);
            await db.SaveChangesAsync();
            return Results.Ok(await ViewAsync(db, storage, d));
        })
            .WithSummary("Valida el catálogo o el backlog")
            .WithDescription("Solo si el borrador no tiene errores (si los tiene, `400` con todos ellos). Las fases siguientes solo ven la versión validada; los pasos posteriores quedan desactualizados.")
            .ChangesDeliverable();

        // Back to draft; the later steps become outdated.
        group.MapPost("/projects/{projectId:int}/deliverables/{kind}/reopen",
            async (int projectId, [Description("`catalog`, `backlog` o `mockup`.")] string kind, AppDbContext db, FileStorage storage) =>
        {
            var d = await FindAsync(db, projectId, kind);
            if (d is null)
            {
                return Results.NotFound();
            }
            if (d.Kind == DeliverableKind.Mockup)
            {
                return Problem(MockupIsNotEdited);
            }
            var change = DeliverableRules.Reopen(d, DateTimeOffset.UtcNow);
            if (!change.Done)
            {
                return Problem(change.Errors[0]);
            }
            await ApplyChangeAsync(db, projectId, change.Change!.Value);
            await db.SaveChangesAsync();
            return Results.Ok(await ViewAsync(db, storage, d));
        })
            .WithSummary("Vuelve a dejar en borrador el catálogo o el backlog")
            .WithDescription("Para editarlo otra vez; los pasos posteriores quedan desactualizados.")
            .ChangesDeliverable();
    }

    // The answers of the endpoints that change a deliverable, for the documentation.
    private static RouteHandlerBuilder ChangesDeliverable(this RouteHandlerBuilder endpoint) => endpoint
        .Produces<DeliverableView>()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);

    // Called by JobWorker when a catalog or backlog job ends with a result:
    // the working copy becomes the current draft (a new version), and the
    // later steps outdated.
    public static async Task StoreResultAsync(AppDbContext db, FileStorage storage, Guid jobId, int projectId, PhasePlan plan, PhaseJobResult result)
    {
        if (result.Source is null)
        {
            return;
        }
        var kind = plan.Kind == PhaseKind.Catalog ? DeliverableKind.Catalog : DeliverableKind.Backlog;
        var content = kind == DeliverableKind.Catalog ? CatalogDraft.FromResult(result, plan) : BacklogDraft.FromResult(result, plan);
        var current = await db.Deliverables.SingleOrDefaultAsync(d => d.ProjectId == projectId && d.Kind == kind);
        var now = DateTimeOffset.UtcNow;
        // Step 111: the iterations whose documentation the job read.
        var iterations = ProjectIterations.OfPlan(plan, await ProjectIterations.DocumentsAsync(db, projectId));
        var (deliverable, change) = DeliverableRules.Generated(current, projectId, kind, jobId, content, now, iterations);
        if (current is null)
        {
            db.Deliverables.Add(deliverable);
        }
        await DeliverableVersions.RecordAsync(db, storage, deliverable, VersionOrigin.Generated, now);
        await ApplyChangeAsync(db, projectId, change);
        await db.SaveChangesAsync();
    }

    // Marks as outdated the deliverables of the project that the change
    // affects, and their versions (the caller saves). Called on a new file, an
    // answer changed or a question discarded, and when a catalog or backlog is
    // generated, validated, reopened or taken back to a version.
    public static async Task ApplyChangeAsync(AppDbContext db, int projectId, FlowChange change)
    {
        var all = await db.Deliverables.Where(d => d.ProjectId == projectId).ToListAsync();
        // Only the versions not yet outdated: after the first change, few or none.
        var kinds = DeliverableRules.OutdatedKinds(change);
        var versions = await db.DeliverableVersions
            .Where(v => v.ProjectId == projectId && kinds.Contains(v.Kind) && !v.Outdated)
            .ToListAsync();
        DeliverableRules.MarkOutdated(all, change, versions);
    }

    public static async Task<Deliverable?> CurrentAsync(AppDbContext db, int projectId, DeliverableKind kind) =>
        await db.Deliverables.SingleOrDefaultAsync(d => d.ProjectId == projectId && d.Kind == kind);

    private static async Task<Deliverable?> FindAsync(AppDbContext db, int projectId, string kind) =>
        Enum.TryParse<DeliverableKind>(kind, ignoreCase: true, out var k) ? await CurrentAsync(db, projectId, k) : null;

    // The checks of a draft: those of its kind, with the plan of the job that
    // generated it and the questions answered now.
    private static async Task<PhaseValidation> CheckAsync(AppDbContext db, FileStorage storage, Deliverable d)
    {
        var plan = PhasePlan.Load(storage.JobFolder(d.JobId));
        var answered = await db.Questions
            .Where(q => q.ProjectId == d.ProjectId && q.Status == QuestionStatus.Answered)
            .Select(q => q.Code)
            .ToListAsync();
        var context = plan.Context() with { AnsweredQuestionIds = answered.ToHashSet() };
        return d.Kind switch
        {
            DeliverableKind.Catalog => CatalogDraft.Check(d.Content, context),
            // The backlog is checked against the validated catalog it was generated from (in the plan).
            DeliverableKind.Backlog => BacklogDraft.Check(d.Content, context),
            _ => throw NotYet(d.Kind),
        };
    }

    // The user's changes over the current draft, with stable ids (step 110).
    private static string Normalize(Deliverable d, string content, IReadOnlyList<string> used) => d.Kind switch
    {
        DeliverableKind.Catalog => CatalogDraft.Normalize(content, d.Content, used),
        DeliverableKind.Backlog => BacklogDraft.Normalize(content, d.Content, used),
        _ => throw NotYet(d.Kind),
    };

    // The mockup is a view of the analysis: it is not edited or validated, it
    // is generated again (MockupEndpoints).
    private const string MockupIsNotEdited = "El mockup no se edita ni se valida: se genera de nuevo desde el backlog validado.";

    private static NotSupportedException NotYet(DeliverableKind kind) => new($"{kind}: no tiene comprobaciones de borrador.");

    internal static async Task<DeliverableView> ViewAsync(AppDbContext db, FileStorage storage, Deliverable d)
    {
        var check = d.Status == DeliverableStatus.Draft && d.Kind != DeliverableKind.Mockup
            ? await CheckAsync(db, storage, d)
            : new PhaseValidation([], []);
        IReadOnlyList<string>? assumed = null;
        if (d.Kind == DeliverableKind.Catalog)
        {
            var answered = await db.Questions.Where(q => q.ProjectId == d.ProjectId && q.Status == QuestionStatus.Answered).ToListAsync();
            assumed = answered.Where(QuestionRules.IsAcceptedGuess).Select(q => q.Code).Order().ToList();
        }
        return new DeliverableView(d.Id, d.Kind, d.Status, d.Outdated, d.JobId, JsonNode.Parse(d.Content)!,
            check.Errors, check.Warnings, d.CreatedAt, d.UpdatedAt, d.ValidatedAt, assumed,
            d.Iterations, await ProjectIterations.MissingAsync(db, d));
    }

    private static string Name(DeliverableKind kind) => kind == DeliverableKind.Catalog ? "catálogo" : "backlog";

    private static IResult Problem(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["deliverable"] = [message] });
}
