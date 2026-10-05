using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LateApexEarlySpeed.Json.Schema;
using static ReqArbiter.Core.Catalog.CatalogJson;
using static ReqArbiter.Core.Catalog.RequirementChecks;

namespace ReqArbiter.Core.Catalog;

// Figures of an inventory, with the same names as the reference report.
public record InventoryMetrics(
    [property: JsonPropertyName("funcionalidades")] int Capabilities,
    [property: JsonPropertyName("por_kind")] IReadOnlyDictionary<string, int> ByKind,
    [property: JsonPropertyName("fragmentos_por_funcionalidad")] double? FragmentsPerCapability,
    [property: JsonPropertyName("preguntas")] int Questions,
    [property: JsonPropertyName("preguntas_rechazadas")] int QuestionsRejected,
    [property: JsonPropertyName("preguntas_por_tipo")] IReadOnlyDictionary<string, int> QuestionsByType,
    [property: JsonPropertyName("descartes")] int Discards,
    [property: JsonPropertyName("secciones_descartadas")] int DiscardedSections,
    [property: JsonPropertyName("fragmentos_asignados")] string AssignedFragments,
    [property: JsonPropertyName("citas_literales")] string LiteralQuotes,
    [property: JsonPropertyName("posibles_repeticiones")] int PossibleRepetitions,
    [property: JsonPropertyName("correcciones")] int Fixes);

// Result of validating the inventory of phase 1 (catalog v2):
// - Valid is false when the inventory has errors (JSON, root structure, IDS,
//   C10, status failed): it is repaired, and there is no Inventory.
// - Rejected: questions taken out because of their quotes (C6).
// Inventory is the normalized inventory, renumbered (CAP-001…, QI-001…), the
// one the lots of phase 2 receive; Ids maps the model's ids to the final ones.
public record InventoryValidationReport(
    [property: JsonPropertyName("valid")] bool Valid,
    [property: JsonPropertyName("needs_repair")] bool NeedsRepair,
    [property: JsonPropertyName("repair_reasons")] IReadOnlyList<string> RepairReasons,
    [property: JsonPropertyName("errors")] IReadOnlyList<string> Errors,
    [property: JsonPropertyName("rejected")] IReadOnlyList<RejectedElement> Rejected,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("metrics")] InventoryMetrics? Metrics,
    [property: JsonPropertyName("ids")] IReadOnlyDictionary<string, string> Ids,
    [property: JsonIgnore] string? Inventory);

// Deterministic validator of the capability inventory v2 (phase 1) written by
// model A or B. Port of check() in prompts/generacion_requisitos/validar_inventario.py,
// the reference: for the same files it must give the same outcome (see the
// fixtures in tests/fixtures/inventory-mutations). Keep both in sync. Steps:
// 1. Schema: an error in the root structure is an error of the inventory; an
//    element that fails the schema is ignored with a warning.
// 2. Normalizer (FIX): fragments that do not exist, quotes of the questions,
//    references to capabilities that do not exist, and exclusivity (a fragment
//    assigned to a capability is not discarded).
// 3. Checks: IDS (repeated ids, error), C6 (quotes of the questions, rejection),
//    KIND and REP (warnings).
// 4. Checks of the inventory: C10 with threshold 0 and the verdict.
// 5. Renumbering of CAP- and QI-.
// 6. Decision: any error asks for a repair.
// Messages of an element start with the id the model wrote; those of the whole
// inventory, with INVENTARIO.
public static class InventoryValidator
{
    // REP: possible repetitions between capabilities. Initial thresholds, to be calibrated.
    private const double RepFragments = 0.5; // Jaccard of fragments from which there is a warning
    private const double RepNames = 0.75;    // Jaccard of the words of the names from which there is a warning
    private const int RepShared = 3;         // a fragment of 3 or more capabilities (summary, matrix) does not count

