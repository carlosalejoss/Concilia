using System.Text.Json;
using System.Text.Json.Nodes;
using Concilia.Core.Catalog;

namespace Concilia.Tests.Catalog;

// Two synthetic lots: the ReservaSalas example catalog v1.3 (lot 1) and the
// same catalog as if it came from a second document, DOC-02 (lot 2). Each lot
// numbers its ids from 001 (RF-, RNF-, RA-, Q-, CAP-); the merge renumbers
// each type on its own and rewrites every reference.
public class CatalogMergerTests
{
    private const string System = "el sistema ReservaSalas";
    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static string ExamplePath(string file) => Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", file);

    private static string Lot1 => File.ReadAllText(ExamplePath("catalogo_ejemplo_v1.3.json"));
    private static string Lot2 => Lot1.Replace("DOC-01#", "DOC-02#");

    private record FragmentsFile(List<Fragment> Fragments);

    private static List<Fragment> BothDocuments()
    {
        var doc1 = JsonSerializer.Deserialize<FragmentsFile>(File.ReadAllText(ExamplePath("fragmentos.json")), SnakeCase)!.Fragments;
        var doc2 = doc1.Select(f => f with { Id = f.Id.Replace("DOC-01#", "DOC-02#"), Source = "DOC-02" });
        return [.. doc1, .. doc2];
    }

    private static JsonObject Merged() => JsonNode.Parse(CatalogMerger.Merge([Lot1, Lot2]))!.AsObject();

    private static List<string> Ids(JsonObject catalog, string list) =>
        catalog[list]!.AsArray().Select(i => i!["id"]!.GetValue<string>()).ToList();

    private static List<string> Strings(JsonNode? array) => array!.AsArray().Select(s => s!.GetValue<string>()).ToList();

    private static JsonObject Req(JsonObject catalog, int index) => catalog["requirements"]![index]!.AsObject();

    [Fact]
    public void Each_type_is_renumbered_consecutively_across_lots()
    {
        var ids = Ids(Merged(), "requirements");

        Assert.Equal(Enumerable.Range(1, 14).Select(i => $"RF-{i:000}"), ids.Where(id => id.StartsWith("RF-")));
        Assert.Equal(["RNF-001", "RNF-002"], ids.Where(id => id.StartsWith("RNF-")));
        Assert.Equal(["RA-001", "RA-002"], ids.Where(id => id.StartsWith("RA-")));
        Assert.Equal(Enumerable.Range(1, 4).Select(i => $"Q-{i:000}"), Ids(Merged(), "open_questions"));
        Assert.Equal(Enumerable.Range(1, 12).Select(i => $"CAP-{i:000}"), Ids(Merged(), "capabilities"));
    }

    [Fact]
    public void References_of_the_second_lot_point_to_its_new_ids()
    {
        var merged = Merged();
        var count = JsonNode.Parse(Lot1)!["requirements"]!.AsArray().Count; // 9 per lot

        // RNF-001 of lot 2 is now RNF-002, linked to its question (Q-001 of lot 2, now Q-003).
        var rnf = Req(merged, count + 6);
        Assert.Equal("RNF-002", rnf["id"]!.GetValue<string>());
        Assert.Equal(["Q-003"], Strings(rnf["open_question_ids"]));
        Assert.Equal(["RNF-002"], Strings(merged["open_questions"]![2]!["related_requirements"]));

        // RF-001 of lot 2 (now RF-008) covers CAP-001 of lot 2 (now CAP-007).
        var rf = Req(merged, count);
        Assert.Equal("RF-008", rf["id"]!.GetValue<string>());
        Assert.Equal(["CAP-007"], Strings(rf["capability_ids"]));
    }

    [Fact]
    public void The_first_lot_keeps_its_ids_and_references()
    {
        var lot = JsonNode.Parse(Lot1)!.AsObject();
        var merged = Merged();

        for (var i = 0; i < lot["requirements"]!.AsArray().Count; i++)
        {
            Assert.True(JsonNode.DeepEquals(lot["requirements"]![i], merged["requirements"]![i]));
        }
    }

    [Fact]
    public void Discards_and_the_verdict_are_joined()
    {
        var merged = Merged();

        Assert.Equal(4, merged["discarded_statements"]!.AsArray().Count);
        Assert.Equal("complete", merged["verdict"]!["status"]!.GetValue<string>());
        Assert.True(merged["verdict"]!["self_check"]!["all_capabilities_covered"]!.GetValue<bool>());
    }

    [Fact]
    public void The_merged_catalog_is_valid_for_the_fragments_of_both_lots()
    {
        var report = CatalogValidator.Validate(CatalogMerger.Merge([Lot1, Lot2]), System, BothDocuments());

        Assert.True(report.Valid, string.Join("\n", report.Errors));
        Assert.False(report.NeedsRepair, string.Join("\n", report.RepairReasons));
        Assert.Empty(report.Warnings);
        Assert.Equal(18, report.Metrics!.Requirements);
        Assert.Equal("12/12", report.Metrics.CapabilityCoverage);
    }

    [Fact]
    public void A_single_lot_is_left_as_it_was()
    {
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(Lot1), JsonNode.Parse(CatalogMerger.Merge([Lot1]))));
    }
}
