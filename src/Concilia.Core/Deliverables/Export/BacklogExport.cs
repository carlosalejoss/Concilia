using System.Text.Json.Nodes;
using Concilia.Core.Projects;

namespace Concilia.Core.Deliverables.Export;

public record ExportCriterion(string Id, string Scenario, string Given, string When, string Then);

public record ExportTask(string Id, string Title);

public record ExportStory(
    string Id, string Title, string As, string Want, string SoThat,
    IReadOnlyList<string> Requirements, IReadOnlyList<ExportCriterion> Criteria, IReadOnlyList<ExportTask> Tasks,
    // A discrepancy the user has not reviewed (only in a draft).
    bool PendingDiscrepancy,
    // The iterations of the project it comes from (step 111): those of its requirements.
    IReadOnlyList<int> Iterations);

public record ExportEpic(string Id, string Title, string Description, IReadOnlyList<ExportStory> Stories)
{
    // Those of its stories (step 111).
    public IReadOnlyList<int> Iterations => Stories.SelectMany(h => h.Iterations).Distinct().Order().ToList();
}

// The backlog as the export files show it (hito 19): the .feature files of
// Gherkin and the CSV for Jira and Azure DevOps. Built once from the working
// copy (BacklogDraft); as in the catalog, the origin (A, B or both) and the
// arbiter's explanation are internal and are left out.
public record BacklogExport(string Project, bool Validated, DateTimeOffset Date, DateTimeOffset? ValidatedAt, IReadOnlyList<ExportEpic> Epics)
{
    // Step 140: the language of the project, for the texts the platform adds.
    public ExportLanguage Language { get; init; } = ExportLanguage.Spanish;

    public string Status => Validated
        ? Language.Text($"validado el {Language.Date(ValidatedAt ?? Date)}", $"validated on {Language.Date(ValidatedAt ?? Date)}")
        : Language.Text("BORRADOR, todavía no está validado", "DRAFT, not validated yet");

    public string Generated => Language.Text(
        $"exportado desde Concilia el {Language.Date(Date)} a las {Date:HH:mm}",
        $"exported from Concilia on {Language.Date(Date)} at {Date:HH:mm}");

    // "Como empleado, quiero reservar una sala, para tenerla asegurada."
    public string Narrative(ExportStory story) => Language.Narrative(story.As, story.Want, story.SoThat);

    // requirementIterations: the iterations of each requirement of the catalog
    // (step 111); without them, no story says its iteration. language: that
    // of the project, "es" or "en" (step 140).
    public static BacklogExport Build(string project, bool validated, DateTimeOffset date, DateTimeOffset? validatedAt, string backlogJson,
        IReadOnlyDictionary<string, IReadOnlyList<int>>? requirementIterations = null, string language = "es")
    {
        var epics = Items(JsonNode.Parse(backlogJson)!.AsObject(), "epicas")
            .Select(e => new ExportEpic(Text(e, "id"), Text(e, "titulo"), Text(e, "descripcion"),
                Items(e, "historias").Select(h => new ExportStory(
                    Text(h, "id"), Text(h, "titulo"), Text(h, "como"), Text(h, "quiero"), Text(h, "para"),
                    RequirementIds(h),
                    Items(h, "criterios").Select(c => new ExportCriterion(Text(c, "id"), Text(c, "escenario"), Text(c, "dado"), Text(c, "cuando"), Text(c, "entonces"))).ToList(),
                    Items(h, "tareas").Select(t => new ExportTask(Text(t, "id"), Text(t, "titulo"))).ToList(),
                    PendingDiscrepancy: Flag(h, "discrepancia") && !Flag(h, "revisada"),
                    Iterations: requirementIterations is null ? [] : ElementIterations.OfStory(RequirementIds(h), requirementIterations))).ToList()))
            .ToList();
        return new BacklogExport(project, validated, date, validatedAt, epics) { Language = new ExportLanguage(language) };
    }

    // The requirements of a story, without spaces: they become tags (Gherkin)
    // and labels (Jira), which cannot have them. A user may have typed
    // "RF 001"; one left empty is dropped.
    private static List<string> RequirementIds(JsonObject story) =>
        story["requisitos"] is JsonArray list
            ? list.OfType<JsonValue>()
                .Select(v => v.TryGetValue<string>(out var s) ? string.Concat(s.Where(ch => !char.IsWhiteSpace(ch))) : "")
                .Where(s => s.Length > 0)
                .ToList()
            : [];

    private static IEnumerable<JsonObject> Items(JsonObject parent, string list) =>
        parent[list] is JsonArray items ? items.OfType<JsonObject>() : [];

    private static string Text(JsonObject o, string field) =>
        o[field] is JsonValue v && v.TryGetValue<string>(out var s) ? s : "";

    private static bool Flag(JsonObject o, string field) =>
        o[field] is JsonValue v && v.TryGetValue<bool>(out var b) && b;
}
