using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LateApexEarlySpeed.Json.Schema;
using LateApexEarlySpeed.Json.Schema.Common;

namespace ReqArbiter.Core.Catalog;

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
public static class CatalogValidator
{
    private const RegexOptions IgnoreCase = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    // Keyword words that must not appear in CAPITALS inside a slot (R-b).
    private static readonly Dictionary<string, HashSet<string>> KeywordWords = new()
    {
        ["es"] = ["CUANDO", "MIENTRAS", "SI", "ENTONCES", "DONDE", "DEBERÁ", "DEBERÍA", "PODRÁ", "PROPORCIONAR",
                  "CAPACIDAD", "CAPAZ", "CUANTO", "SER"],
        ["en"] = ["WHEN", "WHILE", "IF", "THEN", "WHERE", "SHALL", "SHOULD", "MAY", "PROVIDE", "ABILITY", "ABLE",
                  "SOON", "LONG", "BE"],
    };

    // C5: ambiguous-language categories of ISO/IEC/IEEE 29148 (project lists).
    private static readonly Dictionary<string, (string Category, string[] Patterns)[]> Vague = new()
    {
        ["es"] =
        [
            ("subjetivo", [@"f[aá]cil(es|mente)?", @"sencill[oa]s?", @"sencillamente", @"intuitiv[oa]s?", @"amigables?",
                           @"c[oó]mod[oa]s?", @"agradables?", @"atractiv[oa]s?", @"modern[oa]s?", @"user-friendly"]),
            ("cualidad_sin_cifra", [@"r[aá]pid[oa]s?", @"r[aá]pidamente", @"lent[oa]s?", @"eficientes?", @"eficientemente",
                                    @"eficaz(mente)?", @"eficaces", @"robust[oa]s?", @"flexibles?", @"segur[oa]s?",
                                    @"escalables?", @"[oó]ptim[oa]s?", @"adecuad[oa]s?", @"adecuadamente",
                                    @"apropiad[oa]s?", @"suficientes?", @"razonables?"]),
            ("superlativo_comparativo", [@"mejor(es)?", @"mejorad[oa]s?", @"lo antes posible", @"m[aá]ximo posible",
                                         @"m[ií]nimo posible"]),
            ("ambiguo", [@"aproximadamente", @"normalmente", @"generalmente", @"habitualmente", @"casi siempre",
                         @"significativ[oa]s?", @"significativamente", @"considerables?", @"considerablemente"]),
            ("abierto", [@"etc\.?", @"y/o", @"entre otr[oa]s", @"tales como"]),
            ("escape", [@"si es posible", @"en la medida de lo posible", @"cuando proceda", @"si procede",
                        @"cuando sea necesario", @"seg[uú]n sea necesario", @"seg[uú]n corresponda", @"si aplica"]),
            ("pronombre_vago", [@"lo anterior", @"lo siguiente", @"lo mismo", @"esto", @"ello"]),
        ],
        ["en"] =
        [
            ("subjetivo", [@"easy", @"easily", @"simple", @"intuitive", @"user-friendly", @"friendly", @"comfortable",
                           @"attractive", @"modern"]),
            ("cualidad_sin_cifra", [@"fast", @"quick(ly)?", @"slow(ly)?", @"efficient(ly)?", @"effective(ly)?", @"robust",
                                    @"flexible", @"secure(ly)?", @"scalable", @"optimal", @"adequate(ly)?",
                                    @"appropriate(ly)?", @"sufficient(ly)?", @"reasonable"]),
            ("superlativo_comparativo", [@"the best", @"better", @"improved", @"as soon as possible", @"as much as possible"]),
            ("ambiguo", [@"approximately", @"normally", @"generally", @"usually", @"almost always",
                         @"significant(ly)?", @"considerable", @"considerably"]),
            ("abierto", [@"etc\.?", @"and/or", @"among others", @"such as", @"but not limited to"]),
            ("escape", [@"if possible", @"as far as possible", @"where appropriate", @"as appropriate", @"if applicable",
                        @"when necessary", @"as needed"]),
            ("pronombre_vago", [@"the above", @"the aforementioned", @"the former", @"the latter"]),
        ],
    };

