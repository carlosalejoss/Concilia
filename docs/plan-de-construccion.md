# Plan de construcción

Plan por hitos, con una web mínima operativa desde el hito 1. Cada hito termina con algo que se ejecuta.

## Reglas

- Un paso cada vez, en orden, con un commit por paso.
- Si un paso se atasca más de 30 minutos, se anota el bloqueo y se pasa al siguiente que no dependa de él.
- No se pule la interfaz hasta que exista el árbitro.
- Decisiones abiertas que afectan al plan: ninguna del hito 17. Los umbrales del emparejamiento (0,90 y 0,70) los fijó el autor el 19-10-2026 tras calibrarlos en el paso 87, y cómo marcan la discrepancia grave, en el paso 88. La jerarquía del backlog y el modelo del árbitro se cerraron el 08-10-2026 (ver la iteración v0), y el origen de los embeddings el 16-10-2026 (hito 17).
- Desde el 08-10-2026 se trabaja en la iteración v0 (hitos 8 a 13) antes que en el hito 7.
- Desde el 16-10-2026, con los hitos 8 a 16 terminados, se sigue con los hitos 17 a 22 en su orden, acordado con el autor: 17 embeddings, 18 fiabilidad y calidad, 19 exportar, 20 trazabilidad y comparación, 21 historial y 22 evaluación. Los pasos marcados «por decidir» esperan a que el autor elija antes de empezarlos. El experimento del árbitro (paso 103) se cerró el mismo día.

## Dónde estamos (19-10-2026)

- **Terminado:** los pasos 86 (servicio de embeddings en la API), 87 (emparejamiento de A y B por significado, con los umbrales calibrados con el criterio del autor: 0,90 y 0,70).
- **Hecho el 19-10-2026:** el paso 88, el árbitro con embeddings (prompts v0.10), con su prueba real (la unión es más estable: 202–222 requisitos frente a 209–291). El hito 17 queda completado.
- **Hecho el 19-10-2026:** el hito 18 entero (pasos 89 a 95), salvo sus pruebas reales con modelos, que lanza el autor: el flujo completo del proyecto grande sin errores internos ni reparaciones que no caben, y el mockup de dos proyectos sin `.zip` con estilos distintos.
- **Siguiente:** esas pruebas reales y, después, el hito 19 (exportar los entregables).
- **Hito 19, 20-10-2026:** hecho el paso 96 (el catálogo a Word y PDF); siguen el 97 (Gherkin) y el 98 (CSV para Jira y Azure DevOps).

## Hito 0: preparación y prueba de humo (completado)

- [x] 1. Repositorio privado creado, con `.gitignore` y README.
- [x] 2. Herramientas instaladas y comprobadas (.NET 10, Node LTS, Docker, Git).
- [x] 3. Credenciales de la pasarela guardadas fuera del repositorio.
- [x] 4. Lista de modelos y una llamada de chat a cada modelo.
- [x] 5. `claude -p` contra la pasarela, con y sin herramientas, y captura en stream-json.

## Hito 1: esqueleto que arranca en Docker (completado)

- [x] 6. Solución .NET con `Concilia.Api`, `Concilia.Core` y un proyecto de tests. Endpoint `GET /health`.
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
- [x] 17. Proyecto `Concilia.Runner` y parser de stream-json, probado con las salidas grabadas en la prueba de humo del hito 0 (saneadas antes de copiarlas al repositorio).
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
| Si solo uno de los dos modelos termina bien, no se llama al árbitro: su salida pasa como resultado con el aviso «sin verificación cruzada». Si fallan los dos, el trabajo falla. (Sustituida el 14-10-2026 por decisión del autor: si falla uno de los dos, el trabajo falla y no hay resultado; se lanza de nuevo.) | Propuesta de Claude, aprobada por el autor |
| El mockup se muestra en un iframe `sandbox="allow-scripts"`, sin acceso al origen de la aplicación, y se puede descargar. | Propuesta de Claude, aprobada por el autor |
| (09-10-2026) La IA puede sugerir la respuesta de las preguntas pendientes, una a una o todas las de una ronda. La sugerencia rellena el cuadro de respuesta y no se guarda nada hasta que el usuario la guarda, tal cual o editada. Dice en qué se basa (documentación, con los fragmentos, o suposición) y la respuesta guarda su origen: `usuario`, `ia` o `ia-editada`. | Autor |
| (09-10-2026) Las sugerencias las da un solo modelo, sin árbitro: `SS - GPT-6-SOL`, configurable (`RUNNER_MODEL_SUGGESTIONS`). No son un paso del flujo ni desactualizan nada. | Autor (un modelo, el más conveniente); SOL, propuesta de Claude |

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
| Sugerencias de respuesta | `LiteLlm` | `SS - GPT-6-SOL` | `RUNNER_MODEL_SUGGESTIONS` |
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
- [ ] 49. Prueba real con ReservaSalas (SOL, LUNA y árbitro SOL) y dos rondas: tokens, tiempos y preguntas repetidas entre rondas. (09-10-2026: hecha la primera ronda, con sus tokens y tiempos en `docs/evolucion-prompt-requisitos.md`; falta la segunda.)
- [x] 49b. Sugerencias de respuesta con IA (añadido el 09-10-2026, a petición del autor):
  - prompt `prompts/v0/sugerir_respuestas.md`, con su esquema, sus comprobaciones y su ejemplo (el autor lo revisa);
  - trabajo `answer-suggestions` con un solo run (`SUG`, `RUNNER_MODEL_SUGGESTIONS`): `POST /question-rounds/{id}/suggestions` (todas las pendientes de la ronda) y `POST /questions/{id}/suggestion` (una); uno a la vez por proyecto;
  - al terminar, cada pregunta pendiente guarda su sugerencia (texto, base, fuentes y explicación) sin cambiar de estado;
  - al guardar la respuesta, `fromSuggestion` deja el origen `ia` (sin cambios) o `ia-editada`; si no, `usuario`;
  - web: botón «Sugerir con IA» por ronda y por pregunta; la sugerencia abre el cuadro de respuesta relleno, con su base, fuentes y explicación; la respuesta muestra su origen.

