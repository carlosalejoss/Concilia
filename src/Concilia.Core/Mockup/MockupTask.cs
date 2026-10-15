using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Concilia.Core.Catalog;
using Concilia.Core.Phases;
using Concilia.Core.Runner;

namespace Concilia.Core.Mockup;

// What a mockup job needs, saved in its folder when the job is created: the
// validated backlog as the prompt receives it (BacklogDraft.ForNextPhase) and
// the ids of its stories, each of which must be solved in some screen.
public record MockupPlan(string SystemName, string Language, string Backlog, IReadOnlyList<string> StoryIds)
{
    public const string FileName = "mockup-plan.json";

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static MockupPlan From(string systemName, string language, string backlog) =>
        new(systemName, language, backlog, MockupTask.StoryIds(backlog));

    public void Save(string jobFolder)
    {
        Directory.CreateDirectory(jobFolder);
        File.WriteAllText(Path.Combine(jobFolder, FileName), JsonSerializer.Serialize(this, Json));
    }

    public static MockupPlan Load(string jobFolder) =>
        JsonSerializer.Deserialize<MockupPlan>(File.ReadAllText(Path.Combine(jobFolder, FileName)), Json)!;
}

// The limits of each run of a mockup job, from the configuration (MockupOptions).
public record MockupRunOptions
{
    public int MaxOutputTokens { get; init; } = 64_000;
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan TotalTimeout { get; init; } = TimeSpan.FromMinutes(45);
    public int MaxTurns { get; init; } = 40;
}

// The steps of the build (prompts v0.8): the design (the map of screens and
// the shell of the application), each screen and, if the platform finds
// problems between the parts, the fix of each part that has them.
public enum MockupStage { Design, Screen, Fix }

// One run of a mockup job: its stage and, for a screen, which one. A fix is
// of a screen or, without one, of the shell, with the problems to fix.
public record MockupStep(MockupStage Stage, MockupScreen? Screen = null, IReadOnlyList<string>? Problems = null);

// How a mockup job ended: the joined .html (if it is valid), the number of
// screens and what to tell the user.
public record MockupJobResult(bool Succeeded, string? HtmlPath, int Screens, string Message);

// The prototype joined and checked by the platform (MockupTask.Finish): the
// page, the errors that no model run can fix and the problems of each part.
public record MockupFinish(string HtmlPath, IReadOnlyList<string> Errors, IReadOnlyList<MockupProblem> Problems)
{
    public bool Valid => Errors.Count == 0 && Problems.Count == 0;
}

// The request of a run, and the files of input/ the model must read entirely.
public record MockupRequest(RunRequest Request, IReadOnlyList<string> RequiredReads);

// The mockup (prompts v0.6, decision of the author of 14-10-2026): ONE
// prototype of the whole application, built step by step in several runs
// instead of a view per story in a single run.
//
//   1. Design: from the backlog, the screens the application would really
//      have, with the stories each one solves (output/prototipo/mapa.json),
//      and its shell (output/prototipo/app.html): styles, header, menu, test
//      data, common functions and a <!-- PANTALLAS --> line.
//   2. Screens: one run per screen writes output/prototipo/pantallas/P01.html,
//      solving all its stories together with the shell's styles and functions.
//   3. The platform joins the shell and the screens (Finish) and checks the
//      page and what does not fit between the parts (MockupCoherence). If a
//      part has problems, one run fixes that part only (a Fix step), with
//      the shell, the map and the part itself.
//
// Until v0.7 the third step was one run that read the whole prototype; with
// 19 screens it did not fit in 128k and Claude Code stopped it (15-10-2026).
// Each run sees what the earlier ones left: the platform keeps the accepted
// files in the job's folder (prototipo/) and copies them to the next runs.
// Each run has its own checks and one repair.
public static class MockupTask
{
    public const string DesignPrompt = "v0/mockup_diseno.md";
    public const string ScreenPrompt = "v0/mockup_pantalla.md";
    public const string FixPrompt = "v0/mockup_correccion.md";

