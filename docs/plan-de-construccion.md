# Plan de construcción

Plan por hitos, con una web mínima operativa desde el hito 1. Cada hito termina con algo que se ejecuta.

## Reglas

- Un paso cada vez, en orden, con un commit por paso.
- Si un paso se atasca más de 30 minutos, se anota el bloqueo y se pasa al siguiente que no dependa de él.
- No se pule la interfaz hasta que exista el árbitro.
- Decisiones abiertas que afectan al plan: origen de los embeddings y umbral de discrepancia grave (antes del árbitro con embeddings). La jerarquía del backlog y el modelo del árbitro se cerraron el 08-10-2026 (ver la iteración v0).
- Desde el 08-10-2026 se trabaja en la iteración v0 (hitos 8 a 13) antes que en el hito 7.

## Hito 0: preparación y prueba de humo (completado)

- [x] 1. Repositorio privado creado, con `.gitignore` y README.
- [x] 2. Herramientas instaladas y comprobadas (.NET 10, Node LTS, Docker, Git).
- [x] 3. Credenciales de la pasarela guardadas fuera del repositorio.
- [x] 4. Lista de modelos y una llamada de chat a cada modelo.
- [x] 5. `claude -p` contra la pasarela, con y sin herramientas, y captura en stream-json.

## Hito 1: esqueleto que arranca en Docker (completado)

- [x] 6. Solución .NET con `ReqArbiter.Api`, `ReqArbiter.Core` y un proyecto de tests. Endpoint `GET /health`.
- [x] 7. Frontend con Vite, React 19, TypeScript y Tailwind, con una página que muestra el estado de `/health`.
- [x] 8. Dockerfile de la API y del frontend, y `docker-compose.yml` con `db`, `api` y `web`. `.env.example` con `LITELLM_BASE_URL` y `LITELLM_API_KEY` (solo nombres, sin valores).
- [x] 9. EF Core y el proveedor de la base de datos elegida, con una primera migración de la tabla `Projects`.
- [x] 10. Frontend: crear y listar proyectos (nombre). Primer flujo de punta a punta.
- [x] 11. GitHub Actions mínimo: compilar y testear la API y compilar el frontend.

**Hecho cuando:** con `docker compose up` se crea un proyecto en el navegador, se reinician los contenedores y sigue ahí.

## Hito 2: subir archivos (sin IA) (completado)

- [x] 12. Endpoint de subida que guarda el archivo en `data/projects/{id}/` y registra nombre, tipo, tamaño y hash en la base de datos.
- [x] 13. Frontend: zona de arrastrar y soltar y lista de archivos del proyecto.
- [x] 14. Extracción de texto solo para `.txt` y `.md`, guardada junto al original. PDF y DOCX, más adelante.
- [x] 15. Límite de tamaño, tipos permitidos y mensajes de error claros.
- [x] 15b. Extracción de texto de `.docx` (DocumentFormat.OpenXml) y `.pdf` (PdfPig), con tests. Añadido durante el hito 2: adelanta lo que el paso 14 dejaba para más adelante. Sin OCR: un PDF escaneado queda sin texto.

**Hecho cuando:** se sube un `.md` y se ve su texto en la web.

## Hito 3: primera llamada a un modelo desde la web (completado)

- [x] 16. En `Core`, contrato del runner: `IRunnerProvider`, `RunRequest` y `RunResult` (con veredicto). Primero `FakeProvider`, que devuelve un JSON fijo.
- [x] 17. Proyecto `ReqArbiter.Runner` y parser de stream-json, probado con las salidas grabadas en la prueba de humo del hito 0 (saneadas antes de copiarlas al repositorio).
- [x] 18. Lanzamiento del proceso con carpeta de trabajo, lectura línea a línea de stdout, lectura de stderr por separado (el aviso `unrecognized_model` no es un fallo), latido y tiempo máximo.
- [x] 19. Lectura del veredicto final en el campo `result`, con reintento si falta o es inválido.
- [x] 20. `LiteLlmProvider`: `claude -p` real contra la pasarela, con entorno construido de forma explícita, `CLAUDE_CONFIG_DIR` propio por ejecución y `--allowedTools`. Node y Claude Code en la imagen de la API.
- [x] 21. `POST /projects/{id}/runs` con una tarea trivial (resumir los archivos) y tabla `Runs` con estado y resultado.
- [x] 22. Progreso en vivo por SSE y una consola de eventos en la web.

