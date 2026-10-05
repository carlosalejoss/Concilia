#!/usr/bin/env python3
"""
Validador determinista del inventario de funcionalidades v2 (fase 1) generado por el Modelo A o el Modelo B.

Es la implementación de referencia: el port a C# (InventoryValidator) debe dar los mismos resultados para los
mismos ficheros (ver test_validador.py --exportar).

Uso:
    python validar_inventario.py inventario.json --fragmentos fragmentos.json
                                 [--schema inventory_v2.schema.json] [--normalizado salida.json]

--fragmentos   fichero de fragmentar.py con todos los fragmentos del documento (y de las dudas). Sin él no se
               comprueban las citas, la cobertura ni los fragmentos inexistentes.
--normalizado  escribe el inventario normalizado y renumerado (CAP-001…, QI-001…), el que reciben los lotes.

Qué hace, en orden:
1. Esquema. Un fallo en la estructura raíz es un error del inventario. Un elemento (funcionalidad,
   descarte, sección o pregunta) que no cumple el esquema se ignora con un aviso: sus fragmentos quedan
   sin tratar y los recoge C10.
2. Normalizador (avisos FIX): quita los fragmentos que no existen, corrige las citas de las preguntas
   (fragmento equivocado o repetidas), quita las referencias a funcionalidades que no existen y aplica la
   exclusividad: un fragmento asignado a una funcionalidad no puede estar descartado.
3. Comprobaciones: IDS (ids repetidos, error), C6 (citas de las preguntas: rechazan la pregunta), KIND y
   REP (avisos).
4. Comprobaciones del inventario: C10 con umbral 0 (cualquier fragmento sin tratar es error) y veredicto.
5. Renumeración de CAP- y QI- en orden de aparición: desde aquí los ids son fijos.
6. Decisión: el inventario necesita reparación si hay algún error. `repair_reasons` son las líneas del anexo.

Los mensajes de un elemento empiezan por su id tal como lo escribió el modelo y por el código: "CAP-3 REP …".
Los del inventario entero empiezan por INVENTARIO.

Salida: informe JSON por stdout. Código 0 si el inventario se acepta, 1 si necesita reparación.
"""
import argparse
import copy
import json
import re
import sys
import unicodedata
from collections import Counter, defaultdict
from pathlib import Path

import jsonschema

sys.path.insert(0, str(Path(__file__).parent))
from validar_catalogo import (QUOTE_MIN_WORDS, in_section, load_fragments, quote_in, quote_key,  # noqa: E402
                              trim_end)

SCHEMA_FILE = "inventory_v2.schema.json"

# Listas del inventario: un error de esquema dentro de un elemento afecta solo a ese elemento.
LISTS = {"capabilities": "CAP", "discarded_statements": "DISCARDED", "discarded_sections": "SECTION",
         "open_questions": "QI"}

# REP: posibles repeticiones entre funcionalidades. Umbrales iniciales, pendientes de calibrar.
REP_FRAGMENTS = 0.5     # Jaccard de fragmentos a partir del cual se avisa
REP_NAMES = 0.75        # Jaccard de palabras del nombre a partir del cual se avisa
REP_SHARED = 3          # un fragmento de 3 o más funcionalidades (resumen, matriz) no cuenta para REP

# Artículos y preposiciones que no cuentan al comparar nombres (REP).
NAME_STOPWORDS = {
    "es": {"el", "la", "los", "las", "lo", "un", "una", "unos", "unas", "al", "del",
           "a", "ante", "bajo", "con", "contra", "de", "desde", "durante", "en", "entre", "hacia", "hasta",
           "mediante", "para", "por", "segun", "sin", "sobre", "tras"},
    "en": {"the", "a", "an",
           "about", "above", "across", "after", "against", "along", "among", "around", "at", "before", "behind",
           "below", "between", "by", "during", "for", "from", "in", "into", "of", "off", "on", "onto", "over",
           "per", "through", "to", "toward", "towards", "under", "until", "up", "upon", "via", "with", "within",
           "without"},
}

KINDS_WITHOUT_ACTORS = ("quality", "constraint")


def failure(errors: list[str]) -> dict:
    """Informe de un inventario que no se puede usar: hay que repararlo."""
    return {"valid": False, "needs_repair": True, "repair_reasons": errors, "errors": errors, "rejected": [],
            "warnings": [], "metrics": {}, "inventory": None, "ids": {}}


def check_text(raw: str, fragments: dict | None, schema: dict) -> dict:
    """Valida el contenido de output/inventario.json tal cual (puede no ser JSON)."""
    try:
        inv = json.loads(raw)
    except json.JSONDecodeError as ex:
        return failure([f"INVENTARIO JSON no válido: {ex}"])
    return check(inv, fragments, schema)


