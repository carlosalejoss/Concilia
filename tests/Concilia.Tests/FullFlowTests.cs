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

namespace Concilia.Tests;

// Step 63: the whole flow through the API, as the web does it, with Fake and
// an in-memory database (the real one is PostgreSQL, in Docker): create a
// project, upload the ReservaSalas document, a round of questions, answer
// them, generate and validate the catalog and the backlog, generate the
// mockup and open it. Then, launching an earlier step again leaves the later
// ones outdated.
public class FullFlowTests
{
    private static WebApplicationFactory<Program> Api(string storage) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Runner:Provider", "Fake");
            builder.UseSetting("Runner:FakeStepDelayMs", "0");
            builder.UseSetting("Mockup:Provider", "Fake");
            builder.UseSetting("Storage:RootPath", storage);
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.ConfigureServices(services =>
            {
                // The database of the test lives in memory, one per test.
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
                var name = Guid.NewGuid().ToString();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(name));
            });
        });

    [Fact]
    public async Task A_project_goes_from_its_documents_to_the_mockup()
    {
        using var temp = new TempDirectory();
        await using var api = Api(temp.Path);
        var client = api.CreateClient();

        var project = await Json(await client.PostAsJsonAsync("/projects", new { name = "ReservaSalas" }));
        var id = project["id"]!.GetValue<int>();
        Assert.Equal("Blocked", (await Step(client, id, "Questions"))["state"]!.GetValue<string>());

        // Documents.
        var document = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt"));
        using (var form = new MultipartFormDataContent { { new ByteArrayContent(document), "file", "DOC-01.txt" } })
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync($"/projects/{id}/files", form)).StatusCode);

        // A round of questions, and an answer to each one.
        await Launch(client, $"/projects/{id}/question-rounds", id);
        var questions = (await Json(await client.GetAsync($"/projects/{id}/questions"))).AsArray();
        Assert.NotEmpty(questions);
        foreach (var question in questions.Where(q => q!["status"]!.GetValue<string>() == "Pending"))
            (await client.PutAsJsonAsync($"/questions/{question!["id"]}/answer", new { answer = "Sí, como describe el documento." })).EnsureSuccessStatusCode();

        // The catalog and the backlog: generated, reviewed and validated.
        await Launch(client, $"/projects/{id}/catalog-jobs", id);
        await ReviewAndValidate(client, id, "catalog");
        await Launch(client, $"/projects/{id}/backlog-jobs", id);
        await ReviewAndValidate(client, id, "backlog");

        // The mockup, which opens as a page.
        await Launch(client, $"/projects/{id}/mockup-jobs", id);
        var page = await client.GetAsync($"/projects/{id}/mockup");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.StartsWith("<!doctype html", (await page.Content.ReadAsStringAsync()).TrimStart(), StringComparison.OrdinalIgnoreCase);

        var flow = await Flow(client, id);
        Assert.Equal(["Draft", "Draft", "Validated", "Validated", "Draft"], flow.Select(s => s!["state"]!.GetValue<string>()));
        Assert.All(flow, s => Assert.False(s!["outdated"]!.GetValue<bool>()));

        // Any step can be launched again: a new catalog leaves the backlog
        // and the mockup outdated.
        Assert.True((await Step(client, id, "Catalog"))["canLaunch"]!.GetValue<bool>());
        await Launch(client, $"/projects/{id}/catalog-jobs", id);
        flow = await Flow(client, id);
        Assert.Equal("Draft", flow[2]!["state"]!.GetValue<string>());
        Assert.Equal([false, false, false, true, true], flow.Select(s => s!["outdated"]!.GetValue<bool>()));
    }

    // Launches a job and waits until it ends; it must end with a result.
    private static async Task Launch(HttpClient client, string url, int projectId)
    {
        var response = await client.PostAsync(url, null);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Accepted, $"{url}: {response.StatusCode} {text}");
        var jobId = JsonNode.Parse(text)!["job"]!["id"]!.GetValue<string>();
        var until = DateTime.UtcNow.AddSeconds(60);
        while ((await Json(await client.GetAsync($"/jobs/{jobId}")))["job"]!["finishedAt"] is null)
        {
            Assert.True(DateTime.UtcNow < until, $"{url}: the job did not end in 60 s.");
            await Task.Delay(100);
        }
        var result = await Json(await client.GetAsync($"/jobs/{jobId}/result"));
        Assert.True(result["fuente"] is not null, $"{url}: the job failed: {result["mensaje"]}");
    }

    // The user reviews every discrepancy of the arbiter and validates.
    private static async Task ReviewAndValidate(HttpClient client, int projectId, string kind)
    {
        var deliverable = await Json(await client.GetAsync($"/projects/{projectId}/deliverables/{kind}"));
        var content = deliverable["content"]!.DeepClone();
        MarkReviewed(content);
        (await client.PutAsJsonAsync($"/projects/{projectId}/deliverables/{kind}", new { content })).EnsureSuccessStatusCode();
        var validated = await client.PostAsync($"/projects/{projectId}/deliverables/{kind}/validate", null);
        Assert.True(validated.IsSuccessStatusCode, $"{kind}: {await validated.Content.ReadAsStringAsync()}");
    }

    private static void MarkReviewed(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["discrepancia"] is JsonValue d && d.GetValue<bool>())
                    obj["revisada"] = true;
                foreach (var (_, child) in obj.ToList())
                    MarkReviewed(child);
                break;
            case JsonArray array:
                foreach (var child in array)
                    MarkReviewed(child);
                break;
        }
    }

    private static async Task<JsonArray> Flow(HttpClient client, int projectId) =>
        (await Json(await client.GetAsync($"/projects/{projectId}/flow")))["steps"]!.AsArray();

    private static async Task<JsonNode> Step(HttpClient client, int projectId, string step) =>
        (await Flow(client, projectId)).Single(s => s!["step"]!.GetValue<string>() == step)!;

    private static async Task<JsonNode> Json(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.RequestUri}: {response.StatusCode} {text}");
        return JsonNode.Parse(text)!;
    }
}
