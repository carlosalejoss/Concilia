using System.Text.Json.Nodes;
using Concilia.Core.Matching;
using Concilia.Core.Phases;
using Concilia.Core.Runs;

namespace Concilia.Core.Reports;

// The report of a job (step 64), the base of the evaluation: what each run
// cost, what each model proposed, what the arbiter kept from each one, and
// what the user changed afterwards. Built by the platform from what the job
// left: its runs, the outputs of A and B, the arbiter's frozen output and the
// current deliverable.

// One run of the job. Seconds: from its start to its end (for a repair, the
// whole run, since the attempts share the start). Estimated tokens (step
// 95): only for a run without usage, from its saved console (UsageEstimator).
public record RunLine(string Slot, int? Lot, int Attempt, string Model, string State,
    long? InputTokens, long? OutputTokens, int? Turns, double? Seconds, string? Error,
    long? EstimatedInputTokens = null, long? EstimatedOutputTokens = null);

// What each model proposed and what the arbiter made of it: A and B (null if
// that model did not end well), the elements of the merge, by origin, those
// with a discrepancy marked by the arbiter and those it discarded. Deviations
// (step 88): the elements of the merge where the arbiter went against the
// matching of the platform; null in the jobs from before it.
public record ElementCounts(int? A, int? B, int Merged, int Both, int OnlyA, int OnlyB, int Discrepancies, int Discarded, int? Deviations = null);

// Step 100: what the embeddings say of the same proposals, next to the
// arbiter's figures (ProposalMatcher, with the thresholds of the
// configuration): the elements of A and of B with nothing "mismo" or
// "parecido" in the other proposal, and the agreement with the arbiter on
// whether each element is also in the other (null if the arbiter did not end well).
public record EmbeddingCounts(int OnlyA, int OnlyB, Agreement? Agreement);

// The user's changes to the deliverable, against the draft the job left:
// kept as they were, modified, added and deleted.
public record UserChanges(int Original, int Current, int Kept, int Modified, int Added, int Deleted);

// The questions of a round: how they ended and who wrote each answer.
public record QuestionCounts(int Total, int Answered, int Discarded, int Pending, int ByUser, int ByAi, int ByAiEdited);

// RunsWithoutUsage: runs that started but have no token counts. Claude Code
// only reports the usage in its last line ("result"), and a run the platform
// stops (CompactingAfterOutput, TruncatedToolCall) or that runs out of time
// never writes it. Through the gateway the usage of each message comes as 0,
// so there is nothing else to add up: the tokens of the job are then a lower
// bound, and the report says so instead of giving a figure that looks complete.
// Phase: the phase of a v0 job ("Questions", "Catalog"…). ArbiterModel: the
// model of its arbiter. RearbitrationOf: for a new arbitration, the job whose
// proposals it arbitrated again (to compare arbiters on the same proposals).
// Step 95: EstimatedInputTokens and EstimatedOutputTokens, the estimate of the
// runs without usage (the tokens of the job are then InputTokens plus them);
// MatchingTime, what the matching of the arbiter cost the platform.
// AssumedRequirements (step 93, catalog): the requirements of the merge that
// cite a question answered with a guess of the AI taken as it was.
// Embeddings (step 100): null if the job has no valid proposals of A and B.
public record JobReport(
    Guid JobId, string Kind, DateTimeOffset CreatedAt, DateTimeOffset? FinishedAt, bool Succeeded, string? Message,
    long InputTokens, long OutputTokens, int RunsWithoutUsage, int Repairs, double? Seconds,
    IReadOnlyList<RunLine> Runs, ElementCounts? Elements, UserChanges? Changes, string? ChangesNote, QuestionCounts? Questions,
    string? Phase = null, string? ArbiterModel = null, Guid? RearbitrationOf = null,
    long EstimatedInputTokens = 0, long EstimatedOutputTokens = 0, ComputeTime? MatchingTime = null, int? AssumedRequirements = null,
    EmbeddingCounts? Embeddings = null);

