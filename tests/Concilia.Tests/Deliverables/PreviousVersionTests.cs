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
using Concilia.Core.Deliverables;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;

namespace Concilia.Tests.Deliverables;

// Step 115: a catalog or backlog generated from scratch against the version
// before it, with the Fake embedder (it counts words: the same words give
// similarity 1, no word in common gives 0).
public class PreviousVersionTests
{
    private static readonly FakeTextEmbedder Embedder = new();
    private static readonly MatchThresholds Thresholds = new(0.90, 0.70);

    [Fact]
    public void Each_new_requirement_is_same_changed_or_new_and_what_is_left_before_is_gone()
    {
        const string previous = """
            {"requisitos": [
              {"id": "RF-007", "texto": "reservar una sala"},
              {"id": "RF-008", "texto": "cancelar la cuenta"}
            ]}
            """;
        const string current = """
            {"requisitos": [
              {"id": "RF-001", "texto": "reservar una sala"},
              {"id": "RF-002", "texto": "reservar una sala grande"},
              {"id": "RF-003", "texto": "exportar el informe mensual"}
            ]}
            """;

        var comparison = PreviousVersion.Classify(4, DeliverableStatus.Validated,
            PreviousVersion.Similarities(PhaseKind.Catalog, current, previous, Embedder), Thresholds);

        // Ids are not compared: RF-001 is the RF-007 of before.
        Assert.Equal(
            [("RF-001", VersionMark.Same, "RF-007"), ("RF-002", VersionMark.Changed, "RF-007"), ("RF-003", VersionMark.New, "RF-007")],
            comparison.Elements.Select(e => (e.Id, e.Mark, e.PreviousId)).ToList());
        Assert.Equal("reservar una sala", comparison.Elements[1].PreviousText); // what it was, to show it
        Assert.Equal(["RF-008"], comparison.Gone.Select(g => g.Id));
        Assert.Equal(new PreviousCounts(4, DeliverableStatus.Validated, 1, 1, 1, 1), comparison.Counts);
    }

    [Fact]
    public void The_stories_are_compared_by_their_title_and_narrative_across_epics()
    {
        const string previous = """
            {"epicas": [{"id": "EP-001", "historias": [
              {"id": "HU-001", "titulo": "Reservar", "como": "empleado", "quiero": "reservar una sala", "para": "reunirme"}
            ]}]}
            """;
        const string current = """
            {"epicas": [
              {"id": "EP-001", "historias": []},
              {"id": "EP-002", "historias": [
                {"id": "HU-001", "titulo": "Reservar", "como": "empleado", "quiero": "reservar una sala", "para": "reunirme"}
              ]}
            ]}
            """;

        var comparison = PreviousVersion.Classify(1, DeliverableStatus.Draft,
            PreviousVersion.Similarities(PhaseKind.Backlog, current, previous, Embedder), Thresholds);

        Assert.Equal(VersionMark.Same, Assert.Single(comparison.Elements).Mark);
        Assert.Empty(comparison.Gone);
    }

    [Fact]
    public void Without_anything_before_everything_is_new_and_nothing_is_gone()
    {
        var comparison = PreviousVersion.Classify(1, DeliverableStatus.Draft,
            PreviousVersion.Similarities(PhaseKind.Catalog, """{"requisitos": [{"id": "RF-001", "texto": "reservar una sala"}]}""", """{"requisitos": []}""", Embedder),
            Thresholds);

        var element = Assert.Single(comparison.Elements);
        Assert.Equal((VersionMark.New, (string?)null), (element.Mark, element.PreviousId));
        Assert.Empty(comparison.Gone);
    }

