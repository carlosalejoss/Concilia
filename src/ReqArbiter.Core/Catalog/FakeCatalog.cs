using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReqArbiter.Core.Catalog;

// The catalogs FakeProvider writes, so that the whole flow (validation,
// repair, merge and web) can be tried without the gateway. No model is
// involved:
// - For the ReservaSalas example (the same fragments as prompts/.../ejemplo),
//   its reference catalog, with the project's system name.
// - For any other input, a valid catalog that discards every fragment.
// - Mutated (for a simulated repair): the same catalog with a schema error.
public static class FakeCatalog
{
    public const string FragmentsFile = "input/fragmentos.json";

    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // What BuildRequest writes in input/fragmentos.json.
    private record Input(string? System, string? Language, List<Fragment> Fragments);

    public static string For(string workDirectory, bool mutated)
    {
        var input = JsonSerializer.Deserialize<Input>(File.ReadAllText(Path.Combine(workDirectory, FragmentsFile)), SnakeCase)!;
        var system = input.System ?? "el sistema";
        var language = input.Language ?? "es";

        var catalog = IsReservaSalas(input.Fragments) && language == "es"
            ? ExampleCatalog(system)
            : DiscardEverything(system, language, input.Fragments);

        if (mutated)
        {
            catalog["schema_version"] = "0.0"; // SCHEMA: the schema only accepts "1.2"
        }
        return catalog.ToJsonString(Indented);
    }

    private static bool IsReservaSalas(List<Fragment> fragments)
    {
        var example = JsonSerializer.Deserialize<Input>(PromptBuilder.ReadResource("ejemplo_fragmentos.json"), SnakeCase)!.Fragments;
        return fragments.Select(f => (f.Id, f.Text)).SequenceEqual(example.Select(f => (f.Id, f.Text)));
    }

    // The reference catalog names "el sistema ReservaSalas"; the project may
    // call it otherwise, and the validator checks the name (C3).
    private static JsonObject ExampleCatalog(string system)
    {
        var catalog = JsonNode.Parse(PromptBuilder.ReadResource("ejemplo_catalogo.json"))!.AsObject();
        var original = catalog["system"]!.GetValue<string>();
        catalog["system"] = system;
        foreach (var requirement in catalog["requirements"]!.AsArray())
        {
            if (requirement!["slots"]?["system"] is JsonValue value && value.GetValue<string>() == original)
            {
                requirement["slots"]!["system"] = system;
            }
        }
        return catalog;
    }

    private static JsonObject DiscardEverything(string system, string language, List<Fragment> fragments)
    {
        var note = language == "en"
            ? "Simulated catalog written by FakeProvider: no model has read this fragment."
            : "Catálogo simulado por FakeProvider: ningún modelo ha leído este fragmento.";
        var discarded = new JsonArray(fragments.Select(f => (JsonNode)new JsonObject
        {
            ["fragment"] = f.Id,
            ["quote"] = null,
            ["reason"] = "context",
            ["note"] = note,
        }).ToArray());

        return new JsonObject
        {
            ["schema_version"] = "1.2",
            ["system"] = system,
            ["language"] = language,
            ["requirements"] = new JsonArray(),
            ["open_questions"] = new JsonArray(),
            ["discarded_statements"] = discarded,
            ["domain_terms"] = new JsonArray(),
            ["verdict"] = new JsonObject
            {
                ["status"] = "complete",
                ["counts"] = new JsonObject
                {
                    ["requirements"] = 0,
                    ["ears"] = 0,
                    ["master"] = 0,
                    ["open_questions"] = 0,
                    ["discarded"] = fragments.Count,
                },
                ["self_check"] = new JsonObject
                {
                    ["quotes_are_literal"] = true,
                    ["figures_from_source"] = true,
                    ["all_fragments_accounted"] = true,
                    ["no_vague_terms"] = true,
                    ["ears_only_shall"] = true,
                    ["decision_tree_consistent"] = true,
                },
                ["issues"] = new JsonArray(),
                ["unprocessed_fragments"] = new JsonArray(),
            },
        };
    }
}
