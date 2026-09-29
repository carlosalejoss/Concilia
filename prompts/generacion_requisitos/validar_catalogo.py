#!/usr/bin/env python3
"""
Validador determinista del catálogo EARS/MASTeR v1.1 generado por el Modelo A o el Modelo B.

Es la implementación de referencia: el port a C# debe dar los mismos códigos de error para
los mismos ficheros (ver test_validador.py --exportar).

Uso:
    python validar_catalogo.py catalogo.json --sistema "el sistema X" --fragmentos fragmentos.json
                               [--schema requirements.schema.json] [--enriquecido salida.json]

--fragmentos   fichero generado por fragmentar.py con los fragmentos del lote (y de las dudas).
               Sin él no se comprueban C6, C10, C13 ni C16.
--enriquecido  escribe una copia del catálogo con el campo `text` renderizado en cada requisito.
               El texto lo genera siempre la plataforma, nunca el modelo.

Salida: informe JSON por stdout. Código 0 si no hay errores (los avisos no bloquean), 1 si los hay.
El runner devuelve `errors` al modelo en el reintento (anexo de reparación).

Cada error empieza por su código (C2, C5, C12...) para que las métricas se puedan agrupar.
"""
import argparse
import json
import re
import sys
import unicodedata
from pathlib import Path

try:
    import jsonschema
except ImportError:  # pragma: no cover
    sys.exit("Falta jsonschema: pip install jsonschema")

KW = {
    "es": {"WHEN": "CUANDO", "WHILE": "MIENTRAS", "IF": "SI", "THEN": "ENTONCES", "WHERE": "DONDE",
           "SHALL": "DEBERÁ", "SHOULD": "DEBERÍA", "MAY": "PODRÁ",
           "PROVIDE_PRE": "PROPORCIONAR A", "PROVIDE_POST": "LA CAPACIDAD DE", "ABLE": "SER CAPAZ DE", "BE": "SER",
           "C_logic": "SI", "C_event": "EN CUANTO", "C_time": "MIENTRAS", "AND": "y"},
    "en": {"WHEN": "WHEN", "WHILE": "WHILE", "IF": "IF", "THEN": "THEN", "WHERE": "WHERE",
           "SHALL": "SHALL", "SHOULD": "SHOULD", "MAY": "MAY",
           "PROVIDE_PRE": "PROVIDE", "PROVIDE_POST": "WITH THE ABILITY TO", "ABLE": "BE ABLE TO", "BE": "BE",
           "C_logic": "IF", "C_event": "AS SOON AS", "C_time": "AS LONG AS", "AND": "and"},
}

# Palabras de keyword que no pueden aparecer en MAYÚSCULAS dentro de un slot (R-b).
KW_WORDS = {
    "es": {"CUANDO", "MIENTRAS", "SI", "ENTONCES", "DONDE", "DEBERÁ", "DEBERÍA", "PODRÁ", "PROPORCIONAR",
           "CAPACIDAD", "CAPAZ", "CUANTO", "SER"},
    "en": {"WHEN", "WHILE", "IF", "THEN", "WHERE", "SHALL", "SHOULD", "MAY", "PROVIDE", "ABILITY", "ABLE",
           "SOON", "LONG", "BE"},
}

