#!/usr/bin/env python3
"""
Validador determinista del catálogo EARS/MASTeR v1.3 generado por el Modelo A o el Modelo B.

Es la implementación de referencia: el port a C# debe dar los mismos resultados para los mismos
ficheros (ver test_validador.py --exportar). La versión 1.2 está en el historial de git.

Uso:
    python validar_catalogo.py catalogo.json --sistema "el sistema X" --fragmentos fragmentos.json
                               [--schema requirements_v1.3.schema.json] [--enriquecido salida.json]

--fragmentos   fichero de fragmentar.py con los fragmentos del lote (y de las dudas). Sin él no se
               comprueban las citas, las cifras, la cobertura ni los términos de dominio.
--enriquecido  escribe el catálogo aceptado: normalizado, sin los elementos rechazados, renumerado y
               con el campo `text` de cada requisito. El texto lo genera siempre la plataforma.

Qué hace, en orden:
1. Esquema. Un fallo en la estructura raíz es un error del lote. Un fallo dentro de un requisito o de una
   pregunta rechaza ese elemento; dentro de otro elemento (funcionalidad, descarte, término), se ignora
   ese elemento con un aviso.
2. Normalizador: corrige lo mecánico y lo anota como aviso FIX (puntuación final de los slots,
   slots.system, etiquetas de los criterios, citas con el fragmento equivocado o repetidas, vínculos en un
   solo sentido, referencias a ids que no existen).
3. Comprobaciones por elemento. Un error rechaza el requisito o la pregunta; un aviso no hace nada.
4. Renumeración por tipo (RF-001, RNF-001…, Q-001, CAP-001) del catálogo aceptado.
5. Comprobaciones del lote: C10 (cobertura de fragmentos), C20 (funcionalidades sin requisito) y veredicto.
6. Decisión: el lote necesita reparación si hay errores del lote, si se rechaza más del 20 % de los
   requisitos o si hay algún C20. `repair_reasons` son las líneas que van al anexo de reparación.

Los mensajes de un elemento empiezan por su id tal como lo escribió el modelo (el del fichero que se le
devuelve en la reparación) y por el código de la regla: "RF-003 C6 …". Los del lote empiezan por LOTE.

Salida: informe JSON por stdout. Código 0 si el lote se acepta (aunque tenga rechazos o avisos), 1 si
necesita reparación.
"""
import argparse
import copy
import json
import re
import sys
import unicodedata
from collections import defaultdict
from pathlib import Path

try:
    import jsonschema
except ImportError:  # pragma: no cover
    sys.exit('Falta jsonschema: pip install "jsonschema>=4.18"')
if not hasattr(jsonschema, "Draft202012Validator"):  # pragma: no cover
    sys.exit('jsonschema demasiado antiguo para el draft 2020-12: pip install "jsonschema>=4.18"')

SCHEMA_FILE = "requirements_v1.3.schema.json"

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

# C13: señales modales explícitas de §7, por obligatoriedad. Se admiten las de los dos idiomas porque la
# evidencia se copia de la fuente, que puede estar en otro idioma que `language`.
OBLIGATION_SIGNALS = {
    "SHALL": [r"deb(e|en|erá|erán)", r"tienen? que", r"han? de", r"es obligatori[oa]", r"obligatoriamente",
              r"se requiere", r"requerid[oa]s?", r"necesariamente",
              r"must", r"shall", r"required", r"(is|are) required to", r"(has|have) to", r"mandatory"],
    "SHOULD": [r"debería(n)?", r"sería deseable", r"deseable", r"preferiblemente", r"preferentemente", r"idealmente",
               r"se recomienda", r"recomendable",
               r"should", r"preferably", r"recommended", r"desirable", r"ideally"],
    "MAY": [r"opcionalmente", r"de forma opcional", r"opcional(es)?", r"si es posible", r"podría(n)?",
            r"may", r"could", r"optionally", r"optional", r"if possible", r"nice to have"],
}

# U7 / C22: verbos paraguas. Se buscan al principio de la respuesta o tras "permitir a <actor>" /
# "allow <actor> to", con un actor de hasta cuatro palabras.
UMBRELLA_RE = {
    "es": re.compile(r"^(?:permitir\s+(?:\S+\s+){0,4}?)?(gestionar|administrar|mantener|manejar)\b", re.IGNORECASE),
    "en": re.compile(r"^(?:allow\s+(?:\S+\s+){0,4}?to\s+)?(manage|administer|maintain|handle)\b", re.IGNORECASE),
}
UMBRELLA_MIN_OPERATIONS = 5  # U7: un verbo paraguas solo con más de cuatro operaciones

TYPES = ("RF", "RI", "RNF", "RA", "RT")
FUNCTIONAL_TYPES = ("RF", "RI")  # los que explican una funcionalidad del inventario (C20, C21)
LABELS = ("OPERATION", "DATA", "RULE", "LIMIT", "MESSAGE", "ERROR")
# Una etiqueta va entre corchetes ("[DATA]", "[data]:") o seguida de dos puntos ("DATA:"). Sin eso no es una
# etiqueta: "Error messages are shown…" es un criterio sin etiqueta (C19), no un [ERROR].
_LABEL_WORDS = "|".join(LABELS)
LABEL_RE = re.compile(rf"^\s*(?:\[\s*({_LABEL_WORDS})\s*\]\s*:?|({_LABEL_WORDS})\s*:)\s*", re.IGNORECASE)