**Hecho cuando:** se lanzan dos rondas, la segunda no repite lo ya preguntado, y las respuestas y los descartes siguen ahí tras reiniciar los contenedores.

## Hito 10: catálogo v0 y su validación

- [x] 50. Trabajo `catalog-v0`, solo si la fase está disponible. A ‖ B → árbitro, con origen, explicación y discrepancias. La plataforma renumera por tipo.
- [x] 51. Tabla `Deliverables` y su migración, con tests:
  - al terminar el trabajo, el original del árbitro se congela y se crea la copia de trabajo en borrador;
  - endpoints para obtenerla, guardar cambios (que sustituyen lo anterior), validar y reabrir;
  - validar exige que el esquema sea correcto y que todas las discrepancias estén revisadas.
- [x] 52. Web: tabla editable del catálogo.
  - Se editan texto, tipo, notación, obligatoriedad y fuentes, y se pueden añadir y borrar requisitos.
  - Las discrepancias aparecen destacadas, con la explicación del árbitro y una casilla «revisada».
  - Un catálogo validado es de solo lectura. «Reabrir» lo devuelve a borrador y desactualiza el backlog y el mockup.
- [ ] 53. Prueba real con ReservaSalas: generar, editar y validar. Resultados en la sección v0 de `docs/evolucion-prompt-requisitos.md`. (09-10-2026: generado y validado, con resultados; falta editarlo.)

**Hecho cuando:** tras validar un catálogo editado y recargar, solo se ve la versión editada, y el original del árbitro sigue en `data/jobs/{id}/`.

## Hito 11: backlog v0 y su validación

- [x] 54. Trabajo `backlog` desde el catálogo validado: A ‖ B → árbitro. Se validan el esquema, que los ids sean únicos y que las referencias apunten a requisitos que existen. Si un requisito funcional se queda sin historia, la plataforma avisa. Las tareas solo se validan en su forma y se revisan a mano en la prueba, porque son el eslabón más débil según la investigación.
- [x] 55. El backlog se guarda en `Deliverables` (tipo `backlog`), con el original congelado y los mismos endpoints de guardar, validar y reabrir. Tests.
- [x] 56. Web: árbol editable Épica → Historia → Criterios → Tareas, con los requisitos de cada historia, la cobertura de requisitos, las discrepancias y el botón de validar.
- [x] 57. Prueba real con ReservaSalas (09-10-2026: válido a la primera, 16 historias que cubren los 22 requisitos funcionales; resultados en `docs/evolucion-prompt-requisitos.md`).

**Hecho cuando:** el backlog validado tiene todas sus historias trazadas a requisitos del catálogo validado, y la plataforma avisa de los requisitos funcionales sin historia.

## Hito 12: mockup

- [x] 58. Prueba de humo fuera de la aplicación (09-10-2026, resultados en «Runner y pasarela (verificado)» de `CLAUDE.md`):
  - el autor genera el token con `claude setup-token` y lo guarda en `.env` como `CLAUDE_CODE_OAUTH_TOKEN`;
  - `claude -p --model claude-sonnet-5-5` con ese token, con y sin `Write`, y con `CLAUDE_CONFIG_DIR` vacío;
  - se confirma el id del modelo con Claude Code 2.1.283 y se anota en `docs/`, sin el token.
- [x] 59. Proveedor `ClaudeSubscription` en `Concilia.Runner` (el acceso al modelo es una interfaz, `IModelAccess`, con la pasarela y la suscripción como implementaciones; la suscripción solo se admite en el rol del mockup):
  - entorno explícito solo con `CLAUDE_CODE_OAUTH_TOKEN`;
  - el token se oculta en `SecretRedactor`;
  - `.env.example` con `CLAUDE_CODE_OAUTH_TOKEN`, `MOCKUP_PROVIDER` y `MOCKUP_MODEL`, sin valores;
  - el proveedor se elige por rol en la configuración;
  - tests con FakeClaude: el hijo de la pasarela nunca recibe el token, y el del mockup nunca recibe la clave ni la dirección de la pasarela.
- [x] 60. Trabajo `mockup`: un solo run (`MOCK`) a partir del backlog validado, que escribe `output/mockup.html` (`POST /projects/{id}/mockup-jobs`; el `.html` vigente se sirve en `GET /projects/{id}/mockup`, con `?download=true` para descargarlo y una cabecera CSP `sandbox` que le impide usar la red o la aplicación aunque se abra fuera del iframe). El mockup no se edita ni se valida: se genera de nuevo.
  - Validación: el archivo existe, no supera el tamaño máximo, no tiene recursos externos (`src` o `href` con `http`) y todas las historias tienen su `data-hu`. Una reparación.
  - El Fake escribe una plantilla con las historias del backlog.
  - Se copia en `data/projects/{id}/mockup/`. Tests.