**Hecho cuando:** se pulsa "Ejecutar", se ve el progreso y el resultado de un modelo.

## Hito 4: doble modelo, sin árbitro (completado)

- [x] 23. La misma tarea a `SS - GPT-6-SOL` y `SS - GPT-6-LUNA` en paralelo, con dos ejecuciones ligadas a una misma tarea.
- [x] 24. Vista de dos columnas (A y B) en la web.

**Hecho cuando:** se ven dos resultados independientes de la misma entrada.

## Hito 5: primer catálogo real (completado)

- [x] 25. Prompt v1 en `prompts/generacion_requisitos/` y montaje de sus entradas (nombre del sistema, idioma, glosario, dudas resueltas vacías y documentación).
- [x] 26. Esquema y validador portados a C#, con tests con el ejemplo ReservaSalas del propio prompt.
- [x] 27. Reintento con la lista de errores del validador, máximo dos veces.
- [x] 28. Catálogo validado en la web: tabla de requisitos y preguntas abiertas.

**Hecho cuando:** se sube un documento, A y B generan catálogos válidos y se ven en la web.

## Hito 6: catálogo v1.3

Especificación: `docs/hito-6-catalogo-v1.3.md`.

- [x] 29. Especificación del catálogo v1.3, revisada por el autor.
- [x] 30. Referencia en Python: esquema, prompt y anexo v1.3, normalizador, validador con aceptación parcial, índice en el fragmentador, ejemplos y mutaciones.
- [x] 31. Port a C#: normalizador, validador, prompt con índice, revisión con aceptación parcial y una reparación, unión por tipo.
- [x] Prompt v1.4 integrado y medido (05-10-2026): ver `docs/evolucion-prompt-requisitos.md`.
- ~~32. Deduplicación por modelo tras unir los lotes.~~ Sustituido por el hito 7: cada funcionalidad se escribe en un solo lote.
- ~~33. Web: selección de secciones al lanzar el trabajo; tipo, criterios, rechazados y correcciones en el catálogo.~~ Pasa al hito 7.
- ~~34. Medición v1.2 frente a v1.3 y recalibrado de los lotes.~~ Pasa al hito 7.

**Resultado:** con el documento técnico, el catálogo pasó de 1.133 y 862 requisitos (v1.2) a 255 y 214 (v1.4), casi sin reparaciones y con la mitad de tokens, pero sigue lejos de lo revisable. El límite está en el proceso por lotes; se cierra el hito y se pasa al hito 7.

## Hito 7: catálogo v2, de arriba abajo

Propuesta: `docs/propuesta-catalogo-v2.md`.

**Aplazado (08-10-2026):** el autor prioriza el flujo completo con prompts básicos (iteración v0, hitos 8 a 13). Lo ya hecho (validadores y unión del catálogo v2) se conserva. El hito se retoma después, como mejora del prompt del catálogo.

- [ ] 35. Prompts v2 del autor (inventario y requisitos), esquemas v2 y ejemplos de ReservaSalas.
- [ ] 36. Informe de métricas automático de un trabajo (por tipo, por origen, preguntas por tipo, tokens, duración, lotes perdidos y su motivo).
- [ ] 37. Referencia en Python: validador del inventario, reparto de funcionalidades en lotes, validador de la fase 2.
- [ ] 38. Port a C#: ejecución en dos fases, unión, preguntas ordenadas por tipo, lista de funcionalidades sin `[ERROR]`.
- [ ] 39. Web: inventario, catálogo y preguntas por prioridad; selección de secciones al lanzar el trabajo.
- [ ] 40. Medición v1.4 frente a v2, dos ejecuciones por configuración.

