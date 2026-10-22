using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;

namespace Concilia.Tests.Embeddings;

// Step 100: the slow part of the matching is saved once per job, and the
// thresholds and the arbiter are applied on each use. With the Fake embedder.
public class MatchingCacheTests
{
    private const string ProposalA = """
        {"requisitos": [
          {"id": "RF-001", "texto": "reservar una sala"},
          {"id": "RF-002", "texto": "reservar una sala grande"},
          {"id": "RF-003", "texto": "exportar el informe mensual"}
        ]}
        """;

    private const string ProposalB = """
        {"requisitos": [
          {"id": "RF-001", "texto": "reservar una sala"},
          {"id": "RF-002", "texto": "cancelar la cuenta"}
        ]}
        """;

    private static readonly FakeTextEmbedder Embedder = new();

    private static ProposalSimilarities Similarities() => ProposalMatcher.Similarities(
        ProposalMatcher.ElementsOf(PhaseKind.Catalog, ProposalA), ProposalMatcher.ElementsOf(PhaseKind.Catalog, ProposalB), Embedder);

    [Fact]
    public void The_saved_similarities_give_the_same_matching_as_computing_it_again()
    {
        using var temp = new TempDirectory();
        MatchingCache.Write(temp.Path, Similarities());
        var saved = MatchingCache.Read(temp.Path, Embedder.Name)!;

        var thresholds = new MatchThresholds(0.9, 0.7);
        HashSet<(string, string)> arbiter = [("RF-001", "RF-001")];
        var direct = ProposalMatcher.Match(ProposalMatcher.ElementsOf(PhaseKind.Catalog, ProposalA),
            ProposalMatcher.ElementsOf(PhaseKind.Catalog, ProposalB), Embedder, thresholds, arbiter);
        var fromCache = ProposalMatcher.Classify(saved, thresholds, arbiter);

        Assert.Equal(direct.Counts, fromCache.Counts);
        Assert.Equal(direct.Agreement, fromCache.Agreement);
        Assert.Equal(direct.Elements, fromCache.Elements);
        Assert.Equal(direct.Pairs, fromCache.Pairs);
    }

    [Fact]
    public void Other_thresholds_do_not_need_the_embeddings_again()
    {
        var similarities = Similarities();

        // A's RF-002 shares 3 of its 4 words with B's RF-001 (0.87): "parecido"
        // with the thresholds of the author, "mismo" with a lower one.
        var withConfig = ProposalMatcher.Classify(similarities, new MatchThresholds(0.9, 0.7));
        var lower = ProposalMatcher.Classify(similarities, new MatchThresholds(0.8, 0.5));

        Assert.Equal(MatchLevel.Similar, withConfig.Elements.Single(e => e is { Slot: "A", Id: "RF-002" }).Level);
        Assert.Equal(MatchLevel.Same, lower.Elements.Single(e => e is { Slot: "A", Id: "RF-002" }).Level);
        Assert.Equal(withConfig.Elements.Select(e => e.BestId), lower.Elements.Select(e => e.BestId));
    }

    [Fact]
    public void Similarities_of_another_embedder_or_a_damaged_file_are_not_used()
    {
        using var temp = new TempDirectory();
        Assert.Null(MatchingCache.Read(temp.Path, Embedder.Name)); // nothing saved yet

        MatchingCache.Write(temp.Path, Similarities());
        Assert.Null(MatchingCache.Read(temp.Path, OnnxTextEmbedder.ModelName));
        Assert.NotNull(MatchingCache.Read(temp.Path, Embedder.Name));

        File.WriteAllText(Path.Combine(temp.Path, MatchingCache.FileName), "{ no es JSON");
        Assert.Null(MatchingCache.Read(temp.Path, Embedder.Name));
    }
}
