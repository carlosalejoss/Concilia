#!/usr/bin/env python3
"""
Batería de mutaciones del validador v1.1.

Cada caso parte del catálogo de ejemplo (válido), introduce un único error y comprueba que el
validador lo detecta con el código esperado. Incluye los 16 casos que el validador v1 dejaba pasar.

Uso:
    python test_validador.py                   # ejecuta la batería; código 1 si algún caso falla
    python test_validador.py --exportar DIR    # escribe cada caso como fixture (JSON + código esperado)
                                               # para los tests del port a C#
"""
import argparse
import copy
import json
import sys
from pathlib import Path

HERE = Path(__file__).parent
sys.path.insert(0, str(HERE))
from validar_catalogo import check, load_fragments  # noqa: E402

SISTEMA = "el sistema ReservaSalas"
BASE = json.loads((HERE / "ejemplo/catalogo_ejemplo.json").read_text(encoding="utf-8"))
SCHEMA = json.loads((HERE / "requirements.schema.json").read_text(encoding="utf-8"))
FRAGMENTS = load_fragments(str(HERE / "ejemplo/fragmentos.json"))


def req(c, rid):
    return next(r for r in c["requirements"] if r["id"] == rid)


def q(c, qid):
    return next(x for x in c["open_questions"] if x["id"] == qid)


# (nombre, código esperado al principio de algún error tras el id, mutación)
CASES = [
    # --- Casos que el validador v1 dejaba pasar ---
    ("property con escalation_reason de condición", "C12",
     lambda c: req(c, "REQ-003").update(escalation_reason="data_condition")),
    ("property marcada functional", "SCHEMA",
     lambda c: req(c, "REQ-003").update(kind="functional")),
    ("slot terminado en punto", "C14",
     lambda c: req(c, "REQ-001")["slots"].update(response=req(c, "REQ-001")["slots"]["response"] + ".")),
    ("SHOULD con obligation_basis implicit", "SCHEMA",
     lambda c: req(c, "REQ-012").update(obligation_basis="implicit", obligation_evidence=None)),
    ("obligation_evidence ausente del fragmento", "C13",
     lambda c: req(c, "REQ-009").update(obligation_evidence="obligatoriamente")),
    ("fragmento citado equivocado (antes: locator falso)", "C6",
     lambda c: req(c, "REQ-002")["source_trace"][0].update(fragment="DOC-01#F012")),
    ("out_of_subset en un requisito EARS normal", "C12",
     lambda c: req(c, "REQ-001").update(out_of_subset="process")),
    ("origin clarification sin traza a dudas", "C16",
     lambda c: req(c, "REQ-002").update(origin="clarification")),
    ("pregunta no bidireccional", "C11",
     lambda c: q(c, "Q-001").update(related_requirements=["REQ-002"])),
    ("status complete con fragmentos sin procesar", "VERDICT",
     lambda c: c["verdict"].update(unprocessed_fragments=["DOC-01#F014"])),
    ("keyword en mayúsculas dentro de un slot", "C14",
     lambda c: req(c, "REQ-006")["slots"].update(response="impedir la reserva Y DEBERÁ avisar")),
    ("fragmento perdido (C10)", "C10",
     lambda c: (c["discarded_statements"].pop(), c["verdict"]["counts"].update(discarded=1))),
    ("subject_matter sin preposición", "C14",
     lambda c: req(c, "REQ-003")["slots"].update(subject_matter="la confirmación de una reserva")),
    ("EARS con escalation_fallback fuera de D1/D4", "C12",
     lambda c: req(c, "REQ-004").update(escalation_fallback="no se pudo rellenar")),
    ("functional con escalation_reason de propiedad", "C12",
     lambda c: req(c, "REQ-009").update(escalation_reason="non_functional_property")),
    ("self_check mentiroso se registra y el fallo real bloquea", "C6",
     lambda c: req(c, "REQ-004")["source_trace"][0].update(quote="se rechaza la solicitud")),
    # --- Reglas nuevas de v1.1 ---
    ("prohibición en negativo (R-e)", "C15",
     lambda c: req(c, "REQ-006")["slots"].update(response="no permitir la reserva de esa sala")),
    ("término vago de la categoría escape", "C5",
     lambda c: req(c, "REQ-001")["slots"].update(response=req(c, "REQ-001")["slots"]["response"] + " si procede")),
    ("término vago de la categoría pronombre", "C5",
     lambda c: req(c, "REQ-005")["slots"].update(response="informar al empleado de lo anterior")),
    ("valor pendiente sin pregunta missing_value", "N3",
     lambda c: (req(c, "REQ-011").update(open_question_ids=[]), q(c, "Q-002").update(related_requirements=[]))),
    ("D4 aplicado a estados en vez de datos (EARS con D4)", "C12",
     lambda c: req(c, "REQ-010").update(decision_branch="D4")),
    ("D3 con obligación SHALL", "C12",
     lambda c: req(c, "REQ-012").update(obligation="SHALL", escalation_reason="non_mandatory_obligation")),
    ("D2 sin out_of_subset", "C12",
     lambda c: req(c, "REQ-013").update(out_of_subset=None)),
    ("state_driven sin estados", "C2",
     lambda c: req(c, "REQ-006")["slots"].update(states=[])),
    ("complex con un solo tipo de condición", "C2",
     lambda c: req(c, "REQ-008")["slots"].update(trigger=None, trigger_keyword=None)),
    ("campo text escrito por el modelo", "SCHEMA",
     lambda c: req(c, "REQ-001").update(text="El sistema ReservaSalas DEBERÁ …")),
    ("MAY en EARS", "SCHEMA",
     lambda c: req(c, "REQ-001").update(obligation="MAY", obligation_basis="explicit", obligation_evidence="puede")),
    ("sistema distinto de nombre_sistema", "C3",
     lambda c: req(c, "REQ-002")["slots"].update(system="la aplicación")),
    ("recuento del veredicto incorrecto", "VERDICT",
     lambda c: c["verdict"]["counts"].update(requirements=12)),
    ("fragmento inexistente en un descarte", "C6",
     lambda c: c["discarded_statements"][0].update(fragment="DOC-01#F099")),
]


