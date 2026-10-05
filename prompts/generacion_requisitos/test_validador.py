#!/usr/bin/env python3
"""
Batería de mutaciones de los validadores: el del catálogo v1.3 (validar_catalogo.py) y el del inventario v2
(validar_inventario.py).

Cada caso parte de un ejemplo válido (`ejemplo/`, en español, o `ejemplo_en/`, en inglés), introduce un único
cambio y comprueba cómo lo trata el validador, según su nivel:

- reject:  el lote se acepta y el elemento indicado se rechaza con ese código.
- lot:     error del lote con ese código (el lote no se puede usar y se repara).
- warning: el lote se acepta sin rechazos ni reparación y aparece el aviso.
- fix:     como warning, con el aviso FIX del normalizador.
- accept:  el lote se acepta sin rechazos, sin reparación y sin avisos.
- repair:  el lote se puede usar, pero necesita reparación por ese código (C20 o rechazos de más del 20 %).

En el inventario, "lot" es un error del inventario entero (prefijo INVENTARIO), que siempre pide reparación.
Algunos casos comprueban además el resultado (ids renumerados, vínculos completados…).

Uso:
    python test_validador.py                   # ejecuta la batería; código 1 si algún caso falla
    python test_validador.py --exportar DIR    # escribe cada caso como fixture para el port a C#, en
                                               # DIR/catalog-mutations y DIR/inventory-mutations
                                               # (fichero, nivel, código esperado, ejemplo y sistema)
"""
import argparse
import copy
import json
import sys
from pathlib import Path

HERE = Path(__file__).parent
sys.path.insert(0, str(HERE))
from validar_catalogo import SCHEMA_FILE, check_text, enrich, load_fragments  # noqa: E402
import validar_inventario  # noqa: E402

SCHEMA = json.loads((HERE / SCHEMA_FILE).read_text(encoding="utf-8"))
INV_SCHEMA = json.loads((HERE / validar_inventario.SCHEMA_FILE).read_text(encoding="utf-8"))
EXAMPLES = {
    "ejemplo": "el sistema ReservaSalas",
    "ejemplo_en": "the LibraryLoan system",
}
BASE_FILE = "catalogo_ejemplo_v1.3.json"
ENRICHED_FILE = "catalogo_ejemplo_v1.3.enriquecido.json"
BASES = {name: json.loads((HERE / name / BASE_FILE).read_text(encoding="utf-8")) for name in EXAMPLES}
FRAGMENTS = {name: load_fragments(str(HERE / name / "fragmentos.json")) for name in EXAMPLES}
ES, EN = "ejemplo", "ejemplo_en"


def req(c, rid):
    return next(r for r in c["requirements"] if r["id"] == rid)


def q(c, qid):
    return next(x for x in c["open_questions"] if x["id"] == qid)


def cap(c, cid):
    return next(x for x in c["capabilities"] if x["id"] == cid)


def final_req(report, rid):
    return next(r for r in report["catalog"]["requirements"] if r["id"] == rid)


# Inventario v2: el de cada ejemplo, con los fragmentos de todo su documento.
INV_FILE = "inventario_ejemplo_v2.json"
INV_FRAGMENTS_FILE = {ES: "fragmentos_v2.json", EN: "fragmentos.json"}
INV_BASES = {name: json.loads((HERE / name / INV_FILE).read_text(encoding="utf-8")) for name in EXAMPLES}
INV_FRAGMENTS = {name: load_fragments(str(HERE / name / INV_FRAGMENTS_FILE[name])) for name in EXAMPLES}


def disc(c, fid):
    return next(d for d in c["discarded_statements"] if d["fragment"] == fid)


def new_cap(cid, name, kind, fragments, actors=()):
    return {"id": cid, "name": name, "description": f"Funcionalidad de prueba {cid}.", "kind": kind,
            "actors": list(actors), "fragments": fragments}


def final_inv(report):
    return report["inventory"]


