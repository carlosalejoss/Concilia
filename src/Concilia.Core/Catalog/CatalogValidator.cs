using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LateApexEarlySpeed.Json.Schema;
using static Concilia.Core.Catalog.CatalogJson;
using static Concilia.Core.Catalog.RequirementChecks;

namespace Concilia.Core.Catalog;

// Quality figures of a catalog, with the same names as the reference report.
public record CatalogMetrics(
    [property: JsonPropertyName("requisitos")] int Requirements,
    [property: JsonPropertyName("requisitos_escritos")] int RequirementsWritten,
    [property: JsonPropertyName("requisitos_rechazados")] int RequirementsRejected,
    [property: JsonPropertyName("preguntas_rechazadas")] int QuestionsRejected,
    [property: JsonPropertyName("por_tipo")] IReadOnlyDictionary<string, int> ByType,
    [property: JsonPropertyName("conformidad_sintactica")] double? SyntacticConformance,
    [property: JsonPropertyName("calidad_redaccion")] double? WritingQuality,
    [property: JsonPropertyName("coherencia_decision")] double? DecisionConsistency,
    [property: JsonPropertyName("trazabilidad")] double? Traceability,
    [property: JsonPropertyName("fidelidad_cifras")] double? FigureFidelity,
    [property: JsonPropertyName("requisitos_sin_errores")] double? RequirementsWithoutErrors,
    [property: JsonPropertyName("citas_literales")] string LiteralQuotes,
    [property: JsonPropertyName("cobertura_fragmentos")] string FragmentCoverage,
    [property: JsonPropertyName("proporcion_master")] double? MasterShare,
    [property: JsonPropertyName("ramas")] IReadOnlyDictionary<string, int> Branches,
    [property: JsonPropertyName("preguntas_abiertas")] int OpenQuestions,
    [property: JsonPropertyName("terminos_dominio")] int DomainTerms,
    [property: JsonPropertyName("funcionalidades")] int Capabilities,
    [property: JsonPropertyName("cobertura_funcionalidades")] string CapabilityCoverage,
    [property: JsonPropertyName("criterios")] int Criteria,
    [property: JsonPropertyName("criterios_por_requisito")] double? CriteriaPerRequirement,
    [property: JsonPropertyName("criterios_por_etiqueta")] IReadOnlyDictionary<string, int> CriteriaByLabel,
    [property: JsonPropertyName("correcciones")] int Fixes,
    [property: JsonPropertyName("autoverificacion_coincide")] bool SelfCheckMatches);

// A requirement or open question taken out of the catalog, with the id the
// model wrote and its errors.
public record RejectedElement(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("element")] string Element, // "requirement" or "question"
    [property: JsonPropertyName("errors")] IReadOnlyList<string> Errors);

// Result of validating the catalog of one lot (catalog v1.3, three levels):
// - Valid is false when the lot cannot be used (Errors: JSON, root structure,
//   C10 over 10 %, status failed). There is then no Catalog.
// - Rejected: requirements and questions taken out because of their own errors.
// - NeedsRepair: lot errors, more than 20 % of the requirements rejected or a
//   C20; RepairReasons are the lines of the repair annex.
// Catalog is the accepted catalog (normalized, without the rejected elements,
// renumbered by type) and Ids maps the model's ids to the final ones.
public record CatalogValidationReport(
    [property: JsonPropertyName("valid")] bool Valid,
    [property: JsonPropertyName("needs_repair")] bool NeedsRepair,
    [property: JsonPropertyName("repair_reasons")] IReadOnlyList<string> RepairReasons,
    [property: JsonPropertyName("errors")] IReadOnlyList<string> Errors,
    [property: JsonPropertyName("rejected")] IReadOnlyList<RejectedElement> Rejected,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("metrics")] CatalogMetrics? Metrics,
    [property: JsonPropertyName("ids")] IReadOnlyDictionary<string, string> Ids,
    [property: JsonIgnore] string? Catalog,
    [property: JsonIgnore] IReadOnlyDictionary<string, string?> Texts);

