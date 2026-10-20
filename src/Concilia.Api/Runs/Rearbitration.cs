using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.OpenApi;
using Concilia.Api.Persistence;
using Concilia.Core.Phases;
using Concilia.Core.Runner;
using Concilia.Core.Runs;

namespace Concilia.Api.Runs;

// A new arbitration of a finished job (16-10-2026, autor): the same proposals
// of A and B, judged again by the arbiter, with the model the user chooses.
// It answers whether the arbiter prefers the proposal of its own model (SOL is
// both A and the arbiter): only the arbiter changes, so any change in what the
// union takes from A and from B comes from it.
// It is an experiment: it leaves its result and its report, and does not
// touch the questions, the catalog or the backlog of the project.
public static class Rearbitration
{
    public const string JobKind = "rearbitration";

    // The proposals of A and B, copied into the folder of the new job: the job
    // does not depend on the runs of the original one, and the report counts
    // them from here.
    public static string ProposalFile(string slot) => $"propuesta-{slot}.json";

    public record RearbitrationRequest([property: Description("El modelo del árbitro, uno de `GET /jobs/arbiter-models`.")] string Model);

    public static void MapRearbitrationEndpoints(this WebApplication app)
    {
        // The models that can be chosen as the arbiter: those configured for
        // A, B and the arbiter (the ids go to the gateway as they are).
        var group = app.MapGroup("").WithTags(ApiTags.Jobs);

        group.MapGet("/jobs/arbiter-models", (RunnerOptions options) => Results.Ok(Models(options)))
            .WithSummary("Modelos que pueden hacer de árbitro")
            .WithDescription("Los configurados para A, B y el árbitro, sin repetir; los ids van a la pasarela tal cual.")
            .Produces<IReadOnlyList<string>>();

        group.MapPost("/jobs/{jobId:guid}/rearbitrate",
            async (Guid jobId, RearbitrationRequest body, AppDbContext db, JobQueue queue, IRunnerProvider provider, RunnerOptions options, FileStorage storage) =>
        {
            var source = await db.Jobs.FindAsync(jobId);
            if (source is null)
            {
                return Results.NotFound();
            }
            var phase = PhasePlan.KindOfJob(source.Kind);
            if (phase is not (PhaseKind.Questions or PhaseKind.Catalog or PhaseKind.Backlog))
            {
                return Refuse("Solo se puede volver a arbitrar una ronda de preguntas, un catálogo o un backlog.");
            }
            if (await ActiveJobs.RefuseIfBusyAsync(db) is { } busy)
            {
                return busy; // one task at a time in the whole platform
            }
            if (!Models(options).Contains(body.Model))
            {
                return Refuse($"Modelo desconocido: «{body.Model}». Elige uno de: {string.Join(", ", Models(options))}.");
            }

            // The last attempt of A and of B must have ended with a valid output.
            var plan = PhasePlan.Load(storage.JobFolder(jobId));
            var outputFile = PhaseDefinition.For(plan.Kind, PhaseRole.Proposal).OutputFile;
            var runs = await db.Runs.Where(r => r.JobId == jobId).ToListAsync();
            var outputs = new Dictionary<string, string>();
            foreach (var slot in new[] { "A", "B" })
            {
                var last = runs.Where(r => r.Slot == slot).OrderBy(r => r.Attempt).LastOrDefault();
                var path = last is null ? null : Path.Combine(storage.RunFolder(last.Id), outputFile);
                if (last?.State != RunState.Done || !File.Exists(path))
                {
                    return Refuse($"El trabajo no tiene una propuesta válida de {slot}: no hay nada que volver a arbitrar.");
                }
                outputs[slot] = path;
            }

            // The same plan with the order of the proposals drawn again, as in
            // any job, so the arbiter still cannot tell which one is A.
            var now = DateTimeOffset.UtcNow;
            var job = new Job { Id = Guid.NewGuid(), ProjectId = source.ProjectId, Kind = JobKind, CreatedAt = now };
            var folder = storage.JobFolder(job.Id);
            (plan with { ProposalOrder = PhasePlan.RandomOrder(Random.Shared), RearbitrationOf = jobId }).Save(folder);
            foreach (var (slot, path) in outputs)
            {
                File.Copy(path, Path.Combine(folder, ProposalFile(slot)));
            }

            var arbiter = new Run
            {
                Id = Guid.NewGuid(),
                ProjectId = source.ProjectId,
                JobId = job.Id,
                Slot = "ARB",
                Provider = provider.Name,
                Model = body.Model,
                State = RunState.Queued,
                CreatedAt = now,
                PromptVersion = PhaseExecutor.PromptVersion,
            };
            db.Jobs.Add(job);
            db.Runs.Add(arbiter);
            await db.SaveChangesAsync();
            queue.Enqueue(job.Id);
            return Results.Accepted($"/jobs/{job.Id}", new JobView(job, [arbiter]));
        })
            .WithSummary("Vuelve a arbitrar un trabajo (experimental)")
            .WithDescription("Un trabajo nuevo (`rearbitration`) que copia las propuestas de A y B de un trabajo terminado de preguntas, catálogo o backlog, sortea de nuevo su orden y solo ejecuta el árbitro, con el modelo elegido. Deja su resultado y su informe, y no cambia las preguntas, el catálogo ni el backlog del proyecto. Sirve para comprobar si el árbitro prefiere la propuesta de su propio modelo.")
            .LaunchesJob();
    }

    // Called by JobWorker: the arbiter on the copied proposals. The result goes
    // to the folder of the job (resultado-arbitro.json), and nowhere else.
    public static async Task<PhaseJobResult> RunAsync(JobRunner jobRunner, Run arbiter, string jobFolder,
        Func<Run, Task> onRunStarting, Func<RunOutcome, Task> onRunFinished, CancellationToken cancellationToken)
    {
        var plan = PhasePlan.Load(jobFolder);
        string Proposal(string slot) => File.ReadAllText(Path.Combine(jobFolder, ProposalFile(slot)));
        return await jobRunner.RunArbiterAsync(arbiter, plan, Proposal("A"), Proposal("B"), onRunStarting, onRunFinished, cancellationToken);
    }

    private static IReadOnlyList<string> Models(RunnerOptions options) =>
        new[] { options.ModelArbiter, options.ModelA, options.ModelB }.Distinct().ToList();

    private static IResult Refuse(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["job"] = [message] });
}