public static class JobReports
{
    // The elements a phase is made of: the questions, the requirements or the
    // stories (of every epic). The same in the outputs of A and B, in the
    // arbiter's and in the drafts.
    public static IReadOnlyList<JsonObject> Elements(PhaseKind kind, JsonNode? root) => kind switch
    {
        PhaseKind.Questions or PhaseKind.Suggestions => Items(root?["preguntas"]),
        PhaseKind.Catalog => Items(root?["requisitos"]),
        PhaseKind.Backlog => Items(root?["epicas"]).SelectMany(e => Items(e["historias"])).ToList(),
        _ => [],
    };

    public static int? Count(PhaseKind kind, string? json) => json is null ? null : Elements(kind, JsonNode.Parse(json)).Count;

    // What the arbiter made of the two proposals: each element of its output
    // comes from A, B or both (its `de`, in the order of the plan), may have a
    // discrepancy, and some proposed elements are discarded with a reason.
    public static ElementCounts Merge(PhaseKind kind, string arbiterOutput, PhasePlan plan, int? a, int? b)
    {
        var root = JsonNode.Parse(arbiterOutput);
        var elements = Elements(kind, root);
        var origins = elements.Select(e => e["de"] is JsonObject de ? plan.OriginOf(de) : null).ToList();
        var discarded = Items(root?["descartados"]).Count + Items(root?["descartadas"]).Count;
        return new ElementCounts(a, b, elements.Count,
            origins.Count(o => o == "ambas"), origins.Count(o => o == "A"), origins.Count(o => o == "B"),
            elements.Count(e => e["discrepancia"] is JsonValue d && d.TryGetValue<bool>(out var yes) && yes),
            discarded);
    }

    // The fields of the platform in a draft, not the user's content.
    private static readonly HashSet<string> PlatformFields = ["id", "origen", "discrepancia", "explicacion", "revisada"];

    // The user's changes: the elements of the current deliverable against
    // those of the draft the job left. Ids do not help, since the platform
    // renumbers after each edit (deleting RF-002 turns RF-003 into RF-002).
    // So first the elements with the same content (without ids nor the
    // platform's fields) are paired: kept. Of the rest, those that keep their
    // id are modified; what is left in the current one was added, and what is
    // left in the draft, deleted.
    public static UserChanges Diff(PhaseKind kind, string original, string current)
    {
        var before = Elements(kind, JsonNode.Parse(original));
        var after = Elements(kind, JsonNode.Parse(current));

        var leftBefore = before.ToList();
        var leftAfter = new List<JsonObject>();
        foreach (var element in after)
        {
            var same = leftBefore.FindIndex(b => Content(b) == Content(element));
            if (same >= 0)
                leftBefore.RemoveAt(same);
            else
                leftAfter.Add(element);
        }
        var kept = after.Count - leftAfter.Count;

        var modified = 0;
        foreach (var element in leftAfter.ToList())
        {
            var same = leftBefore.FindIndex(b => Id(b) is { } id && id == Id(element));
            if (same < 0)
                continue;
            leftBefore.RemoveAt(same);
            leftAfter.Remove(element);
            modified++;
        }
        return new UserChanges(before.Count, after.Count, kept, modified, leftAfter.Count, leftBefore.Count);
    }

    // The content of an element as the user sees it: without ids (also those
    // of its criteria and tasks, renumbered across the backlog) nor the
    // platform's fields.
    private static string Content(JsonObject element) => Strip(element)!.ToJsonString();

    private static JsonNode? Strip(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj.Where(p => !PlatformFields.Contains(p.Key)).Select(p => KeyValuePair.Create(p.Key, Strip(p.Value)))),
        JsonArray array => new JsonArray(array.Select(Strip).ToArray()),
        _ => node?.DeepClone(),
    };

    private static string? Id(JsonObject element) => element["id"] is JsonValue v && v.TryGetValue<string>(out var id) ? id : null;

    private static List<JsonObject> Items(JsonNode? node) => (node as JsonArray ?? []).OfType<JsonObject>().ToList();
}
