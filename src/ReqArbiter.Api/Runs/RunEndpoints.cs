using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Api.Runs;

// A run plus the summary it produced, if any.
public record RunDetails(Run Run, string? Summary);

public static class RunEndpoints
{
    public static void MapRunEndpoints(this WebApplication app)
    {
        // Launch the summary task in the background. Answers 202 at once.
        app.MapPost("/projects/{projectId:int}/runs",
            async (int projectId, AppDbContext db, RunQueue queue, IRunnerProvider provider, RunnerOptions options) =>
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

            var run = new Run
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Provider = provider.Name,
                Model = options.Model,
                State = RunState.Queued,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Runs.Add(run);
            await db.SaveChangesAsync();
            queue.Enqueue(run.Id);

            return Results.Accepted($"/runs/{run.Id}", run);
        });

        app.MapGet("/projects/{projectId:int}/runs", async (int projectId, AppDbContext db) =>
            await db.Runs
                .Where(r => r.ProjectId == projectId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync());

        app.MapGet("/runs/{runId:guid}", async (Guid runId, AppDbContext db, FileStorage storage) =>
        {
            var run = await db.Runs.FindAsync(runId);
            if (run is null)
            {
                return Results.NotFound();
            }

            var summaryPath = Path.Combine(storage.RunFolder(runId), SummaryTask.OutputFile);
            var summary = File.Exists(summaryPath) ? await File.ReadAllTextAsync(summaryPath) : null;
            return Results.Ok(new RunDetails(run, summary));
        });

        // Server-Sent Events: one "data:" message per event, then "event: end".
        // The browser's EventSource reads this format directly.
        app.MapGet("/runs/{runId:guid}/events",
            async (Guid runId, HttpContext http, AppDbContext db, RunEventStore store, IOptions<JsonOptions> json) =>
        {
            var run = await db.Runs.FindAsync(runId);
            if (run is null)
            {
                http.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            http.Response.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";

            // A finished run whose events were lost (API restarted): nothing to follow.
            var finished = run.State is not (RunState.Queued or RunState.Running);
            if (!(finished && !store.Exists(runId)))
            {
                await foreach (var runEvent in store.Follow(runId, http.RequestAborted))
                {
                    var data = JsonSerializer.Serialize(runEvent, json.Value.SerializerOptions);
                    await http.Response.WriteAsync($"data: {data}\n\n", http.RequestAborted);
                    await http.Response.Body.FlushAsync(http.RequestAborted);
                }
            }

            await http.Response.WriteAsync("event: end\ndata: {}\n\n", http.RequestAborted);
        });
    }
}
