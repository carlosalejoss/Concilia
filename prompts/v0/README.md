# Prompts v0

Prompts muy básicos de la iteración v0 (D-65, D-73): sirven para tener el flujo completo funcionando de punta a punta. Después se mejoran fase a fase. Los propuso Claude el 08-10-2026 y los revisa el autor (paso 41 del plan).

## Ficheros

| Fase | Prompt | Lee de `input/` (entero · si lo necesita) | El modelo escribe | Esquema | Marcadores |
|---|---|---|---|---|---|
| Preguntas, A y B | `preguntas.md` | `documentacion.txt` | `output/preguntas.json` | `esquemas/preguntas.schema.json` | `NOMBRE_SISTEMA`, `IDIOMA`, `GLOSARIO`, `PREGUNTAS_PREVIAS`, `LECTURA` |
| Preguntas, árbitro | `arbitro_preguntas.md` | `propuesta_1.json`, `propuesta_2.json`, `emparejamiento.txt` · `documentacion.txt` | `output/preguntas.json` | `esquemas/arbitro_preguntas.schema.json` | `NOMBRE_SISTEMA`, `IDIOMA`, `PREGUNTAS_PREVIAS`, `LECTURA` |
| Catálogo, A y B | `catalogo.md` | `documentacion.txt` | `output/catalogo.json` | `esquemas/catalogo.schema.json` | `NOMBRE_SISTEMA`, `IDIOMA`, `GLOSARIO`, `RESPUESTAS`, `LECTURA` |
| Catálogo, árbitro | `arbitro_catalogo.md` | `propuesta_1.json`, `propuesta_2.json`, `emparejamiento.txt` · `documentacion.txt` | `output/catalogo.json` | `esquemas/arbitro_catalogo.schema.json` | los del catálogo |
| Backlog, A y B | `backlog.md` | `catalogo.json` | `output/backlog.json` | `esquemas/backlog.schema.json` | `NOMBRE_SISTEMA`, `IDIOMA`, `GLOSARIO`, `RESPUESTAS`, `LECTURA` |
| Backlog, árbitro | `arbitro_backlog.md` | `catalogo.json`, `propuesta_1.json`, `propuesta_2.json`, `emparejamiento.txt` | `output/backlog.json` | `esquemas/arbitro_backlog.schema.json` | los del backlog |
| Sugerencias de respuesta (un modelo) | `sugerir_respuestas.md` | `documentacion.txt` | `output/sugerencias.json` | `esquemas/sugerencias.schema.json` | `NOMBRE_SISTEMA`, `IDIOMA`, `GLOSARIO`, `RESPUESTAS`, `PREGUNTAS`, `LECTURA` |
| Mockup, diseño | `mockup_diseno.md` | `backlog.json` | `output/prototipo/mapa.json` y `output/prototipo/app.html` (el armazón) | comprobaciones propias (abajo) | `NOMBRE_SISTEMA`, `IDIOMA`, `LECTURA`, `ESTILO` |
| Mockup, una pantalla (una ejecución por pantalla) | `mockup_pantalla.md` | `mapa.json`, `app.html`, `historias.json` (las de la pantalla) | `output/prototipo/pantallas/P01.html` | comprobaciones propias (abajo) | los del diseño, `PANTALLA`, `TITULO`, `HISTORIAS`, `FICHERO`, `NAVEGACION` |
| Mockup, corrección de una parte (solo si la plataforma encuentra problemas) | `mockup_correccion.md` | `original.html` (la parte), `mapa.json` y, si es una pantalla, `app.html` | la parte corregida, entera, en su fichero de `output/prototipo/` | comprobaciones propias (abajo) | los del diseño, `PARTE`, `FICHERO`, `PROBLEMAS` |
| Reparación, todas | `anexo_reparacion.md` | — | el mismo fichero | — | `INTENTO`, `FICHERO`, `SALIDA_PREVIA`, `ERRORES` |

