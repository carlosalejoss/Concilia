using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static ReqArbiter.Core.Catalog.CatalogJson;

namespace ReqArbiter.Core.Catalog;

// What every version of the requirements catalog (v1.3 and v2) checks in the
// same way: the normalization of one requirement and its own
// checks (C2, C5-C7, C12-C16, C19, C22, N2, N3), the quotes (C6) and the text
// helpers. Each validator (CatalogValidator, CatalogValidatorV2) adds the
// checks of its version around these. The reference is
// prompts/generacion_requisitos/validar_catalogo.py: messages and their order
// must stay the same as there.
internal static class RequirementChecks
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
    public static readonly string[] Labels = ["OPERATION", "DATA", "RULE", "LIMIT", "MESSAGE", "ERROR"];

    // A label goes in brackets ("[DATA]", "[data]:") or before a colon ("DATA:").
    // Without that it is no label: "Error messages are shown..." is an unlabelled criterion (C19).
    private static readonly Regex LabelRe = new(
        $@"^\s*(?:\[\s*({string.Join("|", Labels)})\s*\]\s*:?|({string.Join("|", Labels)})\s*:)\s*", IgnoreCase);

    public const int QuoteMinWords = 3; // C6: a shorter quote is only valid if it is the whole fragment
    private static readonly Regex NumberRe = new(@"\d+(?:[.,]\d+)*");
    public static readonly string[] Tbd = ["[POR DEFINIR]", "[TBD]"];

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

    private static readonly Dictionary<char, char> QuoteMap = new()
    {
        ['“'] = '"', ['”'] = '"', ['„'] = '"', ['«'] = '"', ['»'] = '"', ['‘'] = '\'', ['’'] = '\'',
        ['–'] = '-', ['—'] = '-', [' '] = ' ',
    };

    // --- Normalizer ---------------------------------------------------------------------------

    // Domain terms (N7): a badly declared one is ignored with a warning and stops
    // protecting its word from C5. Returns the terms that count.
    public static List<string> DomainTerms(JsonArray items, IReadOnlyDictionary<string, Fragment>? fragments, List<string> warnings)
    {
        var terms = new List<string>();
        for (var i = 0; i < items.Count; i++)
        {
            var term = Str(items[i]!, "term")!;
            var fragmentId = Str(items[i]!, "fragment")!;
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
                terms.Add(term);
        }
        return terms;
    }

    // A quote in the wrong fragment but literal in a single fragment of the lot:
    // the id is corrected. Repeated entries are removed.
    public static void NormalizeTraces(
        JsonObject owner, string ownerId, IReadOnlyDictionary<string, Fragment>? fragments,
        IReadOnlyList<Fragment> lotFragments, Action<string, string> fix)
    {
        var seen = new HashSet<(string, string)>();
        var kept = new JsonArray();
        foreach (var t in owner["source_trace"]!.AsArray().Select(t => t!.AsObject()))
        {
            var fragmentId = Str(t, "fragment")!;
            var quote = Str(t, "quote")!;
            if (fragments is not null && (!fragments.TryGetValue(fragmentId, out var frag) || !QuoteIn(quote, frag.Text)))
            {
                var found = lotFragments.Where(f => QuoteIn(quote, f.Text)).Select(f => f.Id).ToList();
                if (found.Count == 1)
                {
                    fix(ownerId, $"cita movida de {fragmentId} a {found[0]}, donde es literal");
                    fragmentId = found[0];
                    t["fragment"] = fragmentId;
                }
            }
            if (!seen.Add((fragmentId, QuoteKey(quote))))
            {
                fix(ownerId, $"cita repetida de {fragmentId} quitada");
                continue;
            }
            kept.Add(t.DeepClone());
        }
        owner["source_trace"] = kept;
    }

    // One requirement: its quotes, slots.system, the slots (spaces, final
    // punctuation, leading keyword) and the labels of its criteria.
    public static void NormalizeRequirement(
        JsonObject r, string id, string sistema, string lang, IReadOnlyDictionary<string, Fragment>? fragments,
        IReadOnlyList<Fragment> lotFragments, Action<string, string> fix)
    {
        var s = r["slots"]!.AsObject();
        NormalizeTraces(r, id, fragments, lotFragments, fix);
        if (s["system"] is JsonValue systemValue && systemValue.GetValue<string>() != sistema)
        {
            fix(id, $"slots.system '{systemValue.GetValue<string>()}' sustituido por '{sistema}'");
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
                fix(id, $"slot '{key}': espacios o puntuación final quitados");
            if (LeadingKeywordSlots.Contains(key))
            {
                var stripped = cleaned.Select(v => LeadingKeyword[lang].Replace(v, "", 1)).ToList();
                foreach (var (before, after) in cleaned.Zip(stripped))
                    if (after != before)
                        fix(id, $"slot '{key}': palabra clave inicial «{before[..(before.Length - after.Length)].Trim()}» quitada (R-h)");
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
                fix(id, $"etiqueta de criterio normalizada: «{Truncate(crit, 40)}»");
            criteria.Add(fixedCriterion);
        }
        r["acceptance_criteria"] = criteria;
    }

    // --- Checks of each element -------------------------------------------------------------

    // C6 (rejection) of already normalized quotes, counting them for the metrics.
    public sealed class TraceChecker(IReadOnlyDictionary<string, Fragment>? fragments)
    {
        public int Total { get; private set; }
        public int Ok { get; private set; }
        public bool AllLiteral { get; private set; } = true;

        public List<string> Check(JsonObject t)
        {
            var problems = new List<string>();
            if (fragments is null)
                return problems;
            Total++;
            var fragmentId = Str(t, "fragment")!;
            var quote = Str(t, "quote")!;
            if (!fragments.TryGetValue(fragmentId, out var frag))
                problems.Add($"C6 fragmento inexistente {fragmentId}");
            else if (!QuoteIn(quote, frag.Text))
                problems.Add($"C6 cita no literal en {fragmentId}: «{quote}»");
            else if (Words(quote).Length < QuoteMinWords && TrimEnd(quote) != TrimEnd(frag.Text))
                problems.Add($"C6 cita de menos de {QuoteMinWords} palabras que no es el fragmento entero: «{quote}»");
            else
                Ok++;
            if (problems.Count > 0)
                AllLiteral = false;
            return problems;
        }
    }

    // What the checks found in the whole catalog (for the self-check of v1.3).
    public sealed class Findings
    {
        public bool Figures { get; set; } = true;
        public bool Vague { get; set; } = true;
        public bool EarsShall { get; set; } = true;
        public bool Tree { get; set; } = true;
    }

    // The checks of one requirement that passed the schema. Errors reject it,
    // warnings do not. questionType gives the type of an open question by the id
    // the requirement uses (N3). Returns the rendered text, or null if it cannot
    // be rendered.
    public static string? CheckRequirement(
        JsonObject r, string lang, List<string> domainTerms, IReadOnlyDictionary<string, Fragment>? fragments,
        TraceChecker traces, Func<string, string?> questionType, Findings found, List<string> errs, List<string> warns)
    {
        var s = r["slots"]!.AsObject();
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
        string? text;
        try
        {
            text = CatalogRenderer.Render(JsonSerializer.SerializeToElement(r), lang);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            errs.Add($"C2 no se pudo renderizar: {ex.Message}");
            text = null;
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
        var vague = text is not null ? VagueTerms(text, lang, domainTerms, exempt) : [];
        foreach (var body in criteriaTexts)
            vague.AddRange(VagueTerms(body, lang, domainTerms, null).Select(v => $"{v} en un criterio"));
        if (vague.Count > 0)
        {
            errs.AddRange(vague.Distinct());
            found.Vague = false;
        }

        // C7 obligation
        if (notation == "EARS" && Str(r, "obligation") != "SHALL")
        {
            errs.Add("C7 EARS con obligación distinta de SHALL");
            found.EarsShall = false;
        }

        // C12 decision tree
        var tree = BranchErrors(r);
        if (tree.Count > 0)
        {
            found.Tree = false;
            errs.AddRange(tree);
        }

        // C6 quotes
        var traceList = r["source_trace"]!.AsArray().Select(x => x!.AsObject()).ToList();
        foreach (var t in traceList)
            errs.AddRange(traces.Check(t));

        if (fragments is not null)
        {
            var cited = traceList.Select(t => Str(t, "fragment")!).Where(fragments.ContainsKey).Select(id => fragments[id]).ToList();
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
                    found.Figures = false;
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
            if (!StrList(r, "open_question_ids").Any(q => questionType(q) == "missing_value"))
                errs.Add("N3 valor pendiente sin pregunta missing_value asociada");
        }

        // C22 umbrella verb with fewer than five [OPERATION]
        if (!string.IsNullOrEmpty(verbSlot) && UmbrellaRe[lang].IsMatch(verbSlot))
        {
            var operations = criteria.Count(crit => SplitLabel(crit).Label == "OPERATION");
            if (operations < UmbrellaMinOperations)
                warns.Add($"C22 verbo paraguas en '{verbSlot}' con {operations} criterios [OPERATION] (U7)");
        }
        return text;
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

    // ("DATA", "text") if the criterion starts with a valid label, or (null, criterion).
    public static (string? Label, string Text) SplitLabel(string criterion)
    {
        var m = LabelRe.Match(criterion);
        if (!m.Success)
            return (null, criterion.Trim());
        var label = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
        return (label.ToUpperInvariant(), criterion[m.Length..].Trim());
    }

    // --- Sections -------------------------------------------------------------------------------

    // Numeric label of a section ("§2.1" -> "2.1", "3. Roles" -> "3"), or null.
    private static string? SectionKey(string section) =>
        Regex.Match(section, @"^\s*§?\s*(\d+(?:\.\d+)*)") is { Success: true } m ? m.Groups[1].Value : null;

    // The fragment belongs to the section or to one of its subsections.
    public static bool InSection(string fragmentSection, string section)
    {
        if (Norm(fragmentSection) == Norm(section))
            return true;
        var fk = SectionKey(fragmentSection);
        var sk = SectionKey(section);
        return fk is not null && sk is not null && (fk == sk || fk.StartsWith(sk + ".", StringComparison.Ordinal));
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
    public static string QuoteKey(string s) =>
        Regex.Replace(Regex.Replace(Norm(s), "[`*_]", ""), @"\s+", " ").Trim().ToLowerInvariant();

    public static bool QuoteIn(string quote, string text) => QuoteKey(text).Contains(QuoteKey(quote), StringComparison.Ordinal);

    private static string TrimEnd(string s) => QuoteKey(s).TrimEnd('.', ',', ';', ':');

    private static string CleanSlot(string value) =>
        Tbd.Contains(value) ? value : value.Trim().TrimEnd('.', ',', ';', ':').TrimEnd();

    // `needle` appears in `haystack` as whole words (normalized, case-insensitive).
    private static bool ContainsWords(string haystack, string needle) =>
        Regex.IsMatch(Norm(haystack).ToLowerInvariant(), $@"(?<!\w){Regex.Escape(Norm(needle).ToLowerInvariant())}(?!\w)");

    // Figures of a text, without thousand or decimal separators ("99,9" and "99.9" give "999").
    private static HashSet<string> Numbers(string s) =>
        NumberRe.Matches(s).Select(m => m.Value.Replace(".", "").Replace(",", "")).ToHashSet();

    public static string[] Words(string s) => s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static string Truncate(string s, int length) => s.Length <= length ? s : s[..length];

    private static string PyBool(bool b) => b ? "True" : "False";
}
