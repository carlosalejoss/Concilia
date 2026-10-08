"""Batería de mutaciones de validar_v0.py y exportación para el port a C# (PhaseValidator).

Cada caso parte de un fichero de ejemplo/ (sin cambios en los casos base), introduce un único cambio y
comprueba qué devuelve el validador:

- ok:      sin errores ni avisos.
- error:   algún error con ese código (provoca la reparación).
- warning: sin errores y con algún aviso con ese código.

El contexto es siempre el del ejemplo: los fragmentos de ReservaSalas, las respuestas de respuestas.json,
el catálogo del árbitro (para el backlog) y las dos propuestas de la fase (para el árbitro).

Uso:
    python test_v0.py                  # ejecuta la batería; código 1 si algún caso falla
    python test_v0.py --exportar DIR   # escribe cada caso en DIR (case-NN.json, manifest.json y
                                       # expected-outcomes.json) para los tests de C#

En expected-outcomes.json, las líneas de error y de aviso van tal cual, salvo las de esquema: los mensajes
de jsonschema y de la librería de C# no coinciden, así que todas se resumen en una sola línea "SCHEMA".
"""
import argparse
import copy
import json
import sys
from pathlib import Path

AQUI = Path(__file__).parent
sys.path.insert(0, str(AQUI))
from validar_v0 import cargar, validar_backlog, validar_catalogo, validar_preguntas  # noqa: E402

EJ = AQUI / "ejemplo"
FRAGMENTOS = {f["id"] for f in cargar(AQUI.parent / "generacion_requisitos" / "ejemplo" / "fragmentos.json")["fragments"]}
RESPONDIDAS = {p["id"] for p in cargar(EJ / "respuestas.json")["preguntas"] if p["estado"] == "respondida"}
CATALOGO = cargar(EJ / "catalogo_arbitro.json")


def par(fase):
    return {1: cargar(EJ / f"{fase}_propuesta_1.json"), 2: cargar(EJ / f"{fase}_propuesta_2.json")}


def validar(fase, arbitro, datos):
    propuestas = par(fase) if arbitro else None
    if fase == "preguntas":
        return validar_preguntas(datos, FRAGMENTOS, propuestas)
    if fase == "catalogo":
        return validar_catalogo(datos, FRAGMENTOS, RESPONDIDAS, propuestas)
    return validar_backlog(datos, CATALOGO, propuestas)


def por_id(lista, id_):
    return next(x for x in lista if x["id"] == id_)


def req(d, id_):
    return por_id(d["requisitos"], id_)


def hu(d, id_):
    return por_id([h for e in d["epicas"] for h in e["historias"]], id_)


def nada(d):
    return None