Todos los prompts terminan con la línea `VEREDICTO:` que ya lee `VerdictParser`, y piden una sola llamada a Write. Desde la v0.1 (09-10-2026) el modelo tiene `Read` y `Write`: lee por partes los ficheros grandes de `input/` y escribe su salida.

## Lo que monta la plataforma

Los valores se insertan en una sola pasada, como en `PromptBuilder.Fill`: un documento que contenga `{{…}}` llega al modelo tal cual.

- `NOMBRE_SISTEMA`, `IDIOMA` y `GLOSARIO`: los ajustes del proyecto.
- `input/documentacion.txt` (`DocumentationFile`): **todos** los fragmentos, sin lotes, uno por línea: `[DOC-01#F001 §1] texto`. Una línea `=== DOC-01 · nombre ===` abre cada documento, y un fragmento de más de 1.500 caracteres sigue en líneas `[DOC-01#F001 sigue] …`, porque `Read` corta las líneas de más de 2.000.
- `PREGUNTAS_PREVIAS`: todas las preguntas de rondas anteriores, respondidas y descartadas: `<pregunta id="P-003" estado="respondida"><texto>…</texto><respuesta>…</respuesta></pregunta>`. Una descartada va sin `<respuesta>`. Vacío en la primera ronda.
- `RESPUESTAS`: solo las respondidas, `<pregunta id="P-003"><texto>…</texto><respuesta>…</respuesta></pregunta>`.
- `PREGUNTAS`: las preguntas pendientes para las que se piden sugerencias, `<pregunta id="P-007"><texto>…</texto><motivo>…</motivo></pregunta>`.
- `input/catalogo.json`: el catálogo **validado**, con los campos de `catalogo.schema.json` (sin `de`, `discrepancia` ni `explicacion`).
- `input/backlog.json`: el backlog **validado**, con los campos de `backlog.schema.json`.
- `input/propuesta_1.json` e `input/propuesta_2.json`: el JSON de A y de B, en orden aleatorio. El reparto se guarda en `plan.json` del trabajo, y la plataforma traduce los `de` del árbitro a A y B.
- `input/emparejamiento.txt` (desde la v0.10, solo el árbitro): el emparejamiento por significado de las dos propuestas que calcula la plataforma con embeddings, una línea por par (`1:RF-003 2:RF-001 mismo 0.94`, `… parecido 0.78`) o por elemento sin par (`1:RF-020 sin-par`). Se lee entero.
- Los ficheros JSON van indentados, un valor por línea, para que `Read` no corte ninguna línea larga.
- `LECTURA` (`ReadingPlan.Block`): qué ficheros de `input/` lee el modelo y cómo. De los que tiene que leer enteros da el número de líneas y los argumentos exactos de cada llamada a `Read` (`offset` y `limit`, desde la línea 1). Cada tramo tiene como mucho unos 40.000 caracteres o 1.000 líneas, para que quepa en una llamada. Los demás, solo para consultar.
- Anexo de reparación: `FICHERO` es la ruta de salida de la fase, `SALIDA_PREVIA` una nota que dice dónde está la salida anterior y cómo corregirla, y `ERRORES` una línea por error de las comprobaciones. Desde la v0.11 la salida anterior no va en el prompt: se queda en su fichero, con un elemento por línea, cada error dice en qué línea está su elemento y la reparación la corrige con Edit.

## Comprobaciones

Los esquemas solo describen la estructura. Las reglas entre campos están en `validar_v0.py`, la referencia en Python del validador en C# del paso 42. Cada error empieza por el elemento afectado y un código, y va tal cual al anexo de reparación:

