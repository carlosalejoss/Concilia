"""Comprobaciones de la plataforma para las salidas de la v0 (referencia en Python).

Es la referencia del validador en C# del paso 42. Cada función devuelve dos
listas de líneas: errores (provocan la reparación, y cada línea va al anexo)
y avisos (se muestran, pero no reparan). Cada línea empieza por el elemento
afectado y un código:

  SCHEMA     el JSON no cumple su esquema de esquemas/
  ID-DUP     id repetido
  ID-SEQ     ids no consecutivos (P-001, P-002…; por tipo en el catálogo)
  TIPO       el prefijo del id no coincide con el tipo
  EARS       un requisito EARS con obligatoriedad should o may
  SIN-ORIGEN requisito sin fragmentos ni preguntas
  REF-FRAG   fragmento que no existe en la documentación
  REF-PREG   pregunta que no existe o no está respondida
  REF-REQ    requisito que no existe en el catálogo
  ARB-DE     el árbitro cita un id que no existe en esa propuesta, o un `de` sin ningún id
  ARB-FALTA  un elemento de una propuesta no acaba ni en `de` ni en los descartes
  FALTA      una pregunta pendiente sin su sugerencia de respuesta
  COB        (aviso) requisito RF o RI sin ninguna historia

Uso: python validar_v0.py   (valida los ejemplos de ejemplo/)
"""
import json
import re
import sys
from pathlib import Path

from jsonschema import Draft202012Validator

AQUI = Path(__file__).parent


def cargar(ruta):
    return json.loads(Path(ruta).read_text(encoding="utf-8"))


def errores_de_esquema(datos, nombre_esquema):
    esquema = cargar(AQUI / "esquemas" / nombre_esquema)
    validador = Draft202012Validator(esquema)
    return [f"SCHEMA {'/'.join(map(str, e.absolute_path)) or '(raíz)'}: {e.message}"
            for e in sorted(validador.iter_errors(datos), key=lambda e: list(e.absolute_path))]


def errores_de_ids(ids, prefijo, cifras):
    """Ids únicos y consecutivos desde 1: P-001, P-002… (cifras = ancho del número)."""
    errores = []
    vistos = set()
    for i in ids:
        if i in vistos:
            errores.append(f"{i} ID-DUP: id repetido.")
        vistos.add(i)
    esperados = [f"{prefijo}-{n:0{cifras}d}" for n in range(1, len(ids) + 1)]
    if ids != esperados and len(vistos) == len(ids):
        errores.append(f"{prefijo} ID-SEQ: los ids deben ir seguidos desde {esperados[0] if esperados else prefijo}; llegan {', '.join(ids)}.")
    return errores


def errores_de_fragmentos(elemento, fuentes, fragmentos):
    return [f"{elemento} REF-FRAG: el fragmento {f} no existe." for f in fuentes if f not in fragmentos]


def errores_de_arbitro(salida, lista, propuestas, campo_descartes):
    """Cada elemento de cada propuesta acaba en algún `de` o en los descartes,
    y todo id citado existe en su propuesta."""
    errores = []
    usados = {1: set(), 2: set()}
    for elemento in salida[lista]:
        if not elemento["de"]["propuesta_1"] and not elemento["de"]["propuesta_2"]:
            errores.append(f"{elemento['id']} ARB-DE: `de` no tiene ningún id; di de qué elementos de las propuestas sale.")
        for n in (1, 2):
            for i in elemento["de"][f"propuesta_{n}"]:
                if i not in propuestas[n]:
                    errores.append(f"{elemento['id']} ARB-DE: {i} no existe en la propuesta {n}.")
                usados[n].add(i)
    for d in salida[campo_descartes]:
        if d["id"] not in propuestas[d["propuesta"]]:
            errores.append(f"{campo_descartes.upper()} ARB-DE: {d['id']} no existe en la propuesta {d['propuesta']}.")
        usados[d["propuesta"]].add(d["id"])
    for n in (1, 2):
        for i in propuestas[n]:
            if i not in usados[n]:
                errores.append(f"{i} ARB-FALTA: el elemento {i} de la propuesta {n} no está en ningún `de` ni en `{campo_descartes}`.")
    return errores


