using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting.Internal;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Api.Projects;
using Concilia.Api.Runs;
using Concilia.Core.Deliverables;
using Concilia.Core.Flow;
using Concilia.Core.Projects;
using Concilia.Core.Questions;

namespace Concilia.Tests.Projects;

// Step 109: the iterations of a project through the API (with Fake and an
// in-memory database), and the "Iteración 1" that the projects from before
// the iterations get at start-up.
public class IterationTests
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
    public async Task Documents_go_to_the_open_iteration_and_the_style_reference_to_the_project()
    {
        using var temp = new TempDirectory();
        await using var api = Api(temp.Path);
        var client = api.CreateClient();
        var id = (await Json(await client.PostAsJsonAsync("/projects", new { name = "Clínicas" })))["id"]!.GetValue<int>();

        // The first iteration is open and cannot be closed empty.
        await Refused(client.PostAsync($"/projects/{id}/iterations/1/close", null), "Sube al menos un documento antes de cerrar la documentación.");
        var first = await Upload(client, id, "acta.md", "# Acta\n\nEl sistema guarda las citas.");
        Assert.Equal(1, await IterationNumber(client, id, first));
        (await client.PostAsync($"/projects/{id}/iterations/1/close", null)).EnsureSuccessStatusCode();

        // With every iteration closed, a document is refused; a style reference is not.
        using (var form = File("otra.md", "Otra cosa."u8.ToArray()))
            await Refused(client.PostAsync($"/projects/{id}/files", form), "La documentación está cerrada: crea una iteración nueva para subir documentos.", "file");
        var style = await Upload(client, id, "estilo.zip", StyleZip());
        Assert.Null(style["iterationId"]);

        // A second one, with notes; only one open at a time.
        var second = await Json(await client.PostAsJsonAsync($"/projects/{id}/iterations", new { name = "", notes = "Reunión del 15-11" }));
        Assert.Equal((2, "Iteración 2", "Reunión del 15-11", "Open"),
            (second["number"]!.GetValue<int>(), second["name"]!.GetValue<string>(), second["notes"]!.GetValue<string>(), second["status"]!.GetValue<string>()));
        await Refused(client.PostAsJsonAsync($"/projects/{id}/iterations", new { name = "Otra" }), "La iteración 2 sigue abierta: cierra su documentación antes de crear otra.");
        var renamed = await Json(await client.PutAsJsonAsync($"/projects/{id}/iterations/2", new { name = " Reunión con el cliente ", notes = "" }));
        Assert.Equal("Reunión con el cliente", renamed["name"]!.GetValue<string>());
        Assert.Null(renamed["notes"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/projects/{id}/iterations/2", new { name = " " })).StatusCode);
        Assert.Equal(2, await IterationNumber(client, id, await Upload(client, id, "acta-2.md", "# Acta 2\n\nTambién recordatorios.")));

        // The documents are still numbered by upload order in the whole project.
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var documents = await PhaseJobs.LoadDocumentsAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(),
                scope.ServiceProvider.GetRequiredService<FileStorage>(), id);
            Assert.Equal([("DOC-01", "acta.md"), ("DOC-02", "acta-2.md")], documents.Select(d => (d.Id, d.Name)));
        }

        // Only the last can be reopened, and none is ever deleted with documents.
        (await client.PostAsync($"/projects/{id}/iterations/2/close", null)).EnsureSuccessStatusCode();
        await Refused(client.PostAsync($"/projects/{id}/iterations/1/reopen", null), "Solo se puede reabrir la última iteración.");
        await Refused(client.DeleteAsync($"/projects/{id}/iterations/2"), "Solo se puede eliminar la última iteración, mientras está abierta.");
        Assert.Equal("Open", (await Json(await client.PostAsync($"/projects/{id}/iterations/2/reopen", null)))["status"]!.GetValue<string>());
        await Refused(client.DeleteAsync($"/projects/{id}/iterations/2"), "La iteración tiene documentos: no se puede eliminar.");
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/projects/{id}/iterations/9/close", null)).StatusCode);
    }

    [Fact]
    public async Task An_iteration_created_by_mistake_can_be_deleted()
    {
        using var temp = new TempDirectory();
        await using var api = Api(temp.Path);
        var client = api.CreateClient();
        var id = (await Json(await client.PostAsJsonAsync("/projects", new { name = "Clínicas" })))["id"]!.GetValue<int>();
        await Upload(client, id, "acta.md", "# Acta");
        (await client.PostAsync($"/projects/{id}/iterations/1/close", null)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/projects/{id}/iterations", new { name = (string?)null })).EnsureSuccessStatusCode();

        Assert.Equal("Cierra la documentación de la iteración 2.", (await Json(await client.GetAsync($"/projects/{id}/flow")))["documentationOpen"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/projects/{id}/iterations/2")).StatusCode);

        var flow = await Json(await client.GetAsync($"/projects/{id}/flow"));
        Assert.Null(flow["documentationOpen"]);
        Assert.True(flow["steps"]!.AsArray().Single(s => s!["step"]!.GetValue<string>() == "Questions")!["canLaunch"]!.GetValue<bool>());
        Assert.Single((await Json(await client.GetAsync($"/projects/{id}/iterations"))).AsArray());
    }

    // Reopening outdates the same as uploading a file; and while it is open,
    // the suggestions are not launched either.
    [Fact]
    public async Task Reopening_outdates_like_a_new_file_and_stops_the_suggestions()
    {
        using var temp = new TempDirectory();
        await using var api = Api(temp.Path);
        var client = api.CreateClient();
        var id = (await Json(await client.PostAsJsonAsync("/projects", new { name = "Clínicas" })))["id"]!.GetValue<int>();
        await Upload(client, id, "acta.md", "# Acta");
        (await client.PostAsync($"/projects/{id}/iterations/1/close", null)).EnsureSuccessStatusCode();
        int questionId;
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var job = Guid.NewGuid();
            db.Deliverables.Add(new Deliverable { ProjectId = id, Kind = DeliverableKind.Catalog, JobId = job, Status = DeliverableStatus.Validated, Content = "{}" });
            var question = new Question { ProjectId = id, JobId = job, Code = "P-001", Text = "¿Hay recordatorios?", Reason = "RF-001", Origin = "ambas" };
            db.Questions.Add(question);
            await db.SaveChangesAsync();
            questionId = question.Id;
        }

        (await client.PostAsync($"/projects/{id}/iterations/1/reopen", null)).EnsureSuccessStatusCode();

        var catalog = (await Json(await client.GetAsync($"/projects/{id}/flow")))["steps"]!.AsArray().Single(s => s!["step"]!.GetValue<string>() == "Catalog")!;
        Assert.True(catalog["outdated"]!.GetValue<bool>());
        Assert.Equal("Cierra la documentación de la iteración 1.", catalog["reason"]!.GetValue<string>());
        await Refused(client.PostAsync($"/questions/{questionId}/suggestion", null), "Cierra la documentación de la iteración 1.", "project");
        await Refused(client.PostAsync($"/projects/{id}/catalog-jobs", null), "Cierra la documentación de la iteración 1.", "project");
    }

    // A project from before the iterations stays as it was, in its
    // "Iteración 1": the same documents, numbered and named the same, and the
    // same flow. The style .zip stays out; an empty project gets it open; a
    // project created after the start is not touched; and the next start does nothing.
    [Fact]
    public async Task The_projects_from_before_get_their_first_iteration_once_and_stay_as_they_were()
    {
        using var temp = new TempDirectory();
        var storage = new FileStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = temp.Path }).Build(),
            new HostingEnvironment { ContentRootPath = temp.Path });
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var start = new DateTimeOffset(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);
        var done = new Project { Name = "Clínicas", SystemName = "el sistema Clínicas", CreatedAt = start.AddDays(-10) };
        var empty = new Project { Name = "Vacío", SystemName = "el sistema Vacío", CreatedAt = start.AddDays(-5) };
        var created = new Project { Name = "Nuevo", SystemName = "el sistema Nuevo", CreatedAt = start.AddSeconds(1) };
        db.Projects.AddRange(done, empty, created);
        await db.SaveChangesAsync();
        Directory.CreateDirectory(storage.ProjectFolder(done.Id));
        ProjectFile Stored(string name, string? text, int day)
        {
            string? textName = null;
            if (text is not null)
            {
                textName = $"{Guid.NewGuid():N}.extracted.txt";
                System.IO.File.WriteAllText(Path.Combine(storage.ProjectFolder(done.Id), textName), text);
            }
            return new ProjectFile { ProjectId = done.Id, FileName = name, StoredName = name, ContentType = "text/plain", Sha256 = "", UploadedAt = start.AddDays(-day), TextStoredName = textName };
        }
        db.ProjectFiles.AddRange(Stored("b.md", "# B", 7), Stored("a.docx", "# A", 8), Stored("estilo.zip", null, 6), Stored("c.txt", "C", 3));
        await db.SaveChangesAsync();
        var documentsBefore = await PhaseJobs.LoadDocumentsAsync(db, storage, done.Id);
        var flowBefore = ProjectFlow.Compute(await FlowEndpoints.ReadFactsAsync(db, storage, done.Id));
        var emptyFlowBefore = ProjectFlow.Compute(await FlowEndpoints.ReadFactsAsync(db, storage, empty.Id));

        await IterationEndpoints.CreateFirstForExistingAsync(db, start);
        await IterationEndpoints.CreateFirstForExistingAsync(db, start); // the next start-up adds nothing

        var iteration = await db.Iterations.SingleAsync(i => i.ProjectId == done.Id);
        Assert.Equal((1, "Iteración 1", IterationStatus.Closed, done.CreatedAt, start.AddDays(-3)),
            (iteration.Number, iteration.Name, iteration.Status, iteration.CreatedAt, iteration.ClosedAt));
        var files = await db.ProjectFiles.Where(f => f.ProjectId == done.Id).ToListAsync();
        Assert.All(files.Where(f => f.FileName != "estilo.zip"), f => Assert.Equal(iteration.Id, f.IterationId));
        Assert.Null(files.Single(f => f.FileName == "estilo.zip").IterationId);
        Assert.Equal(IterationStatus.Open, (await db.Iterations.SingleAsync(i => i.ProjectId == empty.Id)).Status);
        Assert.False(await db.Iterations.AnyAsync(i => i.ProjectId == created.Id));
        Assert.Equal(2, await db.Iterations.CountAsync());

        var documentsAfter = await PhaseJobs.LoadDocumentsAsync(db, storage, done.Id);
        Assert.Equal([("DOC-01", "a.docx"), ("DOC-02", "b.md"), ("DOC-03", "c.txt")], documentsAfter.Select(d => (d.Id, d.Name)));
        Assert.Equal(documentsBefore, documentsAfter);
        Assert.Equal(flowBefore, ProjectFlow.Compute(await FlowEndpoints.ReadFactsAsync(db, storage, done.Id)));
        Assert.Equal(emptyFlowBefore, ProjectFlow.Compute(await FlowEndpoints.ReadFactsAsync(db, storage, empty.Id)));
    }

    private static async Task<JsonNode> Upload(HttpClient client, int projectId, string name, string text) =>
        await Upload(client, projectId, name, System.Text.Encoding.UTF8.GetBytes(text));

    private static async Task<JsonNode> Upload(HttpClient client, int projectId, string name, byte[] content)
    {
        using var form = File(name, content);
        return await Json(await client.PostAsync($"/projects/{projectId}/files", form));
    }

    private static MultipartFormDataContent File(string name, byte[] content) =>
        new() { { new ByteArrayContent(content), "file", name } };

    private static async Task<int> IterationNumber(HttpClient client, int projectId, JsonNode file)
    {
        var iterations = (await Json(await client.GetAsync($"/projects/{projectId}/iterations"))).AsArray();
        return iterations.Single(i => i!["id"]!.GetValue<int>() == file["iterationId"]!.GetValue<int>())!["number"]!.GetValue<int>();
    }

    // A .zip with a stylesheet, valid as a style reference.
    private static byte[] StyleZip()
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create))
        using (var writer = new StreamWriter(zip.CreateEntry("estilo.css").Open()))
            writer.Write("body { color: navy; }");
        return buffer.ToArray();
    }

    // A 400 with this message in the field.
    private static async Task Refused(Task<HttpResponseMessage> request, string message, string field = "iteration")
    {
        var response = await request;
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{response.StatusCode} {text}");
        Assert.Equal(message, JsonNode.Parse(text)!["errors"]![field]![0]!.GetValue<string>());
    }

    private static async Task<JsonNode> Json(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.RequestUri}: {response.StatusCode} {text}");
        return JsonNode.Parse(text)!;
    }
}
