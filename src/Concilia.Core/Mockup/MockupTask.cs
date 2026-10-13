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
// the ids of its stories, each of which must have its view.
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

// The limits of a mockup run, from the configuration (MockupOptions).
public record MockupRunOptions
{
    public int MaxOutputTokens { get; init; } = 64_000;
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan TotalTimeout { get; init; } = TimeSpan.FromMinutes(45);
    public int MaxTurns { get; init; } = 40; // reads, the base, one Write per epic and the verdict
}

// The task of the mockup (step 60): one model writes a self-contained .html
// from the validated backlog (prompts/v0/mockup.md). The platform checks it
// (Validate) and asks for one repair with the list of errors, like the phases.
//
// Since prompts v0.4 (13-10-2026) the model writes it by parts: a base
// (output/mockup/base.html) with a <!-- VISTAS --> line, and one file per
// epic with the views of its stories. With 53 stories, the whole page in one
// answer did not fit in 64,000 output tokens. The platform joins the parts
// (Assemble) into output/mockup.html and checks that file as before.
public static class MockupTask
{
    public const string PromptResource = "v0/mockup.md";
    public const string OutputFile = "output/mockup.html"; // joined by the platform
    public const string PartsFolder = "output/mockup";
    public const string BaseFile = "output/mockup/base.html";
    public const string Placeholder = "<!-- VISTAS -->";

    public static string EpicFile(string epicId) => $"{PartsFolder}/{epicId}.html";
    public const string BacklogFile = "input/backlog.json"; // read by parts by the model (and by FakeProvider)

    // The model must read the whole backlog (the platform checks it).
    public static readonly IReadOnlyList<string> RequiredReads = [BacklogFile];

    // The limit the prompt gives (1 MB).
    public const int MaxBytes = 1024 * 1024;

    // The backlog goes to input/backlog.json, indented, and the model reads it
    // by parts ({{LECTURA}}), as in the phases.
    private static InputFile BacklogInput(MockupPlan plan) => new(BacklogFile, ReadingPlan.Indented(plan.Backlog), Required: true);

    public static string Prompt(MockupPlan plan) => PromptBuilder.Fill(PromptBuilder.ReadResource(PromptResource), new Dictionary<string, string>
    {
        ["NOMBRE_SISTEMA"] = plan.SystemName,
        ["IDIOMA"] = plan.Language,
        ["LECTURA"] = ReadingPlan.Block([BacklogInput(plan)]),
        ["FICHEROS"] = string.Join("\n", Epics(plan.Backlog).Select(e =>
            $"   - `{EpicFile(e.Id)}`: {string.Join(", ", e.StoryIds)}")),
    });

    // The epics of a backlog with the ids of their stories, in order.
    public static IReadOnlyList<(string Id, IReadOnlyList<string> StoryIds)> Epics(string backlog) =>
        (JsonNode.Parse(backlog)!["epicas"] as JsonArray ?? [])
            .Select(e => (e!["id"]!.GetValue<string>(),
                          (IReadOnlyList<string>)(e["historias"] as JsonArray ?? []).Select(h => h!["id"]!.GetValue<string>()).ToList()))
            .ToList();

    private static readonly Regex DocumentTag = new(@"<\s*(?:!doctype|html|head|body)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Joins the parts the model wrote into output/mockup.html: the base with
    // the files of the epics, in the order of the backlog, in place of
    // <!-- VISTAS -->. Returns what is wrong with the parts (a missing file,
    // a base without the line, an epic file that is a whole document); what
    // can be joined is still joined, so the checks of the page also run.
    public static IReadOnlyList<string> Assemble(string workDirectory, MockupPlan plan)
    {
        var errors = new List<string>();
        var basePath = Path.Combine(workDirectory, BaseFile);
        if (!File.Exists(basePath))
            return [$"{BaseFile} FICHERO: no se ha escrito la base del prototipo."];

        var page = File.ReadAllText(basePath);
        if (!page.Contains(Placeholder, StringComparison.Ordinal))
            errors.Add($"{BaseFile} BASE: falta la línea {Placeholder} donde van las vistas de las épicas.");

        var views = new StringBuilder();
        foreach (var (epicId, _) in Epics(plan.Backlog))
        {
            var file = EpicFile(epicId);
            var path = Path.Combine(workDirectory, file);
            if (!File.Exists(path))
            {
                errors.Add($"{epicId} FICHERO: falta {file} con las vistas de sus historias.");
                continue;
            }
            var part = File.ReadAllText(path);
            if (DocumentTag.IsMatch(part))
                errors.Add($"{epicId} FRAGMENTO: {file} solo lleva los <section data-hu> de sus historias (y su <script>), sin <!doctype>, <html>, <head> ni <body>.");
            views.Append($"<!-- {epicId} -->\n").Append(part.Trim()).Append('\n');
        }

        File.WriteAllText(Path.Combine(workDirectory, OutputFile), page.Replace(Placeholder, views.ToString()));
        return errors;
    }

    public static RunRequest BuildRequest(MockupPlan plan, Guid runId, string modelId, string workDirectory, MockupRunOptions options)
    {
        var path = Path.Combine(workDirectory, BacklogFile);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, BacklogInput(plan).Content);
        return new RunRequest
        {
            RunId = runId,
            ModelId = modelId,
            Prompt = Prompt(plan),
            WorkDirectory = workDirectory,
            ExpectedOutputFiles = [BaseFile],
            CarriedFolders = ["input", PartsFolder],
            AllowedTools = ["Read", "Write"],
            MaxTurns = options.MaxTurns,
            HeartbeatTimeout = options.HeartbeatTimeout,
            TotalTimeout = options.TotalTimeout,
            MaxOutputTokens = options.MaxOutputTokens,
            InputFilesInBudget = false,
        };
    }

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