# --- Fases --------------------------------------------------------------

def validar_preguntas(datos, fragmentos, propuestas=None):
    esquema = "arbitro_preguntas.schema.json" if propuestas else "preguntas.schema.json"
    errores = errores_de_esquema(datos, esquema)
    if errores:
        return errores, []
    preguntas = datos["preguntas"]
    errores += errores_de_ids([p["id"] for p in preguntas], "P", 3)
    for p in preguntas:
        errores += errores_de_fragmentos(p["id"], p["fuentes"], fragmentos)
    if propuestas:
        ids = {n: [p["id"] for p in propuestas[n]["preguntas"]] for n in (1, 2)}
        errores += errores_de_arbitro(datos, "preguntas", ids, "descartadas")
    return errores, []


TIPOS = ["RF", "RNF", "RA", "RT", "RI"]


def validar_catalogo(datos, fragmentos, respondidas, propuestas=None):
    esquema = "arbitro_catalogo.schema.json" if propuestas else "catalogo.schema.json"
    errores = errores_de_esquema(datos, esquema)
    if errores:
        return errores, []
    requisitos = datos["requisitos"]
    for r in requisitos:
        if r["id"].split("-")[0] != r["tipo"]:
            errores.append(f"{r['id']} TIPO: el prefijo no coincide con el tipo {r['tipo']}.")
        if r["notacion"] == "EARS" and r["obligatoriedad"] != "shall":
            errores.append(f"{r['id']} EARS: un requisito EARS siempre es shall; para {r['obligatoriedad']} usa MASTeR.")
        if not r["fuentes"] and not r["preguntas"]:
            errores.append(f"{r['id']} SIN-ORIGEN: cita al menos un fragmento o una pregunta respondida.")
        errores += errores_de_fragmentos(r["id"], r["fuentes"], fragmentos)
        for p in r["preguntas"]:
            if p not in respondidas:
                errores.append(f"{r['id']} REF-PREG: la pregunta {p} no existe o no está respondida.")
    for tipo in TIPOS:
        errores += errores_de_ids([r["id"] for r in requisitos if r["tipo"] == tipo], tipo, 3)
    if propuestas:
        ids = {n: [r["id"] for r in propuestas[n]["requisitos"]] for n in (1, 2)}
        errores += errores_de_arbitro(datos, "requisitos", ids, "descartados")
    return errores, []


def historias_de(backlog):
    return [h for e in backlog["epicas"] for h in e["historias"]]


def validar_backlog(datos, catalogo, propuestas=None):
    esquema = "arbitro_backlog.schema.json" if propuestas else "backlog.schema.json"
    errores = errores_de_esquema(datos, esquema)
    if errores:
        return errores, []
    historias = historias_de(datos)
    errores += errores_de_ids([e["id"] for e in datos["epicas"]], "EP", 2)
    errores += errores_de_ids([h["id"] for h in historias], "HU", 3)
    errores += errores_de_ids([c["id"] for h in historias for c in h["criterios"]], "CA", 3)
    errores += errores_de_ids([t["id"] for h in historias for t in h["tareas"]], "T", 3)
    existentes = {r["id"] for r in catalogo["requisitos"]}
    cubiertos = set()
    for h in historias:
        for r in h["requisitos"]:
            if r not in existentes:
                errores.append(f"{h['id']} REF-REQ: el requisito {r} no existe en el catálogo.")
            cubiertos.add(r)
    avisos = [f"{r['id']} COB: el requisito no está en ninguna historia."
              for r in catalogo["requisitos"] if r["tipo"] in ("RF", "RI") and r["id"] not in cubiertos]
    if propuestas:
        ids = {n: [h["id"] for h in historias_de(propuestas[n])] for n in (1, 2)}
        # El árbitro compara historias: se le pasan todas juntas, sin sus épicas.
        planas = {"historias": historias, "descartadas": datos["descartadas"]}
        errores += errores_de_arbitro(planas, "historias", ids, "descartadas")
    return errores, avisos


