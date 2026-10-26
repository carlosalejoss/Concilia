using System.Text.Json.Nodes;
using Concilia.Core.Catalog;
using Concilia.Core.Embeddings;
using Concilia.Core.Phases;
using Concilia.Core.Runner;

namespace Concilia.Core.Matching;

// What a round centred on some iterations reads entirely besides the new
// documentation (option C, decisions of the author of 26-10-2026): the
// earlier fragments related to it and the related requirements of the
// validated catalog, chosen by the platform with embeddings, without AI.
// - A fragment is related if it is "parecido" (0,70) or more to some new
//   fragment; its similarity is the highest one.
// - A requirement is related if it cites a related fragment, or if its text is
//   "parecido" or more to some new fragment (both, measured with clinics in
//   docs/fragmentos-relacionados.md: they complement each other).
// - Each list has a cap of 20.000 estimated tokens: the most similar first;
//   what does not fit can still be consulted by ranges.
public static class RelatedContext
{
    public const double Threshold = 0.70;
    public const int MaxTokens = 20_000;

    public static IReadOnlyList<Fragment> Fragments(
        ITextEmbedder embedder, IReadOnlyList<Fragment> fresh, IReadOnlyList<Fragment> previous, double threshold = Threshold, int maxTokens = MaxTokens)
    {
        if (fresh.Count == 0 || previous.Count == 0)
            return [];
        var freshVectors = embedder.Embed(fresh.Select(f => f.Text).ToList());
        var previousVectors = embedder.Embed(previous.Select(f => f.Text).ToList());
        var related = previous
            .Select((f, i) => (Fragment: f, Order: i, Similarity: freshVectors.Max(v => VectorMath.Cosine(previousVectors[i], v))))
            .Where(r => r.Similarity >= threshold)
            .OrderByDescending(r => r.Similarity)
            .ToList();
        var kept = WithinCap(related, r => TokensOf(r.Fragment), maxTokens);
        // In the order of the documentation, so the file reads like it.
        return kept.OrderBy(r => r.Order).Select(r => r.Fragment).ToList();
    }

    // The related requirements of a catalog (CatalogDraft.ForNextPhase), with
    // the same shape: {"requisitos": [...]}.
    public static string Requirements(
        ITextEmbedder embedder, string catalogJson, IReadOnlyList<Fragment> fresh, IReadOnlySet<string> relatedFragmentIds,
        double threshold = Threshold, int maxTokens = MaxTokens)
    {
        var requirements = (JsonNode.Parse(catalogJson)?["requisitos"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        if (requirements.Count == 0 || fresh.Count == 0)
            return new JsonObject { ["requisitos"] = new JsonArray() }.ToJsonString();
        var freshVectors = embedder.Embed(fresh.Select(f => f.Text).ToList());
        var vectors = embedder.Embed(requirements.Select(r => r["texto"]?.GetValue<string>() ?? "").ToList());
        var scored = requirements
            .Select((r, i) =>
            {
                var similarity = freshVectors.Max(v => VectorMath.Cosine(vectors[i], v));
                var bySources = (r["fuentes"] as JsonArray ?? []).OfType<JsonValue>()
                    .Any(s => s.TryGetValue<string>(out var id) && relatedFragmentIds.Contains(id));
                // One that cites a related fragment goes first, then by similarity.
                return (Requirement: r, Order: i, Similarity: bySources ? 1 + similarity : similarity, Related: bySources || similarity >= threshold);
            })
            .Where(r => r.Related)
            .OrderByDescending(r => r.Similarity)
            .ToList();
        var kept = WithinCap(scored, r => InputBudget.EstimateTokens(r.Requirement.ToJsonString()), maxTokens);
        return new JsonObject
        {
            ["requisitos"] = new JsonArray(kept.OrderBy(r => r.Order).Select(r => r.Requirement.DeepClone()).ToArray()),
        }.ToJsonString();
    }

    // The first ones while they fit in the cap.
    private static List<T> WithinCap<T>(IEnumerable<T> ordered, Func<T, int> tokens, int maxTokens)
    {
        var kept = new List<T>();
        var total = 0;
        foreach (var item in ordered)
        {
            var size = tokens(item);
            if (total + size > maxTokens)
                break;
            kept.Add(item);
            total += size;
        }
        return kept;
    }

    // The estimated tokens of a fragment as the model reads it: its lines in
    // the documentation file, without the heading of its document.
    private static int TokensOf(Fragment f)
    {
        var built = DocumentationFile.Build([f], new Dictionary<string, string>());
        return InputBudget.EstimateTokens(built[(built.IndexOf('\n') + 1)..]);
    }
}
