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
    [property: JsonPropertyName("autoverificacion_coincide")] bool SelfCheckMatches);

// Result of validating a catalog. Errors make it invalid (and trigger a repair
// retry); warnings do not. Texts: rendered text of each requirement by id.
public record CatalogValidationReport(
    [property: JsonPropertyName("valid")] bool Valid,
    [property: JsonPropertyName("errors")] IReadOnlyList<string> Errors,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("metrics")] CatalogMetrics? Metrics,
    [property: JsonIgnore] IReadOnlyDictionary<string, string?> Texts);

// Deterministic validator of the EARS/MASTeR catalog v1.2 written by model A or B.
// Port of check() in prompts/generacion_requisitos/validar_catalogo.py, the
// reference: for the same files it must give the same error codes (see the
// mutation fixtures in tests/fixtures/catalog-mutations). Keep both in sync.
// Each error starts with the affected id (if any) and its code: "REQ-003 C5 ...".
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

    private const int QuoteMinWords = 3; // C6: a shorter quote is only valid if it is the whole fragment
    private static readonly Regex NumberRe = new(@"\d+(?:[.,]\d+)*");
    private static readonly string[] Tbd = ["[POR DEFINIR]", "[TBD]"];

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

    // Category of each code, for the per-requirement metrics.
    private static readonly Dictionary<string, string> Category = new()
    {
        ["C2"] = "sintaxis", ["C3"] = "sintaxis", ["C14"] = "sintaxis",
        ["C5"] = "redaccion", ["C15"] = "redaccion",
        ["C7"] = "decision", ["C12"] = "decision",
        ["C6"] = "trazabilidad", ["C13"] = "trazabilidad", ["C16"] = "trazabilidad",
        ["C11"] = "referencias", ["N3"] = "referencias",
        ["N2"] = "fidelidad", ["C17"] = "trazabilidad",
    };

    // C18: key order of §10 (quote and rationale before the decision).
    private static readonly string[] RequirementKeyOrder =
    [
        "id", "source_trace", "obligation", "obligation_basis", "obligation_evidence", "kind",
        "notation_rationale", "decision_branch", "notation", "ears_pattern", "master_template",
        "master_activity", "master_condition", "escalation_reason", "escalation_fallback",
        "out_of_subset", "slots", "origin", "derived_from", "open_question_ids",
    ];

    private static readonly Dictionary<string, string[]> SlotKeyOrder = new()
    {
        ["EARS"] = ["system", "feature", "states", "trigger", "trigger_keyword", "response"],
        ["functional"] = ["system", "condition", "actor", "process_verb", "object", "details"],
        ["property"] = ["condition", "characteristic", "subject_matter", "qualifying_expression", "value"],
    };

    private static readonly Dictionary<char, char> QuoteMap = new()
    {
        ['“'] = '"', ['”'] = '"', ['„'] = '"', ['«'] = '"', ['»'] = '"', ['‘'] = '\'', ['’'] = '\'',
        ['–'] = '-', ['—'] = '-', [' '] = ' ',
    };

    // The schema is embedded in this DLL from prompts/generacion_requisitos/.
    private static readonly JsonValidator SchemaValidator = new(ReadSchema());

    // catalogJson: the file written by the model. fragments: the lot's
    // fragments (input/fragmentos.json), in order; without them C6, C10, C13,
    // C16, C17, N2 and the domain-term checks are skipped.
    public static CatalogValidationReport Validate(string catalogJson, string systemName, IReadOnlyList<Fragment>? fragments)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(catalogJson);
        }
        catch (JsonException e)
        {
            return new CatalogValidationReport(false, [$"JSON no válido: {e.Message}"], [], null, new Dictionary<string, string?>());
        }

        using (document)
        {
            return Check(document.RootElement, systemName, fragments);
        }
    }

    private static CatalogValidationReport Check(JsonElement cat, string sistema, IReadOnlyList<Fragment>? fragmentList)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var schemaResult = SchemaValidator.Validate(cat, new JsonSchemaOptions { OutputFormat = OutputFormat.List });
        if (!schemaResult.IsValid)
        {
            foreach (var e in schemaResult.ValidationErrors.OrderBy(e => e.InstanceLocation?.ToString(), StringComparer.Ordinal))
            {
                var path = e.InstanceLocation?.ToString() is { Length: > 0 } p ? p.TrimStart('#', '/') : "";
                errors.Add($"SCHEMA {(path.Length > 0 ? path : "(raíz)")}: {e.ErrorMessage ?? e.Keyword}");
            }
            return new CatalogValidationReport(false, errors, warnings, null, new Dictionary<string, string?>());
        }

        var fragments = fragmentList?.ToDictionary(f => f.Id);
        var lang = cat.GetProperty("language").GetString()!;
        var reqs = cat.GetProperty("requirements").EnumerateArray().ToList();
        var questions = cat.GetProperty("open_questions").EnumerateArray().ToList();
        var discarded = cat.GetProperty("discarded_statements").EnumerateArray().ToList();
        var reqIds = reqs.Select(r => r.GetProperty("id").GetString()!).ToList();
        var qById = new Dictionary<string, JsonElement>();
        foreach (var q in questions)
        {
            qById.TryAdd(q.GetProperty("id").GetString()!, q);
        }
        var texts = new Dictionary<string, string?>();
        var perReqCategories = new Dictionary<string, HashSet<string>>();
        bool foundQuotes = true, foundFigures = true, foundFragments = true, foundVague = true, foundEarsShall = true, foundTree = true;
        var wholeDiscards = discarded.Where(d => Json.IsNull(d, "quote")).Select(d => d.GetProperty("fragment").GetString()!).ToHashSet();

        // Declared domain terms (N7): entity names that contain a C5 word.
        var domainTerms = new List<string>();
        var domainIndex = 0;
        foreach (var dt in cat.GetProperty("domain_terms").EnumerateArray())
        {
            var term = dt.GetProperty("term").GetString()!;
            var fragmentId = dt.GetProperty("fragment").GetString()!;
            var local = new List<string>();
            if (Words(term).Length < 2)
                local.Add($"C5 el término de dominio '{term}' debe tener al menos dos palabras");
            if (fragments is not null)
            {
                if (!fragments.TryGetValue(fragmentId, out var frag))
                    local.Add($"C5 fragmento inexistente {fragmentId}");
                else if (!ContainsWords(frag.Text, term))
                    local.Add($"C5 el término de dominio '{term}' no aparece en {fragmentId}");
            }
            if (local.Count > 0)
                foundVague = false;
            else
                domainTerms.Add(term);
            errors.AddRange(local.Select(m => $"DOMAIN[{domainIndex}] {m}"));
            domainIndex++;
        }

        if (reqIds.Distinct().Count() != reqIds.Count)
            errors.Add("C9 ids de requisito duplicados");
        if (qById.Count != questions.Count)
            errors.Add("C9 ids de pregunta duplicados");
        if (!reqIds.SequenceEqual(Enumerable.Range(1, reqs.Count).Select(i => $"REQ-{i:000}")))
            warnings.Add("C9 ids de requisito no consecutivos desde REQ-001");

        var referenced = new HashSet<string>();
        int traceTotal = 0, traceOk = 0;

        void CheckTrace(JsonElement t, List<string> local, bool isDiscard = false)
        {
            var fragmentId = t.GetProperty("fragment").GetString()!;
            referenced.Add(fragmentId);
            if (!isDiscard && wholeDiscards.Contains(fragmentId))
                local.Add($"C17 cita {fragmentId}, que está descartado entero");
            var quote = Json.Str(t, "quote");
            if (fragments is null || quote is null)
                return;
            traceTotal++;
            if (!fragments.TryGetValue(fragmentId, out var frag))
            {
                local.Add($"C6 fragmento inexistente {fragmentId}");
                foundQuotes = false;
            }
            else if (!Norm(frag.Text).Contains(Norm(quote), StringComparison.Ordinal))
            {
                local.Add($"C6 cita no literal en {fragmentId}: «{quote}»");
                foundQuotes = false;
            }
            else if (Words(quote).Length < QuoteMinWords && TrimEnd(quote) != TrimEnd(frag.Text))
            {
                local.Add($"C6 cita de menos de {QuoteMinWords} palabras que no es el fragmento entero: «{quote}»");
                foundQuotes = false;
            }
            else
            {
                traceOk++;
            }
        }

        foreach (var r in reqs)
        {
            var rid = r.GetProperty("id").GetString()!;
            var s = r.GetProperty("slots");
            var notation = r.GetProperty("notation").GetString();
            var masterTemplate = Json.Str(r, "master_template");
            var local = new List<string>();

            // C2 pattern <-> slots
            if (notation == "EARS")
            {
                var p = Json.Str(r, "ears_pattern");
                var has = (Json.Truthy(s, "feature"), Json.Truthy(s, "states"), Json.Truthy(s, "trigger"));
                if (p is not null && EarsShape.TryGetValue(p, out var shape) && has != shape)
                    local.Add($"C2 slots (feature, states, trigger) = ({PyBool(has.Item1)}, {PyBool(has.Item2)}, {PyBool(has.Item3)}) incompatibles con {p}");
                if (p == "complex" && (has.Item1 ? 1 : 0) + (has.Item2 ? 1 : 0) + (has.Item3 ? 1 : 0) < 2)
                    local.Add("C2 complex exige al menos dos tipos de {feature, states, trigger}");
                if (p == "unwanted_behaviour" && Json.Str(s, "trigger_keyword") != "IF")
                    local.Add("C2 unwanted_behaviour exige trigger_keyword IF");
                if (p == "event_driven" && Json.Str(s, "trigger_keyword") != "WHEN")
                    local.Add("C2 event_driven exige trigger_keyword WHEN");
                if (Json.Truthy(s, "trigger") != Json.Truthy(s, "trigger_keyword"))
                    local.Add("C2 trigger y trigger_keyword deben ir juntos");
                foreach (var slot in new[] { "trigger", "feature" })
                {
                    if (Json.Truthy(s, slot) && Regex.IsMatch(Json.Str(s, slot)!, @"\b(y|o|and|or)\b"))
                        warnings.Add($"{rid} condición posiblemente compuesta en '{slot}' (revisar G2/D4): {Json.Str(s, slot)}");
                }
                var states = Json.StrList(s, "states");
                for (var i = 0; i < states.Count; i++)
                {
                    if (Regex.IsMatch(states[i], @"\b(o|or)\b"))
                        warnings.Add($"{rid} estado con 'o' en states[{i}] (revisar G2): {states[i]}");
                }
            }
            else
            {
                var mc = Json.Str(r, "master_condition");
                if ((mc == "none") != Json.IsNull(s, "condition"))
                    local.Add("C2 master_condition y slot condition incoherentes");
                if (masterTemplate == "functional")
                {
                    if ((Json.Str(r, "master_activity") == "user_interaction") != Json.Truthy(s, "actor"))
                        local.Add("C2 actor obligatorio solo y siempre en user_interaction");
                }
                else
                {
                    var subject = Json.Str(s, "subject_matter") ?? "";
                    var first = Words(subject).FirstOrDefault()?.ToLowerInvariant() ?? "";
                    if (!SubjectPrepositions[lang].Contains(first))
                        local.Add($"C14 subject_matter debe empezar por [{string.Join(", ", SubjectPrepositions[lang].Order(StringComparer.Ordinal).Select(x => $"'{x}'"))}]: '{subject}'");
                }
            }

            // C18 key order (warning)
            var templateKey = notation == "EARS" ? "EARS" : masterTemplate ?? "";
            var reqKeys = r.EnumerateObject().Select(p => p.Name).ToList();
            var slotKeys = s.EnumerateObject().Select(p => p.Name).ToList();
            var expectedSlotKeys = SlotKeyOrder.TryGetValue(templateKey, out var order) ? order : slotKeys.ToArray();
            if (!reqKeys.SequenceEqual(RequirementKeyOrder) || !slotKeys.SequenceEqual(expectedSlotKeys))
                warnings.Add($"{rid} C18 orden de claves distinto del de §10");

            // C3 system
            if (Json.Str(s, "system") is { } system && system != sistema)
                local.Add($"C3 slots.system '{system}' ≠ '{sistema}'");

            // C14 slot format (R-a, R-b, R-c) and C15 negation (R-e)
            foreach (var (key, val) in SlotValues(s))
            {
                if (key == "value" && Tbd.Contains(val))
                    continue;
                if (Regex.IsMatch(val, @"[.,;:]\s*$"))
                    local.Add($"C14 slot '{key}' termina en signo de puntuación (R-a)");
                var upperKeywords = Regex.Matches(val, @"\b[A-ZÁÉÍÓÚÑ]{2,}\b").Select(m => m.Value).Where(w => KeywordWords[lang].Contains(w)).ToList();
                if (upperKeywords.Count > 0)
                    local.Add($"C14 slot '{key}' contiene keywords en mayúsculas [{string.Join(", ", upperKeywords.Select(w => $"'{w}'"))}] (R-b)");
                if (val.Length > 0 && char.IsUpper(val[0]) && !Regex.IsMatch(val, @"^[A-ZÁÉÍÓÚÑ0-9]{2,}\b"))
                    warnings.Add($"{rid} slot '{key}' empieza en mayúscula; ¿es nombre propio? (R-a)");
            }
            var verbSlot = notation == "EARS" ? Json.Str(s, "response") : Json.Str(s, "process_verb");
            if (!string.IsNullOrEmpty(verbSlot))
            {
                if (Negation[lang].IsMatch(verbSlot))
                    local.Add($"C15 prohibición en negativo en '{verbSlot}' (R-e: usa impedir, rechazar, bloquear…)");
                if (lang == "es" && !EsInfinitive.IsMatch(Words(verbSlot)[0]))
                    warnings.Add($"{rid} el slot de respuesta no parece empezar por infinitivo (R-c): {verbSlot}");
                if (lang == "es")
                {
                    var coordinated = Regex.Matches(verbSlot, @"\b(?:y|e|o|u)\s+(\w+)").Select(m => m.Groups[1].Value).Where(w => EsInfinitive.IsMatch(w)).ToList();
                    if (coordinated.Count > 0)
                        warnings.Add($"{rid} C4 posible segundo verbo coordinado [{string.Join(", ", coordinated.Select(w => $"'{w}'"))}] (revisar G1/G2): {verbSlot}");
                }
            }

            // Render (always done by the platform)
            try
            {
                texts[rid] = CatalogRenderer.Render(r, lang);
            }
            catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException)
            {
                local.Add($"C2 no se pudo renderizar: {e.Message}");
                texts[rid] = null;
            }

            // C5 vague terms, on the rendered text
            if (texts[rid] is { } rendered)
            {
                var txt = rendered;
                foreach (var t in Tbd)
                    txt = txt.Replace(t, "");
                foreach (var term in domainTerms) // N7: declared domain terms are not vague language
                    txt = Regex.Replace(txt, Regex.Escape(term), "", IgnoreCase);
                if (masterTemplate == "property" && Tbd.Contains(Json.Str(s, "value")))
                    txt = txt.Replace(Json.Str(s, "characteristic")!, ""); // N3: the nominalized characteristic is allowed
                foreach (var (category, patterns) in Vague[lang])
                {
                    foreach (var pattern in patterns)
                    {
                        if (Regex.IsMatch(txt, $@"(?<!\w){pattern}(?!\w)", IgnoreCase))
                        {
                            local.Add($"C5 término vago ({category}) '{pattern}'");
                            foundVague = false;
                        }
                    }
                }
            }

            // C7 obligation
            var obligation = r.GetProperty("obligation").GetString()!;
            if (notation == "EARS" && obligation != "SHALL")
            {
                local.Add("C7 EARS con obligación distinta de SHALL");
                foundEarsShall = false;
            }

            // C12 decision tree
            var branchErrors = ExpectedBranchErrors(r);
            if (branchErrors.Count > 0)
                foundTree = false;
            local.AddRange(branchErrors);

            // C6 quotes
            var traces = r.GetProperty("source_trace").EnumerateArray().ToList();
            foreach (var t in traces)
                CheckTrace(t, local);

            // C13 obligation evidence, N2 figures and C16 origin
            if (fragments is not null)
            {
                var cited = traces.Select(t => t.GetProperty("fragment").GetString()!)
                    .Where(fragments.ContainsKey).Select(id => fragments[id]).ToList();
                if (Json.Str(r, "obligation_basis") == "explicit")
                {
                    var ev = Json.Str(r, "obligation_evidence") ?? "";
                    if (!cited.Any(f => ContainsWords(f.Text, ev)))
                        local.Add($"C13 obligation_evidence '{ev}' no aparece en los fragmentos citados");
                    var evNorm = Norm(ev).ToLowerInvariant();
                    if (!ObligationSignals[obligation].Any(pat => Regex.IsMatch(evNorm, $@"(?<!\w)(?:{pat})(?!\w)")))
                        local.Add($"C13 obligation_evidence '{ev}' no es una señal de {obligation} (§7)");
                }
                // N2: every figure of the slots appears in some cited fragment
                var sourceNumbers = cited.SelectMany(f => Numbers(f.Text)).ToHashSet();
                foreach (var (key, val) in SlotValues(s))
                {
                    var invented = Numbers(val).Where(n => !sourceNumbers.Contains(n)).Order(StringComparer.Ordinal).ToList();
                    if (invented.Count > 0)
                    {
                        local.Add($"N2 cifra [{string.Join(", ", invented.Select(n => $"'{n}'"))}] del slot '{key}' no aparece en los fragmentos citados");
                        foundFigures = false;
                    }
                }
                var kinds = cited.Select(f => f.Kind).ToHashSet();
                var origin = Json.Str(r, "origin");
                if (origin == "explicit" && !kinds.Contains(Fragmenter.DocumentKind))
                    local.Add("C16 origin explicit sin ningún fragmento de documento citado");
                if (origin == "clarification" && !kinds.Contains(Fragmenter.ClarificationKind))
                    local.Add("C16 origin clarification sin ninguna duda resuelta citada");
            }

            // C11 references and N3
            var openQuestionIds = Json.StrList(r, "open_question_ids");
            foreach (var q in openQuestionIds)
            {
                if (!qById.TryGetValue(q, out var question))
                    local.Add($"C11 open_question_id inexistente {q}");
                else if (!Json.StrList(question, "related_requirements").Contains(rid))
                    local.Add($"C11 {q} no incluye {rid} en related_requirements (vínculo no bidireccional)");
            }
            foreach (var d in Json.StrList(r, "derived_from"))
            {
                if (!reqIds.Contains(d))
                    local.Add($"C11 derived_from inexistente {d}");
            }
            if (masterTemplate == "property" && Tbd.Contains(Json.Str(s, "value")))
            {
                if (!openQuestionIds.Any(q => qById.TryGetValue(q, out var question) && Json.Str(question, "type") == "missing_value"))
                    local.Add("N3 valor pendiente sin pregunta missing_value asociada");
            }

            errors.AddRange(local.Select(m => $"{rid} {m}"));
            perReqCategories[rid] = local.Select(m => Category.GetValueOrDefault(m.Split(' ')[0], "otros")).ToHashSet();
        }

        foreach (var q in questions)
        {
            var qid = q.GetProperty("id").GetString()!;
            var local = new List<string>();
            var related = Json.StrList(q, "related_requirements");
            foreach (var rr in related)
            {
                if (!reqIds.Contains(rr))
                    local.Add($"C11 related_requirement inexistente {rr}");
                else if (!Json.StrList(reqs.First(r => r.GetProperty("id").GetString() == rr), "open_question_ids").Contains(qid))
                    local.Add($"C11 {rr} no incluye {qid} en open_question_ids (vínculo no bidireccional)");
            }
            if (related.Count == 0 && Json.Str(q, "type") != "scope")
                warnings.Add($"{qid} pregunta {Json.Str(q, "type")} sin requisitos relacionados");
            foreach (var t in q.GetProperty("source_trace").EnumerateArray())
                CheckTrace(t, local);
            errors.AddRange(local.Select(m => $"{qid} {m}"));
        }

        for (var i = 0; i < discarded.Count; i++)
        {
            var local = new List<string>();
            var fragmentId = discarded[i].GetProperty("fragment").GetString()!;
            if (fragments is not null && !fragments.ContainsKey(fragmentId))
                local.Add($"C6 fragmento inexistente {fragmentId}");
            CheckTrace(discarded[i], local, isDiscard: true);
            errors.AddRange(local.Select(m => $"DISCARDED[{i}] {m}"));
        }

        // C10 coverage of the document fragments
        var verdict = cat.GetProperty("verdict");
        var docFragments = (fragmentList ?? []).Where(f => f.Kind == Fragmenter.DocumentKind).Select(f => f.Id).ToList();
        var unprocessed = Json.StrList(verdict, "unprocessed_fragments").ToHashSet();
        var uncovered = docFragments.Where(id => !referenced.Contains(id) && !unprocessed.Contains(id)).ToList();
        if (fragments is not null && uncovered.Count > 0)
        {
            errors.Add($"C10 fragmentos sin tratar: {string.Join(", ", uncovered)}");
            foundFragments = false;
        }
        if (unprocessed.Count > 0)
            warnings.Add($"C10 fragmentos declarados sin procesar: {string.Join(", ", unprocessed.Order(StringComparer.Ordinal))}");

        // Verdict consistency
        var real = new Dictionary<string, int>
        {
            ["requirements"] = reqs.Count,
            ["ears"] = reqs.Count(r => r.GetProperty("notation").GetString() == "EARS"),
            ["master"] = reqs.Count(r => r.GetProperty("notation").GetString() == "MASTeR"),
            ["open_questions"] = questions.Count,
            ["discarded"] = discarded.Count,
        };
        var counts = verdict.GetProperty("counts");
        foreach (var (key, val) in real)
        {
            var declared = counts.GetProperty(key).GetInt32();
            if (declared != val)
                errors.Add($"VERDICT counts.{key}={declared} pero hay {val}");
        }
        var status = Json.Str(verdict, "status");
        if (status == "complete" && unprocessed.Count > 0)
            errors.Add("VERDICT status complete con unprocessed_fragments no vacío");
        if (status == "partial" && unprocessed.Count == 0)
            errors.Add("VERDICT status partial sin unprocessed_fragments");

        // Declared self-check versus the validator's (metric; does not block)
        var selfCheck = verdict.GetProperty("self_check");
        var declaredVsFound = new (string Key, bool Found)[]
        {
            ("quotes_are_literal", foundQuotes), ("figures_from_source", foundFigures),
            ("all_fragments_accounted", foundFragments), ("no_vague_terms", foundVague),
            ("ears_only_shall", foundEarsShall), ("decision_tree_consistent", foundTree),
        };
        var mismatches = declaredVsFound.Where(x => selfCheck.GetProperty(x.Key).GetBoolean() && !x.Found).Select(x => x.Key).ToList();
        foreach (var key in mismatches)
            warnings.Add($"SELF_CHECK el modelo declara {key}=true y el validador encuentra fallos");

        var n = reqs.Count;
        double? Share(string? category)
        {
            if (n == 0)
                return null;
            var ok = category is null
                ? perReqCategories.Values.Count(c => c.Count == 0)
                : perReqCategories.Values.Count(c => !c.Contains(category));
            return Math.Round((double)ok / n, 3);
        }

        var metrics = new CatalogMetrics(
            Requirements: n,
            SyntacticConformance: Share("sintaxis"), // stage 1 benchmark: >= 0.90
            WritingQuality: Share("redaccion"),
            DecisionConsistency: Share("decision"),
            Traceability: Share("trazabilidad"),
            FigureFidelity: Share("fidelidad"),
            RequirementsWithoutErrors: Share(null),
            LiteralQuotes: fragments is not null ? $"{traceOk}/{traceTotal}" : "no comprobado",
            FragmentCoverage: fragments is not null
                ? $"{docFragments.Count - uncovered.Count - unprocessed.Intersect(docFragments).Count()}/{docFragments.Count}"
                : "no comprobado",
            MasterShare: n > 0 ? Math.Round((double)real["master"] / n, 3) : null,
            Branches: new[] { "D1", "D2", "D3", "D4", "D5" }.ToDictionary(b => b, b => reqs.Count(r => r.GetProperty("decision_branch").GetString() == b)),
            OpenQuestions: questions.Count,
            DomainTerms: cat.GetProperty("domain_terms").GetArrayLength(),
            SelfCheckMatches: mismatches.Count == 0);

        return new CatalogValidationReport(errors.Count == 0, errors, warnings, metrics, texts);
    }

    // C12: consistency of decision_branch with the other fields (tree of §5).
    private static List<string> ExpectedBranchErrors(JsonElement r)
    {
        var b = Json.Str(r, "decision_branch");
        var nt = Json.Str(r, "notation");
        var tpl = Json.Str(r, "master_template");
        var er = Json.Str(r, "escalation_reason");
        var oos = Json.Str(r, "out_of_subset");
        var ob = Json.Str(r, "obligation");
        var fb = Json.Str(r, "escalation_fallback");
        var kind = Json.Str(r, "kind");
        var errs = new List<string>();

        if (fb is not null && b is not ("D1" or "D4"))
            errs.Add($"C12 escalation_fallback solo se admite en D1 o D4 (rama {b})");
        if (oos is not null && b != "D2")
            errs.Add($"C12 out_of_subset solo se admite en D2 (rama {b})");
        switch (b)
        {
            case "D1":
                if (nt == "MASTeR" && (tpl != "property" || er != "non_functional_property"))
                    errs.Add("C12 D1 exige MASTeR property con escalation_reason non_functional_property");
                if (nt == "EARS" && fb is null)
                    errs.Add("C12 D1 en EARS solo como degradación (escalation_fallback)");
                if (kind != "non_functional")
                    errs.Add("C12 D1 exige kind non_functional");
                break;
            case "D2":
                if (oos is null)
                    errs.Add("C12 D2 exige out_of_subset");
                if (kind != "non_functional")
                    errs.Add("C12 D2 exige kind non_functional");
                if (ob == "SHALL" && !(nt == "EARS" && Json.Str(r, "ears_pattern") == "ubiquitous"))
                    errs.Add("C12 D2 con SHALL exige EARS ubiquitous");
                if (ob != "SHALL" && !(nt == "MASTeR" && tpl == "functional" && Json.Str(r, "master_activity") == "autonomous"
                                       && er == "non_mandatory_obligation"))
                    errs.Add("C12 D2 sin SHALL exige MASTeR functional autonomous con non_mandatory_obligation");
                break;
            case "D3":
                if (ob == "SHALL")
                    errs.Add("C12 D3 exige obligation SHOULD o MAY");
                if (!(nt == "MASTeR" && tpl == "functional" && er == "non_mandatory_obligation"))
                    errs.Add("C12 D3 exige MASTeR functional con non_mandatory_obligation");
                break;
            case "D4":
                if (nt == "MASTeR")
                {
                    var mc = Json.Str(r, "master_condition");
                    var ok = tpl == "functional" && ((er == "data_condition" && mc == "logic") || (er == "compound_trigger" && mc == "event"));
                    if (!ok)
                        errs.Add("C12 D4 exige MASTeR functional con data_condition+logic o compound_trigger+event");
                }
                else if (fb is null)
                {
                    errs.Add("C12 D4 en EARS solo como degradación (escalation_fallback)");
                }
                break;
            case "D5":
                if (nt != "EARS")
                    errs.Add("C12 D5 exige EARS");
                break;
        }
        if (er == "non_mandatory_obligation" && ob == "SHALL")
            errs.Add("C12 non_mandatory_obligation con obligation SHALL");
        return errs;
    }

    // Free-text slots (not system nor trigger_keyword), with the states numbered.
    private static IEnumerable<(string Key, string Value)> SlotValues(JsonElement slots)
    {
        foreach (var property in slots.EnumerateObject())
        {
            if (property.Name is "system" or "trigger_keyword" || property.Value.ValueKind == JsonValueKind.Null)
                continue;
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var item in property.Value.EnumerateArray())
                    yield return ($"{property.Name}[{i++}]", item.GetString() ?? "");
            }
            else if (property.Value.ValueKind == JsonValueKind.String)
            {
                yield return (property.Name, property.Value.GetString()!);
            }
        }
    }

    // Normalizes to compare quotes: NFC, typographic quotes and dashes, spaces.
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

    private static string TrimEnd(string s) => Norm(s).TrimEnd('.', ',', ';', ':');

    // `needle` appears in `haystack` as whole words (normalized, case-insensitive).
    private static bool ContainsWords(string haystack, string needle) =>
        Regex.IsMatch(Norm(haystack).ToLowerInvariant(), $@"(?<!\w){Regex.Escape(Norm(needle).ToLowerInvariant())}(?!\w)");

    // Figures of a text, without thousand or decimal separators ("99,9" and "99.9" give "999").
    private static HashSet<string> Numbers(string s) =>
        NumberRe.Matches(s).Select(m => m.Value.Replace(".", "").Replace(",", "")).ToHashSet();

    private static string[] Words(string s) => s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static string PyBool(bool b) => b ? "True" : "False";

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
        return root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }

    private static string ReadSchema()
    {
        using var stream = typeof(CatalogValidator).Assembly.GetManifestResourceStream("requirements.schema.json")
            ?? throw new InvalidOperationException("Embedded resource not found: requirements.schema.json");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