def validar_sugerencias(datos, fragmentos, pendientes):
    """Respuestas sugeridas por la IA: una por cada pregunta pendiente (`pendientes`, en orden), sin
    repetir ni sugerir para otras, con fragmentos que existen; si se basa en la documentación, cita al
    menos uno."""
    errores = errores_de_esquema(datos, "sugerencias.schema.json")
    if errores:
        return errores, []
    vistas = set()
    for s in datos["sugerencias"]:
        p = s["pregunta"]
        if p not in pendientes:
            errores.append(f"{p} REF-PREG: la pregunta {p} no está entre las pendientes.")
        if p in vistas:
            errores.append(f"{p} ID-DUP: hay más de una sugerencia para la pregunta.")
        vistas.add(p)
        if s["base"] == "documentacion" and not s["fuentes"]:
            errores.append(f"{p} SIN-ORIGEN: una sugerencia basada en la documentación cita al menos un fragmento.")
        errores += errores_de_fragmentos(p, s["fuentes"], fragmentos)
    for p in pendientes:
        if p not in vistas:
            errores.append(f"{p} FALTA: no hay sugerencia para la pregunta.")
    return errores, []


# --- Ejemplos -----------------------------------------------------------

def main():
    ej = AQUI / "ejemplo"
    fragmentos = {f["id"] for f in cargar(AQUI.parent / "generacion_requisitos" / "ejemplo" / "fragmentos.json")["fragments"]}
    respondidas = {p["id"] for p in cargar(ej / "respuestas.json")["preguntas"] if p["estado"] == "respondida"}
    catalogo = cargar(ej / "catalogo_arbitro.json")

    def par(nombre):
        return {1: cargar(ej / f"{nombre}_propuesta_1.json"), 2: cargar(ej / f"{nombre}_propuesta_2.json")}

    casos = [
        ("preguntas_propuesta_1", validar_preguntas(cargar(ej / "preguntas_propuesta_1.json"), fragmentos)),
        ("preguntas_propuesta_2", validar_preguntas(cargar(ej / "preguntas_propuesta_2.json"), fragmentos)),
        ("preguntas_arbitro", validar_preguntas(cargar(ej / "preguntas_arbitro.json"), fragmentos, par("preguntas"))),
        ("catalogo_propuesta_1", validar_catalogo(cargar(ej / "catalogo_propuesta_1.json"), fragmentos, respondidas)),
        ("catalogo_propuesta_2", validar_catalogo(cargar(ej / "catalogo_propuesta_2.json"), fragmentos, respondidas)),
        ("catalogo_arbitro", validar_catalogo(catalogo, fragmentos, respondidas, par("catalogo"))),
        ("backlog_propuesta_1", validar_backlog(cargar(ej / "backlog_propuesta_1.json"), catalogo)),
        ("backlog_propuesta_2", validar_backlog(cargar(ej / "backlog_propuesta_2.json"), catalogo)),
        ("backlog_arbitro", validar_backlog(cargar(ej / "backlog_arbitro.json"), catalogo, par("backlog"))),
        # Las sugerencias del ejemplo son para P-002, P-003 y P-004, como si siguieran pendientes.
        ("sugerencias", validar_sugerencias(cargar(ej / "sugerencias.json"), fragmentos, ["P-002", "P-003", "P-004"])),
    ]
    fallos = 0
    for nombre, (errores, avisos) in casos:
        print(f"{nombre}: {'OK' if not errores else f'{len(errores)} errores'}")
        for linea in errores:
            print(f"  ERROR {linea}")
        for linea in avisos:
            print(f"  AVISO {linea}")
        fallos += len(errores)
    sys.exit(1 if fallos else 0)


if __name__ == "__main__":
    main()