- [x] 61. Web: el mockup se ve en un iframe con `sandbox="allow-scripts"`, con botón para descargar el `.html` y aviso cuando está desactualizado.
- [x] 62. Prueba real con Sonnet 5.5 (13-10-2026: generado con los prompts v0.4, 53 historias en 23 min; el 43 % del límite de 5 h de la suscripción; al autor no le convence el resultado y lo deja así por ahora; 09-10-2026: el primer intento, con el backlog de 37 historias del documento B, agotó los 15 minutos sin terminar la respuesta, porque el `.html` entero va en un solo `Write`; los límites del mockup pasan a 30 minutos sin salida y 45 en total, por decisión del autor. 13-10-2026: con 53 historias, la respuesta pasó de 64.000 tokens de salida; el autor aprueba escribir el mockup por partes, una base y un fichero por épica, que la plataforma junta, en los prompts v0.4): el `.html` descargado se abre con doble clic, sin servidor ni conexión.

**Hecho cuando:** el `.html` descargado se abre en el navegador sin conexión, muestra datos de prueba y cubre todas las historias del backlog validado.

## Hito 12b: lectura de la documentación por partes

Propuesta del team leader, aprobada por el autor el 09-10-2026. Va antes del hito 13. Sustituye la decisión v0 «sin lotes: toda la documentación en una sola llamada; si no cabe, error»: la plataforma deja los archivos en `input/` y el modelo los lee por partes con `Read`, como Claude Code en local. No hay lotes: cada run sigue viendo toda la documentación.

| Decisión (09-10-2026) | Origen |
|---|---|
| El modelo lee los archivos por partes con `Read`; el prompt ya no lleva la documentación. | Team leader, aprobada por el autor |
| En todas las fases: preguntas, sugerencias, catálogo, backlog, los tres árbitros y el mockup. | Autor |
| Van a archivo la documentación, el catálogo validado, el backlog validado y las dos propuestas del árbitro; lo pequeño (glosario, respuestas, preguntas previas y pendientes) sigue en el prompt. | Autor (propuesta de Claude) |
| Un solo `input/documentacion.txt`, un fragmento por línea (`[DOC-01#F007 §2.5] texto`); un fragmento de más de 1.500 caracteres sigue en líneas `[DOC-01#F007 sigue]`, porque `Read` corta las líneas de más de 2.000. | Autor (propuesta de Claude) |
| La plataforma comprueba en los eventos del run qué líneas leyó el modelo; si falta alguna de los archivos obligatorios, es un error que pide la reparación con las líneas que faltan. | Autor (propuesta de Claude) |
| Si Claude Code compacta el contexto, se permite y queda como aviso en el run. | Autor (propuesta de Claude) |
| La documentación ya no se rechaza por el presupuesto de 80.000 tokens: este solo se aplica al prompt. La web avisa por encima de unos 80.000 tokens y hay un tope total configurable (300.000 por defecto) que se comprueba al lanzar la fase. | Autor (propuesta de Claude) |
| Hasta 30 turnos por run (antes 10), configurable; el tiempo máximo no cambia. | Autor (propuesta de Claude) |
| No se conserva el modo «todo en el prompt» como opción: la medida de partida (paso 67) queda como referencia. | Autor |

- [x] 67. Medida de partida (el autor, 09-10-2026; resultados en `docs/evolucion-prompt-requisitos.md`): la ronda de preguntas con el documento grande, todo en el prompt, con la corrección del presupuesto (commit `7410b24`).
- [x] 68. Runner: registra las llamadas a `Read` (archivo, líneas, si fue bien) y las compactaciones de contexto; `ReadCoverage` dice qué líneas quedan sin leer; una petición puede dejar `input/` fuera del presupuesto (`InputFilesInBudget`). Tests con un fixture sintético.
- [x] 69. Fases y mockup con lectura por partes: `input/documentacion.txt`, `Read` y `Write`, 30 turnos, comprobación de lectura completa con reparación, tope total de documentación y prompts v0.1 (redactados por Claude, revisados por el autor). Tests con `Fake`.
- [x] 70. Web: tamaño estimado de la documentación en el paso «Documentos» y aviso por encima de 80.000 tokens.
- [x] 71. Prueba real con el documento grande (09-10-2026: funciona, +50 % de tokens y tiempo con un documento que cabe, dos reparaciones; detalle en `docs/evolucion-prompt-requisitos.md`): la misma ronda que en el paso 67, y comparación (tokens, tiempo, turnos, compactaciones, lectura completa y preguntas).

**Hecho cuando:** la ronda del documento grande termina, cada modelo ha leído todas sus líneas, y la comparación con el paso 67 está en `docs/evolucion-prompt-requisitos.md`.

## Hito 13: flujo completo de punta a punta