    // Articles and prepositions that do not count when names are compared (REP).
    private static readonly Dictionary<string, HashSet<string>> NameStopwords = new()
    {
        ["es"] = ["el", "la", "los", "las", "lo", "un", "una", "unos", "unas", "al", "del",
                  "a", "ante", "bajo", "con", "contra", "de", "desde", "durante", "en", "entre", "hacia", "hasta",
                  "mediante", "para", "por", "segun", "sin", "sobre", "tras"],
        ["en"] = ["the", "a", "an",
                  "about", "above", "across", "after", "against", "along", "among", "around", "at", "before", "behind",
                  "below", "between", "by", "during", "for", "from", "in", "into", "of", "off", "on", "onto", "over",
                  "per", "through", "to", "toward", "towards", "under", "until", "up", "upon", "via", "with", "within",
                  "without"],
    };

    private static readonly string[] KindsWithoutActors = ["quality", "constraint"];
    private static readonly string[] Kinds = ["functional", "integration", "quality", "constraint"];
    private static readonly string[] QuestionTypes = ["scope", "contradiction", "ambiguity", "missing_actor"];

    // Lists of the inventory, with the schema definition of each item and the label used in messages.
    private static readonly (string Key, string Definition, string Prefix)[] ItemLists =
    [
        ("capabilities", "capability", "CAP"),
        ("discarded_statements", "discarded", "DISCARDED"),
        ("discarded_sections", "discarded_section", "SECTION"),
        ("open_questions", "open_question", "QI"),
    ];

    private static readonly string SchemaText = ReadResource("inventory.schema.json");
    private static readonly JsonValidator RootValidator = new(SchemaText);
    private static readonly Dictionary<string, JsonValidator> ItemValidators =
        ItemLists.ToDictionary(l => l.Key, l => DefinitionValidator(SchemaText, l.Definition));

