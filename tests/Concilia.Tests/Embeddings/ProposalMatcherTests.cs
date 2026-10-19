using System.Text.Json.Nodes;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;

namespace Concilia.Tests.Embeddings;

// With the Fake embedder: same words give similarity 1 and no shared word gives 0.
public class ProposalMatcherTests
{
    private static readonly MatchThresholds Thresholds = new(Same: 0.8, Similar: 0.5);
    private readonly FakeTextEmbedder _embedder = new();

    [Fact]
    public void Thresholds_split_the_similarity_in_three_levels()
    {
        Assert.Equal(MatchLevel.Same, Thresholds.LevelOf(0.8));
        Assert.Equal(MatchLevel.Similar, Thresholds.LevelOf(0.79));
        Assert.Equal(MatchLevel.Similar, Thresholds.LevelOf(0.5));
        Assert.Equal(MatchLevel.Different, Thresholds.LevelOf(0.49));
    }

    [Fact]
    public void Each_element_is_paired_with_the_most_similar_one_of_the_other_proposal()
    {
        MatchElement[] a = [new("RF-001", "reservar una sala"), new("RF-002", "exportar el informe mensual")];
        MatchElement[] b = [new("RF-001", "exportar el informe mensual"), new("RF-002", "reservar una sala")];

        var result = ProposalMatcher.Match(a, b, _embedder, Thresholds);

        var first = result.Elements.Single(e => e.Slot == "A" && e.Id == "RF-001");
        Assert.Equal("RF-002", first.BestId);
        Assert.Equal(MatchLevel.Same, first.Level);
        Assert.Equal(1, first.Similarity, precision: 6);
        Assert.Equal(new MatchCounts(2, 2, 2, 2, 0, 0, 0, 0), result.Counts);
        Assert.Equal(2, result.Pairs.Count); // each pair once, although both sides chose it
    }

    [Fact]
    public void An_element_without_a_close_one_is_only_in_its_proposal()
    {
        MatchElement[] a = [new("P-001", "¿Quién aprueba las reservas?")];
        MatchElement[] b = [new("P-001", "¿Cuántas salas hay?")];

        var result = ProposalMatcher.Match(a, b, _embedder, Thresholds);

        Assert.All(result.Elements, e => Assert.Equal(MatchLevel.Different, e.Level));
        Assert.Equal(1, result.Counts.OnlyA);
        Assert.Equal(1, result.Counts.OnlyB);
    }

    [Fact]
    public void Several_elements_can_pair_with_the_same_one()
    {
        MatchElement[] a = [new("RF-001", "reservar una sala grande"), new("RF-002", "reservar una sala pequeña")];
        MatchElement[] b = [new("RF-001", "reservar una sala")];

        var result = ProposalMatcher.Match(a, b, _embedder, Thresholds);

        Assert.All(result.Elements.Where(e => e.Slot == "A"), e => Assert.Equal("RF-001", e.BestId));
        Assert.Equal(2, result.Pairs.Count);
    }

    [Fact]
    public void An_empty_proposal_leaves_every_element_of_the_other_alone()
    {
        var result = ProposalMatcher.Match([new("RF-001", "reservar una sala")], [], _embedder, Thresholds);

        var only = Assert.Single(result.Elements);
        Assert.Null(only.BestId);
        Assert.Equal(MatchLevel.Different, only.Level);
        Assert.Empty(result.Pairs);
    }

    [Fact]
    public void The_arbiter_pairs_follow_the_order_of_the_proposals()
    {
        // Proposal 1 is B and proposal 2 is A: the arbiter joined A's RF-003 with B's RF-001 and RF-002.
        var output = """
            {"requisitos": [
              {"id": "RF-001", "texto": "x", "de": {"propuesta_1": ["RF-001", "RF-002"], "propuesta_2": ["RF-003"]}},
              {"id": "RF-002", "texto": "y", "de": {"propuesta_1": [], "propuesta_2": ["RF-004"]}}
            ]}
            """;

        var pairs = ProposalMatcher.ArbiterPairs(PhaseKind.Catalog, output, ["B", "A"]);

        Assert.Equal([("RF-003", "RF-001"), ("RF-003", "RF-002")], pairs.Order());
    }

    [Fact]
    public void Agreement_counts_whether_embeddings_and_arbiter_both_say_an_element_is_in_the_other_proposal()
    {
        MatchElement[] a = [new("RF-001", "reservar una sala"), new("RF-002", "exportar el informe")];
        MatchElement[] b = [new("RF-001", "reservar una sala"), new("RF-002", "cancelar la cuenta")];
        // The arbiter joined A RF-001 with B RF-001 (as the embeddings) and A RF-002 with B RF-002 (unlike them).
        HashSet<(string, string)> arbiter = [("RF-001", "RF-001"), ("RF-002", "RF-002")];

        var result = ProposalMatcher.Match(a, b, _embedder, Thresholds, arbiter);

        Assert.Equal(new Agreement(BothYes: 2, OnlyEmbeddings: 0, OnlyArbiter: 2, BothNo: 0), result.Agreement);
        Assert.Equal(0.5, result.Agreement!.Rate);
        Assert.True(result.Pairs.Single(p => p.AId == "RF-001" && p.BId == "RF-001").ArbiterJoined);
    }

    [Fact]
    public void A_similar_element_counts_as_present_in_the_other_proposal()
    {
        // "reservar una sala grande" and "reservar una sala" share 3 of 4 words:
        // similarity 0.87 with the Fake embedder, "parecido" with thresholds 0.9 and 0.5.
        MatchElement[] a = [new("RF-001", "reservar una sala grande")];
        MatchElement[] b = [new("RF-001", "reservar una sala")];
        HashSet<(string, string)> arbiter = [("RF-001", "RF-001")];

        var result = ProposalMatcher.Match(a, b, _embedder, new MatchThresholds(0.9, 0.5), arbiter);

        Assert.All(result.Elements, e => Assert.Equal(MatchLevel.Similar, e.Level));
        Assert.Equal(new Agreement(BothYes: 2, OnlyEmbeddings: 0, OnlyArbiter: 0, BothNo: 0), result.Agreement);
    }

    [Fact]
    public void Without_arbiter_there_is_no_agreement()
    {
        var result = ProposalMatcher.Match([new("RF-001", "a")], [new("RF-001", "a")], _embedder, Thresholds);

        Assert.Null(result.Agreement);
        Assert.Null(result.Elements[0].ArbiterJoined);
    }

    [Fact]
    public void The_text_of_each_element_depends_on_the_phase()
    {
        var story = JsonNode.Parse("""{"id": "HU-001", "titulo": "Reservar sala", "como": "empleado", "quiero": "reservar una sala", "para": "reunirme"}""")!.AsObject();
        var proposal = """{"preguntas": [{"id": "P-001", "pregunta": "¿Quién aprueba?", "motivo": "m", "fuentes": []}]}""";

        Assert.Equal("Reservar sala. Como empleado, quiero reservar una sala, para reunirme.", ProposalMatcher.TextOf(PhaseKind.Backlog, story));
        Assert.Equal([new MatchElement("P-001", "¿Quién aprueba?")], ProposalMatcher.ElementsOf(PhaseKind.Questions, proposal));
    }
}