# Mismo formato que CASES, sobre el inventario.
INVENTORY_CASES = [
    # --- Errores del inventario (reparación) ---
    (ES, "no es JSON", "lot", None, "JSON", lambda c: "{ esto no es JSON"),
    (ES, "versión de esquema 1.3", "lot", None, "SCHEMA", lambda c: c.update(schema_version="1.3")),
    (ES, "fragmento sin tratar (C10, umbral 0)", "lot", None, "C10",
     lambda c: c["discarded_statements"].remove(disc(c, "DOC-01#F014"))),
    (EN, "fragmento sin tratar en inglés (C10)", "lot", None, "C10",
     lambda c: c["discarded_statements"].remove(disc(c, "DOC-01#F003"))),
    (ES, "id de funcionalidad repetido", "lot", None, "IDS",
     lambda c: cap(c, "CAP-002").update(id="CAP-001")),
    (ES, "id de pregunta repetido", "lot", None, "IDS",
     lambda c: c["open_questions"].append(copy.deepcopy(c["open_questions"][0]))),
    (ES, "el inventario declara status failed", "lot", None, "VERDICT",
     lambda c: c["verdict"].update(status="failed")),

    # --- Rechazo de una pregunta ---
    (ES, "cita no literal en una pregunta", "reject", "QI-001", "C6",
     lambda c: c["open_questions"][0]["source_trace"][0].update(quote="una cita que no está en el documento")),

    # --- Avisos ---
    (ES, "descarte que no cumple el esquema (se ignora)", "warning", None, "SCHEMA",
     lambda c: c["discarded_statements"].append({"fragment": "DOC-01#F001", "reason": "no_existe", "note": None})),
    (ES, "quality con actores", "warning", None, "KIND",
     lambda c: cap(c, "CAP-005").update(actors=["el empleado"])),
    (ES, "funcionalidad repetida por fragmentos", "warning", None, "REP",
     lambda c: c["capabilities"].append(new_cap("CAP-007", "contratar catering", "functional", ["DOC-01#F006"]))),
    (ES, "funcionalidad repetida por nombre", "warning", None, "REP",
     lambda c: c["capabilities"].append(new_cap("CAP-007", "reservar la sala", "functional", ["DOC-01#F013"]))),
    (ES, "un resumen compartido por tres funcionalidades no cuenta para REP", "accept", None, None,
     lambda c: c["capabilities"].append(new_cap("CAP-007", "anular una reserva", "functional",
                                                ["DOC-01#F013", "DOC-01#F021"]))),
    (ES, "status partial sin unprocessed_fragments", "warning", None, "VERDICT",
     lambda c: c["verdict"].update(status="partial")),
    (ES, "sección descartada que no existe", "warning", None, "C10",
     lambda c: c["discarded_sections"].append({"section": "§9", "reason": "project_note"})),

    # --- Correcciones del normalizador ---
    (ES, "referencia a una funcionalidad inexistente", "fix", None, "FIX",
     lambda c: c["open_questions"][0]["related_capabilities"].append("CAP-099"),
     lambda r: final_inv(r)["open_questions"][0]["related_capabilities"] == []),
    (ES, "fragmento inexistente en una funcionalidad", "fix", None, "FIX",
     lambda c: cap(c, "CAP-004")["fragments"].append("DOC-01#F099"),
     lambda r: final_inv(r)["capabilities"][3]["fragments"] == ["DOC-01#F011"]),
    (ES, "descarte de un fragmento inexistente", "fix", None, "FIX",
     lambda c: c["discarded_statements"].append({"fragment": "DOC-01#F099", "reason": "context", "note": None}),
     lambda r: len(final_inv(r)["discarded_statements"]) == 2),
    (ES, "fragmento asignado y descartado: se quita el descarte", "fix", None, "FIX",
     lambda c: c["discarded_statements"].append({"fragment": "DOC-01#F002", "reason": "context", "note": None}),
     lambda r: [d["fragment"] for d in final_inv(r)["discarded_statements"]] == ["DOC-01#F001", "DOC-01#F014"]),
    (ES, "sección descartada con fragmentos asignados: descartes de los demás", "fix", None, "FIX",
     lambda c: c["discarded_sections"].append({"section": "§3", "reason": "project_note"}),
     lambda r: [s["section"] for s in final_inv(r)["discarded_sections"]] == ["§5", "§6"]
     and final_inv(r)["discarded_statements"][-1] == {"fragment": "DOC-01#F013", "reason": "project_note",
                                                     "note": None}),
    (ES, "cita de una pregunta en el fragmento equivocado", "fix", None, "FIX",
     lambda c: c["open_questions"][0]["source_trace"][0].update(fragment="DOC-01#F012"),
     lambda r: final_inv(r)["open_questions"][0]["source_trace"][0]["fragment"] == "DOC-01#F013"),
    (ES, "ids con otro formato: se renumeran CAP- y QI-", "fix", None, "FIX",
     lambda c: (cap(c, "CAP-001").update(id="F1"), c["open_questions"][0].update(id="P1", related_capabilities=["F1"])),
     lambda r: final_inv(r)["capabilities"][0]["id"] == "CAP-001"
     and final_inv(r)["open_questions"][0]["id"] == "QI-001"
     and final_inv(r)["open_questions"][0]["related_capabilities"] == ["CAP-001"]
     and r["ids"]["F1"] == "CAP-001"),
]


