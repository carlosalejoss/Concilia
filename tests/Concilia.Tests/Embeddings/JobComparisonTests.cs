using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;

namespace Concilia.Tests.Embeddings;

// Step 100: A, B and the arbiter side by side. With the Fake embedder (same
// words give similarity 1; no shared word gives 0).
public class JobComparisonTests
{
    private static readonly MatchThresholds Thresholds = new(Same: 0.9, Similar: 0.7);

    // A's RF-001 and B's RF-001 say the same; A's RF-002 and B's RF-002 have
    // nothing alike in the other proposal; A's RF-003 is the same as B's RF-001.
    private const string ProposalA = """
        {"requisitos": [
          {"id": "RF-001", "texto": "reservar una sala"},
          {"id": "RF-002", "texto": "exportar el informe mensual"},
          {"id": "RF-003", "texto": "reservar una sala"}
        ]}
        """;

    private const string ProposalB = """
        {"requisitos": [
          {"id": "RF-001", "texto": "reservar una sala"},
          {"id": "RF-002", "texto": "cancelar la cuenta"}
        ]}
        """;

    // Proposal 1 is B and proposal 2 is A, as the plan may say. The arbiter
    // joins the two RF-001, keeps B's RF-002 alone with a discrepancy, joins
    // A's RF-002 with nothing alike (a deviation), discards A's RF-003 and
    // says nothing of the rest.
    private const string Arbiter = """
        {"requisitos": [
          {"id": "RF-001", "texto": "El sistema deberá permitir reservar una sala.", "de": {"propuesta_1": ["RF-001"], "propuesta_2": ["RF-001"]},
           "discrepancia": false, "explicacion": "Dicen lo mismo."},
          {"id": "RF-002", "texto": "El sistema deberá permitir cancelar la cuenta.", "de": {"propuesta_1": ["RF-002"], "propuesta_2": ["RF-002"]},
           "discrepancia": true, "explicacion": "No está claro."}
        ],
         "descartados": [{"propuesta": 2, "id": "RF-003", "motivo": "Repite RF-001."}]}
        """;

    private const string ArbiterForgetting = """
        {"requisitos": [
          {"id": "RF-001", "texto": "reservar", "de": {"propuesta_1": ["RF-001"], "propuesta_2": ["RF-001"]}, "discrepancia": false, "explicacion": ""}
        ],
         "descartados": []}
        """;

    private static readonly string[] Order = ["B", "A"];

    private static JobComparison Build(string arbiter)
    {
        var similarities = ProposalMatcher.Similarities(ProposalMatcher.ElementsOf(PhaseKind.Catalog, ProposalA),
            ProposalMatcher.ElementsOf(PhaseKind.Catalog, ProposalB), new FakeTextEmbedder());
        var matching = ProposalMatcher.Classify(similarities, Thresholds, ProposalMatcher.ArbiterPairs(PhaseKind.Catalog, arbiter, Order));
        var deviations = ArbiterDeviations.Find(PhaseKind.Catalog, arbiter, matching, Order);
        return JobComparison.Build(PhaseKind.Catalog, arbiter, Order, matching, deviations);
    }

    [Fact]
    public void Each_element_of_the_merge_is_a_row_with_what_it_joined_of_A_and_of_B()
    {
        var rows = Build(Arbiter).Rows;

        var rf1 = rows[0];
        Assert.Equal(ComparisonRowKind.Merged, rf1.Kind);
        Assert.Equal("RF-001", rf1.ArbiterId);
        Assert.Equal("El sistema deberá permitir reservar una sala.", rf1.ArbiterText);
        Assert.Equal("ambas", rf1.Origin);
        // propuesta_2 is A: the slots come from the order of the plan.
        Assert.Equal("reservar una sala", Assert.Single(rf1.A).Text);
        Assert.Equal("RF-001", Assert.Single(rf1.B).Id);
        Assert.Equal(MatchLevel.Same, rf1.A[0].Level);
        Assert.True(rf1.AgreesWithEmbeddings);
        Assert.Empty(rf1.Deviations);
    }

    [Fact]
    public void A_row_says_where_the_arbiter_went_against_the_embeddings()
    {
        var rf2 = Build(Arbiter).Rows[1];

        Assert.True(rf2.Discrepancy);
        Assert.Equal("No está claro.", rf2.Explanation);
        // It joined two elements with nothing alike: the embeddings disagree.
        Assert.Equal(MatchLevel.Different, rf2.A[0].Level);
        Assert.True(rf2.A[0].ArbiterJoined);
        Assert.False(rf2.AgreesWithEmbeddings);
        Assert.Contains("ve distintos", Assert.Single(rf2.Deviations));
    }

    [Fact]
    public void Discarded_elements_have_a_row_with_the_reason()
    {
        var discarded = Assert.Single(Build(Arbiter).Rows, r => r.Kind == ComparisonRowKind.Discarded);

        Assert.Equal("RF-003", Assert.Single(discarded.A).Id);
        Assert.Empty(discarded.B);
        Assert.Equal("Repite RF-001.", discarded.Reason);
        Assert.Null(discarded.ArbiterId);
        // "Lo mismo" as B's RF-001 but not joined: it counts as a disagreement, as in the Agreement of the matching.
        Assert.False(discarded.AgreesWithEmbeddings);
    }

    [Fact]
    public void What_the_arbiter_did_not_classify_has_a_row_too()
    {
        var unclassified = Build(ArbiterForgetting).Rows.Where(r => r.Kind == ComparisonRowKind.Unclassified).ToList();

        Assert.Equal(["RF-002", "RF-003"], unclassified.SelectMany(r => r.A).Select(e => e.Id));
        Assert.Equal(["RF-002"], unclassified.SelectMany(r => r.B).Select(e => e.Id));
    }

    [Fact]
    public void It_carries_the_counts_of_the_matching()
    {
        var comparison = Build(Arbiter);

        Assert.Equal("Catalog", comparison.Phase);
        Assert.Equal(3, comparison.Counts.A);
        Assert.Equal(1, comparison.Counts.OnlyA); // A's RF-002
        Assert.Equal(1, comparison.Counts.OnlyB); // B's RF-002
        Assert.NotNull(comparison.Agreement);
    }
}
