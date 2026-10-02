# Mutaciones del validador de catálogos (v1.3)

Fixtures para comprobar que el validador en C# (`CatalogValidator`) da los mismos resultados que el de referencia en Python (`prompts/generacion_requisitos/validar_catalogo.py`).

| Fichero | Origen |
|---|---|
| `case-XX.json`, `manifest.json` | `python test_validador.py --exportar ../../tests/fixtures/catalog-mutations` (desde `prompts/generacion_requisitos/`). Cada caso es un catálogo de ejemplo v1.3 con un único cambio. El manifiesto dice el ejemplo de origen, el nombre del sistema, el nivel esperado (`reject`, `lot`, `warning`, `fix`, `accept`, `repair`), el elemento afectado y el código. |
| `expected-outcomes.json` | `python tests/fixtures/catalog-mutations/export_outcomes.py prompts/generacion_requisitos tests/fixtures/catalog-mutations` (desde la raíz). Resultado completo de la referencia para cada caso: si el lote es válido y si necesita reparación, los códigos de cada nivel, los elementos rechazados con sus códigos, el catálogo aceptado (normalizado y renumerado), los textos generados, la correspondencia de ids y las métricas. |

Los dos scripts necesitan `pip install "jsonschema>=4.18"`. Hay que regenerarlos si cambian el validador, el esquema o los ejemplos.

Los ficheros se guardan con saltos de línea `\n`. Un caso (`case-31.json`) no es JSON a propósito.

Las fixtures del catálogo v1.2 (`mut-XX.json`, `warn-XX.json`, `expected-codes.json`, `export_codes.py`) se retiraron en el paso 31; están en el historial de git (commit `82e2416`).
