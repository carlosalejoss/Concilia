using Concilia.Core.Deliverables;
using Concilia.Core.Embeddings;
using Concilia.Core.Phases;

namespace Concilia.Core.Matching;

// Step 115 (milestone 22): a catalog or backlog generated from scratch against
// the version the deliverable had just before, so the user sees what stayed,
// what changed, what is new and what disappeared without reading it all again.
//
// The same matching as A and B (step 87): each element of the new version
// with its most similar element of the previous one, and the other way round,
// with the same two thresholds:
//   similarity >= Same               → Same:    it says the same as before
//   Similar <= similarity < Same     → Changed: close to one of before
//   below Similar (or nothing before) → New
// and an element of before whose most similar new element is below Similar
// has disappeared (Gone). Ids are not compared: a generation from scratch
// numbers from 1 again, so RF-012 may now be RF-015. As between A and B,
// several elements may pair with the same one (a requirement split in two is
// two Changed, not one Gone), so the figures need not add up one to one.
public enum VersionMark { Same, Changed, New }

// An element of the new version with its mark and its most similar element of
// the previous one (null if the previous one is empty).
public record MarkedElement(string Id, string Text, VersionMark Mark, string? PreviousId, string? PreviousText, double Similarity);

// An element of the previous version that is not in the new one, with its most
// similar new element (null if the new one is empty).
public record GoneElement(string Id, string Text, string? BestId, string? BestText, double Similarity);

public record PreviousCounts(int Version, DeliverableStatus Status, int Same, int Changed, int New, int Gone);

// Version and Status: the previous version compared, and how it was then.
public record PreviousVersionComparison(int Version, DeliverableStatus Status, MatchThresholds Thresholds,
    IReadOnlyList<MarkedElement> Elements, IReadOnlyList<GoneElement> Gone)
{
    public PreviousCounts Counts => new(Version, Status,
        Elements.Count(e => e.Mark == VersionMark.Same), Elements.Count(e => e.Mark == VersionMark.Changed),
        Elements.Count(e => e.Mark == VersionMark.New), Gone.Count);
}

public static class PreviousVersion
{
    // In the folder of the job, next to emparejamiento.json (MatchingCache).
    public const string FileName = "frente-anterior.json";

    // The slow part, with the embeddings: the elements of the new version
    // (draft content of the catalog or the backlog) as "A" and those of the
    // previous one as "B" of ProposalMatcher.Similarities.
    public static ProposalSimilarities Similarities(PhaseKind kind, string current, string previous, ITextEmbedder embedder) =>
        ProposalMatcher.Similarities(ProposalMatcher.ElementsOf(kind, current), ProposalMatcher.ElementsOf(kind, previous), embedder);

    // The fast part: the marks, with the thresholds.
    public static PreviousVersionComparison Classify(int version, DeliverableStatus status, ProposalSimilarities similarities, MatchThresholds thresholds)
    {
        var elements = similarities.Elements.Where(e => e.Slot == "A")
            .Select(e => new MarkedElement(e.Id, e.Text, MarkOf(e, thresholds), e.BestId, e.BestText, e.Similarity))
            .ToList();
        var gone = similarities.Elements
            .Where(e => e.Slot == "B" && (e.BestId is null || thresholds.LevelOf(e.Similarity) == MatchLevel.Different))
            .Select(e => new GoneElement(e.Id, e.Text, e.BestId, e.BestText, e.Similarity))
            .ToList();
        return new PreviousVersionComparison(version, status, thresholds, elements, gone);
    }

    private static VersionMark MarkOf(ElementSimilarity e, MatchThresholds thresholds) =>
        e.BestId is null ? VersionMark.New : thresholds.LevelOf(e.Similarity) switch
        {
            MatchLevel.Same => VersionMark.Same,
            MatchLevel.Similar => VersionMark.Changed,
            _ => VersionMark.New,
        };
}