    // C13: explicit modal signals of §7 by obligation. Both languages are
    // accepted: the evidence is copied from the source, whatever `language` is.
    private static readonly Dictionary<string, string[]> ObligationSignals = new()
    {
        ["SHALL"] = [@"deb(e|en|erá|erán)", @"tienen? que", @"han? de", @"es obligatori[oa]", @"obligatoriamente",
                     @"se requiere", @"requerid[oa]s?", @"necesariamente",
                     @"must", @"shall", @"required", @"(is|are) required to", @"(has|have) to", @"mandatory"],
        ["SHOULD"] = [@"debería(n)?", @"sería deseable", @"deseable", @"preferiblemente", @"preferentemente", @"idealmente",
                      @"se recomienda", @"recomendable",
                      @"should", @"preferably", @"recommended", @"desirable", @"ideally"],
        ["MAY"] = [@"opcionalmente", @"de forma opcional", @"opcional(es)?", @"si es posible", @"podría(n)?",
                   @"may", @"could", @"optionally", @"optional", @"if possible", @"nice to have"],
    };

    // U7 / C22: umbrella verbs, at the start of the response or after
    // "permitir a <actor>" / "allow <actor> to" (an actor of up to four words).
    private static readonly Dictionary<string, Regex> UmbrellaRe = new()
    {
        ["es"] = new(@"^(?:permitir\s+(?:\S+\s+){0,4}?)?(gestionar|administrar|mantener|manejar)\b", IgnoreCase),
        ["en"] = new(@"^(?:allow\s+(?:\S+\s+){0,4}?to\s+)?(manage|administer|maintain|handle)\b", IgnoreCase),
    };
    private const int UmbrellaMinOperations = 5; // U7: an umbrella verb only with more than four operations

    public static readonly string[] Types = ["RF", "RI", "RNF", "RA", "RT"];
    private static readonly string[] FunctionalTypes = ["RF", "RI"]; // explain a capability of the inventory (C20, C21)
    public static readonly string[] Labels = ["OPERATION", "DATA", "RULE", "LIMIT", "MESSAGE", "ERROR"];

    // A label goes in brackets ("[DATA]", "[data]:") or before a colon ("DATA:").
    // Without that it is no label: "Error messages are shown..." is an unlabelled criterion (C19).
    private static readonly Regex LabelRe = new(
        $@"^\s*(?:\[\s*({string.Join("|", Labels)})\s*\]\s*:?|({string.Join("|", Labels)})\s*:)\s*", IgnoreCase);

    private const double MaxRejectedShare = 0.20;  // partial acceptance: more rejected than this -> repair
    private const double MaxUntreatedShare = 0.10; // C10: more untreated fragments than this -> lot error

    private const int QuoteMinWords = 3; // C6: a shorter quote is only valid if it is the whole fragment
    private static readonly Regex NumberRe = new(@"\d+(?:[.,]\d+)*");
    private static readonly string[] Tbd = ["[POR DEFINIR]", "[TBD]"];

    // R-h: feature, states, trigger and condition do not start with their keyword, which the
    // platform adds. The normalizer removes it when a space follows (a lone keyword would leave
    // the slot empty). Multi-word keywords go first so that "en cuanto" wins.
    private static readonly string[] LeadingKeywordSlots = ["feature", "states", "trigger", "condition"];
    private static readonly Dictionary<string, Regex> LeadingKeyword = new()
    {
        ["es"] = new(@"^(?:en cuanto|cuando|mientras|donde|si)\s+", IgnoreCase),
        ["en"] = new(@"^(?:as soon as|as long as|when|while|where|if)\s+", IgnoreCase),
    };

    private static readonly Dictionary<string, HashSet<string>> SubjectPrepositions = new()
    {
        ["es"] = ["de", "del", "desde", "en", "para", "durante"],
        ["en"] = ["of", "from", "for", "in", "during"],
    };

    private static readonly Dictionary<string, Regex> Negation = new()
    {
        ["es"] = new(@"^no\b", IgnoreCase),
        ["en"] = new(@"^not?\b", IgnoreCase),
    };

    private static readonly Regex EsInfinitive = new(@"^\w+(ar|er|ir|ír)(se|lo|la|le|los|las|les|nos)?$", IgnoreCase);

    // EARS pattern -> (feature, states, trigger) present.
    private static readonly Dictionary<string, (bool, bool, bool)> EarsShape = new()
    {
        ["ubiquitous"] = (false, false, false),
        ["event_driven"] = (false, false, true),
        ["state_driven"] = (false, true, false),
        ["unwanted_behaviour"] = (false, false, true),
        ["optional_feature"] = (true, false, false),
    };

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

