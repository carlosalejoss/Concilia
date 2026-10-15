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

    // A map whose first screen is the sign-in and every other one a menu entry.
    private static string MapJson(params (string Id, string[] Stories)[] screens) =>
        new JsonObject
        {
            ["inicio"] = screens[0].Id,
            ["pantallas"] = new JsonArray(screens.Select(s => (JsonNode)new JsonObject
            {
                ["id"] = s.Id,
                ["titulo"] = $"Pantalla {s.Id}",
                ["aplicacion"] = "App",
                ["roles"] = new JsonArray("socio"),
                ["menu"] = true,
                ["desde"] = new JsonArray(),
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

    // A small map with navigation: sign-in P01, a menu entry P02 and P03,
    // reached from P02. change edits the screens before writing the map.
    private static string NavigationMap(Action<JsonArray>? change = null)
    {
        JsonObject Screen(string id, string title, bool menu, string[] from, string[] stories) => new()
        {
            ["id"] = id, ["titulo"] = title, ["aplicacion"] = id == "P01" ? "Común" : "App del socio",
            ["roles"] = new JsonArray("socio"), ["menu"] = menu,
            ["desde"] = new JsonArray(from.Select(f => (JsonNode)f).ToArray()), ["descripcion"] = "…",
            ["historias"] = new JsonArray(stories.Select(h => (JsonNode)h).ToArray()),
        };
        var half = Plan.StoryIds.Count / 2;
        var screens = new JsonArray(
            Screen("P01", "Iniciar sesión", false, [], []),
            Screen("P02", "Inicio", true, [], [.. Plan.StoryIds.Take(half)]),
            Screen("P03", "Detalle", false, ["P02"], [.. Plan.StoryIds.Skip(half)]));
        change?.Invoke(screens);
        return new JsonObject { ["inicio"] = "P01", ["pantallas"] = screens }.ToJsonString();
    }

    [Fact]
    public void A_map_says_how_one_gets_to_each_screen_from_the_sign_in()
    {
        var (map, errors) = MockupMap.Parse(NavigationMap(), Plan.StoryIds);

        Assert.Empty(errors); // the sign-in may solve no story
        Assert.Equal("P01", map!.Start);
        Assert.Equal([("P02", "P03")], map.Links.Select(l => (l.From, l.To.Id)));
    }

    [Fact]
    public void Each_problem_of_the_navigation_has_its_code()
    {
        string[] Errors(Action<JsonArray> change) => [.. MockupMap.Parse(NavigationMap(change), Plan.StoryIds).Errors];

        Assert.Contains("P03 MAPA-ACCESO: no se llega a esta pantalla: no es una entrada de menú (\"menu\") y ninguna pantalla a la que se llega la tiene en \"desde\".",
            Errors(s => s[2]!["desde"] = new JsonArray()));
        Assert.Contains("P03 MAPA-DESDE: P09 no es una pantalla del mapa.", Errors(s => s[2]!["desde"] = new JsonArray("P02", "P09")));
        Assert.Contains("P02 MAPA-APP: falta la aplicación a la que pertenece la pantalla (\"aplicacion\").", Errors(s => s[1]!["aplicacion"] = ""));
        Assert.Contains("P02 MAPA-ROL: falta qué roles ven la pantalla (\"roles\").", Errors(s => s[1]!["roles"] = new JsonArray()));
        var withoutStart = JsonNode.Parse(NavigationMap())!.AsObject();
        withoutStart.Remove("inicio");
        Assert.Contains(MockupMap.Parse(withoutStart.ToJsonString(), Plan.StoryIds).Errors, e => e.Contains("MAPA-INICIO"));
    }

    [Fact]
    public void A_screen_is_told_where_it_leads_and_the_sign_in_shows_the_test_users()
    {
        var map = MockupMap.Load(NavigationMap(), Plan.StoryIds);

        var signIn = MockupTask.NavigationOf(map, map.Screens[0]);
        var home = MockupTask.NavigationOf(map, map.Screens[1]);
        var detail = MockupTask.NavigationOf(map, map.Screens[2]);

        Assert.Contains("inicio de sesión", signIn);
        Assert.Contains("usuarios de prueba", signIn);
        Assert.Contains("Se llega a ella desde: el menú de App del socio.", home);
        Assert.Contains("`mostrarPantalla('P03')`: Detalle", home);
        Assert.Contains("Se llega a ella desde: P02 «Inicio».", detail);
        Assert.DoesNotContain("mostrarPantalla('", detail);
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
    public void A_fix_reads_the_part_as_it_was_the_map_and_the_shell_and_writes_the_part_new()
    {
        using var temp = new TempDirectory();
        var job = JobAfterDesign(temp);
        var screens = MapOf(job).Screens;
        Directory.CreateDirectory(Path.Combine(MockupTask.JobPrototypeFolder(job), "pantallas"));
        foreach (var s in screens)
            File.WriteAllText(Path.Combine(MockupTask.JobPrototypeFolder(job), "pantallas", $"{s.Id}.html"), $"<section data-pantalla=\"{s.Id}\"></section>");
        var work = Work(temp, "fix");
        var step = new MockupStep(MockupStage.Fix, screens[0], ["P01 JS-GLOBAL: «guardar» ya se declara como global en el armazón (app.html)."]);

        var built = MockupTask.BuildRequest(Plan, step, job, Guid.NewGuid(), "m", work, new MockupRunOptions());

        Assert.Equal([MockupTask.MapInput, MockupTask.AppInput, MockupTask.FixInput], built.RequiredReads);
        Assert.Equal($"<section data-pantalla=\"{screens[0].Id}\"></section>", File.ReadAllText(Path.Combine(work, MockupTask.FixInput)));
        // The part is written new; the rest of the prototype is there to check it against.
        Assert.False(File.Exists(Path.Combine(work, MockupTask.ScreenFile(screens[0].Id))));
        Assert.True(File.Exists(Path.Combine(work, MockupTask.ScreenFile(screens[^1].Id))));
        Assert.Equal([MockupTask.ScreenFile(screens[0].Id)], built.Request.ExpectedOutputFiles);
        Assert.Equal(["Read", "Write"], built.Request.AllowedTools);
        Assert.Contains("- P01 JS-GLOBAL: «guardar»", built.Request.Prompt);
        Assert.Contains($"la pantalla {screens[0].Id}", built.Request.Prompt);
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
    public void A_big_page_only_gets_a_warning_and_embedded_images_do_not_count()
    {
        var big = MockupTask.Validate(Valid(new string('x', MockupTask.WarningBytes + 1)), Plan.StoryIds);
        var image = MockupTask.Validate(Valid($"<img src=\"data:image/png;base64,{new string('A', MockupTask.MaxBytes)}\">"), Plan.StoryIds);

        Assert.True(big.Valid);
        Assert.Contains(big.Warnings, w => w.StartsWith("output/mockup.html TAMANO-AVISO: ocupa 2,0 MB") || w.StartsWith("output/mockup.html TAMANO-AVISO: ocupa 2.0 MB"));
        Assert.True(image.Valid);
        Assert.Empty(image.Warnings);
    }

    [Fact]
    public void There_is_no_maximum_of_screens_only_a_warning_above_25()
    {
        var screens = Enumerable.Range(1, 30).Select(i => ($"P{i:00}", i == 2 ? Plan.StoryIds.ToArray() : new[] { Plan.StoryIds[0] })).ToArray();

        var (map, errors) = MockupMap.Parse(MapJson(screens), Plan.StoryIds);

        Assert.Empty(errors);
        Assert.StartsWith("output/prototipo/mapa.json MAPA-PANTALLAS: tiene 30 pantallas", Assert.Single(map!.Warnings()));
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

    // A job without the database: the runner, its storage and what the job
    // asks for (the runs it adds and those that end).
    private sealed class JobSetup : IDisposable
    {
        private readonly TempDirectory _temp = new();
        public readonly FileStorage Storage;
        public readonly JobRunner Runner;
        public readonly Guid JobId = Guid.NewGuid();
        public readonly List<Run> Added = [];
        public readonly List<RunOutcome> Finished = [];

        public JobSetup(IRunnerProvider provider)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = _temp.Path })
                .Build();
            Storage = new FileStorage(configuration, new HostingEnvironment { ContentRootPath = _temp.Path });
            var events = new RunEventStore();
            Runner = new JobRunner(
                new RunExecutor(provider, events, Storage, new RunnerOptions(), new CatalogOptions()), new CatalogOptions(),
                mockupExecutor: new MockupExecutor(provider, events, Storage, new MockupOptions()));
        }

        public string Folder => Storage.JobFolder(JobId);

        private Run NewRun(int lot) => new() { Id = Guid.NewGuid(), JobId = JobId, Slot = "MOCK", LotNumber = lot, Provider = "Fake", Model = "m", State = RunState.Queued };

        public Task<MockupJobResult> RunAsync()
        {
            Plan.Save(Folder);
            return Runner.RunMockupAsync(NewRun(1), Plan, Folder,
                (count, _) =>
                {
                    var runs = Enumerable.Range(Added.Count + 2, count).Select(NewRun).ToList();
                    Added.AddRange(runs);
                    return Task.FromResult<IReadOnlyList<Run>>(runs);
                },
                _ => Task.CompletedTask,
                o => { lock (Finished) Finished.Add(o); return Task.CompletedTask; },
                CancellationToken.None);
        }

        public void Dispose() => _temp.Dispose();
    }

    // With FakeProvider: the design, one run per screen (each repaired once:
    // the first attempt leaves out the mark of its last story), and the
    // platform joins a valid page without any fix.
    [Fact]
    public async Task A_mockup_job_designs_builds_each_screen_and_the_platform_joins_them()
    {
        using var setup = new JobSetup(new FakeProvider(simulateRepair: true));

        var result = await setup.RunAsync();

        Assert.True(result.Succeeded, result.Message);
        var map = MapOf(setup.Folder);
        Assert.Equal(map.Screens.Count, result.Screens);
        Assert.Equal(map.Screens.Count, setup.Added.Count); // the screens, no fixes
        Assert.All(setup.Finished.Where(o => o.Run.LotNumber > 1), o =>
        {
            Assert.Equal([RunStatus.Failed, RunStatus.Done], o.Attempts!.Select(a => a.Result.Status));
            Assert.Contains(" HU: ningún elemento de la pantalla lleva data-hu", o.Attempts![0].Result.ErrorMessage);
        });
        Assert.True(MockupTask.Validate(File.ReadAllText(result.HtmlPath!), Plan.StoryIds, map.Screens.Select(s => s.Id).ToList()).Valid);
    }

    // Like FakeProvider, but every screen declares the same global function:
    // the platform finds it and a fix of each later screen removes it.
    private sealed class RepeatedGlobal : IRunnerProvider
    {
        private readonly FakeProvider _fake = new();
        public string Name => "Test";

        public async Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken)
        {
            var result = await _fake.RunAsync(request, onEvent, cancellationToken);
            var fix = File.Exists(Path.Combine(request.WorkDirectory, MockupTask.FixInput));
            foreach (var file in request.ExpectedOutputFiles.Where(f => f.Contains("/pantallas/")))
            {
                var path = Path.Combine(request.WorkDirectory, file);
                var html = File.ReadAllText(path);
                File.WriteAllText(path, fix
                    ? html.Replace("<script>function comun() {}</script>", "")
                    : html.Replace("</section>", "<script>function comun() {}</script></section>"));
            }
            return result;
        }
    }

    [Fact]
    public async Task Problems_between_the_screens_are_fixed_one_part_at_a_time()
    {
        using var setup = new JobSetup(new RepeatedGlobal());

        var result = await setup.RunAsync();

        Assert.True(result.Succeeded, result.Message);
        var screens = MapOf(setup.Folder).Screens.Count;
        // The first screen keeps the name; each of the others gets a fix.
        Assert.Equal(screens + (screens - 1), setup.Added.Count);
        var fixes = setup.Finished.Where(o => o.Run.LotNumber > screens + 1).ToList();
        Assert.Equal(screens - 1, fixes.Count);
        Assert.All(fixes, o => Assert.Contains("JS-GLOBAL: «comun»", o.Attempts![0].Request.Prompt));
    }

    // A job that resumes a failed one: its folder already has the design and
    // the screens, so no model runs and the platform joins them.
    [Fact]
    public async Task A_resumed_job_reuses_the_steps_already_done()
    {
        using var first = new JobSetup(new FakeProvider());
        Assert.True((await first.RunAsync()).Succeeded);
        using var resumed = new JobSetup(new FailingProvider());
        CopyAll(MockupTask.JobPrototypeFolder(first.Folder), MockupTask.JobPrototypeFolder(resumed.Folder));

        var result = await resumed.RunAsync();

        Assert.True(result.Succeeded, result.Message);
        Assert.All(resumed.Finished, o => Assert.StartsWith("Reutilizado", o.SkipReason));
    }

    // A provider that must not be called.
    private sealed class FailingProvider : IRunnerProvider
    {
        public string Name => "Test";
        public Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("No model should run when resuming.");
    }

    private static void CopyAll(string from, string to)
    {
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }
}
