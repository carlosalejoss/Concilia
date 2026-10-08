using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Core.Flow;
using ReqArbiter.Core.Phases;
using ReqArbiter.Core.Questions;
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
        // The state of each step of the project (ProjectFlow). The deliverables
        // (catalog, backlog, mockup) come in step 51: until then they count as none.
        app.MapGet("/projects/{projectId:int}/flow", async (int projectId, AppDbContext db, FileStorage storage) =>
            await db.Projects.AnyAsync(p => p.Id == projectId)
                ? Results.Ok(new FlowView(ProjectFlow.Compute(await ReadFactsAsync(db, storage, projectId))))
                : Results.NotFound());
    }

    // What the flow of a project depends on, read from the database and the job folders.
    public static async Task<FlowFacts> ReadFactsAsync(AppDbContext db, FileStorage storage, int projectId)
    {
        var textFiles = await db.ProjectFiles.CountAsync(f => f.ProjectId == projectId && f.TextStoredName != null);
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

        return new FlowFacts
        {
            TextFiles = textFiles,
            FinishedQuestionRounds = rounds,
            PendingQuestions = statuses.Count(s => s == QuestionStatus.Pending),
            AnsweredQuestions = statuses.Count(s => s == QuestionStatus.Answered),
            Running = running,
        };
    }

    private static bool HasResult(string jobFolder)
    {
        var path = Path.Combine(jobFolder, PhaseJobResult.FileName);
        return File.Exists(path) && JsonNode.Parse(File.ReadAllText(path))?["fuente"] is JsonValue;
    }
}
