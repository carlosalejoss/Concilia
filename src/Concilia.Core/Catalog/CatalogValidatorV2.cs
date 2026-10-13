using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LateApexEarlySpeed.Json.Schema;
using static Concilia.Core.Catalog.CatalogJson;
using static Concilia.Core.Catalog.RequirementChecks;

namespace Concilia.Core.Catalog;

// Quality figures of the catalog of one lot (v2), with the same names as the reference report.
public record CatalogV2Metrics(
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
    [property: JsonPropertyName("fragmentos_citados")] string CitedFragments,
    [property: JsonPropertyName("proporcion_master")] double? MasterShare,
    [property: JsonPropertyName("ramas")] IReadOnlyDictionary<string, int> Branches,
    [property: JsonPropertyName("preguntas_abiertas")] int OpenQuestions,
    [property: JsonPropertyName("terminos_dominio")] int DomainTerms,
    [property: JsonPropertyName("funcionalidades")] int Capabilities,
    [property: JsonPropertyName("funcionalidades_objetadas")] int ObjectedCapabilities,
    [property: JsonPropertyName("cobertura_funcionalidades")] string CapabilityCoverage,
    [property: JsonPropertyName("funcionalidades_sin_error")] int CapabilitiesWithoutError,
    [property: JsonPropertyName("criterios")] int Criteria,
    [property: JsonPropertyName("criterios_por_requisito")] double? CriteriaPerRequirement,
    [property: JsonPropertyName("criterios_por_etiqueta")] IReadOnlyDictionary<string, int> CriteriaByLabel,
    [property: JsonPropertyName("correcciones")] int Fixes);

// Result of validating the catalog of one lot (catalog v2). Like the v1.3
// report, plus the valid objections and the capabilities of the lot (functional
// or integration) without any [ERROR] criterion, the list that replaces the
// "what if it fails?" questions (N4).
public record CatalogV2ValidationReport(
    [property: JsonPropertyName("valid")] bool Valid,
    [property: JsonPropertyName("needs_repair")] bool NeedsRepair,
    [property: JsonPropertyName("repair_reasons")] IReadOnlyList<string> RepairReasons,
    [property: JsonPropertyName("errors")] IReadOnlyList<string> Errors,
    [property: JsonPropertyName("rejected")] IReadOnlyList<RejectedElement> Rejected,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("metrics")] CatalogV2Metrics? Metrics,
    [property: JsonPropertyName("ids")] IReadOnlyDictionary<string, string> Ids,
    [property: JsonPropertyName("objections")] IReadOnlyList<string> Objections, // capability ids
    [property: JsonPropertyName("capabilities_without_error")] IReadOnlyList<string> CapabilitiesWithoutError,
    [property: JsonIgnore] string? Catalog,
    [property: JsonIgnore] IReadOnlyDictionary<string, string?> Texts);

// Deterministic validator of the catalog v2 (phase 2) that model A or B writes
// for a lot of capabilities of the inventory. Port of check() in
// prompts/generacion_requisitos/validar_catalogo.py (v2), the reference: for
// the same files it must give the same outcome (see the fixtures in
// tests/fixtures/catalog-v2-mutations). Keep both in sync. It lives next to the
// v1.3 CatalogValidator until the platform runs the v2 (step 38). Steps:
// 1. Schema: root -> error of the lot; requirement or question -> rejected;
//    objection or domain term -> ignored with a warning.
// 2. Normalizer (FIX): the same as v1.3 for each requirement, plus
//    capability_ids outside the lot removed and, in a lot of one capability,
//    an empty capability_ids completed. Malformed objections are ignored.
// 3. Checks of each element (RequirementChecks), plus C21, C24 and C25.
// 4. Renumbering by type and Q-; CAP- and QI- of the inventory do not change.
// 5. Checks of the lot: C20 by kind, C23 and the verdict.
// 6. Decision: repair on lot errors, more than 20 % rejected or a C20.
public static class CatalogValidatorV2
{
    private const double MaxRejectedShare = 0.20; // partial acceptance: more rejected than this -> repair