- [x] 63. Desactualización completa en la API y en la web: cualquier fase se puede lanzar de nuevo y las siguientes muestran el aviso. Test del flujo completo con `Fake`, de crear el proyecto al mockup. (15-10-2026: la API y la web ya lo cumplían; se añade `FullFlowTests`, que recorre la API como la web con una base de datos en memoria y comprueba la desactualización al relanzar el catálogo.)
- [x] 64. Informe automático por trabajo y fase, base de la evaluación:
  - tokens y duración;
  - elementos de A, de B y de la fusión;
  - acuerdo (origen `ambas`) y discrepancias;
  - cambios del usuario frente al original congelado (añadidos, borrados y modificados).
  (15-10-2026: `GET /jobs/{id}/report`, el panel «Informe del trabajo» en la vista de ejecuciones y `GET /projects/{id}/report.csv`, un trabajo por fila, para la hoja de cálculo de la evaluación. Los cambios del usuario se emparejan por contenido y no por id, porque la plataforma renumera tras cada edición; en las preguntas, cuántas se respondieron, descartaron o quedaron pendientes, y quién escribió cada respuesta.)
- [x] 65. Ejecución completa con ReservaSalas y con un segundo documento público. Resultados en `docs/`: qué hace cada prompt v0, sus resultados y qué hay que mejorar. (15-10-2026, a petición del autor: en lugar del documento público, dos proyectos ficticios generados por Claude en `ejemplos/`, uno mediano (7 archivos, unos 5.900 tokens) y otro grande (21 archivos, unos 23.400 tokens), en todos los formatos admitidos y con contradicciones y huecos puestos a propósito, listados en `ejemplos/README.md` como referencia para evaluar la fase de preguntas. 16-10-2026: flujo completo de los dos proyectos; resultados en `docs/resultados-paso-65/`. Las 38 contradicciones y huecos se tratan como se esperaba, y la unión del árbitro se parece mucho a la propuesta de A, lo que abre el experimento de `docs/experimento-sesgo-arbitro.md`.)
- [x] 66. Actualizar el README (puesta en marcha con el token y el flujo), `CLAUDE.md` y la hoja de ruta. (15-10-2026: README reescrito con el flujo actual, la puesta en marcha con cada variable, la evaluación y la hoja de ruta.)

**Hecho cuando:** partiendo de un `docker compose up` limpio, un usuario crea un proyecto, sube un documento, hace al menos una ronda de preguntas, valida el catálogo y el backlog y abre el mockup en el navegador, todo desde la web.

## Hito 14: interfaz limpia y organizada

Decidido por el autor el 13-10-2026. La web era una sola página larga en la que todo estaba apilado y abierto: proyectos, configuración, los cinco pasos con todo su contenido y las ejecuciones en otra pestaña. El objetivo es una web limpia en la que cada funcionalidad se vea clara y se pueda organizar, plegar y desplegar.

| Decisión (13-10-2026) | Origen |
|---|---|
| Barra lateral con los proyectos; la configuración del proyecto pasa a un panel lateral. | Autor |
| Un paso cada vez, con una barra de pasos que muestra el estado de todos. | Autor |
| Las ejecuciones de cada paso se ven dentro del paso («Actividad»); la vista técnica de ejecuciones se mantiene aparte. | Autor |
| URLs por proyecto y paso (`#/proyectos/27/catalogo`), sin dependencias, para seguir en el mismo sitio al recargar. | Autor |
| Iconos con `lucide-react`. | Autor |
| Modo claro y oscuro, con colores definidos como variables (`index.css`). | Autor |
| Un kit de componentes común (`src/ui/`): botón, etiqueta de estado, aviso, sección plegable, panel lateral y estado vacío. Lo que el usuario pliega o despliega se recuerda en el navegador. | Propuesta de Claude, aprobada por el autor |
| Plan de diseño (skill `frontend-design`): papel gris verdoso y tinta, un solo acento (el verde de validación), azul pizarra para el modelo A y ciruela para el B. IBM Plex Sans en la interfaz y Source Serif 4 para el contenido que escriben los modelos; las fuentes van empaquetadas (`@fontsource`) para que funcione sin conexión. Jerarquía con espacio y líneas finas en lugar de tarjetas, sin etiquetas en mayúsculas. El elemento distintivo es la marca de procedencia (`Provenance`): una barra en dos mitades, A y B, junto a cada pregunta, requisito e historia, partida en rojo si hay discrepancia. | Propuesta de Claude, aprobada por el autor |

- [x] 72. Estructura y kit: barra lateral, cabecera del proyecto (estado de la API, ajustes, trabajo en curso, modo claro u oscuro), barra de pasos con un solo paso visible, URLs y componentes comunes. El contenido de cada paso se reutiliza tal cual.
- [x] 73. Documentos y Preguntas: tabla compacta de archivos con zona para arrastrar; rondas en acordeón, filtros, una línea por pregunta que se despliega para responder, y «Sugerir con IA» en la barra de la ronda.
- [x] 74. Catálogo: agrupado por tipo en grupos plegables, búsqueda y filtros, filas de solo lectura que se despliegan para editar, y barra fija con Guardar y Validar.
- [x] 75. Backlog: árbol plegable épica → historia → criterios y tareas, «Desplegar todo» y «Plegar todo», filtros y tarjeta de cobertura.
- [x] 76. Mockup y Actividad: visor a ancho completo con su barra; panel «Actividad de este paso» con sus trabajos y la consola, y la vista técnica de ejecuciones con el mismo estilo.

Revisado por el autor en el navegador el 16-10-2026: funciona bien.

**Hecho cuando:** con un proyecto real (documento B), cada paso cabe en pantalla plegado, se puede desplegar lo que se necesita, la URL lleva al mismo sitio al recargar y el modo oscuro se ve bien en todos los pasos.

