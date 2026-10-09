using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Core.Deliverables;
using ReqArbiter.Core.Flow;
using ReqArbiter.Core.Phases;
using ReqArbiter.Core.Questions;

namespace ReqArbiter.Api.Deliverables;

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
        if (result.Source is null || plan.Kind != PhaseKind.Catalog)
        {
            return;
        }
        var kind = DeliverableKind.Catalog;
        var content = CatalogDraft.FromResult(result, plan);
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
        return d.Kind == DeliverableKind.Catalog ? CatalogDraft.Check(d.Content, context) : throw NotYet(d.Kind);
    }

    private static string Normalize(DeliverableKind kind, string content) =>
        kind == DeliverableKind.Catalog ? CatalogDraft.Normalize(content) : throw NotYet(kind);

    // The backlog arrives in step 55 and the mockup in milestone 12.
    private static NotSupportedException NotYet(DeliverableKind kind) => new($"{kind}: todavía no.");

    private static async Task<DeliverableView> ViewAsync(AppDbContext db, FileStorage storage, Deliverable d)
    {
        var check = d.Status == DeliverableStatus.Draft ? await CheckAsync(db, storage, d) : new PhaseValidation([], []);
        return new DeliverableView(d.Id, d.Kind, d.Status, d.Outdated, d.JobId, JsonNode.Parse(d.Content)!,
            check.Errors, check.Warnings, d.CreatedAt, d.UpdatedAt, d.ValidatedAt);
    }

    private static string Name(DeliverableKind kind) => kind == DeliverableKind.Catalog ? "catálogo" : "backlog";

    private static IResult Problem(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["deliverable"] = [message] });
}
