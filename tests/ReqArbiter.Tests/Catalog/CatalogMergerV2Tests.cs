using System.Text.Json;
using System.Text.Json.Nodes;
using ReqArbiter.Core.Catalog;

namespace ReqArbiter.Tests.Catalog;

// The ReservaSalas example v2 split in two lots, as the platform would: lot 1
// with CAP-001 to CAP-003 (RF-001 to RF-006) and lot 2 with CAP-004 to CAP-006
// (RF-007, RNF-001, RA-001 and the question Q-001). Each lot is validated on
// its own (its ids start again at 001) and then merged with the inventory.
public class CatalogMergerV2Tests
{
    private const string System = "el sistema ReservaSalas";
    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private static readonly string[] Lot1Caps = ["CAP-001", "CAP-002", "CAP-003"];
    private static readonly string[] Lot2Caps = ["CAP-004", "CAP-005", "CAP-006"];

    private static string ExamplePath(string file) => Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", file);

    private static string Inventory => File.ReadAllText(ExamplePath("inventario_ejemplo_v2.json"));

    private record FragmentsFile(List<Fragment> Fragments);

    private static List<Fragment> Fragments =>
        JsonSerializer.Deserialize<FragmentsFile>(File.ReadAllText(ExamplePath("fragmentos_v2.json")), SnakeCase)!.Fragments;

    // The example catalog with only the requirements of these capabilities (and
    // the questions only in lot 2), changed by `change`, validated as a lot.
    private static string Lot(string[] caps, Action<JsonObject>? change = null)
    {
        var catalog = JsonNode.Parse(File.ReadAllText(ExamplePath("catalogo_ejemplo_v2.json")))!.AsObject();
        var requirements = catalog["requirements"]!.AsArray()
            .Where(r => r!["capability_ids"]!.AsArray().Any(c => caps.Contains(c!.GetValue<string>())))
            .Select(r => r!.DeepClone()).ToArray();
        catalog["requirements"] = new JsonArray(requirements);
        if (!caps.Contains("CAP-005"))
            catalog["open_questions"] = new JsonArray();
        change?.Invoke(catalog);

        var report = CatalogValidatorV2.Validate(catalog.ToJsonString(), System, Fragments, Inventory, caps);
        Assert.True(report.Valid && !report.NeedsRepair && report.Rejected.Count == 0, string.Join("\n", report.Warnings.Concat(report.RepairReasons)));
        return report.Catalog!;
    }

    private static JsonObject Merge(string lot1, string lot2) =>
        JsonNode.Parse(CatalogMergerV2.Merge(Inventory, [lot1, lot2]).Catalog)!.AsObject();

    private static List<string> Strings(JsonNode? array) => array!.AsArray().Select(s => s!.GetValue<string>()).ToList();

    private static JsonObject Item(JsonObject catalog, string list, string id) =>
        catalog[list]!.AsArray().Single(x => x!["id"]!.GetValue<string>() == id)!.AsObject();

    private static JsonObject AddQuestion(JsonObject catalog, string id, string type, string text, string requirementId, string fragment, string quote)
    {
        var question = new JsonObject
        {
            ["id"] = id, ["type"] = type, ["question"] = text,
            ["related_requirements"] = new JsonArray(requirementId),
            ["source_trace"] = new JsonArray(new JsonObject { ["fragment"] = fragment, ["quote"] = quote }),
        };
        catalog["open_questions"]!.AsArray().Add(question);
        return question;
    }

    [Fact]
    public void Requirements_are_renumbered_by_type_across_lots()
    {
        var merged = Merge(Lot(Lot1Caps), Lot(Lot2Caps));

        var ids = merged["requirements"]!.AsArray().Select(r => r!["id"]!.GetValue<string>()).ToList();
        Assert.Equal(["RF-001", "RF-002", "RF-003", "RF-004", "RF-005", "RF-006", "RF-007", "RNF-001", "RA-001"], ids);
        Assert.Equal(["CAP-004"], Strings(Item(merged, "requirements", "RF-007")["capability_ids"]));
    }

    [Fact]
    public void Capabilities_and_discards_come_from_the_inventory()
    {
        var merged = Merge(Lot(Lot1Caps), Lot(Lot2Caps));
        var inventory = JsonNode.Parse(Inventory)!;

        Assert.True(JsonNode.DeepEquals(inventory["capabilities"], merged["capabilities"]));
        Assert.True(JsonNode.DeepEquals(inventory["discarded_statements"], merged["discarded_statements"]));
        Assert.True(JsonNode.DeepEquals(inventory["discarded_sections"], merged["discarded_sections"]));
        Assert.Equal("2.0", merged["schema_version"]!.GetValue<string>());
    }

