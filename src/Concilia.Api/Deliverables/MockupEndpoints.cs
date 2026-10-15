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
        // ?estilo=false: a design from scratch, without the style reference.
        app.MapPost("/projects/{projectId:int}/mockup-jobs",
            async (int projectId, bool? estilo, AppDbContext db, JobQueue queue, FileStorage storage, MockupOptions options,
                   [FromKeyedServices(RunnerSetup.MockupKey)] IRunnerProvider provider) =>
                await CreateJobAsync(projectId, db, queue, storage, options, provider, null, async jobId =>
                {
                    if (estilo != false)
                        await SaveStyleAsync(db, storage, projectId, jobId);
                }));

        // Resumes a failed mockup job: a new job that starts from the steps the
        // failed one finished (the design and the screens it accepted, and its
        // style reference), so only what is missing runs again. Only while the
        // validated backlog is the same.
        app.MapPost("/projects/{projectId:int}/mockup-jobs/{jobId:guid}/resume",
            async (int projectId, Guid jobId, AppDbContext db, JobQueue queue, FileStorage storage, MockupOptions options,
                   [FromKeyedServices(RunnerSetup.MockupKey)] IRunnerProvider provider) =>
        {
            var failed = await db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.ProjectId == projectId && j.Kind == JobKind);
            if (failed is null)
            {
                return Results.NotFound();
            }
            var folder = storage.JobFolder(jobId);
            if (failed.FinishedAt is null || await db.Deliverables.AnyAsync(d => d.JobId == jobId)
                || !MockupTask.IsDone(new MockupStep(MockupStage.Design), folder))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["job"] = ["Solo se puede reanudar un mockup que terminó sin generarse y que llegó a tener el diseño."],
                });
            }
            return await CreateJobAsync(projectId, db, queue, storage, options, provider, MockupPlan.Load(folder), newJobId =>
            {
                CopyFolder(MockupTask.JobPrototypeFolder(folder), MockupTask.JobPrototypeFolder(storage.JobFolder(newJobId)));
                CopyFolder(MockupTask.JobStyleFolder(folder), MockupTask.JobStyleFolder(storage.JobFolder(newJobId)));
                return Task.CompletedTask;
            });
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

    // A mockup job: its design run (lot 1, the first of the steps; the rest
    // are added when the design says how many screens there are), only when
    // the step is available (the backlog is validated). resumed: the plan of
    // the job it resumes, which must have the same backlog; prepare fills the
    // new job's folder (the style reference, or the steps of the job it resumes).
    private static async Task<IResult> CreateJobAsync(int projectId, AppDbContext db, JobQueue queue, FileStorage storage,
        MockupOptions options, IRunnerProvider provider, MockupPlan? resumed, Func<Guid, Task> prepare)
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
        if (resumed is not null && resumed.Backlog != plan.Backlog)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["job"] = ["El backlog validado ha cambiado desde ese trabajo: genera el mockup de nuevo."],
            });
        }
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
            LotNumber = 1,
        };
        plan.Save(storage.JobFolder(job.Id));
        await prepare(job.Id);
        db.Jobs.Add(job);
        db.Runs.Add(run);
        await db.SaveChangesAsync();
        queue.Enqueue(job.Id);
        return Results.Accepted($"/jobs/{job.Id}", new JobView(job, [run]));
    }

    private static void CopyFolder(string from, string to)
    {
        if (!Directory.Exists(from))
            return;
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    // The style references of the project (its .zip files), unpacked into the
    // job's folder (estilo/, one folder per .zip): the job keeps the ones
    // there were when it was created, as it keeps its backlog in the plan.
    private static async Task SaveStyleAsync(AppDbContext db, FileStorage storage, int projectId, Guid jobId)
    {
        var files = (await db.ProjectFiles.Where(f => f.ProjectId == projectId).OrderBy(f => f.UploadedAt).ToListAsync())
            .Where(f => StyleReference.IsStyleFile(f.FileName));
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var name = StyleReference.FolderName(file.FileName);
            if (!used.Add(name))
                name = $"{name}-{file.Id}";
            var zip = await File.ReadAllBytesAsync(Path.Combine(storage.ProjectFolder(projectId), file.StoredName));
            StyleReference.Extract(zip, StyleFolder(storage, jobId), name);
        }
    }

    public static string StyleFolder(FileStorage storage, Guid jobId) => MockupTask.JobStyleFolder(storage.JobFolder(jobId));

    // Called by JobWorker when the mockup job ends. A valid .html is copied to
    // the project (data/projects/{id}/mockup/mockup.html) and becomes the
    // current mockup; the job's folder keeps the original (ensamblado/). The
    // job result says how it went, as in the phases.
    public static async Task StoreResultAsync(AppDbContext db, FileStorage storage, Job job, MockupPlan plan, MockupJobResult result)
    {
        if (!result.Succeeded)
        {
            PhaseJobs.WriteResult(storage.JobFolder(job.Id), new PhaseJobResult(null, false, null,
                $"{result.Message} Se puede reanudar, aprovechando los pasos que terminaron bien, o generar de nuevo."));
            return;
        }

        var html = await File.ReadAllTextAsync(result.HtmlPath!);
        var target = storage.MockupFile(job.ProjectId);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await File.WriteAllTextAsync(target, html);

        var check = MockupTask.Validate(html, plan.StoryIds);
        var summary = new JsonObject
        {
            ["historias"] = plan.StoryIds.Count,
            ["pantallas"] = result.Screens,
            ["bytes"] = new FileInfo(target).Length,
            ["avisos"] = new JsonArray(check.Warnings.Select(w => (JsonNode)w).ToArray()),
        };
        PhaseJobs.WriteResult(storage.JobFolder(job.Id), new PhaseJobResult(Slot, false, summary.ToJsonString(),
            $"{result.Message} Con las {plan.StoryIds.Count} historias del backlog validado."));

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
