using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Concilia.Core.Phases;

// Step 108 (prompts v0.12): the arbiter of the backlog cites by their id the
// acceptance criteria and the tasks it keeps ("1:CA-012"), and writes whole
// only those it combines or writes anew. The platform puts the text of each
// cited one in its place and numbers all of them again (CA-001…, T-001…).
//
// Why: in the real test of the v0.11 (22-10-2026) the context of the arbiter
// was already full when it finished reading, and the whole backlog had to fit
// in one answer of 64,000 tokens: it kept exactly one criterion and one task
// per story (121 of the 237 criteria of the longest proposal). A criterion
// written whole takes about 120 tokens, cited by its id about 5.
//
// Applied before checking the output and to what is kept, like PlatformFixes.
// A cited id that is not in its proposal is an error (ARB-CITA) and is taken
// out, so the schema does not report it again. A warning says how many were
// cited and how many criteria of the joined stories were left out, so the
// report and the evaluation can measure it.
public static class CriteriaReferences
{
    public record Expanded(string Output, IReadOnlyList<string> Warnings, IReadOnlyList<string> Errors);

    // The two lists of a story with their prefix of ids.
    private static readonly (string List, string Prefix, string Name)[] Lists =
        [("criterios", "CA", "criterio"), ("tareas", "T", "tarea")];

    public static Expanded Apply(PhaseDefinition phase, string output, PhaseContext context)
    {
        if (!phase.IsArbiter || phase.Kind != PhaseKind.Backlog)
            return new Expanded(output, [], []);
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(output);
        }
        catch (JsonException)
        {
            return new Expanded(output, [], []); // not JSON: the validator reports it
        }
        if (root is null)
            return new Expanded(output, [], []);

        var proposals = new[] { Proposal(context.Proposal1), Proposal(context.Proposal2) };
        var errors = new List<string>();
        var cited = Lists.ToDictionary(l => l.List, _ => 0);
        var written = Lists.ToDictionary(l => l.List, _ => 0);
        var offered = Lists.ToDictionary(l => l.List, _ => 0); // of the stories each story joins
        var leftOut = Lists.ToDictionary(l => l.List, _ => 0);

        foreach (var story in Stories(root))
        {
            var storyId = story["id"]?.ToString() ?? "?";
            foreach (var (list, _, name) in Lists)
            {
                if (story[list] is not JsonArray items)
                    continue;
                var citedHere = new HashSet<string>();
                for (var i = items.Count - 1; i >= 0; i--)
                {
                    if (items[i] is not JsonValue v || !v.TryGetValue<string>(out var reference))
                    {
                        written[list]++;
                        continue;
                    }
                    if (Find(proposals, list, reference) is { } item)
                    {
                        items[i] = item.DeepClone();
                        citedHere.Add(reference);
                        cited[list]++;
                    }
                    else
                    {
                        errors.Add($"{storyId} ARB-CITA: {reference} no es {(name == "tarea" ? "una tarea" : "un criterio")} de {Where(reference)}. " +
                                   $"Cita el id tal como está en el fichero, con su número de propuesta (`1:{(list == "criterios" ? "CA-001" : "T-001")}`), o escríbelo entero.");
                        items.RemoveAt(i);
                    }
                }
                // The criteria (or tasks) of the stories this one joins that it does not cite.
                foreach (var source in Sources(story))
                    foreach (var id in ItemsOf(proposals, source, list))
                    {
                        offered[list]++;
                        if (!citedHere.Contains(id))
                            leftOut[list]++;
                    }
            }
        }

        foreach (var (list, prefix, _) in Lists)
            Renumber(Stories(root), list, prefix);

