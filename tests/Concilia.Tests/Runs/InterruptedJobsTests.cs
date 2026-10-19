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
}
