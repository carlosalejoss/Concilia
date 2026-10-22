using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Core.Phases;
using Concilia.Core.Runs;

namespace Concilia.Api.Runs;

// Runs that were queued or running when the API stopped will never finish.
// Step 91 (decision of the author, 19-10-2026): their jobs are closed too, as
// failed and with a message; until then they stayed without an end, and an
// interrupted mockup could not be resumed. Nothing is launched again by
// itself: the user decides. At start-up no job is running, so every job
// created before it and without an end was interrupted (the queue is in
// memory and is lost with the restart).
public static class InterruptedJobs
{
    public const string RunMessage = "Interrumpida: la API se reinició.";

    public static async Task CloseAsync(AppDbContext db, FileStorage storage, DateTimeOffset start)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var run in await db.Runs.Where(r => (r.State == RunState.Queued || r.State == RunState.Running) && r.CreatedAt < start).ToListAsync())
        {
            run.State = RunState.Failed;
            run.FinishedAt = now;
            run.ErrorMessage = RunMessage;
        }

        foreach (var job in await db.Jobs.Where(j => j.FinishedAt == null && j.CreatedAt < start).ToListAsync())
        {
            // The jobs whose result the web reads get one that says what happened.
            var folder = storage.JobFolder(job.Id);
            if (!File.Exists(Path.Combine(folder, PhaseJobResult.FileName))
                && (job.Kind == Deliverables.MockupEndpoints.JobKind || job.Kind == Rearbitration.JobKind || PhasePlan.KindOfJob(job.Kind) is not null))
            {
                PhaseJobs.WriteResult(folder, new PhaseJobResult(null, false, null, Message(job.Kind)));
            }
            job.FinishedAt = now;
        }
        await db.SaveChangesAsync();
    }

    // The duration of a job, for its report. A job closed here gets the time of
    // the start-up as its end, which can be days after it stopped working (in
    // the cost tables of 22-10-2026, 101 hours for a mockup that worked 12
    // minutes). Its duration ends instead with the last run that ended before
    // that start-up; the runs closed at the start-up end with the job and do
    // not count. Without any, it has no duration.
    public static double? Seconds(Job job, IEnumerable<Run> runs, string? resultMessage)
    {
        if (job.FinishedAt is not { } finished) return null;
        if (resultMessage != Message(job.Kind)) return (finished - job.CreatedAt).TotalSeconds;
        var lastEnd = runs.Where(r => r.FinishedAt < finished).Max(r => r.FinishedAt);
        return lastEnd is { } end ? (end - job.CreatedAt).TotalSeconds : null;
    }

    public static string Message(string jobKind) => jobKind == Deliverables.MockupEndpoints.JobKind
        ? "Interrumpido: la API se reinició antes de terminar. Si llegó a tener el diseño, se puede reanudar."
        : "Interrumpido: la API se reinició antes de terminar. Se puede lanzar de nuevo.";
}