def run_case(mutate):
    c = copy.deepcopy(BASE)
    mutate(c)
    return c, check(c, SISTEMA, FRAGMENTS, SCHEMA)


def error_codes(report):
    codes = set()
    for e in report["errors"]:
        head = e.split()
        # "REQ-001 C14 …", "Q-001 C11 …", "DISCARDED[0] C6 …", "C10 …", "SCHEMA …", "VERDICT …"
        codes.add(head[1] if head[0].startswith(("REQ-", "Q-", "DISCARDED[")) else head[0])
    return codes


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exportar")
    a = ap.parse_args()

    base_report = check(copy.deepcopy(BASE), SISTEMA, FRAGMENTS, SCHEMA)
    failures = 0
    if not base_report["valid"]:
        print("FALLO  | el ejemplo base no es válido:", base_report["errors"])
        failures += 1
    else:
        print("OK     | el ejemplo base es válido")

    out_dir = Path(a.exportar) if a.exportar else None
    if out_dir:
        out_dir.mkdir(parents=True, exist_ok=True)
    manifest = []
    for i, (name, code, mutate) in enumerate(CASES, 1):
        cat, report = run_case(mutate)
        codes = error_codes(report)
        ok = (not report["valid"]) and code in codes
        failures += not ok
        print(f"{'OK    ' if ok else 'FALLO '} | {code:<7} | {name}" + ("" if ok else f"  -> {sorted(codes)}"))
        if out_dir:
            fname = f"mut-{i:02d}.json"
            (out_dir / fname).write_text(json.dumps(cat, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
            manifest.append({"file": fname, "name": name, "expected_code": code})
    if out_dir:
        (out_dir / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
                                               encoding="utf-8")
    print(f"\n{len(CASES) - failures + (1 if base_report['valid'] else 0)}/{len(CASES) + 1} correctos")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