    private static readonly Dictionary<char, char> QuoteMap = new()
    {
        ['“'] = '"', ['”'] = '"', ['„'] = '"', ['«'] = '"', ['»'] = '"', ['‘'] = '\'', ['’'] = '\'',
        ['–'] = '-', ['—'] = '-', [' '] = ' ',
    };

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // The schema is embedded in this DLL from prompts/generacion_requisitos/.
    private static readonly string SchemaText = ReadSchema();
    private static readonly JsonValidator RootValidator = new(SchemaText);
    private static readonly Dictionary<string, JsonValidator> ItemValidators =
        ItemLists.ToDictionary(l => l.Key, l => DefinitionValidator(l.Definition));

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
        var lotErrors = SchemaErrors(RootValidator, WithoutItems(cat)).Select(m => $"LOTE {m}").ToList();
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
        var domainTerms = new List<string>();
        var domainItems = c["domain_terms"]!.AsArray();
        for (var i = 0; i < domainItems.Count; i++)
        {
            var term = Str(domainItems[i]!, "term")!;
            var fragmentId = Str(domainItems[i]!, "fragment")!;
            string? problem = null;
            if (Words(term).Length < 2)
                problem = $"el término de dominio '{term}' debe tener al menos dos palabras";
            else if (fragments is not null && !fragments.ContainsKey(fragmentId))
                problem = $"fragmento inexistente {fragmentId}";
            else if (fragments is not null && !ContainsWords(fragments[fragmentId].Text, term))
                problem = $"el término de dominio '{term}' no aparece en {fragmentId}";
            if (problem is not null)
                warnings.Add($"DOMAIN[{i}] C5 {problem} (se ignora)");
            else
                domainTerms.Add(term);
        }

        void Fix(string owner, string what) => warnings.Add($"{owner} FIX {what}");

        // A quote in the wrong fragment but literal in a single fragment of the lot:
        // the id is corrected. Repeated entries are removed.
        void NormalizeTraces(Entry owner)
        {
            var seen = new HashSet<(string, string)>();
            var kept = new JsonArray();
            foreach (var t in owner.Obj["source_trace"]!.AsArray().Select(t => t!.AsObject()))
            {
                var fragmentId = Str(t, "fragment")!;
                var quote = Str(t, "quote")!;
                if (fragments is not null && (!fragments.TryGetValue(fragmentId, out var frag) || !QuoteIn(quote, frag.Text)))
                {
                    var found = lotFrags.Where(f => QuoteIn(quote, f.Text)).Select(f => f.Id).ToList();
                    if (found.Count == 1)
                    {
                        Fix(owner.Id, $"cita movida de {fragmentId} a {found[0]}, donde es literal");
                        fragmentId = found[0];
                        t["fragment"] = fragmentId;
                    }
                }
                if (!seen.Add((fragmentId, QuoteKey(quote))))
                {
                    Fix(owner.Id, $"cita repetida de {fragmentId} quitada");
                    continue;
                }
                kept.Add(t.DeepClone());
            }
            owner.Obj["source_trace"] = kept;
        }

        foreach (var e in validReqs)
        {
            var r = e.Obj;
            var s = r["slots"]!.AsObject();
            NormalizeTraces(e);
            if (s["system"] is JsonValue systemValue && systemValue.GetValue<string>() != sistema)
            {
                Fix(e.Id, $"slots.system '{systemValue.GetValue<string>()}' sustituido por '{sistema}'");
                s["system"] = sistema;
            }
            foreach (var key in s.Select(p => p.Key).ToList())
            {
                if (key is "system" or "trigger_keyword" || s[key] is null)
                    continue;
                List<string> values;
                if (s[key] is JsonArray list)
                    values = list.Select(v => v!.GetValue<string>()).ToList();
                else if (s[key] is JsonValue value && value.TryGetValue<string>(out var text))
                    values = [text];
                else
                    continue;
                var cleaned = values.Select(CleanSlot).ToList();
                if (!cleaned.SequenceEqual(values))
                    Fix(e.Id, $"slot '{key}': espacios o puntuación final quitados");
                if (LeadingKeywordSlots.Contains(key))
                {
                    var stripped = cleaned.Select(v => LeadingKeyword[lang].Replace(v, "", 1)).ToList();
                    foreach (var (before, after) in cleaned.Zip(stripped))
                        if (after != before)
                            Fix(e.Id, $"slot '{key}': palabra clave inicial «{before[..(before.Length - after.Length)].Trim()}» quitada (R-h)");
                    cleaned = stripped;
                }
                if (!cleaned.SequenceEqual(values))
                    s[key] = s[key] is JsonArray
                        ? new JsonArray(cleaned.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray())
                        : cleaned[0];
            }
            var criteria = new JsonArray();
            foreach (var crit in r["acceptance_criteria"]!.AsArray().Select(x => x!.GetValue<string>()))
            {
                var (tag, body) = SplitLabel(crit);
                var fixedCriterion = tag is not null ? $"[{tag}] {body}" : crit.Trim();
                if (tag is not null && fixedCriterion != crit)
                    Fix(e.Id, $"etiqueta de criterio normalizada: «{Truncate(crit, 40)}»");
                criteria.Add(fixedCriterion);
            }
            r["acceptance_criteria"] = criteria;
        }
        foreach (var e in validQs)
            NormalizeTraces(e);

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