    // fragments: all the fragments of the documents (and of the doubts), in order;
    // without them the quotes, the coverage and the missing fragments are not checked.
    public static InventoryValidationReport Validate(string inventoryJson, IReadOnlyList<Fragment>? fragments)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(inventoryJson);
        }
        catch (JsonException e)
        {
            return Failure([$"INVENTARIO JSON no válido: {e.Message}"]);
        }
        return Check(root, fragments);
    }

    private static InventoryValidationReport Failure(List<string> errors) =>
        new(false, true, errors, errors, [], [], null, new Dictionary<string, string>(), null);

    // Words of a name for REP: lower case, without accents, articles or prepositions.
    private static HashSet<string> NameWords(string name, string lang)
    {
        var decomposed = name.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var plain = new string(decomposed.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Matches(plain, @"\w+").Select(m => m.Value).Where(w => !NameStopwords[lang].Contains(w)).ToHashSet();
    }

    private static double Jaccard(HashSet<string> a, HashSet<string> b)
    {
        var union = a.Union(b).Count();
        return union == 0 ? 0 : (double)a.Intersect(b).Count() / union;
    }

    private static InventoryValidationReport Check(JsonNode? inv, IReadOnlyList<Fragment>? fragmentList)
    {
        var warnings = new List<string>();
        var errors = new List<string>();

        // --- 1. Schema ----------------------------------------------------------------------------
        var rootErrors = SchemaErrors(RootValidator, WithoutItems(inv, ItemLists.Select(l => l.Key))).Select(m => $"INVENTARIO {m}").ToList();
        if (rootErrors.Count > 0)
            return Failure(rootErrors);

        var c = inv!.DeepClone().AsObject();
        foreach (var (key, _, prefix) in ItemLists)
        {
            var kept = new JsonArray();
            var items = c[key]!.AsArray();
            for (var i = 0; i < items.Count; i++)
            {
                var errs = SchemaErrors(ItemValidators[key], items[i], $"{key}/{i}");
                if (errs.Count > 0)
                    warnings.AddRange(errs.Select(m => $"{Label(items[i], $"{prefix}[{i}]")} {m} (se ignora)"));
                else
                    kept.Add(items[i]!.DeepClone());
            }
            c[key] = kept;
        }

        var caps = c["capabilities"]!.AsArray().Select(x => x!.AsObject()).ToList();
        var discards = c["discarded_statements"]!.AsArray().Select(x => x!.AsObject()).ToList();
        var sections = c["discarded_sections"]!.AsArray().Select(x => x!.AsObject()).ToList();
        var qs = c["open_questions"]!.AsArray().Select(x => (Item: x!.AsObject(), Id: Str(x, "id")!, Errors: new List<string>())).ToList();
        var fragments = fragmentList?.ToDictionary(f => f.Id);
        var docFragIds = (fragmentList ?? []).Where(f => f.Kind == "document").Select(f => f.Id).ToList();

        // --- 2. Normalizer --------------------------------------------------------------------------
        void Fix(string owner, string what) => warnings.Add($"{owner} FIX {what}");

        if (fragments is not null)
        {
            foreach (var cp in caps)
            {
                var capFragments = StrList(cp, "fragments");
                var missing = capFragments.Where(f => !fragments.ContainsKey(f)).ToList();
                if (missing.Count > 0)
                {
                    Fix(Str(cp, "id")!, $"fragmentos inexistentes quitados: {string.Join(", ", missing)}");
                    cp["fragments"] = StringArray(capFragments.Where(fragments.ContainsKey));
                }
            }
            var keptDiscards = new List<JsonObject>();
            for (var i = 0; i < discards.Count; i++)
            {
                if (!fragments.ContainsKey(Str(discards[i], "fragment")!))
                    Fix($"DISCARDED[{i}]", $"descarte de un fragmento inexistente {Str(discards[i], "fragment")} quitado");
                else
                    keptDiscards.Add(discards[i]);
            }
            discards = keptDiscards;
        }

        foreach (var q in qs) // quotes in the wrong fragment (literal in a single one) and repeated quotes
            NormalizeTraces(q.Item, q.Id, fragments, fragmentList ?? [], Fix);

        // IDS: a repeated id is an error (references would be ambiguous); a reference to a
        // capability that does not exist is removed.
        foreach (var (what, written) in new[] { ("funcionalidad", caps.Select(cp => Str(cp, "id")!)), ("pregunta", qs.Select(q => q.Id)) })
        {
            foreach (var dup in written.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key))
                errors.Add($"INVENTARIO IDS id de {what} repetido: {dup}");
        }
        var capIds = caps.Select(cp => Str(cp, "id")!).ToHashSet();
        foreach (var q in qs)
        {
            var refs = StrList(q.Item, "related_capabilities");
            foreach (var x in refs.Where(x => !capIds.Contains(x)))
                Fix(q.Id, $"referencia a funcionalidad inexistente {x} quitada");
            q.Item["related_capabilities"] = StringArray(refs.Where(capIds.Contains).Distinct());
        }

        // Exclusivity: a fragment assigned to a capability is not discarded. Its discard is removed;
        // a discarded section with some assigned fragment is replaced by discards of its other
        // fragments, with the reason of the section (skipping those that already have a discard).
        var ownerOf = new Dictionary<string, string>();
        foreach (var cp in caps)
        {
            foreach (var f in StrList(cp, "fragments"))
                ownerOf.TryAdd(f, Str(cp, "id")!);
        }
        var notAssigned = new List<JsonObject>();
        for (var i = 0; i < discards.Count; i++)
        {
            var fragmentId = Str(discards[i], "fragment")!;
            if (ownerOf.TryGetValue(fragmentId, out var owner))
                Fix($"DISCARDED[{i}]", $"descarte de {fragmentId} quitado: el fragmento está en {owner}");
            else
                notAssigned.Add(discards[i]);
        }
        discards = notAssigned;
        if (fragments is not null)
        {
            var keptSections = new List<JsonObject>();
            for (var i = 0; i < sections.Count; i++)
            {
                var d = sections[i];
                var inside = docFragIds.Where(id => InSection(fragments[id].Section, Str(d, "section")!)).ToList();
                var assigned = inside.Where(ownerOf.ContainsKey).ToList();
                if (assigned.Count == 0)
                {
                    keptSections.Add(d);
                    continue;
                }
                var already = discards.Select(x => Str(x, "fragment")!).ToHashSet();
                var added = inside.Where(id => !ownerOf.ContainsKey(id) && !already.Contains(id)).ToList();
                foreach (var id in added)
                    discards.Add(new JsonObject { ["fragment"] = id, ["reason"] = Str(d, "reason"), ["note"] = null });
                var addedText = added.Count > 0 ? string.Join(", ", added) : "ningún fragmento";
                Fix($"SECTION[{i}]", $"sección '{Str(d, "section")}' sustituida por descartes de {addedText}: están asignados {string.Join(", ", assigned)}");
            }
            sections = keptSections;
        }

        // --- 3. Checks of each element ------------------------------------------------------------
        var traces = new TraceChecker(fragments);
        foreach (var q in qs) // C6: quotes of the questions (they reject the question)
        {
            foreach (var t in q.Item["source_trace"]!.AsArray().Select(x => x!.AsObject()))
                q.Errors.AddRange(traces.Check(t));
        }

        foreach (var cp in caps) // KIND
        {
            var actors = StrList(cp, "actors");
            if (KindsWithoutActors.Contains(Str(cp, "kind")) && actors.Count > 0)
                warnings.Add($"{Str(cp, "id")} KIND una funcionalidad {Str(cp, "kind")} no lleva actores: {string.Join(", ", actors)}");
        }

        // REP: possible repetitions, by shared fragments or by similar names. Fragments of
        // RepShared or more capabilities (a summary, a permission matrix) do not count.
        var lang = Str(c, "language")!;
        var shared = caps.SelectMany(cp => StrList(cp, "fragments").Distinct()).GroupBy(f => f).ToDictionary(g => g.Key, g => g.Count());
        var own = caps.Select(cp => StrList(cp, "fragments").Where(f => shared[f] < RepShared).ToHashSet()).ToList();
        var names = caps.Select(cp => NameWords(Str(cp, "name")!, lang)).ToList();
        for (var i = 0; i < caps.Count; i++)
        {
            for (var j = i + 1; j < caps.Count; j++)
            {
                var byFragments = Jaccard(own[i], own[j]);
                var byName = Jaccard(names[i], names[j]);
                if (byFragments < RepFragments && byName < RepNames)
                    continue;
                var reasons = new List<string>();
                if (byFragments >= RepFragments)
                    reasons.Add($"comparten {own[i].Intersect(own[j]).Count()} de {own[i].Union(own[j]).Count()} fragmentos");
                if (byName >= RepNames)
                    reasons.Add("nombres parecidos");
                warnings.Add($"{Str(caps[i], "id")} REP posible repetición con {Str(caps[j], "id")}: {string.Join("; ", reasons)}");
            }
        }

        // --- 4. Checks of the inventory -------------------------------------------------------------
        var verdict = c["verdict"]!.AsObject();
        var unprocessed = StrList(verdict, "unprocessed_fragments");
        if (fragments is not null)
        {
            var sectionFrags = new HashSet<string>();
            for (var i = 0; i < sections.Count; i++)
            {
                var section = Str(sections[i], "section")!;
                var inside = docFragIds.Where(id => InSection(fragments[id].Section, section)).ToList();
                if (inside.Count == 0)
                    warnings.Add($"SECTION[{i}] C10 la sección '{section}' no tiene fragmentos (se ignora)");
                sectionFrags.UnionWith(inside);
            }
            var treated = ownerOf.Keys.ToHashSet();
            treated.UnionWith(discards.Select(d => Str(d, "fragment")!));
            treated.UnionWith(sectionFrags);
            treated.UnionWith(qs.SelectMany(q => TraceFragments(q.Item)));
            var untreated = docFragIds.Where(id => !treated.Contains(id)).ToList();
            if (untreated.Count > 0) // threshold 0: a fragment not assigned reaches no lot
                errors.Add($"INVENTARIO C10 fragmentos sin tratar: {string.Join(", ", untreated)}");
            if (unprocessed.Count > 0)
                warnings.Add($"INVENTARIO C10 fragmentos declarados sin procesar: {string.Join(", ", unprocessed.Order(StringComparer.Ordinal))}");
        }

        var status = Str(verdict, "status");
        if (status == "failed")
            errors.Add("INVENTARIO VERDICT el inventario declara status failed");
        if (status == "complete" && unprocessed.Count > 0)
            warnings.Add("INVENTARIO VERDICT status complete con unprocessed_fragments no vacío");
        if (status == "partial" && unprocessed.Count == 0)
            warnings.Add("INVENTARIO VERDICT status partial sin unprocessed_fragments");

        // --- 5. Rejections and renumbering --------------------------------------------------------
        var rejected = qs.Where(q => q.Errors.Count > 0).Select(q => new RejectedElement(q.Id, "question", q.Errors)).ToList();
        var acceptedQs = qs.Where(q => q.Errors.Count == 0).ToList();
        var newCaps = new Dictionary<string, string>();
        for (var i = 0; i < caps.Count; i++)
            newCaps.TryAdd(Str(caps[i], "id")!, $"CAP-{i + 1:000}");
        var newQs = new Dictionary<string, string>();
        for (var i = 0; i < acceptedQs.Count; i++)
            newQs.TryAdd(acceptedQs[i].Id, $"QI-{i + 1:000}");
        var renumbered = newCaps.Count(p => p.Key != p.Value) + newQs.Count(p => p.Key != p.Value);
        var outCaps = new JsonArray();
        foreach (var cp in caps)
        {
            var copy = cp.DeepClone().AsObject();
            copy["id"] = newCaps[Str(cp, "id")!];
            outCaps.Add(copy);
        }
        var outQs = new JsonArray();
        foreach (var q in acceptedQs)
        {
            var copy = q.Item.DeepClone().AsObject();
            copy["id"] = newQs[q.Id];
            copy["related_capabilities"] = StringArray(StrList(q.Item, "related_capabilities").Select(x => newCaps[x]));
            outQs.Add(copy);
        }
        if (renumbered > 0)
            warnings.Add($"INVENTARIO FIX {renumbered} ids renumerados (CAP- y QI-, en orden de aparición)");

        // --- Metrics ----------------------------------------------------------------------------
        var assignedDocs = docFragIds.Count(ownerOf.ContainsKey);
        var metrics = new InventoryMetrics(
            Capabilities: caps.Count,
            ByKind: Kinds.ToDictionary(k => k, k => caps.Count(cp => Str(cp, "kind") == k)),
            FragmentsPerCapability: caps.Count > 0 ? Math.Round((double)caps.Sum(cp => StrList(cp, "fragments").Count) / caps.Count, 2) : null,
            Questions: acceptedQs.Count,
            QuestionsRejected: rejected.Count,
            QuestionsByType: QuestionTypes.ToDictionary(t => t, t => acceptedQs.Count(q => Str(q.Item, "type") == t)),
            Discards: discards.Count,
            DiscardedSections: sections.Count,
            AssignedFragments: fragments is not null ? $"{assignedDocs}/{docFragIds.Count}" : "no comprobado",
            LiteralQuotes: fragments is not null ? $"{traces.Ok}/{traces.Total}" : "no comprobado",
            PossibleRepetitions: warnings.Count(w => w.Split(' ') is { Length: > 1 } parts && parts[1] == "REP"),
            Fixes: warnings.Count(w => w.Split(' ') is { Length: > 1 } parts && parts[1] == "FIX"));

        var valid = errors.Count == 0;
        var inventory = new JsonObject
        {
            ["schema_version"] = c["schema_version"]!.DeepClone(),
            ["system"] = c["system"]!.DeepClone(),
            ["language"] = c["language"]!.DeepClone(),
            ["capabilities"] = outCaps,
            ["discarded_statements"] = new JsonArray(discards.Select(d => (JsonNode)d.DeepClone()).ToArray()),
            ["discarded_sections"] = new JsonArray(sections.Select(s => (JsonNode)s.DeepClone()).ToArray()),
            ["open_questions"] = outQs,
            ["verdict"] = verdict.DeepClone(),
        };
        var ids = newCaps.Concat(newQs).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);
        return new InventoryValidationReport(
            valid, !valid, errors, errors, rejected, warnings, metrics, ids,
            valid ? inventory.ToJsonString(Indented) : null);
    }
}
