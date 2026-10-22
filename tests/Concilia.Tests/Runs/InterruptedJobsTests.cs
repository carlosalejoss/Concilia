using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Api.Runs;
using Concilia.Core.Phases;
using Concilia.Core.Runs;

namespace Concilia.Tests.Runs;

// Step 91: at start-up, the jobs a restart of the API left half done are
// closed as failed, and the runs that were running too.
public class InterruptedJobsTests
{
    [Fact]
    public async Task The_jobs_left_without_an_end_are_closed_with_a_message()
    {
        using var temp = new TempDirectory();
        var storage = new FileStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = temp.Path }).Build(),
            new HostingEnvironment { ContentRootPath = temp.Path });
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var start = DateTimeOffset.UtcNow;
        var before = start.AddMinutes(-5);
        var interrupted = new Job { Id = Guid.NewGuid(), ProjectId = 1, Kind = PhasePlan.JobKind(PhaseKind.Catalog), CreatedAt = before };
        var mockup = new Job { Id = Guid.NewGuid(), ProjectId = 1, Kind = Concilia.Api.Deliverables.MockupEndpoints.JobKind, CreatedAt = before };
        var finished = new Job { Id = Guid.NewGuid(), ProjectId = 1, Kind = "questions", CreatedAt = before, FinishedAt = before.AddMinutes(1) };
        var justCreated = new Job { Id = Guid.NewGuid(), ProjectId = 1, Kind = "questions", CreatedAt = start.AddSeconds(1) }; // after the start
        db.Jobs.AddRange(interrupted, mockup, finished, justCreated);
        var running = new Run { Id = Guid.NewGuid(), ProjectId = 1, JobId = interrupted.Id, Slot = "A", Provider = "Fake", Model = "m", State = RunState.Running, CreatedAt = before };
        db.Runs.Add(running);
        await db.SaveChangesAsync();

        await InterruptedJobs.CloseAsync(db, storage, start);

        Assert.Equal(RunState.Failed, running.State);
        Assert.Equal(InterruptedJobs.RunMessage, running.ErrorMessage);
        Assert.NotNull(interrupted.FinishedAt);
        Assert.NotNull(mockup.FinishedAt); // so it can be resumed
        Assert.Null(justCreated.FinishedAt);
        Assert.Equal(before.AddMinutes(1), finished.FinishedAt);
        var result = JsonNode.Parse(File.ReadAllText(Path.Combine(storage.JobFolder(interrupted.Id), PhaseJobResult.FileName)))!;
        Assert.Null(result["fuente"]);
        Assert.StartsWith("Interrumpido", result["mensaje"]!.GetValue<string>());
        Assert.Contains("reanudar", JsonNode.Parse(File.ReadAllText(Path.Combine(storage.JobFolder(mockup.Id), PhaseJobResult.FileName)))!["mensaje"]!.GetValue<string>());
    }

    // 22-10-2026: a job closed at start-up lasted until its last run ended,
    // not until the start-up, which can be days later.
    [Fact]
    public void An_interrupted_job_lasts_until_its_last_run_ended()
    {
        var created = DateTimeOffset.UtcNow.AddDays(-4);
        var startUp = DateTimeOffset.UtcNow;
        var job = new Job { Id = Guid.NewGuid(), ProjectId = 1, Kind = "mockup", CreatedAt = created, FinishedAt = startUp };
        Run RunEnding(DateTimeOffset? end) => new() { Id = Guid.NewGuid(), ProjectId = 1, JobId = job.Id, Provider = "Fake", Model = "m", CreatedAt = created, FinishedAt = end };
        var runs = new[] { RunEnding(created.AddMinutes(6)), RunEnding(created.AddMinutes(12)), RunEnding(startUp), RunEnding(null) };
        var message = InterruptedJobs.Message(job.Kind);

        Assert.Equal(12 * 60, InterruptedJobs.Seconds(job, runs, message)!.Value, precision: 3);
        Assert.Null(InterruptedJobs.Seconds(job, [RunEnding(startUp)], message)); // nothing ended before the start-up
        Assert.Equal((startUp - created).TotalSeconds, InterruptedJobs.Seconds(job, runs, "otro mensaje")); // a job that ended by itself
        Assert.Null(InterruptedJobs.Seconds(new Job { Id = Guid.NewGuid(), ProjectId = 1, Kind = "mockup", CreatedAt = created }, runs, null)); // not ended
    }
}