    // What the model writes.
    public const string PrototypeFolder = "output/prototipo";
    public const string MapFile = "output/prototipo/mapa.json";
    public const string AppFile = "output/prototipo/app.html";
    public static string ScreenFile(string screenId) => $"{PrototypeFolder}/pantallas/{screenId}.html";
    public const string Placeholder = "<!-- PANTALLAS -->";

    // The page the platform joins.
    public const string OutputFile = "output/mockup.html";

    // What the model reads. The JSON inputs go with one element per line
    // (ReadingPlan.OnePerLine), as the proposals of the arbiter: a backlog of
    // 9 epics took 2,743 lines indented (15-10-2026).
    public const string BacklogFile = "input/backlog.json";
    public const string MapInput = "input/mapa.json";
    public const string AppInput = "input/app.html";
    public const string StoriesInput = "input/historias.json";
    public const string FixInput = "input/original.html"; // the part to fix, as it was

    // The limit the prompt gives (1 MB), without the images and fonts of the
    // style reference that go inside as data: URIs.
    public const int MaxBytes = 1024 * 1024;

    // What §5 of the prompts says when there is no style reference.
    public const string NoStyle = "No hay referencia de estilo: haz un diseño sobrio y legible.";

    // In the job's folder: the accepted files of each step, and the style reference.
    public static string JobPrototypeFolder(string jobFolder) => Path.Combine(jobFolder, "prototipo");
    public static string JobStyleFolder(string jobFolder) => Path.Combine(jobFolder, "estilo");
    public static string JobAssemblyFolder(string jobFolder) => Path.Combine(jobFolder, "ensamblado");

    // Whether the job's folder already has a step, accepted in this job or in
    // the failed job it resumes.
    public static bool IsDone(MockupStep step, string jobFolder)
    {
        var prototype = JobPrototypeFolder(jobFolder);
        return step.Stage switch
        {
            MockupStage.Design => File.Exists(Path.Combine(prototype, "mapa.json")) && File.Exists(Path.Combine(prototype, "app.html")),
            MockupStage.Screen => File.Exists(Path.Combine(prototype, "pantallas", $"{step.Screen!.Id}.html")),
            _ => false,
        };
    }

    // The file a step writes.
    public static string TargetOf(MockupStep step) => step.Stage switch
    {
        MockupStage.Design => MapFile,
        MockupStage.Screen => ScreenFile(step.Screen!.Id),
        _ => step.Screen is { } screen ? ScreenFile(screen.Id) : AppFile,
    };

    // ---- Requests ----

