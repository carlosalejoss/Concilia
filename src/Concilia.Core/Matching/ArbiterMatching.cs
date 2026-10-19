using Concilia.Core.Embeddings;
using Concilia.Core.Phases;

namespace Concilia.Core.Matching;

// The matching that the platform computes for the arbiter of a job (step 88),
// with the embedder and the thresholds of the configuration.
public sealed class ArbiterMatching(ITextEmbedder embedder, MatchThresholds thresholds)
{
    public MatchThresholds Thresholds => thresholds;

    public MatchingResult Compute(PhaseKind kind, string proposalA, string proposalB) =>
        ProposalMatcher.Match(ProposalMatcher.ElementsOf(kind, proposalA), ProposalMatcher.ElementsOf(kind, proposalB), embedder, thresholds);
}
