using System.ComponentModel;
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
public record ArbiterDeviation(string ElementId, DeviationKind Kind, double Similarity, string Explanation)
{
    // Split-same (step 126): the arbiter's element where the other one of the
    // pair ended, and the text of that other one, so the explanation can say
    // where to find it with the id it has in the draft.
    [Description("Solo si separa iguales: el id, en la salida del árbitro, del elemento donde acabó el otro del par.")]
    public string? OtherElementId { get; init; }
    [Description("Solo si separa iguales: el texto del otro elemento del par, en su propuesta.")]
    public string? OtherText { get; init; }
}

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
            deviations.Add(Split(inA, inB, p.Similarity, p.BText));
            deviations.Add(Split(inB, inA, p.Similarity, p.AText));
        }

        // One deviation of each kind per element is enough to mark it.
        return deviations.DistinctBy(d => (d.ElementId, d.Kind, d.Explanation)).ToList();
    }

    // The arbiter's elements with some deviation.
    public static int ElementCount(IReadOnlyList<ArbiterDeviation> deviations) => deviations.Select(d => d.ElementId).Distinct().Count();

    // The explanation of an element: the arbiter's own one, followed by those of the platform.
    // finalId (step 126): the id an element of the arbiter's output has in the
    // draft, so a split-same says where the other one is; null if it is not there.
    public static string Explain(string arbiterExplanation, IEnumerable<ArbiterDeviation> deviations, Func<string, string?>? finalId = null) =>
        string.Join(" ", new[] { arbiterExplanation }.Concat(deviations.Select(d => $"[Plataforma] {Text(d, finalId)}")).Where(s => s.Length > 0));

    // The id each element of the arbiter's output keeps in the draft, by
    // position: before and after are the same output, before and after the
    // platform gives the ids of the draft.
    public static Func<string, string?> FinalIds(PhaseKind kind, string before, string after)
    {
        static IEnumerable<string?> Ids(PhaseKind kind, string json) =>
            JobReports.Elements(kind, JsonNode.Parse(json)).Select(e => e["id"] is JsonValue v && v.TryGetValue<string>(out var id) ? id : null);
        var map = new Dictionary<string, string>();
        foreach (var (old, @new) in Ids(kind, before).Zip(Ids(kind, after)))
            if (old is not null && @new is not null)
                map.TryAdd(old, @new);
        return id => map.GetValueOrDefault(id);
    }

    private static string Text(ArbiterDeviation d, Func<string, string?>? finalId) =>
        d is { Kind: DeviationKind.SplitSame, OtherElementId: { } other, OtherText: { } otherText } && finalId is not null
            ? SplitExplanation(d.Similarity, otherText, finalId(other))
            : d.Explanation;

    // The explanation of the deviation as the arbiter's output has it: it
    // cites the other element with its id there.
    private static ArbiterDeviation Split(string elementId, string otherElementId, double similarity, string otherText) =>
        new(elementId, DeviationKind.SplitSame, similarity, SplitExplanation(similarity, otherText, otherElementId))
        {
            OtherElementId = otherElementId,
            OtherText = otherText,
        };

    // where: the id of the element the arbiter left the other one in (step
    // 126: its text alone is not enough if the arbiter rewrote it or joined
    // it with others); null if that element is not in the draft.
    private static string SplitExplanation(double similarity, string otherText, string? where) =>
        $"La plataforma ve este elemento como lo mismo (similitud {Number(similarity)}) que «{Excerpt(otherText)}», " +
        $"que el árbitro dejó {(where is null ? "en otro elemento" : $"en {where}")}. Comprueba que no está repetido.";

    private static string Excerpt(string text) => text.Length <= 120 ? text : text[..117].TrimEnd() + "…";

    private static string Number(double value) => value.ToString("F2", CultureInfo.GetCultureInfo("es-ES"));
}