    // By kind of the capability: the types that cover it (C20) and the types it admits (C24).
    // The quality demanded of one function is an RNF of that function; imposed regulations are
    // an RNF of a constraint.
    private static readonly Dictionary<string, string[]> RequiredTypes = new()
    {
        ["functional"] = ["RF", "RI"], ["integration"] = ["RF", "RI"], ["quality"] = ["RNF"], ["constraint"] = ["RA", "RT", "RNF"],
    };
    private static readonly Dictionary<string, string[]> AllowedTypes = new()
    {
        ["functional"] = ["RF", "RI", "RNF"], ["integration"] = ["RF", "RI", "RNF"], ["quality"] = ["RNF"], ["constraint"] = ["RA", "RT", "RNF"],
    };

    // Category of each code, for the per-requirement metrics (errors and warnings count).
    private static readonly Dictionary<string, string> Category = new()
    {
        ["SCHEMA"] = "sintaxis", ["C2"] = "sintaxis", ["C14"] = "sintaxis",
        ["C5"] = "redaccion", ["C15"] = "redaccion", ["C19"] = "redaccion", ["C22"] = "redaccion",
        ["C7"] = "decision", ["C12"] = "decision",
        ["C6"] = "trazabilidad", ["C13"] = "trazabilidad", ["C16"] = "trazabilidad",
        ["N3"] = "referencias", ["C21"] = "referencias", ["C24"] = "referencias", ["C25"] = "referencias",
        ["N2"] = "fidelidad",
    };

    private static readonly (string Key, string Definition, string Prefix)[] ItemLists =
    [
        ("requirements", "requirement", "REQ"),
        ("open_questions", "open_question", "Q"),
        ("capability_objections", "capability_objection", "OBJECTION"),
        ("domain_terms", "domain_term", "DOMAIN"),
    ];

    private static readonly string SchemaText = ReadResource("requirements_v2.schema.json");
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

