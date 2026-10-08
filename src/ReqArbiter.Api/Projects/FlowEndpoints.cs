using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Flow;
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
        // The state of each step of the project (ProjectFlow). The questions
        // and the deliverables come in steps 46 and 51: until then they count
        // as none.
        app.MapGet("/projects/{projectId:int}/flow", async (int projectId, AppDbContext db, FileStorage storage) =>
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            {
                return Results.NotFound();
            }

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

            var facts = new FlowFacts { TextFiles = textFiles, FinishedQuestionRounds = rounds, Running = running };
            return Results.Ok(new FlowView(ProjectFlow.Compute(facts)));
        });
    }

    private static bool HasResult(string jobFolder)
    {
        var path = Path.Combine(jobFolder, PhaseJobResult.FileName);
        return File.Exists(path) && JsonNode.Parse(File.ReadAllText(path))?["fuente"] is JsonValue;
    }
}