    // Through the API: the first catalog has nothing before; the second one,
    // generated after the user changed a requirement, is compared with that
    // draft; the report and the CSV carry the figures, and a round of
    // questions has no comparison.
    [Fact]
    public async Task A_catalog_from_scratch_is_compared_with_the_version_before_it()
    {
        using var temp = new TempDirectory();
        await using var api = Api(temp.Path);
        var client = api.CreateClient();
        var id = (await Json(await client.PostAsJsonAsync("/projects", new { name = "ReservaSalas" })))["id"]!.GetValue<int>();
        var document = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt"));
        using (var form = new MultipartFormDataContent { { new ByteArrayContent(Encoding.UTF8.GetBytes(document)), "file", "DOC-01.txt" } })
            (await client.PostAsync($"/projects/{id}/files", form)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/projects/{id}/iterations/1/close", null)).EnsureSuccessStatusCode();
        var round = await Launch(client, $"/projects/{id}/question-rounds");
        foreach (var q in (await Json(await client.GetAsync($"/projects/{id}/questions"))).AsArray())
            (await client.PutAsJsonAsync($"/questions/{q!["id"]}/answer", new { answer = "Sí." })).EnsureSuccessStatusCode();

        var first = await Launch(client, $"/projects/{id}/catalog-jobs");
        var none = await Json(await client.GetAsync($"/jobs/{first}/previous-version"));
        Assert.Null(none["comparison"]);
        Assert.Equal("Es la primera versión: no hay nada antes con qué compararla.", none["unavailable"]!.GetValue<string>());
        Assert.Null((await Json(await client.GetAsync($"/jobs/{first}/report")))["previous"]);
        Assert.StartsWith("Solo se compara", (await Json(await client.GetAsync($"/jobs/{round}/previous-version")))["unavailable"]!.GetValue<string>());

        // The user rewrites a requirement, and then the catalog is generated again.
        var content = (await Json(await client.GetAsync($"/projects/{id}/deliverables/catalog")))["content"]!.DeepClone();
        var count = content["requisitos"]!.AsArray().Count;
        content["requisitos"]![0]!["texto"] = "Exportar facturas trimestrales XBRL zeta";
        (await client.PutAsJsonAsync($"/projects/{id}/deliverables/catalog", new { content })).EnsureSuccessStatusCode();
        var edited = (await Json(await client.GetAsync($"/projects/{id}/deliverables/catalog/versions")))[0]!["number"]!.GetValue<int>();
        var second = await Launch(client, $"/projects/{id}/catalog-jobs");

        var view = await Json(await client.GetAsync($"/jobs/{second}/previous-version"));
        Assert.False(view["fromCache"]!.GetValue<bool>());
        var comparison = view["comparison"]!;
        Assert.Equal((edited, "Draft"), (comparison["version"]!.GetValue<int>(), comparison["status"]!.GetValue<string>()));
        var elements = comparison["elements"]!.AsArray();
        Assert.Equal(count, elements.Count);
        // The same catalog as the first time: what the user did not touch is the same.
        Assert.All(elements.Skip(1), e => Assert.Equal("Same", e!["mark"]!.GetValue<string>()));
        Assert.Equal("Exportar facturas trimestrales XBRL zeta", Assert.Single(comparison["gone"]!.AsArray())!["text"]!.GetValue<string>());
        // Computed once: the second time it is read from the job's folder.
        Assert.True((await Json(await client.GetAsync($"/jobs/{second}/previous-version")))["fromCache"]!.GetValue<bool>());

        var previous = (await Json(await client.GetAsync($"/jobs/{second}/report")))["previous"]!;
        int Count(string mark) => elements.Count(e => e!["mark"]!.GetValue<string>() == mark);
        Assert.Equal((edited, Count("Same"), Count("Changed"), Count("New"), 1),
            (previous["version"]!.GetValue<int>(), previous["same"]!.GetValue<int>(), previous["changed"]!.GetValue<int>(),
             previous["new"]!.GetValue<int>(), previous["gone"]!.GetValue<int>()));

        // The CSV: the new columns at the end, filled only in the second catalog.
        var csv = (await client.GetStringAsync($"/projects/{id}/report.csv")).TrimStart('﻿').Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(';')).ToList();
        Assert.Equal(["ant_version", "ant_igual", "ant_cambiado", "ant_nuevo", "ant_desaparecido"], csv[0][^5..]);
        var row = csv.Single(r => r[1] == second);
        Assert.Equal([$"{edited}", $"{Count("Same")}", $"{Count("Changed")}", $"{Count("New")}", "1"], row[^5..]);
        Assert.All(csv.Single(r => r[1] == first)[^5..], cell => Assert.Equal("", cell));
    }

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

    private static async Task<string> Launch(HttpClient client, string url)
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
        return jobId;
    }

    private static async Task<JsonNode> Json(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.RequestUri}: {response.StatusCode} {text}");
        return JsonNode.Parse(text)!;
    }
}