def name_words(name: str, lang: str) -> set[str]:
    """Palabras de un nombre para REP: minúsculas, sin tildes, sin artículos ni preposiciones."""
    plain = unicodedata.normalize("NFD", name.lower())
    plain = "".join(ch for ch in plain if unicodedata.category(ch) != "Mn")
    return {w for w in re.findall(r"\w+", plain) if w not in NAME_STOPWORDS[lang]}


def jaccard(a: set, b: set) -> float:
    return len(a & b) / len(a | b) if a | b else 0.0


def check(inv, fragments: dict | None, schema: dict) -> dict:
    warnings: list[str] = []
    errors: list[str] = []

    # --- 1. Esquema --------------------------------------------------------------------------------
    root_errors, element_errors = [], defaultdict(list)
    for e in sorted(jsonschema.Draft202012Validator(schema).iter_errors(inv),
                    key=lambda e: [str(p) for p in e.absolute_path]):
        path = list(e.absolute_path)
        msg = f"SCHEMA {'/'.join(map(str, path)) or '(raíz)'}: {e.message}"
        if len(path) >= 2 and isinstance(path[1], int) and path[0] in LISTS:
            element_errors[(path[0], path[1])].append(msg)
        else:
            root_errors.append(f"INVENTARIO {msg}")
    if root_errors:
        return failure(root_errors)

    c = copy.deepcopy(inv)

    def label(item, fallback: str) -> str:
        return item["id"] if isinstance(item, dict) and isinstance(item.get("id"), str) else fallback

    for key, prefix in LISTS.items():
        kept = []
        for i, item in enumerate(c[key]):
            errs = element_errors.get((key, i))
            if errs:
                warnings += [f"{label(item, f'{prefix}[{i}]')} {m} (se ignora)" for m in errs]
            else:
                kept.append(item)
        c[key] = kept

    caps, discards, sections = c["capabilities"], c["discarded_statements"], c["discarded_sections"]
    qs = [{"item": q, "id": q["id"], "errors": []} for q in c["open_questions"]]
    doc_frag_ids = [fid for fid, f in (fragments or {}).items() if f["kind"] == "document"]

    # --- 2. Normalizador ---------------------------------------------------------------------------
    def fix(owner: str, what: str):
        warnings.append(f"{owner} FIX {what}")

    if fragments is not None:
        for cp in caps:
            missing = [f for f in cp["fragments"] if f not in fragments]
            if missing:
                fix(cp["id"], f"fragmentos inexistentes quitados: {', '.join(missing)}")
                cp["fragments"] = [f for f in cp["fragments"] if f in fragments]
        kept = []
        for i, d in enumerate(discards):
            if d["fragment"] not in fragments:
                fix(f"DISCARDED[{i}]", f"descarte de un fragmento inexistente {d['fragment']} quitado")
            else:
                kept.append(d)
        discards = kept

    for e in qs:  # citas en el fragmento equivocado (literales en uno solo) y citas repetidas
        seen, kept = set(), []
        for t in e["item"]["source_trace"]:
            if fragments is not None:
                frag = fragments.get(t["fragment"])
                if frag is None or not quote_in(t["quote"], frag["text"]):
                    found = [fid for fid, f in fragments.items() if quote_in(t["quote"], f["text"])]
                    if len(found) == 1:
                        fix(e["id"], f"cita movida de {t['fragment']} a {found[0]}, donde es literal")
                        t["fragment"] = found[0]
            key = (t["fragment"], quote_key(t["quote"]))
            if key in seen:
                fix(e["id"], f"cita repetida de {t['fragment']} quitada")
                continue
            seen.add(key)
            kept.append(t)
        e["item"]["source_trace"] = kept

    # IDS: un id repetido es error (las referencias serían ambiguas); una referencia a una funcionalidad
    # que no existe se quita.
    for what, ids in (("funcionalidad", [cp["id"] for cp in caps]), ("pregunta", [e["id"] for e in qs])):
        for dup in [x for x, n in Counter(ids).items() if n > 1]:
            errors.append(f"INVENTARIO IDS id de {what} repetido: {dup}")
    cap_ids = {cp["id"] for cp in caps}
    for e in qs:
        refs = e["item"]["related_capabilities"]
        for x in refs:
            if x not in cap_ids:
                fix(e["id"], f"referencia a funcionalidad inexistente {x} quitada")
        e["item"]["related_capabilities"] = list(dict.fromkeys(x for x in refs if x in cap_ids))

    # Exclusividad: un fragmento asignado a una funcionalidad no está descartado. Se quita su descarte; una
    # sección descartada con algún fragmento asignado se sustituye por descartes de sus demás fragmentos,
    # con el motivo de la sección (sin repetir los que ya tienen descarte).
    owner_of = {}
    for cp in caps:
        for f in cp["fragments"]:
            owner_of.setdefault(f, cp["id"])
    kept = []
    for i, d in enumerate(discards):
        if d["fragment"] in owner_of:
            fix(f"DISCARDED[{i}]", f"descarte de {d['fragment']} quitado: el fragmento está en {owner_of[d['fragment']]}")
        else:
            kept.append(d)
    discards = kept
    if fragments is not None:
        kept = []
        for i, d in enumerate(sections):
            inside = [fid for fid in doc_frag_ids if in_section(fragments[fid]["section"], d["section"])]
            assigned = [fid for fid in inside if fid in owner_of]
            if not assigned:
                kept.append(d)
                continue
            already = {x["fragment"] for x in discards}
            added = [fid for fid in inside if fid not in owner_of and fid not in already]
            discards += [{"fragment": fid, "reason": d["reason"], "note": None} for fid in added]
            fix(f"SECTION[{i}]", f"sección '{d['section']}' sustituida por descartes de "
                                 f"{', '.join(added) or 'ningún fragmento'}: están asignados {', '.join(assigned)}")
        sections = kept
    c["discarded_statements"], c["discarded_sections"] = discards, sections

    # --- 3. Comprobaciones por elemento ------------------------------------------------------------
    trace_total, trace_ok = 0, 0
    for e in qs:  # C6: citas de las preguntas (rechazan la pregunta)
        for t in e["item"]["source_trace"]:
            if fragments is None:
                continue
            trace_total += 1
            frag = fragments.get(t["fragment"])
            if frag is None:
                e["errors"].append(f"C6 fragmento inexistente {t['fragment']}")
            elif not quote_in(t["quote"], frag["text"]):
                e["errors"].append(f"C6 cita no literal en {t['fragment']}: «{t['quote']}»")
            elif len(t["quote"].split()) < QUOTE_MIN_WORDS and trim_end(t["quote"]) != trim_end(frag["text"]):
                e["errors"].append(f"C6 cita de menos de {QUOTE_MIN_WORDS} palabras que no es el fragmento "
                                   f"entero: «{t['quote']}»")
            else:
                trace_ok += 1

    for cp in caps:  # KIND
        if cp["kind"] in KINDS_WITHOUT_ACTORS and cp["actors"]:
            warnings.append(f"{cp['id']} KIND una funcionalidad {cp['kind']} no lleva actores: "
                            f"{', '.join(cp['actors'])}")

    # REP: posibles repeticiones, por fragmentos compartidos o por nombres parecidos. Los fragmentos de
    # REP_SHARED o más funcionalidades (un resumen, una matriz de permisos) no cuentan.
    lang = c["language"]
    shared = Counter(f for cp in caps for f in set(cp["fragments"]))
    own = [{f for f in cp["fragments"] if shared[f] < REP_SHARED} for cp in caps]
    names = [name_words(cp["name"], lang) for cp in caps]
    for i in range(len(caps)):
        for j in range(i + 1, len(caps)):
            by_fragments = jaccard(own[i], own[j])
            by_name = jaccard(names[i], names[j])
            if by_fragments >= REP_FRAGMENTS or by_name >= REP_NAMES:
                reasons = []
                if by_fragments >= REP_FRAGMENTS:
                    reasons.append(f"comparten {len(own[i] & own[j])} de {len(own[i] | own[j])} fragmentos")
                if by_name >= REP_NAMES:
                    reasons.append("nombres parecidos")
                warnings.append(f"{caps[i]['id']} REP posible repetición con {caps[j]['id']}: {'; '.join(reasons)}")

    # --- 4. Comprobaciones del inventario ----------------------------------------------------------
    unprocessed = c["verdict"]["unprocessed_fragments"]
    if fragments is not None:
        section_frags = set()
        for i, d in enumerate(sections):
            inside = {fid for fid in doc_frag_ids if in_section(fragments[fid]["section"], d["section"])}
            if not inside:
                warnings.append(f"SECTION[{i}] C10 la sección '{d['section']}' no tiene fragmentos (se ignora)")
            section_frags |= inside
        treated = set(owner_of) | {d["fragment"] for d in discards} | section_frags
        treated |= {t["fragment"] for e in qs for t in e["item"]["source_trace"]}
        untreated = [fid for fid in doc_frag_ids if fid not in treated]
        if untreated:  # umbral 0: un fragmento sin asignar no llega a ningún lote
            errors.append(f"INVENTARIO C10 fragmentos sin tratar: {', '.join(untreated)}")
        if unprocessed:
            warnings.append(f"INVENTARIO C10 fragmentos declarados sin procesar: {', '.join(sorted(unprocessed))}")

    status = c["verdict"]["status"]
    if status == "failed":
        errors.append("INVENTARIO VERDICT el inventario declara status failed")
    if status == "complete" and unprocessed:
        warnings.append("INVENTARIO VERDICT status complete con unprocessed_fragments no vacío")
    if status == "partial" and not unprocessed:
        warnings.append("INVENTARIO VERDICT status partial sin unprocessed_fragments")

    # --- 5. Rechazos y renumeración ----------------------------------------------------------------
    rejected = [{"id": e["id"], "element": "question", "errors": e["errors"]} for e in qs if e["errors"]]
    accepted_qs = [e for e in qs if not e["errors"]]
    new_caps = {}
    for i, cp in enumerate(caps, 1):
        new_caps.setdefault(cp["id"], f"CAP-{i:03d}")
    new_qs = {}
    for i, e in enumerate(accepted_qs, 1):
        new_qs.setdefault(e["id"], f"QI-{i:03d}")
    renumbered = sum(old != new for old, new in new_caps.items()) + sum(old != new for old, new in new_qs.items())
    out_caps = [{**cp, "id": new_caps[cp["id"]]} for cp in caps]
    out_qs = []
    for e in accepted_qs:
        q = e["item"]
        out_qs.append({**q, "id": new_qs[q["id"]],
                       "related_capabilities": [new_caps[x] for x in q["related_capabilities"]]})
    if renumbered:
        warnings.append(f"INVENTARIO FIX {renumbered} ids renumerados (CAP- y QI-, en orden de aparición)")

    # --- Métricas ----------------------------------------------------------------------------------
    assigned_docs = [fid for fid in doc_frag_ids if fid in owner_of]
    metrics = {
        "funcionalidades": len(caps),
        "por_kind": {k: sum(cp["kind"] == k for cp in caps)
                     for k in ("functional", "integration", "quality", "constraint")},
        "fragmentos_por_funcionalidad": round(sum(len(cp["fragments"]) for cp in caps) / len(caps), 2)
        if caps else None,
        "preguntas": len(accepted_qs),
        "preguntas_rechazadas": len(rejected),
        "preguntas_por_tipo": {k: sum(e["item"]["type"] == k for e in accepted_qs)
                               for k in ("scope", "contradiction", "ambiguity", "missing_actor")},
        "descartes": len(discards),
        "secciones_descartadas": len(sections),
        "fragmentos_asignados": f"{len(assigned_docs)}/{len(doc_frag_ids)}" if fragments is not None
        else "no comprobado",
        "citas_literales": f"{trace_ok}/{trace_total}" if fragments is not None else "no comprobado",
        "posibles_repeticiones": sum(w.split()[1] == "REP" for w in warnings),
        "correcciones": sum(1 for w in warnings if len(w.split()) > 1 and w.split()[1] == "FIX"),
    }

    valid = not errors
    inventory = {
        "schema_version": c["schema_version"], "system": c["system"], "language": c["language"],
        "capabilities": out_caps, "discarded_statements": discards, "discarded_sections": sections,
        "open_questions": out_qs, "verdict": c["verdict"],
    }
    return {"valid": valid, "needs_repair": bool(errors), "repair_reasons": list(errors), "errors": errors,
            "rejected": rejected, "warnings": warnings, "metrics": metrics,
            "inventory": inventory if valid else None, "ids": {**new_caps, **new_qs}}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("inventario")
    ap.add_argument("--fragmentos")
    ap.add_argument("--schema", default=str(Path(__file__).with_name(SCHEMA_FILE)))
    ap.add_argument("--normalizado")
    a = ap.parse_args()
    schema = json.loads(Path(a.schema).read_text(encoding="utf-8"))
    fragments = load_fragments(a.fragmentos) if a.fragmentos else None
    report = check_text(Path(a.inventario).read_text(encoding="utf-8"), fragments, schema)
    if a.normalizado and report["inventory"] is not None:
        Path(a.normalizado).write_text(json.dumps(report["inventory"], ensure_ascii=False, indent=2) + "\n",
                                       encoding="utf-8")
    report.pop("inventory")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    sys.exit(1 if report["needs_repair"] else 0)


if __name__ == "__main__":
    main()