MAX_REJECTED_SHARE = 0.20   # aceptación parcial: más rechazos que esto -> reparación
MAX_UNTREATED_SHARE = 0.10  # C10: más fragmentos sin tratar que esto -> error del lote

QUOTE_MIN_WORDS = 3  # C6: una cita más corta solo vale si es el fragmento entero
NUMBER_RE = re.compile(r"\d+(?:[.,]\d+)*")
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

# Categoría de cada código, para las métricas por requisito (cuentan errores y avisos).
CATEGORY = {"SCHEMA": "sintaxis", "C2": "sintaxis", "C14": "sintaxis",
            "C5": "redaccion", "C15": "redaccion", "C19": "redaccion", "C22": "redaccion",
            "C7": "decision", "C12": "decision",
            "C6": "trazabilidad", "C13": "trazabilidad", "C16": "trazabilidad", "C17": "trazabilidad",
            "N3": "referencias", "C21": "referencias",
            "N2": "fidelidad"}

# Campos del catálogo que son listas de elementos; un error de esquema dentro de uno de ellos afecta solo
# a ese elemento.
ELEMENT_LISTS = ("requirements", "open_questions")
OTHER_LISTS = {"capabilities": "CAP", "discarded_statements": "DISCARDED", "discarded_sections": "SECTION",
               "domain_terms": "DOMAIN"}

QUOTE_MAP = str.maketrans({"“": '"', "”": '"', "„": '"', "«": '"', "»": '"', "‘": "'", "’": "'",
                           "–": "-", "—": "-", " ": " "})


def norm(s: str) -> str:
    """NFC, comillas y guiones tipográficos unificados, espacios colapsados."""
    s = unicodedata.normalize("NFC", s).translate(QUOTE_MAP)
    return re.sub(r"\s+", " ", s).strip()


def quote_key(s: str) -> str:
    """Forma de comparar citas: como norm, sin marcas de Markdown (` * _) y sin distinguir mayúsculas."""
    return re.sub(r"\s+", " ", re.sub(r"[`*_]", "", norm(s))).strip().lower()


def quote_in(quote: str, text: str) -> bool:
    return quote_key(quote) in quote_key(text)


def trim_end(s: str) -> str:
    return quote_key(s).rstrip(".,;:")


def contains_words(haystack: str, needle: str) -> bool:
    """`needle` aparece en `haystack` como palabras completas (normalizado y sin distinguir mayúsculas)."""
    return re.search(rf"(?<!\w){re.escape(norm(needle).lower())}(?!\w)", norm(haystack).lower()) is not None


def numbers(s: str) -> set[str]:
    """Cifras de un texto, sin separadores de miles ni decimales ("99,9" y "99.9" dan "999")."""
    return {re.sub(r"[.,]", "", n) for n in NUMBER_RE.findall(s)}


def cap(s: str) -> str:
    return s[:1].upper() + s[1:] if s else s


def join_list(items: list[str], lang: str) -> str:
    if len(items) <= 1:
        return "".join(items)
    return ", ".join(items[:-1]) + f" {KW[lang]['AND']} " + items[-1]


def section_key(section: str) -> str | None:
    """Etiqueta numérica de una sección ("§2.1" -> "2.1", "3. Roles" -> "3"), o None si no tiene."""
    m = re.match(r"^\s*§?\s*(\d+(?:\.\d+)*)", section or "")
    return m.group(1) if m else None


def in_section(fragment_section: str, section: str) -> bool:
    """El fragmento pertenece a la sección o a una de sus subsecciones (C10 con discarded_sections)."""
    if norm(fragment_section) == norm(section):
        return True
    fk, sk = section_key(fragment_section), section_key(section)
    return fk is not None and sk is not None and (fk == sk or fk.startswith(sk + "."))


def split_label(criterion: str) -> tuple[str | None, str]:
    """("DATA", "texto") si el criterio empieza por una etiqueta válida (tras normalizar), o (None, criterio)."""
    m = LABEL_RE.match(criterion)
    if not m:
        return None, criterion.strip()
    return (m.group(1) or m.group(2)).upper(), criterion[m.end():].strip()


def render(req: dict, lang: str) -> str:
    """Genera el texto del requisito a partir de los slots (plantillas de §6 del prompt)."""
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


