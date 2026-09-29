using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Api.Runs;

// A job with all its runs: A and B, plus their retries.
public record JobView(Job Job, IReadOnlyList<Run> Runs);

public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        // Launch the summary task to model A and model B. Answers 202 at once;
        // the progress of each run is followed with GET /runs/{id}/events.
        app.MapPost("/projects/{projectId:int}/jobs",
            async (int projectId, AppDbContext db, JobQueue queue, IRunnerProvider provider, RunnerOptions options) =>
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            {
                return Results.NotFound();
            }

            if (!await db.ProjectFiles.AnyAsync(f => f.ProjectId == projectId && f.TextStoredName != null))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["project"] = ["El proyecto no tiene archivos con texto. Sube algún .txt, .md, .docx o .pdf."],
                });
            }

            var now = DateTimeOffset.UtcNow;
            var job = new Job { Id = Guid.NewGuid(), ProjectId = projectId, Kind = "summary", CreatedAt = now };

            Run NewRun(string slot, string model) => new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                JobId = job.Id,
                Slot = slot,
                Provider = provider.Name,
                Model = model,
                State = RunState.Queued,
                CreatedAt = now,
            };
            var runs = new List<Run> { NewRun("A", options.ModelA), NewRun("B", options.ModelB) };

            db.Jobs.Add(job);
            db.Runs.AddRange(runs);
            await db.SaveChangesAsync();
            queue.Enqueue(job.Id);

            return Results.Accepted($"/jobs/{job.Id}", new JobView(job, runs));
        });

        app.MapGet("/projects/{projectId:int}/jobs", async (int projectId, AppDbContext db) =>
        {
            var jobs = await db.Jobs
                .Where(j => j.ProjectId == projectId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
            var jobIds = jobs.Select(j => j.Id).ToList();
            var runs = await db.Runs
                .Where(r => r.JobId != null && jobIds.Contains(r.JobId.Value))
                .OrderBy(r => r.Slot).ThenBy(r => r.Attempt)
                .ToListAsync();

            return jobs.Select(j => new JobView(j, runs.Where(r => r.JobId == j.Id).ToList())).ToList();
        });

        app.MapGet("/jobs/{jobId:guid}", async (Guid jobId, AppDbContext db) =>
        {
            var job = await db.Jobs.FindAsync(jobId);
            if (job is null)
            {
                return Results.NotFound();
            }

            var runs = await db.Runs
                .Where(r => r.JobId == jobId)
                .OrderBy(r => r.Slot).ThenBy(r => r.Attempt)
                .ToListAsync();
            return Results.Ok(new JobView(job, runs));
        });
    }
}
