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
}