        bool foundQuotes = true, foundFigures = true, foundFragments = true, foundVague = true,
            foundEarsShall = true, foundTree = true, foundCapabilities = true;
        int traceTotal = 0, traceOk = 0;

        // C6 (rejection) of an already normalized quote.
        List<string> CheckTrace(JsonObject t)
        {
            var problems = new List<string>();
            if (fragments is null)
                return problems;
            traceTotal++;
            var fragmentId = Str(t, "fragment")!;
            var quote = Str(t, "quote")!;
            if (!fragments.TryGetValue(fragmentId, out var frag))
                problems.Add($"C6 fragmento inexistente {fragmentId}");
            else if (!QuoteIn(quote, frag.Text))
                problems.Add($"C6 cita no literal en {fragmentId}: «{quote}»");
            else if (Words(quote).Length < QuoteMinWords && TrimEnd(quote) != TrimEnd(frag.Text))
                problems.Add($"C6 cita de menos de {QuoteMinWords} palabras que no es el fragmento entero: «{quote}»");
            else
                traceOk++;
            if (problems.Count > 0)
                foundQuotes = false;
            return problems;
        }

        foreach (var e in validQs)
        {
            foreach (var t in e.Obj["source_trace"]!.AsArray().Select(x => x!.AsObject()))
            {
                e.Errors.AddRange(CheckTrace(t));
            }
        }