def branch_errors(r: dict) -> list[str]:
    """C12: coherencia de decision_branch con el tipo, la notación y la obligatoriedad (árbol de §6)."""
    b, t, nt, tpl = r["decision_branch"], r["type"], r["notation"], r["master_template"]
    ob, fb = r["obligation"], r["escalation_fallback"]
    errs = []
    if fb is not None and b not in ("D1", "D4"):
        errs.append(f"C12 escalation_fallback solo se admite en D1 o D4 (rama {b})")
    if tpl == "property" and b != "D1":
        errs.append(f"C12 MASTeR property solo en D1 (rama {b})")
    if b == "D1":
        if t != "RNF":
            errs.append(f"C12 D1 exige type RNF (es {t})")
        if nt == "MASTeR" and tpl != "property":
            errs.append("C12 D1 exige MASTeR property")
        if nt == "EARS" and fb is None:
            errs.append("C12 D1 en EARS solo como degradación (escalation_fallback)")
    elif b == "D2":
        if t not in ("RA", "RT", "RNF"):
            errs.append(f"C12 D2 exige type RA, RT o RNF (es {t})")
        if ob == "SHALL" and not (nt == "EARS" and r["ears_pattern"] == "ubiquitous"):
            errs.append("C12 D2 con SHALL exige EARS ubiquitous")
        if ob != "SHALL" and not (nt == "MASTeR" and tpl == "functional" and r["master_activity"] == "autonomous"):
            errs.append("C12 D2 sin SHALL exige MASTeR functional autonomous")
    else:
        if t not in ("RF", "RI", "RNF"):
            errs.append(f"C12 {b} exige type RF, RI o RNF (es {t})")
        if b == "D3":
            if ob == "SHALL":
                errs.append("C12 D3 exige obligation SHOULD o MAY")
            if not (nt == "MASTeR" and tpl == "functional"):
                errs.append("C12 D3 exige MASTeR functional")
        elif b == "D4":
            if nt == "MASTeR" and not (tpl == "functional" and r["master_condition"] in ("logic", "event")):
                errs.append("C12 D4 exige MASTeR functional con condición logic o event")
            if nt == "EARS" and fb is None:
                errs.append("C12 D4 en EARS solo como degradación (escalation_fallback)")
        elif b == "D5" and nt != "EARS":
            errs.append("C12 D5 exige EARS")
    return errs


def vague_terms(text: str, lang: str, domain_terms: list[str], exempt: str | None = None) -> list[str]:
    """C5: términos vagos de un texto, salvo los valores pendientes, los términos de dominio declarados (N7)
    y `exempt` (la característica nominalizada de N3)."""
    for t in TBD:
        text = text.replace(t, "")
    for term in domain_terms:
        text = re.sub(re.escape(term), "", text, flags=re.IGNORECASE)
    if exempt:
        text = text.replace(exempt, "")
    found = []
    for category, pats in VAGUE[lang].items():
        for pat in pats:
            if re.search(rf"(?<!\w){pat}(?!\w)", text, flags=re.IGNORECASE):
                found.append(f"C5 término vago ({category}) '{pat}'")
    return found


def lot_failure(errors: list[str]) -> dict:
    """Informe de un lote que no se puede usar: hay que repararlo."""
    return {"valid": False, "needs_repair": True, "repair_reasons": errors, "errors": errors, "rejected": [],
            "warnings": [], "metrics": {}, "catalog": None, "texts": {}, "ids": {}}


def check_text(raw: str, sistema: str, fragments: dict | None, schema: dict) -> dict:
    """Valida el contenido de output/catalogo.json tal cual (puede no ser JSON)."""
    try:
        cat = json.loads(raw)
    except json.JSONDecodeError as ex:
        return lot_failure([f"LOTE JSON no válido: {ex}"])
    return check(cat, sistema, fragments, schema)