## Hito 15: más tipos de archivo

Pedido por el autor el 14-10-2026: además de `.txt`, `.md`, `.docx` y `.pdf`, los proyectos admiten los tipos que suele traer la documentación de un cliente. Cada formato se convierte a texto con las marcas que ya entienden el fragmentador y el lector de la web: títulos `##`, elementos `- ` y filas de tabla `| a | b |`, con una línea en blanco entre bloques para que cada fila o elemento sea un fragmento citable.

| Decisión (14-10-2026) | Origen |
|---|---|
| Primera tanda: `.xlsx`, `.csv`, `.pptx`, `.html`/`.htm`, `.eml`, `.msg`, `.vtt`, `.srt`, `.json`, `.yaml`/`.yml` y `.xml`. Sin imágenes (no hay OCR). | Autor |
| Hojas de cálculo: una tabla por hoja, como mucho 2.000 filas por hoja, con una línea que dice cuántas se omiten. | Propuesta de Claude, aprobada por el autor |
| Librerías nuevas, todas MIT: HtmlAgilityPack (HTML), MimeKit (`.eml`) y MsgReader (`.msg`). Las hojas y presentaciones usan DocumentFormat.OpenXml, que ya estaba. | Claude |
| Los adjuntos de un correo no se leen: se listan por nombre y, si interesan, se suben aparte. | Claude |
| Un `.zip` no es documentación: serán mockups existentes del cliente cuyo estilo debe seguir nuestro mockup. Es otra funcionalidad, que se diseña aparte. | Autor |

- [x] 77. Convertidores (`SpreadsheetText`, `SlidesText`, `HtmlText`, `EmailText`, `TranscriptText`), reglas de subida en la API y en la web, y tests con documentos construidos en memoria. Un `.msg` no se puede construir en un test: solo se prueba que uno dañado da error.
- [x] 78. Referencia de estilo para el mockup a partir de un `.zip` con un mockup del cliente (prompts v0.5; detalle en `prompts/v0/CHANGELOG.md`). Decisiones del autor (14-10-2026): el `.zip` se sube en Documentos, marcado como «estilo»; el modelo recibe solo los ficheros y saca el estilo él solo; las imágenes de la referencia entran en el mockup (la plataforma las incrusta). Probado por el autor con un modelo el 16-10-2026: funciona bien.

## Hito 16: el mockup como una sola aplicación

Pedido por el autor el 14-10-2026, para probar el mockup con `SS - GPT-6-SOL`.

| Decisión (14-10-2026) | Origen |
|---|---|
| Un único prototipo que reúne todas las historias en las pantallas de la aplicación real, en lugar de una vista por historia. | Autor |
| Se construye con muchas llamadas: varias ejecuciones del mismo trabajo (diseño, una por pantalla y una revisión del conjunto) y no una sesión larga. | Propuesta de Claude, elegida por el autor |
| Modelo: `SS - GPT-6-SOL` por la pasarela (`MOCKUP_PROVIDER=LiteLlm`, `MOCKUP_MODEL`). La versión 6.1, si existe, se pone en `MOCKUP_MODEL` sin tocar código. | Autor |
| Con una referencia de estilo subida, el usuario elige al generar si el mockup la sigue o se hace desde cero. | Autor |
| Al terminar una fase la web se queda en su paso; no avanza sola al siguiente. | Autor |

- [x] 79. Prompts v0.6 (`mockup_diseno.md`, `mockup_pantalla.md`, `mockup_integracion.md`), `MockupMap`, `MockupTask` por pasos, orquestación en `JobRunner` (las pantallas y la revisión se crean cuando el diseño dice cuántas pantallas hay), pasos en la web y tests con `Fake`.
- [x] 80. Prueba real con `SS - GPT-6-SOL` (documento C, 19 pantallas): el diseño y las pantallas salen válidos; la revisión del conjunto no cabe en 128k. Resultados en `docs/evolucion-prompt-requisitos.md` (v0.8).
- [x] 81. Prompts v0.8: la plataforma junta y revisa el prototipo (`MockupCoherence`) y corrige con un modelo solo las partes con problemas (`mockup_correccion.md`); reanudar un mockup fallido desde sus pasos aceptados. Decidido por el autor el 15-10-2026, a propuesta de Claude.
- [x] 82. Reanudar el mockup del documento C y medirlo: se genera (19 pantallas, una corrección), pero es un índice de pantallas y no una aplicación.
- [x] 83. Prompts v0.9: el mockup como aplicación que funciona (inicio de sesión con los usuarios de prueba a la vista, una aplicación por rol, menús de secciones y navegación por acciones), con la navegación en el mapa y comprobada por la plataforma. Decidido por el autor el 15-10-2026.
- [x] 84. Generar el mockup del documento C con la v0.9 y medirlo: 25 pantallas, las 46 historias, 866 KB, en 40 minutos y con una sola corrección. El autor lo da por bueno (15-10-2026).

## Hito 17: embeddings en local

**Primero de esta tanda** (autor, 16-10-2026: «quiero usarlos al 100 %, pueden aportar mucho valor al TFG»). Resuelve la decisión abierta desde el principio (el origen de los embeddings): la pasarela no los ofrece. Decidido por el autor (16-10-2026): el modelo `paraphrase-multilingual-MiniLM-L12-v2` (ONNX, Apache 2.0, español e inglés), ejecutado con ONNX Runtime dentro del contenedor de la API, sin servicios nuevos.