# (ejemplo, nombre, nivel, elemento afectado o None, código esperado, mutación[, comprobación extra])
# Una mutación que devuelve una cadena sustituye el fichero entero por ese texto.
CASES = [
    # --- Rechazo de un requisito o una pregunta ---
    (ES, "property en una rama distinta de D1", "reject", "RNF-001", "C12",
     lambda c: req(c, "RNF-001").update(decision_branch="D5")),
    (ES, "RA fuera de D2", "reject", "RA-001", "C12",
     lambda c: req(c, "RA-001").update(decision_branch="D5")),
    (ES, "RF en D1", "reject", "RF-001", "C12",
     lambda c: req(c, "RF-001").update(decision_branch="D1")),
    (ES, "D3 con obligación SHALL", "reject", "RF-007", "C12",
     lambda c: req(c, "RF-007").update(obligation="SHALL")),
    (ES, "EARS con escalation_fallback fuera de D1/D4", "reject", "RF-001", "C12",
     lambda c: req(c, "RF-001").update(escalation_fallback="no se pudo rellenar")),
    (ES, "state_driven sin estados", "reject", "RF-006", "C2",
     lambda c: req(c, "RF-006")["slots"].update(states=[])),
    (ES, "complex con un solo tipo de condición", "reject", "RF-004", "C2",
     lambda c: req(c, "RF-004")["slots"].update(trigger=None, trigger_keyword=None)),
    (ES, "event_driven con trigger_keyword IF", "reject", "RF-002", "C2",
     lambda c: req(c, "RF-002")["slots"].update(trigger_keyword="IF")),
    (ES, "término vago en un slot (escape)", "reject", "RF-001", "C5",
     lambda c: req(c, "RF-001")["slots"].update(response="permitir al empleado reservar una sala si procede")),
    (ES, "término vago en un criterio", "reject", "RF-001", "C5",
     lambda c: req(c, "RF-001")["acceptance_criteria"].append("[RULE] La reserva se confirma de forma rápida.")),
    (ES, "pronombre vago", "reject", "RF-002", "C5",
     lambda c: req(c, "RF-002")["slots"].update(response="enviar lo anterior al empleado")),
    (ES, "cita no literal en ningún fragmento", "reject", "RF-005", "C6",
     lambda c: req(c, "RF-005")["source_trace"][0].update(
         quote="Si la reserva es larga, deberá aprobarla el administrador")),
    (ES, "cita trivial de menos de tres palabras", "reject", "RF-002", "C6",
     lambda c: req(c, "RF-002")["source_trace"][0].update(quote="la reserva")),
    (ES, "fragmento inexistente y cita que no está en el lote", "reject", "RF-003", "C6",
     lambda c: req(c, "RF-003")["source_trace"][0].update(fragment="DOC-01#F099",
                                                         quote="Si la empresa compra el módulo de catering")),
    (ES, "MAY en EARS", "reject", "RF-001", "SCHEMA",
     lambda c: req(c, "RF-001").update(obligation="MAY", obligation_basis="explicit", obligation_evidence="puede")),
    (ES, "campo text escrito por el modelo", "reject", "RF-001", "SCHEMA",
     lambda c: req(c, "RF-001").update(text="El sistema ReservaSalas DEBERÁ …")),
    (ES, "SHOULD con obligation_basis implicit", "reject", "RF-007", "SCHEMA",
     lambda c: req(c, "RF-007").update(obligation_basis="implicit", obligation_evidence=None)),
    (ES, "tipo de requisito desconocido", "reject", "RF-003", "SCHEMA",
     lambda c: req(c, "RF-003").update(type="RX")),
    (ES, "prohibición en negativo (R-e)", "reject", "RF-006", "C15",
     lambda c: req(c, "RF-006")["slots"].update(response="no mostrar al empleado sus reservas del día")),
    (ES, "cifra inventada en un criterio", "reject", "RF-002", "N2",
     lambda c: req(c, "RF-002").update(acceptance_criteria=["[LIMIT] El correo se envía en menos de 5 minutos."])),
    (ES, "cifra convertida de unidad (4 horas -> 240 minutos)", "reject", "RF-005", "N2",
     lambda c: req(c, "RF-005")["slots"].update(
         condition="la reserva dura más de 240 minutos y tiene más de 20 asistentes")),
    (ES, "valor pendiente sin pregunta missing_value", "reject", "RNF-001", "N3",
     lambda c: (req(c, "RNF-001").update(open_question_ids=[]), q(c, "Q-001").update(related_requirements=[]))),
    (ES, "pregunta con una cita que no está en la fuente", "reject", "Q-002", "C6",
     lambda c: q(c, "Q-002")["source_trace"][0].update(quote="el sistema se integrará con Gmail")),
    (EN, "cifra inventada en un slot", "reject", "RNF-001", "N2",
     lambda c: req(c, "RNF-001")["slots"].update(value="1.5 seconds")),
    (EN, "término de dominio sin declarar", "reject", "RF-005", "C5",
     lambda c: c.update(domain_terms=[])),
    (EN, "término de dominio de una sola palabra", "reject", "RF-005", "C5",
     lambda c: c["domain_terms"][0].update(term="Flexible")),

    # --- Errores del lote ---
    (ES, "falta el inventario de funcionalidades", "lot", None, "SCHEMA",
     lambda c: c.pop("capabilities")),
    (ES, "versión de esquema 1.2", "lot", None, "SCHEMA",
     lambda c: c.update(schema_version="1.2")),
    (ES, "más del 10 % de los fragmentos sin tratar", "lot", None, "C10",
     lambda c: c.update(discarded_statements=[])),
    (ES, "el catálogo declara status failed", "lot", None, "VERDICT",
     lambda c: c["verdict"].update(status="failed")),
    (ES, "fichero que no es JSON", "lot", None, "JSON",
     lambda c: '{"schema_version": "1.3", "requirements": ['),

    # --- Avisos ---
    (ES, "obligation_evidence ausente del fragmento", "warning", "RF-005", "C13",
     lambda c: req(c, "RF-005").update(obligation_evidence="obligatoriamente")),
    (ES, "'puede' de capacidad marcado como SHALL explícito", "warning", "RF-001", "C13",
     lambda c: req(c, "RF-001").update(obligation_basis="explicit", obligation_evidence="puede")),
    (EN, "'will' marcado como SHALL explícito", "warning", "RF-002", "C13",
     lambda c: req(c, "RF-002").update(obligation_basis="explicit", obligation_evidence="will")),
    (ES, "subject_matter sin preposición", "warning", "RNF-001", "C14",
     lambda c: req(c, "RNF-001")["slots"].update(subject_matter="el sistema ReservaSalas")),
    (ES, "keyword en mayúsculas dentro de un slot", "warning", "RF-006", "C14",
     lambda c: req(c, "RF-006")["slots"].update(response="mostrar al empleado sus reservas del día Y DEBERÁ avisar")),
    (ES, "origin clarification sin traza a dudas", "warning", "RF-002", "C16",
     lambda c: req(c, "RF-002").update(origin="clarification")),
    (ES, "criterio sin etiqueta", "warning", "RF-001", "C19",
     lambda c: req(c, "RF-001")["acceptance_criteria"].__setitem__(
         0, "El empleado indica la fecha, la hora de inicio, la hora de fin y el número de asistentes.")),
    (EN, "criterio que empieza por una palabra que es etiqueta, sin corchetes", "warning", "RF-002", "C19",
     lambda c: req(c, "RF-002")["acceptance_criteria"].append("Error messages are shown to the librarian."),
     lambda r: final_req(r, "RF-002")["acceptance_criteria"][-1] == "Error messages are shown to the librarian."),
    (ES, "RF sin capability_ids (su funcionalidad la cubre otro)", "warning", "RF-004", "C21",
     lambda c: req(c, "RF-004").update(capability_ids=[])),
    (ES, "verbo paraguas sin criterios [OPERATION]", "warning", "RF-001", "C22",
     lambda c: req(c, "RF-001")["slots"].update(response="permitir al empleado gestionar sus reservas")),
    (EN, "pregunta de ambigüedad sin requisitos relacionados", "warning", "Q-001", "C11",
     lambda c: (q(c, "Q-001").update(related_requirements=[]), req(c, "RF-002").update(open_question_ids=[]))),
    (ES, "un fragmento sin tratar (no llega al 10 %)", "warning", None, "C10",
     lambda c: c["discarded_statements"].pop()),
    (ES, "autoverificación que no coincide con el validador", "warning", None, "SELF_CHECK",
     lambda c: c["discarded_statements"].pop()),
    (ES, "status complete con fragmentos sin procesar", "warning", None, "VERDICT",
     lambda c: c["verdict"].update(unprocessed_fragments=["DOC-01#F014"])),
    (ES, "sección descartada que no está en el lote", "warning", None, "C10",
     lambda c: c["discarded_sections"].append({"section": "§9", "reason": "project_note"})),

    # --- Correcciones del normalizador ---
    (ES, "ids con otro formato: se renumeran por tipo", "fix", None, "FIX",
     lambda c: (req(c, "RF-001").update(id="REQ-1"), cap(c, "CAP-001").update(id="F-1"),
                [r.update(capability_ids=["F-1" if x == "CAP-001" else x for x in r["capability_ids"]])
                 for r in c["requirements"]]),
     lambda r: [x["id"] for x in r["catalog"]["requirements"][:2]] == ["RF-001", "RF-002"]
     and r["catalog"]["capabilities"][0]["id"] == "CAP-001" and final_req(r, "RF-004")["capability_ids"] == ["CAP-001"]),
    (ES, "vínculo declarado en un solo sentido", "fix", None, "FIX",
     lambda c: req(c, "RNF-001").update(open_question_ids=[]),
     lambda r: final_req(r, "RNF-001")["open_question_ids"] == ["Q-001"]),
    (ES, "slot terminado en punto", "fix", None, "FIX",
     lambda c: req(c, "RF-001")["slots"].update(response="permitir al empleado reservar una sala."),
     lambda r: r["texts"]["RF-001"].endswith("reservar una sala.")),
    (ES, "slots.system distinto del nombre del sistema", "fix", None, "FIX",
     lambda c: req(c, "RF-002")["slots"].update(system="la aplicación"),
     lambda r: final_req(r, "RF-002")["slots"]["system"] == "el sistema ReservaSalas"),
    (ES, "cita en el fragmento equivocado: se corrige el id", "fix", None, "FIX",
     lambda c: req(c, "RF-002")["source_trace"][0].update(fragment="DOC-01#F012"),
     lambda r: final_req(r, "RF-002")["source_trace"][0]["fragment"] == "DOC-01#F003"),
    (ES, "cita repetida", "fix", None, "FIX",
     lambda c: req(c, "RF-001")["source_trace"].append(dict(req(c, "RF-001")["source_trace"][0])),
     lambda r: len(final_req(r, "RF-001")["source_trace"]) == 3),
    (ES, "etiqueta de criterio en minúsculas y con dos puntos", "fix", None, "FIX",
     lambda c: req(c, "RF-001")["acceptance_criteria"].__setitem__(
         0, "data: El empleado indica la fecha, la hora de inicio, la hora de fin y el número de asistentes."),
     lambda r: final_req(r, "RF-001")["acceptance_criteria"][0].startswith("[DATA] El empleado")),
    (ES, "referencia a una pregunta inexistente", "fix", None, "FIX",
     lambda c: req(c, "RF-001").update(open_question_ids=["Q-099"]),
     lambda r: final_req(r, "RF-001")["open_question_ids"] == []),
    (ES, "condition que empieza por su palabra clave (R-h)", "fix", None, "FIX",
     lambda c: req(c, "RF-005")["slots"].update(condition="si la reserva dura más de 4 horas y tiene más de 20 asistentes"),
     lambda r: r["texts"]["RF-005"].startswith("SI la reserva dura")),
    (ES, "states y trigger que empiezan por su palabra clave (R-h)", "fix", None, "FIX",
     lambda c: req(c, "RF-004")["slots"].update(states=["Mientras el modo festivo esté activado"],
                                                 trigger="cuando un empleado intente reservar una sala"),
     lambda r: final_req(r, "RF-004")["slots"]["states"] == ["el modo festivo esté activado"]
     and final_req(r, "RF-004")["slots"]["trigger"] == "un empleado intente reservar una sala"),
    (ES, "fragmento descartado entero y citado por un requisito: se quita el descarte", "fix", None, "FIX",
     lambda c: req(c, "RF-002")["source_trace"].append(
         {"fragment": "DOC-01#F001", "quote": "reducir los conflictos de uso"}),
     lambda r: [d["fragment"] for d in r["catalog"]["discarded_statements"]] == ["DOC-01#F014"]),
    (ES, "sección descartada entera con fragmentos citados: descartes de los no citados", "fix", None, "FIX",
     lambda c: (c["discarded_statements"].pop(), c["discarded_sections"].append({"section": "§3", "reason": "project_note"})),
     lambda r: r["catalog"]["discarded_sections"] == []
     and r["catalog"]["discarded_statements"][-1] == {"fragment": "DOC-01#F014", "quote": None,
                                                      "reason": "project_note", "note": None}),
    (ES, "funcionalidad con un fragmento fuera del lote", "fix", None, "FIX",
     lambda c: cap(c, "CAP-001")["fragments"].append("DOC-01#F099"),
     lambda r: "DOC-01#F099" not in r["catalog"]["capabilities"][0]["fragments"]),

    # --- Aceptación sin cambios ---
    (ES, "cita con comillas de código y mayúsculas distintas", "accept", None, None,
     lambda c: req(c, "RF-002")["source_trace"][0].update(
         quote="al confirmar la `reserva`, el sistema envía un correo de confirmación")),

    # --- Reparación ---
    (ES, "funcionalidad del inventario sin requisito (C20)", "repair", None, "C20",
     lambda c: req(c, "RF-006").update(capability_ids=[])),
    (ES, "funcionalidad cubierta solo por un RNF (C20)", "repair", None, "C20",
     lambda c: (req(c, "RNF-001").update(capability_ids=["CAP-005"]), req(c, "RF-006").update(capability_ids=[]))),
    (ES, "más del 20 % de los requisitos rechazados", "repair", None, "C15",
     lambda c: (req(c, "RF-003")["slots"].update(response="no permitir a los empleados añadir catering"),
                req(c, "RF-004")["slots"].update(response="mostrar al empleado un aviso si procede"))),
    (ES, "un solo rechazo (11 %) no pide reparación", "reject", "RF-004", "C5",
     lambda c: req(c, "RF-004")["slots"].update(response="mostrar al empleado un aviso si procede"),
     lambda r: not r["needs_repair"]),
]