**Hecho cuando:** con el documento técnico, el catálogo de A y el de B están completos frente a una lista de referencia hecha a mano, sin requisitos ni preguntas repetidos y sin requisitos que deberían haberse descartado. No hay un número objetivo: el tamaño sale del documento.

## Iteración v0: flujo completo con prompts básicos

Decidida por el autor el 08-10-2026. Objetivo: que el proceso entero funcione de punta a punta con prompts muy básicos y, después, mejorar los prompts fase a fase. Tiene prioridad sobre el hito 7.

### Flujo

1. El usuario crea un proyecto.
2. Sube archivos.
3. Lanza una ronda de preguntas sobre el contenido de los archivos: A ‖ B → árbitro. Contesta o descarta cada pregunta (innecesaria o fuera de alcance). Las respuestas se guardan como información del proyecto para el catálogo y el backlog.
4. Puede lanzar todas las rondas que quiera, en cualquier momento.
5. Genera el catálogo de requisitos (A ‖ B → árbitro). Solo puede hacerlo con al menos una ronda terminada y ninguna pregunta pendiente.
6. Valida el catálogo: lo revisa y, si hace falta, lo edita. Lo que cambia sustituye a lo anterior; lo sustituido deja de existir para el resto del flujo.
7. Genera el backlog (A ‖ B → árbitro) a partir del catálogo validado.
8. Valida el backlog, igual que el catálogo.
9. Genera el mockup a partir del backlog validado: un único `.html` con datos de prueba que se abre en el navegador.

### Decisiones de esta iteración (08-10-2026)

| Decisión | Origen |
|---|---|
| Prompts muy básicos en todas las fases; la calidad se trabaja después, con el flujo ya funcionando. | Autor |
| El árbitro empareja y fusiona solo con un LLM. Los embeddings llegan en un hito posterior. Es una desviación temporal de la decisión «fusión por embeddings + LLM», que sigue vigente como objetivo. | Autor |
| Árbitro: `SS - GPT-6-SOL`, configurable (`RUNNER_MODEL_ARBITER`). | Autor |
| Al árbitro le llegan las propuestas como «Propuesta 1» y «Propuesta 2», en orden aleatorio, para que no sepa cuál es la de SOL (que también es A). El reparto se guarda en el plan del trabajo. | Propuesta de Claude, aprobada por el autor |
| Discrepancia grave: en v0 no hay umbral numérico. El árbitro marca `discrepancia` con una explicación y el usuario la revisa al validar. El umbral se define con los embeddings. | Propuesta de Claude, aprobada por el autor |
| Backlog: Épica → Historia de usuario → Criterios de aceptación (Gherkin) → Tarea, sin nivel Feature. Cada historia indica los requisitos que cubre. | Autor |
| Mockup: Claude Code con Sonnet 5.5 y el token de la suscripción del autor (`claude setup-token` → `CLAUDE_CODE_OAUTH_TOKEN`). Sustituye la regla «No existe proveedor de suscripción personal ni debe crearse». Solo para uso del propio autor: si la aplicación la usa más gente, se cambia a una clave de API de Anthropic (pago por uso) o a un modelo de la pasarela. | Autor |
| El mockup lo genera un solo modelo, sin árbitro: es una visualización del análisis, no un artefacto de análisis que haya que verificar. | Autor (Sonnet 5.5); sin árbitro, propuesta de Claude aprobada por el autor |
| Validación: lo que el usuario cambia sustituye a lo anterior y las fases siguientes solo ven la versión validada. La salida original del árbitro se congela en `data/jobs/{id}/` solo para métricas; nunca se muestra como vigente ni se reutiliza. | Autor |
| El backlog también se valida antes del mockup. | Autor |
| El catálogo exige al menos una ronda terminada (aunque no haya dado preguntas) y ninguna pregunta pendiente. | Autor |
| Los prompts v0 los propone Claude en `prompts/v0/` y el autor los revisa antes de integrarlos. | Autor |
| Sin lotes: cada modelo recibe toda la documentación en una sola llamada. Si supera el presupuesto de entrada, la plataforma lo dice con un error claro. Los lotes, los prompts v1.4/v2 y sus validadores se conservan para el hito 7. | Propuesta de Claude, aprobada por el autor |
| En v0 el modelo escribe el texto de cada requisito; no se generan desde slots (D-23). | Propuesta de Claude, aprobada por el autor |
| Regenerar una fase sustituye su borrador. Si cambia algo anterior, las fases siguientes quedan «desactualizadas»: es un aviso, no se borra nada, y se pueden regenerar. | Propuesta de Claude, aprobada por el autor |
| Si solo uno de los dos modelos termina bien, no se llama al árbitro: su salida pasa como resultado con el aviso «sin verificación cruzada». Si fallan los dos, el trabajo falla. | Propuesta de Claude, aprobada por el autor |
| El mockup se muestra en un iframe `sandbox="allow-scripts"`, sin acceso al origen de la aplicación, y se puede descargar. | Propuesta de Claude, aprobada por el autor |

