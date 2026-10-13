using System.Text.Json;
using System.Text.Json.Nodes;

namespace Concilia.Core.Catalog;

// The catalogs (v1.3) FakeProvider writes, so that the whole flow (validation,
// repair, merge and web) can be tried without the gateway. No model is
// involved:
// - For the ReservaSalas example (the same fragments as prompts/.../ejemplo),
//   its reference catalog (catalogo_ejemplo_v1.3.json), with the project's
//   system name.
// - For any other input, a valid catalog that discards every fragment.
// - Mutated (for a simulated repair): the same catalog with an error of the
//   lot (wrong schema version), which asks for a repair.
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
            catalog["schema_version"] = "0.0"; // SCHEMA of the lot: the schema only accepts "1.3"
        }
        return catalog.ToJsonString(Indented);
    }

    private static bool IsReservaSalas(List<Fragment> fragments)
    {
        var example = JsonSerializer.Deserialize<Input>(PromptBuilder.ReadResource("ejemplo_fragmentos.json"), SnakeCase)!.Fragments;
        return fragments.Select(f => (f.Id, f.Text)).SequenceEqual(example.Select(f => (f.Id, f.Text)));
    }

    // The reference catalog names "el sistema ReservaSalas"; the project may
    // call it otherwise (the normalizer would fix it, but with a FIX warning).
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
            ["schema_version"] = "1.3",
            ["system"] = system,
            ["language"] = language,
            ["capabilities"] = new JsonArray(),
            ["requirements"] = new JsonArray(),
            ["open_questions"] = new JsonArray(),
            ["discarded_statements"] = discarded,
            ["discarded_sections"] = new JsonArray(),
            ["domain_terms"] = new JsonArray(),
            ["verdict"] = new JsonObject
            {
                ["status"] = "complete",
                ["self_check"] = new JsonObject
                {
                    ["quotes_are_literal"] = true,
                    ["figures_from_source"] = true,
                    ["all_fragments_accounted"] = true,
                    ["all_capabilities_covered"] = true,
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
