using System.Text.Json;

namespace Concilia.Core.Phases;

// Keys written twice in the same JSON object, such as
// "de": {"propuesta_2": [...], "propuesta_2": [...]} (LUNA as the arbiter of
// a catalog, 16-10-2026). JSON allows reading them, but JsonNode, which the
// validator and the cleaning of the outputs use, throws when it meets one:
// the review stopped with an internal error instead of asking for a repair.
// JsonDocument does read them, so they are found first and reported as an
// error of the output, with where they are.
public static class JsonDuplicates
{
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
