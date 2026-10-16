using Microsoft.EntityFrameworkCore;
using Concilia.Api.Persistence;
using Concilia.Core.Phases;
using Concilia.Core.Runs;

namespace Concilia.Api.Runs;

// The job being executed now, in any project, if any.
// Task: what it does, as the web shows it ("el mockup").
public record ActiveJobView(Guid JobId, int ProjectId, string ProjectName, string Kind, string Task, string Message);

// One task at a time in the whole platform (15-10-2026, autor). JobWorker
// already runs one job after another; on top of that, nothing new can be
// launched while a job is in progress, in its project or in any other: the
// web greys out the buttons that launch a task and says which one is running,
// and the API refuses the request (ensure it here, not only in the web).
public static class ActiveJobs
{
    // A job is in progress while some run of it is queued or running, as in
    // the flow of a project (FinishedAt alone is not enough: an API restart
    // leaves it unset).
    public static async Task<ActiveJobView?> FindAsync(AppDbContext db)
    {
        var jobId = await db.Runs
            .Where(r => r.JobId != null && (r.State == RunState.Queued || r.State == RunState.Running))
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.JobId)
            .FirstOrDefaultAsync();
        if (jobId is null)
            return null;
        var job = await db.Jobs.FindAsync(jobId.Value);
        if (job is null)
            return null;
        var project = await db.Projects.FindAsync(job.ProjectId);
        var projectName = project?.Name ?? $"#{job.ProjectId}";
        var task = TaskName(job.Kind);
        return new ActiveJobView(job.Id, job.ProjectId, projectName, job.Kind, task,
            $"Se está ejecutando {task} en el proyecto «{projectName}». Espera a que termine para lanzar otra tarea.");
    }

    // The answer to a request to launch a task while another one runs, or
    // null if nothing is running.
    public static async Task<IResult?> RefuseIfBusyAsync(AppDbContext db) =>
        await FindAsync(db) is { } active
            ? Results.ValidationProblem(new Dictionary<string, string[]> { ["job"] = [active.Message] })
            : null;

    private static string TaskName(string kind) => kind switch
    {
        _ when kind == PhasePlan.JobKind(PhaseKind.Questions) => "una ronda de preguntas",
        _ when kind == PhasePlan.JobKind(PhaseKind.Suggestions) => "las sugerencias de respuesta",
        _ when kind == PhasePlan.JobKind(PhaseKind.Catalog) => "el catálogo de requisitos",
        _ when kind == PhasePlan.JobKind(PhaseKind.Backlog) => "el backlog",
        Deliverables.MockupEndpoints.JobKind => "el mockup",
        CatalogTask.Kind => "el catálogo v1.4 (experimental)",
        Rearbitration.JobKind => "un nuevo arbitraje (experimental)",
        _ => "una tarea",
    };

    // GET /jobs/active: the job in progress (200), or 204 if there is none.
    public static void MapActiveJobEndpoint(this WebApplication app) =>
        app.MapGet("/jobs/active", async (AppDbContext db) =>
            await FindAsync(db) is { } active ? Results.Ok(active) : Results.NoContent());
}
