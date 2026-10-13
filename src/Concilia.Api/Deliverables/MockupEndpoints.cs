using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Api.Projects;
using Concilia.Api.Runs;
using Concilia.Core.Deliverables;
using Concilia.Core.Flow;
using Concilia.Core.Mockup;
using Concilia.Core.Phases;
using Concilia.Core.Runner;
using Concilia.Core.Runs;

namespace Concilia.Api.Deliverables;

// The mockup (milestone 12): one model writes a self-contained .html from the
// validated backlog. It is not edited or validated: it is a view of the
// analysis, and it is generated again when the backlog changes.
public static class MockupEndpoints
{
    public const string JobKind = "mockup";
    public const string Slot = "MOCK";

    // The page cannot reach the network or the application even if it is
    // opened directly (not only inside the sandboxed iframe of the web):
    // scripts and styles of its own, images and fonts only as data: URIs.
    private const string ContentSecurityPolicy =
        "sandbox allow-scripts; default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src data:; font-src data:";

    public static void MapMockupEndpoints(this WebApplication app)
    {
        // A mockup job: a single run (MOCK) with the mockup provider and model,
        // only when the step is available (the backlog is validated).
        app.MapPost("/projects/{projectId:int}/mockup-jobs",
            async (int projectId, AppDbContext db, JobQueue queue, FileStorage storage, MockupOptions options,
                   [FromKeyedServices(RunnerSetup.MockupKey)] IRunnerProvider provider) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null)
            {
                return Results.NotFound();
            }
            var step = ProjectFlow.Compute(await FlowEndpoints.ReadFactsAsync(db, storage, projectId)).Single(s => s.Step == FlowStep.Mockup);
            if (!step.CanLaunch)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["project"] = [step.Reason!] });
            }

            var backlog = (await DeliverableEndpoints.CurrentAsync(db, projectId, DeliverableKind.Backlog))!;
            var plan = MockupPlan.From(project.SystemName, project.Language, BacklogDraft.ForNextPhase(backlog.Content));
            var now = DateTimeOffset.UtcNow;
            var job = new Job { Id = Guid.NewGuid(), ProjectId = projectId, Kind = JobKind, CreatedAt = now };
            var run = new Run
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                JobId = job.Id,
                Slot = Slot,
                Provider = provider.Name,
                Model = options.Model,
                State = RunState.Queued,
                CreatedAt = now,
                PromptVersion = PhaseExecutor.PromptVersion,
            };
            plan.Save(storage.JobFolder(job.Id));
            db.Jobs.Add(job);
            db.Runs.Add(run);
            await db.SaveChangesAsync();
            queue.Enqueue(job.Id);
            return Results.Accepted($"/jobs/{job.Id}", new JobView(job, [run]));
        });

        // The current mockup as a page (for the iframe) or, with ?download=true,
        // as a file to save and open with a double click.
        app.MapGet("/projects/{projectId:int}/mockup", async (int projectId, bool? download, AppDbContext db, FileStorage storage, HttpContext http) =>
        {
            var path = storage.MockupFile(projectId);
            if (await DeliverableEndpoints.CurrentAsync(db, projectId, DeliverableKind.Mockup) is null || !File.Exists(path))
            {
                return Results.NotFound();
            }
            http.Response.Headers.ContentSecurityPolicy = ContentSecurityPolicy;
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(path, "text/html; charset=utf-8", download == true ? $"mockup-proyecto-{projectId}.html" : null);
        });
    }

    // Called by JobWorker when the mockup run ends. A valid .html is copied to
    // the project (data/projects/{id}/mockup/mockup.html) and becomes the
    // current mockup; the run folder keeps the original. The job result says
    // how it went, as in the phases.
    public static async Task StoreResultAsync(AppDbContext db, FileStorage storage, Job job, MockupPlan plan, RunOutcome outcome)
    {
        if (!outcome.Succeeded)
        {
            PhaseJobs.WriteResult(storage.JobFolder(job.Id), new PhaseJobResult(null, false, null,
                "El modelo no terminó con un mockup válido; se puede generar de nuevo."));
            return;
        }

        var attempt = outcome.Attempts![^1];
        var html = await File.ReadAllTextAsync(MockupExecutor.OutputPath(attempt));
        var target = storage.MockupFile(job.ProjectId);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await File.WriteAllTextAsync(target, html);

        var check = MockupTask.Validate(html, plan.StoryIds);
        var summary = new JsonObject
        {
            ["historias"] = plan.StoryIds.Count,
            ["bytes"] = new FileInfo(target).Length,
            ["avisos"] = new JsonArray(check.Warnings.Select(w => (JsonNode)w).ToArray()),
        };
        PhaseJobs.WriteResult(storage.JobFolder(job.Id), new PhaseJobResult(Slot, false, summary.ToJsonString(),
            $"Mockup generado con las {plan.StoryIds.Count} historias del backlog validado."));

        var current = await DeliverableEndpoints.CurrentAsync(db, job.ProjectId, DeliverableKind.Mockup);
        var (deliverable, change) = DeliverableRules.Generated(current, job.ProjectId, DeliverableKind.Mockup, job.Id, summary.ToJsonString(), DateTimeOffset.UtcNow);
        if (current is null)
        {
            db.Deliverables.Add(deliverable);
        }
        await DeliverableEndpoints.ApplyChangeAsync(db, job.ProjectId, change);
        await db.SaveChangesAsync();
    }
}
