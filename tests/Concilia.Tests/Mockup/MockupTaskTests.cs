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

// The mockup as one application built in several runs (prompts v0.6): the
// map and the shell, each screen and the integration, with their checks, the
// joined page and a whole job with FakeProvider, on the backlog of the
// ReservaSalas example.
public class MockupTaskTests
{
    private static string Example(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file));

    public static string ExampleBacklog() => Example("backlog_arbitro.json");

    // The validated backlog as the mockup receives it.
    private static readonly string Backlog = BacklogDraft.ForNextPhase(ExampleBacklog());

    private static readonly MockupPlan Plan = MockupPlan.From("el sistema ReservaSalas", "es", Backlog);

    // A joined page that passes every check: two screens that share the stories.
    private static string Valid(string extra = "")
    {
        var half = Plan.StoryIds.Count / 2;
        return "<!doctype html><html><body>"
            + $"<section data-pantalla=\"P01\"><div data-hu=\"{string.Join(" ", Plan.StoryIds.Take(half))}\"></div></section>"
            + string.Concat(Plan.StoryIds.Skip(half).Select(id => $"<section data-pantalla=\"P02\"><a data-hu=\"{id}\" href=\"#{id}\">{id}</a></section>"))
            + extra
            + "<script>const DATOS = { salas: [] };</script></body></html>";
    }

    private static string MapJson(params (string Id, string[] Stories)[] screens) =>
        new JsonObject
        {
            ["pantallas"] = new JsonArray(screens.Select(s => (JsonNode)new JsonObject
            {
                ["id"] = s.Id,
                ["titulo"] = $"Pantalla {s.Id}",
                ["descripcion"] = "…",
                ["historias"] = new JsonArray(s.Stories.Select(h => (JsonNode)h).ToArray()),
            }).ToArray()),
        }.ToJsonString();

    private static string Work(TempDirectory temp, string name) => Path.Combine(temp.Path, name);

    // A job's folder with what the design leaves (FakeMockup: one screen per epic).
    private static string JobAfterDesign(TempDirectory temp)
    {
        var job = Work(temp, "job");
        var design = Work(temp, "design");
        MockupTask.BuildRequest(Plan, new MockupStep(MockupStage.Design), job, Guid.NewGuid(), "m", design, new MockupRunOptions());
        Directory.CreateDirectory(Path.Combine(design, MockupTask.PrototypeFolder));
        File.WriteAllText(Path.Combine(design, MockupTask.MapFile), FakeMockup.Map(design));
        File.WriteAllText(Path.Combine(design, MockupTask.AppFile), FakeMockup.App(design));
        MockupTask.Keep(new MockupStep(MockupStage.Design), design, job);
        return job;
    }

    private static MockupMap MapOf(string job) =>
        MockupMap.Load(File.ReadAllText(Path.Combine(MockupTask.JobPrototypeFolder(job), "mapa.json")), Plan.StoryIds);

    [Fact]
    public void The_plan_has_the_stories_of_the_backlog_in_order()
    {
        Assert.NotEmpty(Plan.StoryIds);
        Assert.Equal("HU-001", Plan.StoryIds[0]);
        Assert.Equal(Plan.StoryIds.Count, Plan.StoryIds.Distinct().Count());
    }

    // ---- The map ----

    [Fact]
    public void A_map_with_every_story_is_valid_and_a_story_can_be_in_two_screens()
    {
        var (map, errors) = MockupMap.Parse(MapJson(("P01", [.. Plan.StoryIds]), ("P02", [Plan.StoryIds[0]])), Plan.StoryIds);

        Assert.Empty(errors);
        Assert.Equal(["P01", "P02"], map!.Screens.Select(s => s.Id));
    }

    [Fact]
    public void Each_problem_of_the_map_has_its_code()
    {
        var errors = MockupMap.Parse(MapJson(("P1", ["HU-999"]), ("P02", [.. Plan.StoryIds.Skip(1)]), ("P02", [Plan.StoryIds[1]])), Plan.StoryIds).Errors;

        Assert.Contains("P1 MAPA-ID: el id de una pantalla es P01, P02…", errors);
        Assert.Contains("P02 MAPA-ID: hay dos pantallas con este id.", errors);
        Assert.Contains("P1 MAPA-HU: HU-999 no es una historia del backlog.", errors);
        Assert.Contains($"{Plan.StoryIds[0]} MAPA-HU: la historia no está en ninguna pantalla.", errors);
        Assert.StartsWith("output/prototipo/mapa.json JSON:", Assert.Single(MockupMap.Parse("{", Plan.StoryIds).Errors));
        Assert.StartsWith("output/prototipo/mapa.json MAPA:", Assert.Single(MockupMap.Parse("{\"pantallas\":[]}", Plan.StoryIds).Errors));
    }

    // ---- The requests of each step ----

    [Fact]
    public void The_design_reads_the_whole_backlog_and_writes_the_map_and_the_shell()
    {
        using var temp = new TempDirectory();
        var work = Work(temp, "run");

        var built = MockupTask.BuildRequest(Plan, new MockupStep(MockupStage.Design), Work(temp, "job"), Guid.NewGuid(), "claude-sonnet-5-5", work, new MockupRunOptions());

        var request = built.Request;
        Assert.Contains("<nombre_sistema>el sistema ReservaSalas</nombre_sistema>", request.Prompt);
        Assert.Contains("- `input/backlog.json` (", request.Prompt);
        Assert.Contains(MockupTask.NoStyle, request.Prompt);
        Assert.DoesNotContain("{{", request.Prompt);
        Assert.Equal([MockupTask.MapFile, MockupTask.AppFile], request.ExpectedOutputFiles);
        Assert.Equal(["Read", "Write"], request.AllowedTools);
        Assert.Equal(["input", MockupTask.PrototypeFolder], request.CarriedFolders);
        Assert.Equal([MockupTask.BacklogFile], built.RequiredReads);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(Backlog), JsonNode.Parse(File.ReadAllText(Path.Combine(work, MockupTask.BacklogFile)))));
    }

    [Fact]
    public void A_screen_reads_the_map_the_shell_and_only_its_stories()
    {
        using var temp = new TempDirectory();
        var job = JobAfterDesign(temp);
        var screen = MapOf(job).Screens[0];
        var work = Work(temp, "screen");

        var built = MockupTask.BuildRequest(Plan, new MockupStep(MockupStage.Screen, screen), job, Guid.NewGuid(), "m", work, new MockupRunOptions());

        Assert.Equal([MockupTask.MapInput, MockupTask.AppInput, MockupTask.StoriesInput], built.RequiredReads);
        Assert.Contains($"**{screen.Id} «{screen.Title}»**", built.Request.Prompt);
        Assert.Contains($"`{MockupTask.ScreenFile(screen.Id)}`", built.Request.Prompt);
        Assert.DoesNotContain("{{", built.Request.Prompt);
        Assert.Equal([MockupTask.ScreenFile(screen.Id)], built.Request.ExpectedOutputFiles);
        var stories = JsonNode.Parse(File.ReadAllText(Path.Combine(work, MockupTask.StoriesInput)))!["historias"]!.AsArray();
        Assert.Equal(screen.StoryIds, stories.Select(h => h!["id"]!.GetValue<string>()));
        Assert.StartsWith("EP-", stories[0]!["epica"]!.GetValue<string>());
        Assert.Equal(FakeMockup.App(Work(temp, "design")), File.ReadAllText(Path.Combine(work, MockupTask.AppInput)));
    }

    [Fact]
    public void The_integration_reads_a_copy_of_the_whole_prototype_and_edits_the_files()
    {
        using var temp = new TempDirectory();
        var job = JobAfterDesign(temp);
        var screens = MapOf(job).Screens;
        Directory.CreateDirectory(Path.Combine(MockupTask.JobPrototypeFolder(job), "pantallas"));
        foreach (var screen in screens)
            File.WriteAllText(Path.Combine(MockupTask.JobPrototypeFolder(job), "pantallas", $"{screen.Id}.html"), "<section></section>");
        var work = Work(temp, "integration");

        var built = MockupTask.BuildRequest(Plan, new MockupStep(MockupStage.Integration), job, Guid.NewGuid(), "m", work, new MockupRunOptions());

        Assert.Equal(["input/prototipo/mapa.json", "input/prototipo/app.html", .. screens.Select(s => $"input/prototipo/pantallas/{s.Id}.html")],
            built.RequiredReads);
        Assert.Equal(["Read", "Write", "Edit"], built.Request.AllowedTools);
        Assert.Empty(built.Request.ExpectedOutputFiles);
        Assert.True(File.Exists(Path.Combine(work, MockupTask.ScreenFile(screens[0].Id))));
        Assert.Contains("- `input/backlog.json` (", built.Request.Prompt); // only to consult
        Assert.DoesNotContain("{{", built.Request.Prompt);
    }

    // ---- The checks ----

    [Fact]
    public void Each_problem_of_a_screen_has_its_code()
    {
        using var temp = new TempDirectory();
        var screen = new MockupScreen("P02", "Reservas", "", ["HU-001", "HU-002"]);
        var file = Path.Combine(temp.Path, MockupTask.ScreenFile("P02"));
        var step = new MockupStep(MockupStage.Screen, screen);

        Assert.Equal(["output/prototipo/pantallas/P02.html FICHERO: no se ha escrito la pantalla."], MockupTask.Check(step, Plan, temp.Path).Errors);

        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "<html><body><section data-pantalla=\"P01\"><p data-hu=\"HU-001\"></p></section></body></html>");
        var errors = MockupTask.Check(step, Plan, temp.Path).Errors;

        Assert.Contains(errors, e => e.StartsWith("P02 PANTALLA:"));
        Assert.Contains(errors, e => e.StartsWith("P02 FRAGMENTO:"));
        Assert.Equal(["HU-002 HU: ningún elemento de la pantalla lleva data-hu=\"HU-002\"."], errors.Where(e => e.Contains(" HU:")));

        File.WriteAllText(file, "<section data-pantalla=\"P02\"><p data-hu=\"HU-001 HU-002\"></p></section>");
        Assert.True(MockupTask.Check(step, Plan, temp.Path).Valid);
    }

    [Fact]
    public void A_joined_page_with_every_story_and_screen_is_valid()
    {
        var result = MockupTask.Validate(Valid(), Plan.StoryIds, ["P01", "P02"]);

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
    public void Each_rule_of_the_page_has_its_code()
    {
        var last = Plan.StoryIds[^1];
        var withoutLast = Valid().Replace($"data-hu=\"{last}\"", "");

        Assert.Equal(["output/mockup.html FICHERO: no se ha escrito el fichero."], MockupTask.Validate(null, Plan.StoryIds).Errors);
        Assert.Contains("DOCTYPE", Assert.Single(MockupTask.Validate(Valid()[15..], Plan.StoryIds).Errors));
        Assert.Contains("DATOS", Assert.Single(MockupTask.Validate(Valid().Replace("const DATOS", "let datos"), Plan.StoryIds).Errors));
        Assert.Equal([$"{last} HU: ningún elemento lleva data-hu=\"{last}\"."], MockupTask.Validate(withoutLast, Plan.StoryIds).Errors);
        Assert.Equal(["P03 PANTALLA: no hay ningún elemento con data-pantalla=\"P03\"."], MockupTask.Validate(Valid(), Plan.StoryIds, ["P01", "P03"]).Errors);
        Assert.Contains("TAMANO", Assert.Single(MockupTask.Validate(Valid(new string('x', MockupTask.MaxBytes)), Plan.StoryIds).Errors));
    }

    [Fact]
    public void A_mark_of_a_story_that_is_not_in_the_backlog_is_a_warning()
    {
        var result = MockupTask.Validate(Valid("<div data-hu=\"HU-999\"></div><script>const t = `<div data-hu=\"${h[0]}\">`;</script>"), Plan.StoryIds);

        Assert.True(result.Valid);
        // The one built in JavaScript is not a mark.
        Assert.Equal(["HU-999 HU-EXTRA: un elemento lleva el id de una historia que no está en el backlog."], result.Warnings);
    }

    // ---- Joining ----

    private static MockupMap TwoScreens() => MockupMap.Load(MapJson(("P01", [.. Plan.StoryIds]), ("P02", [Plan.StoryIds[0]])), Plan.StoryIds);

    private static void WriteApp(string folder, string body = MockupTask.Placeholder)
    {
        Directory.CreateDirectory(Path.Combine(folder, MockupTask.PrototypeFolder, "pantallas"));
        File.WriteAllText(Path.Combine(folder, MockupTask.AppFile),
            $"<!doctype html><html><head><script>const DATOS = {{}};</script></head><body>{body}</body></html>");
    }

    private static void WriteScreen(string folder, string id, IEnumerable<string> storyIds, string extra = "") =>
        File.WriteAllText(Path.Combine(folder, MockupTask.ScreenFile(id)),
            $"<section data-pantalla=\"{id}\">" + string.Concat(storyIds.Select(h => $"<p data-hu=\"{h}\"></p>")) + extra + "</section>");

    [Fact]
    public void The_screens_are_joined_in_the_order_of_the_map()
    {
        using var temp = new TempDirectory();
        WriteApp(temp.Path);
        WriteScreen(temp.Path, "P02", [Plan.StoryIds[0]]);
        WriteScreen(temp.Path, "P01", Plan.StoryIds);

        Assert.Empty(MockupTask.Assemble(temp.Path, TwoScreens()));

        var html = File.ReadAllText(Path.Combine(temp.Path, MockupTask.OutputFile));
        Assert.True(MockupTask.Validate(html, Plan.StoryIds, ["P01", "P02"]).Valid);
        Assert.DoesNotContain(MockupTask.Placeholder, html);
        Assert.True(html.IndexOf("<!-- P01 -->") < html.IndexOf("<!-- P02 -->"));
    }

    [Fact]
    public void Each_problem_of_the_parts_has_its_code()
    {
        using var temp = new TempDirectory();

        Assert.Equal(["output/prototipo/app.html FICHERO: falta el armazón de la aplicación."], MockupTask.Assemble(temp.Path, TwoScreens()));

        WriteApp(temp.Path, body: "sin la línea");
        WriteScreen(temp.Path, "P01", Plan.StoryIds, extra: "<html><body></body></html>");
        var errors = MockupTask.Assemble(temp.Path, TwoScreens());

        Assert.Contains(errors, e => e.StartsWith("output/prototipo/app.html BASE:"));
        Assert.Contains(errors, e => e.StartsWith("P01 FRAGMENTO:"));
        Assert.Contains("P02 FICHERO: falta output/prototipo/pantallas/P02.html.", errors);
    }

    [Fact]
    public void An_image_of_the_style_reference_goes_inside_and_does_not_count_for_the_size()
    {
        using var temp = new TempDirectory();
        var image = Path.Combine(temp.Path, StyleReference.Folder, "cliente", "fondo.png");
        Directory.CreateDirectory(Path.GetDirectoryName(image)!);
        File.WriteAllBytes(image, new byte[400 * 1024]);
        WriteApp(temp.Path, body: $"<img src=\"input/estilo/cliente/fondo.png\"><img src='input/estilo/cliente/fondo.png'>{MockupTask.Placeholder}");
        WriteScreen(temp.Path, "P01", Plan.StoryIds);
        WriteScreen(temp.Path, "P02", [Plan.StoryIds[0]]);

        Assert.Empty(MockupTask.Assemble(temp.Path, TwoScreens()));

        var html = File.ReadAllText(Path.Combine(temp.Path, MockupTask.OutputFile));
        Assert.True(html.Length > MockupTask.MaxBytes);
        Assert.DoesNotContain("input/estilo", html);
        Assert.True(MockupTask.Validate(html, Plan.StoryIds).Valid);
    }

    [Fact]
    public void The_repair_of_each_step_says_where_its_files_are()
    {
        using var temp = new TempDirectory();
        var screen = new MockupScreen("P01", "Reservas", "", ["HU-001"]);

        var design = MockupTask.Review(new MockupStep(MockupStage.Design), Plan, temp.Path, "PROMPT", 1, []);
        var screenReview = MockupTask.Review(new MockupStep(MockupStage.Screen, screen), Plan, temp.Path, "PROMPT", 1, ["input/mapa.json LECTURA: …"]);

        Assert.StartsWith("PROMPT", design.RepairPrompt);
        Assert.Contains("output/prototipo/mapa.json FICHERO", design.RepairPrompt);
        Assert.Contains("Tus ficheros siguen en `output/prototipo/`", design.RepairPrompt);
        Assert.Contains("Tu pantalla sigue en `output/prototipo/pantallas/P01.html`", screenReview.RepairPrompt);
        Assert.Contains("input/mapa.json LECTURA: …", screenReview.Validation.Errors);
    }

    [Fact]
    public void The_fake_prototype_joins_into_a_valid_page_that_escapes_the_texts()
    {
        using var temp = new TempDirectory();
        var withTags = Backlog.Replace("\"titulo\": \"", "\"titulo\": \"<b>");
        var plan = MockupPlan.From("s", "es", withTags);
        var work = temp.Path;
        MockupTask.BuildRequest(plan, new MockupStep(MockupStage.Design), Work(temp, "job"), Guid.NewGuid(), "m", work, new MockupRunOptions());
        Directory.CreateDirectory(Path.Combine(work, MockupTask.PrototypeFolder, "pantallas"));
        File.WriteAllText(Path.Combine(work, MockupTask.MapFile), FakeMockup.Map(work));
        File.WriteAllText(Path.Combine(work, MockupTask.AppFile), FakeMockup.App(work));
        var map = MockupMap.Load(FakeMockup.Map(work), plan.StoryIds);
        foreach (var screen in map.Screens)
        {
            File.WriteAllText(Path.Combine(work, MockupTask.StoriesInput),
                new JsonObject { ["historias"] = new JsonArray(screen.StoryIds.Select(id => (JsonNode)new JsonObject { ["id"] = id, ["titulo"] = "<b>t" }).ToArray()) }.ToJsonString());
            File.WriteAllText(Path.Combine(work, MockupTask.ScreenFile(screen.Id)), FakeMockup.Screen(work, screen.Id, broken: false));
        }

        Assert.Empty(MockupTask.Assemble(work, map));

        var html = File.ReadAllText(Path.Combine(work, MockupTask.OutputFile));
        Assert.True(MockupTask.Validate(html, plan.StoryIds, map.Screens.Select(s => s.Id).ToList()).Valid);
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

    // ---- A whole job ----

    // The job without the database, with FakeProvider: the design, one run per
    // screen (each repaired once: the first attempt leaves out the mark of its
    // last story) and the integration, which joins a valid page.
    [Fact]
    public async Task A_mockup_job_designs_builds_each_screen_and_integrates()
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
        var jobId = Guid.NewGuid();
        Run NewRun(int lot) => new() { Id = Guid.NewGuid(), JobId = jobId, Slot = "MOCK", LotNumber = lot, Provider = "Fake", Model = "claude-sonnet-5-5", State = RunState.Queued };
        var added = new List<Run>();
        var finished = new List<RunOutcome>();

        var outcome = await runner.RunMockupAsync(NewRun(1), Plan, storage.JobFolder(jobId),
            count =>
            {
                added.AddRange(Enumerable.Range(2, count).Select(NewRun));
                return Task.FromResult<IReadOnlyList<Run>>(added);
            },
            _ => Task.CompletedTask,
            o => { lock (finished) finished.Add(o); return Task.CompletedTask; },
            CancellationToken.None);

        Assert.True(outcome.Succeeded);
        var map = MapOf(storage.JobFolder(jobId));
        Assert.Equal(map.Screens.Count + 1, added.Count); // the screens and the integration
        Assert.Equal(map.Screens.Count + 2, finished.Count);
        Assert.All(finished.Where(o => o.Run.LotNumber is > 1 && o.Run != added[^1]), o =>
        {
            Assert.Equal([RunStatus.Failed, RunStatus.Done], o.Attempts!.Select(a => a.Result.Status));
            Assert.Contains(" HU: ningún elemento de la pantalla lleva data-hu", o.Attempts![0].Result.ErrorMessage);
        });
        Assert.Equal(["Read", "Write", "Edit"], outcome.Attempts![0].Request.AllowedTools);
        var html = File.ReadAllText(MockupExecutor.OutputPath(outcome.Attempts[^1]));
        Assert.True(MockupTask.Validate(html, Plan.StoryIds, map.Screens.Select(s => s.Id).ToList()).Valid);
    }
}