def check(cat, sistema: str, fragments: dict | None, schema: dict) -> dict:
    warnings: list[str] = []

    # --- 1. Esquema, repartido por niveles ---------------------------------------------------------
    lot_errors, element_errors = [], defaultdict(list)
    for e in sorted(jsonschema.Draft202012Validator(schema).iter_errors(cat),
                    key=lambda e: [str(p) for p in e.absolute_path]):
        path = list(e.absolute_path)
        msg = f"SCHEMA {'/'.join(map(str, path)) or '(raíz)'}: {e.message}"
        if len(path) >= 2 and isinstance(path[1], int) and path[0] in (*ELEMENT_LISTS, *OTHER_LISTS):
            element_errors[(path[0], path[1])].append(msg)
        else:
            lot_errors.append(f"LOTE {msg}")
    if lot_errors:
        return lot_failure(lot_errors)

    c = copy.deepcopy(cat)
    lang = c["language"]

    def label(item, fallback: str) -> str:
        return item["id"] if isinstance(item, dict) and isinstance(item.get("id"), str) else fallback

    # Elementos que no son requisitos ni preguntas: si no cumplen el esquema, se ignoran con aviso.
    for key, prefix in OTHER_LISTS.items():
        kept = []
        for i, item in enumerate(c[key]):
            errs = element_errors.get((key, i))
            if errs:
                warnings += [f"{label(item, f'{prefix}[{i}]')} {m} (se ignora)" for m in errs]
            else:
                kept.append(item)
        c[key] = kept

    # Requisitos y preguntas: cada uno con sus errores. Los de esquema no siguen al resto de comprobaciones.
    reqs = [{"item": r, "id": label(r, f"REQ[{i}]"), "errors": list(element_errors.get(("requirements", i), [])),
             "warnings": []} for i, r in enumerate(c["requirements"])]
    qs = [{"item": q, "id": label(q, f"Q[{i}]"), "errors": list(element_errors.get(("open_questions", i), [])),
           "warnings": []} for i, q in enumerate(c["open_questions"])]
    valid_reqs = [e for e in reqs if not e["errors"]]
    valid_qs = [e for e in qs if not e["errors"]]

    lot_frags = {fid: f for fid, f in (fragments or {}).items()}
    doc_frag_ids = [fid for fid, f in lot_frags.items() if f["kind"] == "document"]

    # --- 2. Normalizador --------------------------------------------------------------------------
    # Términos de dominio (N7): uno mal declarado se ignora y deja de proteger su palabra frente a C5.
    domain_terms = []
    for i, dt in enumerate(c["domain_terms"]):
        problem = None
        if len(dt["term"].split()) < 2:
            problem = f"el término de dominio '{dt['term']}' debe tener al menos dos palabras"
        elif fragments is not None and dt["fragment"] not in lot_frags:
            problem = f"fragmento inexistente {dt['fragment']}"
        elif fragments is not None and not contains_words(lot_frags[dt["fragment"]]["text"], dt["term"]):
            problem = f"el término de dominio '{dt['term']}' no aparece en {dt['fragment']}"
        if problem:
            warnings.append(f"DOMAIN[{i}] C5 {problem} (se ignora)")
        else:
            domain_terms.append(dt["term"])

    def fix(owner: str, what: str):
        warnings.append(f"{owner} FIX {what}")

    def normalize_traces(owner: dict):
        """Cita en el fragmento equivocado pero literal en un único fragmento del lote: se corrige el id.
        Entradas repetidas: se quitan."""
        item, seen, kept = owner["item"], set(), []
        for t in item["source_trace"]:
            if fragments is not None:
                frag = lot_frags.get(t["fragment"])
                if frag is None or not quote_in(t["quote"], frag["text"]):
                    found = [fid for fid, f in lot_frags.items() if quote_in(t["quote"], f["text"])]
                    if len(found) == 1:
                        fix(owner["id"], f"cita movida de {t['fragment']} a {found[0]}, donde es literal")
                        t["fragment"] = found[0]
            key = (t["fragment"], quote_key(t["quote"]))
            if key in seen:
                fix(owner["id"], f"cita repetida de {t['fragment']} quitada")
                continue
            seen.add(key)
            kept.append(t)
        item["source_trace"] = kept

    for e in valid_reqs:
        r, s = e["item"], e["item"]["slots"]
        normalize_traces(e)
        if "system" in s and s["system"] != sistema:
            fix(e["id"], f"slots.system '{s['system']}' sustituido por '{sistema}'")
            s["system"] = sistema
        for key, val in list(s.items()):
            if key in ("system", "trigger_keyword") or val is None:
                continue
            values = val if isinstance(val, list) else [val]
            cleaned = [v if v in TBD else v.strip().rstrip(".,;:").rstrip() for v in values]
            if cleaned != values:
                fix(e["id"], f"slot '{key}': espacios o puntuación final quitados")
                s[key] = cleaned if isinstance(val, list) else cleaned[0]
        criteria = []
        for crit in r["acceptance_criteria"]:
            tag, text = split_label(crit)
            fixed = f"[{tag}] {text}" if tag else crit.strip()
            if tag and fixed != crit:
                fix(e["id"], f"etiqueta de criterio normalizada: «{crit[:40]}»")
            criteria.append(fixed)
        r["acceptance_criteria"] = criteria
    for e in valid_qs:
        normalize_traces(e)

    # Funcionalidades: fragmentos que no están en el lote, fuera.
    for cap_item in c["capabilities"]:
        if fragments is not None:
            outside = [f for f in cap_item["fragments"] if f not in lot_frags]
            if outside:
                fix(cap_item["id"], f"fragmentos fuera del lote quitados: {', '.join(outside)}")
                cap_item["fragments"] = [f for f in cap_item["fragments"] if f in lot_frags]

    # Vínculos (con los ids que escribió el modelo; ante ids repetidos, cuenta el primero).
    req_by_id, q_by_id, cap_ids = {}, {}, set()
    for e in valid_reqs:
        if e["id"] in req_by_id:
            warnings.append(f"{e['id']} C9 id de requisito repetido; las referencias apuntan al primero")
        req_by_id.setdefault(e["id"], e)
    for e in valid_qs:
        if e["id"] in q_by_id:
            warnings.append(f"{e['id']} C9 id de pregunta repetido; las referencias apuntan a la primera")
        q_by_id.setdefault(e["id"], e)
    for cap_item in c["capabilities"]:
        if cap_item["id"] in cap_ids:
            warnings.append(f"{cap_item['id']} C9 id de funcionalidad repetido")
        cap_ids.add(cap_item["id"])

    def keep_known(owner: str, refs: list[str], known, what: str) -> list[str]:
        kept = [x for x in refs if x in known]
        for x in refs:
            if x not in known:
                fix(owner, f"referencia a {what} inexistente {x} quitada")
        return list(dict.fromkeys(kept))  # sin repetidos, en orden

    for e in valid_reqs:
        r = e["item"]
        r["open_question_ids"] = keep_known(e["id"], r["open_question_ids"], q_by_id, "pregunta")
        r["derived_from"] = keep_known(e["id"], r["derived_from"], req_by_id, "requisito")
        r["capability_ids"] = keep_known(e["id"], r["capability_ids"], cap_ids, "funcionalidad")
    for e in valid_qs:
        e["item"]["related_requirements"] = keep_known(e["id"], e["item"]["related_requirements"], req_by_id,
                                                       "requisito")
    for e in valid_reqs:  # completar el sentido que falte
        for qid in e["item"]["open_question_ids"]:
            related = q_by_id[qid]["item"]["related_requirements"]
            if e["id"] not in related:
                related.append(e["id"])
                fix(qid, f"vínculo con {e['id']} completado")
    for e in valid_qs:
        for rid in e["item"]["related_requirements"]:
            linked = req_by_id[rid]["item"]["open_question_ids"]
            if e["id"] not in linked:
                linked.append(e["id"])
                fix(rid, f"vínculo con {e['id']} completado")

    # --- 3. Comprobaciones por elemento -------------------------------------------------------------
    whole_discards = {d["fragment"] for d in c["discarded_statements"] if d["quote"] is None}
    sections = c["discarded_sections"]

    def discarded_whole(fid: str) -> bool:
        frag = lot_frags.get(fid)
        return fid in whole_discards or (frag is not None and any(in_section(frag["section"], d["section"])
                                                                  for d in sections))

    found = {"quotes": True, "figures": True, "fragments": True, "vague": True, "ears_shall": True, "tree": True,
             "capabilities": True}
    trace_total, trace_ok = 0, 0

    def check_trace(t: dict) -> list[str]:
        """C6 (rechazo) y C17 (aviso) de una cita ya normalizada."""
        nonlocal trace_total, trace_ok
        problems = []
        if fragments is None:
            return problems
        trace_total += 1
        frag = lot_frags.get(t["fragment"])
        if frag is None:
            problems.append(f"C6 fragmento inexistente {t['fragment']}")
        elif not quote_in(t["quote"], frag["text"]):
            problems.append(f"C6 cita no literal en {t['fragment']}: «{t['quote']}»")
        elif len(t["quote"].split()) < QUOTE_MIN_WORDS and trim_end(t["quote"]) != trim_end(frag["text"]):
            problems.append(f"C6 cita de menos de {QUOTE_MIN_WORDS} palabras que no es el fragmento entero: "
                            f"«{t['quote']}»")
        else:
            trace_ok += 1
        if problems:
            found["quotes"] = False
        return problems

    for e in valid_qs:
        for t in e["item"]["source_trace"]:
            e["errors"] += check_trace(t)
            if discarded_whole(t["fragment"]):
                e["warnings"].append(f"C17 cita {t['fragment']}, que está descartado entero")

    texts_by_entry = {}
    for e in valid_reqs:
        r, s, errs, warns = e["item"], e["item"]["slots"], e["errors"], e["warnings"]
        # C2 plantilla <-> slots
        if r["notation"] == "EARS":
            p = r["ears_pattern"]
            has = (bool(s.get("feature")), bool(s.get("states")), bool(s.get("trigger")))
            if p in EARS_SHAPE and has != EARS_SHAPE[p]:
                errs.append(f"C2 slots (feature, states, trigger) = {has} incompatibles con {p}")
            if p == "complex" and sum(has) < 2:
                errs.append("C2 complex exige al menos dos tipos de {feature, states, trigger}")
            if p == "unwanted_behaviour" and s.get("trigger_keyword") != "IF":
                errs.append("C2 unwanted_behaviour exige trigger_keyword IF")
            if p == "event_driven" and s.get("trigger_keyword") != "WHEN":
                errs.append("C2 event_driven exige trigger_keyword WHEN")
            if bool(s.get("trigger")) != bool(s.get("trigger_keyword")):
                errs.append("C2 trigger y trigger_keyword deben ir juntos")
        else:
            mc, cond = r["master_condition"], s.get("condition")
            if (mc == "none") != (cond is None):
                errs.append("C2 master_condition y slot condition incoherentes")
            if r["master_template"] == "functional":
                if (r["master_activity"] == "user_interaction") != bool(s.get("actor")):
                    errs.append("C2 actor obligatorio solo y siempre en user_interaction")
            else:
                first = s["subject_matter"].split()[0].lower() if s["subject_matter"].split() else ""
                if first not in SUBJECT_PREPS[lang]:
                    warns.append(f"C14 subject_matter debe empezar por {sorted(SUBJECT_PREPS[lang])}: "
                                 f"'{s['subject_matter']}'")
        # C14 formato de slots (R-a, R-b, R-c): avisos. La puntuación final ya la quitó el normalizador.
        for key, val in slot_values(r).items():
            if key == "value" and val in TBD:
                continue
            upper_kw = [w for w in re.findall(r"\b[A-ZÁÉÍÓÚÑ]{2,}\b", val) if w in KW_WORDS[lang]]
            if upper_kw:
                warns.append(f"C14 slot '{key}' contiene keywords en mayúsculas {upper_kw} (R-b)")
            if val[:1].isupper() and not re.match(r"^[A-ZÁÉÍÓÚÑ0-9]{2,}\b", val):
                warns.append(f"C14 slot '{key}' empieza en mayúscula; ¿es nombre propio? (R-a)")
        verb_slot = s.get("response") if r["notation"] == "EARS" else s.get("process_verb")
        if verb_slot:
            if NEGATION[lang].match(verb_slot):
                errs.append(f"C15 prohibición en negativo en '{verb_slot}' (R-e: usa impedir, rechazar, bloquear…)")
            if lang == "es" and not ES_INFINITIVE.match(verb_slot.split()[0]):
                warns.append(f"C14 el slot de respuesta no parece empezar por infinitivo (R-c): {verb_slot}")
        # Render (lo hace siempre la plataforma)
        try:
            text = render(r, lang)
        except (KeyError, TypeError) as ex:
            errs.append(f"C2 no se pudo renderizar: {ex}")
            text = None
        texts_by_entry[id(e)] = text
        # Criterios: etiqueta (C19) y texto sin etiqueta para C5 y N2.
        criteria_texts = []
        for crit in r["acceptance_criteria"]:
            tag, body = split_label(crit)
            if tag is None:
                warns.append(f"C19 criterio sin etiqueta válida: «{crit[:60]}»")
            criteria_texts.append(body)
        # C5 términos vagos, sobre el texto renderizado y sobre cada criterio
        exempt = s.get("characteristic") if r["master_template"] == "property" and s.get("value") in TBD else None
        vague = (vague_terms(text, lang, domain_terms, exempt) if text else [])
        for body in criteria_texts:
            vague += [f"{v} en un criterio" for v in vague_terms(body, lang, domain_terms)]
        if vague:
            errs += list(dict.fromkeys(vague))
            found["vague"] = False
        # C7 obligatoriedad
        if r["notation"] == "EARS" and r["obligation"] != "SHALL":
            errs.append("C7 EARS con obligación distinta de SHALL")
            found["ears_shall"] = False
        # C12 árbol de decisión
        tree = branch_errors(r)
        if tree:
            found["tree"] = False
            errs += tree
        # C6 citas y C17
        for t in r["source_trace"]:
            errs += check_trace(t)
            if discarded_whole(t["fragment"]):
                warns.append(f"C17 cita {t['fragment']}, que está descartado entero")
        if fragments is not None:
            cited = [lot_frags[t["fragment"]] for t in r["source_trace"] if t["fragment"] in lot_frags]
            # C13 evidencia de obligatoriedad (aviso)
            if r["obligation_basis"] == "explicit":
                ev = r["obligation_evidence"]
                if not any(contains_words(f["text"], ev) for f in cited):
                    warns.append(f"C13 obligation_evidence '{ev}' no aparece en los fragmentos citados")
                signals = OBLIGATION_SIGNALS[r["obligation"]]
                if not any(re.search(rf"(?<!\w)(?:{pat})(?!\w)", norm(ev).lower()) for pat in signals):
                    warns.append(f"C13 obligation_evidence '{ev}' no es una señal de {r['obligation']} (§7)")
            # N2 cifras de slots y criterios: tienen que estar en algún fragmento citado
            source_numbers = set().union(*(numbers(f["text"]) for f in cited)) if cited else set()
            pieces = {**slot_values(r), **{f"criterio {i + 1}": b for i, b in enumerate(criteria_texts)}}
            for key, val in pieces.items():
                invented = sorted(numbers(val) - source_numbers)
                if invented:
                    errs.append(f"N2 cifra {invented} de '{key}' no aparece en los fragmentos citados")
                    found["figures"] = False
            # C16 origen (aviso)
            kinds = {f["kind"] for f in cited}
            if r["origin"] == "explicit" and "document" not in kinds:
                warns.append("C16 origin explicit sin ningún fragmento de documento citado")
            if r["origin"] == "clarification" and "clarification" not in kinds:
                warns.append("C16 origin clarification sin ninguna duda resuelta citada")
        # N3 valor pendiente con su pregunta missing_value
        if r["master_template"] == "property" and s.get("value") in TBD:
            if not any(q_by_id[q]["item"]["type"] == "missing_value" for q in r["open_question_ids"]):
                errs.append("N3 valor pendiente sin pregunta missing_value asociada")
        # C22 verbo paraguas con menos de cinco [OPERATION]
        if verb_slot and UMBRELLA_RE[lang].match(verb_slot):
            operations = sum(1 for crit in r["acceptance_criteria"] if split_label(crit)[0] == "OPERATION")
            if operations < UMBRELLA_MIN_OPERATIONS:
                warns.append(f"C22 verbo paraguas en '{verb_slot}' con {operations} criterios [OPERATION] (U7)")

    # --- 4. Rechazos y renumeración ----------------------------------------------------------------
    rejected = [{"id": e["id"], "element": "requirement", "errors": e["errors"]} for e in reqs if e["errors"]]
    rejected += [{"id": e["id"], "element": "question", "errors": e["errors"]} for e in qs if e["errors"]]
    accepted_reqs = [e for e in reqs if not e["errors"]]
    accepted_qs = [e for e in qs if not e["errors"]]

    # Primero el id nuevo de cada elemento aceptado; después se reescriben las referencias. Las
    # referencias usan los ids del modelo: ante un id repetido, apuntan al primero (setdefault).
    counters = defaultdict(int)
    for e in accepted_reqs:
        counters[e["item"]["type"]] += 1
        e["new_id"] = f"{e['item']['type']}-{counters[e['item']['type']]:03d}"
    for i, e in enumerate(accepted_qs, 1):
        e["new_id"] = f"Q-{i:03d}"
    caps = [{"item": cp, "id": cp["id"], "new_id": f"CAP-{i:03d}"} for i, cp in enumerate(c["capabilities"], 1)]
    new_req_ids, new_q_ids, new_cap_ids = {}, {}, {}
    for entries, mapping in ((accepted_reqs, new_req_ids), (accepted_qs, new_q_ids), (caps, new_cap_ids)):
        for e in entries:
            mapping.setdefault(e["id"], e["new_id"])

    def rewrite(owner: str, refs: list[str], mapping: dict, what: str) -> list[str]:
        out = []
        for x in refs:
            if x in mapping:
                out.append(mapping[x])
            else:
                fix(owner, f"referencia a {what} rechazado {x} quitada")
        return list(dict.fromkeys(out))

    final_reqs, final_qs, texts = [], [], {}
    for e in accepted_reqs:
        r = e["item"]
        r["id"] = e["new_id"]
        r["open_question_ids"] = rewrite(e["id"], r["open_question_ids"], new_q_ids, "pregunta")
        r["derived_from"] = rewrite(e["id"], r["derived_from"], new_req_ids, "requisito")
        r["capability_ids"] = [new_cap_ids[x] for x in r["capability_ids"]]
        final_reqs.append(r)
        texts[e["new_id"]] = texts_by_entry[id(e)]
    for e in accepted_qs:
        e["item"]["id"] = e["new_id"]
        e["item"]["related_requirements"] = rewrite(e["id"], e["item"]["related_requirements"], new_req_ids,
                                                    "requisito")
        final_qs.append(e["item"])
    for e in caps:
        e["item"]["id"] = e["new_id"]
    renumbered = sum(e["id"] != e["new_id"] for e in accepted_reqs + accepted_qs + caps)
    if renumbered:
        warnings.append(f"LOTE FIX {renumbered} ids renumerados (por tipo, en orden de aparición)")
    final_ids = {**new_req_ids, **new_q_ids, **new_cap_ids}

    # --- 5. Comprobaciones del lote -----------------------------------------------------------------
    lot_errors, repair_reasons = [], []
    # C21 y C20: funcionalidades del inventario
    for e in accepted_reqs:
        if e["item"]["type"] in FUNCTIONAL_TYPES and not e["item"]["capability_ids"]:
            e["warnings"].append("C21 requisito funcional o de integración sin capability_ids")
    for e in accepted_qs:
        if not e["item"]["related_requirements"] and e["item"]["type"] != "scope":
            e["warnings"].append(f"C11 pregunta {e['item']['type']} sin requisitos relacionados")
    covered = {x for r in final_reqs if r["type"] in FUNCTIONAL_TYPES for x in r["capability_ids"]}
    for e in caps:
        if e["new_id"] not in covered:
            line = (f"{e['id']} C20 la funcionalidad «{e['item']['name']}» no tiene ningún "
                    f"requisito RF o RI aceptado")
            warnings.append(line)
            repair_reasons.append(line)
            found["capabilities"] = False

    # Descartes: un fragmento que no está en el lote o una cita que no es literal no cuentan (aviso).
    discard_frags = set()
    for i, d in enumerate(c["discarded_statements"]):
        if fragments is not None and d["fragment"] not in lot_frags:
            warnings.append(f"DISCARDED[{i}] C6 fragmento inexistente {d['fragment']} (se ignora)")
            continue
        if fragments is not None and d["quote"] is not None and not quote_in(d["quote"], lot_frags[d["fragment"]]["text"]):
            warnings.append(f"DISCARDED[{i}] C6 cita no literal en {d['fragment']}: «{d['quote']}»")
        discard_frags.add(d["fragment"])
    section_frags = set()
    for i, d in enumerate(sections):
        inside = {fid for fid in doc_frag_ids if in_section(lot_frags[fid]["section"], d["section"])}
        if fragments is not None and not inside:
            warnings.append(f"SECTION[{i}] C10 la sección '{d['section']}' no tiene fragmentos en el lote (se ignora)")
        section_frags |= inside

    # C10: fragmentos tratados por lo que escribió el modelo (aceptado o rechazado: el rechazo ya se cuenta aparte)
    if fragments is not None:
        # Las citas de todos los requisitos y preguntas, también los rechazados por el esquema (si sus
        # citas se pueden leer): el modelo trató esos fragmentos, y el rechazo ya cuenta por su lado.
        treated = {t["fragment"] for e in reqs + qs
                   for t in (e["item"].get("source_trace") if isinstance(e["item"], dict) else None) or []
                   if isinstance(t, dict) and isinstance(t.get("fragment"), str)}
        treated |= discard_frags | section_frags
        unprocessed = set(c["verdict"]["unprocessed_fragments"])
        untreated = [fid for fid in doc_frag_ids if fid not in treated]
        if untreated:
            found["fragments"] = False
            line = f"C10 fragmentos sin tratar: {', '.join(untreated)}"
            if len(untreated) > MAX_UNTREATED_SHARE * len(doc_frag_ids):
                lot_errors.append(f"LOTE {line}")
            else:
                warnings.append(f"LOTE {line}")
        if unprocessed:
            warnings.append(f"LOTE C10 fragmentos declarados sin procesar: {', '.join(sorted(unprocessed))}")

    # Veredicto del catálogo
    v = c["verdict"]
    if v["status"] == "failed":
        lot_errors.append("LOTE VERDICT el catálogo declara status failed")
    if v["status"] == "complete" and v["unprocessed_fragments"]:
        warnings.append("LOTE VERDICT status complete con unprocessed_fragments no vacío")
    if v["status"] == "partial" and not v["unprocessed_fragments"]:
        warnings.append("LOTE VERDICT status partial sin unprocessed_fragments")
    declared_vs_found = {
        "quotes_are_literal": found["quotes"], "figures_from_source": found["figures"],
        "all_fragments_accounted": found["fragments"], "all_capabilities_covered": found["capabilities"],
        "no_vague_terms": found["vague"], "ears_only_shall": found["ears_shall"],
        "decision_tree_consistent": found["tree"],
    }
    mismatches = [k for k, real_ok in declared_vs_found.items() if v["self_check"][k] and not real_ok]
    warnings += [f"LOTE SELF_CHECK el modelo declara {k}=true y el validador encuentra fallos" for k in mismatches]

    # Avisos de cada elemento, con su id original, detrás de los del normalizador.
    for e in reqs + qs:
        warnings += [f"{e['id']} {w}" for w in e["warnings"]]

    # --- 6. Decisión ------------------------------------------------------------------------------
    written = len(reqs)
    rejected_reqs = [x for x in rejected if x["element"] == "requirement"]
    if written and len(rejected_reqs) > MAX_REJECTED_SHARE * written:
        repair_reasons = [f"{x['id']} {err}" for x in rejected_reqs for err in x["errors"]] + repair_reasons
    repair_reasons = lot_errors + repair_reasons

    # --- Métricas -----------------------------------------------------------------------------------
    categories = [{CATEGORY.get(m.split()[0], "otros") for m in e["errors"] + e["warnings"]} for e in reqs]

    def share(category: str | None):
        if not written:
            return None
        if category is None:
            return round((written - len(rejected_reqs)) / written, 3)
        return round(sum(category not in cats for cats in categories) / written, 3)

    accepted_frags = ({t["fragment"] for r in final_reqs for t in r["source_trace"]}
                      | {t["fragment"] for q in final_qs for t in q["source_trace"]} | discard_frags | section_frags)
    n = len(final_reqs)
    criteria = [split_label(crit)[0] for r in final_reqs for crit in r["acceptance_criteria"]]
    metrics = {
        "requisitos": n,
        "requisitos_escritos": written,
        "requisitos_rechazados": len(rejected_reqs),
        "preguntas_rechazadas": len(rejected) - len(rejected_reqs),
        "por_tipo": {t: sum(r["type"] == t for r in final_reqs) for t in TYPES},
        "conformidad_sintactica": share("sintaxis"),   # benchmark Etapa 1: ≥ 0,90
        "calidad_redaccion": share("redaccion"),
        "coherencia_decision": share("decision"),
        "trazabilidad": share("trazabilidad"),
        "fidelidad_cifras": share("fidelidad"),
        "requisitos_sin_errores": share(None),
        "citas_literales": f"{trace_ok}/{trace_total}" if fragments is not None else "no comprobado",
        "cobertura_fragmentos": (f"{sum(fid in accepted_frags for fid in doc_frag_ids)}/{len(doc_frag_ids)}"
                                 if fragments is not None else "no comprobado"),
        "proporcion_master": round(sum(r["notation"] == "MASTeR" for r in final_reqs) / n, 3) if n else None,
        "ramas": {b: sum(r["decision_branch"] == b for r in final_reqs) for b in ("D1", "D2", "D3", "D4", "D5")},
        "preguntas_abiertas": len(final_qs),
        "terminos_dominio": len(domain_terms),
        "funcionalidades": len(caps),
        "cobertura_funcionalidades": f"{sum(e['new_id'] in covered for e in caps)}/{len(caps)}",
        "criterios": len(criteria),
        "criterios_por_requisito": round(len(criteria) / n, 2) if n else None,
        "criterios_por_etiqueta": {**{t: criteria.count(t) for t in LABELS}, "sin_etiqueta": criteria.count(None)},
        "correcciones": sum(1 for w in warnings if len(w.split()) > 1 and w.split()[1] == "FIX"),
        "autoverificacion_coincide": not mismatches,
    }

    catalog = {**{k: c[k] for k in ("schema_version", "system", "language", "capabilities")},
               "requirements": final_reqs, "open_questions": final_qs,
               **{k: c[k] for k in ("discarded_statements", "discarded_sections", "domain_terms", "verdict")}}
    valid = not lot_errors
    return {"valid": valid, "needs_repair": bool(repair_reasons), "repair_reasons": repair_reasons,
            "errors": lot_errors, "rejected": rejected, "warnings": warnings, "metrics": metrics,
            "catalog": catalog if valid else None, "texts": texts if valid else {}, "ids": final_ids}


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
    ap.add_argument("--schema", default=str(Path(__file__).with_name(SCHEMA_FILE)))
    ap.add_argument("--enriquecido")
    a = ap.parse_args()
    schema = json.loads(Path(a.schema).read_text(encoding="utf-8"))
    fragments = load_fragments(a.fragmentos) if a.fragmentos else None
    report = check_text(Path(a.catalogo).read_text(encoding="utf-8"), a.sistema, fragments, schema)
    if a.enriquecido and report["catalog"] is not None:
        Path(a.enriquecido).write_text(
            json.dumps(enrich(report["catalog"], report["texts"]), ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8")
    report.pop("catalog")
    report.pop("texts")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    sys.exit(1 if report["needs_repair"] else 0)


if __name__ == "__main__":
    main()
