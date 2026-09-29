# Hito 5: primer catálogo real

Cubre los pasos 25 a 28 de `docs/plan-de-construccion.md` y aplica las decisiones D-21 a D-25 de la bitácora. Léelo junto con `CLAUDE.md`. Esta especificación completa el plan; si choca con él o con `CLAUDE.md`, dilo antes de aplicarla. Los valores marcados como "por defecto" son configurables.

## Objetivo

Se sube un documento, A y B generan cada uno un catálogo de requisitos válido (con reparación si hace falta) y la web los muestra en dos columnas, con el texto de cada requisito y las preguntas abiertas.

## Qué hay ya en `prompts/generacion_requisitos/`

| Fichero | Qué es |
|---|---|
| `prompt_requisitos_v1.2.md` | Prompt que reciben A y B, idéntico byte a byte. Las versiones anteriores están en el historial de git. |
| `anexo_reparacion_v1.2.md` | Texto que se añade al final del prompt en un reintento. |
| `requirements.schema.json` | Esquema JSON 1.2 del catálogo (draft 2020-12). |
| `fragmentar.py` | Fragmentador de referencia. |
| `test_fragmentar.py` | 11 casos: los dos ejemplos y los casos límite del fragmentador. |
| `validar_catalogo.py` | Validador y render de referencia. |
| `test_validador.py` | 46 casos sobre los dos ejemplos: bases válidas, 42 mutaciones con su código de error y 2 con su código de aviso. Con `--exportar DIR` los escribe como fixtures. |
| `ejemplo/` | ReservaSalas (`es`): `fuentes/DOC-01.txt`, `fragmentos.json`, `catalogo_ejemplo.json` (válido) y `catalogo_ejemplo.enriquecido.json` (con `text`). |
| `ejemplo_en/` | LibraryLoan (`en`), con la misma estructura. |

Los scripts de Python son la **referencia**: el código C# debe producir la misma salida para las mismas entradas. Python no entra en la imagen de la API; solo en CI (paso 26).

## Reglas

Las de `CLAUDE.md`, en particular: ningún test llama a la pasarela y Claude Code no ejecuta nada contra ella. Usa solo el ejemplo ReservaSalas y datos públicos.

## Paso 25a: fragmentación

Port de `fragmentar.py` a `Core`, con `Fragment { Id, Source, Kind, Section, Text, SourceRef }`.

- Documento: cada archivo del proyecto con texto extraído, con id `DOC-01`, `DOC-02`… por orden de subida. Fragmentos `DOC-01#F001`…; dudas `DUDAS#<id>` (vacío hasta la fase de dudas).
- Reglas (las mismas que el script): etiqueta de sección al principio de línea (`§2.1`, `2.1`, `2.1.`, `3.`), título markdown como fragmento único, título numerado como fragmento único (resto de la línea de 10 palabras o menos, sin puntuación final y seguido de línea vacía o de una línea que empieza bloque), id de requisito de la fuente al principio de un párrafo copiado en `SourceRef` sin cambiar el texto, viñeta como párrafo propio sin su marcador, líneas de continuación unidas, corte de frase por signo de cierre + espacio + mayúscula/dígito, sin cortar tras las abreviaturas de la lista ni tras una inicial, `<` y `>` sustituidos por `＜` y `＞`, NFC y espacios colapsados.
- Tests: los `DOC-01.txt` de `ejemplo/` y `ejemplo_en/` producen exactamente sus `fragmentos.json`, y los casos límite de `test_fragmentar.py` dan el mismo resultado: título markdown y numerado, línea partida de PDF que no es título, viñetas, `SourceRef`, abreviaturas ("p. ej.", "EE. UU."), inicial ("J. Pérez"), "etc." al final de frase, `<etiqueta>`.

## Paso 25b: lotes

El tope de salida de Claude Code (32k tokens por respuesta) limita el tamaño del catálogo de una ejecución. Cada documento se divide en lotes antes de lanzar A y B.

- Salida estimada de un lote = caracteres de sus fragmentos × `Catalog:OutputRatio` / 3,5. Por defecto `OutputRatio` = 15 (el ejemplo mide 10,7 en JSON compacto y 14,4 con sangría; es un documento denso, así que es una cota prudente).
- Un lote no supera `Catalog:MaxOutputTokensPerLot`, por defecto 20000: deja margen dentro de los 32k para el razonamiento y el mensaje final. Con los valores por defecto salen unos 4.600 caracteres de fuente por lote.
- Se corta por secciones completas cuando cabe; si una sección no cabe, por fragmentos. Nunca se parte un fragmento.
- Cada ejecución guarda la razón real (caracteres del catálogo / caracteres de fragmentos) para recalibrar `OutputRatio` con datos.
- Tests: un documento corto da un lote; uno largo da varios sin fragmentos repetidos ni perdidos.

## Paso 25c: montaje del prompt

