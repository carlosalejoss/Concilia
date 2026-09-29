# Mutaciones del validador de catálogos

Fixtures para comprobar que el validador en C# (`CatalogValidator`) da los mismos resultados que el de referencia en Python (`prompts/generacion_requisitos/validar_catalogo.py`).

| Fichero | Origen |
|---|---|
| `mut-XX.json`, `warn-XX.json`, `manifest.json` | `python test_validador.py --exportar tests/fixtures/catalog-mutations` (desde `prompts/generacion_requisitos/`). Cada caso es un catálogo de ejemplo con un único error o aviso. |
| `expected-codes.json` | `python tests/fixtures/catalog-mutations/export_codes.py prompts/generacion_requisitos tests/fixtures/catalog-mutations`. Conjunto completo de códigos de error y de aviso que da la referencia para cada fichero, para comprobar la paridad exacta y no solo el código esperado. |

Los dos scripts necesitan `pip install "jsonschema>=4.18"`. Hay que regenerarlos si cambian el validador, el esquema o los ejemplos.

Los ficheros se guardan con saltos de línea `\n` (Python en Windows escribe `\r\n`; se normalizan tras exportar).