    public static MockupRequest BuildRequest(MockupPlan plan, MockupStep step, string jobFolder, Guid runId, string modelId,
        string workDirectory, MockupRunOptions options)
    {
        CopyFolder(JobStyleFolder(jobFolder), Path.Combine(workDirectory, StyleReference.Folder));
        var prototype = JobPrototypeFolder(jobFolder);
        var style = StyleReference.Describe(workDirectory) ?? NoStyle;
        var values = new Dictionary<string, string>
        {
            ["NOMBRE_SISTEMA"] = plan.SystemName,
            ["IDIOMA"] = plan.Language,
            ["ESTILO"] = style,
        };

        List<InputFile> reads;
        string template;
        IReadOnlyList<string> outputs;
        IReadOnlyList<string> tools = ["Read", "Write"];
        switch (step.Stage)
        {
            case MockupStage.Design:
                template = DesignPrompt;
                reads = [new InputFile(BacklogFile, ReadingPlan.OnePerLine(plan.Backlog), Required: true)];
                outputs = [MapFile, AppFile];
                break;

            case MockupStage.Screen:
                var screen = step.Screen ?? throw new ArgumentException("A screen step needs its screen.", nameof(step));
                template = ScreenPrompt;
                reads =
                [
                    new InputFile(MapInput, ReadingPlan.OnePerLine(File.ReadAllText(Path.Combine(prototype, "mapa.json"))), Required: true),
                    new InputFile(AppInput, File.ReadAllText(Path.Combine(prototype, "app.html")), Required: true),
                    new InputFile(StoriesInput, ReadingPlan.OnePerLine(StoriesOf(plan.Backlog, screen.StoryIds)), Required: true),
                ];
                outputs = [ScreenFile(screen.Id)];
                values["PANTALLA"] = screen.Id;
                values["TITULO"] = screen.Title;
                values["FICHERO"] = ScreenFile(screen.Id);
                values["HISTORIAS"] = screen.StoryIds.Count > 0 ? string.Join(", ", screen.StoryIds) : "ninguna";
                values["NAVEGACION"] = NavigationOf(MockupMap.Load(File.ReadAllText(Path.Combine(prototype, "mapa.json")), plan.StoryIds), screen);
                break;

            default:
                // A fix: the part as it was goes to input/original.html and the
                // rest of the prototype to output/prototipo/, so the platform can
                // check the fixed part against the others. The part itself is not
                // there: the model writes it new (Write cannot overwrite a file
                // it has not read).
                template = FixPrompt;
                var target = TargetOf(step);
                CopyFolder(prototype, Path.Combine(workDirectory, PrototypeFolder));
                File.Delete(Path.Combine(workDirectory, target));
                var original = File.ReadAllText(Path.Combine(prototype, Path.GetRelativePath(PrototypeFolder, target)));
                reads = [new InputFile(MapInput, ReadingPlan.OnePerLine(File.ReadAllText(Path.Combine(prototype, "mapa.json"))), Required: true)];
                if (step.Screen is not null)
                    reads.Add(new InputFile(AppInput, File.ReadAllText(Path.Combine(prototype, "app.html")), Required: true));
                reads.Add(new InputFile(FixInput, original, Required: true));
                outputs = [target];
                values["PARTE"] = step.Screen is { } fixedScreen ? $"la pantalla {fixedScreen.Id} «{fixedScreen.Title}»" : "el armazón de la aplicación (`app.html`)";
                values["FICHERO"] = target;
                values["PROBLEMAS"] = string.Join("\n", (step.Problems ?? []).Select(problem => $"- {problem}"));
                break;
        }

        foreach (var input in reads)
        {
            var path = Path.Combine(workDirectory, input.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, input.Content);
        }
        values["LECTURA"] = ReadingPlan.Block(reads);

        var request = new RunRequest
        {
            RunId = runId,
            ModelId = modelId,
            Prompt = PromptBuilder.Fill(PromptBuilder.ReadResource(template), values),
            WorkDirectory = workDirectory,
            ExpectedOutputFiles = outputs,
            CarriedFolders = ["input", PrototypeFolder],
            AllowedTools = tools,
            MaxTurns = options.MaxTurns,
            HeartbeatTimeout = options.HeartbeatTimeout,
            TotalTimeout = options.TotalTimeout,
            MaxOutputTokens = options.MaxOutputTokens,
            InputFilesInBudget = false,
        };
        return new MockupRequest(request, reads.Where(r => r.Required).Select(r => r.Path).ToList());
    }

    // How a screen fits in the navigation of the application (prompts v0.9):
    // which application and roles, how one gets to it and, above all, the
    // screens it must lead to, each with mostrarPantalla (the platform checks
    // that those calls are there).
    public static string NavigationOf(MockupMap map, MockupScreen screen)
    {
        var lines = new List<string>();
        if (screen.Id == map.Start)
            lines.Add("- **Es la pantalla de inicio de sesión**, la primera que se ve. Muestra a la vista los usuarios de prueba, uno por rol (nombre, rol y aplicación a la que entra), y con un clic en uno se entra como ese usuario: llama a la función del armazón que inicia la sesión. También un formulario de usuario y contraseña que acepta esos mismos usuarios.");
        else
            lines.Add($"- Aplicación: {screen.Application}. La ven los roles: {string.Join(", ", screen.RolesOrEmpty)}.");

        var reachedFrom = screen.FromOrEmpty.Select(id => map.Screens.FirstOrDefault(s => s.Id == id)).OfType<MockupScreen>()
            .Select(s => $"{s.Id} «{s.Title}»").ToList();
        if (screen.Menu)
            reachedFrom.Insert(0, $"el menú de {screen.Application}");
        if (reachedFrom.Count > 0)
            lines.Add($"- Se llega a ella desde: {string.Join("; ", reachedFrom)}.");

        var leadsTo = map.Screens.Where(s => s.FromOrEmpty.Contains(screen.Id)).ToList();
        if (leadsTo.Count > 0)
        {
            lines.Add("- **Desde esta pantalla se llega a estas otras.** Cada una necesita una acción visible (un botón, un enlace, una tarjeta o una fila que se pulsa) cuyo código llame exactamente a `mostrarPantalla('…')` con su id; la plataforma lo comprueba:");
            lines.AddRange(leadsTo.Select(s => $"  - `mostrarPantalla('{s.Id}')`: {s.Title}"));
        }
        return string.Join("\n", lines);
    }

