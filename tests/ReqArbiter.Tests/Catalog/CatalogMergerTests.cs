using System.Text.Json;
using System.Text.Json.Nodes;
using ReqArbiter.Core.Catalog;

namespace ReqArbiter.Tests.Catalog;

// Two synthetic lots: the ReservaSalas example catalog (lot 1) and the same
// catalog as if it came from a second document, DOC-02 (lot 2). Each lot
// numbers its ids from REQ-001 and Q-001; the merge renumbers them and
// rewrites every reference.
public class CatalogMergerTests
{
    private const string System = "el sistema ReservaSalas";
    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static string ExamplePath(string file) => Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", file);

    private static string Lot1 => File.ReadAllText(ExamplePath("catalogo_ejemplo.json"));
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

    [Fact]
    public void Ids_are_renumbered_consecutively_across_lots()
    {
        var merged = Merged();

        Assert.Equal(Enumerable.Range(1, 26).Select(i => $"REQ-{i:D3}"), Ids(merged, "requirements"));
        Assert.Equal(Enumerable.Range(1, 6).Select(i => $"Q-{i:D3}"), Ids(merged, "open_questions"));
    }

    [Fact]
    public void References_of_the_second_lot_point_to_its_new_ids()
    {
        var lot = JsonNode.Parse(Lot1)!.AsObject();
        var merged = Merged();
        var requirementsInLot1 = lot["requirements"]!.AsArray().Count; // 13
        var questionsInLot1 = lot["open_questions"]!.AsArray().Count;  // 3

        string Shift(string id, string prefix, int by) => $"{prefix}-{int.Parse(id[(prefix.Length + 1)..]) + by:D3}";

        for (var i = 0; i < requirementsInLot1; i++)
        {
            var original = lot["requirements"]![i]!;
            var copy = merged["requirements"]![requirementsInLot1 + i]!;
            Assert.Equal(Strings(original["open_question_ids"]).Select(q => Shift(q, "Q", questionsInLot1)), Strings(copy["open_question_ids"]));
            Assert.Equal(Strings(original["derived_from"]).Select(r => Shift(r, "REQ", requirementsInLot1)), Strings(copy["derived_from"]));
        }
        for (var i = 0; i < questionsInLot1; i++)
        {
            var original = lot["open_questions"]![i]!;
            var copy = merged["open_questions"]![questionsInLot1 + i]!;
            Assert.Equal(Strings(original["related_requirements"]).Select(r => Shift(r, "REQ", requirementsInLot1)), Strings(copy["related_requirements"]));
        }
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
    public void The_verdict_adds_the_counts_of_the_lots()
    {
        var merged = Merged();

        Assert.Equal(26, merged["verdict"]!["counts"]!["requirements"]!.GetValue<int>());
        Assert.Equal(4, merged["verdict"]!["counts"]!["discarded"]!.GetValue<int>());
        Assert.Equal("complete", merged["verdict"]!["status"]!.GetValue<string>());
        Assert.Equal(4, merged["discarded_statements"]!.AsArray().Count);
    }

    [Fact]
    public void The_merged_catalog_is_valid_for_the_fragments_of_both_lots()
    {
        var report = CatalogValidator.Validate(CatalogMerger.Merge([Lot1, Lot2]), System, BothDocuments());

        Assert.True(report.Valid, string.Join("\n", report.Errors));
        Assert.Empty(report.Warnings);
        Assert.Equal(26, report.Metrics!.Requirements);
    }

    [Fact]
    public void A_single_lot_is_left_as_it_was()
    {
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(Lot1), JsonNode.Parse(CatalogMerger.Merge([Lot1]))));
    }
}
