using System.Text.Json.Nodes;
using static Concilia.Core.Catalog.CatalogJson;

namespace Concilia.Core.Catalog;

// The catalog of one model in a job (v2) and the fixes the merge made (FIX lines).
public record MergedCatalogV2(string Catalog, IReadOnlyList<string> Fixes);

// Joins the accepted catalogs of the lots of one model (catalog v2) with its
// inventory. It lives next to the v1.3 CatalogMerger until the platform runs
// the v2 (step 38). There is no reference in Python, as with the v1.3 merge.
// - Capabilities and discards come from the inventory, unchanged (CAP- ids are
//   fixed since phase 1). Each capability is in a single lot, so nothing is
//   deduplicated between lots.
// - Requirements are renumbered consecutively, each type on its own (RF-, RNF-,
//   RA-, RT-, RI-), and their references rewritten.
// - Questions: the inventory's (QI-) and the lots' (Q-) are one list. Those of
//   the same type with the same text after normalizing are merged (FIX), a
//   safety net: in the v1.4 run, about 15 questions of one model were identical
//   but for the requirement. Then they are ordered by type (contradiction,
//   scope, missing_value, ambiguity, missing_actor; arrival order within a
//   type), none is cut, and all are renumbered Q-001…
// - Every final question has related_capabilities and related_requirements,
//   with the link completed in both directions: a question gains the
//   requirements that cite it, and the capabilities of its requirements.
// - Domain terms: repeated ones (same term after normalizing) are removed.
public static class CatalogMergerV2
{
    private static readonly string[] QuestionOrder = ["contradiction", "scope", "missing_value", "ambiguity", "missing_actor"];

    // A question while the catalogs are joined: where it comes from and its links.
    private sealed class Question(string origin, JsonObject item)
    {
        public string Origin { get; } = origin; // "QI-001" or "lote 2 Q-001", for the FIX messages
        public string Type { get; } = Str(item, "type")!;
        public string Text { get; } = Str(item, "question")!;
        public List<string> Capabilities { get; } = StrList(item, "related_capabilities");
        public List<string> Requirements { get; } = []; // final requirement ids
        public JsonArray Traces { get; } = item["source_trace"]!.AsArray().DeepClone().AsArray();
        public Question? MergedInto { get; set; }
        public string? FinalId { get; set; }

        public Question Kept => MergedInto?.Kept ?? this;
    }