def run_case(example, mutate):
    c = copy.deepcopy(BASES[example])
    result = mutate(c)
    raw = result if isinstance(result, str) else json.dumps(c, ensure_ascii=False, indent=2)
    return raw, check_text(raw, EXAMPLES[example], FRAGMENTS[example], SCHEMA)


def run_inventory_case(example, mutate):
    c = copy.deepcopy(INV_BASES[example])
    result = mutate(c)
    raw = result if isinstance(result, str) else json.dumps(c, ensure_ascii=False, indent=2)
    return raw, validar_inventario.check_text(raw, INV_FRAGMENTS[example], INV_SCHEMA)


def code_of(message):
    """'RF-001 C14 …' -> 'C14'; 'LOTE C10 …' -> 'C10'; 'LOTE JSON no válido' -> 'JSON'."""
    parts = message.split()
    return parts[1] if len(parts) > 1 else parts[0]


def lot_codes(report):
    return {code_of(e) for e in report["errors"]}


def warning_codes(report):
    return {code_of(w) for w in report["warnings"]}


def repair_codes(report):
    return {code_of(x) for x in report["repair_reasons"]}


def rejected_codes(report, element_id):
    return {e.split()[0] for x in report["rejected"] if x["id"] == element_id for e in x["errors"]}


def outcome_ok(kind, target, code, report):
    if kind == "reject":
        return report["valid"] and code in rejected_codes(report, target)
    if kind == "lot":
        return not report["valid"] and report["needs_repair"] and code in lot_codes(report)
    if kind in ("warning", "fix"):
        return (report["valid"] and not report["needs_repair"] and not report["rejected"]
                and code in warning_codes(report))
    if kind == "accept":
        return report["valid"] and not report["needs_repair"] and not report["rejected"] and not report["warnings"]
    if kind == "repair":
        return report["valid"] and report["needs_repair"] and code in repair_codes(report)
    raise ValueError(kind)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exportar")
    a = ap.parse_args()

    failures, total = 0, 0
    for example, sistema in EXAMPLES.items():
        total += 1
        raw = (HERE / example / BASE_FILE).read_text(encoding="utf-8")
        report = check_text(raw, sistema, FRAGMENTS[example], SCHEMA)
        enriched = json.loads((HERE / example / ENRICHED_FILE).read_text(encoding="utf-8"))
        ok = (report["valid"] and not report["needs_repair"] and not report["rejected"] and not report["warnings"]
              and enrich(report["catalog"], report["texts"]) == enriched)
        failures += not ok
        print(f"{'OK    ' if ok else 'FALLO '} | base    | {example}: válido, sin avisos y con el texto de "
              f"{ENRICHED_FILE}" + ("" if ok else f"  -> {report['errors'] + report['warnings']}"))
        # El catálogo aceptado ya está normalizado: validarlo otra vez no cambia nada.
        total += 1
        again = check_text(json.dumps(report["catalog"], ensure_ascii=False), sistema, FRAGMENTS[example], SCHEMA)
        ok = again["catalog"] == report["catalog"] and not again["warnings"]
        failures += not ok
        print(f"{'OK    ' if ok else 'FALLO '} | base    | {example}: normalizar dos veces da lo mismo")

    for example in EXAMPLES:
        total += 1
        report = validar_inventario.check_text((HERE / example / INV_FILE).read_text(encoding="utf-8"),
                                               INV_FRAGMENTS[example], INV_SCHEMA)
        ok = report["valid"] and not report["needs_repair"] and not report["rejected"] and not report["warnings"]
        failures += not ok
        print(f"{'OK    ' if ok else 'FALLO '} | base    | {example}: inventario válido y sin avisos"
              + ("" if ok else f"  -> {report['errors'] + report['warnings']}"))
        total += 1
        again = validar_inventario.check(report["inventory"], INV_FRAGMENTS[example], INV_SCHEMA)
        ok = again["inventory"] == report["inventory"] and not again["warnings"]
        failures += not ok
        print(f"{'OK    ' if ok else 'FALLO '} | base    | {example}: normalizar el inventario dos veces da lo mismo")

    root = Path(a.exportar) if a.exportar else None
    suites = [("catalog-mutations", CASES, run_case,
               lambda example: {"sistema": EXAMPLES[example]}),
              ("inventory-mutations", INVENTORY_CASES, run_inventory_case,
               lambda example: {"fragments": INV_FRAGMENTS_FILE[example]})]
    for folder, cases, run, fields in suites:
        print(f"\n--- {folder}")
        out_dir = root / folder if root else None
        if out_dir:
            out_dir.mkdir(parents=True, exist_ok=True)
        manifest = []
        for i, (example, name, kind, target, code, mutate, *extra) in enumerate(cases, 1):
            total += 1
            raw, report = run(example, mutate)
            ok = outcome_ok(kind, target, code, report) and all(check(report) for check in extra)
            failures += not ok
            label = f"{kind}:{code}" if code else kind
            print(f"{'OK    ' if ok else 'FALLO '} | {label:<16} | {name}" + ("" if ok else
                  f"\n         lote {sorted(lot_codes(report))} · rechazos {[(x['id'], x['errors']) for x in report['rejected']]}"
                  f"\n         avisos {report['warnings']} · reparación {report['repair_reasons']}"))
            if out_dir:
                fname = f"case-{i:02d}.json"
                (out_dir / fname).write_text(raw if raw.endswith("\n") else raw + "\n", encoding="utf-8",
                                             newline="\n")
                manifest.append({"file": fname, "name": name, "example": example, **fields(example),
                                 "kind": kind, "target": target, "expected_code": code})
        if out_dir:
            (out_dir / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
                                                   encoding="utf-8", newline="\n")
    print(f"\n{total - failures}/{total} correctos")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