    // catalogJson: the file written by the model. inventoryJson: the normalized
    // inventory of phase 1. lot: the ids of the lot's capabilities (all of the
    // inventory if null). fragments: all the fragments (documents and doubts);
    // those of the lot are the ones of its capabilities, plus the doubts.
    public static CatalogV2ValidationReport Validate(
        string catalogJson, string systemName, IReadOnlyList<Fragment>? fragments, string inventoryJson,
        IReadOnlyList<string>? lot = null)
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
        return Check(root, systemName, fragments, JsonNode.Parse(inventoryJson)!.AsObject(), lot);
    }

    private static CatalogV2ValidationReport LotFailure(List<string> errors) =>
        new(false, true, errors, errors, [], [], null, new Dictionary<string, string>(), [], [], null, new Dictionary<string, string?>());

    private static CatalogV2ValidationReport Check(
        JsonNode? cat, string sistema, IReadOnlyList<Fragment>? allFragments, JsonObject inventory, IReadOnlyList<string>? lot)
    {
        var warnings = new List<string>();

        // --- 1. Schema, split by levels -------------------------------------------------------
        var lotErrors = SchemaErrors(RootValidator, WithoutItems(cat, ItemLists.Select(l => l.Key))).Select(m => $"LOTE {m}").ToList();
        if (lotErrors.Count > 0)
            return LotFailure(lotErrors);

        var c = cat!.DeepClone().AsObject();
        var lang = Str(c, "language")!;

        foreach (var (key, _, prefix) in ItemLists.Skip(2))
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

        var reqs = c["requirements"]!.AsArray().Select((r, i) => new Entry(r, Label(r, $"REQ[{i}]"), SchemaErrors(ItemValidators["requirements"], r, $"requirements/{i}"))).ToList();
        var qs = c["open_questions"]!.AsArray().Select((q, i) => new Entry(q, Label(q, $"Q[{i}]"), SchemaErrors(ItemValidators["open_questions"], q, $"open_questions/{i}"))).ToList();
        var validReqs = reqs.Where(e => e.Errors.Count == 0).ToList();
        var validQs = qs.Where(e => e.Errors.Count == 0).ToList();

        // The lot: its capabilities (from the inventory) and its fragments (those of the capabilities and the doubts).
        var invCaps = inventory["capabilities"]!.AsArray().Select(x => x!.AsObject()).ToDictionary(x => Str(x, "id")!);
        var invQs = inventory["open_questions"]!.AsArray().Select(x => x!.AsObject()).ToDictionary(x => Str(x, "id")!);
        var lotIds = lot?.ToList() ?? invCaps.Keys.ToList();
        var lotCaps = lotIds.Select(x => invCaps[x]).ToList();
        var lotFragIds = lotCaps.SelectMany(cp => StrList(cp, "fragments")).ToHashSet();
        var lotFragList = allFragments?.Where(f => lotFragIds.Contains(f.Id) || f.Kind == "clarification").ToList();
        var fragments = lotFragList?.ToDictionary(f => f.Id);
        var docFragIds = (lotFragList ?? []).Where(f => f.Kind == "document").Select(f => f.Id).ToList();

        // --- 2. Normalizer ------------------------------------------------------------------------
        var domainTerms = DomainTerms(c["domain_terms"]!.AsArray(), fragments, warnings);

        void Fix(string owner, string what) => warnings.Add($"{owner} FIX {what}");

        foreach (var e in validReqs)
            NormalizeRequirement(e.Obj, e.Id, sistema, lang, fragments, lotFragList ?? [], Fix);
        foreach (var e in validQs)
            NormalizeTraces(e.Obj, e.Id, fragments, lotFragList ?? [], Fix);

        // Objections: only those of a capability of the lot count; a duplicate must point to another
        // capability of the inventory. The rest are ignored with a warning.
        var objections = new JsonArray();
        var objected = new HashSet<string>();
        var objectionList = c["capability_objections"]!.AsArray();
        for (var i = 0; i < objectionList.Count; i++)
        {
            var o = objectionList[i]!;
            var capId = Str(o, "capability_id")!;
            var reason = Str(o, "reason");
            var duplicateOf = Str(o, "duplicate_of");
            string? problem = null;
            if (!lotIds.Contains(capId))
                problem = $"la funcionalidad {capId} no es del lote";
            else if (reason == "duplicate" && !invCaps.ContainsKey(duplicateOf!))
                problem = $"duplicate_of {duplicateOf} no está en el inventario";
            else if (reason == "duplicate" && duplicateOf == capId)
                problem = "duplicate_of es la misma funcionalidad";
            if (problem is not null)
            {
                warnings.Add($"OBJECTION[{i}] OBJECTION {problem} (se ignora)");
            }
            else
            {
                objections.Add(o.DeepClone());
                objected.Add(capId);
            }
        }

        // Links (with the model's ids; with repeated ids, the first one counts).
        var reqById = new Dictionary<string, Entry>();
        var qById = new Dictionary<string, Entry>();
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
            // The questions of the inventory (QI-) are valid too: the doubt is already open.
            r["open_question_ids"] = KeepKnown(e.Id, r, "open_question_ids", x => qById.ContainsKey(x) || invQs.ContainsKey(x), "pregunta");
            r["derived_from"] = KeepKnown(e.Id, r, "derived_from", reqById.ContainsKey, "requisito");
            // C21: only capabilities of the lot; with a single one in the lot, a requirement without any is of it.
            var capIds = StrList(r, "capability_ids");
            var outside = capIds.Where(x => !lotIds.Contains(x)).ToList();
            if (outside.Count > 0)
                Fix(e.Id, $"funcionalidades que no son del lote quitadas: {string.Join(", ", outside)}");
            var inLot = capIds.Where(lotIds.Contains).Distinct().ToList();
            if (inLot.Count == 0 && lotIds.Count == 1)
            {
                Fix(e.Id, $"capability_ids completado con {lotIds[0]}, única funcionalidad del lote");
                inLot = [lotIds[0]];
            }
            r["capability_ids"] = StringArray(inLot);
        }
        foreach (var e in validQs)
            e.Obj["related_requirements"] = KeepKnown(e.Id, e.Obj, "related_requirements", reqById.ContainsKey, "requisito");
        foreach (var e in validReqs) // complete the missing direction (the merge completes the QI- ones)
        {
            foreach (var qid in StrList(e.Obj, "open_question_ids").Where(qById.ContainsKey))
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

        // --- 3. Checks of each element ---------------------------------------------------------
        var traces = new TraceChecker(fragments);
        var found = new Findings(); // unused in v2 (no self-check), required by the shared checks

        string? QuestionType(string qid) =>
            qById.TryGetValue(qid, out var q) ? Str(q.Obj, "type") : Str(invQs[qid], "type");

        foreach (var e in validQs)
        {
            foreach (var t in e.Obj["source_trace"]!.AsArray().Select(x => x!.AsObject()))
                e.Errors.AddRange(traces.Check(t));
        }

        foreach (var e in validReqs)
        {
            e.Text = CheckRequirement(e.Obj, lang, domainTerms, fragments, traces, QuestionType, found, e.Errors, e.Warnings);
            // C21, C24 and C25: the capability of the requirement
            var type = Str(e.Obj, "type")!;
            var capIds = StrList(e.Obj, "capability_ids");
            if (capIds.Count == 0)
                e.Warnings.Add("C21 requisito sin capability_ids");
            foreach (var x in capIds)
            {
                var kind = Str(invCaps[x], "kind")!;
                if (!AllowedTypes[kind].Contains(type))
                    e.Warnings.Add($"C24 el tipo {type} no lo admite {x} ({kind}: {string.Join(", ", AllowedTypes[kind])})");
                if (objected.Contains(x))
                    e.Warnings.Add($"C25 requisito de la funcionalidad objetada {x}");
            }
        }

        // --- 4. Rejections and renumbering ----------------------------------------------------
        var rejected = reqs.Where(e => e.Errors.Count > 0).Select(e => new RejectedElement(e.Id, "requirement", e.Errors))
            .Concat(qs.Where(e => e.Errors.Count > 0).Select(e => new RejectedElement(e.Id, "question", e.Errors)))
            .ToList();
        var acceptedReqs = reqs.Where(e => e.Errors.Count == 0).ToList();
        var acceptedQs = qs.Where(e => e.Errors.Count == 0).ToList();

        var counters = Types.ToDictionary(t => t, _ => 0);
        foreach (var e in acceptedReqs)
        {
            var type = Str(e.Obj, "type")!;
            counters[type]++;
            e.NewId = $"{type}-{counters[type]:000}";
        }
        for (var i = 0; i < acceptedQs.Count; i++)
            acceptedQs[i].NewId = $"Q-{i + 1:000}";
        var newReqIds = new Dictionary<string, string>();
        var newQIds = new Dictionary<string, string>();
        foreach (var e in acceptedReqs)
            newReqIds.TryAdd(e.Id, e.NewId!);
        foreach (var e in acceptedQs)
            newQIds.TryAdd(e.Id, e.NewId!);
        // The questions of the inventory keep their id.
        var qMapping = invQs.Keys.ToDictionary(x => x, x => x);
        foreach (var (oldId, newId) in newQIds)
            qMapping[oldId] = newId;

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
            r["open_question_ids"] = Rewrite(e.Id, r, "open_question_ids", qMapping, "pregunta");
            r["derived_from"] = Rewrite(e.Id, r, "derived_from", newReqIds, "requisito");
            texts[e.NewId!] = e.Text;
        }
        foreach (var e in acceptedQs)
        {
            e.Obj["id"] = e.NewId;
            e.Obj["related_requirements"] = Rewrite(e.Id, e.Obj, "related_requirements", newReqIds, "requisito");
        }
        var renumbered = acceptedReqs.Count(e => e.Id != e.NewId) + acceptedQs.Count(e => e.Id != e.NewId);
        if (renumbered > 0)
            warnings.Add($"LOTE FIX {renumbered} ids renumerados (por tipo, en orden de aparición)");
        var finalIds = newReqIds.Concat(newQIds).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);

        // --- 5. Checks of the lot ------------------------------------------------------------
        lotErrors = [];
        var repairReasons = new List<string>();
        foreach (var e in acceptedQs)
        {
            var type = Str(e.Obj, "type");
            if (StrList(e.Obj, "related_requirements").Count == 0 && type != "scope")
                e.Warnings.Add($"C11 pregunta {type} sin requisitos relacionados");
        }
        // C20: each capability of the lot without objection has an accepted requirement of the type its kind needs.
        var covered = new HashSet<string>();
        foreach (var cp in lotCaps)
        {
            var id = Str(cp, "id")!;
            if (objected.Contains(id))
                continue;
            var needed = RequiredTypes[Str(cp, "kind")!];
            if (acceptedReqs.Any(e => StrList(e.Obj, "capability_ids").Contains(id) && needed.Contains(Str(e.Obj, "type"))))
            {
                covered.Add(id);
                continue;
            }
            var line = $"{id} C20 la funcionalidad «{Str(cp, "name")}» ({Str(cp, "kind")}) no tiene ningún requisito {string.Join(" o ", needed)} aceptado";
            warnings.Add(line);
            repairReasons.Add(line);
        }

        // C23: fragments of the lot that no requirement or question cites (rejected ones too: the
        // model treated them). It measures the detail lost without forcing to cite everything.
        var citedFrags = reqs.Concat(qs).SelectMany(e => TraceFragments(e.Item)).ToHashSet();
        if (fragments is not null)
        {
            var uncited = docFragIds.Where(id => !citedFrags.Contains(id)).ToList();
            if (uncited.Count > 0)
                warnings.Add($"LOTE C23 fragmentos del lote que no cita ningún requisito ni pregunta: {string.Join(", ", uncited)}");
        }

        // Verdict of the catalog
        var verdict = c["verdict"]!.AsObject();
        var status = Str(verdict, "status");
        var uncovered = StrList(verdict, "uncovered_capabilities");
        if (status == "failed")
            lotErrors.Add("LOTE VERDICT el catálogo declara status failed");
        if (status == "complete" && uncovered.Count > 0)
            warnings.Add("LOTE VERDICT status complete con uncovered_capabilities no vacío");
        if (status == "partial" && uncovered.Count == 0)
            warnings.Add("LOTE VERDICT status partial sin uncovered_capabilities");

        foreach (var e in reqs.Concat(qs))
            warnings.AddRange(e.Warnings.Select(w => $"{e.Id} {w}"));

        // --- 6. Decision ---------------------------------------------------------------------
        var written = reqs.Count;
        var rejectedReqs = rejected.Where(x => x.Element == "requirement").ToList();
        if (written > 0 && rejectedReqs.Count > MaxRejectedShare * written)
            repairReasons.InsertRange(0, rejectedReqs.SelectMany(x => x.Errors.Select(err => $"{x.Id} {err}")));
        repairReasons.InsertRange(0, lotErrors);

        // Report that replaces N4: functional or integration capabilities without any [ERROR] criterion.
        var withError = acceptedReqs
            .Where(e => StrList(e.Obj, "acceptance_criteria").Any(crit => SplitLabel(crit).Label == "ERROR"))
            .SelectMany(e => StrList(e.Obj, "capability_ids")).ToHashSet();
        var withoutError = lotCaps
            .Where(cp => Str(cp, "kind") is "functional" or "integration")
            .Select(cp => Str(cp, "id")!)
            .Where(id => !objected.Contains(id) && !withError.Contains(id))
            .ToList();

        // --- Metrics -------------------------------------------------------------------------
        var categories = reqs.Select(e => e.Errors.Concat(e.Warnings).Select(m => Category.GetValueOrDefault(m.Split(' ')[0], "otros")).ToHashSet()).ToList();
        double? Share(string? category)
        {
            if (written == 0)
                return null;
            var ok = category is null ? written - rejectedReqs.Count : categories.Count(cats => !cats.Contains(category));
            return Math.Round((double)ok / written, 3);
        }

        var n = acceptedReqs.Count;
        var criteriaLabels = acceptedReqs.SelectMany(e => StrList(e.Obj, "acceptance_criteria")).Select(crit => SplitLabel(crit).Label).ToList();
        var byLabel = Labels.ToDictionary(l => l, l => criteriaLabels.Count(x => x == l));
        byLabel["sin_etiqueta"] = criteriaLabels.Count(x => x is null);

        var metrics = new CatalogV2Metrics(
            Requirements: n,
            RequirementsWritten: written,
            RequirementsRejected: rejectedReqs.Count,
            QuestionsRejected: rejected.Count - rejectedReqs.Count,
            ByType: Types.ToDictionary(t => t, t => acceptedReqs.Count(e => Str(e.Obj, "type") == t)),
            SyntacticConformance: Share("sintaxis"),
            WritingQuality: Share("redaccion"),
            DecisionConsistency: Share("decision"),
            Traceability: Share("trazabilidad"),
            FigureFidelity: Share("fidelidad"),
            RequirementsWithoutErrors: Share(null),
            LiteralQuotes: fragments is not null ? $"{traces.Ok}/{traces.Total}" : "no comprobado",
            CitedFragments: fragments is not null ? $"{docFragIds.Count(citedFrags.Contains)}/{docFragIds.Count}" : "no comprobado",
            MasterShare: n > 0 ? Math.Round((double)acceptedReqs.Count(e => Str(e.Obj, "notation") == "MASTeR") / n, 3) : null,
            Branches: new[] { "D1", "D2", "D3", "D4", "D5" }.ToDictionary(b => b, b => acceptedReqs.Count(e => Str(e.Obj, "decision_branch") == b)),
            OpenQuestions: acceptedQs.Count,
            DomainTerms: domainTerms.Count,
            Capabilities: lotCaps.Count,
            ObjectedCapabilities: objected.Count,
            CapabilityCoverage: $"{covered.Count}/{lotCaps.Count - objected.Count}",
            CapabilitiesWithoutError: withoutError.Count,
            Criteria: criteriaLabels.Count,
            CriteriaPerRequirement: n > 0 ? Math.Round((double)criteriaLabels.Count / n, 2) : null,
            CriteriaByLabel: byLabel,
            Fixes: warnings.Count(w => w.Split(' ') is { Length: > 1 } parts && parts[1] == "FIX"));

        var catalog = new JsonObject
        {
            ["schema_version"] = c["schema_version"]!.DeepClone(),
            ["system"] = c["system"]!.DeepClone(),
            ["language"] = c["language"]!.DeepClone(),
            ["capability_objections"] = objections,
            ["requirements"] = new JsonArray(acceptedReqs.Select(e => (JsonNode)e.Obj.DeepClone()).ToArray()),
            ["open_questions"] = new JsonArray(acceptedQs.Select(e => (JsonNode)e.Obj.DeepClone()).ToArray()),
            ["domain_terms"] = c["domain_terms"]!.DeepClone(),
            ["verdict"] = verdict.DeepClone(),
        };
        var valid = lotErrors.Count == 0;
        return new CatalogV2ValidationReport(
            valid, repairReasons.Count > 0, repairReasons, lotErrors, rejected, warnings, metrics, finalIds,
            objections.Select(o => Str(o!, "capability_id")!).ToList(), withoutError,
            valid ? catalog.ToJsonString(Indented) : null,
            valid ? texts : new Dictionary<string, string?>());
    }

    // Copy of the catalog with the rendered `text` after `slots` in each requirement.
    public static string Enrich(string catalogJson, IReadOnlyDictionary<string, string?> texts) =>
        CatalogJson.Enrich(catalogJson, texts);
}