// Deterministic validator of the EARS/MASTeR catalog v1.3 written by model A or B.
// Port of check() in prompts/generacion_requisitos/validar_catalogo.py, the
// reference: for the same files it must give the same outcome (see the mutation
// fixtures in tests/fixtures/catalog-mutations). Keep both in sync. The steps,
// in order, are the same as in the reference:
// 1. Schema: an error in the root structure fails the lot; an error inside a
//    requirement or question rejects it; inside any other element, that
//    element is ignored with a warning.
// 2. Normalizer: mechanical fixes, each one a FIX warning.
// 3. Checks of each element: an error rejects it; a warning does nothing.
// 4. Renumbering by type of the accepted catalog.
// 5. Checks of the lot: C10, C20 and the verdict.
// 6. Decision: repair on lot errors, more than 20 % rejected or a C20.
// Messages of an element start with the id the model wrote ("RF-003 C6 ...");
// those of the lot, with LOTE.
// The checks of each requirement, the text helpers and the JSON helpers are
// shared with the v2 validator (RequirementChecks, CatalogJson).
public static class CatalogValidator
{
    public static readonly string[] Types = RequirementChecks.Types;
    private static readonly string[] FunctionalTypes = ["RF", "RI"]; // explain a capability of the inventory (C20, C21)
    public static readonly string[] Labels = RequirementChecks.Labels;

    private const double MaxRejectedShare = 0.20;  // partial acceptance: more rejected than this -> repair
    private const double MaxUntreatedShare = 0.10; // C10: more untreated fragments than this -> lot error

    // Category of each code, for the per-requirement metrics (errors and warnings count).
    private static readonly Dictionary<string, string> Category = new()
    {
        ["SCHEMA"] = "sintaxis", ["C2"] = "sintaxis", ["C14"] = "sintaxis",
        ["C5"] = "redaccion", ["C15"] = "redaccion", ["C19"] = "redaccion", ["C22"] = "redaccion",
        ["C7"] = "decision", ["C12"] = "decision",
        ["C6"] = "trazabilidad", ["C13"] = "trazabilidad", ["C16"] = "trazabilidad",
        ["N3"] = "referencias", ["C21"] = "referencias",
        ["N2"] = "fidelidad",
    };

    // Lists of the catalog whose items are checked one by one, with the schema
    // definition of each item and the label used in messages.
    private static readonly (string Key, string Definition, string Prefix)[] ItemLists =
    [
        ("requirements", "requirement", "REQ"),
        ("open_questions", "open_question", "Q"),
        ("capabilities", "capability", "CAP"),
        ("discarded_statements", "discarded", "DISCARDED"),
        ("discarded_sections", "discarded_section", "SECTION"),
        ("domain_terms", "domain_term", "DOMAIN"),
    ];

    // The schema is embedded in this DLL from prompts/generacion_requisitos/.
    private static readonly string SchemaText = ReadResource("requirements.schema.json");
    private static readonly JsonValidator RootValidator = new(SchemaText);
    private static readonly Dictionary<string, JsonValidator> ItemValidators =
        ItemLists.ToDictionary(l => l.Key, l => DefinitionValidator(SchemaText, l.Definition));

    // A requirement or question of the catalog while it is being checked.
    private sealed class Entry(JsonNode? item, string id, List<string> errors)
    {
        public JsonNode? Item { get; } = item;
        public JsonObject Obj => Item!.AsObject();
        public string Id { get; } = id;          // as the model wrote it
        public List<string> Errors { get; } = errors;
        public List<string> Warnings { get; } = [];
        public string? NewId { get; set; }
        public string? Text { get; set; }
    }

