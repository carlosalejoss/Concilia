using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Concilia.Api.Persistence;
using Concilia.Core.Catalog;
using Concilia.Core.Phases;

namespace Concilia.Tests.Questions;

// Step 112: a round centred on an iteration through the API, with Fake and an
// in-memory database. A first round with the documentation of the iteration 1;
// then a second iteration whose acta repeats a fragment of the first, and a
// round centred on it.
public class FocusedRoundFlowTests
{
    private static WebApplicationFactory<Program> Api(string storage) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Runner:Provider", "Fake");
            builder.UseSetting("Mockup:Provider", "Fake");
            builder.UseSetting("Storage:RootPath", storage);
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
                var name = Guid.NewGuid().ToString();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(name));
            });
        });

    [Fact]
    public async Task A_round_centred_on_the_new_iteration_asks_about_it_and_revises_the_pending_questions()
    {
        using var temp = new TempDirectory();
        await using var api = Api(temp.Path);
        var client = api.CreateClient();
        var id = (await Json(await client.PostAsJsonAsync("/projects", new { name = "ReservaSalas" })))["id"]!.GetValue<int>();
        var document = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt"));
        await Upload(client, id, "DOC-01.txt", document);
        (await client.PostAsync($"/projects/{id}/iterations/1/close", null)).EnsureSuccessStatusCode();
        var first = await Launch(client, $"/projects/{id}/question-rounds", null);
        var asked = (await Json(await client.GetAsync($"/projects/{id}/questions"))).AsArray();

        // The iteration 2: an acta that repeats a fragment of DOC-01 (so it is related) and adds something.
        (await client.PostAsJsonAsync($"/projects/{id}/iterations", new { name = (string?)null })).EnsureSuccessStatusCode();
        var repeated = Fragmenter.FragmentDocument("DOC-01", document).First(f => f.Kind == Fragmenter.DocumentKind && f.Text.Length > 40).Text;
        await Upload(client, id, "acta.md", $"# Acta de la reunión\n\n{repeated}\n\nEl cliente pide un recordatorio por correo antes de cada reserva.\n");
        (await client.PostAsync($"/projects/{id}/iterations/2/close", null)).EnsureSuccessStatusCode();

        var wrong = await client.PostAsJsonAsync($"/projects/{id}/question-rounds", new { focus = new[] { 9 } });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal("La iteración 9 no existe.", JsonNode.Parse(await wrong.Content.ReadAsStringAsync())!["errors"]!["focus"]![0]!.GetValue<string>());

        var centred = await Launch(client, $"/projects/{id}/question-rounds", new { focus = new[] { 2 } });

        // The job keeps its focus, and its runs used the prompts v0.13.
        var job = await Json(await client.GetAsync($"/jobs/{centred}"));
        Assert.Equal([2], job["job"]!["focus"]!.AsArray().Select(n => n!.GetValue<int>()));
        Assert.All(job["runs"]!.AsArray(), r => Assert.Equal("v0.13", r!["promptVersion"]!.GetValue<string>()));
        Assert.Null((await Json(await client.GetAsync($"/jobs/{first}")))["job"]!["focus"]);
        var plan = PhasePlan.Load(Path.Combine(temp.Path, "jobs", centred));
        Assert.Equal(["DOC-02"], plan.Focus!.NewDocuments);
        Assert.DoesNotContain("focus", File.ReadAllText(Path.Combine(temp.Path, "jobs", first, PhasePlan.FileName))); // a round of always, as with v0.12

        // What A and the arbiter read.
        string Input(string slot, string file) =>
            File.ReadAllText(Path.Combine(temp.Path, "runs", job["runs"]!.AsArray().First(r => r!["slot"]!.GetValue<string>() == slot)!["id"]!.GetValue<string>(), file));
        Assert.StartsWith("=== DOC-02 · acta.md ===", Input("A", FocusedRound.NewDocumentationFile));
        Assert.Contains(repeated[..30], Input("A", FocusedRound.RelatedFragmentsFile));
        Assert.Contains("previa:", Input("ARB", "input/emparejamiento.txt"));
        Assert.StartsWith(asked[0]!["code"]!.GetValue<string>(), Input("ARB", FocusedRound.RevisionsFile));

        // The new question cites the new documentation, and the pending one has the proposal "resuelta".
        var questions = (await Json(await client.GetAsync($"/projects/{id}/questions"))).AsArray();
        var added = questions.Where(q => q!["jobId"]!.GetValue<string>() == centred).ToList();
        Assert.NotEmpty(added);
        Assert.All(added, q => Assert.StartsWith("DOC-02#", q!["sources"]![0]!.GetValue<string>()));
        var revised = questions.Single(q => q!["code"]!.GetValue<string>() == asked[0]!["code"]!.GetValue<string>())!;
        Assert.Equal(("Pending", "resuelta"), (revised["status"]!.GetValue<string>(), revised["proposedAction"]!.GetValue<string>()));
        var change = revised["history"]!.AsArray().Single()!;
        Assert.Equal(("propuesta-resuelta", "ambas"), (change["action"]!.GetValue<string>(), change["origin"]!.GetValue<string>()));
        Assert.Equal([2], change["iterations"]!.AsArray().Select(n => n!.GetValue<int>()));

        // A round with all the iterations is still the one of always.
        var always = await Launch(client, $"/projects/{id}/question-rounds", new { focus = Array.Empty<int>() });
        Assert.All((await Json(await client.GetAsync($"/jobs/{always}")))["runs"]!.AsArray(), r => Assert.Equal("v0.12", r!["promptVersion"]!.GetValue<string>()));
    }

    // Step 116: the same centred round reading everything (option A) is an
    // experiment: A and B read all the earlier documentation, and its
    // questions are not added to the project nor does it count as a round.
    [Fact]
    public async Task A_round_that_reads_everything_reads_all_the_earlier_documentation_and_changes_nothing()
    {
        using var temp = new TempDirectory();
        await using var api = Api(temp.Path);
        var client = api.CreateClient();
        var id = (await Json(await client.PostAsJsonAsync("/projects", new { name = "ReservaSalas" })))["id"]!.GetValue<int>();
        var document = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt"));
        await Upload(client, id, "DOC-01.txt", document);
        (await client.PostAsync($"/projects/{id}/iterations/1/close", null)).EnsureSuccessStatusCode();
        await Launch(client, $"/projects/{id}/question-rounds", null);
        (await client.PostAsJsonAsync($"/projects/{id}/iterations", new { name = (string?)null })).EnsureSuccessStatusCode();
        await Upload(client, id, "acta.md", "# Acta de la reunión\n\nEl cliente pide un recordatorio por correo antes de cada reserva.\n");
        (await client.PostAsync($"/projects/{id}/iterations/2/close", null)).EnsureSuccessStatusCode();
        var questions = (await client.GetStringAsync($"/projects/{id}/questions"));
        var flow = await client.GetStringAsync($"/projects/{id}/flow");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/projects/{id}/question-rounds/read-all", new { focus = Array.Empty<int>() })).StatusCode);
        var readAll = await Launch(client, $"/projects/{id}/question-rounds/read-all", new { focus = new[] { 2 } });

        var job = await Json(await client.GetAsync($"/jobs/{readAll}"));
        Assert.Equal("questions-read-all", job["job"]!["kind"]!.GetValue<string>());
        Assert.All(job["runs"]!.AsArray(), r => Assert.Equal("v0.13", r!["promptVersion"]!.GetValue<string>()));
        // A reads entirely every fragment of DOC-01, not only the related ones (here, none would be).
        var runA = job["runs"]!.AsArray().First(r => r!["slot"]!.GetValue<string>() == "A")!["id"]!.GetValue<string>();
        var related = File.ReadAllText(Path.Combine(temp.Path, "runs", runA, FocusedRound.RelatedFragmentsFile));
        Assert.All(Fragmenter.FragmentDocument("DOC-01", document), f => Assert.Contains(f.Id, related));

        // Nothing of the project changes, and the report says it is an experiment.
        Assert.Equal(questions, await client.GetStringAsync($"/projects/{id}/questions"));
        Assert.Equal(flow, await client.GetStringAsync($"/projects/{id}/flow"));
        var report = await Json(await client.GetAsync($"/jobs/{readAll}/report"));
        Assert.Equal(("Questions", "Ronda experimental que lo lee todo: sus preguntas no se añaden al proyecto."),
            (report["phase"]!.GetValue<string>(), report["changesNote"]!.GetValue<string>()));
        Assert.NotNull(report["elements"]);
    }

    private static async Task Upload(HttpClient client, int projectId, string name, string text)
    {
        using var form = new MultipartFormDataContent { { new ByteArrayContent(Encoding.UTF8.GetBytes(text)), "file", name } };
        (await client.PostAsync($"/projects/{projectId}/files", form)).EnsureSuccessStatusCode();
    }

    // Launches a round and waits until it ends with a result.
    private static async Task<string> Launch(HttpClient client, string url, object? body)
    {
        var response = body is null ? await client.PostAsync(url, null) : await client.PostAsJsonAsync(url, body);
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
        return jobId;
    }

    private static async Task<JsonNode> Json(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.RequestUri}: {response.StatusCode} {text}");
        return JsonNode.Parse(text)!;
    }
}