- El proyecto necesita tres campos nuevos, con migración: nombre del sistema (obligatorio, con artículo: "el sistema X"), idioma (`es` por defecto) y glosario (texto, opcional).
- Placeholders: `{{NOMBRE_SISTEMA}}`, `{{IDIOMA}}` (aparece dos veces), `{{LOTE_NUMERO}}`, `{{LOTE_TOTAL}}`, `{{GLOSARIO}}`, `{{DUDAS_RESUELTAS}}` (vacío por ahora) y `{{DOCUMENTACION}}` (el bloque `<documento>` con los `<f>` del lote, con el formato de `fragmentar.py --bloque-doc`).
- **Sustitución en una sola pasada** (una expresión regular sobre el texto original): el texto de un documento puede contener `{{…}}` y no debe sustituirse.
- En la carpeta de la ejecución: `input/fragmentos.json` (lo usa el validador; el modelo no lo lee).
- `RunRequest`: `ExpectedOutputFiles` = `output/catalogo.json`; `AllowedTools` = `Write` (el prompt no necesita leer nada). Si el modelo falla sin `Read`, vuelve a `Read,Write` y anótalo.
- Guarda en cada ejecución la versión del prompt (`req-1.2`), el SHA-256 del prompt montado, el modelo y la versión de Claude Code.

## Paso 26: validador en C#

- Port de `validar_catalogo.py`: esquema, comprobaciones con los **mismos códigos** (C2 a C18, N2, N3, SCHEMA, VERDICT; los errores de `domain_terms` llevan el prefijo `DOMAIN[n]`), avisos, render del texto y métricas.
- Librería de JSON Schema con soporte de draft 2020-12 e `if`/`then`/`else`. Revisa la licencia antes de añadirla, como en D-19.
- Tests:
  - Los `catalogo_ejemplo.json` de los dos ejemplos son válidos, sin avisos, y el render coincide con su `catalogo_ejemplo.enriquecido.json`.
  - `python test_validador.py --exportar tests/fixtures/catalog-mutations/` genera 44 fixtures y un `manifest.json`. Cada entrada indica el ejemplo de origen (para sus fragmentos), el nombre del sistema, el código esperado y si es un error (catálogo no válido) o un aviso (catálogo válido con ese aviso).
- CI: añade un paso que ejecute `python test_fragmentar.py` y `python test_validador.py` (necesita `pip install "jsonschema>=4.18"`, que trae el draft 2020-12).
- El informe del validador (errores, avisos y métricas) se guarda por ejecución.

## Paso 27: reintento como reparación

- Se reintenta si falta `output/catalogo.json`, no es JSON o el validador devuelve errores. Los avisos no provocan reintento.
- El reintento es una ejecución nueva, enlazada a la anterior, con el prompt original + `anexo_reparacion_v1.2.md` con `{{INTENTO}}`, `{{CATALOGO_PREVIO}}` (el fichero tal cual) y `{{ERRORES}}` (un error por línea).
- Máximo dos reintentos por lote y modelo, contando también los del veredicto del hito 3.
- Si sigue sin ser válido, el lote de ese modelo queda `Failed` y se registra el informe. El árbitro recibirá solo salidas válidas.

## Paso 28: unión de lotes y web

- Unión por modelo: se concatenan los lotes en orden, se renumeran `REQ-` y `Q-` de forma consecutiva y se reescriben `open_question_ids`, `related_requirements` y `derived_from`. No se deduplica entre lotes: lo hará el árbitro. Tests con dos lotes sintéticos.
- Web, en las columnas A y B del hito 4: tabla de requisitos (id, notación y patrón, obligatoriedad, texto renderizado, fragmento y cita al pasar el ratón) y lista de preguntas abiertas. Encima, las métricas del validador. Sin pulido visual.

## Definición de hecho

- Con `FakeProvider`: `FakeClaude` puede dejar un fichero de salida (opción para copiar una fixture a `output/`) y el flujo completo funciona en el navegador con `docker compose up`, incluido un reintento con una fixture mutada.
- Con `LiteLlm` (lo prueba el autor): se sube `DOC-01.txt` de ReservaSalas y A y B terminan con catálogos válidos, visibles en la web. Las métricas de ambos se anotan en la bitácora.

## Copiar a `CLAUDE.md` (decisiones vigentes)

- Catálogo: el modelo escribe `output/catalogo.json` con Write y termina con `VEREDICTO:`; el texto de cada requisito lo genera la plataforma a partir de los slots (D-23).
- La documentación se divide en fragmentos con id estable (`DOC-01#F001`) y en lotes que respetan el tope de salida (D-24).
- Los scripts de `prompts/generacion_requisitos/` son la referencia del fragmentador y del validador en C#.

## Pendientes del autor

- Probar si `CLAUDE_CODE_MAX_OUTPUT_TOKENS` (variable de entorno de Claude Code) eleva el tope de 32k con los modelos de la pasarela. Si funciona, se sube `MaxOutputTokensPerLot` y se anota como decisión.
- Elegir 1 o 2 documentos públicos de PURE para la primera medición.
