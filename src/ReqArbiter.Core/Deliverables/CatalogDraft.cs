using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ReqArbiter.Core.Phases;

namespace ReqArbiter.Core.Deliverables;

// The working copy of the catalog v0, the one the user reviews, edits and
// validates. Each requirement has the fields of prompts/v0/esquemas/catalogo.schema.json
// plus four of the platform:
//   origen        "A", "B" or "ambas" (from the arbiter's `de`, in the order of the plan)
//   discrepancia  the arbiter found a difference the source does not settle
//   explicacion   the arbiter's explanation (or why there was no cross-check)
//   revisada      the user reviewed the discrepancy (needed to validate)
public static class CatalogDraft
{
    public static readonly string[] Types = ["RF", "RNF", "RA", "RT", "RI"];
    public static readonly string[] SchemaFields = ["id", "tipo", "notacion", "obligatoriedad", "texto", "fuentes", "preguntas"];

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // The working copy of a job result: the arbiter's catalog or, without
    // cross-check, the only model that ended well. Renumbered by type.
    public static string FromResult(PhaseJobResult result, PhasePlan plan)
    {
        var crossChecked = result.Source == "ARB";
        var requirements = new JsonArray();
        foreach (var r in JsonNode.Parse(result.Output!)!["requisitos"]!.AsArray().Select(x => x!.AsObject()))
        {
            var copy = Pick(r, SchemaFields);
            copy["origen"] = crossChecked ? plan.OriginOf(r["de"]!.AsObject()) ?? "ambas" : result.Source;
            copy["discrepancia"] = crossChecked && r["discrepancia"]!.GetValue<bool>();
            copy["explicacion"] = crossChecked ? r["explicacion"]!.GetValue<string>() : $"Sin verificación cruzada: solo terminó bien {result.Source}.";
            copy["revisada"] = false;
            requirements.Add(copy);
        }
        return Normalize(new JsonObject { ["requisitos"] = requirements }.ToJsonString());
    }

    // Ids by type in order of appearance (RF-001, RF-002…, RNF-001…), as the
    // validator expects them: after the user adds or deletes requirements the
    // platform renumbers. A requirement with an unknown type keeps its id (the
    // validator reports it).
    public static string Normalize(string json)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        var counters = Types.ToDictionary(t => t, _ => 0);
        foreach (var r in Requirements(root))
        {
            if (r["tipo"] is JsonValue v && v.TryGetValue<string>(out var type) && counters.ContainsKey(type))
                r["id"] = $"{type}-{++counters[type]:000}";
        }
        return root.ToJsonString(Indented);
    }

    // Errors that keep it from being validated (the checks of the v0 validator
    // on the schema fields, plus each discrepancy not reviewed yet) and warnings.
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
        var result = PhaseValidator.Validate(PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal), ForNextPhase(root), context);
        var pending = Requirements(root)
            .Where(r => r["discrepancia"] is JsonValue d && d.GetValue<bool>() && !(r["revisada"] is JsonValue rv && rv.GetValue<bool>()))
            .Select(r => $"{r["id"]} DISC: hay una discrepancia sin revisar.");
        return new PhaseValidation([.. result.Errors, .. pending], result.Warnings);
    }

    // The validated catalog as the next phases receive it ({{CATALOGO}} of the
    // backlog): only the fields of catalogo.schema.json.
    public static string ForNextPhase(string json) => ForNextPhase(JsonNode.Parse(json)!.AsObject());

    private static string ForNextPhase(JsonObject root) =>
        new JsonObject { ["requisitos"] = new JsonArray(Requirements(root).Select(r => (JsonNode)Pick(r, SchemaFields)).ToArray()) }
            .ToJsonString(Indented);

    private static IEnumerable<JsonObject> Requirements(JsonObject root) =>
        root["requisitos"] is JsonArray list ? list.OfType<JsonObject>() : [];

    // A copy with only these fields, in this order (missing ones are left out).
    internal static JsonObject Pick(JsonObject source, IEnumerable<string> fields)
    {
        var copy = new JsonObject();
        foreach (var field in fields)
        {
            if (source.TryGetPropertyValue(field, out var value))
                copy[field] = value?.DeepClone();
        }
        return copy;
    }
}
