using System.Text.Json;
using System.Text.Json.Nodes;
using LateApexEarlySpeed.Json.Schema;
using LateApexEarlySpeed.Json.Schema.Common;

namespace ReqArbiter.Core.Catalog;

// JSON and schema helpers shared by the catalog and inventory validators.
internal static class CatalogJson
{
    public static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Label(JsonNode? item, string fallback) =>
        item is JsonObject obj && obj["id"] is JsonValue v && v.TryGetValue<string>(out var id) ? id : fallback;

    public static string? Str(JsonNode node, string name) =>
        node is JsonObject obj && obj[name] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    public static List<string> StrList(JsonNode node, string name) =>
        node is JsonObject obj && obj[name] is JsonArray list
            ? list.Where(x => x is JsonValue v && v.TryGetValue<string>(out _)).Select(x => x!.GetValue<string>()).ToList()
            : [];

    // Python's bool(x.get(name)): false for missing, null, "" and [].
    public static bool Truthy(JsonObject obj, string name) => obj[name] switch
    {
        null => false,
        JsonArray list => list.Count > 0,
        JsonValue v when v.TryGetValue<string>(out var s) => s.Length > 0,
        JsonValue v when v.TryGetValue<bool>(out var b) => b,
        _ => true,
    };

    public static JsonArray StringArray(IEnumerable<string> values) =>
        new(values.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray());

    // Fragment ids of the traces of an element, also of one rejected by the schema (if they can be read).
    public static IEnumerable<string> TraceFragments(JsonNode? item)
    {
        if (item is not JsonObject obj || obj["source_trace"] is not JsonArray traces)
            yield break;
        foreach (var t in traces)
        {
            if (t is JsonObject trace && trace["fragment"] is JsonValue v && v.TryGetValue<string>(out var id))
                yield return id;
        }
    }

    // --- Schema ------------------------------------------------------------------------------

    // The root with the item lists emptied: its errors are those of the whole file.
    public static JsonNode? WithoutItems(JsonNode? root, IEnumerable<string> listKeys)
    {
        if (root is not JsonObject obj)
            return root;
        var copy = obj.DeepClone().AsObject();
        foreach (var key in listKeys)
        {
            if (copy[key] is JsonArray)
                copy[key] = new JsonArray();
        }
        return copy;
    }

    // "SCHEMA <path>: <message>" for each error, sorted like the reference.
    public static List<string> SchemaErrors(JsonValidator validator, JsonNode? instance, string basePath = "")
    {
        var element = JsonSerializer.SerializeToElement(instance);
        var result = validator.Validate(element, new JsonSchemaOptions { OutputFormat = OutputFormat.List });
        if (result.IsValid)
            return [];
        return result.ValidationErrors
            .Select(e =>
            {
                var path = e.InstanceLocation?.ToString() is { Length: > 0 } p ? p.TrimStart('#', '/') : "";
                var full = string.Join("/", new[] { basePath, path }.Where(x => x.Length > 0));
                return $"SCHEMA {(full.Length > 0 ? full : "(raíz)")}: {e.ErrorMessage ?? e.Keyword}";
            })
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    // A validator for one definition of a schema ("requirement", "capability"...).
    public static JsonValidator DefinitionValidator(string schemaText, string definition)
    {
        var schema = JsonNode.Parse(schemaText)!.AsObject();
        var sub = new JsonObject
        {
            ["$schema"] = schema["$schema"]!.DeepClone(),
            ["$defs"] = schema["$defs"]!.DeepClone(),
            ["$ref"] = $"#/$defs/{definition}",
        };
        return new JsonValidator(sub.ToJsonString());
    }

    // A file embedded in this DLL from prompts/generacion_requisitos/ (see ReqArbiter.Core.csproj).
    public static string ReadResource(string name)
    {
        using var stream = typeof(CatalogJson).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource not found: {name}");
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        return reader.ReadToEnd();
    }

    // Copy of the catalog with the rendered `text` after `slots` in each requirement.
    public static string Enrich(string catalogJson, IReadOnlyDictionary<string, string?> texts)
    {
        var root = JsonNode.Parse(catalogJson)!.AsObject();
        var requirements = root["requirements"]!.AsArray();
        for (var i = 0; i < requirements.Count; i++)
        {
            var original = requirements[i]!.AsObject();
            var enriched = new JsonObject();
            foreach (var (key, value) in original)
            {
                enriched[key] = value?.DeepClone();
                if (key == "slots")
                    enriched["text"] = texts.GetValueOrDefault(original["id"]!.GetValue<string>());
            }
            requirements[i] = enriched;
        }
        return root.ToJsonString(Indented);
    }
}
