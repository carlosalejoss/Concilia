using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Concilia.Core.Phases;

// Keys written twice in the same JSON object, such as
// "de": {"propuesta_2": [...], "propuesta_2": [...]} (LUNA as the arbiter of
// a catalog, 16-10-2026). JSON allows reading them, but JsonNode, which the
// validator and the cleaning of the outputs use, throws when it meets one:
// the review stopped with an internal error instead of asking for a repair.
// JsonDocument does read them, so they are found first.
// When both values are lists, what the model meant is clear (two lists of ids
// of the same proposal), so the platform joins them, as it removes the "sigue"
// mark (Merge). Otherwise they are an error of the output (CLAVE-DUP) that
// asks for a repair: a repair of a big output, such as the arbiter's catalog,
// does not fit in the context (16-10-2026).
public static class JsonDuplicates
{
    // The output with each repeated key whose values are all lists turned into
    // one key with the items of all of them, in order and without repeating
    // an item. Null if some repeated key has a value that is not a list (it
    // cannot be joined) or the text is not JSON. The same text if nothing repeats.
    public static string? Merge(string json)
    {
        if (Find(json).Count == 0)
            return json;
        try
        {
            using var document = JsonDocument.Parse(json);
            return Rebuild(document.RootElement) is { } merged ? merged.ToJsonString(Indented) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // The element as a JsonNode, joining the repeated keys; null if one cannot be joined.
    private static JsonNode? Rebuild(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new JsonObject();
                foreach (var property in element.EnumerateObject())
                {
                    var value = Rebuild(property.Value);
                    if (value is null && property.Value.ValueKind != JsonValueKind.Null)
                        throw new JsonException("cannot be joined");
                    if (!obj.ContainsKey(property.Name))
                    {
                        obj[property.Name] = value;
                        continue;
                    }
                    if (obj[property.Name] is not JsonArray joined || value is not JsonArray more)
                        throw new JsonException("cannot be joined");
                    foreach (var item in more.ToList())
                    {
                        more.Remove(item);
                        if (!joined.Any(x => JsonNode.DeepEquals(x, item)))
                            joined.Add(item);
                    }
                }
                return obj;
            case JsonValueKind.Array:
                var array = new JsonArray();
                foreach (var item in element.EnumerateArray())
                {
                    var value = Rebuild(item);
                    if (value is null && item.ValueKind != JsonValueKind.Null)
                        throw new JsonException("cannot be joined");
                    array.Add(value);
                }
                return array;
            default:
                return JsonNode.Parse(element.GetRawText());
        }
    }

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // One line per repeated key: its path and its name, e.g. "requisitos[12].de: propuesta_2".
    // Empty if there is none, or if the text is not JSON (the validator reports that).
    public static IReadOnlyList<string> Find(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var found = new List<string>();
            Walk(document.RootElement, "", found);
            return found;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void Walk(JsonElement element, string path, List<string> found)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var seen = new HashSet<string>();
                foreach (var property in element.EnumerateObject())
                {
                    if (!seen.Add(property.Name))
                        found.Add($"{(path.Length == 0 ? "(raíz)" : path)}: {property.Name}");
                    Walk(property.Value, path.Length == 0 ? property.Name : $"{path}.{property.Name}", found);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                    Walk(item, $"{path}[{index++}]", found);
                break;
        }
    }
}