| Código | Qué comprueba | Fases |
|---|---|---|
| `SCHEMA` | El JSON cumple su esquema | todas |
| `ID-DUP`, `ID-SEQ` | Ids únicos y consecutivos (por tipo en el catálogo) | todas |
| `REF-FRAG` | Los fragmentos citados existen | preguntas, catálogo |
| `TIPO`, `EARS`, `SIN-ORIGEN` | Prefijo igual al tipo; EARS solo con `shall`; al menos una fuente o una pregunta | catálogo |
| `REF-PREG` | Las preguntas citadas existen y están respondidas | catálogo |
| `REF-REQ` | Los requisitos citados existen en el catálogo validado | backlog |
| `ARB-DE`, `ARB-FALTA` | Cada `de` tiene algún id y todos existen; cada elemento de cada propuesta acaba en un `de` o en los descartes. Desde la v0.3 el árbitro escribe los ids con el número de su propuesta (`1:P-003`); la plataforma los quita antes de validar, y uno con el número de la otra propuesta es un `ARB-DE` | árbitros |
| `COB` (aviso) | Requisitos `RF` o `RI` sin historia | backlog |
| `LECTURA` | El modelo leyó todas las líneas de los ficheros que tiene que leer enteros, sumando los intentos del run (lo mira la plataforma en las llamadas a `Read`, no está en `validar_v0.py`) | todas, también el mockup |
| `REF-PREG`, `ID-DUP`, `FALTA` | Una sugerencia por cada pregunta pendiente, sin repetir ni sugerir para otras | sugerencias |
| `SIN-ORIGEN`, `REF-FRAG` | Una sugerencia `documentacion` cita al menos un fragmento, y todos existen | sugerencias |