### Estado de cada fase

Estados: bloqueada → disponible → en curso → borrador → validada. Además, cualquier fase generada puede quedar desactualizada.

| Fase | Se puede lanzar cuando | Entrada de los modelos |
|---|---|---|
| Preguntas | Hay al menos un archivo con texto | Documentación, ajustes del proyecto y preguntas anteriores (respondidas y descartadas), marcadas como «ya planteadas, no repetir» |
| Catálogo | Al menos una ronda terminada y ninguna pregunta pendiente | Documentación, preguntas respondidas (pregunta y respuesta), nombre del sistema, idioma y glosario |
| Backlog | Catálogo validado | Catálogo validado y preguntas respondidas |
| Mockup | Backlog validado | Backlog validado, nombre del sistema e idioma |

Desactualización:

- Archivo nuevo, respuesta nueva o cambiada, o pregunta descartada → catálogo, backlog y mockup desactualizados (si existen).
- Catálogo reabierto o validado de nuevo → backlog y mockup desactualizados.
- Backlog reabierto o validado de nuevo → mockup desactualizado.

### Ejecución A ‖ B → árbitro

- Un trabajo (`Job`) por fase lanzada, con `Kind` `questions`, `catalog-v0`, `backlog` o `mockup`. Los runs llevan `Slot` `A`, `B`, `ARB` (árbitro) o `MOCK`.
- A y B reciben el mismo prompt, en paralelo, y no saben del otro ni del árbitro (decisión del 1-10-2026).
- Cuando terminan los dos con una salida válida, se lanza el árbitro con las dos salidas dentro del prompt («propuesta 1» y «propuesta 2», en orden aleatorio) y las mismas entradas de la fase como referencia.
- El árbitro devuelve el esquema de la fase y, además, por cada elemento: `de` (los ids de cada propuesta de los que sale, que la plataforma traduce a A, B o ambos), `explicacion` y, en catálogo y backlog, `discrepancia` (verdadero o falso). Lo que no pasa a la unión va a los descartes con su motivo.
- Cada salida se valida contra su esquema JSON, junto con los ids y las referencias. Si falla, se repara una vez con la lista de errores (`RetryRunner`).

### Esquemas mínimos (detalle en `prompts/v0/`)