        foreach (var e in validReqs)
        {
            var r = e.Obj;
            var s = r["slots"]!.AsObject();
            var errs = e.Errors;
            var warns = e.Warnings;
            var notation = Str(r, "notation");
            var masterTemplate = Str(r, "master_template");

            // C2 template <-> slots
            if (notation == "EARS")
            {
                var p = Str(r, "ears_pattern")!;
                var has = (Truthy(s, "feature"), Truthy(s, "states"), Truthy(s, "trigger"));
                if (EarsShape.TryGetValue(p, out var shape) && has != shape)
                    errs.Add($"C2 slots (feature, states, trigger) = ({PyBool(has.Item1)}, {PyBool(has.Item2)}, {PyBool(has.Item3)}) incompatibles con {p}");
                if (p == "complex" && (has.Item1 ? 1 : 0) + (has.Item2 ? 1 : 0) + (has.Item3 ? 1 : 0) < 2)
                    errs.Add("C2 complex exige al menos dos tipos de {feature, states, trigger}");
                if (p == "unwanted_behaviour" && Str(s, "trigger_keyword") != "IF")
                    errs.Add("C2 unwanted_behaviour exige trigger_keyword IF");
                if (p == "event_driven" && Str(s, "trigger_keyword") != "WHEN")
                    errs.Add("C2 event_driven exige trigger_keyword WHEN");
                if (Truthy(s, "trigger") != Truthy(s, "trigger_keyword"))
                    errs.Add("C2 trigger y trigger_keyword deben ir juntos");
            }
            else
            {
                var mc = Str(r, "master_condition");
                if ((mc == "none") != (s["condition"] is null))
                    errs.Add("C2 master_condition y slot condition incoherentes");
                if (masterTemplate == "functional")
                {
                    if ((Str(r, "master_activity") == "user_interaction") != Truthy(s, "actor"))
                        errs.Add("C2 actor obligatorio solo y siempre en user_interaction");
                }
                else
                {
                    var subject = Str(s, "subject_matter") ?? "";
                    var first = Words(subject).FirstOrDefault()?.ToLowerInvariant() ?? "";
                    if (!SubjectPrepositions[lang].Contains(first))
                        warns.Add($"C14 subject_matter debe empezar por [{string.Join(", ", SubjectPrepositions[lang].Order(StringComparer.Ordinal).Select(x => $"'{x}'"))}]: '{subject}'");
                }
            }

            // C14 slot format (R-a, R-b, R-c): warnings. The final punctuation was already removed.
            foreach (var (key, val) in SlotValues(s))
            {
                if (key == "value" && Tbd.Contains(val))
                    continue;
                var upperKeywords = Regex.Matches(val, @"\b[A-ZÁÉÍÓÚÑ]{2,}\b").Select(m => m.Value).Where(w => KeywordWords[lang].Contains(w)).ToList();
                if (upperKeywords.Count > 0)
                    warns.Add($"C14 slot '{key}' contiene keywords en mayúsculas [{string.Join(", ", upperKeywords.Select(w => $"'{w}'"))}] (R-b)");
                if (val.Length > 0 && char.IsUpper(val[0]) && !Regex.IsMatch(val, @"^[A-ZÁÉÍÓÚÑ0-9]{2,}\b"))
                    warns.Add($"C14 slot '{key}' empieza en mayúscula; ¿es nombre propio? (R-a)");
            }
            var verbSlot = notation == "EARS" ? Str(s, "response") : Str(s, "process_verb");
            if (!string.IsNullOrEmpty(verbSlot))
            {
                if (Negation[lang].IsMatch(verbSlot))
                    errs.Add($"C15 prohibición en negativo en '{verbSlot}' (R-e: usa impedir, rechazar, bloquear…)");
                if (lang == "es" && !EsInfinitive.IsMatch(Words(verbSlot).FirstOrDefault() ?? ""))
                    warns.Add($"C14 el slot de respuesta no parece empezar por infinitivo (R-c): {verbSlot}");
            }

            // Render (always done by the platform)
            try
            {
                e.Text = CatalogRenderer.Render(JsonSerializer.SerializeToElement(r), lang);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
            {
                errs.Add($"C2 no se pudo renderizar: {ex.Message}");
                e.Text = null;
            }

            // Criteria: label (C19) and text without the label for C5 and N2.
            var criteria = StrList(r, "acceptance_criteria");
            var criteriaTexts = new List<string>();
            foreach (var crit in criteria)
            {
                var (tag, body) = SplitLabel(crit);
                if (tag is null)
                    warns.Add($"C19 criterio sin etiqueta válida: «{Truncate(crit, 60)}»");
                criteriaTexts.Add(body);
            }

            // C5 vague terms, on the rendered text and on each criterion
            var exempt = masterTemplate == "property" && Tbd.Contains(Str(s, "value")) ? Str(s, "characteristic") : null;
            var vague = e.Text is not null ? VagueTerms(e.Text, lang, domainTerms, exempt) : [];
            foreach (var body in criteriaTexts)
                vague.AddRange(VagueTerms(body, lang, domainTerms, null).Select(v => $"{v} en un criterio"));
            if (vague.Count > 0)
            {
                errs.AddRange(vague.Distinct());
                foundVague = false;
            }

            // C7 obligation
            if (notation == "EARS" && Str(r, "obligation") != "SHALL")
            {
                errs.Add("C7 EARS con obligación distinta de SHALL");
                foundEarsShall = false;
            }

            // C12 decision tree
            var tree = BranchErrors(r);
            if (tree.Count > 0)
            {
                foundTree = false;
                errs.AddRange(tree);
            }

            // C6 quotes
            var traces = r["source_trace"]!.AsArray().Select(x => x!.AsObject()).ToList();
            foreach (var t in traces)
                errs.AddRange(CheckTrace(t));

            if (fragments is not null)
            {
                var cited = traces.Select(t => Str(t, "fragment")!).Where(fragments.ContainsKey).Select(id => fragments[id]).ToList();
                // C13 obligation evidence (warning)
                if (Str(r, "obligation_basis") == "explicit")
                {
                    var evidence = Str(r, "obligation_evidence") ?? "";
                    if (!cited.Any(f => ContainsWords(f.Text, evidence)))
                        warns.Add($"C13 obligation_evidence '{evidence}' no aparece en los fragmentos citados");
                    var obligation = Str(r, "obligation")!;
                    var normalized = Norm(evidence).ToLowerInvariant();
                    if (!ObligationSignals[obligation].Any(pat => Regex.IsMatch(normalized, $@"(?<!\w)(?:{pat})(?!\w)")))
                        warns.Add($"C13 obligation_evidence '{evidence}' no es una señal de {obligation} (§7)");
                }
                // N2 figures of slots and criteria: they must be in some cited fragment
                var sourceNumbers = cited.SelectMany(f => Numbers(f.Text)).ToHashSet();
                var pieces = SlotValues(s).ToList();
                pieces.AddRange(criteriaTexts.Select((b, i) => ($"criterio {i + 1}", b)));
                foreach (var (key, val) in pieces)
                {
                    var invented = Numbers(val).Except(sourceNumbers).Order(StringComparer.Ordinal).ToList();
                    if (invented.Count > 0)
                    {
                        errs.Add($"N2 cifra [{string.Join(", ", invented.Select(n => $"'{n}'"))}] de '{key}' no aparece en los fragmentos citados");
                        foundFigures = false;
                    }
                }
                // C16 origin (warning)
                var kinds = cited.Select(f => f.Kind).ToHashSet();
                var origin = Str(r, "origin");
                if (origin == "explicit" && !kinds.Contains("document"))
                    warns.Add("C16 origin explicit sin ningún fragmento de documento citado");
                if (origin == "clarification" && !kinds.Contains("clarification"))
                    warns.Add("C16 origin clarification sin ninguna duda resuelta citada");
            }

            // N3 pending value with its missing_value question
            if (masterTemplate == "property" && Tbd.Contains(Str(s, "value")))
            {
                if (!StrList(r, "open_question_ids").Any(q => Str(qById[q].Obj, "type") == "missing_value"))
                    errs.Add("N3 valor pendiente sin pregunta missing_value asociada");
            }

            // C22 umbrella verb with fewer than five [OPERATION]
            if (!string.IsNullOrEmpty(verbSlot) && UmbrellaRe[lang].IsMatch(verbSlot))
            {
                var operations = criteria.Count(crit => SplitLabel(crit).Label == "OPERATION");
                if (operations < UmbrellaMinOperations)
                    warns.Add($"C22 verbo paraguas en '{verbSlot}' con {operations} criterios [OPERATION] (U7)");
            }
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

        var finalReqs = new JsonArray();
        var finalQs = new JsonArray();
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
            ("quotes_are_literal", foundQuotes), ("figures_from_source", foundFigures),
            ("all_fragments_accounted", foundFragments), ("all_capabilities_covered", foundCapabilities),
            ("no_vague_terms", foundVague), ("ears_only_shall", foundEarsShall), ("decision_tree_consistent", foundTree),
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
            LiteralQuotes: fragments is not null ? $"{traceOk}/{traceTotal}" : "no comprobado",
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

    // C12: consistency of decision_branch with type, notation and obligation (tree of §6).
    private static List<string> BranchErrors(JsonObject r)
    {
        var b = Str(r, "decision_branch");
        var t = Str(r, "type");
        var nt = Str(r, "notation");
        var tpl = Str(r, "master_template");
        var ob = Str(r, "obligation");
        var fb = Str(r, "escalation_fallback");
        var errs = new List<string>();

        if (fb is not null && b is not ("D1" or "D4"))
            errs.Add($"C12 escalation_fallback solo se admite en D1 o D4 (rama {b})");
        if (tpl == "property" && b != "D1")
            errs.Add($"C12 MASTeR property solo en D1 (rama {b})");
        switch (b)
        {
            case "D1":
                if (t != "RNF")
                    errs.Add($"C12 D1 exige type RNF (es {t})");
                if (nt == "MASTeR" && tpl != "property")
                    errs.Add("C12 D1 exige MASTeR property");
                if (nt == "EARS" && fb is null)
                    errs.Add("C12 D1 en EARS solo como degradación (escalation_fallback)");
                break;
            case "D2":
                if (t is not ("RA" or "RT" or "RNF"))
                    errs.Add($"C12 D2 exige type RA, RT o RNF (es {t})");
                if (ob == "SHALL" && !(nt == "EARS" && Str(r, "ears_pattern") == "ubiquitous"))
                    errs.Add("C12 D2 con SHALL exige EARS ubiquitous");
                if (ob != "SHALL" && !(nt == "MASTeR" && tpl == "functional" && Str(r, "master_activity") == "autonomous"))
                    errs.Add("C12 D2 sin SHALL exige MASTeR functional autonomous");
                break;
            default:
                if (t is not ("RF" or "RI" or "RNF"))
                    errs.Add($"C12 {b} exige type RF, RI o RNF (es {t})");
                if (b == "D3")
                {
                    if (ob == "SHALL")
                        errs.Add("C12 D3 exige obligation SHOULD o MAY");
                    if (!(nt == "MASTeR" && tpl == "functional"))
                        errs.Add("C12 D3 exige MASTeR functional");
                }
                else if (b == "D4")
                {
                    if (nt == "MASTeR" && !(tpl == "functional" && Str(r, "master_condition") is "logic" or "event"))
                        errs.Add("C12 D4 exige MASTeR functional con condición logic o event");
                    if (nt == "EARS" && fb is null)
                        errs.Add("C12 D4 en EARS solo como degradación (escalation_fallback)");
                }
                else if (b == "D5" && nt != "EARS")
                {
                    errs.Add("C12 D5 exige EARS");
                }
                break;
        }
        return errs;
    }

    // C5: vague terms of a text, except pending values, declared domain terms
    // (N7) and `exempt` (the nominalized characteristic of N3).
    private static List<string> VagueTerms(string text, string lang, List<string> domainTerms, string? exempt)
    {
        foreach (var t in Tbd)
            text = text.Replace(t, "");
        foreach (var term in domainTerms)
            text = Regex.Replace(text, Regex.Escape(term), "", IgnoreCase);
        if (exempt is not null)
            text = text.Replace(exempt, "");
        var found = new List<string>();
        foreach (var (category, patterns) in Vague[lang])
        {
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(text, $@"(?<!\w){pattern}(?!\w)", IgnoreCase))
                    found.Add($"C5 término vago ({category}) '{pattern}'");
            }
        }
        return found;
    }