Mockup (`MockupMap` y `MockupTask.Check` en C#, sin referencia en Python). Desde la v0.6 se comprueba cada paso:
- **Diseño:** el mapa es JSON (`JSON`), tiene pantallas (`MAPA`) con id `P01`, `P02`… sin repetir (`MAPA-ID`), título (`MAPA-TITULO`) y alguna historia, todas del backlog, y cada historia del backlog está en al menos una pantalla (`MAPA-HU`). Desde la v0.9: `inicio` es una pantalla del mapa (`MAPA-INICIO`, la única que puede no tener historias), cada pantalla tiene aplicación (`MAPA-APP`) y roles (`MAPA-ROL`), sus `desde` existen (`MAPA-DESDE`) y desde el inicio de sesión se llega a todas: el menú de cada aplicación y, desde cada pantalla alcanzada, las que la tienen en `desde` (`MAPA-ACCESO`). No hay un máximo de pantallas: por encima de 25 solo hay un aviso (`MAPA-PANTALLAS`). El armazón es un documento entero (`DOCTYPE`, `DATOS`) con la línea `<!-- PANTALLAS -->` (`BASE`).
- **Pantalla:** empieza por `<section data-pantalla="P01">` (`PANTALLA`), no es un documento entero (`FRAGMENTO`) y cada historia de la pantalla tiene un elemento con su `data-hu`, que admite varios ids (`HU`).
- **En todos:** sin recursos externos (`EXTERNO`) y solo las rutas de `input/estilo/` que ofrece la referencia de estilo (`ESTILO`).
- **Entre las partes (v0.8, `MockupCoherence`):** un `id` que ya usa otra parte (`ID-DUP`), un nombre global de JavaScript (`function`, `const`, `let`, `var`, `class`) que ya declara otra parte (`JS-GLOBAL`) y `mostrarPantalla` hacia una pantalla que no está en el mapa (`NAV`). Desde la v0.9, también un enlace del mapa (`desde`) que la pantalla de origen no tiene (`NAV-FALTA`). El armazón y, después, las pantallas en el orden del mapa se quedan el nombre; lo corrige la parte que llega después.
- **Página juntada:** la plataforma junta el armazón y las pantallas en el orden del mapa: falta un fichero (`FICHERO`), el armazón no tiene la línea (`BASE`) o una pantalla es un documento entero (`FRAGMENTO`). Cada pantalla del mapa tiene su `data-pantalla` en la página (`PANTALLA`). Después, sobre la página juntada: el fichero existe (`FICHERO`), ocupa como mucho 10 MB sin contar las imágenes y fuentes incrustadas (`TAMANO`; por encima de 2 MB, solo un aviso, `TAMANO-AVISO`), empieza por `<!doctype html>` (`DOCTYPE`), tiene `const DATOS` (`DATOS`), no carga recursos externos (`EXTERNO`: `src`, `href`, `url(` o `@import` con `http://`, `https://` o `//`) y cada historia del backlog validado tiene su `data-hu` (`HU`). Un `data-hu` de una historia que no está en el backlog es un aviso (`HU-EXTRA`). En la reparación, los ficheros del intento anterior siguen en `output/prototipo/` y el anexo dice dónde están.

`python validar_v0.py` valida los ejemplos. Necesita `jsonschema` 4 o superior (draft 2020-12).

## Lo que corrige la plataforma sin reparación

Antes de comprobar una salida, la plataforma corrige ella misma los errores de formato que tienen una única solución evidente, y deja un aviso con su código. Lo mismo se aplica a lo que se guarda. Ninguna de estas correcciones existe en `validar_v0.py`, que sigue siendo la referencia del validador:

- **Claves repetidas** cuyos valores son listas: se juntan (`CLAVE-DUP`, 16-10-2026).
- **La marca `sigue`** en un fragmento citado (`DOC-17#F003 sigue`): se quita (15-10-2026).
- **Desde el paso 89** (19-10-2026):
  - **`REF-LINEA`:** un fragmento citado que no existe y cuyo número es una línea de `input/documentacion.txt` con un fragmento del mismo documento se cambia por ese fragmento. Si el documento no coincide, sigue siendo el error `REF-FRAG`, que dice qué fragmento hay en esa línea.
  - **`TIPO`** (catálogo): si el prefijo de algún id no coincide con su tipo, se toma el tipo como bueno y se renumera el catálogo por tipo.
  - **`ARB-FALTA`** (árbitro): un elemento de una propuesta que no está en ningún `de` ni en los descartes se añade al elemento donde está su par «mismo» de `input/emparejamiento.txt`. Sin ese par, o con pares en varios elementos, sigue siendo un error.
  - **`FRAGMENTO`** (pantalla del mockup): una pantalla escrita como documento entero se queda con el contenido de su `<body>`, y los `<style>` y `<script>` de su `<head>` pasan dentro de la `<section>` raíz.

## Ejemplo

`ejemplo/` usa ReservaSalas: `generacion_requisitos/ejemplo/fuentes/DOC-01.txt` y sus 14 fragmentos de `fragmentos.json`. Cada fase tiene dos propuestas y la salida del árbitro, con diferencias pensadas para ver cómo se une:

- **Preguntas:** las dos piden la cifra de «rápida» (se unen). Cada una trae preguntas que la otra no tiene (se mantienen), y la 2 pregunta por los navegadores, que ya da la documentación (se descarta). `respuestas.json` contesta cuatro y descarta la de la revisión mensual.
- **Catálogo:** los mismos requisitos con ids en otro orden. La 2 dice «o» donde la fuente dice «y» (gana la 1) e inventa un requisito con la revisión mensual (se descarta). La 1 no tiene el de aprobar o rechazar (se toma de la 2). En el día festivo hay una discrepancia real: la 2 impide reservar y la fuente no lo dice.
- **Sugerencias:** `sugerencias.json` sugiere respuesta para P-002 (la da la documentación, F013) y para P-003 y P-004 (suposiciones, con los fragmentos relacionados). `test_v0.py` las valida como si esas tres estuvieran pendientes.
- **Backlog:** la 2 junta en una historia lo que la 1 separa en dos. La 1 deja `RF-009` sin historia (aviso `COB`) y la 2 tiene una historia que no está en el catálogo (se descarta).

## Qué no hace la v0

Todo esto queda para las versiones siguientes: los slots y el texto generado por la plataforma (D-23), los lotes, el inventario de funcionalidades, los criterios etiquetados, las preguntas abiertas dentro del catálogo, los ejemplos dentro del prompt y la comparación por embeddings (D-68).

Tamaño: los ocho ficheros suman unos 28 KB, menos que el prompt de requisitos v1.4 solo (32 KB).
