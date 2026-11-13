using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Concilia.Api.Persistence;

namespace Concilia.Tests.Runs;

// Step 138: each repetition of a task keeps its own duration. The attempts of
// a task are saved together when it ends; before, a repetition took the start
// of the first attempt and all of them ended at that moment, so each one had
// the duration of the whole task.
public class RunDurationTests
{
    [Fact]
    public async Task Each_attempt_of_a_repeated_task_has_its_own_duration()
    {
        using var temp = new TempDirectory();
        // With SimulateRepair, the first attempt of each model writes an invalid
        // output and the task is repeated as a repair; each event of Fake takes 20 ms.
        await using var api = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Runner:Provider", "Fake");
            builder.UseSetting("Runner:FakeStepDelayMs", "20");
            builder.UseSetting("Runner:SimulateRepair", "true");
            builder.UseSetting("Mockup:Provider", "Fake");
            builder.UseSetting("Storage:RootPath", temp.Path);
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
                var name = Guid.NewGuid().ToString();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(name));
            });
        });
        var client = api.CreateClient();

        var id = (await Json(await client.PostAsJsonAsync("/projects", new { name = "ReservaSalas" })))["id"]!.GetValue<int>();
        var document = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt"));
        using (var form = new MultipartFormDataContent { { new ByteArrayContent(document), "file", "DOC-01.txt" } })
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync($"/projects/{id}/files", form)).StatusCode);
        await Json(await client.PostAsync($"/projects/{id}/iterations/1/close", null));

        var launched = await client.PostAsync($"/projects/{id}/question-rounds", null);
        Assert.Equal(HttpStatusCode.Accepted, launched.StatusCode);
        var jobId = (await Json(launched))["job"]!["id"]!.GetValue<string>();
        JsonNode view;
        var until = DateTime.UtcNow.AddSeconds(60);
        while ((view = await Json(await client.GetAsync($"/jobs/{jobId}")))["job"]!["finishedAt"] is null)
        {
            Assert.True(DateTime.UtcNow < until, "The round did not end in 60 s.");
            await Task.Delay(100);
        }

        foreach (var slot in new[] { "A", "B", "ARB" })
        {
            var attempts = view["runs"]!.AsArray().Where(r => r!["slot"]!.GetValue<string>() == slot)
                .OrderBy(r => r!["attempt"]!.GetValue<int>())
                .Select(r => (Start: r!["startedAt"]!.GetValue<DateTimeOffset>(), End: r["finishedAt"]!.GetValue<DateTimeOffset>()))
                .ToList();
            Assert.Equal(2, attempts.Count); // the first one and its repair
            var (first, repair) = (attempts[0], attempts[1]);
            // The repair begins when the first attempt has ended, not when it began…
            Assert.True(repair.Start >= first.End, $"{slot}: the repair began at {repair.Start:O}, before the first attempt ended at {first.End:O}.");
            // …and the first attempt ends when it ends, not when the repair does.
            Assert.True(first.End < repair.End, $"{slot}: the first attempt ended with its repair, at {first.End:O}.");
        }
    }

    private static async Task<JsonNode> Json(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.RequestUri}: {response.StatusCode} {text}");
        return JsonNode.Parse(text)!;
    }
}
