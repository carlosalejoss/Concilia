using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReqArbiter.Core.Catalog;

// Step 28: joins the catalogs of the lots of one model into one catalog.
// The lots are concatenated in order and their ids renumbered consecutively
// (each lot starts again at REQ-001 and Q-001), rewriting every reference:
// derived_from and open_question_ids of the requirements, and
// related_requirements of the open questions. Nothing is deduplicated between
// lots: comparing requirements is the arbiter's job.
public static class CatalogMerger
{
    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // lotCatalogs: the catalog JSON of each lot, in lot order. All of them
    // must be valid (the caller only passes lots that ended Done).
    public static string Merge(IReadOnlyList<string> lotCatalogs)
    {
        if (lotCatalogs.Count == 0)
        {
            throw new ArgumentException("No hay catálogos que unir.", nameof(lotCatalogs));
        }

        var lots = lotCatalogs.Select(c => JsonNode.Parse(c)!.AsObject()).ToList();
        var requirements = new JsonArray();
        var questions = new JsonArray();
        var discarded = new JsonArray();
        var domainTerms = new JsonArray();

        foreach (var lot in lots)
        {
            // Old id of this lot -> new id in the merged catalog.
            var newRequirementIds = Renumber(lot["requirements"]!.AsArray(), "REQ", requirements.Count);
            var newQuestionIds = Renumber(lot["open_questions"]!.AsArray(), "Q", questions.Count);

            foreach (var requirement in lot["requirements"]!.AsArray())
            {
                var copy = requirement!.DeepClone().AsObject();
                copy["id"] = newRequirementIds[requirement["id"]!.GetValue<string>()];
                Rewrite(copy, "derived_from", newRequirementIds);
                Rewrite(copy, "open_question_ids", newQuestionIds);
                requirements.Add(copy);
            }

            foreach (var question in lot["open_questions"]!.AsArray())
            {
                var copy = question!.DeepClone().AsObject();
                copy["id"] = newQuestionIds[question["id"]!.GetValue<string>()];
                Rewrite(copy, "related_requirements", newRequirementIds);
                questions.Add(copy);
            }

            foreach (var item in lot["discarded_statements"]!.AsArray())
            {
                discarded.Add(item!.DeepClone());
            }
            foreach (var item in lot["domain_terms"]?.AsArray() ?? new JsonArray())
            {
                domainTerms.Add(item!.DeepClone());
            }
        }

        var first = lots[0];
        var merged = new JsonObject
        {
            ["schema_version"] = first["schema_version"]!.DeepClone(),
            ["system"] = first["system"]!.DeepClone(),
            ["language"] = first["language"]!.DeepClone(),
            ["requirements"] = requirements,
            ["open_questions"] = questions,
            ["discarded_statements"] = discarded,
            ["domain_terms"] = domainTerms,
            ["verdict"] = MergeVerdicts(lots.Select(l => l["verdict"]!.AsObject()).ToList()),
        };
        return merged.ToJsonString(Indented);
    }

    // REQ-001, REQ-002... of one lot become REQ-(offset+1), REQ-(offset+2)...
    // in the order they appear.
    private static Dictionary<string, string> Renumber(JsonArray items, string prefix, int offset)
    {
        var map = new Dictionary<string, string>();
        foreach (var item in items)
        {
            var number = offset + map.Count + 1;
            map[item!["id"]!.GetValue<string>()] = $"{prefix}-{number.ToString("D3", CultureInfo.InvariantCulture)}";
        }
        return map;
    }

    // A reference to an id the lot does not define is left as it is: the
    // validator of the lot would have rejected it (C11), so it does not happen
    // with valid lots, and hiding it here would lose information.
    private static void Rewrite(JsonObject item, string property, Dictionary<string, string> newIds)
    {
        if (item[property] is not JsonArray references)
        {
            return;
        }
        item[property] = new JsonArray(references
            .Select(r => r!.GetValue<string>())
            .Select(id => (JsonNode)JsonValue.Create(newIds.GetValueOrDefault(id, id)))
            .ToArray());
    }

    // Counts are added, the self-check holds only if it held in every lot,
    // issues and unprocessed fragments are joined, and the status is
    // "complete" only if every lot was complete ("failed" if any failed).
    private static JsonObject MergeVerdicts(List<JsonObject> verdicts)
    {
        var statuses = verdicts.Select(v => v["status"]!.GetValue<string>()).ToList();
        var status = statuses.Contains("failed") ? "failed" : statuses.All(s => s == "complete") ? "complete" : "partial";

        var counts = new JsonObject();
        foreach (var name in verdicts[0]["counts"]!.AsObject().Select(p => p.Key))
        {
            counts[name] = verdicts.Sum(v => v["counts"]![name]!.GetValue<int>());
        }

        var selfCheck = new JsonObject();
        foreach (var name in verdicts[0]["self_check"]!.AsObject().Select(p => p.Key))
        {
            selfCheck[name] = verdicts.All(v => v["self_check"]![name]!.GetValue<bool>());
        }

        return new JsonObject
        {
            ["status"] = status,
            ["counts"] = counts,
            ["self_check"] = selfCheck,
            ["issues"] = new JsonArray(verdicts.SelectMany(v => v["issues"]!.AsArray()).Select(i => i!.DeepClone()).ToArray()),
            ["unprocessed_fragments"] = new JsonArray(verdicts.SelectMany(v => v["unprocessed_fragments"]!.AsArray()).Select(i => i!.DeepClone()).ToArray()),
        };
    }
}
