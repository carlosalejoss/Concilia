using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Core.Deliverables;
using Concilia.Core.Flow;
using Concilia.Core.Phases;
using Concilia.Core.Questions;

namespace Concilia.Api.Deliverables;

// A deliverable as the web sees it: its content and, while it is a draft,
// what keeps it from being validated (Errors) and the warnings.
public record DeliverableView(
    int Id, DeliverableKind Kind, DeliverableStatus Status, bool Outdated, Guid JobId,
    JsonNode Content, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, DateTimeOffset? ValidatedAt);

public record SaveDeliverableRequest(JsonElement Content);

public static class DeliverableEndpoints
{
    public static void MapDeliverableEndpoints(this WebApplication app)
    {
        app.MapGet("/projects/{projectId:int}/deliverables/{kind}", async (int projectId, string kind, AppDbContext db, FileStorage storage) =>
        {
            var d = await FindAsync(db, projectId, kind);
            return d is null ? Results.NotFound() : Results.Ok(await ViewAsync(db, storage, d));
        });

        // The user's changes replace the content (normalized: ids renumbered).
        app.MapPut("/projects/{projectId:int}/deliverables/{kind}",
            async (int projectId, string kind, SaveDeliverableRequest body, AppDbContext db, FileStorage storage) =>
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
                content = Normalize(d.Kind, body.Content.GetRawText());
            }
            catch (Exception e) when (e is JsonException or InvalidOperationException or NullReferenceException)
            {
                return Problem($"El contenido no tiene la forma de un {Name(d.Kind)}: {e.Message}");
            }
            var change = DeliverableRules.Save(d, content, DateTimeOffset.UtcNow);
            if (!change.Done)
            {
                return Problem(change.Errors[0]);
            }
            await db.SaveChangesAsync();
            return Results.Ok(await ViewAsync(db, storage, d));
        });

        // Validates it if it has no errors; the later steps become outdated.
        app.MapPost("/projects/{projectId:int}/deliverables/{kind}/validate",
            async (int projectId, string kind, AppDbContext db, FileStorage storage) =>
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
            var change = DeliverableRules.Validate(d, await CheckAsync(db, storage, d), DateTimeOffset.UtcNow);
            if (!change.Done)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["deliverable"] = [.. change.Errors] });
            }
            await ApplyChangeAsync(db, projectId, change.Change!.Value);
            await db.SaveChangesAsync();
            return Results.Ok(await ViewAsync(db, storage, d));
        });

        // Back to draft; the later steps become outdated.
        app.MapPost("/projects/{projectId:int}/deliverables/{kind}/reopen",
            async (int projectId, string kind, AppDbContext db, FileStorage storage) =>
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
        });
    }

    // Called by JobWorker when a catalog or backlog job ends with a result:
    // the working copy becomes the current draft, and the later steps outdated.
    public static async Task StoreResultAsync(AppDbContext db, Guid jobId, int projectId, PhasePlan plan, PhaseJobResult result)
    {
        if (result.Source is null)
        {
            return;
        }
        var kind = plan.Kind == PhaseKind.Catalog ? DeliverableKind.Catalog : DeliverableKind.Backlog;
        var content = kind == DeliverableKind.Catalog ? CatalogDraft.FromResult(result, plan) : BacklogDraft.FromResult(result, plan);
        var current = await db.Deliverables.SingleOrDefaultAsync(d => d.ProjectId == projectId && d.Kind == kind);
        var (deliverable, change) = DeliverableRules.Generated(current, projectId, kind, jobId, content, DateTimeOffset.UtcNow);
        if (current is null)
        {
            db.Deliverables.Add(deliverable);
        }
        await ApplyChangeAsync(db, projectId, change);
        await db.SaveChangesAsync();
    }

    // Marks as outdated the deliverables of the project that the change
    // affects (the caller saves). Called on a new file, an answer changed or a
    // question discarded, and when a catalog or backlog is generated, validated
    // or reopened.
    public static async Task ApplyChangeAsync(AppDbContext db, int projectId, FlowChange change)
    {
        var all = await db.Deliverables.Where(d => d.ProjectId == projectId).ToListAsync();
        DeliverableRules.MarkOutdated(all, change);
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

    private static string Normalize(DeliverableKind kind, string content) => kind switch
    {
        DeliverableKind.Catalog => CatalogDraft.Normalize(content),
        DeliverableKind.Backlog => BacklogDraft.Normalize(content),
        _ => throw NotYet(kind),
    };

    // The mockup is a view of the analysis: it is not edited or validated, it
    // is generated again (MockupEndpoints).
    private const string MockupIsNotEdited = "El mockup no se edita ni se valida: se genera de nuevo desde el backlog validado.";

    private static NotSupportedException NotYet(DeliverableKind kind) => new($"{kind}: no tiene comprobaciones de borrador.");

    private static async Task<DeliverableView> ViewAsync(AppDbContext db, FileStorage storage, Deliverable d)
    {
        var check = d.Status == DeliverableStatus.Draft && d.Kind != DeliverableKind.Mockup
            ? await CheckAsync(db, storage, d)
            : new PhaseValidation([], []);
        return new DeliverableView(d.Id, d.Kind, d.Status, d.Outdated, d.JobId, JsonNode.Parse(d.Content)!,
            check.Errors, check.Warnings, d.CreatedAt, d.UpdatedAt, d.ValidatedAt);
    }

    private static string Name(DeliverableKind kind) => kind == DeliverableKind.Catalog ? "catálogo" : "backlog";

    private static IResult Problem(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["deliverable"] = [message] });
}
