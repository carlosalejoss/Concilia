#!/usr/bin/env python3
"""
Validador determinista del catálogo EARS/MASTeR generado por el Modelo A o el Modelo B.

Uso:
    python validar_catalogo.py salida.json --sistema "el sistema X" [--fuentes DIR] [--schema requirements.schema.json]

--fuentes: carpeta con un fichero de texto por fuente, cuyo nombre (sin extensión) es el id
           de la fuente (DOC-01.txt, DUDAS.txt...). Si se indica, se comprueba que cada `quote`
           es literal (normalizando espacios).

Salida: informe JSON por stdout. Código de salida 0 si no hay errores (los avisos no bloquean), 1 si los hay.
El runner puede devolver `errors` al modelo como mensaje de corrección (reintento).
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
           "C_logic": "SI", "C_event": "EN CUANTO", "C_time": "MIENTRAS"},
    "en": {"WHEN": "WHEN", "WHILE": "WHILE", "IF": "IF", "THEN": "THEN", "WHERE": "WHERE",
           "SHALL": "SHALL", "SHOULD": "SHOULD", "MAY": "MAY",
           "PROVIDE_PRE": "PROVIDE", "PROVIDE_POST": "WITH THE ABILITY TO", "ABLE": "BE ABLE TO", "BE": "BE",
           "C_logic": "IF", "C_event": "AS SOON AS", "C_time": "AS LONG AS"},
}

VAGUE = {
    "es": [r"r[aá]pid[oa]s?", r"r[aá]pidamente", r"f[aá]cil(es|mente)?", r"intuitiv[oa]s?", r"amigables?",
           r"sencill[oa]s?", r"adecuad[oa]s?", r"adecuadamente", r"eficientes?", r"eficientemente", r"flexibles?",
           r"robust[oa]s?", r"[oó]ptim[oa]s?", r"suficientes?", r"razonables?", r"apropiad[oa]s?", r"etc\.?",
           r"y/o", r"si es posible", r"en la medida de lo posible", r"cuando proceda", r"cuando sea necesario",
           r"seg[uú]n corresponda", r"aproximadamente", r"lo antes posible", r"normalmente"],
    "en": [r"fast", r"quick(ly)?", r"easy", r"easily", r"intuitive", r"user-friendly", r"simple", r"adequate(ly)?",
           r"efficient(ly)?", r"flexible", r"robust", r"optimal", r"sufficient", r"reasonable", r"appropriate",
           r"etc\.?", r"and/or", r"if possible", r"as far as possible", r"where appropriate", r"when necessary",
           r"approximately", r"as soon as possible", r"normally"],
}

EARS_REQUIRED = {  # patrón -> (feature, state, trigger) obligatorios / prohibidos ; None = libre
    "ubiquitous": (False, False, False),
    "event_driven": (False, False, True),
    "state_driven": (False, True, False),
    "unwanted_behaviour": (False, False, True),
    "optional_feature": (True, False, False),
}


def cap(s: str) -> str:
    return s[:1].upper() + s[1:] if s else s


def norm_ws(s: str) -> str:
    return re.sub(r"\s+", " ", unicodedata.normalize("NFC", s)).strip()


def render(req: dict, lang: str) -> str:
    k = KW[lang]
    s = req["slots"]
    if req["notation"] == "EARS":
        parts = []
        if s.get("feature"):
            parts.append(f"{k['WHERE']} {s['feature']}, ")
        if s.get("state"):
            parts.append(f"{k['WHILE']} {s['state']}, ")
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
    # property
    q = f"{s['qualifying_expression']} " if s.get("qualifying_expression") else ""
    return cap(f"{cond}{s['characteristic']} {s['subject_matter']} {liab} {k['BE']} {q}{s['value']}.")


def check(cat: dict, sistema: str, fuentes: dict | None, schema: dict) -> dict:
    errors, warnings = [], []

    schema_errors = sorted(jsonschema.Draft202012Validator(schema).iter_errors(cat), key=lambda e: list(e.path))
    for e in schema_errors:
        errors.append(f"SCHEMA {'/'.join(map(str, e.path)) or '(raíz)'}: {e.message}")
    if schema_errors:
        return {"valid": False, "errors": errors, "warnings": warnings, "metrics": {}}

    lang = cat["language"]
    reqs = cat["requirements"]
    req_ids = [r["id"] for r in reqs]
    q_ids = [q["id"] for q in cat["open_questions"]]
    valid_reqs = 0

    if len(set(req_ids)) != len(req_ids):
        errors.append("C9 ids de requisito duplicados")
    expected = [f"REQ-{i:03d}" for i in range(1, len(reqs) + 1)]
    if req_ids != expected:
        warnings.append("C9 ids no consecutivos desde REQ-001")

    all_traces = []
    for r in reqs:
        rid, s, local = r["id"], r["slots"], []
        # C1 render
        try:
            expected_text = render(r, lang)
            if r["text"] != expected_text:
                local.append(f"C1 text no coincide con el render de slots.\n   esperado: {expected_text}\n   recibido: {r['text']}")
        except (KeyError, TypeError) as ex:
            local.append(f"C1 no se pudo renderizar: {ex}")
        # C2 patrón <-> slots
        if r["notation"] == "EARS":
            p = r["ears_pattern"]
            has = (bool(s.get("feature")), bool(s.get("state")), bool(s.get("trigger")))
            if p in EARS_REQUIRED and has != EARS_REQUIRED[p]:
                local.append(f"C2 slots {has} (feature,state,trigger) incompatibles con patrón {p}")
            if p == "complex" and sum(has) < 2:
                local.append("C2 patrón complex requiere ≥2 de feature/state/trigger")
            if p == "unwanted_behaviour" and s.get("trigger_keyword") != "IF":
                local.append("C2 unwanted_behaviour requiere trigger_keyword IF")
            if p == "event_driven" and s.get("trigger_keyword") != "WHEN":
                local.append("C2 event_driven requiere trigger_keyword WHEN")
            if s.get("trigger") and not s.get("trigger_keyword"):
                local.append("C2 trigger sin trigger_keyword")
            if not s.get("trigger") and s.get("trigger_keyword"):
                local.append("C2 trigger_keyword sin trigger")
            for slot in ("trigger", "state", "feature"):
                if s.get(slot) and re.search(r"\b(y|o|and|or)\b", s[slot]):
                    warnings.append(f"{rid} condición posiblemente compuesta en '{slot}' (revisar D4/G2): {s[slot]}")
        else:
            mc, st = r["master_condition"], s.get("condition")
            if (mc == "none") != (st is None):
                local.append("C2 master_condition y slot condition incoherentes")
            if r["master_template"] == "functional":
                act = r["master_activity"]
                if (act == "user_interaction") != bool(s.get("actor")):
                    local.append("C2 actor obligatorio solo y siempre en user_interaction")
            if r["obligation"] == "SHALL" and r["escalation_reason"] == "non_mandatory_obligation":
                local.append("C7 escalation_reason non_mandatory_obligation con obligation SHALL")
        # C3 sistema
        if "system" in s and s["system"] != sistema:
            local.append(f"C3 slots.system '{s['system']}' ≠ '{sistema}'")
        # C5 términos vagos (se excluyen marcadores de valor pendiente)
        txt = r["text"].replace("[POR DEFINIR]", "").replace("[TBD]", "")
        for pat in VAGUE[lang]:
            if re.search(rf"(?<!\w){pat}(?!\w)", txt, flags=re.IGNORECASE):
                local.append(f"C5 término vago '{pat}' en text")
        # C7 obligatoriedad
        if r["notation"] == "EARS" and r["obligation"] != "SHALL":
            local.append("C7 EARS con obligación distinta de SHALL")
        # C11 referencias
        for q in r["open_question_ids"]:
            if q not in q_ids:
                local.append(f"C11 open_question_id inexistente {q}")
        for d in r["derived_from"]:
            if d not in req_ids:
                local.append(f"C11 derived_from inexistente {d}")
        if ("[POR DEFINIR]" in r["text"] or "[TBD]" in r["text"]) and not r["open_question_ids"]:
            local.append("N3 valor pendiente sin pregunta abierta asociada")
        all_traces += [(rid, t) for t in r["source_trace"]]
        errors += [f"{rid} {m}" for m in local]
        valid_reqs += not local

    for q in cat["open_questions"]:
        for rr in q["related_requirements"]:
            if rr not in req_ids:
                errors.append(f"{q['id']} C11 related_requirement inexistente {rr}")
        all_traces += [(q["id"], t) for t in q["source_trace"]]
    all_traces += [("DISCARDED", d) for d in cat["discarded_statements"]]

    # C6 citas literales
    literal_ok = None
    if fuentes is not None:
        literal_ok = 0
        for owner, t in all_traces:
            src = fuentes.get(t["source"])
            if src is None:
                errors.append(f"{owner} C6 fuente desconocida '{t['source']}'")
            elif norm_ws(t["quote"]) not in src:
                errors.append(f"{owner} C6 cita no literal en {t['source']} {t['locator']}: «{t['quote']}»")
            else:
                literal_ok += 1

    # Coherencia del veredicto
    c = cat["verdict"]["counts"]
    real = {"requirements": len(reqs), "ears": sum(r["notation"] == "EARS" for r in reqs),
            "master": sum(r["notation"] == "MASTeR" for r in reqs), "open_questions": len(q_ids),
            "discarded": len(cat["discarded_statements"])}
    for key, val in real.items():
        if c[key] != val:
            errors.append(f"VERDICT counts.{key}={c[key]} pero hay {val}")

    metrics = {
        "requisitos": len(reqs),
        "validez_sintactica": round(valid_reqs / len(reqs), 3) if reqs else None,  # benchmark Etapa 1: ≥ 0,90
        "proporcion_master": round(real["master"] / len(reqs), 3) if reqs else None,
        "citas_literales": f"{literal_ok}/{len(all_traces)}" if literal_ok is not None else "no comprobado",
        "preguntas_abiertas": len(q_ids),
    }
    return {"valid": not errors, "errors": errors, "warnings": warnings, "metrics": metrics}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("catalogo")
    ap.add_argument("--sistema", required=True)
    ap.add_argument("--fuentes")
    ap.add_argument("--schema", default=str(Path(__file__).with_name("requirements.schema.json")))
    a = ap.parse_args()
    cat = json.loads(Path(a.catalogo).read_text(encoding="utf-8"))
    schema = json.loads(Path(a.schema).read_text(encoding="utf-8"))
    fuentes = None
    if a.fuentes:
        fuentes = {p.stem: norm_ws(p.read_text(encoding="utf-8")) for p in Path(a.fuentes).iterdir() if p.is_file()}
    report = check(cat, a.sistema, fuentes, schema)
    print(json.dumps(report, ensure_ascii=False, indent=2))
    sys.exit(0 if report["valid"] else 1)


if __name__ == "__main__":
    main()
