using System.Text.Json.Nodes;

namespace Concilia.Core.Deliverables.Export;

public record ExportCriterion(string Id, string Scenario, string Given, string When, string Then);

public record ExportTask(string Id, string Title);

public record ExportStory(
    string Id, string Title, string As, string Want, string SoThat,
    IReadOnlyList<string> Requirements, IReadOnlyList<ExportCriterion> Criteria, IReadOnlyList<ExportTask> Tasks,
    // A discrepancy the user has not reviewed (only in a draft).
    bool PendingDiscrepancy)
{
    // "Como empleado, quiero reservar una sala, para tenerla asegurada."
    public string Narrative => $"Como {As}, quiero {Want}, para {SoThat}.";
}

public record ExportEpic(string Id, string Title, string Description, IReadOnlyList<ExportStory> Stories);

// The backlog as the export files show it (hito 19): the .feature files of
// Gherkin and the CSV for Jira and Azure DevOps. Built once from the working
// copy (BacklogDraft); as in the catalog, the origin (A, B or both) and the
// arbiter's explanation are internal and are left out.
public record BacklogExport(string Project, bool Validated, DateTimeOffset Date, DateTimeOffset? ValidatedAt, IReadOnlyList<ExportEpic> Epics)
{
    public string Status => Validated
        ? $"validado el {ValidatedAt ?? Date:dd-MM-yyyy}"
        : "BORRADOR, todavía no está validado";

    public string Generated => $"exportado desde Concilia el {Date:dd-MM-yyyy} a las {Date:HH:mm}";

    public static BacklogExport Build(string project, bool validated, DateTimeOffset date, DateTimeOffset? validatedAt, string backlogJson)
    {
        var epics = Items(JsonNode.Parse(backlogJson)!.AsObject(), "epicas")
            .Select(e => new ExportEpic(Text(e, "id"), Text(e, "titulo"), Text(e, "descripcion"),
                Items(e, "historias").Select(h => new ExportStory(
                    Text(h, "id"), Text(h, "titulo"), Text(h, "como"), Text(h, "quiero"), Text(h, "para"),
                    h["requisitos"] is JsonArray r ? r.OfType<JsonValue>().Select(v => v.TryGetValue<string>(out var s) ? s : "").Where(s => s.Length > 0).ToList() : [],
                    Items(h, "criterios").Select(c => new ExportCriterion(Text(c, "id"), Text(c, "escenario"), Text(c, "dado"), Text(c, "cuando"), Text(c, "entonces"))).ToList(),
                    Items(h, "tareas").Select(t => new ExportTask(Text(t, "id"), Text(t, "titulo"))).ToList(),
                    PendingDiscrepancy: Flag(h, "discrepancia") && !Flag(h, "revisada"))).ToList()))
            .ToList();
        return new BacklogExport(project, validated, date, validatedAt, epics);
    }

    private static IEnumerable<JsonObject> Items(JsonObject parent, string list) =>
        parent[list] is JsonArray items ? items.OfType<JsonObject>() : [];

    private static string Text(JsonObject o, string field) =>
        o[field] is JsonValue v && v.TryGetValue<string>(out var s) ? s : "";

    private static bool Flag(JsonObject o, string field) =>
        o[field] is JsonValue v && v.TryGetValue<bool>(out var b) && b;
}