- [x] 85. **Decisión del autor:** origen de los embeddings, decidido el 16-10-2026 (el modelo local de arriba), y umbral de «discrepancia grave», que se calibra con datos reales en el paso 87 antes de fijarlo. (19-10-2026: dos umbrales, «lo mismo» desde 0,90 y «parecido» desde 0,70, fijados por el autor tras la calibración del paso 87. Cómo marcan la discrepancia grave se concreta en el paso 88; la candidata es la franja «parecido».)
- [x] 86. Servicio de embeddings en la API: el modelo, el tokenizador y el cálculo del vector de un texto, y la similitud entre dos textos, con tests. (19-10-2026: `Concilia.Core/Embeddings`. `ITextEmbedder` con dos implementaciones: `OnnxTextEmbedder`, con el modelo en fp32 (ONNX Runtime 1.30.0) y su tokenizador SentencePiece (Microsoft.ML.Tokenizers 2.0.0), y `FakeTextEmbedder`, que cuenta palabras. El vector es la media de los vectores de token sin el relleno, como en sentence-transformers. Textos de hasta 128 tokens, de 32 en 32. El modelo se descarga en la imagen de Docker, con el commit de Hugging Face fijado y el SHA-256 comprobado, y en local con `scripts/descargar-modelo-embeddings.ps1`. `Embeddings:Provider` (`Fake` por defecto, `Onnx` en Docker) y `POST /embeddings/similarity` para probarlo a mano. Comprobado frente a Python con 14 textos: los mismos ids de tokens que Hugging Face y una diferencia máxima de 9,4·10⁻⁷ con los vectores de sentence-transformers. Los 6 tests que necesitan el modelo se saltan si no está, como en GitHub Actions.)
- [x] 87. Emparejamiento de A y B por significado, fuera del LLM: qué elementos de A y de B dicen lo mismo y cuáles solo están en uno. Es la referencia objetiva del experimento del sesgo del árbitro (`docs/experimento-sesgo-arbitro.md`). (19-10-2026: `Concilia.Core/Matching/ProposalMatcher` y `GET /jobs/{id}/matching`. Cada elemento se une con el más parecido de la otra propuesta y queda como «mismo» (desde 0,90), «parecido» (desde 0,70) o «distinto». Calibración con la opción D del autor: 48 pares de los proyectos ficticios etiquetados por el autor fijan los umbrales (coinciden en 37 de 48 con las tres etiquetas), y el consenso de los 5 y 6 arbitrajes de clínicas los comprueba (el 99 % y el 82 % de lo que juntan los árbitros es «mismo» o «parecido»). Acuerdo por elemento con el árbitro en los 9 trabajos originales: 89 %. El árbitro juntó los 9 pares «mismo» del autor y solo 2 de sus 22 «distintos». Scripts y resultados en `docs/calibracion-embeddings/`.)
- [x] 88. Árbitro con embeddings, como estaba en el diseño original: el emparejamiento se calcula antes, el LLM explica y fusiona, y la discrepancia grave la marca el umbral. (19-10-2026, prompts v0.10, decisiones del autor: el emparejamiento es orientativo: el árbitro lo recibe en `input/emparejamiento.txt` y puede apartarse si la fuente lo justifica; la discrepancia grave es lo que el árbitro hace en contra de los números, es decir, unir algo sin nada «mismo» ni «parecido» en la otra propuesta o separar un par «mismo» (`ArbiterDeviations`), y se suma a la que marca el propio árbitro; en las preguntas, que no tienen discrepancia, solo se cuenta. Estimación con los trabajos de antes: de 0 a 10 por trabajo, y 21 en el backlog del proyecto grande. Prueba real del 19-10-2026, lanzada por el autor: con las mismas propuestas del catálogo de clínicas, 3 arbitrajes de SOL v0.10 dan una unión de 202, 213 y 222 requisitos, frente a 209, 220 y 291 con la v0.9. Las desviaciones no bajan (unas 8 por ejecución). Detalle en `docs/evolucion-prompt-requisitos.md`, v0.10.)

**Hecho cuando:** el emparejamiento de A y B de un trabajo real coincide en lo esencial con lo que hizo el árbitro, y las diferencias se pueden explicar.

## Hito 18: fiabilidad y calidad

Acordado con el autor el 16-10-2026, tras el paso 65 y el experimento del árbitro. Recoge lo que falló o se quedó corto en las pruebas reales.

