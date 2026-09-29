#!/usr/bin/env python3
"""
Batería de mutaciones del validador v1.2.

Cada caso parte de un catálogo de ejemplo válido (`ejemplo/`, en español, o `ejemplo_en/`, en inglés),
introduce un único error y comprueba que el validador lo detecta con el código esperado. Los casos de
aviso comprueban que el catálogo sigue siendo válido y que aparece el aviso esperado.

Uso:
    python test_validador.py                   # ejecuta la batería; código 1 si algún caso falla
    python test_validador.py --exportar DIR    # escribe cada caso como fixture (JSON + código esperado,
                                               # ejemplo de origen y nombre del sistema) para el port a C#
"""
import argparse
import copy
import json
import sys
from pathlib import Path

HERE = Path(__file__).parent
sys.path.insert(0, str(HERE))
from validar_catalogo import check, load_fragments  # noqa: E402

SCHEMA = json.loads((HERE / "requirements.schema.json").read_text(encoding="utf-8"))
EXAMPLES = {
    "ejemplo": "el sistema ReservaSalas",
    "ejemplo_en": "the LibraryLoan system",
}
BASES = {name: json.loads((HERE / name / "catalogo_ejemplo.json").read_text(encoding="utf-8")) for name in EXAMPLES}
FRAGMENTS = {name: load_fragments(str(HERE / name / "fragmentos.json")) for name in EXAMPLES}
ES, EN = "ejemplo", "ejemplo_en"


def req(c, rid):
    return next(r for r in c["requirements"] if r["id"] == rid)


def q(c, qid):
    return next(x for x in c["open_questions"] if x["id"] == qid)


