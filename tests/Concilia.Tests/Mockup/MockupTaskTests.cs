using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using Concilia.Api.Files;
using Concilia.Api.Runs;
using Concilia.Core.Catalog;
using Concilia.Core.Deliverables;
using Concilia.Core.Flow;
using Concilia.Core.Mockup;
using Concilia.Core.Runner;
using Concilia.Core.Runs;

namespace Concilia.Tests.Mockup;

// Step 60: the checks of the mockup .html, the page FakeProvider writes and a
// mockup job with its repair, with the backlog of the ReservaSalas example.
public class MockupTaskTests
{
    private static string Example(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file));

    public static string ExampleBacklog() => Example("backlog_arbitro.json");

    // The validated backlog as the mockup receives it.
    private static readonly string Backlog = BacklogDraft.ForNextPhase(Example("backlog_arbitro.json"));

    private static readonly MockupPlan Plan = MockupPlan.From("el sistema ReservaSalas", "es", Backlog);

    // A page that passes every check: one view per story of the plan.
    private static string Valid(string extra = "") =>
        "<!doctype html><html><body>"
        + string.Concat(Plan.StoryIds.Select(id => $"<section data-hu=\"{id}\"><a href=\"#{id}\">{id}</a></section>"))
        + extra
        + "<script>const DATOS = { salas: [] };</script></body></html>";

    [Fact]
    public void The_plan_has_the_stories_of_the_backlog_in_order()
    {
        Assert.NotEmpty(Plan.StoryIds);
        Assert.Equal("HU-001", Plan.StoryIds[0]);
        Assert.Equal(Plan.StoryIds.Count, Plan.StoryIds.Distinct().Count());
    }

    [Fact]
    public void The_backlog_goes_to_a_file_the_model_reads_entirely()
    {
        using var temp = new TempDirectory();

        var request = MockupTask.BuildRequest(Plan, Guid.NewGuid(), "claude-sonnet-5-5", temp.Path, new MockupRunOptions());

        Assert.Contains("<nombre_sistema>el sistema ReservaSalas</nombre_sistema>", request.Prompt);
        Assert.Contains("- `input/backlog.json` (", request.Prompt);
        var firstTitle = JsonNode.Parse(Backlog)!["epicas"]![0]!["historias"]![0]!["titulo"]!.GetValue<string>();
        Assert.DoesNotContain(firstTitle, request.Prompt);
        Assert.DoesNotContain("{{", request.Prompt);
        Assert.Equal(["Read", "Write"], request.AllowedTools);
        Assert.False(request.InputFilesInBudget);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(Backlog), JsonNode.Parse(File.ReadAllText(Path.Combine(temp.Path, MockupTask.BacklogFile)))));
    }

    [Fact]
    public void A_page_with_every_view_is_valid()
    {
        var result = MockupTask.Validate(Valid(), Plan.StoryIds);

        Assert.True(result.Valid, string.Join(" | ", result.Errors));
        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData("<script src=\"https://cdn.invalid/x.js\"></script>")]
    [InlineData("<link rel=\"stylesheet\" href=\"//fonts.invalid/f.css\">")]
    [InlineData("<img src=http://img.invalid/a.png>")]
    [InlineData("<style>body { background: url('https://img.invalid/f.png'); }</style>")]
    [InlineData("<style>@import \"https://fonts.invalid/f.css\";</style>")]
    public void An_external_resource_is_an_error(string extra)
    {
        var result = MockupTask.Validate(Valid(extra), Plan.StoryIds);

        Assert.StartsWith("output/mockup.html EXTERNO:", Assert.Single(result.Errors));
    }

    [Fact]
    public void Each_rule_has_its_code()
    {
        var withoutLastView = Valid().Replace($"data-hu=\"{Plan.StoryIds[^1]}\"", "");

        Assert.Equal(["output/mockup.html FICHERO: no se ha escrito el fichero."], MockupTask.Validate(null, Plan.StoryIds).Errors);
        Assert.Contains("DOCTYPE", Assert.Single(MockupTask.Validate(Valid()[15..], Plan.StoryIds).Errors));
        Assert.Contains("DATOS", Assert.Single(MockupTask.Validate(Valid().Replace("const DATOS", "let datos"), Plan.StoryIds).Errors));
        Assert.Equal([$"{Plan.StoryIds[^1]} HU: ninguna vista lleva data-hu=\"{Plan.StoryIds[^1]}\"."], MockupTask.Validate(withoutLastView, Plan.StoryIds).Errors);
        Assert.Contains("TAMANO", Assert.Single(MockupTask.Validate(Valid(new string('x', MockupTask.MaxBytes)), Plan.StoryIds).Errors));
    }

    [Fact]
    public void A_view_of_a_story_that_is_not_in_the_backlog_is_a_warning()
    {
        var result = MockupTask.Validate(Valid("<section data-hu=\"HU-999\"></section><script>const t = `<div data-hu=\"${h[0]}\">`;</script>"), Plan.StoryIds);

        Assert.True(result.Valid);
        // The one built in JavaScript is not a view.
        Assert.Equal(["HU-999 HU-EXTRA: hay una vista para una historia que no está en el backlog."], result.Warnings);
    }

    [Fact]
    public void The_repair_sends_the_errors_and_keeps_the_parts()
    {
        var review = MockupTask.Review(Valid().Replace("const DATOS", "let datos"), Plan.StoryIds, "PROMPT", 1);

        Assert.StartsWith("PROMPT", review.RepairPrompt);
        Assert.Contains("DATOS: falta", review.RepairPrompt);
        Assert.Contains("Tus ficheros siguen en `output/mockup/`", review.RepairPrompt);
    }

    // Prompts v0.4: the page is written by parts and the platform joins them.
    private static string WriteBase(string folder, string body = MockupTask.Placeholder)
    {
        Directory.CreateDirectory(Path.Combine(folder, MockupTask.PartsFolder));
        var page = $"<!doctype html><html><head><script>const DATOS = {{}};</script></head><body>{body}</body></html>";
        File.WriteAllText(Path.Combine(folder, MockupTask.BaseFile), page);
        return page;
    }

    private static void WriteEpic(string folder, string epicId, IEnumerable<string> storyIds, string extra = "") =>
        File.WriteAllText(Path.Combine(folder, MockupTask.EpicFile(epicId)),
            string.Concat(storyIds.Select(id => $"<section data-hu=\"{id}\"></section>")) + extra);

    [Fact]
    public void The_parts_are_joined_in_the_order_of_the_backlog()
    {
        using var temp = new TempDirectory();
        WriteBase(temp.Path);
        var epics = MockupTask.Epics(Backlog);
        foreach (var (id, stories) in epics.Reverse())
            WriteEpic(temp.Path, id, stories);

        var errors = MockupTask.Assemble(temp.Path, Plan);

        Assert.Empty(errors);
        var html = File.ReadAllText(Path.Combine(temp.Path, MockupTask.OutputFile));
        Assert.True(MockupTask.Validate(html, Plan.StoryIds).Valid);
        Assert.DoesNotContain(MockupTask.Placeholder, html);
        Assert.True(html.IndexOf($"<!-- {epics[0].Id} -->") < html.IndexOf($"<!-- {epics[^1].Id} -->"));
        // The prompt lists the file of each epic with its stories.
        Assert.Contains($"`{MockupTask.EpicFile(epics[0].Id)}`: {string.Join(", ", epics[0].StoryIds)}", MockupTask.Prompt(Plan));
    }

    [Fact]
    public void An_image_of_the_style_reference_goes_inside_and_does_not_count_for_the_size()
    {
        using var temp = new TempDirectory();
        var image = Path.Combine(temp.Path, StyleReference.Folder, "cliente", "fondo.png");
        Directory.CreateDirectory(Path.GetDirectoryName(image)!);
        File.WriteAllBytes(image, new byte[400 * 1024]);
        WriteBase(temp.Path, body: $"<img src=\"input/estilo/cliente/fondo.png\"><img src='input/estilo/cliente/fondo.png'>{MockupTask.Placeholder}");
        foreach (var (id, stories) in MockupTask.Epics(Backlog))
            WriteEpic(temp.Path, id, stories);

        Assert.Empty(MockupTask.Assemble(temp.Path, Plan));

        var html = File.ReadAllText(Path.Combine(temp.Path, MockupTask.OutputFile));
        Assert.True(html.Length > MockupTask.MaxBytes);
        Assert.DoesNotContain("input/estilo", html);
        Assert.True(MockupTask.Validate(html, Plan.StoryIds).Valid);
    }

    [Fact]
    public void Each_problem_of_the_parts_has_its_code()
    {
        using var temp = new TempDirectory();
        var epics = MockupTask.Epics(Backlog);

        Assert.Equal(["output/mockup/base.html FICHERO: no se ha escrito la base del prototipo."], MockupTask.Assemble(temp.Path, Plan));

        WriteBase(temp.Path, body: "sin la línea");
        WriteEpic(temp.Path, epics[0].Id, epics[0].StoryIds, extra: "<html><body></body></html>");
        var errors = MockupTask.Assemble(temp.Path, Plan);

        Assert.Contains(errors, e => e.StartsWith("output/mockup/base.html BASE:"));
        Assert.Contains(errors, e => e.StartsWith($"{epics[0].Id} FRAGMENTO:"));
        Assert.Contains($"{epics[^1].Id} FICHERO: falta output/mockup/{epics[^1].Id}.html con las vistas de sus historias.", errors);
    }

    [Fact]
    public void Without_a_base_only_the_errors_of_the_parts_are_reported()
    {
        var review = MockupTask.Review(null, Plan.StoryIds, "PROMPT", 1, ["output/mockup/base.html FICHERO: no se ha escrito la base del prototipo."]);

        Assert.Equal(["output/mockup/base.html FICHERO: no se ha escrito la base del prototipo."], review.Validation.Errors);
    }

    [Fact]
    public void The_fake_parts_join_into_a_valid_page_that_escapes_the_texts()
    {
        using var temp = new TempDirectory();
        var withTags = Backlog.Replace("\"titulo\": \"", "\"titulo\": \"<b>");
        var plan = MockupPlan.From("s", "es", withTags);
        MockupTask.BuildRequest(plan, Guid.NewGuid(), "m", temp.Path, new MockupRunOptions());

        File.WriteAllText(Path.Combine(temp.Path, MockupTask.BaseFile), FakeMockup.WriteParts(temp.Path, broken: false));
        Assert.Empty(MockupTask.Assemble(temp.Path, plan));

        var html = File.ReadAllText(Path.Combine(temp.Path, MockupTask.OutputFile));
        Assert.True(MockupTask.Validate(html, Plan.StoryIds).Valid);
        Assert.Contains("&lt;b&gt;", html);
        Assert.DoesNotContain("<b>", html);
    }

    [Fact]
    public void Generating_the_mockup_outdates_nothing_and_a_new_backlog_outdates_it()
    {
        var mockup = new Deliverable { Kind = DeliverableKind.Mockup, Content = "{}" };

        var (_, change) = DeliverableRules.Generated(null, 1, DeliverableKind.Mockup, Guid.NewGuid(), "{}", DateTimeOffset.UtcNow);
        DeliverableRules.MarkOutdated([mockup], change);
        Assert.Equal(FlowChange.MockupGenerated, change);
        Assert.False(mockup.Outdated);

        DeliverableRules.MarkOutdated([mockup], FlowChange.BacklogReopened);
        Assert.True(mockup.Outdated);
    }

    // The job without the database: one run with FakeProvider, which leaves
    // out the file of the last epic in its first attempt, so the repair
    // writes it (the other parts are carried to the repair's folder).
    [Fact]
    public async Task A_mockup_job_runs_once_and_is_repaired_once()
    {
        using var temp = new TempDirectory();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = temp.Path })
            .Build();
        var storage = new FileStorage(configuration, new HostingEnvironment { ContentRootPath = temp.Path });
        var provider = new FakeProvider(simulateRepair: true);
        var events = new RunEventStore();
        var runner = new JobRunner(
            new RunExecutor(provider, events, storage, new RunnerOptions(), new CatalogOptions()), new CatalogOptions(),
            mockupExecutor: new MockupExecutor(provider, events, storage, new MockupOptions()));
        var run = new Run { Id = Guid.NewGuid(), Slot = "MOCK", Provider = "Fake", Model = "claude-sonnet-5-5", State = RunState.Queued };

        var outcome = await runner.RunMockupAsync(run, Plan, _ => Task.CompletedTask, _ => Task.CompletedTask, CancellationToken.None);

        Assert.True(outcome.Succeeded);
        var attempts = outcome.Attempts!;
        Assert.Equal([RunStatus.Failed, RunStatus.Done], attempts.Select(a => a.Result.Status));
        var lastEpic = MockupTask.Epics(Backlog)[^1].Id;
        Assert.Contains($"{lastEpic} FICHERO: falta", attempts[0].Result.ErrorMessage);
        Assert.Equal("claude-sonnet-5-5", attempts[0].Request.ModelId);
        Assert.Equal(["input", MockupTask.PartsFolder], attempts[0].Request.CarriedFolders);
        var html = File.ReadAllText(MockupExecutor.OutputPath(attempts[^1]));
        Assert.True(MockupTask.Validate(html, Plan.StoryIds).Valid);
    }
}
