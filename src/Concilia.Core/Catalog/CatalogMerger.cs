using System.Text.Json;
using System.Text.Json.Nodes;

namespace Concilia.Core.Catalog;

// Steps 28 and 31: joins the catalogs of the lots of one model into one catalog
// (v1.3). The lots are concatenated in order and their ids renumbered
// consecutively, each type on its own (RF-, RNF-, RA-, RT-, RI-), plus Q- and
// CAP- (each lot starts again at 001). Every reference is rewritten:
// derived_from, open_question_ids and capability_ids of the requirements, and
// related_requirements of the open questions. Nothing is deduplicated here
// (that is step 32 and, later, the arbiter's job).
public static class CatalogMerger
{
    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // lotCatalogs: the accepted catalog of each lot (CatalogValidationReport.Catalog),
    // in lot order.
    public static string Merge(IReadOnlyList<string> lotCatalogs)
    {
        if (lotCatalogs.Count == 0)
        {
            throw new ArgumentException("No hay catálogos que unir.", nameof(lotCatalogs));
        }

        var lots = lotCatalogs.Select(c => JsonNode.Parse(c)!.AsObject()).ToList();
        var capabilities = new JsonArray();
        var requirements = new JsonArray();
        var questions = new JsonArray();
        var discarded = new JsonArray();
        var discardedSections = new JsonArray();
        var domainTerms = new JsonArray();
        var counters = CatalogValidator.Types.ToDictionary(t => t, _ => 0);

        foreach (var lot in lots)
        {
            // The new id of each requirement, in order, and old id of this lot -> new id.
            var lotRequirements = lot["requirements"]!.AsArray();
            var newIds = new List<string>();
            var newRequirementIds = new Dictionary<string, string>();
            foreach (var requirement in lotRequirements)
            {
                var type = requirement!["type"]!.GetValue<string>();
                counters[type]++;
                newIds.Add($"{type}-{counters[type]:000}");
                newRequirementIds.TryAdd(requirement["id"]!.GetValue<string>(), newIds[^1]);
            }
            var newQuestionIds = Renumber(lot["open_questions"]!.AsArray(), "Q", questions.Count);
            var newCapabilityIds = Renumber(lot["capabilities"]!.AsArray(), "CAP", capabilities.Count);

            foreach (var capability in lot["capabilities"]!.AsArray())
            {
                var copy = capability!.DeepClone().AsObject();
                copy["id"] = newCapabilityIds[capability["id"]!.GetValue<string>()];
                capabilities.Add(copy);
            }

            for (var i = 0; i < lotRequirements.Count; i++)
            {
                var copy = lotRequirements[i]!.DeepClone().AsObject();
                copy["id"] = newIds[i];
                Rewrite(copy, "derived_from", newRequirementIds);
                Rewrite(copy, "open_question_ids", newQuestionIds);
                Rewrite(copy, "capability_ids", newCapabilityIds);
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
                discarded.Add(item!.DeepClone());
            foreach (var item in lot["discarded_sections"]?.AsArray() ?? new JsonArray())
                discardedSections.Add(item!.DeepClone());
            foreach (var item in lot["domain_terms"]?.AsArray() ?? new JsonArray())
                domainTerms.Add(item!.DeepClone());
        }

        var first = lots[0];
        var merged = new JsonObject
        {
            ["schema_version"] = first["schema_version"]!.DeepClone(),
            ["system"] = first["system"]!.DeepClone(),
            ["language"] = first["language"]!.DeepClone(),
            ["capabilities"] = capabilities,
            ["requirements"] = requirements,
            ["open_questions"] = questions,
            ["discarded_statements"] = discarded,
            ["discarded_sections"] = discardedSections,
            ["domain_terms"] = domainTerms,
            ["verdict"] = MergeVerdicts(lots.Select(l => l["verdict"]!.AsObject()).ToList()),
        };
        return merged.ToJsonString(Indented);
    }

    // Q-001, Q-002... of one lot become Q-(offset+1), Q-(offset+2)... in the
    // order they appear.
    private static Dictionary<string, string> Renumber(JsonArray items, string prefix, int offset)
    {
        var map = new Dictionary<string, string>();
        var number = offset;
        foreach (var item in items)
        {
            number++;
            map.TryAdd(item!["id"]!.GetValue<string>(), $"{prefix}-{number:000}");
        }
        return map;
    }

    // A reference to an id the lot does not define is left as it is: the
    // validator of the lot already removed those, so it does not happen with
    // accepted catalogs, and hiding it here would lose information.
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

    // The self-check holds only if it held in every lot, issues and unprocessed
    // fragments are joined, and the status is "complete" only if every lot was
    // complete ("failed" if any failed).
    private static JsonObject MergeVerdicts(List<JsonObject> verdicts)
    {
        var statuses = verdicts.Select(v => v["status"]!.GetValue<string>()).ToList();
        var status = statuses.Contains("failed") ? "failed" : statuses.All(s => s == "complete") ? "complete" : "partial";

        var selfCheck = new JsonObject();
        foreach (var name in verdicts[0]["self_check"]!.AsObject().Select(p => p.Key))
        {
            selfCheck[name] = verdicts.All(v => v["self_check"]![name]!.GetValue<bool>());
        }

        return new JsonObject
        {
            ["status"] = status,
            ["self_check"] = selfCheck,
            ["issues"] = new JsonArray(verdicts.SelectMany(v => v["issues"]!.AsArray()).Select(i => i!.DeepClone()).ToArray()),
            ["unprocessed_fragments"] = new JsonArray(verdicts.SelectMany(v => v["unprocessed_fragments"]!.AsArray()).Select(i => i!.DeepClone()).ToArray()),
        };
    }
}