- **Preguntas:** `preguntas[]` con `id` (`P-001`), `pregunta`, `motivo` y `fuentes` (`DOC-01`…).
- **Catálogo:** `requisitos[]` con `id` (`RF-001`, `RNF-001`, `RA-`, `RT-`, `RI-`), `tipo`, `notacion` (`EARS` o `MASTeR`), `obligatoriedad` (`shall`, `should` o `may`), `texto`, `fuentes` y `preguntas` (las respuestas en que se apoya).
- **Backlog:** `epicas[]` (`EP-01`, `titulo`, `descripcion`) → `historias[]` (`HU-001`, `titulo`, `como`, `quiero`, `para`, `requisitos`) → `criterios[]` (`CA-001`, `escenario`, `dado`, `cuando`, `entonces`) y `tareas[]` (`T-001`, `titulo`).
- **Árbitro:** el esquema de su fase más `de`, `explicacion` y (catálogo y backlog) `discrepancia` en cada pregunta, requisito o historia, y una lista de descartes (`propuesta`, `id`, `motivo`).
- **Mockup:** `output/mockup.html`. Un solo archivo con el CSS y el JS en línea, sin recursos externos, con datos de prueba ficticios en un bloque `const DATOS = {...}` y cada pantalla o sección marcada con `data-hu="HU-001"`.

### Persistencia

- Tabla `Questions`: proyecto, ronda (`JobId`), código `P-001` (numerado por proyecto), pregunta, motivo, fuentes, origen, nota del árbitro, estado (`Pendiente`, `Respondida` o `Descartada`), respuesta y fechas.
- Tabla `Deliverables`: proyecto, tipo (`catalog`, `backlog` o `mockup`), trabajo, estado (`Borrador` o `Validada`), si está desactualizado, contenido (JSON, o la ruta del `.html` en el mockup) y fechas. Hay uno vigente por tipo y proyecto. Guardar cambios sustituye su contenido.
- Original congelado: `data/jobs/{id}/resultado-arbitro.json`. Las salidas de A y B ya quedan en sus runs. Solo los lee el informe de métricas.

### Proveedores y modelos

| Rol | Proveedor | Modelo por defecto | Variables |
|---|---|---|---|
| A | `LiteLlm` | `SS - GPT-6-SOL` | `RUNNER_MODEL_A` |
| B | `LiteLlm` | `SS - GPT-6-LUNA` | `RUNNER_MODEL_B` |
| Árbitro | `LiteLlm` | `SS - GPT-6-SOL` | `RUNNER_MODEL_ARBITER` |
| Mockup | `ClaudeSubscription` | `claude-sonnet-5-5` | `MOCKUP_PROVIDER`, `MOCKUP_MODEL`, `CLAUDE_CODE_OAUTH_TOKEN` |

Todos los roles admiten `Fake`. Cada proveedor construye su propio entorno:

- El hijo de `LiteLlm` recibe `ANTHROPIC_BASE_URL` y `ANTHROPIC_AUTH_TOKEN`, y nunca el token de la suscripción.
- El hijo de `ClaudeSubscription` recibe solo `CLAUDE_CODE_OAUTH_TOKEN`, y nunca las variables de la pasarela. En Claude Code, `ANTHROPIC_AUTH_TOKEN` tiene prioridad sobre `CLAUDE_CODE_OAUTH_TOKEN`: si llegaran las dos, el mockup acabaría en la pasarela sin dar error.
- El token de `claude setup-token` dura un año y exige un plan Pro, Max, Team o Enterprise. No se usa `--bare`, porque ese modo no lee el token.

## Hito 8: base común del flujo v0

- [x] 41. Prompts v0 y esquemas mínimos en `prompts/v0/` (propuesta de Claude): `preguntas.md`, `catalogo.md`, `backlog.md`, `arbitro_preguntas.md`, `arbitro_catalogo.md`, `arbitro_backlog.md`, `mockup.md` y un `*.schema.json` por fase. Cada prompt cabe en una página. Sección «v0» en `docs/evolucion-prompt-requisitos.md`. El autor los revisa y aprueba antes del paso 42.
- [x] 42. Tarea de fase genérica en Core, con tests con ReservaSalas:
  - montaje del prompt desde la plantilla (`{{...}}`), los archivos de `input/` y la salida esperada;
  - validador por esquema (LateApexEarlySpeed), con comprobación de ids y referencias;
  - una reparación con la lista de errores;
  - toda la documentación en una sola llamada, comprobando `InputTokenBudget`.
