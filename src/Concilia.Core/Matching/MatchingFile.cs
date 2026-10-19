using System.Globalization;
using System.Text;

namespace Concilia.Core.Matching;

// input/emparejamiento.txt (step 88, prompts v0.10): the matching of the two
// proposals that the platform computes with embeddings before calling the
// arbiter, which uses it as a guide (decision of the author, 19-10-2026: the
// arbiter may depart from it if the source justifies it, and the platform
// shows where it did). The ids carry the number of their proposal, as in the
// proposal files ("1:RF-003"), so A and B stay anonymous. One line per pair or
// per element without a pair:
//   1:RF-003 2:RF-001 mismo 0.94
//   1:RF-010 2:RF-007 parecido 0.78
//   1:RF-020 sin-par
public static class MatchingFile
{
    public const string Path = "input/emparejamiento.txt";

    public static string Write(MatchingResult matching, IReadOnlyList<string> proposalOrder)
    {
        int Number(string slot) => proposalOrder.ToList().IndexOf(slot) + 1;
        string Id(string slot, string id) => $"{Number(slot)}:{id}";

        var lines = new List<string>();
        // The pairs "mismo" and "parecido", in the order of proposal 1 (each
        // pair written with proposal 1 first).
        var pairs = matching.Pairs
            .Where(p => p.Level != MatchLevel.Different)
            .Select(p => Number("A") == 1
                ? (First: Id("A", p.AId), Second: Id("B", p.BId), p.Level, p.Similarity)
                : (First: Id("B", p.BId), Second: Id("A", p.AId), p.Level, p.Similarity))
            .OrderBy(p => p.First, StringComparer.Ordinal).ThenBy(p => p.Second, StringComparer.Ordinal);
        foreach (var p in pairs)
            lines.Add($"{p.First} {p.Second} {Name(p.Level)} {p.Similarity.ToString("F2", CultureInfo.InvariantCulture)}");

        // The elements that have nothing "mismo" or "parecido" in the other proposal.
        foreach (var e in matching.Elements.Where(e => e.Level == MatchLevel.Different)
                     .Select(e => Id(e.Slot, e.Id)).OrderBy(id => id, StringComparer.Ordinal))
            lines.Add($"{e} sin-par");

        var text = new StringBuilder();
        foreach (var line in lines)
            text.Append(line).Append('\n');
        return text.ToString();
    }

    private static string Name(MatchLevel level) => level == MatchLevel.Same ? "mismo" : "parecido";
}