- [x] 89. Fiabilidad de LUNA. Comete errores de formato que obligan a reparar: claves repetidas, elementos sin clasificar (`ARB-FALTA`) y números de línea citados como fragmentos. Hay que hacer los prompts más explícitos y añadir más correcciones mecánicas en la plataforma. **Por decidir** cuáles se corrigen solas, por ejemplo las citas que son números de línea, las etiquetas `<html>`, `<body>`… de las pantallas del mockup, o un id cuyo prefijo no coincide con su tipo (`RF-066` de tipo `RNF`: la plataforma podría renumerarlo, como hace la v1.3). (19-10-2026, decisiones del autor: las cuatro correcciones automáticas y sin tocar los prompts, para poder medir el efecto de las correcciones por separado. `PlatformFixes`, antes de comprobar cada salida y en lo que se guarda, con un aviso cada una: `REF-LINEA`, la cita de un número de línea del mismo documento cambiada por su fragmento; `TIPO`, renumeración por tipo cuando el prefijo no coincide; y `ARB-FALTA`, el elemento olvidado por el árbitro añadido donde está su par «mismo» del emparejamiento. Y en el mockup, `FRAGMENTO`: la pantalla escrita como documento entero se queda con su contenido. No se arregla el JSON mal formado ni la salida cortada, que son del paso 90.)
- [x] 90. Reparar editando. **Por decidir.** La reparación pega la salida anterior entera en el prompt, y la de un catálogo de árbitro (unos 30.000 tokens) no cabe en 128k junto con las propuestas. Con `Edit`, el modelo corregiría solo los elementos con errores. (19-10-2026, decisión del autor, prompts v0.11: la salida anterior se queda en su fichero, revisada por la plataforma y con un elemento por línea; cada error dice su línea; la reparación lee solo esas líneas y corrige con Edit, que recibe solo la reparación. Vale también para las reparaciones del mockup.)
- [x] 91. Cerrar los trabajos interrumpidos al arrancar la API. **Por decidir.** Hoy quedan sin fecha de fin, y un mockup interrumpido no se puede reanudar. (19-10-2026, decisión del autor: cerrarlos sin relanzarlos. Al arrancar, `InterruptedJobs` marca como fallidas las ejecuciones a medias y cierra sus trabajos con un resultado «Interrumpido: la API se reinició…»; un mockup interrumpido se puede reanudar desde sus pasos aceptados. Solo lo creado antes del arranque.)
- [x] 92. Parar las ejecuciones que compactan varias veces antes de escribir su salida. **Por decidir.** (19-10-2026, decisión del autor: el runner detiene la ejecución en la segunda compactación si todavía no ha escrito su salida (`CompactingBeforeOutput`) y `RetryRunner` la repite una vez desde cero, como una perdida por inactividad (una sola repetición por tarea en total). Datos: compactar una vez antes de escribir pasó 4 veces y 3 terminaron bien; un árbitro real compactó 6 veces en 98 turnos y unos 1,15 M tokens sin escribir, y su repetición sí escribió.) (Corregido el 20-10-2026: contaba los avisos «compacting» y Claude Code da dos por la misma compactación, así que en la prueba real cortó el árbitro del catálogo en su primera compactación, las dos veces, y el trabajo falló. Ahora un aviso solo abre una compactación nueva cuando la anterior ha terminado.)
- [x] 93. Sugerencias de respuesta más prudentes, y marca en el catálogo de lo que sale de una suposición de la IA. En las pruebas, casi todas las sugerencias eran suposiciones y se aceptaron sin revisar, lo que infla el catálogo. (19-10-2026, decisión del autor, prompts v0.11: «Aprobar todas las sugerencias» solo aprueba las que salen de la documentación; una decisión del cliente se sugiere como «A decidir por el cliente:» con las opciones; los requisitos que citan una pregunta respondida con una suposición de la IA aceptada tal cual llevan la marca «Suposición de la IA», con filtro en la web y recuento en el informe, sin bloquear la validación.)
- [x] 94. Variedad del mockup sin `.zip`. Observación del autor (16-10-2026): sin una referencia de estilo, sale siempre con el mismo estilo y la misma organización. Relacionado con la idea aparcada de usar la skill `frontend-design` con la suscripción del autor (ver `docs/trazabilidad-proyecto.md`). (19-10-2026, decisión del autor, prompts v0.11: sin `.zip`, `StyleDirections` asigna a cada proyecto una de 8 direcciones de estilo, guardada en el plan del mockup, que el modelo adapta al sector y a los usuarios. Con `.zip` no cambia nada.)
- [x] 95. Estimar el consumo de las ejecuciones detenidas antes del final, que hoy no tienen dato. **Por decidir.** (19-10-2026, decisión del autor: estimarlo como se pueda, validando la estimación. `UsageEstimator` reconstruye el contexto turno a turno a partir de la consola guardada (cada Read con su tramo, cada Write y las compactaciones) y lo convierte en tokens con coeficientes ajustados en las 125 ejecuciones con dato real (`scripts/evaluacion/ajustar-estimador.cs`). Error del total: −0,7 % en el mockup y −3,5 % en las fases. El informe, su CSV y la web dan la estimación aparte. Junto con él, a petición del autor, las tablas de coste en tiempo y dinero (`docs/costes/`, `scripts/evaluacion/costes.cs` y `precios.json`) y el tiempo de CPU de los embeddings, guardado con cada arbitraje.)

**Hecho cuando:** el flujo completo del proyecto grande de ejemplo termina sin errores internos ni reparaciones que no caben, y el mockup sin `.zip` cambia de estilo entre proyectos distintos.

## Hito 19: exportar los entregables

Acordado con el autor el 16-10-2026. Hoy todo se queda dentro de la web.