    // ("DATA", "text") if the criterion starts with a valid label, or (null, criterion).
    public static (string? Label, string Text) SplitLabel(string criterion)
    {
        var m = LabelRe.Match(criterion);
        if (!m.Success)
            return (null, criterion.Trim());
        var label = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
        return (label.ToUpperInvariant(), criterion[m.Length..].Trim());
    }

    // Numeric label of a section ("§2.1" -> "2.1", "3. Roles" -> "3"), or null.
    private static string? SectionKey(string section) =>
        Regex.Match(section, @"^\s*§?\s*(\d+(?:\.\d+)*)") is { Success: true } m ? m.Groups[1].Value : null;

    // The fragment belongs to the section or to one of its subsections (C10 with discarded_sections).
    private static bool InSection(string fragmentSection, string section)
    {
        if (Norm(fragmentSection) == Norm(section))
            return true;
        var fk = SectionKey(fragmentSection);
        var sk = SectionKey(section);
        return fk is not null && sk is not null && (fk == sk || fk.StartsWith(sk + ".", StringComparison.Ordinal));
    }

    // Fragment ids of the traces of an element, also of one rejected by the schema (if they can be read).
    private static IEnumerable<string> TraceFragments(JsonNode? item)
    {
        if (item is not JsonObject obj || obj["source_trace"] is not JsonArray traces)
            yield break;
        foreach (var t in traces)
        {
            if (t is JsonObject trace && trace["fragment"] is JsonValue v && v.TryGetValue<string>(out var id))
                yield return id;
        }
    }

