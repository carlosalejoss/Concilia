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

namespace Concilia.Tests.Deliverables;

// Step 113: the validated catalog updated with a second iteration, through the
// API with Fake. A new requirement is added with the next id and its history;
// the modification of a related requirement waits for the user, and the
// catalog is validated after accepting it.
public class CatalogUpdateFlowTests
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
    public async Task The_validated_catalog_is_updated_with_a_new_iteration_and_its_changes_are_reviewed()
    {
        using var temp = new TempDirectory();
        await using var api = Api(temp.Path);
        var client = api.CreateClient();
        var id = (await Json(await client.PostAsJsonAsync("/projects", new { name = "ReservaSalas" })))["id"]!.GetValue<int>();
        var document = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt"));
        await Upload(client, id, "DOC-01.txt", document);
        (await client.PostAsync($"/projects/{id}/iterations/1/close", null)).EnsureSuccessStatusCode();
        await Launch(client, $"/projects/{id}/question-rounds", null);
        await AnswerAll(client, id);
        var first = await Launch(client, $"/projects/{id}/catalog-jobs", null);
        await ReviewAndValidate(client, id);
        var before = await Json(await client.GetAsync($"/projects/{id}/deliverables/catalog"));
        var requirements = before["content"]!["requisitos"]!.AsArray();

        // The iteration 2: an acta that repeats the fragment of the first requirement (so it is related).
        (await client.PostAsJsonAsync($"/projects/{id}/iterations", new { name = (string?)null })).EnsureSuccessStatusCode();
        var cited = requirements[0]!["fuentes"]![0]!.GetValue<string>();
        var repeated = Fragmenter.FragmentDocument("DOC-01", document).Single(f => f.Id == cited).Text;
        await Upload(client, id, "acta.md", $"# Acta\n\n{repeated}\n\nEl cliente pide un recordatorio por correo antes de cada reserva.\n");
        (await client.PostAsync($"/projects/{id}/iterations/2/close", null)).EnsureSuccessStatusCode();

        // Without a round about the iteration 2 after closing it, no update.
        var refused = await client.PostAsJsonAsync($"/projects/{id}/catalog-update-jobs", new { iterations = new[] { 2 } });
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.StartsWith("Haz una ronda de preguntas sobre la iteración 2",
            JsonNode.Parse(await refused.Content.ReadAsStringAsync())!["errors"]!["iterations"]![0]!.GetValue<string>());

        await Launch(client, $"/projects/{id}/question-rounds", new { focus = new[] { 2 } });
        await AnswerAll(client, id);
        var update = await Launch(client, $"/projects/{id}/catalog-update-jobs", new { iterations = new[] { 2 } });

        var job = await Json(await client.GetAsync($"/jobs/{update}"));
        Assert.Equal("catalog-update", job["job"]!["kind"]!.GetValue<string>());
        Assert.All(job["runs"]!.AsArray(), r => Assert.Equal("v0.13", r!["promptVersion"]!.GetValue<string>()));

        // Its report says the iterations and what A, B and the arbiter proposed; the comparison A | arbiter | B, its new requirements.
        var report = await Json(await client.GetAsync($"/jobs/{update}/report"));
        Assert.Equal([2], report["update"]!["iterations"]!.AsArray().Select(n => n!.GetValue<int>()));
        Assert.Equal((1, 1, 0), (report["update"]!["arbiter"]!["new"]!.GetValue<int>(), report["update"]!["arbiter"]!["modified"]!.GetValue<int>(), report["update"]!["arbiter"]!["deleted"]!.GetValue<int>()));
        Assert.Equal(1, report["update"]!["a"]!["new"]!.GetValue<int>());
        Assert.Single((await Json(await client.GetAsync($"/jobs/{update}/comparison")))["rows"]!.AsArray(), r => r!["kind"]!.GetValue<string>() == "Merged");

        var draft = await Json(await client.GetAsync($"/projects/{id}/deliverables/catalog"));
        Assert.Equal("Draft", draft["status"]!.GetValue<string>());
        Assert.Equal([1, 2], draft["iterations"]!.AsArray().Select(n => n!.GetValue<int>()));
        var updated = draft["content"]!["requisitos"]!.AsArray();
        // The validated requirements keep their ids; the new one takes the next number of its type.
        Assert.Equal(requirements.Select(r => r!["id"]!.GetValue<string>()), updated.Take(requirements.Count).Select(r => r!["id"]!.GetValue<string>()));
        var added = updated[^1]!;
        Assert.Equal($"RF-{requirements.Count + 1:000}", added["id"]!.GetValue<string>());
        Assert.Equal("creado", added["historial"]![0]!["accion"]!.GetValue<string>());
        // The related requirement is modified, as a proposal to review.
        var modified = updated.Single(r => r!["cambio"] is not null)!;
        var modifiedId = modified["id"]!.GetValue<string>();
        Assert.EndsWith("(actualizado)", modified["texto"]!.GetValue<string>());
        Assert.Contains(draft["errors"]!.AsArray(), e => e!.GetValue<string>().StartsWith($"{modifiedId} CAMBIO"));
        var versions = (await Json(await client.GetAsync($"/projects/{id}/deliverables/catalog/versions"))).AsArray();
        Assert.Equal(("Updated", update), (versions[0]!["origin"]!.GetValue<string>(), versions[0]!["jobId"]!.GetValue<string>()));
        Assert.Equal(first, versions[1]!["jobId"]!.GetValue<string>()); // the validated catalog it started from

        var accepted = await Json(await client.PostAsync($"/projects/{id}/deliverables/catalog/requirements/{modifiedId}/accept-change", null));
        var kept = accepted["content"]!["requisitos"]!.AsArray().Single(r => r!["id"]!.GetValue<string>() == modifiedId)!;
        Assert.Null(kept["cambio"]);
        Assert.Equal("modificado", kept["historial"]![0]!["accion"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/projects/{id}/deliverables/catalog/requirements/{modifiedId}/reject-change", null)).StatusCode);
        await ReviewAndValidate(client, id);
    }

    private static async Task AnswerAll(HttpClient client, int projectId)
    {
        foreach (var q in (await Json(await client.GetAsync($"/projects/{projectId}/questions"))).AsArray().Where(q => q!["status"]!.GetValue<string>() == "Pending"))
            (await client.PutAsJsonAsync($"/questions/{q!["id"]}/answer", new { answer = "Sí, como dice la documentación." })).EnsureSuccessStatusCode();
    }

    // Reviews every discrepancy and validates.
    private static async Task ReviewAndValidate(HttpClient client, int projectId)
    {
        var content = (await Json(await client.GetAsync($"/projects/{projectId}/deliverables/catalog")))["content"]!.DeepClone();
        foreach (var r in content["requisitos"]!.AsArray())
            if (r!["discrepancia"]!.GetValue<bool>())
                r["revisada"] = true;
        (await client.PutAsJsonAsync($"/projects/{projectId}/deliverables/catalog", new { content })).EnsureSuccessStatusCode();
        var validated = await client.PostAsync($"/projects/{projectId}/deliverables/catalog/validate", null);
        Assert.True(validated.IsSuccessStatusCode, await validated.Content.ReadAsStringAsync());
    }

    private static async Task Upload(HttpClient client, int projectId, string name, string text)
    {
        using var form = new MultipartFormDataContent { { new ByteArrayContent(Encoding.UTF8.GetBytes(text)), "file", name } };
        (await client.PostAsync($"/projects/{projectId}/files", form)).EnsureSuccessStatusCode();
    }

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