    [Fact]
    public void Inventory_and_lot_questions_are_one_list_ordered_by_type()
    {
        var merged = Merge(Lot(Lot1Caps), Lot(Lot2Caps));

        // QI-001 (scope) goes before the lot's Q-001 (missing_value): both are now Q-.
        var questions = merged["open_questions"]!.AsArray();
        Assert.Equal(["Q-001", "Q-002"], questions.Select(q => q!["id"]!.GetValue<string>()));
        Assert.Equal(["scope", "missing_value"], questions.Select(q => q!["type"]!.GetValue<string>()));

        // The missing_value question keeps its link with RNF-001, in both directions, and gains its capability.
        var missingValue = Item(merged, "open_questions", "Q-002");
        Assert.Equal(["RNF-001"], Strings(missingValue["related_requirements"]));
        Assert.Equal(["CAP-005"], Strings(missingValue["related_capabilities"]));
        Assert.Equal(["Q-002"], Strings(Item(merged, "requirements", "RNF-001")["open_question_ids"]));
        Assert.Equal(["id", "type", "question", "related_capabilities", "related_requirements", "source_trace"],
            missingValue.Select(p => p.Key));
    }

    [Fact]
    public void A_requirement_that_cites_an_inventory_question_completes_the_link()
    {
        var lot1 = Lot(Lot1Caps, c => Item(c, "requirements", "RF-005")["open_question_ids"]!.AsArray().Add("QI-001"));

        var merged = Merge(lot1, Lot(Lot2Caps));

        var scope = Item(merged, "open_questions", "Q-001");
        Assert.Equal(["RF-005"], Strings(scope["related_requirements"]));
        Assert.Equal(["CAP-002"], Strings(scope["related_capabilities"]));
        Assert.Equal(["Q-001"], Strings(Item(merged, "requirements", "RF-005")["open_question_ids"]));
    }

    [Fact]
    public void Identical_questions_of_two_lots_are_merged_with_a_fix()
    {
        const string text = "¿Qué ocurre con la reserva si la sala deja de estar disponible?";
        var lot1 = Lot(Lot1Caps, c =>
        {
            AddQuestion(c, "Q-001", "ambiguity", text, "RF-001", "DOC-01#F004", "Si la sala ya está reservada en esa franja");
            Item(c, "requirements", "RF-001")["open_question_ids"] = new JsonArray("Q-001");
        });
        var lot2 = Lot(Lot2Caps, c =>
        {
            AddQuestion(c, "Q-002", "ambiguity", text.ToUpperInvariant(), "RF-007", "DOC-01#F011", "exportar el histórico de reservas a CSV");
            Item(c, "requirements", "RF-007")["open_question_ids"] = new JsonArray("Q-002");
        });

        var result = CatalogMergerV2.Merge(Inventory, [lot1, lot2]);
        var merged = JsonNode.Parse(result.Catalog)!.AsObject();

        var ambiguity = merged["open_questions"]!.AsArray().Single(q => q!["type"]!.GetValue<string>() == "ambiguity")!.AsObject();
        Assert.Equal(["RF-001", "RF-007"], Strings(ambiguity["related_requirements"]));
        Assert.Equal(["CAP-001", "CAP-004"], Strings(ambiguity["related_capabilities"]));
        Assert.Equal(2, ambiguity["source_trace"]!.AsArray().Count);
        Assert.Equal(Strings(Item(merged, "requirements", "RF-001")["open_question_ids"]), Strings(Item(merged, "requirements", "RF-007")["open_question_ids"]));
        Assert.Equal(3, merged["open_questions"]!.AsArray().Count); // scope, missing_value and the merged ambiguity
        Assert.Contains(result.Fixes, f => f.Contains(" FIX pregunta idéntica a lote 1 Q-001"));
    }

    [Fact]
    public void Objections_are_joined_domain_terms_deduplicated_and_verdicts_merged()
    {
        var term = new JsonObject { ["term"] = "modo festivo", ["fragment"] = "DOC-01#F007" };
        var lot1 = Lot(Lot1Caps, c => c["domain_terms"]!.AsArray().Add(term.DeepClone()));
        var lot2 = Lot(Lot2Caps, c =>
        {
            c["verdict"]!["status"] = "partial";
            c["verdict"]!["uncovered_capabilities"] = new JsonArray("CAP-004");
            c["capability_objections"]!.AsArray().Add(new JsonObject
            {
                ["capability_id"] = "CAP-004", ["reason"] = "future", ["duplicate_of"] = null, ["note"] = "Se hará más adelante.",
            });
            c["requirements"] = new JsonArray(c["requirements"]!.AsArray()
                .Where(r => r!["id"]!.GetValue<string>() != "RF-007").Select(r => r!.DeepClone()).ToArray());
        });
        var lot1Again = Lot(Lot1Caps, c => c["domain_terms"]!.AsArray().Add(new JsonObject { ["term"] = "Modo  Festivo", ["fragment"] = "DOC-01#F007" }));

        var merged = JsonNode.Parse(CatalogMergerV2.Merge(Inventory, [lot1, lot2, lot1Again]).Catalog)!.AsObject();

        Assert.Single(merged["capability_objections"]!.AsArray());
        Assert.Single(merged["domain_terms"]!.AsArray());
        Assert.Equal("partial", merged["verdict"]!["status"]!.GetValue<string>());
        Assert.Equal(["CAP-004"], Strings(merged["verdict"]!["uncovered_capabilities"]));
    }
}