    // The stories of a screen, with their epic, as {"historias":[…]}.
    private static string StoriesOf(string backlog, IReadOnlyList<string> storyIds)
    {
        var stories = new JsonArray();
        foreach (var epic in JsonNode.Parse(backlog)!["epicas"] as JsonArray ?? [])
        {
            foreach (var story in epic!["historias"] as JsonArray ?? [])
            {
                if (!storyIds.Contains(story!["id"]!.GetValue<string>()))
                    continue;
                var copy = story.DeepClone().AsObject();
                copy["epica"] = $"{epic["id"]} · {epic["titulo"]}";
                stories.Add(copy);
            }
        }
        return new JsonObject { ["historias"] = stories }.ToJsonString();
    }

    // Keeps the files a step left, once accepted, in the job's folder, where
    // the next steps take them from.
    public static void Keep(MockupStep step, string workDirectory, string jobFolder)
    {
        var prototype = JobPrototypeFolder(jobFolder);
        IEnumerable<string> files = step.Stage == MockupStage.Design ? [MapFile, AppFile] : [TargetOf(step)];
        foreach (var file in files)
        {
            var target = Path.Combine(prototype, Path.GetRelativePath(PrototypeFolder, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(workDirectory, file), target, overwrite: true);
        }
    }

    private static void CopyFolder(string from, string to)
    {
        if (!Directory.Exists(from))
            return;
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    // ---- Checks ----

    // The ids of the stories of a backlog, in order.
    public static IReadOnlyList<string> StoryIds(string backlog) =>
        (JsonNode.Parse(backlog)!["epicas"] as JsonArray ?? [])
            .SelectMany(e => e!["historias"] as JsonArray ?? [])
            .Select(h => h!["id"]!.GetValue<string>())
            .ToList();

    // An external resource: src= or href= with http://, https:// or "//", or
    // the same inside CSS url(...) or @import. The .html must open offline.
    private static readonly Regex External = new(
        """(?:\b(?:src|href)\s*=\s*["']?|url\(\s*["']?|@import\s+["']?)\s*(?:https?:)?//""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // data-hu="HU-001" or data-hu="HU-001 HU-004": the stories an element solves.
    private static readonly Regex StoryMark = new("""data-hu\s*=\s*["']([^"']+)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // data-pantalla="P01": the root of a screen.
    private static readonly Regex ScreenMark = new("""data-pantalla\s*=\s*["']([^"']+)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DocumentTag = new(@"<\s*(?:!doctype|html|head|body)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // The content of a data: URI ("data:image/png;base64,iVBOR…").
    private static readonly Regex DataUri = new(@"data:[\w.+/-]+;base64,[A-Za-z0-9+/=]+", RegexOptions.Compiled);

    // The stories marked with data-hu in a page. A value built in JavaScript
    // (data-hu="${h[0]}" in a template) is not a mark: a real mockup gave that
    // false warning (13-10-2026).
    public static IReadOnlySet<string> StoryMarks(string html) =>
        StoryMark.Matches(html)
            .Select(m => m.Groups[1].Value)
            .Where(v => !v.Contains('$') && !v.Contains('{'))
            .SelectMany(v => v.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet();

    // The checks of a step. Errors start with the element and a code, like
    // those of PhaseValidator, and go as they are to the repair.
    public static PhaseValidation Check(MockupStep step, MockupPlan plan, string workDirectory) => step.Stage switch
    {
        MockupStage.Design => CheckDesign(plan, workDirectory),
        MockupStage.Screen => CheckScreen(step.Screen!, workDirectory),
        _ => CheckFix(step, plan, workDirectory),
    };

    private static PhaseValidation CheckDesign(MockupPlan plan, string workDirectory)
    {
        var (_, errors) = MockupMap.Parse(ReadOrNull(workDirectory, MapFile), plan.StoryIds);
        var all = new List<string>(errors);
        var app = ReadOrNull(workDirectory, AppFile);
        if (app is null)
            all.Add($"{AppFile} FICHERO: no se ha escrito el armazón de la aplicación.");
        else
        {
            if (!app.Contains(Placeholder, StringComparison.Ordinal))
                all.Add($"{AppFile} BASE: falta la línea {Placeholder} donde van las pantallas.");
            all.AddRange(PageErrors(app, AppFile, workDirectory));
        }
        return new PhaseValidation(all, []);
    }

    private static PhaseValidation CheckScreen(MockupScreen screen, string workDirectory)
    {
        var file = ScreenFile(screen.Id);
        var html = ReadOrNull(workDirectory, file);
        if (html is null)
            return new PhaseValidation([$"{file} FICHERO: no se ha escrito la pantalla."], []);

        var errors = new List<string>();
        var root = ScreenMark.Match(html);
        if (!html.TrimStart().StartsWith("<section", StringComparison.OrdinalIgnoreCase) || !root.Success || root.Groups[1].Value != screen.Id)
            errors.Add($"{screen.Id} PANTALLA: el fichero empieza por <section data-pantalla=\"{screen.Id}\">.");
        if (DocumentTag.IsMatch(html))
            errors.Add($"{screen.Id} FRAGMENTO: la pantalla no lleva <!doctype>, <html>, <head> ni <body>: el armazón ya los tiene.");
        var marks = StoryMarks(html);
        foreach (var id in screen.StoryIds.Where(id => !marks.Contains(id)))
            errors.Add($"{id} HU: ningún elemento de la pantalla lleva data-hu=\"{id}\".");
        errors.AddRange(PageErrors(html, file, workDirectory, wholeDocument: false));
        return new PhaseValidation(errors, []);
    }

    // A fixed part: the checks of its kind, and that it no longer has
    // problems with the rest of the prototype, which is in the same folder.
    private static PhaseValidation CheckFix(MockupStep step, MockupPlan plan, string workDirectory)
    {
        var own = step.Screen is { } screen ? CheckScreen(screen, workDirectory) : CheckShell(workDirectory);
        if (own.Errors.Any(e => e.Contains(" FICHERO:", StringComparison.Ordinal)))
            return own;
        var map = MockupMap.Load(File.ReadAllText(Path.Combine(workDirectory, MapFile)), plan.StoryIds);
        var target = step.Screen?.Id ?? MockupCoherence.Shell;
        var left = Coherence(workDirectory, map).Where(p => p.Target == target).Select(p => p.Message);
        return new PhaseValidation([.. own.Errors, .. left], own.Warnings);
    }

    private static PhaseValidation CheckShell(string workDirectory)
    {
        var app = ReadOrNull(workDirectory, AppFile);
        if (app is null)
            return new PhaseValidation([$"{AppFile} FICHERO: no se ha escrito el armazón de la aplicación."], []);
        var errors = new List<string>();
        if (!app.Contains(Placeholder, StringComparison.Ordinal))
            errors.Add($"{AppFile} BASE: falta la línea {Placeholder} donde van las pantallas.");
        errors.AddRange(PageErrors(app, AppFile, workDirectory));
        return new PhaseValidation(errors, []);
    }

    // The problems between the parts of the prototype in a folder.
    private static IReadOnlyList<MockupProblem> Coherence(string workDirectory, MockupMap map) =>
        MockupCoherence.Check(
            ReadOrNull(workDirectory, AppFile) ?? "",
            map.Screens
                .Select(s => (s.Id, Html: ReadOrNull(workDirectory, ScreenFile(s.Id))))
                .Where(s => s.Html is not null)
                .Select(s => (s.Id, s.Html!))
                .ToList(),
            map.Links.Select(l => (l.From, l.To.Id, l.To.Title)));

    // The platform joins the prototype of a job (the accepted files of its
    // steps) in its folder ensamblado/ and checks it: the page as before, and
    // what does not fit between the parts.
    public static MockupFinish Finish(string jobFolder, MockupPlan plan, MockupMap map)
    {
        var work = JobAssemblyFolder(jobFolder);
        if (Directory.Exists(work))
            Directory.Delete(work, recursive: true);
        CopyFolder(JobPrototypeFolder(jobFolder), Path.Combine(work, PrototypeFolder));
        CopyFolder(JobStyleFolder(jobFolder), Path.Combine(work, StyleReference.Folder));
        Directory.CreateDirectory(Path.Combine(work, "output"));

        var parts = Assemble(work, map);
        var page = Validate(ReadOrNull(work, OutputFile), plan.StoryIds, map.Screens.Select(s => s.Id).ToList());
        return new MockupFinish(Path.Combine(work, OutputFile), [.. parts, .. page.Errors], Coherence(work, map));
    }

    // What any part must satisfy: no external resources and only images and
    // fonts of the style reference; a whole document, its doctype and data.
    private static IEnumerable<string> PageErrors(string html, string file, string workDirectory, bool wholeDocument = true)
    {
        if (wholeDocument && !html.TrimStart('﻿', ' ', '\t', '\r', '\n').StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase))
            yield return $"{file} DOCTYPE: no empieza por <!doctype html>.";
        if (wholeDocument && !html.Contains("const DATOS", StringComparison.Ordinal))
            yield return $"{file} DATOS: falta el bloque «const DATOS = {{ … }};» con los datos de prueba.";
        foreach (var match in External.Matches(html).Select(m => m.Value).Distinct().Take(5))
            yield return $"{file} EXTERNO: carga un recurso externo ({match.Trim()}…); todo debe ir dentro del fichero.";
        foreach (var path in StyleReference.Inline(workDirectory, html).Missing)
            yield return $"{path} ESTILO: no está en la lista de imágenes y fuentes de la referencia de estilo (sección ESTILO); usa solo esas rutas.";
    }

    // Joins the shell and the screens, in the order of the map, into
    // output/mockup.html, with the images and fonts of the style reference
    // inside. Returns what is wrong with the parts; what can be joined is
    // still joined, so the checks of the page also run.
    public static IReadOnlyList<string> Assemble(string workDirectory, MockupMap map)
    {
        var app = ReadOrNull(workDirectory, AppFile);
        if (app is null)
            return [$"{AppFile} FICHERO: falta el armazón de la aplicación."];

        var errors = new List<string>();
        if (!app.Contains(Placeholder, StringComparison.Ordinal))
            errors.Add($"{AppFile} BASE: falta la línea {Placeholder} donde van las pantallas.");

        var screens = new StringBuilder();
        foreach (var screen in map.Screens)
        {
            var file = ScreenFile(screen.Id);
            var part = ReadOrNull(workDirectory, file);
            if (part is null)
            {
                errors.Add($"{screen.Id} FICHERO: falta {file}.");
                continue;
            }
            if (DocumentTag.IsMatch(part))
                errors.Add($"{screen.Id} FRAGMENTO: {file} no lleva <!doctype>, <html>, <head> ni <body>.");
            // Only the id: a title written by the model could close the comment.
            screens.Append($"<!-- {screen.Id} -->\n").Append(part.Trim()).Append('\n');
        }

        var (html, missing) = StyleReference.Inline(workDirectory, app.Replace(Placeholder, screens.ToString()));
        errors.AddRange(missing.Select(path =>
            $"{path} ESTILO: no está en la lista de imágenes y fuentes de la referencia de estilo (sección ESTILO); usa solo esas rutas."));
        File.WriteAllText(Path.Combine(workDirectory, OutputFile), html);
        return errors;
    }

    // The checks of the joined page (prompts/v0/README.md). html: null if
    // there is no page. screenIds: the screens of the map, each with its
    // data-pantalla; null to check only the stories (an earlier mockup).
    public static PhaseValidation Validate(string? html, IReadOnlyList<string> storyIds, IReadOnlyList<string>? screenIds = null)
    {
        if (html is null)
            return new PhaseValidation([$"{OutputFile} FICHERO: no se ha escrito el fichero."], []);

        var errors = new List<string>();
        // The images and fonts that go inside as data: URIs (those of the style
        // reference) do not count towards the limit.
        var bytes = Encoding.UTF8.GetByteCount(DataUri.Replace(html, "data:"));
        if (bytes > MaxBytes)
            errors.Add($"{OutputFile} TAMANO: ocupa {bytes:N0} bytes sin contar las imágenes y fuentes incrustadas; el máximo es {MaxBytes:N0} (1 MB).");
        if (!html.TrimStart('﻿', ' ', '\t', '\r', '\n').StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase))
            errors.Add($"{OutputFile} DOCTYPE: no empieza por <!doctype html>.");
        if (!html.Contains("const DATOS", StringComparison.Ordinal))
            errors.Add($"{OutputFile} DATOS: falta el bloque «const DATOS = {{ … }};» con los datos de prueba.");
        foreach (var match in External.Matches(html).Select(m => m.Value).Distinct().Take(5))
            errors.Add($"{OutputFile} EXTERNO: carga un recurso externo ({match.Trim()}…); todo debe ir dentro del fichero.");

        var screens = ScreenMark.Matches(html).Select(m => m.Groups[1].Value).ToHashSet();
        foreach (var id in (screenIds ?? []).Where(id => !screens.Contains(id)))
            errors.Add($"{id} PANTALLA: no hay ningún elemento con data-pantalla=\"{id}\".");

        var marks = StoryMarks(html);
        foreach (var id in storyIds.Where(id => !marks.Contains(id)))
            errors.Add($"{id} HU: ningún elemento lleva data-hu=\"{id}\".");
        var warnings = marks.Where(v => !storyIds.Contains(v)).Order()
            .Select(v => $"{v} HU-EXTRA: un elemento lleva el id de una historia que no está en el backlog.")
            .ToList();
        return new PhaseValidation(errors, warnings);
    }

    // The review of an attempt: valid, or the repair prompt with the errors.
    // unread: the errors of the lines it left unread.
    public static PhaseReview Review(MockupStep step, MockupPlan plan, string workDirectory, string originalPrompt, int attempt,
        IReadOnlyList<string> unread)
    {
        var check = Check(step, plan, workDirectory);
        var validation = new PhaseValidation([.. check.Errors, .. unread], check.Warnings);
        if (validation.Valid)
            return new PhaseReview(validation, null);

        // The files stay in the folder of the repair (RunRequest.CarriedFolders):
        // it only has to correct those with errors.
        var (target, previous) = step.Stage switch
        {
            MockupStage.Design => (PrototypeFolder + "/",
                "Tus ficheros siguen en `output/prototipo/`. Reescribe solo los que señalan los errores; léelos antes con Read."),
            MockupStage.Screen => (ScreenFile(step.Screen!.Id),
                $"Tu pantalla sigue en `{ScreenFile(step.Screen!.Id)}`. Léela con Read y reescríbela corregida."),
            _ => (TargetOf(step),
                $"Tu versión sigue en `{TargetOf(step)}`. Léela con Read y reescríbela corregida."),
        };
        return new PhaseReview(validation, PhasePrompt.Repair(originalPrompt, attempt, target, previous, validation.Errors));
    }

    private static string? ReadOrNull(string workDirectory, string file)
    {
        var path = Path.Combine(workDirectory, file);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