# C5: categorías de lenguaje ambiguo de ISO/IEC/IEEE 29148 (listas propias del proyecto).
VAGUE = {
    "es": {
        "subjetivo": [r"f[aá]cil(es|mente)?", r"sencill[oa]s?", r"sencillamente", r"intuitiv[oa]s?", r"amigables?",
                      r"c[oó]mod[oa]s?", r"agradables?", r"atractiv[oa]s?", r"modern[oa]s?", r"user-friendly"],
        "cualidad_sin_cifra": [r"r[aá]pid[oa]s?", r"r[aá]pidamente", r"lent[oa]s?", r"eficientes?", r"eficientemente",
                               r"eficaz(mente)?", r"eficaces", r"robust[oa]s?", r"flexibles?", r"segur[oa]s?",
                               r"escalables?", r"[oó]ptim[oa]s?", r"adecuad[oa]s?", r"adecuadamente",
                               r"apropiad[oa]s?", r"suficientes?", r"razonables?"],
        "superlativo_comparativo": [r"mejor(es)?", r"mejorad[oa]s?", r"lo antes posible", r"m[aá]ximo posible",
                                    r"m[ií]nimo posible"],
        "ambiguo": [r"aproximadamente", r"normalmente", r"generalmente", r"habitualmente", r"casi siempre",
                    r"significativ[oa]s?", r"significativamente", r"considerables?", r"considerablemente"],
        "abierto": [r"etc\.?", r"y/o", r"entre otr[oa]s", r"tales como"],
        "escape": [r"si es posible", r"en la medida de lo posible", r"cuando proceda", r"si procede",
                   r"cuando sea necesario", r"seg[uú]n sea necesario", r"seg[uú]n corresponda", r"si aplica"],
        "pronombre_vago": [r"lo anterior", r"lo siguiente", r"lo mismo", r"esto", r"ello"],
    },
    "en": {
        "subjetivo": [r"easy", r"easily", r"simple", r"intuitive", r"user-friendly", r"friendly", r"comfortable",
                      r"attractive", r"modern"],
        "cualidad_sin_cifra": [r"fast", r"quick(ly)?", r"slow(ly)?", r"efficient(ly)?", r"effective(ly)?", r"robust",
                               r"flexible", r"secure(ly)?", r"scalable", r"optimal", r"adequate(ly)?",
                               r"appropriate(ly)?", r"sufficient(ly)?", r"reasonable"],
        "superlativo_comparativo": [r"the best", r"better", r"improved", r"as soon as possible",
                                    r"as much as possible"],
        "ambiguo": [r"approximately", r"normally", r"generally", r"usually", r"almost always",
                    r"significant(ly)?", r"considerable", r"considerably"],
        "abierto": [r"etc\.?", r"and/or", r"among others", r"such as", r"but not limited to"],
        "escape": [r"if possible", r"as far as possible", r"where appropriate", r"as appropriate", r"if applicable",
                   r"when necessary", r"as needed"],
        "pronombre_vago": [r"the above", r"the aforementioned", r"the former", r"the latter"],
    },
}

TBD = ("[POR DEFINIR]", "[TBD]")
SUBJECT_PREPS = {"es": {"de", "del", "desde", "en", "para", "durante"},
                 "en": {"of", "from", "for", "in", "during"}}
NEGATION = {"es": re.compile(r"^no\b", re.IGNORECASE), "en": re.compile(r"^not?\b", re.IGNORECASE)}
ES_INFINITIVE = re.compile(r"^\w+(ar|er|ir|ír)(se|lo|la|le|los|las|les|nos)?$", re.IGNORECASE)

EARS_SHAPE = {  # patrón -> (feature, states, trigger) presentes
    "ubiquitous": (False, False, False),
    "event_driven": (False, False, True),
    "state_driven": (False, True, False),
    "unwanted_behaviour": (False, False, True),
    "optional_feature": (True, False, False),
}

# Categoría de cada código, para las métricas por requisito.
CATEGORY = {"C2": "sintaxis", "C3": "sintaxis", "C14": "sintaxis",
            "C5": "redaccion", "C15": "redaccion",
            "C7": "decision", "C12": "decision",
            "C6": "trazabilidad", "C13": "trazabilidad", "C16": "trazabilidad",
            "C11": "referencias", "N3": "referencias"}

QUOTE_MAP = str.maketrans({"“": '"', "”": '"', "„": '"', "«": '"', "»": '"', "‘": "'", "’": "'",
                           "–": "-", "—": "-", " ": " "})


def norm(s: str) -> str:
    """Normaliza para comparar citas: NFC, comillas y guiones tipográficos, espacios."""
    s = unicodedata.normalize("NFC", s).translate(QUOTE_MAP)
    return re.sub(r"\s+", " ", s).strip()