# (nombre, código esperado al principio de algún error tras el id, mutación)
# (ejemplo, nombre, código esperado al principio de algún error tras el id, mutación)
CASES = [
    # --- Casos que el validador v1 dejaba pasar ---
    (ES, "property con escalation_reason de condición", "C12",
     lambda c: req(c, "REQ-003").update(escalation_reason="data_condition")),
    (ES, "property marcada functional", "SCHEMA",
     lambda c: req(c, "REQ-003").update(kind="functional")),
    (ES, "slot terminado en punto", "C14",
     lambda c: req(c, "REQ-001")["slots"].update(response=req(c, "REQ-001")["slots"]["response"] + ".")),
    (ES, "SHOULD con obligation_basis implicit", "SCHEMA",
     lambda c: req(c, "REQ-012").update(obligation_basis="implicit", obligation_evidence=None)),
    (ES, "obligation_evidence ausente del fragmento", "C13",
     lambda c: req(c, "REQ-009").update(obligation_evidence="obligatoriamente")),
    (ES, "fragmento citado equivocado (antes: locator falso)", "C6",
     lambda c: req(c, "REQ-002")["source_trace"][0].update(fragment="DOC-01#F012")),
    (ES, "out_of_subset en un requisito EARS normal", "C12",
     lambda c: req(c, "REQ-001").update(out_of_subset="process")),
    (ES, "origin clarification sin traza a dudas", "C16",
     lambda c: req(c, "REQ-002").update(origin="clarification")),
    (ES, "pregunta no bidireccional", "C11",
     lambda c: q(c, "Q-001").update(related_requirements=["REQ-002"])),
    (ES, "status complete con fragmentos sin procesar", "VERDICT",
     lambda c: c["verdict"].update(unprocessed_fragments=["DOC-01#F014"])),
    (ES, "keyword en mayúsculas dentro de un slot", "C14",
     lambda c: req(c, "REQ-006")["slots"].update(response="impedir la reserva Y DEBERÁ avisar")),
    (ES, "fragmento perdido (C10)", "C10",
     lambda c: (c["discarded_statements"].pop(), c["verdict"]["counts"].update(discarded=1))),
    (ES, "subject_matter sin preposición", "C14",
     lambda c: req(c, "REQ-003")["slots"].update(subject_matter="la confirmación de una reserva")),
    (ES, "EARS con escalation_fallback fuera de D1/D4", "C12",
     lambda c: req(c, "REQ-004").update(escalation_fallback="no se pudo rellenar")),
    (ES, "functional con escalation_reason de propiedad", "C12",
     lambda c: req(c, "REQ-009").update(escalation_reason="non_functional_property")),
    (ES, "self_check mentiroso se registra y el fallo real bloquea", "C6",
     lambda c: req(c, "REQ-004")["source_trace"][0].update(quote="se rechaza la solicitud")),
    # --- Reglas nuevas de v1.1 ---
    (ES, "prohibición en negativo (R-e)", "C15",
     lambda c: req(c, "REQ-006")["slots"].update(response="no permitir la reserva de esa sala")),
    (ES, "término vago de la categoría escape", "C5",
     lambda c: req(c, "REQ-001")["slots"].update(response=req(c, "REQ-001")["slots"]["response"] + " si procede")),
    (ES, "término vago de la categoría pronombre", "C5",
     lambda c: req(c, "REQ-005")["slots"].update(response="informar al empleado de lo anterior")),
    (ES, "valor pendiente sin pregunta missing_value", "N3",
     lambda c: (req(c, "REQ-011").update(open_question_ids=[]), q(c, "Q-002").update(related_requirements=[]))),
    (ES, "D4 aplicado a estados en vez de datos (EARS con D4)", "C12",
     lambda c: req(c, "REQ-010").update(decision_branch="D4")),
    (ES, "D3 con obligación SHALL", "C12",
     lambda c: req(c, "REQ-012").update(obligation="SHALL", escalation_reason="non_mandatory_obligation")),
    (ES, "D2 sin out_of_subset", "C12",
     lambda c: req(c, "REQ-013").update(out_of_subset=None)),
    (ES, "state_driven sin estados", "C2",
     lambda c: req(c, "REQ-006")["slots"].update(states=[])),
    (ES, "complex con un solo tipo de condición", "C2",
     lambda c: req(c, "REQ-008")["slots"].update(trigger=None, trigger_keyword=None)),
    (ES, "campo text escrito por el modelo", "SCHEMA",
     lambda c: req(c, "REQ-001").update(text="El sistema ReservaSalas DEBERÁ …")),
    (ES, "MAY en EARS", "SCHEMA",
     lambda c: req(c, "REQ-001").update(obligation="MAY", obligation_basis="explicit", obligation_evidence="puede")),
    (ES, "sistema distinto de nombre_sistema", "C3",
     lambda c: req(c, "REQ-002")["slots"].update(system="la aplicación")),
    (ES, "recuento del veredicto incorrecto", "VERDICT",
     lambda c: c["verdict"]["counts"].update(requirements=12)),
    (ES, "fragmento inexistente en un descarte", "C6",
     lambda c: c["discarded_statements"][0].update(fragment="DOC-01#F099")),
    # --- Reglas nuevas de v1.2 ---
    (ES, "cifra inventada (N2)", "N2",
     lambda c: req(c, "REQ-003")["slots"].update(value="30 segundos")),
    (ES, "cifra convertida de unidad (4 horas -> 240 minutos)", "N2",
     lambda c: req(c, "REQ-009")["slots"].update(condition="la reserva dura más de 240 minutos y tiene más de 20 asistentes")),
    (ES, "cita trivial de menos de tres palabras", "C6",
     lambda c: req(c, "REQ-002")["source_trace"][0].update(quote="la reserva")),
    (ES, "evidencia modal trivial", "C13",
     lambda c: req(c, "REQ-009").update(obligation_evidence="a")),
    (ES, "evidencia de SHOULD con obligación MAY", "C13",
     lambda c: req(c, "REQ-012").update(obligation="MAY")),
    (ES, "'puede' de capacidad marcado como SHALL explícito", "C13",
     lambda c: req(c, "REQ-001").update(obligation_basis="explicit", obligation_evidence="puede")),
    (ES, "fragmento descartado entero y citado por un requisito", "C17",
     lambda c: req(c, "REQ-002")["source_trace"].append(
         {"fragment": "DOC-01#F001", "quote": "reducir los conflictos de uso"})),
    (EN, "'will' marcado como SHALL explícito", "C13",
     lambda c: req(c, "REQ-002").update(obligation_basis="explicit", obligation_evidence="will")),
    (EN, "término de dominio sin declarar (C5)", "C5",
     lambda c: c.update(domain_terms=[])),
    (EN, "término de dominio de una sola palabra", "C5",
     lambda c: c["domain_terms"][0].update(term="Flexible")),
    (EN, "término de dominio que no está en el fragmento", "C5",
     lambda c: c["domain_terms"][0].update(term="Flexible Return plan")),
    (EN, "self_check de cifras mentiroso: el fallo real bloquea", "N2",
     lambda c: req(c, "REQ-008")["slots"].update(value="1.5 seconds")),
]

