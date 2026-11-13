using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Concilia.Core.Matching;
using Concilia.Core.Phases;

namespace Concilia.Core.Deliverables;

// The changes of an update of the validated backlog (milestone 22, step 114),
// applied to the working copy as those of the catalog (CatalogUpdate):
// - a new story goes to its epic (or to a new one, with the next EP id), with
//   the next ids of stories, criteria and tasks (StableIds) and its history;
// - what the AI modifies or deletes is a proposal: the story keeps a pending
//   `cambio` with what it was before and why, and the backlog is not validated
//   until the user accepts or rejects it. A modified story keeps the criteria
//   and tasks it cites by their id, and the new ones get the next ids.
//   Accepting a deletion removes the story (it goes to `eliminadas`).
public static class BacklogUpdate
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly string[] ContentFields = ["titulo", "como", "quiero", "para", "requisitos", "criterios", "tareas"];

    public static string Apply(string workingCopy, PhaseJobResult result, PhasePlan plan, Guid jobId, IEnumerable<string> used, DateTimeOffset now)
    {
        var root = JsonNode.Parse(workingCopy)!.AsObject();
        if (root["epicas"] is not JsonArray epics)
            root["epicas"] = epics = [];
        if (result.Output is null)
            return root.ToJsonString(Indented);
        var output = JsonNode.Parse(result.Output)!;
        var iterations = new JsonArray((plan.Focus?.Iterations ?? []).Select(n => (JsonNode)n).ToArray());
        var ids = new StableIds(BacklogDraft.Ids(workingCopy), used);
        var storiesById = Stories(epics).ToDictionary(h => h["id"]!.GetValue<string>());

        // The criteria and tasks of a story: those cited by id copied from the
        // current story, and those written whole with the next id.
        JsonArray Items(JsonNode? list, JsonObject? current, string field, string prefix)
        {
            var own = (current?[field] as JsonArray ?? []).OfType<JsonObject>().ToDictionary(x => x["id"]!.GetValue<string>());
            var items = new JsonArray();
            foreach (var item in list as JsonArray ?? [])
            {
                if (item is JsonValue cited && own.TryGetValue(cited.GetValue<string>(), out var kept))
                    items.Add(kept.DeepClone());
                else if (item is JsonObject written)
                {
                    var copy = written.DeepClone().AsObject();
                    var withId = new JsonObject { ["id"] = ids.Assign(null, prefix, 3) };
                    foreach (var (key, value) in copy)
                        withId[key] = value?.DeepClone();
                    items.Add(withId);
                }
            }
            return items;
        }

        // Step 126: the id each new story takes (N-001 → HU-087), so the
        // explanation of a split-same pair cites the other one by its id here.
        var finalIds = new Dictionary<string, string>();
        var explained = new List<(JsonObject Story, string Arbiter, List<ArbiterDeviation> Deviations)>();
        var newEpics = new Dictionary<string, JsonObject>(); // by title, so two new stories of the same new epic share it
        foreach (var n in (output["nuevas"] as JsonArray ?? []).OfType<JsonObject>())
        {
            JsonObject? epic = null;
            if (n["epica"] is JsonValue epicId)
                epic = epics.OfType<JsonObject>().FirstOrDefault(e => e["id"]?.GetValue<string>() == epicId.GetValue<string>());
            else if (n["epica"] is JsonObject created)
            {
                var title = created["titulo"]!.GetValue<string>();
                if (!newEpics.TryGetValue(title, out epic))
                {
                    epic = new JsonObject
                    {
                        ["id"] = ids.Assign(null, "EP", 2),
                        ["titulo"] = title,
                        ["descripcion"] = created["descripcion"]?.GetValue<string>() ?? "",
                        ["historias"] = new JsonArray(),
                    };
                    epics.Add(epic);
                    newEpics[title] = epic;
                }
            }
            if (epic is null)
                continue; // an epic that is not there any more (the user deleted it meanwhile)
            var origin = plan.OriginOf(n["de"]!.AsObject()) ?? "ambas";
            var deviations = result.Deviations.Where(d => d.ElementId == n["id"]!.GetValue<string>()).ToList();
            var story = new JsonObject
            {
                ["id"] = ids.Assign(null, "HU", 3),
                ["titulo"] = n["titulo"]!.DeepClone(),
                ["como"] = n["como"]!.DeepClone(),
                ["quiero"] = n["quiero"]!.DeepClone(),
                ["para"] = n["para"]!.DeepClone(),
                ["requisitos"] = n["requisitos"]!.DeepClone(),
                ["criterios"] = Items(n["criterios"], null, "criterios", "CA"),
                ["tareas"] = Items(n["tareas"], null, "tareas", "T"),
                ["origen"] = origin,
                ["discrepancia"] = n["discrepancia"]!.GetValue<bool>() || deviations.Count > 0,
                ["explicacion"] = "", // written below, once every new story has its id
                ["revisada"] = false,
                ["historial"] = new JsonArray(Entry("creada", iterations, null, n["motivo"]!.GetValue<string>(), origin, n["explicacion"]!.GetValue<string>(), jobId, now)),
            };
            (epic["historias"] as JsonArray)!.Add(story);
            finalIds[n["id"]!.GetValue<string>()] = story["id"]!.GetValue<string>();
            explained.Add((story, n["explicacion"]!.GetValue<string>(), deviations));
        }
        foreach (var (story, arbiter, deviations) in explained)
            story["explicacion"] = ArbiterDeviations.Explain(arbiter, deviations, finalIds.GetValueOrDefault);

        foreach (var c in (output["cambios"] as JsonArray ?? []).OfType<JsonObject>())
        {
            var action = c["accion"]!.GetValue<string>();
            if (action == "ninguna" || !storiesById.TryGetValue(c["id"]!.GetValue<string>(), out var target))
                continue;
            var from = c["de"]!.AsArray().Select(x => plan.ProposalOrder[x!.GetValue<int>() - 1]).ToList();
            var change = new JsonObject
            {
                ["accion"] = action,
                ["antes"] = action == "modificar" ? CatalogDraft.Pick(target, ContentFields) : null,
                ["motivo"] = c["motivo"]!.GetValue<string>(),
                ["origen"] = from.Count == 2 ? "ambas" : from.Single(),
                ["discrepancia"] = c["discrepancia"]!.GetValue<bool>(),
                ["explicacion"] = c["explicacion"]!.GetValue<string>(),
                ["iteraciones"] = iterations.DeepClone(),
                ["trabajo"] = jobId.ToString(),
                ["fecha"] = now.ToString("O"),
            };
            if (action == "modificar")
            {
                var criteria = Items(c["criterios"], target, "criterios", "CA");
                var tasks = Items(c["tareas"], target, "tareas", "T");
                foreach (var field in new[] { "titulo", "como", "quiero", "para", "requisitos" })
                    target[field] = c[field]?.DeepClone();
                target["criterios"] = criteria;
                target["tareas"] = tasks;
            }
            target["cambio"] = change;
        }
        return root.ToJsonString(Indented);
    }

    public static DeliverableChange Accept(Deliverable d, string id, DateTimeOffset now) => Decide(d, id, now, accept: true);

    public static DeliverableChange Reject(Deliverable d, string id, DateTimeOffset now) => Decide(d, id, now, accept: false);

    private static DeliverableChange Decide(Deliverable d, string id, DateTimeOffset now, bool accept)
    {
        if (d.Status == DeliverableStatus.Validated)
            return DeliverableChange.Fail("Está validado: reábrelo para editarlo.");
        var root = JsonNode.Parse(d.Content)!.AsObject();
        var epic = (root["epicas"] as JsonArray ?? []).OfType<JsonObject>()
            .FirstOrDefault(e => (e["historias"] as JsonArray ?? []).OfType<JsonObject>().Any(h => h["id"]?.GetValue<string>() == id));
        var target = (epic?["historias"] as JsonArray)?.OfType<JsonObject>().First(h => h["id"]?.GetValue<string>() == id);
        if (target?["cambio"] is not JsonObject change)
            return DeliverableChange.Fail($"{id} no tiene ningún cambio por revisar.");
        target.Remove("cambio");
        var action = change["accion"]!.GetValue<string>();
        if (accept)
        {
            if (target["historial"] is not JsonArray history)
                target["historial"] = history = [];
            history.Add(Entry(action == "modificar" ? "modificada" : "eliminada", change["iteraciones"]!.AsArray(), change["antes"],
                change["motivo"]!.GetValue<string>(), change["origen"]!.GetValue<string>(), change["explicacion"]!.GetValue<string>(),
                Guid.Parse(change["trabajo"]!.GetValue<string>()), now));
            if (action == "eliminar")
            {
                (epic!["historias"] as JsonArray)!.Remove(target);
                target["epica"] = epic["id"]?.DeepClone();
                if (root["eliminadas"] is not JsonArray deleted)
                    root["eliminadas"] = deleted = [];
                deleted.Add(target);
            }
        }
        else if (action == "modificar")
        {
            foreach (var field in ContentFields)
                target[field] = change["antes"]![field]?.DeepClone();
        }
        d.Content = root.ToJsonString(Indented);
        d.UpdatedAt = now;
        return new DeliverableChange([], null);
    }

    // The stories with a change still to review (for the checks).
    public static IEnumerable<string> Pending(JsonObject root) =>
        Stories(root["epicas"] as JsonArray ?? []).Where(h => h["cambio"] is JsonObject).Select(h => h["id"]?.GetValue<string>() ?? "");

    private static IEnumerable<JsonObject> Stories(JsonArray epics) =>
        epics.OfType<JsonObject>().SelectMany(e => (e["historias"] as JsonArray ?? []).OfType<JsonObject>());

    private static JsonObject Entry(string action, JsonArray iterations, JsonNode? before, string reason, string origin, string note, Guid jobId, DateTimeOffset now) => new()
    {
        ["accion"] = action,
        ["iteraciones"] = iterations.DeepClone(),
        ["antes"] = before?.DeepClone(),
        ["motivo"] = reason,
        ["origen"] = origin,
        ["explicacion"] = note,
        ["trabajo"] = jobId.ToString(),
        ["fecha"] = now.ToString("O"),
    };
}
