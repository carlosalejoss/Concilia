using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Concilia.Core.Phases;

// The ids of the proposals as the arbiter sees them (prompts v0.3, 09-10-2026):
// "1:P-003" in proposal 1 and "2:P-001" in proposal 2. Reading the proposals
// from two files, the arbiter put ids of one proposal in the `de` of the
// other in its first attempt three times out of three; with the number of the
// proposal in the id itself they cannot be mixed. The platform removes the
// prefix from the arbiter's output before checking and keeping it, so the
// schemas, the validator and the rest of the flow do not change.
public static class ProposalIds
{
    // "1:P-003" -> "P-003"; an id without prefix stays as it is.
    public static string Bare(string id) =>
        id.Length > 2 && char.IsDigit(id[0]) && id[1] == ':' ? id[2..] : id;

    // The proposal written for the arbiter, with "n:" before the id of each
    // element it compares (questions, requirements or stories). Ids that point
    // elsewhere (the answers a requirement cites, the requirements of a story)
    // keep their form. Text that is not JSON is kept as it is.
    public static string Prefix(PhaseKind kind, string proposal, int n) =>
        MapElements(kind, proposal, id => $"{n}:{Bare(id)}");

    // The same proposal without the prefixes (the validator's context).
    public static string Unprefix(PhaseKind kind, string proposal) => MapElements(kind, proposal, Bare);

    // The arbiter's output without the prefixes: in `de`, "1:" from the ids of
    // propuesta_1 and "2:" from those of propuesta_2; in the discards, the
    // prefix of their `propuesta`. A wrong prefix ("2:P-005" in propuesta_1)
    // stays, so the validator reports it as an id that does not exist there.
    public static string Strip(string output)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(output);
        }
        catch (JsonException)
        {
            return output;
        }
        if (root is null)
            return output;
        StripIn(root);
        return root.ToJsonString(Indented);
    }

    // One error per id cited under the wrong proposal ("2:P-005" in
    // propuesta_1, or a discard of proposal 1 with "2:"), the mistake the
    // prefixes exist to catch. The schema would reject them too, but with a
    // message about a pattern that does not help the repair.
    public static IReadOnlyList<string> WrongPrefixes(string output)
    {
        var errors = new List<string>();
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(output);
        }
        catch (JsonException)
        {
            return errors;
        }
        void Visit(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject obj:
                    if (obj["de"] is JsonObject de)
                        for (var n = 1; n <= 2; n++)
                            foreach (var id in (de[$"propuesta_{n}"] as JsonArray ?? []).OfType<JsonValue>()
                                         .Select(v => v.TryGetValue<string>(out var s) ? s : null).OfType<string>())
                                if (OtherProposal(id, n) is { } other)
                                    errors.Add($"{obj["id"]} ARB-DE: {id} está en `de.propuesta_{n}`, pero es un id de la propuesta {other}.");
                    if (obj["propuesta"] is JsonValue p && p.TryGetValue<int>(out var proposal)
                        && obj["id"] is JsonValue v2 && v2.TryGetValue<string>(out var discarded)
                        && OtherProposal(discarded, proposal) is { } wrong)
                        errors.Add($"DESCARTES ARB-DE: {discarded} se descarta como de la propuesta {proposal}, pero es un id de la propuesta {wrong}.");
                    foreach (var (_, child) in obj)
                        Visit(child);
                    break;
                case JsonArray array:
                    foreach (var child in array)
                        Visit(child);
                    break;
            }
        }
        Visit(root);
        return errors;
    }

    // The other proposal when the id carries its prefix ("2:" while n is 1).
    private static int? OtherProposal(string id, int n)
    {
        var other = 3 - n;
        return id.StartsWith($"{other}:", StringComparison.Ordinal) ? other : null;
    }

    private static void StripIn(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["de"] is JsonObject de)
                    for (var n = 1; n <= 2; n++)
                        if (de[$"propuesta_{n}"] is JsonArray ids)
                            StripArray(ids, n);
                if (obj["propuesta"] is JsonValue p && p.TryGetValue<int>(out var proposal)
                    && obj["id"] is JsonValue idValue && idValue.TryGetValue<string>(out var id))
                    obj["id"] = WithoutOwnPrefix(id, proposal);
                foreach (var (_, child) in obj.ToList())
                    if (child is not null)
                        StripIn(child);
                break;
            case JsonArray array:
                foreach (var child in array)
                    if (child is not null)
                        StripIn(child);
                break;
        }
    }

    private static void StripArray(JsonArray ids, int n)
    {
        for (var i = 0; i < ids.Count; i++)
            if (ids[i] is JsonValue v && v.TryGetValue<string>(out var id))
                ids[i] = WithoutOwnPrefix(id, n);
    }

    private static string WithoutOwnPrefix(string id, int n) => id.StartsWith($"{n}:", StringComparison.Ordinal) ? id[2..] : id;

    private static string MapElements(PhaseKind kind, string proposal, Func<string, string> map)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(proposal);
        }
        catch (JsonException)
        {
            return proposal;
        }
        if (root is null)
            return proposal;
        foreach (var element in Elements(kind, root))
            if (element["id"] is JsonValue v && v.TryGetValue<string>(out var id))
                element["id"] = map(id);
        return root.ToJsonString(Indented) + "\n";
    }

    // The elements the arbiter compares: those its `de` cites.
    private static IEnumerable<JsonObject> Elements(PhaseKind kind, JsonNode root) => kind switch
    {
        PhaseKind.Questions => Items(root["preguntas"]),
        PhaseKind.Catalog => Items(root["requisitos"]),
        PhaseKind.Backlog => Items(root["epicas"]).SelectMany(e => Items(e["historias"])),
        _ => [],
    };

    private static IEnumerable<JsonObject> Items(JsonNode? list) => list is JsonArray array ? array.OfType<JsonObject>() : [];

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
