using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Deliverables;
using ReqArbiter.Core.Flow;
using ReqArbiter.Core.Phases;
using ReqArbiter.Core.Questions;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Api.Projects;

public record FlowView(IReadOnlyList<StepStatus> Steps);

public static class FlowEndpoints
{
    // The step of the flow that a job kind belongs to (the catalog v1.4 by lots is not part of it).
    public static FlowStep? StepOfJob(string kind) => kind switch
    {
        "questions" => FlowStep.Questions,
        "catalog-v0" => FlowStep.Catalog,
        "backlog" => FlowStep.Backlog,
        "mockup" => FlowStep.Mockup,
        _ => null,
    };

    public static void MapFlowEndpoints(this WebApplication app)
    {
        // The state of each step of the project (ProjectFlow).
        // The documents step also says the size of the documentation, against the limit of the phases.
        app.MapGet("/projects/{projectId:int}/flow", async (int projectId, AppDbContext db, FileStorage storage, RunnerOptions options) =>
            await db.Projects.AnyAsync(p => p.Id == projectId)
                ? Results.Ok(new FlowView(ProjectFlow.Compute(
                    await ReadFactsAsync(db, storage, projectId) with { MaxDocumentationTokens = options.MaxDocumentationTokens })))
                : Results.NotFound());
    }

    // What the flow of a project depends on, read from the database and the job folders.
    public static async Task<FlowFacts> ReadFactsAsync(AppDbContext db, FileStorage storage, int projectId)
    {
        var textNames = await db.ProjectFiles
            .Where(f => f.ProjectId == projectId && f.TextStoredName != null)
            .Select(f => f.TextStoredName!)
            .ToListAsync();
        // The size of the extracted text in bytes stands in for characters
        // (a slight overestimate with accents): enough for a warning.
        var textBytes = textNames
            .Select(name => new FileInfo(Path.Combine(storage.ProjectFolder(projectId), name)))
            .Where(file => file.Exists)
            .Sum(file => file.Length);
        var jobs = await db.Jobs.Where(j => j.ProjectId == projectId).ToListAsync();
        var jobIds = jobs.Select(j => j.Id).ToList();
        // A job is in progress while some run of it is queued or running
        // (FinishedAt alone is not enough: an API restart leaves it unset).
        var busy = await db.Runs
            .Where(r => r.JobId != null && jobIds.Contains(r.JobId.Value) && (r.State == RunState.Queued || r.State == RunState.Running))
            .Select(r => r.JobId!.Value)
            .Distinct()
            .ToListAsync();
        var running = jobs.Where(j => busy.Contains(j.Id)).Select(j => StepOfJob(j.Kind)).OfType<FlowStep>().ToHashSet();

        // A round counts as finished when its job left a result (from the arbiter, or from A or B alone).
        var rounds = jobs.Count(j => j.Kind == "questions" && !busy.Contains(j.Id) && HasResult(storage.JobFolder(j.Id)));
        var statuses = await db.Questions.Where(q => q.ProjectId == projectId).Select(q => q.Status).ToListAsync();
        var deliverables = await db.Deliverables.Where(d => d.ProjectId == projectId).ToListAsync();
        DeliverableFacts? Of(DeliverableKind kind) => DeliverableRules.Facts(deliverables.SingleOrDefault(d => d.Kind == kind));

        return new FlowFacts
        {
            TextFiles = textNames.Count,
            DocumentationTokens = InputBudget.EstimateTokens(textBytes),
            FinishedQuestionRounds = rounds,
            PendingQuestions = statuses.Count(s => s == QuestionStatus.Pending),
            AnsweredQuestions = statuses.Count(s => s == QuestionStatus.Answered),
            Catalog = Of(DeliverableKind.Catalog),
            Backlog = Of(DeliverableKind.Backlog),
            Mockup = Of(DeliverableKind.Mockup),
            Running = running,
        };
    }

    private static bool HasResult(string jobFolder)
    {
        var path = Path.Combine(jobFolder, PhaseJobResult.FileName);
        return File.Exists(path) && JsonNode.Parse(File.ReadAllText(path))?["fuente"] is JsonValue;
    }
}
