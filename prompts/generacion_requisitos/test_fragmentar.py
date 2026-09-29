#!/usr/bin/env python3
"""
Tests del fragmentador de referencia v1.2.

Comprueba que los dos ejemplos producen exactamente su `fragmentos.json` y los casos límite que el port
a C# debe reproducir (títulos numerados, líneas partidas de PDF, viñetas, abreviaturas, ids de la fuente).

Uso:
    python test_fragmentar.py      # código 1 si algún caso falla
"""
import json
import sys
from pathlib import Path

HERE = Path(__file__).parent
sys.path.insert(0, str(HERE))
from fragmentar import fragment_document  # noqa: E402

# (nombre, texto del documento, [(texto, sección, source_ref)] esperados)
CASES = [
    ("título numerado como fragmento propio",
     "3.2 Gestión de reservas\nEl sistema deberá permitir reservar.\n",
     [("Gestión de reservas", "3.2", None), ("El sistema deberá permitir reservar.", "3.2", None)]),
    ("título numerado de un nivel ('2.')",
     "2. Functional requirements\n2.1 A member can reserve a book.\n",
     [("Functional requirements", "2.", None), ("A member can reserve a book.", "2.1", None)]),
    ("línea partida de PDF: no es título aunque sea corta y sin punto",
     "3.3 El sistema mostrará la lista de\nsalas disponibles.\n",
     [("El sistema mostrará la lista de salas disponibles.", "3.3", None)]),
    ("frase introductoria y viñetas sin su marcador",
     "El sistema deberá permitir al administrador:\n- crear salas,\n- eliminar salas.\n",
     [("El sistema deberá permitir al administrador:", "", None), ("crear salas,", "", None),
      ("eliminar salas.", "", None)]),
    ("título markdown",
     "## Seguridad\nEl sistema cifrará las contraseñas.\n",
     [("Seguridad", "Seguridad", None), ("El sistema cifrará las contraseñas.", "Seguridad", None)]),
    ("id de requisito de la fuente en source_ref (el texto no cambia)",
     "FR-12 The system shall log every failed login. R7: El usuario puede salir. ISO 9001 aplica.\n",
     [("FR-12 The system shall log every failed login.", "", "FR-12"),
      ("R7: El usuario puede salir.", "", "R7"), ("ISO 9001 aplica.", "", None)]),
    ("abreviaturas e iniciales no cortan la frase",
     "Se admiten tarjetas, p. ej. de EE. UU. y de la UE. Lo aprueba J. Pérez.\n",
     [("Se admiten tarjetas, p. ej. de EE. UU. y de la UE.", "", None), ("Lo aprueba J. Pérez.", "", None)]),
    ("'etc.' seguido de mayúscula cierra la frase",
     "Se exportan PDF, CSV, etc. El envío es diario.\n",
     [("Se exportan PDF, CSV, etc.", "", None), ("El envío es diario.", "", None)]),
    ("etiquetas neutralizadas",
     "El campo <nombre> es obligatorio.\n",
     [("El campo ＜nombre＞ es obligatorio.", "", None)]),
]


def main():
    failures, total = 0, 0
    for example in ("ejemplo", "ejemplo_en"):
        total += 1
        expected = json.loads((HERE / example / "fragmentos.json").read_text(encoding="utf-8"))["fragments"]
        got = fragment_document("DOC-01", (HERE / example / "fuentes" / "DOC-01.txt").read_text(encoding="utf-8"))
        ok = got == [f for f in expected if f["kind"] == "document"]
        failures += not ok
        print(f"{'OK    ' if ok else 'FALLO '} | {example}/fragmentos.json se reproduce exactamente")
    for name, text, expected in CASES:
        total += 1
        got = [(f["text"], f["section"], f["source_ref"]) for f in fragment_document("DOC-01", text)]
        ok = got == expected
        failures += not ok
        print(f"{'OK    ' if ok else 'FALLO '} | {name}" + ("" if ok else f"\n         esperado {expected}\n         obtenido {got}"))
    print(f"\n{total - failures}/{total} correctos")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
