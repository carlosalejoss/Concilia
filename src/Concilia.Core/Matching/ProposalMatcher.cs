using System.Text.Json.Nodes;
using Concilia.Core.Embeddings;
using Concilia.Core.Phases;
using Concilia.Core.Reports;

namespace Concilia.Core.Matching;

// Step 87 (milestone 17): which elements of A and of B say the same, decided
// by the platform with embeddings and without any LLM, to compare it with what
// the arbiter did.
//
// Every element of A is compared with every element of B (cosine similarity
// of their embeddings). Each element is paired with the most similar one of
// the other proposal, and two thresholds (author, 19-10-2026) say how close
// they are:
//   similarity >= Same               → "lo mismo": the element is in both proposals
//   Similar <= similarity < Same     → "parecido": close, but not the same
//   similarity < Similar             → "distinto": the element is only in its proposal
// As the arbiter does, several elements may pair with the same one.

// The two thresholds. Same must be above Similar.
public record MatchThresholds(double Same, double Similar)
{
    public MatchLevel LevelOf(double similarity) =>
        similarity >= Same ? MatchLevel.Same : similarity >= Similar ? MatchLevel.Similar : MatchLevel.Different;
}

public enum MatchLevel { Same, Similar, Different }

// One element of a proposal: its id and the text that is compared.
public record MatchElement(string Id, string Text);

// An element with its most similar element in the other proposal (null if the
// other proposal is empty). ArbiterJoined: whether the arbiter put it in the
// same element of its output as some element of the other proposal (null if
// the job has no arbiter output).
public record ElementMatch(string Slot, string Id, string Text, string? BestId, string? BestText,
    double Similarity, MatchLevel Level, bool? ArbiterJoined);

// A pair of A and B that is the best match of at least one of the two, with
// whether the arbiter joined exactly these two.
public record MatchPair(string AId, string BId, string AText, string BText, double Similarity, MatchLevel Level, bool? ArbiterJoined);

// How the embeddings and the arbiter agree on each element: "is it also in
// the other proposal?". Yes for the embeddings when its best match is "lo
// mismo" or "parecido"; yes for the arbiter when it joined it with an element
// of the other. "Parecido" counts as yes because the arbiter joins those too,
// keeping the detail of each one (calibration of 19-10-2026).
public record Agreement(int BothYes, int OnlyEmbeddings, int OnlyArbiter, int BothNo)
{
    public int Total => BothYes + OnlyEmbeddings + OnlyArbiter + BothNo;
    public double Rate => Total == 0 ? 0 : (double)(BothYes + BothNo) / Total;
}

public record MatchCounts(int A, int B, int SameA, int SameB, int SimilarA, int SimilarB, int OnlyA, int OnlyB);

// The slow part of the matching (step 100): each element with its most
// similar element in the other proposal (null if the other is empty) and their
// similarity, A first and then B, in the order of each proposal. Embedder:
// the model that computed it (ITextEmbedder.Name).
public record ElementSimilarity(string Slot, string Id, string Text, string? BestId, string? BestText, double Similarity);

public record ProposalSimilarities(string Embedder, IReadOnlyList<ElementSimilarity> Elements);

public record MatchingResult(MatchThresholds Thresholds, MatchCounts Counts, Agreement? Agreement,
    IReadOnlyList<ElementMatch> Elements, IReadOnlyList<MatchPair> Pairs);

public static class ProposalMatcher
{
    // The elements of a proposal and the text compared for each one:
    //   question:    the question
    //   requirement: its text
    //   story:       its title and its "como… quiero… para…"
    public static IReadOnlyList<MatchElement> ElementsOf(PhaseKind kind, string proposal) =>
        JobReports.Elements(kind, JsonNode.Parse(proposal))
            .Select(e => new MatchElement(Text(e, "id"), TextOf(kind, e)))
            .ToList();