def cap(s: str) -> str:
    return s[:1].upper() + s[1:] if s else s


def join_list(items: list[str], lang: str) -> str:
    if len(items) <= 1:
        return "".join(items)
    return ", ".join(items[:-1]) + f" {KW[lang]['AND']} " + items[-1]


def render(req: dict, lang: str) -> str:
    """Genera el texto del requisito a partir de los slots (gramática de §4 y §5 del prompt)."""
    k, s = KW[lang], req["slots"]
    if req["notation"] == "EARS":
        parts = []
        if s.get("feature"):
            parts.append(f"{k['WHERE']} {s['feature']}, ")
        if s.get("states"):
            parts.append(f"{k['WHILE']} {join_list(s['states'], lang)}, ")
        if s.get("trigger"):
            if s.get("trigger_keyword") == "IF":
                parts.append(f"{k['IF']} {s['trigger']}, {k['THEN']} ")
            else:
                parts.append(f"{k['WHEN']} {s['trigger']}, ")
        return cap("".join(parts) + f"{s['system']} {k['SHALL']} {s['response']}.")

    liab = k[req["obligation"]]
    cond = ""
    if req.get("master_condition") not in (None, "none") and s.get("condition"):
        cond = f"{k['C_' + req['master_condition']]} {s['condition']}, "
    if req["master_template"] == "functional":
        act = req.get("master_activity")
        mid = ""
        if act == "user_interaction":
            mid = f"{k['PROVIDE_PRE']} {s['actor']} {k['PROVIDE_POST']} "
        elif act == "interface":
            mid = f"{k['ABLE']} "
        details = f" {s['details']}" if s.get("details") else ""
        out = cap(f"{cond}{s['system']} {liab} {mid}{s['process_verb']} {s['object']}{details}.")
        if lang == "es":
            out = out.replace("PROPORCIONAR A el ", "PROPORCIONAR AL ")
        return out
    q = f"{s['qualifying_expression']} " if s.get("qualifying_expression") else ""
    return cap(f"{cond}{s['characteristic']} {s['subject_matter']} {liab} {k['BE']} {q}{s['value']}.")


def slot_values(req: dict) -> dict[str, str]:
    """Slots de texto libre (sin system ni trigger_keyword), con los estados numerados."""
    out = {}
    for key, val in req["slots"].items():
        if key in ("system", "trigger_keyword") or val is None:
            continue
        if isinstance(val, list):
            for i, v in enumerate(val):
                out[f"{key}[{i}]"] = v
        else:
            out[key] = val
    return out


def expected_branch_errors(r: dict) -> list[str]:
    """C12: coherencia de decision_branch con el resto de campos (árbol §5)."""
    b, nt, tpl = r["decision_branch"], r["notation"], r["master_template"]
    er, oos, ob, fb = r["escalation_reason"], r["out_of_subset"], r["obligation"], r["escalation_fallback"]
    errs = []
    if fb is not None and b not in ("D1", "D4"):
        errs.append(f"C12 escalation_fallback solo se admite en D1 o D4 (rama {b})")
    if oos is not None and b != "D2":
        errs.append(f"C12 out_of_subset solo se admite en D2 (rama {b})")
    if b == "D1":
        if nt == "MASTeR" and (tpl != "property" or er != "non_functional_property"):
            errs.append("C12 D1 exige MASTeR property con escalation_reason non_functional_property")
        if nt == "EARS" and fb is None:
            errs.append("C12 D1 en EARS solo como degradación (escalation_fallback)")
        if r["kind"] != "non_functional":
            errs.append("C12 D1 exige kind non_functional")
    elif b == "D2":
        if oos is None:
            errs.append("C12 D2 exige out_of_subset")
        if r["kind"] != "non_functional":
            errs.append("C12 D2 exige kind non_functional")
        if ob == "SHALL" and not (nt == "EARS" and r["ears_pattern"] == "ubiquitous"):
            errs.append("C12 D2 con SHALL exige EARS ubiquitous")
        if ob != "SHALL" and not (nt == "MASTeR" and tpl == "functional" and r["master_activity"] == "autonomous"
                                  and er == "non_mandatory_obligation"):
            errs.append("C12 D2 sin SHALL exige MASTeR functional autonomous con non_mandatory_obligation")
    elif b == "D3":
        if ob == "SHALL":
            errs.append("C12 D3 exige obligation SHOULD o MAY")
        if not (nt == "MASTeR" and tpl == "functional" and er == "non_mandatory_obligation"):
            errs.append("C12 D3 exige MASTeR functional con non_mandatory_obligation")
    elif b == "D4":
        if nt == "MASTeR":
            ok = tpl == "functional" and ((er == "data_condition" and r["master_condition"] == "logic")
                                          or (er == "compound_trigger" and r["master_condition"] == "event"))
            if not ok:
                errs.append("C12 D4 exige MASTeR functional con data_condition+logic o compound_trigger+event")
        elif fb is None:
            errs.append("C12 D4 en EARS solo como degradación (escalation_fallback)")
    elif b == "D5":
        if nt != "EARS":
            errs.append("C12 D5 exige EARS")
    if er == "non_mandatory_obligation" and ob == "SHALL":
        errs.append("C12 non_mandatory_obligation con obligation SHALL")
    return errs


