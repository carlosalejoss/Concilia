using System.Text.Json.Nodes;
using Concilia.Core.Deliverables;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;

namespace Concilia.Tests.Embeddings;

// Step 88: the matching the arbiter gets, and where it went against it. With
// the Fake embedder (same words give similarity 1; no shared word gives 0).
public class ArbiterDeviationsTests
{
    private static readonly MatchThresholds Thresholds = new(Same: 0.9, Similar: 0.7);
    private readonly FakeTextEmbedder _embedder = new();

    // A: RF-001 the same as B's RF-001; RF-002 with nothing like it in B.
    // B: RF-001, and RF-002 with nothing like it in A.
    private const string ProposalA = """
        {"requisitos": [
          {"id": "RF-001", "texto": "reservar una sala"},
          {"id": "RF-002", "texto": "exportar el informe mensual"}
        ]}
        """;

    private const string ProposalB = """
        {"requisitos": [
          {"id": "RF-001", "texto": "reservar una sala"},
          {"id": "RF-002", "texto": "cancelar la cuenta"}
        ]}
        """;

    private MatchingResult Matching() => new ArbiterMatching(_embedder, Thresholds).Compute(PhaseKind.Catalog, ProposalA, ProposalB);

    [Fact]
    public void The_matching_file_uses_the_numbers_of_the_proposals()
    {
        // Proposal 1 is B and proposal 2 is A.
        var text = MatchingFile.Write(Matching(), ["B", "A"]);

        Assert.Equal(["1:RF-001 2:RF-001 mismo 1.00", "1:RF-002 sin-par", "2:RF-002 sin-par"], text.TrimEnd('\n').Split('\n'));
    }

    [Fact]
    public void Following_the_matching_leaves_no_deviation()
    {
        var output = Arbiter(("RF-001", ["RF-001"], ["RF-001"]), ("RF-002", ["RF-002"], []), ("RF-003", [], ["RF-002"]));

        Assert.Empty(ArbiterDeviations.Find(PhaseKind.Catalog, output, Matching(), ["A", "B"]));
    }

    [Fact]
    public void Joining_what_has_no_pair_is_a_deviation()
    {
        // The arbiter joined A's RF-002 with B's RF-002: nothing alike between them.
        var output = Arbiter(("RF-001", ["RF-001"], ["RF-001"]), ("RF-002", ["RF-002"], ["RF-002"]));

        var deviation = Assert.Single(ArbiterDeviations.Find(PhaseKind.Catalog, output, Matching(), ["A", "B"]).DistinctBy(d => d.ElementId));
        Assert.Equal("RF-002", deviation.ElementId);
        Assert.Equal(DeviationKind.JoinedDifferent, deviation.Kind);
        Assert.Contains("ve distintos", deviation.Explanation);
    }

    [Fact]
    public void Leaving_apart_what_is_the_same_marks_both_elements()
    {
        var output = Arbiter(("RF-001", ["RF-001"], []), ("RF-002", [], ["RF-001"]), ("RF-003", ["RF-002"], []), ("RF-004", [], ["RF-002"]));

        var deviations = ArbiterDeviations.Find(PhaseKind.Catalog, output, Matching(), ["A", "B"]);

        Assert.Equal(["RF-001", "RF-002"], deviations.Select(d => d.ElementId).Order());
        Assert.All(deviations, d => Assert.Equal(DeviationKind.SplitSame, d.Kind));
        Assert.Contains("«reservar una sala»", deviations[0].Explanation);
    }

    [Fact]
    public void Discarding_one_of_two_equal_elements_is_not_a_deviation()
    {
        // The arbiter kept A's RF-001 and discarded B's RF-001 as a repetition: it kept one of the two.
        var output = Arbiter(("RF-001", ["RF-001"], []), ("RF-002", ["RF-002"], []), ("RF-003", [], ["RF-002"]));

        Assert.Empty(ArbiterDeviations.Find(PhaseKind.Catalog, output, Matching(), ["A", "B"]));
    }

    [Fact]
    public void A_deviation_is_a_discrepancy_of_the_catalog_draft_with_the_platform_explanation()
    {
        var output = Arbiter(("RF-001", ["RF-001"], ["RF-001"]), ("RF-002", ["RF-002"], ["RF-002"]));
        var deviations = ArbiterDeviations.Find(PhaseKind.Catalog, output, Matching(), ["A", "B"]);
        var plan = new PhasePlan(PhaseKind.Catalog, new PhaseInput { SystemName = "", Language = "" }, [], [], ["A", "B"]);

        var draft = JsonNode.Parse(CatalogDraft.FromResult(new PhaseJobResult("ARB", true, output, "") { Deviations = deviations }, plan))!;

        var requirements = draft["requisitos"]!.AsArray();
        Assert.False(requirements[0]!["discrepancia"]!.GetValue<bool>());
        Assert.True(requirements[1]!["discrepancia"]!.GetValue<bool>());
        Assert.StartsWith("Unidos. [Plataforma] La plataforma ve distintos", requirements[1]!["explicacion"]!.GetValue<string>());
    }

    [Fact]
    public void The_discrepancy_of_the_arbiter_stays_without_deviations()
    {
        var arbiter = JsonNode.Parse(Arbiter(("RF-001", ["RF-001"], ["RF-001"])))!;
        arbiter["requisitos"]![0]!["discrepancia"] = true;
        var output = arbiter.ToJsonString();
        var plan = new PhasePlan(PhaseKind.Catalog, new PhaseInput { SystemName = "", Language = "" }, [], [], ["A", "B"]);

        var draft = JsonNode.Parse(CatalogDraft.FromResult(new PhaseJobResult("ARB", true, output, ""), plan))!;

        Assert.True(draft["requisitos"]![0]!["discrepancia"]!.GetValue<bool>());
        Assert.Equal("Unidos.", draft["requisitos"]![0]!["explicacion"]!.GetValue<string>());
    }

    // An arbiter's catalog: each requirement with the ids of A and B it comes from (order A, B).
    private static string Arbiter(params (string Id, string[] A, string[] B)[] requirements) =>
        new JsonObject
        {
            ["requisitos"] = new JsonArray(requirements.Select(r => (JsonNode)new JsonObject
            {
                ["id"] = r.Id, ["tipo"] = "RF", ["notacion"] = "EARS", ["obligatoriedad"] = "shall", ["texto"] = "x",
                ["fuentes"] = new JsonArray(), ["preguntas"] = new JsonArray(),
                ["de"] = new JsonObject
                {
                    ["propuesta_1"] = new JsonArray(r.A.Select(id => (JsonNode)id).ToArray()),
                    ["propuesta_2"] = new JsonArray(r.B.Select(id => (JsonNode)id).ToArray()),
                },
                ["discrepancia"] = false,
                ["explicacion"] = "Unidos.",
            }).ToArray()),
            ["descartados"] = new JsonArray(),
        }.ToJsonString();
}