    // Free-text slots (not system nor trigger_keyword), with the states numbered.
    private static IEnumerable<(string Key, string Value)> SlotValues(JsonObject slots)
    {
        foreach (var (name, value) in slots)
        {
            if (name is "system" or "trigger_keyword" || value is null)
                continue;
            if (value is JsonArray list)
            {
                var i = 0;
                foreach (var item in list)
                    yield return ($"{name}[{i++}]", item?.GetValue<string>() ?? "");
            }
            else if (value is JsonValue v && v.TryGetValue<string>(out var text))
            {
                yield return (name, text);
            }
        }
    }

    // --- Schema ------------------------------------------------------------------------------

    // The root with the item lists emptied: its errors are those of the lot.
    private static JsonNode? WithoutItems(JsonNode? cat)
    {
        if (cat is not JsonObject obj)
            return cat;
        var copy = obj.DeepClone().AsObject();
        foreach (var (key, _, _) in ItemLists)
        {
            if (copy[key] is JsonArray)
                copy[key] = new JsonArray();
        }
        return copy;
    }

    private static List<string> ItemSchemaErrors(string list, int index, JsonNode? item) =>
        SchemaErrors(ItemValidators[list], item, $"{list}/{index}");

    private static List<string> SchemaErrors(JsonValidator validator, JsonNode? instance, string basePath = "")
    {
        var element = JsonSerializer.SerializeToElement(instance);
        var result = validator.Validate(element, new JsonSchemaOptions { OutputFormat = OutputFormat.List });
        if (result.IsValid)
            return [];
        return result.ValidationErrors
            .Select(e =>
            {
                var path = e.InstanceLocation?.ToString() is { Length: > 0 } p ? p.TrimStart('#', '/') : "";
                var full = string.Join("/", new[] { basePath, path }.Where(x => x.Length > 0));
                return $"SCHEMA {(full.Length > 0 ? full : "(raíz)")}: {e.ErrorMessage ?? e.Keyword}";
            })
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    // A validator for one definition of the schema ("requirement", "capability"...).
    private static JsonValidator DefinitionValidator(string definition)
    {
        var schema = JsonNode.Parse(SchemaText)!.AsObject();
        var sub = new JsonObject
        {
            ["$schema"] = schema["$schema"]!.DeepClone(),
            ["$defs"] = schema["$defs"]!.DeepClone(),
            ["$ref"] = $"#/$defs/{definition}",
        };
        return new JsonValidator(sub.ToJsonString());
    }

    // --- Text helpers -------------------------------------------------------------------------

    // NFC, typographic quotes and dashes unified, spaces collapsed.
    public static string Norm(string s)
    {
        var mapped = new StringBuilder(s.Normalize(NormalizationForm.FormC));
        for (var i = 0; i < mapped.Length; i++)
        {
            if (QuoteMap.TryGetValue(mapped[i], out var replacement))
                mapped[i] = replacement;
        }
        return Regex.Replace(mapped.ToString(), @"\s+", " ").Trim();
    }

    // How quotes are compared: like Norm, without Markdown marks (` * _) and case-insensitive.
    private static string QuoteKey(string s) =>
        Regex.Replace(Regex.Replace(Norm(s), "[`*_]", ""), @"\s+", " ").Trim().ToLowerInvariant();

    private static bool QuoteIn(string quote, string text) => QuoteKey(text).Contains(QuoteKey(quote), StringComparison.Ordinal);

    private static string TrimEnd(string s) => QuoteKey(s).TrimEnd('.', ',', ';', ':');

    private static string CleanSlot(string value) =>
        Tbd.Contains(value) ? value : value.Trim().TrimEnd('.', ',', ';', ':').TrimEnd();

    // `needle` appears in `haystack` as whole words (normalized, case-insensitive).
    private static bool ContainsWords(string haystack, string needle) =>
        Regex.IsMatch(Norm(haystack).ToLowerInvariant(), $@"(?<!\w){Regex.Escape(Norm(needle).ToLowerInvariant())}(?!\w)");

    // Figures of a text, without thousand or decimal separators ("99,9" and "99.9" give "999").
    private static HashSet<string> Numbers(string s) =>
        NumberRe.Matches(s).Select(m => m.Value.Replace(".", "").Replace(",", "")).ToHashSet();

    private static string[] Words(string s) => s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static string Truncate(string s, int length) => s.Length <= length ? s : s[..length];

    private static string PyBool(bool b) => b ? "True" : "False";

    // --- JSON helpers -------------------------------------------------------------------------

    private static string Label(JsonNode? item, string fallback) =>
        item is JsonObject obj && obj["id"] is JsonValue v && v.TryGetValue<string>(out var id) ? id : fallback;

    private static string? Str(JsonNode node, string name) =>
        node is JsonObject obj && obj[name] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    private static List<string> StrList(JsonNode node, string name) =>
        node is JsonObject obj && obj[name] is JsonArray list
            ? list.Where(x => x is JsonValue v && v.TryGetValue<string>(out _)).Select(x => x!.GetValue<string>()).ToList()
            : [];

    // Python's bool(x.get(name)): false for missing, null, "" and [].
    private static bool Truthy(JsonObject obj, string name) => obj[name] switch
    {
        null => false,
        JsonArray list => list.Count > 0,
        JsonValue v when v.TryGetValue<string>(out var s) => s.Length > 0,
        JsonValue v when v.TryGetValue<bool>(out var b) => b,
        _ => true,
    };

    private static JsonArray StringArray(IEnumerable<string> values) =>
        new(values.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray());

    // Copy of the catalog with the rendered `text` after `slots` in each requirement.
    public static string Enrich(string catalogJson, IReadOnlyDictionary<string, string?> texts)
    {
        var root = JsonNode.Parse(catalogJson)!.AsObject();
        var requirements = root["requirements"]!.AsArray();
        for (var i = 0; i < requirements.Count; i++)
        {
            var original = requirements[i]!.AsObject();
            var enriched = new JsonObject();
            foreach (var (key, value) in original)
            {
                enriched[key] = value?.DeepClone();
                if (key == "slots")
                    enriched["text"] = texts.GetValueOrDefault(original["id"]!.GetValue<string>());
            }
            requirements[i] = enriched;
        }
        return root.ToJsonString(Indented);
    }

    private static string ReadSchema()
    {
        using var stream = typeof(CatalogValidator).Assembly.GetManifestResourceStream("requirements.schema.json")
            ?? throw new InvalidOperationException("Embedded resource not found: requirements.schema.json");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