    // catalogJson: the file written by the model. fragments: the lot's
    // fragments (input/fragmentos.json), in order; without them the quotes,
    // figures, coverage and domain terms are not checked.
    public static CatalogValidationReport Validate(string catalogJson, string systemName, IReadOnlyList<Fragment>? fragments)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(catalogJson);
        }
        catch (JsonException e)
        {
            return LotFailure([$"LOTE JSON no válido: {e.Message}"]);
        }
        return Check(root, systemName, fragments);
    }

    private static CatalogValidationReport LotFailure(List<string> errors) =>
        new(false, true, errors, errors, [], [], null, new Dictionary<string, string>(), null, new Dictionary<string, string?>());

    private static CatalogValidationReport Check(JsonNode? cat, string sistema, IReadOnlyList<Fragment>? fragmentList)
    {
        var warnings = new List<string>();

        // --- 1. Schema, split by levels -------------------------------------------------------
        var lotErrors = SchemaErrors(RootValidator, WithoutItems(cat, ItemLists.Select(l => l.Key))).Select(m => $"LOTE {m}").ToList();
        if (lotErrors.Count > 0)
            return LotFailure(lotErrors);

        var c = cat!.DeepClone().AsObject();
        var lang = Str(c, "language")!;

        // Elements that are neither requirements nor questions: ignored with a warning if they fail the schema.
        foreach (var (key, _, prefix) in ItemLists.Skip(2))
        {
            var kept = new JsonArray();
            var items = c[key]!.AsArray();
            for (var i = 0; i < items.Count; i++)
            {
                var errs = ItemSchemaErrors(key, i, items[i]);
                if (errs.Count > 0)
                    warnings.AddRange(errs.Select(m => $"{Label(items[i], $"{prefix}[{i}]")} {m} (se ignora)"));
                else
                    kept.Add(items[i]!.DeepClone());
            }
            c[key] = kept;
        }

        // Requirements and questions: each with its errors. Schema errors skip the other checks.
        var reqs = c["requirements"]!.AsArray().Select((r, i) => new Entry(r, Label(r, $"REQ[{i}]"), ItemSchemaErrors("requirements", i, r))).ToList();
        var qs = c["open_questions"]!.AsArray().Select((q, i) => new Entry(q, Label(q, $"Q[{i}]"), ItemSchemaErrors("open_questions", i, q))).ToList();
        var validReqs = reqs.Where(e => e.Errors.Count == 0).ToList();
        var validQs = qs.Where(e => e.Errors.Count == 0).ToList();

        var fragments = fragmentList?.ToDictionary(f => f.Id);
        var lotFrags = fragmentList ?? [];
        var docFragIds = lotFrags.Where(f => f.Kind == "document").Select(f => f.Id).ToList();

        // --- 2. Normalizer ------------------------------------------------------------------------
        // Domain terms (N7): a badly declared one is ignored and stops protecting its word from C5.
        var domainTerms = RequirementChecks.DomainTerms(c["domain_terms"]!.AsArray(), fragments, warnings);

        void Fix(string owner, string what) => warnings.Add($"{owner} FIX {what}");

        foreach (var e in validReqs)
            RequirementChecks.NormalizeRequirement(e.Obj, e.Id, sistema, lang, fragments, lotFrags, Fix);
        foreach (var e in validQs)
            RequirementChecks.NormalizeTraces(e.Obj, e.Id, fragments, lotFrags, Fix);

        // Capabilities: fragments that are not in the lot, out.
        foreach (var cap in c["capabilities"]!.AsArray().Select(x => x!.AsObject()))
        {
            if (fragments is null)
                continue;
            var capFragments = StrList(cap, "fragments");
            var outside = capFragments.Where(f => !fragments.ContainsKey(f)).ToList();
            if (outside.Count > 0)
            {
                Fix(Str(cap, "id")!, $"fragmentos fuera del lote quitados: {string.Join(", ", outside)}");
                cap["fragments"] = StringArray(capFragments.Where(fragments.ContainsKey));
            }
        }

        // Links (with the model's ids; with repeated ids, the first one counts).
        var reqById = new Dictionary<string, Entry>();
        var qById = new Dictionary<string, Entry>();
        var capIds = new HashSet<string>();
        foreach (var e in validReqs)
        {
            if (!reqById.TryAdd(e.Id, e))
                warnings.Add($"{e.Id} C9 id de requisito repetido; las referencias apuntan al primero");
        }
        foreach (var e in validQs)
        {
            if (!qById.TryAdd(e.Id, e))
                warnings.Add($"{e.Id} C9 id de pregunta repetido; las referencias apuntan a la primera");
        }
        foreach (var cap in c["capabilities"]!.AsArray())
        {
            var id = Str(cap!, "id")!;
            if (!capIds.Add(id))
                warnings.Add($"{id} C9 id de funcionalidad repetido");
        }

        JsonArray KeepKnown(string owner, JsonObject item, string property, Func<string, bool> known, string what)
        {
            var refs = StrList(item, property);
            foreach (var x in refs.Where(x => !known(x)))
                Fix(owner, $"referencia a {what} inexistente {x} quitada");
            return StringArray(refs.Where(known).Distinct());
        }

        foreach (var e in validReqs)
        {
            var r = e.Obj;
            r["open_question_ids"] = KeepKnown(e.Id, r, "open_question_ids", qById.ContainsKey, "pregunta");
            r["derived_from"] = KeepKnown(e.Id, r, "derived_from", reqById.ContainsKey, "requisito");
            r["capability_ids"] = KeepKnown(e.Id, r, "capability_ids", capIds.Contains, "funcionalidad");
        }
        foreach (var e in validQs)
            e.Obj["related_requirements"] = KeepKnown(e.Id, e.Obj, "related_requirements", reqById.ContainsKey, "requisito");
        foreach (var e in validReqs) // complete the missing direction
        {
            foreach (var qid in StrList(e.Obj, "open_question_ids"))
            {
                var related = qById[qid].Obj["related_requirements"]!.AsArray();
                if (!related.Any(x => x!.GetValue<string>() == e.Id))
                {
                    related.Add(e.Id);
                    Fix(qid, $"vínculo con {e.Id} completado");
                }
            }
        }
        foreach (var e in validQs)
        {
            foreach (var rid in StrList(e.Obj, "related_requirements"))
            {
                var linked = reqById[rid].Obj["open_question_ids"]!.AsArray();
                if (!linked.Any(x => x!.GetValue<string>() == e.Id))
                {
                    linked.Add(e.Id);
                    Fix(rid, $"vínculo con {e.Id} completado");
                }
            }
        }

        // Whole discards of fragments that are cited (§3: a fragment that yields something is not
        // discarded whole). The quotes of the requirements and questions that pass the schema count.
        // The whole discard (quote null) is removed. A section with some cited fragment is replaced by
        // whole discards of its fragments that are not cited, with the section's reason (skipping the
        // fragments that already have a discard).
        var citedFragments = validReqs.Concat(validQs)
            .SelectMany(e => e.Obj["source_trace"]!.AsArray().Select(t => Str(t!, "fragment")!))
            .ToHashSet();
        var keptDiscards = new JsonArray();
        var discardList = c["discarded_statements"]!.AsArray();
        for (var i = 0; i < discardList.Count; i++)
        {
            var d = discardList[i]!;
            if (d["quote"] is null && citedFragments.Contains(Str(d, "fragment")!))
                Fix($"DISCARDED[{i}]", $"descarte entero de {Str(d, "fragment")} quitado: el fragmento se cita");
            else
                keptDiscards.Add(d.DeepClone());
        }
        c["discarded_statements"] = keptDiscards;
        if (fragments is not null)
        {
            var keptSections = new JsonArray();
            var sectionList = c["discarded_sections"]!.AsArray();
            for (var i = 0; i < sectionList.Count; i++)
            {
                var d = sectionList[i]!;
                var inside = docFragIds.Where(id => InSection(fragments[id].Section, Str(d, "section")!)).ToList();
                var citedInside = inside.Where(citedFragments.Contains).ToList();
                if (citedInside.Count == 0)
                {
                    keptSections.Add(d.DeepClone());
                    continue;
                }
                var already = keptDiscards.Select(x => Str(x!, "fragment")!).ToHashSet();
                var added = inside.Where(id => !citedFragments.Contains(id) && !already.Contains(id)).ToList();
                foreach (var id in added)
                    keptDiscards.Add(new JsonObject { ["fragment"] = id, ["quote"] = null, ["reason"] = Str(d, "reason"), ["note"] = null });
                var addedText = added.Count > 0 ? string.Join(", ", added) : "ningún fragmento";
                Fix($"SECTION[{i}]", $"sección '{Str(d, "section")}' sustituida por descartes enteros de {addedText}: se citan {string.Join(", ", citedInside)}");
            }
            c["discarded_sections"] = keptSections;
        }

        // --- 3. Checks of each element ---------------------------------------------------------
        var sections = c["discarded_sections"]!.AsArray().Select(d => Str(d!, "section")!).ToList();

        var traces = new RequirementChecks.TraceChecker(fragments);
        var found = new RequirementChecks.Findings();
        bool foundFragments = true, foundCapabilities = true;

        foreach (var e in validQs)
        {
            foreach (var t in e.Obj["source_trace"]!.AsArray().Select(x => x!.AsObject()))
                e.Errors.AddRange(traces.Check(t));
        }

        foreach (var e in validReqs)
        {
            e.Text = RequirementChecks.CheckRequirement(
                e.Obj, lang, domainTerms, fragments, traces, q => Str(qById[q].Obj, "type"), found, e.Errors, e.Warnings);
        }

        // --- 4. Rejections and renumbering ----------------------------------------------------
        var rejected = reqs.Where(e => e.Errors.Count > 0).Select(e => new RejectedElement(e.Id, "requirement", e.Errors))
            .Concat(qs.Where(e => e.Errors.Count > 0).Select(e => new RejectedElement(e.Id, "question", e.Errors)))
            .ToList();
        var acceptedReqs = reqs.Where(e => e.Errors.Count == 0).ToList();
        var acceptedQs = qs.Where(e => e.Errors.Count == 0).ToList();

        // First the new id of each accepted element; then the references are rewritten.
        // References use the model's ids: with a repeated id, they point to the first one.
        var counters = Types.ToDictionary(t => t, _ => 0);
        foreach (var e in acceptedReqs)
        {
            var type = Str(e.Obj, "type")!;
            counters[type]++;
            e.NewId = $"{type}-{counters[type]:000}";
        }
        for (var i = 0; i < acceptedQs.Count; i++)
            acceptedQs[i].NewId = $"Q-{i + 1:000}";
        var caps = c["capabilities"]!.AsArray().Select((cp, i) => (Item: cp!.AsObject(), Id: Str(cp!, "id")!, NewId: $"CAP-{i + 1:000}")).ToList();
        var newReqIds = new Dictionary<string, string>();
        var newQIds = new Dictionary<string, string>();
        var newCapIds = new Dictionary<string, string>();
        foreach (var e in acceptedReqs)
            newReqIds.TryAdd(e.Id, e.NewId!);
        foreach (var e in acceptedQs)
            newQIds.TryAdd(e.Id, e.NewId!);
        foreach (var cp in caps)
            newCapIds.TryAdd(cp.Id, cp.NewId);

        JsonArray Rewrite(string owner, JsonObject item, string property, Dictionary<string, string> mapping, string what)
        {
            var output = new List<string>();
            foreach (var x in StrList(item, property))
            {
                if (mapping.TryGetValue(x, out var newId))
                    output.Add(newId);
                else
                    Fix(owner, $"referencia a {what} rechazado {x} quitada");
            }
            return StringArray(output.Distinct());
        }

        var texts = new Dictionary<string, string?>();
        foreach (var e in acceptedReqs)
        {
            var r = e.Obj;
            r["id"] = e.NewId;
            r["open_question_ids"] = Rewrite(e.Id, r, "open_question_ids", newQIds, "pregunta");
            r["derived_from"] = Rewrite(e.Id, r, "derived_from", newReqIds, "requisito");
            r["capability_ids"] = StringArray(StrList(r, "capability_ids").Select(x => newCapIds[x]));
            texts[e.NewId!] = e.Text;
        }
        foreach (var e in acceptedQs)
        {
            e.Obj["id"] = e.NewId;
            e.Obj["related_requirements"] = Rewrite(e.Id, e.Obj, "related_requirements", newReqIds, "requisito");
        }
        foreach (var cp in caps)
            cp.Item["id"] = cp.NewId;
        var renumbered = acceptedReqs.Count(e => e.Id != e.NewId) + acceptedQs.Count(e => e.Id != e.NewId) + caps.Count(cp => cp.Id != cp.NewId);
        if (renumbered > 0)
            warnings.Add($"LOTE FIX {renumbered} ids renumerados (por tipo, en orden de aparición)");
        var finalIds = newReqIds.Concat(newQIds).Concat(newCapIds).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);

        // --- 5. Checks of the lot ------------------------------------------------------------
        lotErrors = [];
        var repairReasons = new List<string>();
        // C21 and C20: capabilities of the inventory
        foreach (var e in acceptedReqs)
        {
            if (FunctionalTypes.Contains(Str(e.Obj, "type")) && StrList(e.Obj, "capability_ids").Count == 0)
                e.Warnings.Add("C21 requisito funcional o de integración sin capability_ids");
        }
        foreach (var e in acceptedQs)
        {
            var type = Str(e.Obj, "type");
            if (StrList(e.Obj, "related_requirements").Count == 0 && type != "scope")
                e.Warnings.Add($"C11 pregunta {type} sin requisitos relacionados");
        }
        var covered = acceptedReqs.Where(e => FunctionalTypes.Contains(Str(e.Obj, "type")))
            .SelectMany(e => StrList(e.Obj, "capability_ids")).ToHashSet();
        foreach (var cp in caps.Where(cp => !covered.Contains(cp.NewId)))
        {
            var line = $"{cp.Id} C20 la funcionalidad «{Str(cp.Item, "name")}» no tiene ningún requisito RF o RI aceptado";
            warnings.Add(line);
            repairReasons.Add(line);
            foundCapabilities = false;
        }

        // Discards: a fragment outside the lot or a non-literal quote do not count (warning).
        var discardFrags = new HashSet<string>();
        var discardItems = c["discarded_statements"]!.AsArray();
        for (var i = 0; i < discardItems.Count; i++)
        {
            var fragmentId = Str(discardItems[i]!, "fragment")!;
            if (fragments is not null && !fragments.ContainsKey(fragmentId))
            {
                warnings.Add($"DISCARDED[{i}] C6 fragmento inexistente {fragmentId} (se ignora)");
                continue;
            }
            var quote = Str(discardItems[i]!, "quote");
            if (fragments is not null && quote is not null && !QuoteIn(quote, fragments[fragmentId].Text))
                warnings.Add($"DISCARDED[{i}] C6 cita no literal en {fragmentId}: «{quote}»");
            discardFrags.Add(fragmentId);
        }
        var sectionFrags = new HashSet<string>();
        for (var i = 0; i < sections.Count; i++)
        {
            var inside = docFragIds.Where(id => InSection(fragments![id].Section, sections[i])).ToList();
            if (fragments is not null && inside.Count == 0)
                warnings.Add($"SECTION[{i}] C10 la sección '{sections[i]}' no tiene fragmentos en el lote (se ignora)");
            sectionFrags.UnionWith(inside);
        }

        // C10: fragments treated by what the model wrote (accepted or rejected: the rejection counts on its own)
        var verdict = c["verdict"]!.AsObject();
        var unprocessed = StrList(verdict, "unprocessed_fragments");
        if (fragments is not null)
        {
            var treated = reqs.Concat(qs).SelectMany(e => TraceFragments(e.Item)).ToHashSet();
            treated.UnionWith(discardFrags);
            treated.UnionWith(sectionFrags);
            var untreated = docFragIds.Where(id => !treated.Contains(id)).ToList();
            if (untreated.Count > 0)
            {
                foundFragments = false;
                var line = $"C10 fragmentos sin tratar: {string.Join(", ", untreated)}";
                if (untreated.Count > MaxUntreatedShare * docFragIds.Count)
                    lotErrors.Add($"LOTE {line}");
                else
                    warnings.Add($"LOTE {line}");
            }
            if (unprocessed.Count > 0)
                warnings.Add($"LOTE C10 fragmentos declarados sin procesar: {string.Join(", ", unprocessed.Order(StringComparer.Ordinal))}");
        }

        // Verdict of the catalog
        var status = Str(verdict, "status");
        if (status == "failed")
            lotErrors.Add("LOTE VERDICT el catálogo declara status failed");
        if (status == "complete" && unprocessed.Count > 0)
            warnings.Add("LOTE VERDICT status complete con unprocessed_fragments no vacío");
        if (status == "partial" && unprocessed.Count == 0)
            warnings.Add("LOTE VERDICT status partial sin unprocessed_fragments");
        var selfCheck = verdict["self_check"]!.AsObject();
        var declaredVsFound = new (string Key, bool Found)[]
        {
            ("quotes_are_literal", traces.AllLiteral), ("figures_from_source", found.Figures),
            ("all_fragments_accounted", foundFragments), ("all_capabilities_covered", foundCapabilities),
            ("no_vague_terms", found.Vague), ("ears_only_shall", found.EarsShall), ("decision_tree_consistent", found.Tree),
        };
        var mismatches = declaredVsFound.Where(x => selfCheck[x.Key]!.GetValue<bool>() && !x.Found).Select(x => x.Key).ToList();
        warnings.AddRange(mismatches.Select(k => $"LOTE SELF_CHECK el modelo declara {k}=true y el validador encuentra fallos"));

        // Warnings of each element, with its original id, after those of the normalizer.
        foreach (var e in reqs.Concat(qs))
            warnings.AddRange(e.Warnings.Select(w => $"{e.Id} {w}"));

        // --- 6. Decision ---------------------------------------------------------------------
        var written = reqs.Count;
        var rejectedReqs = rejected.Where(x => x.Element == "requirement").ToList();
        if (written > 0 && rejectedReqs.Count > MaxRejectedShare * written)
            repairReasons.InsertRange(0, rejectedReqs.SelectMany(x => x.Errors.Select(err => $"{x.Id} {err}")));
        repairReasons.InsertRange(0, lotErrors);

        // --- Metrics -------------------------------------------------------------------------
        var categories = reqs.Select(e => e.Errors.Concat(e.Warnings).Select(m => Category.GetValueOrDefault(m.Split(' ')[0], "otros")).ToHashSet()).ToList();
        double? Share(string? category)
        {
            if (written == 0)
                return null;
            var ok = category is null ? written - rejectedReqs.Count : categories.Count(cats => !cats.Contains(category));
            return Math.Round((double)ok / written, 3);
        }

        var acceptedFrags = acceptedReqs.Concat(acceptedQs).SelectMany(e => TraceFragments(e.Item)).ToHashSet();
        acceptedFrags.UnionWith(discardFrags);
        acceptedFrags.UnionWith(sectionFrags);
        var n = acceptedReqs.Count;
        var criteriaLabels = acceptedReqs.SelectMany(e => StrList(e.Obj, "acceptance_criteria")).Select(crit => SplitLabel(crit).Label).ToList();
        var byLabel = Labels.ToDictionary(l => l, l => criteriaLabels.Count(x => x == l));
        byLabel["sin_etiqueta"] = criteriaLabels.Count(x => x is null);

        var metrics = new CatalogMetrics(
            Requirements: n,
            RequirementsWritten: written,
            RequirementsRejected: rejectedReqs.Count,
            QuestionsRejected: rejected.Count - rejectedReqs.Count,
            ByType: Types.ToDictionary(t => t, t => acceptedReqs.Count(e => Str(e.Obj, "type") == t)),
            SyntacticConformance: Share("sintaxis"), // stage 1 benchmark: >= 0.90
            WritingQuality: Share("redaccion"),
            DecisionConsistency: Share("decision"),
            Traceability: Share("trazabilidad"),
            FigureFidelity: Share("fidelidad"),
            RequirementsWithoutErrors: Share(null),
            LiteralQuotes: fragments is not null ? $"{traces.Ok}/{traces.Total}" : "no comprobado",
            FragmentCoverage: fragments is not null ? $"{docFragIds.Count(acceptedFrags.Contains)}/{docFragIds.Count}" : "no comprobado",
            MasterShare: n > 0 ? Math.Round((double)acceptedReqs.Count(e => Str(e.Obj, "notation") == "MASTeR") / n, 3) : null,
            Branches: new[] { "D1", "D2", "D3", "D4", "D5" }.ToDictionary(b => b, b => acceptedReqs.Count(e => Str(e.Obj, "decision_branch") == b)),
            OpenQuestions: acceptedQs.Count,
            DomainTerms: domainTerms.Count,
            Capabilities: caps.Count,
            CapabilityCoverage: $"{caps.Count(cp => covered.Contains(cp.NewId))}/{caps.Count}",
            Criteria: criteriaLabels.Count,
            CriteriaPerRequirement: n > 0 ? Math.Round((double)criteriaLabels.Count / n, 2) : null,
            CriteriaByLabel: byLabel,
            Fixes: warnings.Count(w => w.Split(' ') is { Length: > 1 } parts && parts[1] == "FIX"),
            SelfCheckMatches: mismatches.Count == 0);

        var catalog = new JsonObject
        {
            ["schema_version"] = c["schema_version"]!.DeepClone(),
            ["system"] = c["system"]!.DeepClone(),
            ["language"] = c["language"]!.DeepClone(),
            ["capabilities"] = new JsonArray(caps.Select(cp => (JsonNode)cp.Item.DeepClone()).ToArray()),
            ["requirements"] = new JsonArray(acceptedReqs.Select(e => (JsonNode)e.Obj.DeepClone()).ToArray()),
            ["open_questions"] = new JsonArray(acceptedQs.Select(e => (JsonNode)e.Obj.DeepClone()).ToArray()),
            ["discarded_statements"] = c["discarded_statements"]!.DeepClone(),
            ["discarded_sections"] = c["discarded_sections"]!.DeepClone(),
            ["domain_terms"] = c["domain_terms"]!.DeepClone(),
            ["verdict"] = c["verdict"]!.DeepClone(),
        };
        var valid = lotErrors.Count == 0;
        return new CatalogValidationReport(
            valid, repairReasons.Count > 0, repairReasons, lotErrors, rejected, warnings, metrics, finalIds,
            valid ? catalog.ToJsonString(Indented) : null,
            valid ? texts : new Dictionary<string, string?>());
    }

    private static List<string> ItemSchemaErrors(string list, int index, JsonNode? item) =>
        SchemaErrors(ItemValidators[list], item, $"{list}/{index}");

    // ("DATA", "text") if the criterion starts with a valid label, or (null, criterion).
    public static (string? Label, string Text) SplitLabel(string criterion) => RequirementChecks.SplitLabel(criterion);

    // NFC, typographic quotes and dashes unified, spaces collapsed.
    public static string Norm(string s) => RequirementChecks.Norm(s);

    // Copy of the catalog with the rendered `text` after `slots` in each requirement.
    public static string Enrich(string catalogJson, IReadOnlyDictionary<string, string?> texts) =>
        CatalogJson.Enrich(catalogJson, texts);
}