    // inventoryJson: the normalized inventory of the model. lotCatalogs: the
    // accepted catalog of each lot (CatalogV2ValidationReport.Catalog), in lot order.
    public static MergedCatalogV2 Merge(string inventoryJson, IReadOnlyList<string> lotCatalogs)
    {
        var inventory = JsonNode.Parse(inventoryJson)!.AsObject();
        var lots = lotCatalogs.Select(c => JsonNode.Parse(c)!.AsObject()).ToList();
        var fixes = new List<string>();

        // 1. Questions of the inventory first, then those of each lot, in order.
        var questions = new List<Question>();
        var inventoryQuestions = new Dictionary<string, Question>();
        foreach (var item in inventory["open_questions"]!.AsArray().Select(x => x!.AsObject()))
        {
            var question = new Question(Str(item, "id")!, item);
            inventoryQuestions[Str(item, "id")!] = question;
            questions.Add(question);
        }

        // 2. Requirements renumbered by type; their questions are kept as Question objects
        //    until the questions get their final ids.
        var counters = RequirementChecks.Types.ToDictionary(t => t, _ => 0);
        var requirements = new List<(JsonObject Item, List<Question> Questions)>();
        var objections = new JsonArray();
        var domainTerms = new JsonArray();
        var seenTerms = new HashSet<string>();
        for (var k = 0; k < lots.Count; k++)
        {
            var lot = lots[k];
            var newRequirementIds = new Dictionary<string, string>();
            foreach (var r in lot["requirements"]!.AsArray())
            {
                var type = Str(r!, "type")!;
                counters[type]++;
                newRequirementIds.TryAdd(Str(r!, "id")!, $"{type}-{counters[type]:000}");
            }
            var lotQuestions = new Dictionary<string, Question>();
            foreach (var item in lot["open_questions"]!.AsArray().Select(x => x!.AsObject()))
            {
                var question = new Question($"lote {k + 1} {Str(item, "id")}", item);
                question.Requirements.AddRange(StrList(item, "related_requirements").Select(x => newRequirementIds.GetValueOrDefault(x, x)));
                lotQuestions.TryAdd(Str(item, "id")!, question);
                questions.Add(question);
            }
            foreach (var r in lot["requirements"]!.AsArray().Select(x => x!.AsObject()))
            {
                var copy = r.DeepClone().AsObject();
                copy["id"] = newRequirementIds[Str(r, "id")!];
                copy["derived_from"] = StringArray(StrList(r, "derived_from").Select(x => newRequirementIds.GetValueOrDefault(x, x)));
                var linked = StrList(r, "open_question_ids")
                    .Select(x => lotQuestions.GetValueOrDefault(x) ?? inventoryQuestions.GetValueOrDefault(x))
                    .OfType<Question>()
                    .ToList();
                requirements.Add((copy, linked));
            }
            foreach (var o in lot["capability_objections"]!.AsArray())
                objections.Add(o!.DeepClone());
            foreach (var term in lot["domain_terms"]!.AsArray())
            {
                if (seenTerms.Add(RequirementChecks.QuoteKey(Str(term!, "term")!)))
                    domainTerms.Add(term!.DeepClone());
            }
        }

        // 3. Identical questions (same type and same text after normalizing) are merged into the first one.
        var firstByKey = new Dictionary<(string, string), Question>();
        foreach (var question in questions)
        {
            if (firstByKey.TryGetValue((question.Type, RequirementChecks.QuoteKey(question.Text)), out var kept))
            {
                question.MergedInto = kept;
                kept.Capabilities.AddRange(question.Capabilities.Where(x => !kept.Capabilities.Contains(x)));
                kept.Requirements.AddRange(question.Requirements.Where(x => !kept.Requirements.Contains(x)));
                foreach (var trace in question.Traces)
                {
                    if (!kept.Traces.Any(t => JsonNode.DeepEquals(t, trace)))
                        kept.Traces.Add(trace!.DeepClone());
                }
                fixes.Add($"{question.Origin} FIX pregunta idéntica a {kept.Origin}: fusionadas");
            }
            else
            {
                firstByKey[(question.Type, RequirementChecks.QuoteKey(question.Text))] = question;
            }
        }
        var final = questions.Where(q => q.MergedInto is null)
            .OrderBy(q => Array.IndexOf(QuestionOrder, q.Type)) // stable: arrival order within a type
            .ToList();
        for (var i = 0; i < final.Count; i++)
            final[i].FinalId = $"Q-{i + 1:000}";

        // 4. Links in both directions: requirement -> question and question -> requirement and capabilities.
        var capabilitiesOf = requirements.ToDictionary(r => Str(r.Item, "id")!, r => StrList(r.Item, "capability_ids"));
        foreach (var (item, linked) in requirements)
        {
            var id = Str(item, "id")!;
            var kept = linked.Select(q => q.Kept).Distinct().ToList();
            foreach (var question in kept.Where(q => !q.Requirements.Contains(id)))
                question.Requirements.Add(id);
            item["open_question_ids"] = StringArray(kept.Select(q => q.FinalId!));
        }
        foreach (var question in final)
        {
            foreach (var rid in question.Requirements)
            {
                var requirement = requirements.First(r => Str(r.Item, "id") == rid).Item;
                var ids = StrList(requirement, "open_question_ids");
                if (!ids.Contains(question.FinalId!))
                    requirement["open_question_ids"] = StringArray([.. ids, question.FinalId!]);
                foreach (var cap in capabilitiesOf[rid].Where(x => !question.Capabilities.Contains(x)))
                    question.Capabilities.Add(cap);
            }
        }

        var merged = new JsonObject
        {
            ["schema_version"] = inventory["schema_version"]!.DeepClone(),
            ["system"] = inventory["system"]!.DeepClone(),
            ["language"] = inventory["language"]!.DeepClone(),
            ["capabilities"] = inventory["capabilities"]!.DeepClone(),
            ["capability_objections"] = objections,
            ["requirements"] = new JsonArray(requirements.Select(r => (JsonNode)r.Item).ToArray()),
            ["open_questions"] = new JsonArray(final.Select(q => (JsonNode)new JsonObject
            {
                ["id"] = q.FinalId,
                ["type"] = q.Type,
                ["question"] = q.Text,
                ["related_capabilities"] = StringArray(q.Capabilities),
                ["related_requirements"] = StringArray(q.Requirements),
                ["source_trace"] = q.Traces.DeepClone(),
            }).ToArray()),
            ["discarded_statements"] = inventory["discarded_statements"]!.DeepClone(),
            ["discarded_sections"] = inventory["discarded_sections"]!.DeepClone(),
            ["domain_terms"] = domainTerms,
            ["verdict"] = MergeVerdicts(lots.Select(l => l["verdict"]!.AsObject()).ToList()),
        };
        return new MergedCatalogV2(merged.ToJsonString(Indented), fixes);
    }

    // "complete" only if every lot was complete ("failed" if any failed, and
    // "complete" with no lots); issues and uncovered capabilities are joined.
    private static JsonObject MergeVerdicts(List<JsonObject> verdicts)
    {
        var statuses = verdicts.Select(v => Str(v, "status")!).ToList();
        var status = statuses.Contains("failed") ? "failed" : statuses.All(s => s == "complete") ? "complete" : "partial";
        return new JsonObject
        {
            ["status"] = status,
            ["issues"] = new JsonArray(verdicts.SelectMany(v => v["issues"]!.AsArray()).Select(i => i!.DeepClone()).ToArray()),
            ["uncovered_capabilities"] = new JsonArray(verdicts.SelectMany(v => v["uncovered_capabilities"]!.AsArray()).Select(i => i!.DeepClone()).ToArray()),
        };
    }
}
