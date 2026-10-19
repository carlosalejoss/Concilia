using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Concilia.Core.Matching;
using Concilia.Core.Phases;

namespace Concilia.Core.Deliverables;

// The working copy of the backlog v0: Épica → Historia → Criterios (Gherkin)
// and Tareas, as in prompts/v0/esquemas/backlog.schema.json. Each story also
// has the four fields of the platform, as the requirements of the catalog:
// origen ("A", "B" or "ambas"), discrepancia, explicacion and revisada.
public static class BacklogDraft
{
    public static readonly string[] EpicFields = ["id", "titulo", "descripcion", "historias"];
    public static readonly string[] StoryFields = ["id", "titulo", "como", "quiero", "para", "requisitos", "criterios", "tareas"];

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // The working copy of a job result: the arbiter's backlog or, without
    // cross-check, the only model that ended well. Renumbered.
    public static string FromResult(PhaseJobResult result, PhasePlan plan)
    {
        var crossChecked = result.Source == "ARB";
        var epics = new JsonArray();
        foreach (var e in JsonNode.Parse(result.Output!)!["epicas"]!.AsArray().Select(x => x!.AsObject()))
        {
            var epic = CatalogDraft.Pick(e, EpicFields);
            var stories = new JsonArray();
            foreach (var h in e["historias"]!.AsArray().Select(x => x!.AsObject()))
            {
                var story = CatalogDraft.Pick(h, StoryFields);
                story["origen"] = crossChecked ? plan.OriginOf(h["de"]!.AsObject()) ?? "ambas" : result.Source;
                // A deviation from the matching of the platform is a discrepancy too (step 88).
                var deviations = result.Deviations.Where(d => d.ElementId == h["id"]!.GetValue<string>()).ToList();
                story["discrepancia"] = crossChecked && (h["discrepancia"]!.GetValue<bool>() || deviations.Count > 0);
                story["explicacion"] = crossChecked
                    ? ArbiterDeviations.Explain(h["explicacion"]!.GetValue<string>(), deviations)
                    : $"Sin verificación cruzada: solo terminó bien {result.Source}.";
                story["revisada"] = false;
                stories.Add(story);
            }
            epic["historias"] = stories;
            epics.Add(epic);
        }
        return Normalize(new JsonObject { ["epicas"] = epics }.ToJsonString());
    }

    // Consecutive ids in order of appearance: EP-01…, HU-001…, CA-001… and
    // T-001… (criteria and tasks are numbered across the whole backlog, as the
    // validator expects), so the user can add and delete freely.
    public static string Normalize(string json)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        int epics = 0, stories = 0, criteria = 0, tasks = 0;
        foreach (var e in Epics(root))
        {
            e["id"] = $"EP-{++epics:00}";
            foreach (var h in Items(e, "historias"))
            {
                h["id"] = $"HU-{++stories:000}";
                foreach (var c in Items(h, "criterios"))
                    c["id"] = $"CA-{++criteria:000}";
                foreach (var t in Items(h, "tareas"))
                    t["id"] = $"T-{++tasks:000}";
            }
        }
        return root.ToJsonString(Indented);
    }

    // Errors that keep it from being validated (the v0 validator on the schema
    // fields, with the validated catalog of the context, plus each discrepancy
    // not reviewed yet) and warnings (functional requirements without a story).
    public static PhaseValidation Check(string json, PhaseContext context)
    {
        JsonObject root;
        try
        {
            root = JsonNode.Parse(json)!.AsObject();
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException)
        {
            return new PhaseValidation([$"JSON no válido: {e.Message}"], []);
        }
        var result = PhaseValidator.Validate(PhaseDefinition.For(PhaseKind.Backlog, PhaseRole.Proposal), ForNextPhase(root), context);
        var pending = Epics(root).SelectMany(e => Items(e, "historias"))
            .Where(h => h["discrepancia"] is JsonValue d && d.GetValue<bool>() && !(h["revisada"] is JsonValue rv && rv.GetValue<bool>()))
            .Select(h => $"{h["id"]} DISC: hay una discrepancia sin revisar.");
        return new PhaseValidation([.. result.Errors, .. pending], result.Warnings);
    }

    // The validated backlog as the mockup receives it ({{BACKLOG}}): only the
    // fields of backlog.schema.json.
    public static string ForNextPhase(string json) => ForNextPhase(JsonNode.Parse(json)!.AsObject());

    private static string ForNextPhase(JsonObject root)
    {
        var epics = new JsonArray();
        foreach (var e in Epics(root))
        {
            var epic = CatalogDraft.Pick(e, EpicFields);
            epic["historias"] = new JsonArray(Items(e, "historias").Select(h => (JsonNode)CatalogDraft.Pick(h, StoryFields)).ToArray());
            epics.Add(epic);
        }
        return new JsonObject { ["epicas"] = epics }.ToJsonString(Indented);
    }

    private static IEnumerable<JsonObject> Epics(JsonObject root) => Items(root, "epicas");

    private static IEnumerable<JsonObject> Items(JsonObject parent, string list) =>
        parent[list] is JsonArray items ? items.OfType<JsonObject>() : [];
}
