using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Concilia.Core.Matching;
using Concilia.Core.Phases;

namespace Concilia.Core.Deliverables;

// The working copy of the catalog v0, the one the user reviews, edits and
// validates. Each requirement has the fields of prompts/v0/esquemas/catalogo.schema.json
// plus four of the platform:
//   origen        "A", "B" or "ambas" (from the arbiter's `de`, in the order of the plan)
//   discrepancia  the arbiter found a difference the source does not settle, or
//                 the arbiter went against the matching of the platform (step 88)
//   explicacion   the arbiter's explanation, followed by the platform's one if
//                 there is a deviation (or why there was no cross-check)
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
            var deviations = result.Deviations.Where(d => d.ElementId == r["id"]!.GetValue<string>()).ToList();
            copy["discrepancia"] = crossChecked && (r["discrepancia"]!.GetValue<bool>() || deviations.Count > 0);
            copy["explicacion"] = crossChecked
                ? ArbiterDeviations.Explain(r["explicacion"]!.GetValue<string>(), deviations)
                : $"Sin verificación cruzada: solo terminó bien {result.Source}.";
            copy["revisada"] = false;
            requirements.Add(copy);
        }
        return Renumber(new JsonObject { ["requisitos"] = requirements }.ToJsonString());
    }

    // Ids by type in order of appearance (RF-001, RF-002…, RNF-001…), as the
    // validator expects them from a model: for what a job generates (and the
    // TIPO correction of PlatformFixes). A requirement with an unknown type
    // keeps its id (the validator reports it).
    public static string Renumber(string json)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        var counters = Types.ToDictionary(t => t, _ => 0);
        foreach (var r in Requirements(root))
        {
            if (TypeOf(r) is { } type)
                r["id"] = $"{type}-{++counters[type]:000}";
        }
        return root.ToJsonString(Indented);
    }

    // The user's changes, with stable ids (step 110): each requirement keeps
    // the id it had in the draft being replaced (previous); a new one, or one
    // whose type changed, takes the next number of its type, never one given
    // before in the history of the draft (used). Deleting leaves a gap.
    public static string Normalize(string json, string previous, IEnumerable<string> used)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        var ids = new StableIds(Ids(previous), used);
        foreach (var r in Requirements(root))
        {
            if (TypeOf(r) is { } type)
                r["id"] = ids.Assign(IdOf(r), type, 3);
        }
        return root.ToJsonString(Indented);
    }

    // The ids of the requirements of a catalog.
    public static IReadOnlyList<string> Ids(string json) =>
        Requirements(JsonNode.Parse(json)!.AsObject()).Select(IdOf).OfType<string>().ToList();

    private static string? IdOf(JsonObject r) => r["id"] is JsonValue v && v.TryGetValue<string>(out var id) ? id : null;

    private static string? TypeOf(JsonObject r) =>
        r["tipo"] is JsonValue v && v.TryGetValue<string>(out var type) && Types.Contains(type) ? type : null;

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
        // Stable ids (step 110): gaps are allowed in the draft; ID-SEQ is only for the models.
        var result = PhaseValidator.Validate(PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal), ForNextPhase(root), context with { GapsInIds = true });
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
