using System.ComponentModel;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.OpenApi;
using Concilia.Api.Persistence;
using Concilia.Core.Phases;
using Concilia.Core.Runner;
using Concilia.Core.Runs;

namespace Concilia.Api.Runs;

// A job with all its runs: A and B for each lot, plus their retries.
public record JobView(Job Job, IReadOnlyList<Run> Runs);

public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("").WithTags(ApiTags.Jobs);

        // Launch the catalog task to model A and model B. Answers 202 at once;
        // the progress of each run is followed with GET /runs/{id}/events.
        group.MapPost("/projects/{projectId:int}/jobs",
            async (int projectId, AppDbContext db, JobQueue queue, IRunnerProvider provider,
                   RunnerOptions options, CatalogOptions catalogOptions, FileStorage storage) =>
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            {
                return Results.NotFound();
            }
            if (await ActiveJobs.RefuseIfBusyAsync(db) is { } busy)
            {
                return busy; // one task at a time in the whole platform
            }

            // The documents are the project files with extracted text, numbered
            // DOC-01, DOC-02... in upload order.
            var files = await db.ProjectFiles
                .Where(f => f.ProjectId == projectId && f.TextStoredName != null)
                .OrderBy(f => f.UploadedAt).ThenBy(f => f.Id)
                .ToListAsync();
            var documents = new List<CatalogDocument>();
            for (var i = 0; i < files.Count; i++)
            {
                var text = await File.ReadAllTextAsync(Path.Combine(storage.ProjectFolder(projectId), files[i].TextStoredName!));
                documents.Add(new CatalogDocument($"DOC-{i + 1:00}", files[i].FileName, text));
            }

            var plan = CatalogTask.Plan(documents, catalogOptions);
            if (plan.Lots.Count == 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["project"] = ["El proyecto no tiene archivos con texto. Sube algún documento con texto."],
                });
            }

            var now = DateTimeOffset.UtcNow;
            var job = new Job { Id = Guid.NewGuid(), ProjectId = projectId, Kind = CatalogTask.Kind, CreatedAt = now };
            CatalogTask.SavePlan(Path.Combine(storage.JobFolder(job.Id), CatalogTask.PlanFile), plan);

            // One run per slot and lot.
            Run NewRun(string slot, string model, LotPlan lot) => new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                JobId = job.Id,
                Slot = slot,
                LotNumber = lot.Number,
                LotTotal = lot.Total,
                Provider = provider.Name,
                Model = model,
                State = RunState.Queued,
                CreatedAt = now,
            };
            var runs = plan.Lots
                .SelectMany(lot => new[] { NewRun("A", options.ModelA, lot), NewRun("B", options.ModelB, lot) })
                .ToList();

            db.Jobs.Add(job);
            db.Runs.AddRange(runs);
            await db.SaveChangesAsync();
            queue.Enqueue(job.Id);

            return Results.Accepted($"/jobs/{job.Id}", new JobView(job, runs));
        })
            .WithSummary("Lanza el catálogo v1.4 por lotes (experimental)")
            .WithDescription("El catálogo del hito 5 y 6, anterior al flujo v0: la documentación se divide en lotes de secciones y A y B escriben el catálogo de cada lote. No forma parte del flujo; su resultado se lee en `GET /jobs/{jobId}/catalogs/{slot}`.")
            .LaunchesJob();

        // The result of a v0 phase job: which output counts (the arbiter's, or
        // A's or B's without cross-check) and the output itself.
        group.MapGet("/jobs/{jobId:guid}/result", (Guid jobId, FileStorage storage) =>
        {
            var path = Path.Combine(storage.JobFolder(jobId), PhaseJobResult.FileName);
            return File.Exists(path)
                ? Results.Text(File.ReadAllText(path), "application/json")
                : Results.NotFound();
        })
            .WithSummary("Resultado de un trabajo de fase")
            .WithDescription("El resultado congelado del trabajo (`resultado-arbitro.json`): `fuente` (`ARB`, o nulo si no hubo resultado), `verificacion_cruzada`, `salida` (la salida del árbitro tal como la escribió), `mensaje`, `desviaciones` del árbitro respecto al emparejamiento y el tiempo del `emparejamiento`. `404` hasta que el trabajo termina.")
            .Produces<JsonNode>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/projects/{projectId:int}/jobs", async (int projectId, AppDbContext db) =>
        {
            var jobs = await db.Jobs
                .Where(j => j.ProjectId == projectId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
            var jobIds = jobs.Select(j => j.Id).ToList();
            var runs = await db.Runs
                .Where(r => r.JobId != null && jobIds.Contains(r.JobId.Value))
                .OrderBy(r => r.Slot).ThenBy(r => r.LotNumber).ThenBy(r => r.Attempt)
                .ToListAsync();

            return jobs.Select(j => new JobView(j, runs.Where(r => r.JobId == j.Id).ToList())).ToList();
        })
            .WithSummary("Lista los trabajos del proyecto")
            .WithDescription("Del más reciente al más antiguo, cada uno con sus ejecuciones ordenadas por lado (`A`, `B`, `ARB`), lote e intento.");

        group.MapGet("/jobs/{jobId:guid}", async (Guid jobId, AppDbContext db) =>
        {
            var job = await db.Jobs.FindAsync(jobId);
            if (job is null)
            {
                return Results.NotFound();
            }

            var runs = await db.Runs
                .Where(r => r.JobId == jobId)
                .OrderBy(r => r.Slot).ThenBy(r => r.LotNumber).ThenBy(r => r.Attempt)
                .ToListAsync();
            return Results.Ok(new JobView(job, runs));
        })
            .WithSummary("Un trabajo con sus ejecuciones")
            .WithDescription("Para seguir su avance: el trabajo termina cuando tiene `finishedAt`, y cada ejecución dice su estado (`Queued`, `Running`, `Done`, `Failed`…).")
            .Produces<JobView>()
            .Produces(StatusCodes.Status404NotFound);

        // The merged catalog of model A or B, with its validator report.
        group.MapGet("/jobs/{jobId:guid}/catalogs/{slot}", async (Guid jobId, [Description("`A` o `B`.")] string slot, AppDbContext db, FileStorage storage) =>
        {
            var job = await db.Jobs.FindAsync(jobId);
            var planPath = Path.Combine(storage.JobFolder(jobId), CatalogTask.PlanFile);
            if (job is null || job.Kind != CatalogTask.Kind || slot is not ("A" or "B") || !File.Exists(planPath))
            {
                return Results.NotFound();
            }

            var project = await db.Projects.FindAsync(job.ProjectId);
            var runs = await db.Runs.Where(r => r.JobId == jobId).ToListAsync();
            return Results.Ok(JobCatalog.Build(slot, runs, project!.SystemName, CatalogTask.LoadPlan(planPath), storage, jobId));
        })
            .WithSummary("Catálogo v1.4 de un modelo (experimental)")
            .WithDescription("La unión de los lotes terminados de A o de B en un trabajo del catálogo v1.4, renumerada y con el informe del validador.")
            .Produces<SlotCatalog>()
            .Produces(StatusCodes.Status404NotFound);
    }
}