def check(cat: dict, sistema: str, fragments: dict | None, schema: dict) -> dict:
    errors, warnings = [], []
    schema_errors = sorted(jsonschema.Draft202012Validator(schema).iter_errors(cat), key=lambda e: list(e.path))
    for e in schema_errors:
        errors.append(f"SCHEMA {'/'.join(map(str, e.path)) or '(raíz)'}: {e.message}")
    if schema_errors:
        return {"valid": False, "errors": errors, "warnings": warnings, "metrics": {}, "texts": {}}

    lang = cat["language"]
    reqs, questions = cat["requirements"], cat["open_questions"]
    req_ids = [r["id"] for r in reqs]
    q_by_id = {q["id"]: q for q in questions}
    texts = {}
    per_req_categories: dict[str, set] = {}
    found = {"quotes": True, "fragments": True, "vague": True, "ears_shall": True, "tree": True}

    if len(set(req_ids)) != len(req_ids):
        errors.append("C9 ids de requisito duplicados")
    if len(set(q_by_id)) != len(questions):
        errors.append("C9 ids de pregunta duplicados")
    if req_ids != [f"REQ-{i:03d}" for i in range(1, len(reqs) + 1)]:
        warnings.append("C9 ids de requisito no consecutivos desde REQ-001")

    referenced: set[str] = set()
    trace_total, trace_ok = 0, 0

    def check_trace(owner: str, t: dict, local: list):
        nonlocal trace_total, trace_ok
        referenced.add(t["fragment"])
        if fragments is None or t.get("quote") is None:
            return
        trace_total += 1
        frag = fragments.get(t["fragment"])
        if frag is None:
            local.append(f"C6 fragmento inexistente {t['fragment']}")
            found["quotes"] = False
        elif norm(t["quote"]) not in norm(frag["text"]):
            local.append(f"C6 cita no literal en {t['fragment']}: «{t['quote']}»")
            found["quotes"] = False
        else:
            trace_ok += 1

    for r in reqs:
        rid, s, local = r["id"], r["slots"], []
        # C2 patrón <-> slots
        if r["notation"] == "EARS":
            p = r["ears_pattern"]
            has = (bool(s.get("feature")), bool(s.get("states")), bool(s.get("trigger")))
            if p in EARS_SHAPE and has != EARS_SHAPE[p]:
                local.append(f"C2 slots (feature, states, trigger) = {has} incompatibles con {p}")
            if p == "complex" and sum(has) < 2:
                local.append("C2 complex exige al menos dos tipos de {feature, states, trigger}")
            if p == "unwanted_behaviour" and s.get("trigger_keyword") != "IF":
                local.append("C2 unwanted_behaviour exige trigger_keyword IF")
            if p == "event_driven" and s.get("trigger_keyword") != "WHEN":
                local.append("C2 event_driven exige trigger_keyword WHEN")
            if bool(s.get("trigger")) != bool(s.get("trigger_keyword")):
                local.append("C2 trigger y trigger_keyword deben ir juntos")
            for slot in ("trigger", "feature"):
                if s.get(slot) and re.search(r"\b(y|o|and|or)\b", s[slot]):
                    warnings.append(f"{rid} condición posiblemente compuesta en '{slot}' (revisar G2/D4): {s[slot]}")
            for i, st in enumerate(s.get("states", [])):
                if re.search(r"\b(o|or)\b", st):
                    warnings.append(f"{rid} estado con 'o' en states[{i}] (revisar G2): {st}")
        else:
            mc, cond = r["master_condition"], s.get("condition")
            if (mc == "none") != (cond is None):
                local.append("C2 master_condition y slot condition incoherentes")
            if r["master_template"] == "functional":
                if (r["master_activity"] == "user_interaction") != bool(s.get("actor")):
                    local.append("C2 actor obligatorio solo y siempre en user_interaction")
            else:
                first = s["subject_matter"].split()[0].lower() if s["subject_matter"].split() else ""
                if first not in SUBJECT_PREPS[lang]:
                    local.append(f"C14 subject_matter debe empezar por {sorted(SUBJECT_PREPS[lang])}: '{s['subject_matter']}'")
        # C3 sistema
        if "system" in s and s["system"] != sistema:
            local.append(f"C3 slots.system '{s['system']}' ≠ '{sistema}'")
        # C14 formato de slots (R-a, R-b, R-c) y C15 negación (R-e)
        for key, val in slot_values(r).items():
            if key == "value" and val in TBD:
                continue
            if re.search(r"[.,;:]\s*$", val):
                local.append(f"C14 slot '{key}' termina en signo de puntuación (R-a)")
            upper_kw = [w for w in re.findall(r"\b[A-ZÁÉÍÓÚÑ]{2,}\b", val) if w in KW_WORDS[lang]]
            if upper_kw:
                local.append(f"C14 slot '{key}' contiene keywords en mayúsculas {upper_kw} (R-b)")
            if val[:1].isupper() and not re.match(r"^[A-ZÁÉÍÓÚÑ0-9]{2,}\b", val):
                warnings.append(f"{rid} slot '{key}' empieza en mayúscula; ¿es nombre propio? (R-a)")
        verb_slot = s.get("response") if r["notation"] == "EARS" else s.get("process_verb")
        if verb_slot:
            if NEGATION[lang].match(verb_slot):
                local.append(f"C15 prohibición en negativo en '{verb_slot}' (R-e: usa impedir, rechazar, bloquear…)")
            if lang == "es" and not ES_INFINITIVE.match(verb_slot.split()[0]):
                warnings.append(f"{rid} el slot de respuesta no parece empezar por infinitivo (R-c): {verb_slot}")
        # Render (lo hace siempre la plataforma)
        try:
            texts[rid] = render(r, lang)
        except (KeyError, TypeError) as ex:
            local.append(f"C2 no se pudo renderizar: {ex}")
            texts[rid] = None
        # C5 términos vagos, sobre el texto renderizado
        if texts.get(rid):
            txt = texts[rid]
            for t in TBD:
                txt = txt.replace(t, "")
            if r["master_template"] == "property" and s.get("value") in TBD:
                txt = txt.replace(s["characteristic"], "")  # N3: la característica nominalizada se admite
            for category, pats in VAGUE[lang].items():
                for pat in pats:
                    if re.search(rf"(?<!\w){pat}(?!\w)", txt, flags=re.IGNORECASE):
                        local.append(f"C5 término vago ({category}) '{pat}'")
                        found["vague"] = False
        # C7 obligatoriedad
        if r["notation"] == "EARS" and r["obligation"] != "SHALL":
            local.append("C7 EARS con obligación distinta de SHALL")
            found["ears_shall"] = False
        # C12 árbol de decisión
        branch_errs = expected_branch_errors(r)
        if branch_errs:
            found["tree"] = False
        local += branch_errs
        # C6 citas
        for t in r["source_trace"]:
            check_trace(rid, t, local)
        # C13 evidencia de obligatoriedad y C16 origen
        if fragments is not None:
            cited = [fragments[t["fragment"]] for t in r["source_trace"] if t["fragment"] in fragments]
            if r["obligation_basis"] == "explicit":
                ev = norm(r["obligation_evidence"]).lower()
                if not any(ev in norm(f["text"]).lower() for f in cited):
                    local.append(f"C13 obligation_evidence '{r['obligation_evidence']}' no aparece en los fragmentos citados")
            kinds = {f["kind"] for f in cited}
            if r["origin"] == "explicit" and "document" not in kinds:
                local.append("C16 origin explicit sin ningún fragmento de documento citado")
            if r["origin"] == "clarification" and "clarification" not in kinds:
                local.append("C16 origin clarification sin ninguna duda resuelta citada")
        # C11 referencias y N3
        for q in r["open_question_ids"]:
            if q not in q_by_id:
                local.append(f"C11 open_question_id inexistente {q}")
            elif rid not in q_by_id[q]["related_requirements"]:
                local.append(f"C11 {q} no incluye {rid} en related_requirements (vínculo no bidireccional)")
        for d in r["derived_from"]:
            if d not in req_ids:
                local.append(f"C11 derived_from inexistente {d}")
        if r["master_template"] == "property" and s.get("value") in TBD:
            if not any(q_by_id.get(q, {}).get("type") == "missing_value" for q in r["open_question_ids"]):
                local.append("N3 valor pendiente sin pregunta missing_value asociada")
        errors += [f"{rid} {m}" for m in local]
        per_req_categories[rid] = {CATEGORY.get(m.split()[0], "otros") for m in local}

    for q in questions:
        local = []
        for rr in q["related_requirements"]:
            if rr not in req_ids:
                local.append(f"C11 related_requirement inexistente {rr}")
            elif q["id"] not in next(r for r in reqs if r["id"] == rr)["open_question_ids"]:
                local.append(f"C11 {rr} no incluye {q['id']} en open_question_ids (vínculo no bidireccional)")
        if not q["related_requirements"] and q["type"] != "scope":
            warnings.append(f"{q['id']} pregunta {q['type']} sin requisitos relacionados")
        for t in q["source_trace"]:
            check_trace(q["id"], t, local)
        errors += [f"{q['id']} {m}" for m in local]
    for i, d in enumerate(cat["discarded_statements"]):
        local = []
        if fragments is not None and d["fragment"] not in fragments:
            local.append(f"C6 fragmento inexistente {d['fragment']}")
        check_trace(f"DISCARDED[{i}]", d, local)
        errors += [f"DISCARDED[{i}] {m}" for m in local]

    # C10 cobertura de fragmentos del documento
    doc_frags = [fid for fid, f in (fragments or {}).items() if f["kind"] == "document"]
    unprocessed = set(cat["verdict"]["unprocessed_fragments"])
    uncovered = [fid for fid in doc_frags if fid not in referenced and fid not in unprocessed]
    if fragments is not None and uncovered:
        errors.append(f"C10 fragmentos sin tratar: {', '.join(uncovered)}")
        found["fragments"] = False
    if unprocessed:
        warnings.append(f"C10 fragmentos declarados sin procesar: {', '.join(sorted(unprocessed))}")

    # Coherencia del veredicto
    v = cat["verdict"]
    real = {"requirements": len(reqs), "ears": sum(r["notation"] == "EARS" for r in reqs),
            "master": sum(r["notation"] == "MASTeR" for r in reqs), "open_questions": len(questions),
            "discarded": len(cat["discarded_statements"])}
    for key, val in real.items():
        if v["counts"][key] != val:
            errors.append(f"VERDICT counts.{key}={v['counts'][key]} pero hay {val}")
    if v["status"] == "complete" and v["unprocessed_fragments"]:
        errors.append("VERDICT status complete con unprocessed_fragments no vacío")
    if v["status"] == "partial" and not v["unprocessed_fragments"]:
        errors.append("VERDICT status partial sin unprocessed_fragments")

    # Autoverificación declarada frente a la del validador (métrica, no bloquea)
    sc = v["self_check"]
    declared_vs_found = {
        "quotes_are_literal": found["quotes"], "all_fragments_accounted": found["fragments"],
        "no_vague_terms": found["vague"], "ears_only_shall": found["ears_shall"],
        "decision_tree_consistent": found["tree"],
    }
    mismatches = [k for k, real_ok in declared_vs_found.items() if sc[k] and not real_ok]
    for k in mismatches:
        warnings.append(f"SELF_CHECK el modelo declara {k}=true y el validador encuentra fallos")

    n = len(reqs)

    def share(category: str | None):
        if not n:
            return None
        if category is None:
            return round(sum(not cats for cats in per_req_categories.values()) / n, 3)
        return round(sum(category not in cats for cats in per_req_categories.values()) / n, 3)

    metrics = {
        "requisitos": n,
        "conformidad_sintactica": share("sintaxis"),   # benchmark Etapa 1: ≥ 0,90
        "calidad_redaccion": share("redaccion"),
        "coherencia_decision": share("decision"),
        "trazabilidad": share("trazabilidad"),
        "requisitos_sin_errores": share(None),
        "citas_literales": f"{trace_ok}/{trace_total}" if fragments is not None else "no comprobado",
        "cobertura_fragmentos": (f"{len(doc_frags) - len(uncovered) - len(unprocessed & set(doc_frags))}/{len(doc_frags)}"
                                 if fragments is not None else "no comprobado"),
        "proporcion_master": round(real["master"] / n, 3) if n else None,
        "ramas": {b: sum(r["decision_branch"] == b for r in reqs) for b in ("D1", "D2", "D3", "D4", "D5")},
        "preguntas_abiertas": len(questions),
        "autoverificacion_coincide": not mismatches,
    }
    return {"valid": not errors, "errors": errors, "warnings": warnings, "metrics": metrics, "texts": texts}


