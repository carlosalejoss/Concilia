using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Concilia.Api.Persistence;
using Concilia.Core.Runs;

namespace Concilia.Api.Runs;

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

        app.MapGet("/runs/{runId:guid}", async (Guid runId, AppDbContext db) =>
            await db.Runs.FindAsync(runId) is { } run ? Results.Ok(run) : Results.NotFound());

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

            // A finished run without events in memory or saved (it was still
            // running when the API restarted): nothing to follow.
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