        var warnings = new List<string>();
        if (cited.Values.Sum() + leftOut.Values.Sum() > 0)
            warnings.Add($"{phase.OutputFile} ARB-CITA: la plataforma pone el texto de {cited["criterios"]} criterios y {cited["tareas"]} tareas citados por su id; " +
                         $"el árbitro escribió enteros {written["criterios"]} criterios y {written["tareas"]} tareas. " +
                         $"De los {offered["criterios"]} criterios y {offered["tareas"]} tareas de las historias que une, no cita {leftOut["criterios"]} criterios y {leftOut["tareas"]} tareas " +
                         "(pueden estar repetidos o combinados en uno nuevo).");
        return new Expanded(root.ToJsonString(Indented), warnings, errors);
    }

    // The criteria or tasks of each proposal by their prefixed id ("1:CA-012"),
    // and the ids of those of each story ("1:HU-003" -> ["1:CA-012", …]).
    private record Index(Dictionary<string, JsonObject> Items, Dictionary<string, List<string>> OfStory);

    private static Dictionary<string, Index> Proposal(string? proposal)
    {
        var lists = Lists.ToDictionary(l => l.List, _ => new Index([], []));
        if (proposal is null)
            return lists;
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(proposal);
        }
        catch (JsonException)
        {
            return lists;
        }
        if (root is null)
            return lists;
        foreach (var story in Stories(root))
        {
            var storyId = story["id"]?.ToString() ?? "";
            foreach (var (list, _, _) in Lists)
                foreach (var item in (story[list] as JsonArray ?? []).OfType<JsonObject>())
                    if (item["id"]?.ToString() is { } id)
                    {
                        lists[list].Items[id] = item;
                        if (!lists[list].OfStory.TryGetValue(storyId, out var ids))
                            lists[list].OfStory[storyId] = ids = [];
                        ids.Add(id);
                    }
        }
        return lists;
    }

    // "1:CA-012": the criterion CA-012 of proposal 1 (the proposals of the
    // context have no prefixes).
    private static JsonObject? Find(Dictionary<string, Index>[] proposals, string list, string reference) =>
        Split(reference) is var (n, id) && proposals[n - 1][list].Items.TryGetValue(id, out var item) ? item : null;

    private static IEnumerable<string> ItemsOf(Dictionary<string, Index>[] proposals, string story, string list) =>
        Split(story) is var (n, id) && proposals[n - 1][list].OfStory.TryGetValue(id, out var ids) ? ids.Select(i => $"{n}:{i}") : [];

    private static (int Number, string Id)? Split(string reference) =>
        reference.Length > 2 && reference[0] is '1' or '2' && reference[1] == ':' ? (reference[0] - '0', reference[2..]) : null;

    private static string Where(string reference) =>
        Split(reference) is var (n, _) ? $"la propuesta {n}" : "ninguna propuesta: le falta el número de su propuesta delante";

    // The stories an arbiter's story joins, from its `de`, with their prefix.
    private static IEnumerable<string> Sources(JsonObject story)
    {
        if (story["de"] is not JsonObject de)
            yield break;
        for (var n = 1; n <= 2; n++)
            foreach (var v in (de[$"propuesta_{n}"] as JsonArray ?? []).OfType<JsonValue>())
                if (v.TryGetValue<string>(out var id))
                    yield return id.StartsWith($"{n}:", StringComparison.Ordinal) ? id : $"{n}:{id}";
    }

    // Every criterion (or task) of the backlog numbered again in order, with
    // the id as its first key: the cited ones come with the ids of their
    // proposal, which repeat between the two.
    private static void Renumber(IEnumerable<JsonObject> stories, string list, string prefix)
    {
        var number = 0;
        foreach (var story in stories)
        {
            if (story[list] is not JsonArray items)
                continue;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] is not JsonObject item)
                    continue;
                var renumbered = new JsonObject { ["id"] = $"{prefix}-{++number:000}" };
                foreach (var (key, value) in item.ToList())
                    if (key != "id")
                    {
                        item.Remove(key);
                        renumbered[key] = value;
                    }
                items[i] = renumbered;
            }
        }
    }

    private static IEnumerable<JsonObject> Stories(JsonNode root) =>
        (root["epicas"] as JsonArray ?? []).OfType<JsonObject>()
            .SelectMany(e => (e["historias"] as JsonArray ?? []).OfType<JsonObject>());

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