# Avisos: el catálogo sigue siendo válido y aparece el código esperado entre los avisos.
WARNING_CASES = [
    (ES, "dos verbos coordinados en response (C4)", "C4",
     lambda c: req(c, "REQ-002")["slots"].update(response="enviar un correo de confirmación y registrar el envío")),
    (ES, "orden de claves distinto de §10 (C18)", "C18",
     lambda c: c["requirements"].__setitem__(1, dict(reversed(list(req(c, "REQ-002").items()))))),
]


def run_case(example, mutate):
    c = copy.deepcopy(BASES[example])
    mutate(c)
    return c, check(c, EXAMPLES[example], FRAGMENTS[example], SCHEMA)


def error_codes(report):
    codes = set()
    for e in report["errors"]:
        head = e.split()
        # "REQ-001 C14 …", "Q-001 C11 …", "DISCARDED[0] C6 …", "DOMAIN[0] C5 …", "C10 …", "SCHEMA …", "VERDICT …"
        codes.add(head[1] if head[0].startswith(("REQ-", "Q-", "DISCARDED[", "DOMAIN[")) else head[0])
    return codes


def warning_codes(report):
    return {w.split()[1] for w in report["warnings"] if len(w.split()) > 1}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exportar")
    a = ap.parse_args()

    failures, total = 0, 0
    for example, sistema in EXAMPLES.items():
        total += 1
        base_report = check(copy.deepcopy(BASES[example]), sistema, FRAGMENTS[example], SCHEMA)
        ok = base_report["valid"] and not base_report["warnings"]
        failures += not ok
        print(f"{'OK    ' if ok else 'FALLO '} | base    | {example} es válido y sin avisos"
              + ("" if ok else f"  -> {base_report['errors'] + base_report['warnings']}"))

    out_dir = Path(a.exportar) if a.exportar else None
    if out_dir:
        out_dir.mkdir(parents=True, exist_ok=True)
    manifest = []
    for i, (example, name, code, mutate) in enumerate(CASES, 1):
        total += 1
        cat, report = run_case(example, mutate)
        codes = error_codes(report)
        ok = (not report["valid"]) and code in codes
        failures += not ok
        print(f"{'OK    ' if ok else 'FALLO '} | {code:<7} | {name}" + ("" if ok else f"  -> {sorted(codes)}"))
        if out_dir:
            fname = f"mut-{i:02d}.json"
            (out_dir / fname).write_text(json.dumps(cat, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
            manifest.append({"file": fname, "name": name, "example": example, "sistema": EXAMPLES[example],
                             "expected_code": code, "kind": "error"})
    for i, (example, name, code, mutate) in enumerate(WARNING_CASES, 1):
        total += 1
        cat, report = run_case(example, mutate)
        ok = report["valid"] and code in warning_codes(report)
        failures += not ok
        print(f"{'OK    ' if ok else 'FALLO '} | {code:<7} | aviso: {name}"
              + ("" if ok else f"  -> {report['errors'] + report['warnings']}"))
        if out_dir:
            fname = f"warn-{i:02d}.json"
            (out_dir / fname).write_text(json.dumps(cat, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
            manifest.append({"file": fname, "name": name, "example": example, "sistema": EXAMPLES[example],
                             "expected_code": code, "kind": "warning"})
    if out_dir:
        (out_dir / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
                                               encoding="utf-8")
    print(f"\n{total - failures}/{total} correctos")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