- [x] 43. Ejecución A ‖ B → árbitro en `JobRunner`, con tests:
  - slots `A`, `B` y `ARB`, y orden aleatorio de las propuestas guardado en `plan.json`;
  - qué pasa si falla uno de los modelos o los dos;
  - `RUNNER_MODEL_ARBITER` en la configuración y en `.env.example`;
  - `FakeProvider` escribe salidas válidas de cada fase, y su árbitro une las dos propuestas.
- [x] 44. Estado del flujo en Core (fases, condiciones de paso y desactualización), con un test por regla, y `GET /projects/{id}/flow`.
- [x] 45. Web: página del proyecto por pasos (Documentos, Preguntas, Catálogo, Backlog y Mockup), con el estado de cada paso y los botones desactivados con su motivo. Las consolas actuales pasan a una pestaña «Ejecuciones», donde el catálogo v1.4 queda como «experimental».

**Hecho cuando:** con `Fake`, la página muestra los cinco pasos y sus bloqueos, y un trabajo ejecuta A y B en paralelo y después el árbitro, con las tres consolas.

## Hito 9: fase de preguntas

- [x] 46. Tabla `Questions` y su migración. `POST /projects/{id}/question-rounds` lanza el trabajo `questions`. Cuando termina el árbitro, la plataforma renumera las preguntas (`P-001`… por proyecto) y las guarda como pendientes, con su origen y la nota del árbitro.
- [x] 47. Endpoints para responder, editar la respuesta, descartar (con motivo opcional) y reabrir una pregunta, con tests. Las respuestas son la información del proyecto que reciben el catálogo y el backlog.
- [x] 48. Web: rondas con sus preguntas y filtro de pendientes. Se puede contestar o descartar, y se ven el origen (A, B o ambas) y la explicación del árbitro. Un contador de pendientes muestra por qué el catálogo está bloqueado.
- [ ] 49. Prueba real con ReservaSalas (SOL, LUNA y árbitro SOL) y dos rondas: tokens, tiempos y preguntas repetidas entre rondas.

**Hecho cuando:** se lanzan dos rondas, la segunda no repite lo ya preguntado, y las respuestas y los descartes siguen ahí tras reiniciar los contenedores.

## Hito 10: catálogo v0 y su validación

- [ ] 50. Trabajo `catalog-v0`, solo si la fase está disponible. A ‖ B → árbitro, con origen, explicación y discrepancias. La plataforma renumera por tipo.
- [ ] 51. Tabla `Deliverables` y su migración, con tests:
  - al terminar el trabajo, el original del árbitro se congela y se crea la copia de trabajo en borrador;
  - endpoints para obtenerla, guardar cambios (que sustituyen lo anterior), validar y reabrir;
  - validar exige que el esquema sea correcto y que todas las discrepancias estén revisadas.
- [ ] 52. Web: tabla editable del catálogo.
  - Se editan texto, tipo, notación, obligatoriedad y fuentes, y se pueden añadir y borrar requisitos.
  - Las discrepancias aparecen destacadas, con la explicación del árbitro y una casilla «revisada».
  - Un catálogo validado es de solo lectura. «Reabrir» lo devuelve a borrador y desactualiza el backlog y el mockup.
- [ ] 53. Prueba real con ReservaSalas: generar, editar y validar. Resultados en la sección v0 de `docs/evolucion-prompt-requisitos.md`.

**Hecho cuando:** tras validar un catálogo editado y recargar, solo se ve la versión editada, y el original del árbitro sigue en `data/jobs/{id}/`.

## Hito 11: backlog v0 y su validación

- [ ] 54. Trabajo `backlog` desde el catálogo validado: A ‖ B → árbitro. Se validan el esquema, que los ids sean únicos y que las referencias apunten a requisitos que existen. Si un requisito funcional se queda sin historia, la plataforma avisa. Las tareas solo se validan en su forma y se revisan a mano en la prueba, porque son el eslabón más débil según la investigación.
- [ ] 55. El backlog se guarda en `Deliverables` (tipo `backlog`), con el original congelado y los mismos endpoints de guardar, validar y reabrir. Tests.
- [ ] 56. Web: árbol editable Épica → Historia → Criterios → Tareas, con los requisitos de cada historia, la cobertura de requisitos, las discrepancias y el botón de validar.
- [ ] 57. Prueba real con ReservaSalas.

