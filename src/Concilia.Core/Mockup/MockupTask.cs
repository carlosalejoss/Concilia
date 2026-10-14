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

// The three steps of the build (prompts v0.6): the design (the map of screens
// and the shell of the application), each screen, and the review of the whole.
public enum MockupStage { Design, Screen, Integration }

// One run of a mockup job: its stage and, for a screen, which one.
public record MockupStep(MockupStage Stage, MockupScreen? Screen = null);

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
//   3. Integration: one run reads the whole prototype and fixes, with Edit,
//      what does not fit (links between screens, data, repeated names).
//
// Each run sees what the earlier ones left: the platform keeps the accepted
// files in the job's folder (prototipo/) and copies them to the next runs.
// It joins the shell and the screens (Assemble) into output/mockup.html and
// checks the page as before. Each run has its own checks and one repair.
public static class MockupTask
{
    public const string DesignPrompt = "v0/mockup_diseno.md";
    public const string ScreenPrompt = "v0/mockup_pantalla.md";
    public const string IntegrationPrompt = "v0/mockup_integracion.md";

    // What the model writes.
    public const string PrototypeFolder = "output/prototipo";
    public const string MapFile = "output/prototipo/mapa.json";
    public const string AppFile = "output/prototipo/app.html";
    public static string ScreenFile(string screenId) => $"{PrototypeFolder}/pantallas/{screenId}.html";
    public const string Placeholder = "<!-- PANTALLAS -->";

    // The page the platform joins.
    public const string OutputFile = "output/mockup.html";

    // What the model reads.
    public const string BacklogFile = "input/backlog.json";
    public const string MapInput = "input/mapa.json";
    public const string AppInput = "input/app.html";
    public const string StoriesInput = "input/historias.json";
    public const string PrototypeCopy = "input/prototipo"; // the integration reads this copy entirely

    // The limit the prompt gives (1 MB), without the images and fonts of the
    // style reference that go inside as data: URIs.
    public const int MaxBytes = 1024 * 1024;

    // What §5 of the prompts says when there is no style reference.
    public const string NoStyle = "No hay referencia de estilo: haz un diseño sobrio y legible.";

    // In the job's folder: the accepted files of each step, and the style reference.
    public static string JobPrototypeFolder(string jobFolder) => Path.Combine(jobFolder, "prototipo");
    public static string JobStyleFolder(string jobFolder) => Path.Combine(jobFolder, "estilo");

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
                reads = [new InputFile(BacklogFile, ReadingPlan.Indented(plan.Backlog), Required: true)];
                outputs = [MapFile, AppFile];
                break;

            case MockupStage.Screen:
                var screen = step.Screen ?? throw new ArgumentException("A screen step needs its screen.", nameof(step));
                template = ScreenPrompt;
                reads =
                [
                    new InputFile(MapInput, ReadingPlan.Indented(File.ReadAllText(Path.Combine(prototype, "mapa.json"))), Required: true),
                    new InputFile(AppInput, File.ReadAllText(Path.Combine(prototype, "app.html")), Required: true),
                    new InputFile(StoriesInput, ReadingPlan.Indented(StoriesOf(plan.Backlog, screen.StoryIds)), Required: true),
                ];
                outputs = [ScreenFile(screen.Id)];
                values["PANTALLA"] = screen.Id;
                values["TITULO"] = screen.Title;
                values["FICHERO"] = ScreenFile(screen.Id);
                values["HISTORIAS"] = string.Join(", ", screen.StoryIds);
                break;

            default:
                template = IntegrationPrompt;
                // The model edits the files in output/prototipo/ and reads the
                // copy in input/prototipo/, which does not change while it edits:
                // the platform checks against it that it read everything.
                CopyFolder(prototype, Path.Combine(workDirectory, PrototypeFolder));
                CopyFolder(prototype, Path.Combine(workDirectory, PrototypeCopy));
                reads = Directory.GetFiles(Path.Combine(workDirectory, PrototypeCopy), "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(workDirectory, f).Replace('\\', '/'))
                    .OrderBy(PrototypeOrder).ThenBy(p => p, StringComparer.Ordinal)
                    .Select(p => new InputFile(p, File.ReadAllText(Path.Combine(workDirectory, p)), Required: true))
                    .Append(new InputFile(BacklogFile, ReadingPlan.Indented(plan.Backlog), Required: false))
                    .ToList();
                outputs = [];
                tools = ["Read", "Write", "Edit"];
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

    // The map first, then the shell, then the screens.
    private static int PrototypeOrder(string path) =>
        path.EndsWith("mapa.json", StringComparison.Ordinal) ? 0 : path.EndsWith("app.html", StringComparison.Ordinal) ? 1 : 2;

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
        IEnumerable<string> files = step.Stage switch
        {
            MockupStage.Design => [MapFile, AppFile],
            MockupStage.Screen => [ScreenFile(step.Screen!.Id)],
            _ => [],
        };
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
        _ => CheckIntegration(plan, workDirectory),
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

    private static PhaseValidation CheckIntegration(MockupPlan plan, string workDirectory)
    {
        var (map, mapErrors) = MockupMap.Parse(ReadOrNull(workDirectory, MapFile), plan.StoryIds);
        if (map is null)
            return new PhaseValidation(mapErrors, []);
        var parts = Assemble(workDirectory, map);
        var page = Validate(ReadOrNull(workDirectory, OutputFile), plan.StoryIds, map.Screens.Select(s => s.Id).ToList());
        // The errors of the parts first: they explain those of the page.
        return new PhaseValidation([.. parts, .. page.Errors], page.Warnings);
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
            yield return $"{path} ESTILO: no está en la lista de imágenes y fuentes de la referencia de estilo (§5); usa solo esas rutas.";
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
            $"{path} ESTILO: no está en la lista de imágenes y fuentes de la referencia de estilo (§5); usa solo esas rutas."));
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
            _ => (PrototypeFolder + "/",
                "Tus correcciones siguen en `output/prototipo/`. Corrige con Edit lo que señalan los errores."),
        };
        return new PhaseReview(validation, PhasePrompt.Repair(originalPrompt, attempt, target, previous, validation.Errors));
    }

    private static string? ReadOrNull(string workDirectory, string file)
    {
        var path = Path.Combine(workDirectory, file);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
