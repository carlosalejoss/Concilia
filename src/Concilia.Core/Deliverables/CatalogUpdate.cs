using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Concilia.Core.Matching;
using Concilia.Core.Phases;

namespace Concilia.Core.Deliverables;

// The changes of an update of the validated catalog with some iterations
// (milestone 22, step 113), applied to the working copy, so what the user
// edited stays (decisions of the author of 26-10-2026):
// - a new requirement is added with the next id of its type (StableIds) and
//   its history ("creado");
// - what the AI modifies or deletes is a proposal: the requirement keeps a
//   pending `cambio` with what it was before and why, and the catalog is not
//   validated until the user accepts or rejects it. Accepting a deletion
//   removes the requirement (it goes to `eliminados`, with its history);
//   rejecting a modification gives back the earlier content.
// Each requirement keeps its `historial`: in which iterations it was created,
// modified or deleted, with why, where it came from and the arbiter's note.
public static class CatalogUpdate
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly string[] ContentFields = ["tipo", "notacion", "obligatoriedad", "texto", "fuentes", "preguntas"];

    // workingCopy: the catalog as it is now; result: the arbiter's changes;
    // used: the ids already given in the history of the catalog (never reused).
    public static string Apply(string workingCopy, PhaseJobResult result, PhasePlan plan, Guid jobId, IEnumerable<string> used, DateTimeOffset now)
    {
        var root = JsonNode.Parse(workingCopy)!.AsObject();
        if (root["requisitos"] is not JsonArray requirements)
            root["requisitos"] = requirements = [];
        if (result.Output is null)
            return root.ToJsonString(Indented);
        var output = JsonNode.Parse(result.Output)!;
        var iterations = new JsonArray((plan.Focus?.Iterations ?? []).Select(n => (JsonNode)n).ToArray());
        var ids = new StableIds(CatalogDraft.Ids(workingCopy), used);

        foreach (var n in (output["nuevos"] as JsonArray ?? []).OfType<JsonObject>())
        {
            var copy = CatalogDraft.Pick(n, CatalogDraft.SchemaFields);
            copy["id"] = ids.Assign(null, n["tipo"]!.GetValue<string>(), 3);
            var origin = plan.OriginOf(n["de"]!.AsObject()) ?? "ambas";
            var deviations = result.Deviations.Where(d => d.ElementId == n["id"]!.GetValue<string>()).ToList();
            copy["origen"] = origin;
            copy["discrepancia"] = n["discrepancia"]!.GetValue<bool>() || deviations.Count > 0;
            copy["explicacion"] = ArbiterDeviations.Explain(n["explicacion"]!.GetValue<string>(), deviations);
            copy["revisada"] = false;
            copy["historial"] = new JsonArray(Entry("creado", iterations, null, n["motivo"]!.GetValue<string>(), origin, n["explicacion"]!.GetValue<string>(), jobId, now));
            requirements.Add(copy);
        }

        foreach (var c in (output["cambios"] as JsonArray ?? []).OfType<JsonObject>())
        {
            var action = c["accion"]!.GetValue<string>();
            var target = requirements.OfType<JsonObject>().FirstOrDefault(r => r["id"]?.GetValue<string>() == c["id"]!.GetValue<string>());
            // Deleted by the user meanwhile, or nothing to do.
            if (action == "ninguna" || target is null)
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
                foreach (var field in ContentFields)
                    target[field] = c[field]?.DeepClone();
            target["cambio"] = change;
        }
        return root.ToJsonString(Indented);
    }

    // The user accepts the change proposed for a requirement: a modification
    // stays, a deletion removes it. Either way it goes to its history.
    public static DeliverableChange Accept(Deliverable d, string id, DateTimeOffset now) => Decide(d, id, now, accept: true);

    // The user rejects it: a modification gives back the earlier content, a
    // deletion keeps the requirement. Nothing goes to the history.
    public static DeliverableChange Reject(Deliverable d, string id, DateTimeOffset now) => Decide(d, id, now, accept: false);

    private static DeliverableChange Decide(Deliverable d, string id, DateTimeOffset now, bool accept)
    {
        if (d.Status == DeliverableStatus.Validated)
            return DeliverableChange.Fail("Está validado: reábrelo para editarlo.");
        var root = JsonNode.Parse(d.Content)!.AsObject();
        var requirements = root["requisitos"] as JsonArray ?? [];
        var target = requirements.OfType<JsonObject>().FirstOrDefault(r => r["id"]?.GetValue<string>() == id);
        if (target?["cambio"] is not JsonObject change)
            return DeliverableChange.Fail($"{id} no tiene ningún cambio por revisar.");
        target.Remove("cambio");
        var action = change["accion"]!.GetValue<string>();
        if (accept)
        {
            var entry = Entry(action == "modificar" ? "modificado" : "eliminado", change["iteraciones"]!.AsArray().DeepClone().AsArray(),
                change["antes"]?.DeepClone(), change["motivo"]!.GetValue<string>(), change["origen"]!.GetValue<string>(),
                change["explicacion"]!.GetValue<string>(), Guid.Parse(change["trabajo"]!.GetValue<string>()), now);
            if (target["historial"] is not JsonArray history)
                target["historial"] = history = [];
            history.Add(entry);
            if (action == "eliminar")
            {
                requirements.Remove(target);
                if (root["eliminados"] is not JsonArray deleted)
                    root["eliminados"] = deleted = [];
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

    // The requirements with a change still to review (for the checks).
    public static IEnumerable<string> Pending(JsonObject root) =>
        (root["requisitos"] as JsonArray ?? []).OfType<JsonObject>()
            .Where(r => r["cambio"] is JsonObject)
            .Select(r => r["id"]?.GetValue<string>() ?? "");

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