- [x] 96. Catálogo a Word (`.docx`) y PDF, con los requisitos agrupados por tipo y sus fuentes. (20-10-2026, decisiones del autor, todas como recomendaba Claude: una portada con el estado (validado o borrador, que también se puede exportar, con «BORRADOR» en el pie), una sección por tipo en el orden de la web y, en cada requisito, su texto, notación, obligatoriedad, fuentes y preguntas por id; dos anexos con el texto de cada fragmento citado y cada pregunta citada con su respuesta, una sola vez cada uno; las marcas «Suposición de la IA» y «Discrepancia sin revisar», y sin el origen A/B ni la explicación del árbitro, que son internos. `Concilia.Core/Deliverables/Export`: `CatalogExport` arma el contenido una vez y `CatalogDocx` (DocumentFormat.OpenXml, MIT, que ya estaba) y `CatalogPdf` lo escriben. El PDF usa MigraDoc 6.2.4 (MIT), fijada en el `.csproj`: desde la 6.5.0, del 14-10-2026, PDFsharp y MigraDoc son AGPL-3.0, y el autor eligió quedarse en la última MIT. Lleva dentro IBM Plex Sans (OFL 1.1), la fuente de la web, porque el contenedor no tiene fuentes. Se descarga con `GET /projects/{id}/catalog.docx` y `.pdf`, que solo leen, y en la web con «Word» y «PDF» en la barra del catálogo, ocultos mientras hay cambios sin guardar. Los fragmentos salen del plan del trabajo que generó el catálogo y las preguntas, del proyecto. Tests: el `.docx` pasa `OpenXmlValidator` sin errores, el PDF se vuelve a leer con PdfPig y el flujo completo los descarga. Comprobado además que Word abre el `.docx` (5 páginas y 18 títulos en el panel de navegación con el catálogo de ReservaSalas). Un catálogo de 250 requisitos sale en unos 20 ms en Word y 0,3 s en PDF. Probado en Docker el mismo día con los catálogos validados de clínicas (209 requisitos: 62 páginas en PDF y 59 en Word, en menos de 1 s) y del banco de alimentos (100 requisitos, 28 páginas): la fuente va embebida, los acentos salen bien, la hora es la peninsular, el `.docx` pasa el validador y Word lo abre.)
- [ ] 97. Backlog a ficheros `.feature` de Gherkin, uno por épica, comprobados con el analizador oficial (`cucumber/gherkin`, MIT, con implementación .NET).
- [ ] 98. Backlog a CSV que se pueda importar en Jira y en Azure DevOps.

**Hecho cuando:** desde la web se descargan el catálogo y el backlog en esos formatos, y los ficheros se abren o se importan sin errores.

## Hito 20: trazabilidad visual y comparación de A, B y el árbitro

Acordado con el autor el 16-10-2026. Las dos vistas usan los mismos datos, que ya existen: fuentes, preguntas y requisitos de cada historia, y el `de` del árbitro.

- [ ] 99. Trazabilidad: desde un requisito, sus fragmentos (con el texto), las preguntas que lo justifican y las historias que lo cubren; y al revés, desde una historia.
- [ ] 100. Comparación lado a lado de A, B y el árbitro en cada trabajo: qué propuso cada uno y qué hizo el árbitro con cada elemento (unido, descartado o con discrepancia). Con el hito 17, también los emparejamientos por significado (`GET /jobs/{id}/matching`). Además, el informe de cada trabajo y el CSV del proyecto muestran, junto a las cifras del árbitro, las de los embeddings: «solo A», «solo B» y el acuerdo con el árbitro. El autor lo dejó para este paso el 19-10-2026, porque calcularlo en cada informe lo hace más lento (unos segundos por trabajo): conviene guardar el cálculo una vez.

**Hecho cuando:** desde cualquier requisito se llega a su origen en la documentación, y en un trabajo se ve qué aportó cada modelo.

## Hito 21: historial de versiones de los entregables

Acordado con el autor el 16-10-2026.

- [ ] 101. Guardar cada versión del catálogo, el backlog y el mockup (generada, editada o validada), con su fecha y su origen.
- [ ] 102. Ver una versión anterior, compararla con la actual y volver a ella.

**Hecho cuando:** después de regenerar o editar un entregable, se puede volver a la versión anterior.

## Hito 22: evaluación

Material para el capítulo de evaluación de la memoria.

- [x] 103. Terminar el experimento de si el árbitro prefiere a su propio modelo (`docs/experimento-sesgo-arbitro.md`). (16-10-2026: no hay preferencia; la unión se parece a A porque A propone más y casi todo lo de B ya está en A. LUNA, 2 arbitrajes válidos de 6 en el catálogo.)
- [ ] 104. Métricas externas sobre el catálogo y el backlog generados en inglés: AQUSA (`RELabUU/aqusa-core`) para las historias y `vale-ears` para EARS.
- [ ] 105. El flujo completo con un documento público del dataset PURE, que el tribunal pueda reproducir.
- [ ] 106. Una prueba editando de verdad el catálogo y el backlog, como un analista, para que la métrica de cambios del usuario tenga datos. Incluye los pasos 49 (segunda ronda de preguntas) y 53 (editar el catálogo), que siguen pendientes.

**Hecho cuando:** el capítulo de evaluación tiene datos de los tres experimentos (sesgo del árbitro, calidad con métricas externas y cambios del usuario) y de al menos un documento público.

## Después

Por este orden:

1. Mejorar los prompts fase a fase sobre el flujo que ya funciona. En el catálogo, se retoma el hito 7 con los prompts v2 del autor.
2. ~~Árbitro con embeddings.~~ Pasa al hito 17 (16-10-2026).
3. Aprobación por umbral y evaluación: precision, recall y F1 frente a la versión validada y a listas de referencia hechas a mano.
4. Redactar la memoria: sin capítulo de Metodología; 3.1 especificación de la solución, 3.2 implementación y 4 evaluación.