    public static string TextOf(PhaseKind kind, JsonObject element) => kind switch
    {
        PhaseKind.Questions => Text(element, "pregunta"),
        PhaseKind.Catalog => Text(element, "texto"),
        PhaseKind.Backlog => $"{Text(element, "titulo")}. Como {Text(element, "como")}, quiero {Text(element, "quiero")}, para {Text(element, "para")}.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    // The pairs (A id, B id) the arbiter joined: every A and B element cited
    // in the `de` of the same element of its output. ProposalOrder says which
    // of "propuesta_1" and "propuesta_2" is A.
    public static HashSet<(string A, string B)> ArbiterPairs(PhaseKind kind, string arbiterOutput, IReadOnlyList<string> proposalOrder)
    {
        var pairs = new HashSet<(string, string)>();
        foreach (var element in JobReports.Elements(kind, JsonNode.Parse(arbiterOutput)))
        {
            if (element["de"] is not JsonObject de)
                continue;
            List<string> Ids(string slot)
            {
                var n = proposalOrder.ToList().IndexOf(slot) + 1;
                return (de[$"propuesta_{n}"] as JsonArray ?? []).OfType<JsonValue>()
                    .Select(v => v.TryGetValue<string>(out var id) ? id : null).OfType<string>().ToList();
            }
            foreach (var a in Ids("A"))
                foreach (var b in Ids("B"))
                    pairs.Add((a, b));
        }
        return pairs;
    }

    public static MatchingResult Match(IReadOnlyList<MatchElement> a, IReadOnlyList<MatchElement> b,
        ITextEmbedder embedder, MatchThresholds thresholds, HashSet<(string A, string B)>? arbiterPairs = null) =>
        Classify(Similarities(a, b, embedder), thresholds, arbiterPairs);

    // The slow part of the matching, the only one that needs the embeddings
    // (step 100): each element of A with its most similar element of B, and
    // the other way round. It does not depend on the thresholds nor on the
    // arbiter, so it is computed once per job and saved (MatchingCache).
    public static ProposalSimilarities Similarities(IReadOnlyList<MatchElement> a, IReadOnlyList<MatchElement> b, ITextEmbedder embedder)
    {
        // One call for all the texts: the embedder works in batches.
        var vectors = embedder.Embed(a.Concat(b).Select(e => e.Text).ToList());
        var similarity = new double[a.Count, b.Count];
        for (var i = 0; i < a.Count; i++)
            for (var j = 0; j < b.Count; j++)
                similarity[i, j] = VectorMath.Cosine(vectors[i], vectors[a.Count + j]);

        var elements = new List<ElementSimilarity>();
        for (var i = 0; i < a.Count; i++)
        {
            var j = Best(b.Count, k => similarity[i, k]);
            elements.Add(j < 0
                ? new ElementSimilarity("A", a[i].Id, a[i].Text, null, null, 0)
                : new ElementSimilarity("A", a[i].Id, a[i].Text, b[j].Id, b[j].Text, similarity[i, j]));
        }
        for (var j = 0; j < b.Count; j++)
        {
            var i = Best(a.Count, k => similarity[k, j]);
            elements.Add(i < 0
                ? new ElementSimilarity("B", b[j].Id, b[j].Text, null, null, 0)
                : new ElementSimilarity("B", b[j].Id, b[j].Text, a[i].Id, a[i].Text, similarity[i, j]));
        }
        return new ProposalSimilarities(embedder.Name, elements);
    }

    // The fast part: with the thresholds, how close each element is to its
    // best match, and with the arbiter's pairs, whether the arbiter joined it.
    public static MatchingResult Classify(ProposalSimilarities similarities, MatchThresholds thresholds, HashSet<(string A, string B)>? arbiterPairs = null)
    {
        var joinedA = arbiterPairs?.Select(p => p.A).ToHashSet();
        var joinedB = arbiterPairs?.Select(p => p.B).ToHashSet();

        var elements = similarities.Elements.Select(e => new ElementMatch(e.Slot, e.Id, e.Text, e.BestId, e.BestText, e.Similarity,
            e.BestId is null ? MatchLevel.Different : thresholds.LevelOf(e.Similarity),
            (e.Slot == "A" ? joinedA : joinedB)?.Contains(e.Id))).ToList();

        // The pairs: the best match of each element, once each (as A, B).
        var pairs = new Dictionary<(string, string), MatchPair>();
        foreach (var e in similarities.Elements.Where(e => e.BestId is not null))
        {
            var (aId, aText, bId, bText) = e.Slot == "A" ? (e.Id, e.Text, e.BestId!, e.BestText!) : (e.BestId!, e.BestText!, e.Id, e.Text);
            pairs.TryAdd((aId, bId), new MatchPair(aId, bId, aText, bText, e.Similarity, thresholds.LevelOf(e.Similarity),
                arbiterPairs?.Contains((aId, bId))));
        }

        int Count(string slot, MatchLevel level) => elements.Count(e => e.Slot == slot && e.Level == level);
        var counts = new MatchCounts(elements.Count(e => e.Slot == "A"), elements.Count(e => e.Slot == "B"),
            Count("A", MatchLevel.Same), Count("B", MatchLevel.Same),
            Count("A", MatchLevel.Similar), Count("B", MatchLevel.Similar),
            Count("A", MatchLevel.Different), Count("B", MatchLevel.Different));

        Agreement? agreement = null;
        if (arbiterPairs is not null)
        {
            bool Embeddings(ElementMatch e) => e.Level != MatchLevel.Different;
            agreement = new Agreement(
                elements.Count(e => Embeddings(e) && e.ArbiterJoined == true),
                elements.Count(e => Embeddings(e) && e.ArbiterJoined == false),
                elements.Count(e => !Embeddings(e) && e.ArbiterJoined == true),
                elements.Count(e => !Embeddings(e) && e.ArbiterJoined == false));
        }

        return new MatchingResult(thresholds, counts, agreement, elements,
            pairs.Values.OrderByDescending(p => p.Similarity).ToList());
    }

    // The index with the highest similarity among count candidates, or -1 if there are none.
    private static int Best(int count, Func<int, double> similarityOf)
    {
        var best = -1;
        for (var k = 0; k < count; k++)
            if (best < 0 || similarityOf(k) > similarityOf(best))
                best = k;
        return best;
    }

    private static string Text(JsonObject element, string field) =>
        element[field] is JsonValue v && v.TryGetValue<string>(out var text) ? text : "";
}