    private static readonly Regex StoryView = new("""data-hu\s*=\s*["']([^"']+)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // The checks of prompts/v0/README.md. Errors start with the element and a
    // code, like those of PhaseValidator, and go as they are to the repair.
    // html: null if the model did not write the file.
    public static PhaseValidation Validate(string? html, IReadOnlyList<string> storyIds)
    {
        if (html is null)
            return new PhaseValidation([$"{OutputFile} FICHERO: no se ha escrito el fichero."], []);

        var errors = new List<string>();
        var bytes = Encoding.UTF8.GetByteCount(html);
        if (bytes > MaxBytes)
            errors.Add($"{OutputFile} TAMANO: ocupa {bytes:N0} bytes; el máximo es {MaxBytes:N0} (1 MB).");
        if (!html.TrimStart('﻿', ' ', '\t', '\r', '\n').StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase))
            errors.Add($"{OutputFile} DOCTYPE: no empieza por <!doctype html>.");
        if (!html.Contains("const DATOS", StringComparison.Ordinal))
            errors.Add($"{OutputFile} DATOS: falta el bloque «const DATOS = {{ … }};» con los datos de prueba.");
        foreach (var match in External.Matches(html).Select(m => m.Value).Distinct().Take(5))
            errors.Add($"{OutputFile} EXTERNO: carga un recurso externo ({match.Trim()}…); todo debe ir dentro del fichero.");

        // A value built in JavaScript (data-hu="${h[0]}" in a template) is not
        // a view: a real mockup gave that false warning (13-10-2026).
        var views = StoryView.Matches(html).Select(m => m.Groups[1].Value)
            .Where(v => !v.Contains('$') && !v.Contains('{'))
            .ToHashSet();
        foreach (var id in storyIds.Where(id => !views.Contains(id)))
            errors.Add($"{id} HU: ninguna vista lleva data-hu=\"{id}\".");
        var warnings = views.Where(v => !storyIds.Contains(v)).Order()
            .Select(v => $"{v} HU-EXTRA: hay una vista para una historia que no está en el backlog.")
            .ToList();
        return new PhaseValidation(errors, warnings);
    }

    // The review of an attempt: valid, or the repair prompt with the errors.
    // extraErrors: errors found outside the .html (the lines left unread).
    public static PhaseReview Review(string? html, IReadOnlyList<string> storyIds, string originalPrompt, int attempt,
        IReadOnlyList<string>? extraErrors = null)
    {
        // Without a page (no base), the errors of the parts say why; the
        // "page not written" one would only repeat it.
        var checkedHtml = html is null && extraErrors is { Count: > 0 } ? new PhaseValidation([], []) : Validate(html, storyIds);
        // The errors of the parts first: they explain those of the page.
        var validation = new PhaseValidation([.. extraErrors ?? [], .. checkedHtml.Errors], checkedHtml.Warnings);
        if (validation.Valid)
            return new PhaseReview(validation, null);
        // The parts stay in the folder of the repair (RunRequest.CarriedFolders):
        // it only has to rewrite those with errors.
        const string previous = "Tus ficheros siguen en `output/mockup/`. Reescribe solo los que señalan los errores, y léelos con Read si lo necesitas.";
        return new PhaseReview(validation, PhasePrompt.Repair(originalPrompt, attempt, PartsFolder + "/", previous, validation.Errors));
    }
}
