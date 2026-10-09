using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Phases;
using ReqArbiter.Core.Runner;

namespace ReqArbiter.Core.Mockup;

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
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan TotalTimeout { get; init; } = TimeSpan.FromMinutes(15);
}

// The task of the mockup (step 60): one model writes a self-contained .html
// from the validated backlog (prompts/v0/mockup.md). The platform checks it
// (Validate) and asks for one repair with the list of errors, like the phases.
public static class MockupTask
{
    public const string PromptResource = "v0/mockup.md";
    public const string OutputFile = "output/mockup.html";
    public const string BacklogFile = "input/backlog.json"; // a copy for FakeProvider and whoever reads the folder

    // The limit the prompt gives (1 MB).
    public const int MaxBytes = 1024 * 1024;

    // In the repair the previous .html goes back to the model only if it is
    // not too big; otherwise the errors alone must do.
    public const int MaxPreviousOutputChars = 200_000;

    public static string Prompt(MockupPlan plan) => PromptBuilder.Fill(PromptBuilder.ReadResource(PromptResource), new Dictionary<string, string>
    {
        ["NOMBRE_SISTEMA"] = plan.SystemName,
        ["IDIOMA"] = plan.Language,
        ["BACKLOG"] = plan.Backlog,
    });

    public static RunRequest BuildRequest(MockupPlan plan, Guid runId, string modelId, string workDirectory, MockupRunOptions options)
    {
        var path = Path.Combine(workDirectory, BacklogFile);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, plan.Backlog);
        return new RunRequest
        {
            RunId = runId,
            ModelId = modelId,
            Prompt = Prompt(plan),
            WorkDirectory = workDirectory,
            ExpectedOutputFiles = [OutputFile],
            AllowedTools = ["Write"],
            HeartbeatTimeout = options.HeartbeatTimeout,
            TotalTimeout = options.TotalTimeout,
            MaxOutputTokens = options.MaxOutputTokens,
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

        var views = StoryView.Matches(html).Select(m => m.Groups[1].Value).ToHashSet();
        foreach (var id in storyIds.Where(id => !views.Contains(id)))
            errors.Add($"{id} HU: ninguna vista lleva data-hu=\"{id}\".");
        var warnings = views.Where(v => !storyIds.Contains(v)).Order()
            .Select(v => $"{v} HU-EXTRA: hay una vista para una historia que no está en el backlog.")
            .ToList();
        return new PhaseValidation(errors, warnings);
    }

    // The review of an attempt: valid, or the repair prompt with the errors.
    public static PhaseReview Review(string? html, IReadOnlyList<string> storyIds, string originalPrompt, int attempt)
    {
        var validation = Validate(html, storyIds);
        if (validation.Valid)
            return new PhaseReview(validation, null);
        var previous = html is null ? ""
            : html.Length <= MaxPreviousOutputChars ? html
            : $"(No se incluye: ocupa {html.Length:N0} caracteres. Escríbelo de nuevo completo corrigiendo los errores.)";
        return new PhaseReview(validation, PhasePrompt.Repair(originalPrompt, attempt, OutputFile, previous, validation.Errors));
    }
}
