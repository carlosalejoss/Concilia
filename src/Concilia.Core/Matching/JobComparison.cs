using System.Text.Json.Nodes;
using Concilia.Core.Phases;
using Concilia.Core.Reports;

namespace Concilia.Core.Matching;

// An element of A or of B in a row of the comparison, with what the
// embeddings say of it: its most similar element in the other proposal, how
// close it is (Level) and whether the arbiter joined it with an element of
// the other proposal. AgreesWithEmbeddings: both say the same on "is it also
// in the other proposal?" (the Agreement of ProposalMatcher, element by element).
public record ComparedElement(
    string Id, string Text, string? BestId, double Similarity, MatchLevel Level, bool ArbiterJoined)
{
    public bool AgreesWithEmbeddings => (Level != MatchLevel.Different) == ArbiterJoined;
}

// What the arbiter did with some elements of A and B.
public enum ComparisonRowKind
{
    Merged,       // an element of its output, from A, B or both (its `de`)
    Discarded,    // an element it discarded, with its reason
    Unclassified, // an element of A or B that is in no `de` and was not discarded
}

// A row of the comparison side by side: the elements of A on the left, what
// the arbiter did in the middle and the elements of B on the right.
// ArbiterId and ArbiterText: the element of the arbiter's output (ids as the
// arbiter wrote them, before the platform renumbers the draft). Origin: "A",
// "B" or "ambas" (merged rows). Discrepancy: marked by the arbiter.
// Deviations: where it went against the matching (step 88), explained.
// Reason: why it discarded it.
public record ComparisonRow(
    ComparisonRowKind Kind, string? ArbiterId, string? ArbiterText, string? Origin,
    IReadOnlyList<ComparedElement> A, IReadOnlyList<ComparedElement> B,
    bool Discrepancy, string? Explanation, IReadOnlyList<string> Deviations, string? Reason)
{
    public bool AgreesWithEmbeddings => A.Concat(B).All(e => e.AgreesWithEmbeddings);
}

// The comparison of a job side by side (step 100): what A and B proposed and
// what the arbiter did with each element, next to the matching by meaning.
// Counts and Agreement: those of the matching, with the thresholds used.
public record JobComparison(
    string Phase, MatchThresholds Thresholds, MatchCounts Counts, Agreement? Agreement, IReadOnlyList<ComparisonRow> Rows)
{
    // matching: classified with the pairs of this arbiter output
    // (ProposalMatcher.Classify), so each element knows whether it was joined.
    public static JobComparison Build(
        PhaseKind kind, string arbiterOutput, IReadOnlyList<string> proposalOrder,
        MatchingResult matching, IReadOnlyList<ArbiterDeviation> deviations)
    {
        var root = JsonNode.Parse(arbiterOutput);
        var bySlot = matching.Elements.ToLookup(e => e.Slot);
        var elements = new Dictionary<(string Slot, string Id), ElementMatch>();
        foreach (var e in matching.Elements)
            elements.TryAdd((e.Slot, e.Id), e);

        // "propuesta_1" or the number 1 → the slot ("A" or "B") of the plan.
        string SlotOf(int number) => proposalOrder[number - 1];
        ComparedElement Compared(string slot, string id) => elements.TryGetValue((slot, id), out var e)
            ? new ComparedElement(id, e.Text, e.BestId, e.Similarity, e.Level, e.ArbiterJoined == true)
            // An id that is not in the proposal (the validator does not let it through).
            : new ComparedElement(id, "", null, 0, MatchLevel.Different, false);

        var rows = new List<ComparisonRow>();
        var placed = new HashSet<(string Slot, string Id)>();

        foreach (var element in JobReports.Elements(kind, root))
        {
            var id = Text(element, "id");
            var de = element["de"] as JsonObject;
            var ids = new Dictionary<string, List<string>> { ["A"] = [], ["B"] = [] };
            for (var n = 1; n <= 2; n++)
                ids[SlotOf(n)].AddRange(Ids(de?[$"propuesta_{n}"]));
            foreach (var slot in ids.Keys)
                foreach (var cited in ids[slot])
                    placed.Add((slot, cited));
            var origin = ids["A"].Count > 0 && ids["B"].Count > 0 ? "ambas" : ids["A"].Count > 0 ? "A" : ids["B"].Count > 0 ? "B" : null;
            rows.Add(new ComparisonRow(ComparisonRowKind.Merged, id, ProposalMatcher.TextOf(kind, element), origin,
                ids["A"].Select(x => Compared("A", x)).ToList(), ids["B"].Select(x => Compared("B", x)).ToList(),
                element["discrepancia"] is JsonValue d && d.TryGetValue<bool>(out var yes) && yes,
                Text(element, "explicacion"),
                deviations.Where(x => x.ElementId == id).Select(x => x.Explanation).Distinct().ToList(),
                null));
        }

        // The discarded ones: "descartados" in the catalog, "descartadas" in the questions and the backlog.
        foreach (var discarded in Items(root?["descartados"]).Concat(Items(root?["descartadas"])))
        {
            if (discarded["propuesta"] is not JsonValue p || !p.TryGetValue<int>(out var number) || number is < 1 or > 2)
                continue;
            var slot = SlotOf(number);
            var id = Text(discarded, "id");
            placed.Add((slot, id));
            var compared = new List<ComparedElement> { Compared(slot, id) };
            rows.Add(new ComparisonRow(ComparisonRowKind.Discarded, null, null, null,
                slot == "A" ? compared : [], slot == "B" ? compared : [], false, null, [], Text(discarded, "motivo")));
        }

        // What the arbiter left out without saying it (ARB-FALTA, if the platform did not fix it).
        foreach (var slot in new[] { "A", "B" })
            foreach (var e in bySlot[slot].Where(e => !placed.Contains((slot, e.Id))))
            {
                var compared = new List<ComparedElement> { Compared(slot, e.Id) };
                rows.Add(new ComparisonRow(ComparisonRowKind.Unclassified, null, null, null,
                    slot == "A" ? compared : [], slot == "B" ? compared : [], false, null, [], null));
            }

        return new JobComparison(kind.ToString(), matching.Thresholds, matching.Counts, matching.Agreement, rows);
    }

    private static List<string> Ids(JsonNode? list) =>
        (list as JsonArray ?? []).OfType<JsonValue>().Select(v => v.TryGetValue<string>(out var s) ? s : null).OfType<string>().ToList();

    private static IEnumerable<JsonObject> Items(JsonNode? list) => list as JsonArray is { } items ? items.OfType<JsonObject>() : [];

    private static string Text(JsonObject o, string field) =>
        o[field] is JsonValue v && v.TryGetValue<string>(out var s) ? s : "";
}