**Hecho cuando:** el backlog validado tiene todas sus historias trazadas a requisitos del catálogo validado, y la plataforma avisa de los requisitos funcionales sin historia.

## Hito 12: mockup

- [ ] 58. Prueba de humo fuera de la aplicación:
  - el autor genera el token con `claude setup-token` y lo guarda en `.env` como `CLAUDE_CODE_OAUTH_TOKEN`;
  - `claude -p --model claude-sonnet-5-5` con ese token, con y sin `Write`, y con `CLAUDE_CONFIG_DIR` vacío;
  - se confirma el id del modelo con Claude Code 2.1.283 y se anota en `docs/`, sin el token.
- [ ] 59. Proveedor `ClaudeSubscription` en `ReqArbiter.Runner`:
  - entorno explícito solo con `CLAUDE_CODE_OAUTH_TOKEN`;
  - el token se oculta en `SecretRedactor`;
  - `.env.example` con `CLAUDE_CODE_OAUTH_TOKEN`, `MOCKUP_PROVIDER` y `MOCKUP_MODEL`, sin valores;
  - el proveedor se elige por rol en la configuración;
  - tests con FakeClaude: el hijo de la pasarela nunca recibe el token, y el del mockup nunca recibe la clave ni la dirección de la pasarela.
- [ ] 60. Trabajo `mockup`: un solo run (`MOCK`) a partir del backlog validado, que escribe `output/mockup.html`.
  - Validación: el archivo existe, no supera el tamaño máximo, no tiene recursos externos (`src` o `href` con `http`) y todas las historias tienen su `data-hu`. Una reparación.
  - El Fake escribe una plantilla con las historias del backlog.
  - Se copia en `data/projects/{id}/mockup/`. Tests.
- [ ] 61. Web: el mockup se ve en un iframe con `sandbox="allow-scripts"`, con botón para descargar el `.html` y aviso cuando está desactualizado.
- [ ] 62. Prueba real con Sonnet 5.5: el `.html` descargado se abre con doble clic, sin servidor ni conexión.

**Hecho cuando:** el `.html` descargado se abre en el navegador sin conexión, muestra datos de prueba y cubre todas las historias del backlog validado.

## Hito 13: flujo completo de punta a punta

- [ ] 63. Desactualización completa en la API y en la web: cualquier fase se puede lanzar de nuevo y las siguientes muestran el aviso. Test del flujo completo con `Fake`, de crear el proyecto al mockup.
- [ ] 64. Informe automático por trabajo y fase, base de la evaluación:
  - tokens y duración;
  - elementos de A, de B y de la fusión;
  - acuerdo (origen `ambas`) y discrepancias;
  - cambios del usuario frente al original congelado (añadidos, borrados y modificados).
- [ ] 65. Ejecución completa con ReservaSalas y con un segundo documento público. Resultados en `docs/`: qué hace cada prompt v0, sus resultados y qué hay que mejorar.
- [ ] 66. Actualizar el README (puesta en marcha con el token y el flujo), `CLAUDE.md` y la hoja de ruta.

**Hecho cuando:** partiendo de un `docker compose up` limpio, un usuario crea un proyecto, sube un documento, hace al menos una ronda de preguntas, valida el catálogo y el backlog y abre el mockup en el navegador, todo desde la web.

## Después

Por este orden:

1. Mejorar los prompts fase a fase sobre el flujo que ya funciona. En el catálogo, se retoma el hito 7 con los prompts v2 del autor.
2. Árbitro con embeddings. Queda pendiente decidir su origen (un modelo local en el contenedor de la API u otro proveedor) y el umbral de discrepancia grave.
3. Aprobación por umbral y evaluación: precision, recall y F1 frente a la versión validada y a listas de referencia hechas a mano.
