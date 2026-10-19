using System.Globalization;
using System.Text.Json.Nodes;
using Concilia.Core.Phases;
using Concilia.Core.Reports;

namespace Concilia.Core.Matching;

// Step 88 (decisions of the author, 19-10-2026): the matching is a guide for
// the arbiter, not a rule. After the arbiter, the platform looks for where it
// went against the numbers, and in the catalog and the backlog marks those
// elements as a discrepancy, next to the ones the arbiter marks itself (the
// numbers measure how close two texts are, not whether they contradict each
// other: "1 hora" and "24 horas" are close, and only the LLM sees that). In
// the questions they are only counted in the report.
//
// Two kinds of deviation:
//   joined-different  the arbiter put in one element something that has
//                     nothing "mismo" or "parecido" in the other proposal
//   split-same        two elements the numbers see as "lo mismo" ended in two
//                     different elements of the arbiter (if it discarded one
//                     of them, it is not a deviation: it kept one of the two)
public enum DeviationKind { JoinedDifferent, SplitSame }

// ElementId: the id of the element in the arbiter's output (before the
// platform renumbers the draft). Explanation: for the user, in Spanish.
public record ArbiterDeviation(string ElementId, DeviationKind Kind, double Similarity, string Explanation);

public static class ArbiterDeviations
{
    public static IReadOnlyList<ArbiterDeviation> Find(
        PhaseKind kind, string arbiterOutput, MatchingResult matching, IReadOnlyList<string> proposalOrder)
    {
        // Where each element of A and of B ended: the id of the arbiter's element whose `de` cites it.
        var endedIn = new Dictionary<(string Slot, string Id), string>();
        foreach (var element in JobReports.Elements(kind, JsonNode.Parse(arbiterOutput)))
        {
            if (element["de"] is not JsonObject de || element["id"] is not JsonValue v || !v.TryGetValue<string>(out var elementId))
                continue;
            for (var n = 1; n <= 2; n++)
                foreach (var id in (de[$"propuesta_{n}"] as JsonArray ?? []).OfType<JsonValue>()
                             .Select(x => x.TryGetValue<string>(out var s) ? s : null).OfType<string>())
                    endedIn.TryAdd((proposalOrder[n - 1], id), elementId);
        }
        var joined = ProposalMatcher.ArbiterPairs(kind, arbiterOutput, proposalOrder);
        bool Joined(ElementMatch e) => e.Slot == "A" ? joined.Any(p => p.A == e.Id) : joined.Any(p => p.B == e.Id);

        var deviations = new List<ArbiterDeviation>();

        foreach (var e in matching.Elements.Where(e => e.Level == MatchLevel.Different && Joined(e)))
            if (endedIn.TryGetValue((e.Slot, e.Id), out var target))
                deviations.Add(new ArbiterDeviation(target, DeviationKind.JoinedDifferent, e.Similarity,
                    $"La plataforma ve distintos los elementos que el árbitro unió aquí: lo más parecido que encuentra en la otra propuesta llega a {Number(e.Similarity)}, " +
                    $"por debajo de «parecido» ({Number(matching.Thresholds.Similar)}). Comprueba que no se han mezclado dos cosas."));

        foreach (var p in matching.Pairs.Where(p => p.Level == MatchLevel.Same && !joined.Contains((p.AId, p.BId))))
        {
            if (!endedIn.TryGetValue(("A", p.AId), out var inA) || !endedIn.TryGetValue(("B", p.BId), out var inB) || inA == inB)
                continue;
            deviations.Add(new ArbiterDeviation(inA, DeviationKind.SplitSame, p.Similarity, SplitExplanation(p.Similarity, p.BText)));
            deviations.Add(new ArbiterDeviation(inB, DeviationKind.SplitSame, p.Similarity, SplitExplanation(p.Similarity, p.AText)));
        }

        // One deviation of each kind per element is enough to mark it.
        return deviations.DistinctBy(d => (d.ElementId, d.Kind, d.Explanation)).ToList();
    }

    // The arbiter's elements with some deviation.
    public static int ElementCount(IReadOnlyList<ArbiterDeviation> deviations) => deviations.Select(d => d.ElementId).Distinct().Count();

    // The explanation of an element: the arbiter's own one, followed by those of the platform.
    public static string Explain(string arbiterExplanation, IEnumerable<ArbiterDeviation> deviations) =>
        string.Join(" ", new[] { arbiterExplanation }.Concat(deviations.Select(d => $"[Plataforma] {d.Explanation}")).Where(s => s.Length > 0));

    private static string SplitExplanation(double similarity, string otherText) =>
        $"La plataforma ve este elemento como lo mismo (similitud {Number(similarity)}) que «{Excerpt(otherText)}», " +
        "que el árbitro dejó en otro elemento. Comprueba que no está repetido.";

    private static string Excerpt(string text) => text.Length <= 120 ? text : text[..117].TrimEnd() + "…";

    private static string Number(double value) => value.ToString("F2", CultureInfo.GetCultureInfo("es-ES"));
}
