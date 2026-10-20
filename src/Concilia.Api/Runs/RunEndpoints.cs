using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Concilia.Api.OpenApi;
using Concilia.Api.Persistence;
using Concilia.Core.Runs;

namespace Concilia.Api.Runs;

public static class RunEndpoints
{
    public static void MapRunEndpoints(this WebApplication app)
    {
        // Runs are launched through jobs (POST /projects/{id}/jobs, in
        // JobEndpoints); these endpoints read single runs.
        var group = app.MapGroup("").WithTags(ApiTags.Runs);

        group.MapGet("/projects/{projectId:int}/runs", async (int projectId, AppDbContext db) =>
            await db.Runs
                .Where(r => r.ProjectId == projectId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync())
            .WithSummary("Lista las ejecuciones del proyecto")
            .WithDescription("De la más reciente a la más antigua, de todos sus trabajos.");

        group.MapGet("/runs/{runId:guid}", async (Guid runId, AppDbContext db) =>
            await db.Runs.FindAsync(runId) is { } run ? Results.Ok(run) : Results.NotFound())
            .WithSummary("Una ejecución")
            .WithDescription("Un modelo sobre una tarea: proveedor, modelo, estado, veredicto, tokens, turnos y error, si lo hubo. Nunca guarda claves ni el coste que da Claude Code.")
            .Produces<Run>()
            .Produces(StatusCodes.Status404NotFound);

        // Server-Sent Events: one "data:" message per event, then "event: end".
        // The browser's EventSource reads this format directly.
        group.MapGet("/runs/{runId:guid}/events",
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
        })
            .WithSummary("Consola de una ejecución en directo (SSE)")
            .WithDescription("Server-Sent Events: un mensaje `data:` por evento de Claude Code (texto, llamadas a herramientas, avisos…) y `event: end` al terminar. Una ejecución terminada repite su consola guardada. Se lee con `EventSource` en el navegador; Swagger UI espera a que termine para mostrarla.")
            .Produces<string>(contentType: "text/event-stream")
            .Produces(StatusCodes.Status404NotFound);
    }
}