def load_fragments(path: str) -> dict:
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    return {f["id"]: f for f in data["fragments"]}


def enrich(cat: dict, texts: dict) -> dict:
    """Copia del catálogo con `text` tras `slots` en cada requisito."""
    out = json.loads(json.dumps(cat))
    new_reqs = []
    for r in out["requirements"]:
        nr = {}
        for k, val in r.items():
            nr[k] = val
            if k == "slots":
                nr["text"] = texts.get(r["id"])
        new_reqs.append(nr)
    out["requirements"] = new_reqs
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("catalogo")
    ap.add_argument("--sistema", required=True)
    ap.add_argument("--fragmentos")
    ap.add_argument("--schema", default=str(Path(__file__).with_name("requirements.schema.json")))
    ap.add_argument("--enriquecido")
    a = ap.parse_args()
    try:
        cat = json.loads(Path(a.catalogo).read_text(encoding="utf-8"))
    except json.JSONDecodeError as ex:
        print(json.dumps({"valid": False, "errors": [f"JSON no válido: {ex}"], "warnings": [], "metrics": {}},
                         ensure_ascii=False, indent=2))
        sys.exit(1)
    schema = json.loads(Path(a.schema).read_text(encoding="utf-8"))
    fragments = load_fragments(a.fragmentos) if a.fragmentos else None
    report = check(cat, a.sistema, fragments, schema)
    if a.enriquecido and report["texts"]:
        Path(a.enriquecido).write_text(json.dumps(enrich(cat, report["texts"]), ensure_ascii=False, indent=2) + "\n",
                                       encoding="utf-8")
    report.pop("texts")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    sys.exit(0 if report["valid"] else 1)


if __name__ == "__main__":
    main()
