using Concilia.Core.Embeddings;
using Concilia.Core.Phases;

namespace Concilia.Core.Matching;

// The matching that the platform computes for the arbiter of a job (step 88),
// with the embedder and the thresholds of the configuration. Step 100: in two
// parts, so the job can save the slow one (MatchingCache).
public sealed class ArbiterMatching(ITextEmbedder embedder, MatchThresholds thresholds)
{
    public MatchThresholds Thresholds => thresholds;

    public ProposalSimilarities Similarities(PhaseKind kind, string proposalA, string proposalB) =>
        ProposalMatcher.Similarities(ProposalMatcher.ElementsOf(kind, proposalA), ProposalMatcher.ElementsOf(kind, proposalB), embedder);

    public MatchingResult Classify(ProposalSimilarities similarities) => ProposalMatcher.Classify(similarities, thresholds);

    public MatchingResult Compute(PhaseKind kind, string proposalA, string proposalB) =>
        Classify(Similarities(kind, proposalA, proposalB));

    // Step 112 (a round centred on some iterations): the new questions of the
    // two proposals (in the order of the plan, as "Propuesta 1" and
    // "Propuesta 2") that are "parecido" or more to a question already asked,
    // with the most similar one: the lines "previa" of the matching file.
    public IReadOnlyList<PreviousMatch> AgainstPrevious(string proposal1, string proposal2, IReadOnlyList<FocusQuestion> previous) =>
        AgainstExisting(PhaseKind.Questions, proposal1, proposal2, previous.Select(q => (q.Code, q.Text)).ToList());

    // Step 113 (an update of the catalog): the new requirements of the two
    // proposals like a requirement already in the catalog (the lines
    // "existente"). The same for the questions of a centred round.
    public IReadOnlyList<PreviousMatch> AgainstExisting(PhaseKind kind, string proposal1, string proposal2, IReadOnlyList<(string Id, string Text)> previous)
    {
        if (previous.Count == 0)
            return [];
        var previousVectors = embedder.Embed(previous.Select(q => q.Text).ToList());
        var matches = new List<PreviousMatch>();
        foreach (var (proposal, number) in new[] { (proposal1, 1), (proposal2, 2) })
        {
            var elements = ProposalMatcher.ElementsOf(kind, proposal);
            if (elements.Count == 0)
                continue;
            var vectors = embedder.Embed(elements.Select(e => e.Text).ToList());
            for (var i = 0; i < elements.Count; i++)
            {
                var best = previous.Select((q, j) => (q.Id, Similarity: VectorMath.Cosine(vectors[i], previousVectors[j])))
                    .MaxBy(m => m.Similarity);
                if (thresholds.LevelOf(best.Similarity) != MatchLevel.Different)
                    matches.Add(new PreviousMatch(number, elements[i].Id, best.Id, thresholds.LevelOf(best.Similarity), best.Similarity));
            }
        }
        return matches;
    }
}

// A new question of a centred round like one already asked: proposal 1 or 2,
// its id, the code of the earlier question and how similar they are.
public record PreviousMatch(int Proposal, string Id, string PreviousCode, MatchLevel Level, double Similarity);
