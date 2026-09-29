using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Api.Runs;

// A run plus the summary it produced, if any.
public record RunDetails(Run Run, string? Summary);

public static class RunEndpoints
{
    public static void MapRunEndpoints(this WebApplication app)
    {
        // Runs are launched through jobs (POST /projects/{id}/jobs, in
        // JobEndpoints); these endpoints read single runs.
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