# (fichero base de ejemplo/, nombre, nivel, código esperado o None, mutación)
CASOS = [
    # --- Los ejemplos, sin cambios ---
    ("preguntas_propuesta_1", "ejemplo tal cual", "ok", None, nada),
    ("preguntas_propuesta_2", "ejemplo tal cual", "ok", None, nada),
    ("preguntas_arbitro", "ejemplo tal cual", "ok", None, nada),
    ("catalogo_propuesta_1", "ejemplo tal cual", "ok", None, nada),
    ("catalogo_propuesta_2", "ejemplo tal cual", "ok", None, nada),
    ("catalogo_arbitro", "ejemplo tal cual", "ok", None, nada),
    ("backlog_propuesta_1", "ejemplo tal cual: RF-009 sin historia", "warning", "COB", nada),
    ("backlog_propuesta_2", "ejemplo tal cual", "ok", None, nada),
    ("backlog_arbitro", "ejemplo tal cual", "ok", None, nada),

    # --- Preguntas ---
    ("preguntas_propuesta_1", "falta el motivo", "error", "SCHEMA",
     lambda d: d["preguntas"][0].pop("motivo")),
    ("preguntas_propuesta_1", "id repetido", "error", "ID-DUP",
     lambda d: d["preguntas"][1].update(id="P-001")),
    ("preguntas_propuesta_1", "ids no consecutivos", "error", "ID-SEQ",
     lambda d: d["preguntas"][-1].update(id="P-099")),
    ("preguntas_propuesta_1", "fragmento que no existe", "error", "REF-FRAG",
     lambda d: d["preguntas"][0]["fuentes"].append("DOC-01#F099")),
    ("preguntas_arbitro", "árbitro: id que no existe en la propuesta", "error", "ARB-DE",
     lambda d: d["preguntas"][0]["de"]["propuesta_1"].append("P-099")),
    ("preguntas_arbitro", "árbitro: `de` sin ningún id", "error", "ARB-DE",
     lambda d: d["preguntas"][0].update(de={"propuesta_1": [], "propuesta_2": []})),
    ("preguntas_arbitro", "árbitro: descarte de un id que no existe", "error", "ARB-DE",
     lambda d: d["descartadas"][0].update(id="P-099")),
    ("preguntas_arbitro", "árbitro: una pregunta de una propuesta se pierde", "error", "ARB-FALTA",
     lambda d: d["descartadas"].pop(0)),

    # --- Catálogo ---
    ("catalogo_propuesta_1", "tipo desconocido", "error", "SCHEMA",
     lambda d: d["requisitos"][0].update(tipo="RX")),
    ("catalogo_propuesta_1", "el prefijo no coincide con el tipo", "error", "TIPO",
     lambda d: d["requisitos"][0].update(tipo="RI")),
    ("catalogo_propuesta_1", "EARS con should", "error", "EARS",
     lambda d: d["requisitos"][0].update(obligatoriedad="should")),
    ("catalogo_propuesta_1", "requisito sin fuentes ni preguntas", "error", "SIN-ORIGEN",
     lambda d: d["requisitos"][0].update(fuentes=[], preguntas=[])),
    ("catalogo_propuesta_1", "fragmento que no existe", "error", "REF-FRAG",
     lambda d: d["requisitos"][0]["fuentes"].append("DOC-01#F099")),
    ("catalogo_propuesta_1", "pregunta descartada", "error", "REF-PREG",
     lambda d: d["requisitos"][0]["preguntas"].append("P-005")),
    ("catalogo_propuesta_1", "pregunta que no existe", "error", "REF-PREG",
     lambda d: d["requisitos"][0]["preguntas"].append("P-099")),
    ("catalogo_propuesta_1", "id repetido", "error", "ID-DUP",
     lambda d: d["requisitos"][1].update(id=d["requisitos"][0]["id"])),
    ("catalogo_propuesta_1", "ids de un tipo no consecutivos", "error", "ID-SEQ",
     lambda d: next(r for r in d["requisitos"] if r["tipo"] == "RT").update(id="RT-002")),
    ("catalogo_arbitro", "árbitro: id que no existe en la propuesta", "error", "ARB-DE",
     lambda d: req(d, "RF-001")["de"]["propuesta_2"].append("RF-099")),
    ("catalogo_arbitro", "árbitro: un requisito de una propuesta se pierde", "error", "ARB-FALTA",
     lambda d: d["descartados"].clear()),
    ("catalogo_arbitro", "árbitro: falta discrepancia", "error", "SCHEMA",
     lambda d: req(d, "RF-001").pop("discrepancia")),

    # --- Backlog ---
    ("backlog_propuesta_2", "historia sin criterios", "error", "SCHEMA",
     lambda d: d["epicas"][0]["historias"][0].update(criterios=[])),
    ("backlog_propuesta_2", "requisito que no existe en el catálogo", "error", "REF-REQ",
     lambda d: d["epicas"][0]["historias"][0]["requisitos"].append("RF-099")),
    ("backlog_propuesta_2", "historia repetida", "error", "ID-DUP",
     lambda d: d["epicas"][0]["historias"][1].update(id=d["epicas"][0]["historias"][0]["id"])),
    ("backlog_propuesta_2", "épicas no consecutivas", "error", "ID-SEQ",
     lambda d: d["epicas"][-1].update(id="EP-09")),
    ("backlog_propuesta_2", "criterios no consecutivos", "error", "ID-SEQ",
     lambda d: d["epicas"][-1]["historias"][-1]["criterios"][-1].update(id="CA-099")),
    ("backlog_propuesta_2", "tareas no consecutivas", "error", "ID-SEQ",
     lambda d: d["epicas"][-1]["historias"][-1]["tareas"].append({"id": "T-099", "titulo": "Tarea de prueba"})),
    ("backlog_arbitro", "requisito funcional sin historia", "warning", "COB",
     lambda d: hu(d, "HU-001")["requisitos"].remove("RF-004")),
    ("backlog_arbitro", "árbitro: id que no existe en la propuesta", "error", "ARB-DE",
     lambda d: d["epicas"][0]["historias"][0]["de"]["propuesta_1"].append("HU-099")),
    ("backlog_arbitro", "árbitro: una historia de una propuesta se pierde", "error", "ARB-FALTA",
     lambda d: d["descartadas"].clear()),
]


def resumir(lineas):
    """Las líneas tal cual, salvo las de esquema, que se resumen en una sola "SCHEMA"."""
    otras = [x for x in lineas if not x.startswith("SCHEMA ")]
    return (["SCHEMA"] if len(otras) < len(lineas) else []) + otras


def codigo(linea):
    """'RF-001 TIPO: …' -> 'TIPO'; 'SCHEMA …' -> 'SCHEMA'."""
    partes = linea.split()
    return "SCHEMA" if partes[0] == "SCHEMA" else partes[1].rstrip(":")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exportar")
    a = ap.parse_args()

    salida = Path(a.exportar) if a.exportar else None
    if salida:
        salida.mkdir(parents=True, exist_ok=True)
    manifiesto, esperados, fallos = [], {}, 0
    for i, (base, nombre, nivel, cod, mutar) in enumerate(CASOS, 1):
        fase, rol = base.split("_", 1)
        arbitro = rol == "arbitro"
        datos = copy.deepcopy(cargar(EJ / f"{base}.json"))
        datos.pop("$comment", None)
        mutar(datos)
        errores, avisos = validar(fase, arbitro, datos)
        codigos_e, codigos_a = {codigo(x) for x in errores}, {codigo(x) for x in avisos}
        ok = {"ok": not errores and not avisos,
              "error": cod in codigos_e,
              "warning": not errores and cod in codigos_a}[nivel]
        fallos += not ok
        print(f"{'OK    ' if ok else 'FALLO '} | {base:<22} | {nivel}:{cod or '-':<10} | {nombre}"
              + ("" if ok else f"\n         errores {errores}\n         avisos {avisos}"))
        if salida:
            fichero = f"case-{i:02d}.json"
            (salida / fichero).write_text(json.dumps(datos, ensure_ascii=False, indent=2) + "\n",
                                          encoding="utf-8", newline="\n")
            manifiesto.append({"file": fichero, "name": nombre, "base": base, "phase": fase, "arbiter": arbitro,
                               "kind": nivel, "code": cod})
            esperados[fichero] = {"errors": resumir(errores), "warnings": resumir(avisos)}
    if salida:
        for nombre, contenido in (("manifest.json", manifiesto), ("expected-outcomes.json", esperados)):
            (salida / nombre).write_text(json.dumps(contenido, ensure_ascii=False, indent=2) + "\n",
                                         encoding="utf-8", newline="\n")
    print(f"\n{len(CASOS) - fallos}/{len(CASOS)} correctos")
    sys.exit(1 if fallos else 0)


if __name__ == "__main__":
    main()
