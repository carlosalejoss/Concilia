# Plan de construcción

Plan por hitos, con una web mínima operativa desde el hito 1. Cada hito termina con algo que se ejecuta.

## Reglas

- Un paso cada vez, en orden, con un commit por paso.
- Si un paso se atasca más de 30 minutos, se anota el bloqueo y se pasa al siguiente que no dependa de él.
- No se pule la interfaz hasta que exista el árbitro.
- Decisiones abiertas que afectan al plan: ninguna del hito 17. Los umbrales del emparejamiento (0,90 y 0,70) los fijó el autor el 19-10-2026 tras calibrarlos en el paso 87, y cómo marcan la discrepancia grave, en el paso 88. La jerarquía del backlog y el modelo del árbitro se cerraron el 08-10-2026 (ver la iteración v0), y el origen de los embeddings el 16-10-2026 (hito 17).
- Desde el 08-10-2026 se trabaja en la iteración v0 (hitos 8 a 13) antes que en el hito 7.
- Desde el 16-10-2026, con los hitos 8 a 16 terminados, se sigue con los hitos 17 a 22 en su orden, acordado con el autor: 17 embeddings, 18 fiabilidad y calidad, 19 exportar, 20 trazabilidad y comparación, 21 historial y 22 evaluación. Los pasos marcados «por decidir» esperan a que el autor elija antes de empezarlos. El experimento del árbitro (paso 103) se cerró el mismo día.
- Desde el 26-10-2026 (autor): el hito 22 son las iteraciones del proyecto (pasos 109 a 116), y la evaluación pasa a ser el hito 23, el último. Los pasos 103 a 106 de la evaluación conservan su número.
- Desde el 27-10-2026 (autor): el hito 23 suma los pasos 117 a 121 (versión congelada, referencia revisada por el autor, repeticiones, Concilia frente a un solo modelo y SOL ‖ SOL).

## Dónde estamos (04-11-2026)

- **Hechos:** los hitos 17 (embeddings en local), 18 y 18b (fiabilidad y calidad, prompts v0.11 y v0.12), 19 (exportar), 20 (trazabilidad y comparación) y 21 (historial de versiones), revisados por el autor.
- **Hecho, a falta de que el autor revise sus resultados:** el hito 22, iteraciones del proyecto, diseñado con el autor el 26-10-2026.
  - Hechos los pasos 109 a 111, revisados por el autor en el navegador.
  - Hecho el 112 (preguntas centradas, prompts v0.13), revisado el 27-10-2026.
  - Hechos el 113 y el 114 (actualizar el catálogo y el backlog), en Docker y revisados por el autor en el navegador el 27-10-2026.
  - Hecho el 115 (desde cero, frente a lo anterior), en Docker y revisado por el autor en el navegador el 27-10-2026.
  - Hecho el 116 (la prueba real con clínicas en dos iteraciones), el 27-10 y el 03-11-2026: resultados en `docs/resultados-paso-116/`.
  - Hechos el 122 (sus mejoras, 03-11-2026) y el 123 (la lectura relacionada con una documentación grande, 04-11-2026: leerlo todo no cabe y la lectura relacionada funciona).
- **En curso:** el hito 23 (evaluación), con sus pasos explicados uno a uno más abajo. Hechos el 117 (la versión congelada, `docs/evaluacion/version.md`), el 105 (PURE en inglés, `docs/evaluacion/pure.md`) y el 104 (métricas externas, `docs/evaluacion/metricas-externas.md`), los tres el 04-11-2026; el siguiente es el 106.

### Lo que queda, en orden

Lista única de todo lo pendiente, para no dejarse nada. Cada paso del plan tiene el detalle en su hito.

**Hito 22, iteraciones del proyecto:**
1. ~~**Revisar en el navegador los pasos 113 y 114**~~ (hecho el 27-10-2026).
2. ~~**115,** desde cero frente a lo anterior~~ (hecho y revisado por el autor el 27-10-2026).
3. ~~**116,** la prueba real con clínicas en dos iteraciones~~ (hecha el 27-10 y el 03-11-2026; costó unos 16 $ con los intentos fallidos). El autor revisa la lista de cambios de la segunda iteración en el paso 118. Sus propuestas de mejora, decididas: las hechas, en el 122; las demás, en «Después», punto 5.
4. ~~**122,** las mejoras de la prueba real~~ (03-11-2026).
5. ~~**123,** la lectura relacionada con una documentación grande~~ (04-11-2026).

**Hito 23, evaluación** (en este orden; cada paso dice qué hace el autor y qué hace Claude):

6. ~~117, congelar la versión, con una prueba barata en inglés antes~~ (04-11-2026, etiqueta `evaluacion-1` en `6d34025`).
7. ~~105, el flujo con un documento de PURE, en inglés~~ (04-11-2026, 9,29 $).
8. ~~104, AQUSA y `vale-ears` sobre lo del 105~~ (04-11-2026).
9. 106, la prueba como analista con ReservaSalas (cierra los pasos 49 y 53).
10. 118, la referencia: revisar las trampas y hacer la lista de funcionalidades.
11. 119, las 9 ejecuciones de clínicas.
12. 120, las hojas de revisión y la comparación con un solo modelo.
13. 121, SOL ‖ SOL.

**Después del plan:**

14. Redactar la memoria. Es de tipo B: unas 20 páginas, resumen en castellano e inglés y el anexo VII sobre el uso de la IA. Está en `memoria/tfg/memoria.tex`, fuera de git, y se compila con MiKTeX (instalado el 22-10-2026) y `memoria/tfg/compilar.ps1`. Estructura del autor (22-10-2026): 1 Introducción; 2 Desarrollo (2.1 especificación del sistema, 2.2 construcción del sistema, 2.3 validación de la solución); 3 Conclusiones. **Empezada el 04-11-2026:** la introducción, redactada.
15. Lo que queda en «Después», al final de este plan: el hito 7 (catálogo v2, aplazado), la aprobación por umbral y las mejoras futuras de las iteraciones (punto 5).

**Abierto, sin paso todavía** (en `docs/trazabilidad-proyecto.md`, «Decisiones que siguen abiertas»):
- ~~si se hace un CSV solo con lo nuevo de una iteración~~ (cerrado el 04-11-2026 sin hacerlo, como mejora futura);
- abaratar las pantallas del mockup;
- la licencia del repositorio, por acordar con la tutora y con la empresa (README);
- el mockup con la suscripción y la skill `frontend-design`, aparcado desde el 15-10-2026.

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

**Hecho cuando:** el flujo completo del proyecto grande de ejemplo termina sin errores internos ni reparaciones que no caben, y el mockup sin `.zip` cambia de estilo entre proyectos distintos. (Cumplido el 22-10-2026, prompts v0.11: en «Clinicas Veterinarias v0.11» terminan preguntas, catálogo, backlog y mockup, y las reparaciones de LUNA y del árbitro caben y corrigen con Edit. El primer catálogo falló por un error del paso 92, corregido el 20-10-2026. Los mockups de clínicas («cálida y cercana») y del banco de alimentos («institucional y sobria») salen a la primera y con estilos distintos.)

## Hito 18b: el árbitro tras la prueba real

Acordado con el autor el 22-10-2026, tras la prueba real del hito 18 (prompts v0.11). Recoge las dos mejoras que dejó: el árbitro del catálogo consulta casi toda la documentación y el del backlog se queda con un criterio por historia. Prompts v0.12. Se miden con un nuevo arbitraje de los trabajos de «Clinicas Veterinarias v0.11», sobre las mismas propuestas.

- [x] 107. El árbitro del catálogo consulta solo las fuentes que necesita. (22-10-2026, decisión del autor, como recomendaba Claude: en la prueba leyó 34 tramos de la documentación, 1.242 líneas, compactó y tuvo que repetirse, aunque el prompt le dice que no la lea entera. No sabe qué fragmentos le hacen falta. La plataforma le da `input/fuentes.txt` en lugar de `input/documentacion.txt`: los fragmentos que citan los requisitos sin par «mismo» en el emparejamiento y los de los pares «mismo» con cifras distintas, porque los embeddings ven iguales «1 hora» y «24 horas» (`SourcesFile`). Mismo formato que la documentación, con la cabecera de cada documento. El árbitro de las preguntas sigue consultando la documentación.)
- [x] 108. El árbitro del backlog conserva los criterios y las tareas citándolos por su id. (22-10-2026, decisión del autor, como recomendaba Claude: el contexto ya estaba lleno al terminar de leer y el backlog entero tenía que caber en una respuesta de 64.000 tokens, así que dejó uno por historia. En las propuestas que lee, los criterios y las tareas llevan el número de su propuesta (`1:CA-012`). El árbitro cita por su id los que conserva tal cual y escribe enteros solo los que combina o redacta; el prompt le pide conservar todos salvo los repetidos. La plataforma pone el texto de los citados, numera todos de nuevo y avisa de cuántos se citan y cuántos de las historias unidas no (`CriteriaReferences`, aviso `ARB-CITA`); un id que no está en su propuesta es un error `ARB-CITA` y pide reparación. El formato final del backlog no cambia.)

**Hecho cuando:** con las mismas propuestas, el árbitro del catálogo lee mucho menos de la documentación sin empeorar la unión, y el del backlog conserva los criterios de las historias que une. (Cumplido el 22-10-2026 con dos nuevos arbitrajes de «Clinicas Veterinarias v0.11» con SOL: el del catálogo consultó 298 líneas en lugar de 1.242, a la primera y por 1,43 $ en lugar de 3,20 $, con una unión más atómica, 277 requisitos, y 44 avisos de la plataforma, casi todos falsos; el del backlog conservó 239 criterios en lugar de 121, por 1,23 $ en lugar de 1,05 $. Detalle en `docs/evolucion-prompt-requisitos.md`, v0.12.)

## Hito 19: exportar los entregables

Acordado con el autor el 16-10-2026. Hoy todo se queda dentro de la web.

- [x] 96. Catálogo a Word (`.docx`) y PDF, con los requisitos agrupados por tipo y sus fuentes. (20-10-2026, decisiones del autor, todas como recomendaba Claude: una portada con el estado (validado o borrador, que también se puede exportar, con «BORRADOR» en el pie), una sección por tipo en el orden de la web y, en cada requisito, su texto, notación, obligatoriedad, fuentes y preguntas por id; dos anexos con el texto de cada fragmento citado y cada pregunta citada con su respuesta, una sola vez cada uno; las marcas «Suposición de la IA» y «Discrepancia sin revisar», y sin el origen A/B ni la explicación del árbitro, que son internos. `Concilia.Core/Deliverables/Export`: `CatalogExport` arma el contenido una vez y `CatalogDocx` (DocumentFormat.OpenXml, MIT, que ya estaba) y `CatalogPdf` lo escriben. El PDF usa MigraDoc 6.2.4 (MIT), fijada en el `.csproj`: desde la 6.5.0, del 14-10-2026, PDFsharp y MigraDoc son AGPL-3.0, y el autor eligió quedarse en la última MIT. Lleva dentro IBM Plex Sans (OFL 1.1), la fuente de la web, porque el contenedor no tiene fuentes. Se descarga con `GET /projects/{id}/catalog.docx` y `.pdf`, que solo leen, y en la web con «Word» y «PDF» en la barra del catálogo, ocultos mientras hay cambios sin guardar. Los fragmentos salen del plan del trabajo que generó el catálogo y las preguntas, del proyecto. Tests: el `.docx` pasa `OpenXmlValidator` sin errores, el PDF se vuelve a leer con PdfPig y el flujo completo los descarga. Comprobado además que Word abre el `.docx` (5 páginas y 18 títulos en el panel de navegación con el catálogo de ReservaSalas). Un catálogo de 250 requisitos sale en unos 20 ms en Word y 0,3 s en PDF. Probado en Docker el mismo día con los catálogos validados de clínicas (209 requisitos: 62 páginas en PDF y 59 en Word, en menos de 1 s) y del banco de alimentos (100 requisitos, 28 páginas): la fuente va embebida, los acentos salen bien, la hora es la peninsular, el `.docx` pasa el validador y Word lo abre.)
- [x] 97. Backlog a ficheros `.feature` de Gherkin, uno por épica, comprobados con el analizador oficial (`cucumber/gherkin`, MIT, con implementación .NET). (22-10-2026, decisiones del autor: palabras clave en inglés, el idioma que Gherkin supone si el fichero no dice otro, con el texto tal cual; todos los ficheros en un `.zip`. Cada épica es una `Feature`, cada historia una `Rule` (la palabra de Gherkin 6 para una regla con sus ejemplos), con su «Como… quiero… para…» y sus tareas en la descripción, y cada criterio un `Scenario` con `Given`/`When`/`Then`; una segunda línea de un paso va con `And`. Los ids van como etiquetas (`@EP-01`, `@HU-001 @RF-001`, `@CA-001`) y una historia con una discrepancia sin revisar lleva `@discrepancia-sin-revisar`. La primera línea de cada fichero dice si el backlog está validado o es un borrador. `BacklogExport` arma el contenido una vez (también servirá al paso 98) y `BacklogGherkin` escribe y comprueba cada fichero con `Gherkin` 42.0.1 antes de entregarlo. Al probar el analizador se vio que nunca rechaza una línea de descripción, pero algunas cambian el sentido sin avisar: `Scenario: …`, `Rule: …` o `Background: …` abren otro elemento, `@…` se vuelve etiqueta y `# …` desaparece como comentario. Esas líneas llevan «- » delante. `GET /projects/{id}/backlog-gherkin.zip` y «Gherkin» en la barra del backlog. Tests con el analizador y en el flujo completo.)
- [x] 98. Backlog a CSV que se pueda importar en Jira y en Azure DevOps. (22-10-2026, decisiones del autor: un CSV por herramienta, porque cada una espera otras columnas, y en Azure DevOps el proceso Agile. `BacklogCsv`, con el mismo `BacklogExport` del paso 97. Los dos ficheros van separados por comas, con todos los campos entre comillas, en UTF-8 sin BOM (el BOM se pegaría al nombre de la primera columna), y tienen una fila por épica, historia y tarea, cada una detrás de su padre. **Jira:** la jerarquía con `Issue ID` y `Parent ID` (`Epic` → `Story` → `Sub-task`); `Epic Name` en las épicas, que algunos proyectos gestionados por la empresa todavía exigen; la descripción de cada historia con su «Como… quiero… para…», los criterios en Gherkin y los requisitos; y los ids de Concilia en columnas `Labels` repetidas, una por valor. **Azure DevOps:** `ID` vacío (elementos nuevos) y el título en `Title 1`, `Title 2` o `Title 3` según el nivel (`Epic` → `User Story` → `Task`); `Description` y `Acceptance Criteria` en HTML, escapando solo `&`, `<` y `>` para que los acentos se lean; y los ids en `Tags`. Una historia con una discrepancia sin revisar lleva también `discrepancia-sin-revisar`. `GET /projects/{id}/backlog-jira.csv` y `backlog-azure-devops.csv`, y «CSV Jira» y «CSV Azure DevOps» en la barra del backlog. Tests leyendo los CSV con un lector estándar (`TextFieldParser`) y en el flujo completo. **Sin probar en real:** la importación en Azure DevOps; el autor da el paso por cerrado sin ella (23-10-2026). (23-10-2026: el autor importó el CSV de Jira del backlog de clínicas v0.11 en un Jira Cloud real, en un espacio gestionado por el equipo, con el importador para quien no es administrador: épicas, historias y subtareas con su jerarquía, acentos, descripción y etiquetas correctos y ningún error. Ese importador admite 250 elementos por fichero: creó 251 de 255 y las 4 subtareas que faltaban se importaron en un segundo CSV. Queda como mejora posible, sin decidir, que Concilia reparta el CSV en ficheros de 250 elementos como máximo, por épicas completas.) El 22-10-2026 el autor reconstruyó la API y la web y comprobó en el navegador que los tres enlaces del backlog aparecen y descargan.)

**Hecho cuando:** desde la web se descargan el catálogo y el backlog en esos formatos, y los ficheros se abren o se importan sin errores.

## Hito 20: trazabilidad visual y comparación de A, B y el árbitro

Acordado con el autor el 16-10-2026. Las dos vistas usan los mismos datos, que ya existen: fuentes, preguntas y requisitos de cada historia, y el `de` del árbitro.

- [x] 99. Trazabilidad: desde un requisito, sus fragmentos (con el texto), las preguntas que lo justifican y las historias que lo cubren; y al revés, desde una historia. (22-10-2026, el autor deja las decisiones a Claude: un panel lateral navegable, del catálogo y el backlog guardados, y sin vista de matriz de todo el catálogo por ahora. `Concilia.Core/Deliverables/Traceability` se construye con el mismo contenido que las exportaciones (`CatalogExport` y `BacklogExport`), así que el panel y el Word dicen lo mismo: cada requisito con sus fuentes, preguntas e historias; cada historia con su épica y sus requisitos; el texto de cada fragmento citado y cada pregunta con su respuesta una sola vez; y lo que se cita y no existe (un fragmento que no está en la documentación de la que salió el catálogo, `missingQuestions` y `missingRequirements`). `GET /projects/{id}/traceability`, que solo lee. En la web, el botón «Trazabilidad» de cada requisito y cada historia abre `TracePanel`: desde un requisito se pasa a una historia que lo cubre y de ahí a otro de sus requisitos, con «Volver», sin salir del paso. Desde una historia, los fragmentos y preguntas de sus requisitos salen una vez cada uno, con los requisitos que los citan. El botón se oculta mientras hay cambios sin guardar, como las descargas, porque el panel muestra lo guardado. Sin librerías nuevas. Tests de `Traceability` y en el flujo completo.)
- [x] 100. Comparación lado a lado de A, B y el árbitro en cada trabajo: qué propuso cada uno y qué hizo el árbitro con cada elemento (unido, descartado o con discrepancia). Con el hito 17, también los emparejamientos por significado (`GET /jobs/{id}/matching`). Además, el informe de cada trabajo y el CSV del proyecto muestran, junto a las cifras del árbitro, las de los embeddings: «solo A», «solo B» y el acuerdo con el árbitro. El autor lo dejó para este paso el 19-10-2026, porque calcularlo en cada informe lo hace más lento (unos segundos por trabajo): conviene guardar el cálculo una vez. (22-10-2026, decisiones del autor, todas como recomendaba Claude. **Guardar el cálculo una vez:** `ProposalMatcher` se divide en la parte lenta, `Similarities` (con embeddings: cada elemento con el más parecido de la otra propuesta y su similitud), y la rápida, `Classify` (umbrales, recuentos, pares y acuerdo con el árbitro). La lenta se guarda en `data/jobs/{id}/emparejamiento.json` (`MatchingCache`), sin umbrales ni árbitro, con el nombre del modelo de embeddings y una versión: si cambian, se recalcula. Los trabajos nuevos la guardan al arbitrar, donde ya se calculaba; los antiguos, la primera vez que se pide. `GET /jobs/{id}/matching` la usa también, con cualquier umbral, y dice si venía guardada (`fromCache`). **Informe y CSV:** `embeddings` en el informe (solo A, solo B y el acuerdo con el árbitro), una línea más en la web, y tres columnas al final del CSV: `emb_solo_A`, `emb_solo_B` y `emb_acuerdo_arbitro_pct`. **Comparación:** `GET /jobs/{id}/comparison` (`JobComparison`): una fila por elemento de la unión con lo que juntó de A y de B, una por descartado con su motivo y una por lo que el árbitro no clasificó; cada elemento de A y B con su par más parecido en la otra, su nivel y si el árbitro lo juntó, y la fila marcada si el árbitro no coincide con los embeddings. En la web, «Comparación de A, B y el árbitro» bajo el informe de cada trabajo con árbitro: tres columnas A | árbitro | B, filtros y búsqueda, de 50 en 50; se carga al abrirla. Sin librerías nuevas. Tests de la caché (mismo resultado que calcularlo de nuevo, otros umbrales sin embeddings, otro modelo o fichero dañado), de `JobComparison` y en el flujo completo.)

**Hecho cuando:** desde cualquier requisito se llega a su origen en la documentación, y en un trabajo se ve qué aportó cada modelo. (Cumplido: revisado en el navegador por Claude el 22-10-2026, con Edge sin ventana, y por el autor el 23-10-2026, que comprobó además que «Trazabilidad» se oculta con cambios sin guardar.)

## Hito 21: historial de versiones de los entregables

Acordado con el autor el 16-10-2026.

- [x] 101. Guardar cada versión del catálogo, el backlog y el mockup (generada, editada o validada), con su fecha y su origen. (23-10-2026, decisiones del autor, todas como recomendaba Claude. **Dónde:** una tabla nueva, `DeliverableVersions` (migración `AddDeliverableVersions`): proyecto, tipo, número (1, 2, 3… por tipo y proyecto), origen (`Generated`, `Edited`, `Validated` y `Existing`), el trabajo que generó el contenido, el estado, el contenido entero y la fecha, y el número de requisitos, historias o pantallas, contado al guardarla para que la lista no lea los contenidos. Se borra con el proyecto. Del mockup se guarda su resumen y su página se copia en `data/projects/{id}/mockup/versiones/{n}.html`, porque `mockup.html` se sobrescribe. El trabajo de cada versión hace falta para volver a ella en el paso 102: las comprobaciones de un borrador usan el plan de su trabajo. **Cuándo:** al generar (catálogo, backlog y mockup), al guardar cambios y al validar; un guardado que deja el entregable como su última versión (mismo contenido, estado y trabajo) no guarda nada, y reabrir tampoco, porque el contenido no cambia (`DeliverableHistory`). Se guardan todas, sin límite. Al arrancar, la API da su versión 1 (`Existing`, con la fecha de su último cambio) a cada entregable que aún no tiene ninguna, como `InterruptedJobs`. La copia de trabajo (`Deliverables`) no cambia: las exportaciones, la trazabilidad, la comparación y el informe la siguen leyendo. **Endpoint:** `GET /projects/{id}/deliverables/{kind}/versions`, de la más reciente a la más antigua, sin el contenido y con `current` en la que tiene ahora. **Web:** «Historial de versiones», plegable, bajo la barra del catálogo y del backlog y bajo el mockup, solo para leer. Sin librerías nuevas. Tests de `DeliverableHistory`, de las versiones al arrancar y en el flujo completo.)
- [x] 102. Ver una versión anterior, compararla con la actual y volver a ella. (23-10-2026, decisiones del autor como recomendaba Claude. **Ver:** `GET /projects/{id}/deliverables/{kind}/versions/{n}`, con su contenido, y la página de una versión del mockup en `GET /projects/{id}/mockup/versions/{n}`, con la misma política de seguridad que el mockup actual. **Comparar:** `GET …/versions/{n}/comparison`, de la versión a lo actual, con el emparejamiento por contenido del informe de cambios del usuario (`JobReports.Compare`, del que ahora sale `Diff`): iguales (contados), modificados (mismo id y otro contenido, como eran y como son), añadidos y borrados. Solo el catálogo y el backlog; el mockup se abre. **Volver:** `POST …/versions/{n}/restore` (`DeliverableRules.Restore`): el entregable recupera el contenido y el trabajo de esa versión, siempre en borrador, y es una versión nueva (`Restored`, con `RestoredFrom`), así que la que había queda en el historial y se puede deshacer. Los pasos siguientes quedan desactualizados, como al generar. La versión actual se rechaza. Del mockup se recupera también su página. **Desactualizada:** una versión guarda si estaba desactualizada (migración `AddVersionRestore`, columnas `Outdated` y `RestoredFrom`); cuando un cambio del flujo desactualiza un entregable, desactualiza también todas sus versiones, porque son de antes del cambio, y al volver a una vuelve su aviso. La migración marca así las que ya había. **Web:** en «Historial de versiones», cada versión que no es la actual tiene «Ver» y «Comparar con la actual» (en un panel lateral) y «Volver a esta versión», con una confirmación que explica que la actual queda en el historial; del mockup, «Ver» abre su página aparte. Con cambios sin guardar no se puede volver. Sin librerías nuevas. Tests de `Restore`, de las versiones desactualizadas, de la comparación y en el flujo completo.)

**Hecho cuando:** después de regenerar o editar un entregable, se puede volver a la versión anterior. (Cumplido el 23-10-2026 en los tests, incluido el flujo completo: se regenera el catálogo validado y se vuelve a él. Revisado en el navegador por el autor el 26-10-2026, con la API reconstruida en Docker: funciona todo. Hito 21 cerrado.)

## Hito 22: iteraciones del proyecto

Pedido por el autor el 26-10-2026. Hoy, si a un proyecto ya analizado le llega documentación nueva, el catálogo, el backlog y el mockup quedan desactualizados y la única salida es generarlo todo de nuevo: se pierde lo que el usuario editó y validó, y se paga otra vez todo el análisis. Con las iteraciones, la documentación se agrupa en tandas, y en preguntas, catálogo y backlog se elige entre rehacerlo con todas o trabajar solo sobre las que se marquen, conservando lo demás.

En la documentación se habla de **iteraciones del proyecto**, para no confundirlas con la «iteración v0» del desarrollo y de los prompts. En la web son «Iteración 1», «Iteración 2»…, y en el código, `Iteration`.

### Decisiones (26-10-2026)

| Decisión | Origen |
|---|---|
| La documentación se agrupa en iteraciones, con número, nombre editable y notas opcionales («Reunión del 15-11»). Cada una está abierta o cerrada. Solo hay una abierta a la vez y solo se sube a la abierta; se cierra con «Cerrar documentación». Un proyecto nuevo empieza con la «Iteración 1» abierta. | Autor; nombre, notas y la primera abierta, propuesta de Claude |
| Mientras haya una iteración abierta no se lanzan rondas de preguntas, sugerencias, catálogo ni backlog, para no trabajar con la mitad de la documentación. Sí se edita, se valida y se genera el mockup, que no lee la documentación. | Autor (el mockup y las sugerencias, propuesta de Claude) |
| Solo se puede reabrir la última iteración, y reabrirla desactualiza lo mismo que subir un archivo. | Autor |
| El `.zip` de estilo del mockup es del proyecto, no de una iteración. | Propuesta de Claude |
| Los proyectos que ya existen pasan a tener una «Iteración 1», cerrada, con todos sus archivos, preguntas, requisitos e historias: se comportan como hoy. | Propuesta de Claude, aprobada por el autor |
| Los documentos siguen numerándose por orden de subida en todo el proyecto (los de la iteración 2 siguen a los de la 1: `DOC-08`, `DOC-09`…) y no se pueden borrar, así que lo que ya cita `DOC-03#F010` sigue valiendo. La iteración de un fragmento se deduce de su documento; el formato de los ids no cambia. | Claude (lo que ya hace la plataforma) |
| **Ids estables** en el catálogo y el backlog. Al generar desde cero se numeran seguidos, como ahora. Después, borrar deja un hueco, lo nuevo toma el siguiente número de su tipo (o el siguiente `EP`, `HU`, `CA` o `T`) y cambiar el tipo de un requisito le da un id nuevo de ese tipo. `ID-SEQ` deja de aplicarse al borrador, pero sigue en lo que escriben los modelos, y la corrección `TIPO` no cambia. Sustituye a «la plataforma renumera tras cada edición» (pasos 51 y 55): sin ids estables no se puede decir «RF-012, modificado en la iteración 2» ni saber qué historias afecta un cambio. | Autor (propuesta de Claude) |
| **La iteración de cada elemento la calcula la plataforma**, no el modelo: la de una pregunta o un requisito, por sus fuentes (y las preguntas que cita); la de una historia, por sus requisitos. Sale siempre bien, no cuesta tokens y vale también para lo que se añade a mano. Aparte, cada elemento guarda su historial: en qué iteración se creó, se modificó o se eliminó, con el motivo. | Propuesta de Claude, aprobada por el autor |
| **Dos modos** en preguntas, catálogo y backlog. «Todas las iteraciones» (preguntas) y «Desde cero» (catálogo y backlog) son lo de hoy: el mismo prompt v0.12, las mismas entradas y el mismo árbitro. «Centrarse en…» (preguntas) y «Actualizar con…» (catálogo y backlog) trabajan sobre las iteraciones que se marquen, teniendo en cuenta todo el contexto. | Autor |
| **Qué se lee en los modos nuevos: la lectura relacionada (en el diseño, opción C).** Es obligatorio leer la documentación de las iteraciones elegidas y los fragmentos anteriores relacionados con ella, que elige la plataforma con embeddings (desde «parecido», 0,70, con un tope, **por decidir**). El resto de la documentación se puede consultar por tramos. Además, el modelo recibe lo que ya se sabe: las preguntas con su respuesta y, si existe, el catálogo validado. Descartadas: la lectura completa (A en el diseño: leerlo todo, como ahora), porque cada ronda centrada costaría lo mismo que una completa, más en cada iteración, y con más compactaciones; y «solo lo nuevo» (B en el diseño: lo nuevo, con lo anterior opcional), porque puede pasar por alto contradicciones con detalles antiguos y lo que consulta el modelo es impredecible. Es la idea que funcionó en el paso 107 (`fuentes.txt`). En la prueba real se mide frente a la lectura completa. Los nombres «lectura relacionada», «lectura completa» y «solo lo nuevo» los puso el autor el 04-11-2026, en lugar de las letras del diseño. | Autor (opción C de Claude) |
| La iteración no va en las cabeceras `=== DOC-01 · nombre ===` de la documentación, que leen `FragmentAtLine`, `SourcesFile` y `CatalogExport`: en los modos nuevos va en un bloque aparte. Así los modos de siempre reciben exactamente las mismas entradas. | Claude |
| **Preguntas.** Cada ronda guarda en qué iteraciones se centró, y cada pregunta muestra las iteraciones de sus fuentes, con filtro. Las preguntas centradas se fijan sobre todo en lo nuevo, pero también se muestran las de otras iteraciones. En el modo centrado, el modelo puede proponer, además de preguntas nuevas, **revisiones** de las anteriores: reformular una pendiente (se guarda el texto anterior, la iteración y el motivo) o marcarla como resuelta u obsoleta por la nueva documentación. «Resuelta» y «obsoleta» son propuestas: nunca se cierra una pregunta sola, la responde o la descarta el usuario. | Autor; las revisiones, propuesta de Claude |
| Una respuesta nunca se reescribe. Si la nueva documentación la contradice, se crea una **pregunta nueva enlazada** («La respuesta a P-003 decía X, pero DOC-09 dice Y: ¿cuál vale?»). Si hay catálogo validado, el modelo lo recibe y pregunta también por las contradicciones entre la nueva documentación y los requisitos validados, con la pregunta enlazada al requisito. | Autor; las del catálogo, propuesta de Claude |
| **Cuándo se puede actualizar.** El catálogo: con el catálogo validado, al menos una ronda de preguntas terminada después de cerrar las iteraciones elegidas y que las cubra (centrada en ellas o con todas), y ninguna pregunta pendiente. El backlog: con el backlog validado y el catálogo validado con esas iteraciones. | Autor (propuesta de Claude) |
| **La actualización es un conjunto de cambios.** A y B escriben los requisitos nuevos, los modificados (id, contenido nuevo y motivo) y los eliminados (id y motivo). El árbitro une los cambios de los dos: modificaciones y eliminaciones se alinean por id sin IA (si A modifica RF-012 y B lo elimina, es una discrepancia directa); lo nuevo se empareja con embeddings entre A y B y **también contra el catálogo existente**, para avisar de un requisito «nuevo» que ya existía. La plataforma aplica los cambios sobre la copia de trabajo, así que las ediciones del usuario se conservan. | Propuesta de Claude, aprobada por el autor |
| **Lo que modifica o elimina la IA es una propuesta.** Queda marcado como las discrepancias, con lo que había antes y el motivo, y no se valida sin revisarlo: aceptar una eliminación quita el elemento, rechazarla lo deja; rechazar una modificación devuelve el contenido anterior. | Autor |
| **Backlog: análisis de impacto sin IA.** La plataforma calcula las historias que citan requisitos modificados o eliminados y los requisitos nuevos que aún no tienen historia, y el modelo solo trabaja sobre eso: historias nuevas, modificadas (criterios y tareas añadidos o quitados) y eliminadas, con motivo e historial, como en el catálogo. | Propuesta de Claude, aprobada por el autor |
| **Cobertura por entregable.** Cada catálogo, backlog y versión guarda qué iteraciones incluye («Catálogo: iteraciones 1–2»). La web dice qué iteración falta en cada uno y la marca por defecto al actualizar. | Propuesta de Claude |
| **Desde cero, frente a lo anterior.** Tras generar desde cero, la plataforma empareja con embeddings el nuevo catálogo o backlog con la versión anterior y marca cada elemento como igual, cambiado o nuevo, y lo que ha desaparecido. | Propuesta de Claude |
| Las exportaciones llevan la iteración: en Word y PDF, junto a cada requisito; en Jira y Azure DevOps, como etiqueta (`iteracion-2`). Un CSV solo con lo nuevo de una iteración, para no duplicar lo ya importado, queda como mejora posible, sin decidir. | Propuesta de Claude |
| El mockup no cambia: se genera siempre con toda la información, para que no quede fuera ninguna funcionalidad. | Autor |
| **Lo que ya funciona sigue igual:** trazabilidad, discrepancias dentro de una misma iteración (sigue pudiendo haber información contradictoria), emparejamiento y árbitro con embeddings, correcciones automáticas, reparación editando, comparación A \| árbitro \| B, nuevo arbitraje, informe y su CSV (columnas nuevas al final, como en el paso 100), historial de versiones, exportaciones y mockup. Se comprueba con todos los tests actuales y con un test que asegura que los modos de siempre generan exactamente el mismo prompt y los mismos ficheros de entrada que la v0.12. Antes de aplicar las migraciones en Docker se hace una copia de la base de datos. | Autor (requisito); cómo comprobarlo, propuesta de Claude |
| El hito va antes de la evaluación, que pasa a ser el hito 23: la comparación entre actualizar y empezar de cero es un experimento más para la memoria. | Autor |

### Pasos

- [x] 109. Iteraciones en Documentos. (26-10-2026, diseño aprobado por el autor; los tres huecos del plan, como proponía Claude.)
  - Tabla `Iterations` (proyecto, número, nombre, notas, estado `Open`/`Closed`, fechas de creación y cierre; migración `AddIterations`, que solo añade) y `ProjectFile.IterationId`, nulo en el `.zip` de estilo. Un índice único filtrado asegura una sola abierta por proyecto. Las reglas están en `IterationRules` (Core).
  - **La «Iteración 1» de los proyectos que ya existen se crea al arrancar la API** (`IterationEndpoints.CreateFirstForExistingAsync`, como `RecordExistingAsync`), no en la migración: así se prueba con los tests, que usan la base de datos en memoria y no ejecutan migraciones; la migración se queda en esquema; y solo toca los proyectos sin iteraciones creados antes del arranque. Cerrada, con todos sus documentos y la fecha de su última subida. **Cambio frente al plan (autor):** un proyecto que ya existe sin documentos la recibe abierta, como uno nuevo, para no tener que subir su primer documento a una «Iteración 2».
  - Endpoints (grupo «Documentos»): `GET` y `POST /projects/{id}/iterations`, `PUT …/{n}` (nombre y notas), `POST …/{n}/close` (exige al menos un documento), `POST …/{n}/reopen` (solo la última; `FileAdded`) y **`DELETE …/{n}`** (añadido, autor: la última, abierta, sin documentos y nunca la 1, para una iteración creada por error). Un documento va a la iteración abierta y, si no hay, se rechaza; el `.zip` se sube siempre. `POST /projects` crea la «Iteración 1» abierta.
  - Flujo: `FlowFacts.OpenIteration`; con una iteración abierta y algún documento, rondas, catálogo y backlog dan «Cierra la documentación de la iteración N.» (`ProjectFlow.DocumentationOpen`), y las sugerencias también (`FlowView.documentationOpen`). Sin documentos, los pasos dicen lo mismo que antes. El mockup no cambia. El catálogo v1.4 experimental y el nuevo arbitraje no se bloquean (autor).
  - Web: Documentos agrupados por iteración, de la más reciente a la más antigua, con el nombre y las notas editables, «Cerrar documentación», «Reabrir» (con confirmación), «Eliminar» (la vacía) y «Nueva iteración»; los `.zip`, aparte. Las sugerencias se apagan con el motivo.
  - Tests: las reglas (`IterationRulesTests`) y el flujo (`ProjectFlowTests`); los endpoints, reabrir que desactualiza y las sugerencias bloqueadas (`IterationTests`); el arranque sobre un proyecto ya hecho, que da los mismos documentos (`DOC-01`…, mismos nombres y texto) y el mismo flujo que antes; y el flujo completo con el clic de cerrar (los tres tests de `FullFlowTests` que lanzan una ronda). 966 tests en verde (951 + 15).
- [x] 110. Ids estables en el catálogo y el backlog. (26-10-2026.)
  - El borrador ya no se renumera al guardar (`CatalogDraft.Normalize` y `BacklogDraft.Normalize`, con `StableIds`): cada elemento conserva el id que tenía en el borrador que se sustituye; lo nuevo (o una copia, o un id escrito a mano) toma el siguiente número de su prefijo, borrar deja hueco y cambiar el tipo de un requisito le da un id nuevo de ese tipo. Al generar desde cero se sigue numerando seguido (`Renumber`, también en la corrección `TIPO`, que no cambia).
  - **Un número ya dado no se vuelve a dar** (detalle de Claude, para que «RF-012» sea siempre el mismo requisito): cuentan todos los ids de las versiones del historial con el mismo trabajo (`DeliverableVersions.IdsOfJobAsync`); tras borrar RF-010 y guardar, el siguiente requisito nuevo es RF-011. Una generación nueva vuelve a empezar en 1.
  - `ID-SEQ` solo en las salidas de los modelos: el validador recibe `GapsInIds` en las comprobaciones del borrador. Los ids siguen siendo únicos y, en el catálogo, con el prefijo de su tipo. El validador de las salidas de los modelos no cambia.
  - Web: los textos que decían que los ids se renumeran al guardar.
  - Tests: los dos de la renumeración pasan a comprobar los ids estables; `StableIdsTests` (asignación, números del historial, generar desde cero, la referencia de una historia que sobrevive al borrar otro requisito, la trazabilidad y las exportaciones con huecos); un borrador con huecos se valida y la misma salida de un modelo daría `ID-SEQ`; los ids del historial son los del mismo trabajo. 974 tests en verde.
- [x] 111. La iteración de cada elemento y la cobertura de cada entregable. (26-10-2026; revisado por el autor en el navegador el mismo día, con la migración aplicada en Docker tras su copia de la base de datos.)
  - La plataforma calcula la iteración de cada pregunta, requisito e historia (`ElementIterations`, Core): un fragmento es de la iteración de su documento (`ProjectIterations.DocumentsAsync`, con la numeración de `LoadDocumentsAsync`), una pregunta de las de sus fuentes, un requisito de las de sus fuentes y sus preguntas y una historia de las de sus requisitos. Como un elemento puede apoyarse en documentación de varias iteraciones, cada uno tiene una **lista** (detalle de Claude). `GET /projects/{id}/element-iterations` da la de cada documento y cada pregunta, y la web calcula con ellas las de los requisitos y las historias, también mientras se editan.
  - Web: «It. 1, 2» en cada pregunta, requisito e historia y un filtro por iteración en las tres fases, y la iteración de cada requisito, historia y fragmento en el panel de trazabilidad. **Solo cuando el proyecto tiene más de una iteración** (detalle de Claude): un proyecto de una sola se ve como antes.
  - Cada entregable y cada versión guardan las iteraciones que incluyen (`Iterations`, migración `AddDeliverableIterations`, que solo añade): el catálogo y el backlog, las de los documentos del plan de su trabajo; el mockup, las de su backlog. Volver a una versión recupera las suyas. Los que ya existían las reciben al arrancar la API (`ProjectIterations.RecordExistingCoverageAsync`). La vista del entregable lleva `iterations` y `missingIterations` (las cerradas con documentos que no incluye; la abierta no cuenta, porque con ella no se puede generar), y la web dice qué incluye (con más de una) y qué le falta; el historial de versiones, lo que incluye cada una en cuanto alguna pasa de la primera iteración.
  - Exportaciones: «Iteración: N» o «Iteraciones: 1, 2» junto a las fuentes y preguntas de cada requisito en Word y PDF, y la etiqueta `iteracion-N` en Jira y Azure DevOps (una épica, las de sus historias; una tarea, las de su historia). Gherkin no cambia.
  - Tests: `ElementIterationsTests` (el cálculo, Word y PDF, Jira y Azure DevOps y la trazabilidad); en `IterationTests`, el endpoint, lo que incluye y le falta a un entregable y la cobertura de lo que ya existía al arrancar; en el flujo completo, el catálogo generado y sus versiones incluyen la «Iteración 1». Ningún test anterior cambia. 983 tests en verde.
  - Revisión del autor en el navegador antes de los prompts nuevos.
- [x] 112. Preguntas centradas en unas iteraciones (prompts v0.13). (26-10-2026; con la migración aplicada en Docker tras la copia de la base de datos, revisado por el autor en el navegador el 27-10-2026 con un proyecto de prueba. La prueba con modelos es la del paso 116.)
  - Antes que nada, el test de que «Todas las iteraciones» y «Desde cero» generan exactamente el mismo prompt y las mismas entradas que la v0.12. (Hecho el 26-10-2026: `V012SnapshotTests`, con la instantánea en `tests/fixtures/v0.12-snapshot/`.)
  - Los prompts del modo centrado los redacta Claude y los revisa el autor **antes** de escribir el código que los usa (autor, 26-10-2026). Revisados el mismo día (`prompts/v0/preguntas_centradas.md` y `arbitro_preguntas_centradas.md`), con estas decisiones del autor: el catálogo validado llega como **requisitos relacionados de lectura obligatoria** (`input/requisitos_relacionados.json`: los que citan un fragmento relacionado o se parecen a lo nuevo desde 0,70, con un tope de 20.000 tokens, los más parecidos primero; medido en `docs/fragmentos-relacionados.md`) y el catálogo entero solo para consultar; el emparejamiento lleva una línea `previa` cuando una pregunta nueva se parece a una ya hecha; una reformulación que mantiene el árbitro se aplica, con el texto anterior, la iteración y el motivo, y `resuelta` y `obsoleta` quedan como propuesta; una revisión de un solo modelo la mantiene o la rechaza el árbitro.
  - Al lanzar una ronda: «Todas las iteraciones» o «Centrarse en…», con casillas por iteración. La ronda guarda su foco.
  - Fragmentos relacionados (`RelatedFragments`): los anteriores parecidos a los de las iteraciones elegidas, desde 0,70 y con un tope de **20.000 tokens** estimados, los más parecidos primero (medido por Claude con el proyecto de clínicas en `docs/fragmentos-relacionados.md` y aprobado por el autor el 26-10-2026: con un documento nuevo salen de mediana 9 fragmentos y unos 270 tokens, y con una tanda de 11 documentos, 90 y unos 2.430). Lectura obligatoria: la documentación elegida y los relacionados; el resto, consultable por tramos.
  - Prompts nuevos del modo centrado (pregunta y árbitro), sin tocar los de siempre. Salida: preguntas nuevas y revisiones (reformular, resuelta, obsoleta, contradice una respuesta, contradice un requisito validado), con motivo y fuentes. El árbitro alinea las revisiones por la pregunta a la que se refieren, sin IA.
  - `Questions`: la pregunta o el requisito con que se enlaza, y el historial de reformulaciones (texto anterior, iteración y motivo).
  - **Cómo quedó (26-10-2026):** la ronda centrada sigue siendo una ronda de preguntas (`questions`) con un foco en su plan (`FocusPlan`, que no se escribe en las rondas de siempre) y en el trabajo (`Job.Focus`). `POST /projects/{id}/question-rounds` acepta `{ "focus": [2] }`; sin cuerpo o vacío, es la ronda de siempre. `FocusedRoundInput` construye las entradas al lanzarla (embeddings en local, `RelatedContext`), `FocusedRound` da los ficheros de `input/` y las revisiones agrupadas, `ArbiterMatching.AgainstPrevious` las líneas `previa`, `PhaseValidator` las comprobaciones nuevas, `QuestionRules.ApplyRevisions` aplica las revisiones y `FakePhaseOutput` responde como una ronda centrada. Migración `AddFocusedQuestions`, que solo añade (`ContradictsQuestion`, `ContradictsRequirement`, `ProposedAction`, `ProposedReason`, `ProposedSources` e `History` en `Questions`; `Focus` en `Jobs`). Web: «Todas las iteraciones» o «Centrarse en…» junto al botón de la ronda, en cuanto hay más de una iteración; el foco de cada ronda; y en cada pregunta, lo que contradice, la propuesta y el historial. Tests: `FocusedRoundTests` (11) y `FocusedRoundFlowTests` (la ronda centrada por la API con `Fake`); los 990 anteriores siguen en verde sin cambios, la instantánea de la v0.12 incluida. 1.002 tests.
  - Web: el foco de cada ronda, la iteración de cada pregunta, sus enlaces y su historial, y las propuestas «resuelta» y «obsoleta» para responder o descartar.
- [x] 113. Actualizar el catálogo con unas iteraciones (prompts v0.13). (27-10-2026; código hecho y probado de punta a punta con `Fake`, sin migración; reconstruido en Docker y revisado por el autor en el navegador el mismo día, con un proyecto de prueba. La prueba con modelos es la del paso 116.)
  - Prompts y esquemas del conjunto de cambios (nuevos, modificados con motivo y eliminados con motivo) y de su árbitro. Entradas: el catálogo validado, la lectura de la lectura relacionada y las respuestas, marcando las nuevas desde el catálogo de partida.
  - Árbitro de cambios: alineación por id sin IA y emparejamiento con embeddings de lo nuevo entre A y B y contra el catálogo existente.
  - La plataforma aplica los cambios a la copia de trabajo, con el historial de cada requisito (iteración, acción, lo que había antes, motivo, origen A/B/ambas y explicación del árbitro) y las propuestas de modificación y eliminación por revisar. No se valida sin revisarlas.
  - Trabajo nuevo (`catalog-update`), con su versión en el historial, su informe (modo, iteraciones y cuántos se crean, modifican y eliminan, de A, de B y del árbitro), la comparación A | árbitro | B y el nuevo arbitraje.
  - Web: «Desde cero» o «Actualizar con…» al generar; el historial y los cambios por revisar de cada requisito.
  - **Cómo quedó (27-10-2026; prompts revisados por el autor antes del código, con los cuatro detalles de Claude aprobados: cambiar el tipo es eliminar y proponer uno nuevo; una contradicción sin aclarar no se cambia; una respuesta es nueva si se contestó o se cambió después de validar el catálogo; la salida es `output/cambios.json`):** `PhaseKind.CatalogUpdate` y el trabajo `catalog-update` (paso Catálogo), `POST /projects/{id}/catalog-update-jobs` con `{ "iterations": [2] }`, que además de lo que exige cualquier catálogo pide el catálogo validado y, para cada iteración, una ronda terminada después de cerrarla que la cubra (`CatalogUpdateInput`). Entradas con `FocusedRoundInput` y `CatalogUpdateRound`; validación `ValidateCatalogUpdate`; corrección `CAMBIO-DISC`; emparejamiento contra el catálogo (`AgainstExisting`). `CatalogUpdate.Apply` aplica los cambios a la copia de trabajo (nuevos con el siguiente id que no se haya dado en el historial, que ahora sigue las actualizaciones hasta la última generación desde cero; modificados y eliminados con un `cambio` pendiente; cada requisito con su `historial`; los eliminados aceptados pasan a `eliminados`, que guardar conserva aunque la web no lo envíe), y `POST …/deliverables/catalog/requirements/{id}/accept-change` o `reject-change` los decide; un cambio sin revisar impide validar (`CAMBIO`). Versión `Updated` en el historial; cobertura, la de antes más las iteraciones de la actualización; informe con `update` (iteraciones y nuevos, modificados y eliminados de A, B y el árbitro); comparación A | árbitro | B y nuevo arbitraje también para este trabajo. Web: «Actualizar con…» en el paso Catálogo cuando el catálogo validado no incluye alguna iteración cerrada; en cada requisito, «Modificación/Eliminación propuesta» con aceptar o rechazar, y su historial; filtro «Solo cambios propuestos»; la actualización en el informe. Tests: `CatalogUpdateTests` (8) y `CatalogUpdateFlowTests` (la actualización por la API con `Fake`, de la ronda centrada a validar tras aceptar el cambio). Ningún test anterior cambia. 1.013 tests.
- [x] 114. Actualizar el backlog con unas iteraciones (prompts v0.13). (27-10-2026; código hecho y probado de punta a punta con `Fake`, sin migración; reconstruido en Docker y revisado por el autor en el navegador el mismo día, junto con el paso 113.)
  - `BacklogImpact`: del catálogo del que salió el backlog al validado de ahora, las historias que citan requisitos modificados o eliminados y los requisitos nuevos sin historia.
  - Prompts y esquemas del conjunto de cambios del backlog (historias nuevas, modificadas y eliminadas, y criterios y tareas por id, como en el paso 108) y de su árbitro.
  - La plataforma los aplica con su historial y sus propuestas por revisar, como en el catálogo. Trabajo `backlog-update`, con versión, informe, comparación y nuevo arbitraje.
  - **Cómo quedó (27-10-2026; prompts revisados por el autor antes del código, con los cuatro detalles de Claude aprobados: una historia que pierde un requisito eliminado se modifica y solo se elimina si se queda sin ninguno; un `RNF`, `RA` o `RT` nuevo va como criterio o tarea; los criterios de las historias nuevas se escriben enteros; se ofrece actualizar con las iteraciones que tiene el catálogo validado y le faltan al backlog):** `PhaseKind.BacklogUpdate` y el trabajo `backlog-update` (paso Backlog); `BacklogImpact` (el impacto sin IA, del catálogo del plan del trabajo que generó el backlog al validado ahora); `BacklogUpdateRound` (ficheros de `input/` y cambios agrupados por historia); `ValidateBacklogUpdate` (`REF-REQ`, `REF-EP`, `REF-HU`, `CAMBIO-DUP`, `CITA`, `ARB-CAMBIO`); `CAMBIO-DISC` también aquí; emparejamiento de las nuevas contra el backlog (`existente:`); `BacklogUpdate` (las nuevas en su épica, o en una nueva con el siguiente `EP`, con ids estables de historias, criterios y tareas; lo modificado o eliminado como `cambio` pendiente; las eliminadas aceptadas en `eliminadas`, con su épica). `POST /projects/{id}/backlog-update-jobs` exige el backlog validado y el catálogo validado con esas iteraciones, y rechaza la actualización si el catálogo no ha cambiado. Aceptar o rechazar en `POST …/deliverables/{catalog|backlog}/{requirements|stories}/{id}/{accept-change|reject-change}`. Web: el lanzador «Actualizar con…» sirve a los dos pasos (`UpdateLauncher`); en cada historia, el cambio propuesto con aceptar o rechazar y su historial. Tests: `BacklogUpdateTests` (5) y el flujo de `CatalogUpdateFlowTests`, que ahora actualiza también el backlog. 1.020 tests.
- [x] 115. Desde cero, frente a lo anterior. (27-10-2026; diseño aprobado por el autor, sin prompts nuevos ni migración; reconstruido en Docker y revisado por el autor en el navegador el mismo día, con el backlog de clínicas: 22 historias iguales, 65 cambiadas, 7 nuevas y 19 desaparecidas frente a su versión 1 validada. Los trabajos de antes del historial de versiones no tienen comparación, y la API dice por qué.)
  - Tras generar el catálogo o el backlog desde cero, la plataforma empareja con embeddings el resultado con la versión anterior y marca cada elemento como igual, cambiado o nuevo, y lo que ha desaparecido.
  - El informe y su CSV, con las columnas nuevas al final.
  - **Cómo quedó (27-10-2026, detalles de Claude aprobados por el autor):**
    - **Contra qué:** la versión que tenía el entregable justo antes de la versión `Generated` del trabajo, en borrador o validada (el informe dice cuál y cómo estaba). No la última validada: es más fácil de explicar y en el paso 116 coinciden. Sin comparación en la primera versión, en las actualizaciones (ya traen su lista de cambios), en el nuevo arbitraje (no cambia el entregable) y en las otras fases.
    - **Cómo:** como A y B en el paso 87 (`PreviousVersion`, Core): cada elemento generado con el más parecido de antes y al revés, con los mismos textos (el del requisito; el título y la narrativa de la historia) y los dos umbrales calibrados: igual desde 0,90, cambiado desde 0,70, nuevo por debajo; desaparecido, uno de antes sin ninguno nuevo desde 0,70. Sin mirar los ids, porque desde cero se numera otra vez desde 1. Como entre A y B, varios elementos pueden emparejarse con el mismo, así que las cifras no tienen por qué sumar uno a uno.
    - **Se calcula una vez:** la primera vez que se pide, y se guarda en `data/jobs/{id}/frente-anterior.json` con el nombre del modelo de embeddings (`MatchingCache`, que ahora acepta el nombre del fichero). Las dos versiones están fijas en el historial, así que no cambia aunque el usuario edite después.
    - **API:** `GET /jobs/{id}/previous-version` (`JobPreviousVersion`): cada elemento con su marca (`Same`, `Changed`, `New`), su par de antes y el parecido, los desaparecidos (`gone`) y, sin comparación, el motivo (`unavailable`). El informe lleva `previous` (versión, estado y las cuatro cifras) y el CSV del proyecto, `ant_version`, `ant_igual`, `ant_cambiado`, `ant_nuevo` y `ant_desaparecido` al final.
    - **Web:** en el editor del catálogo y del backlog, si lo que hay salió de una generación desde cero con versión anterior, una marca en cada requisito o historia («Igual que RF-012», «Cambiado», con lo que era antes al abrirlo, o «Nuevo»), un filtro por marca y un apartado plegado con los desaparecidos; como los ids no cambian al editar (paso 110), las marcas siguen a los elementos, y lo añadido a mano no lleva. En el informe del trabajo, la línea «Frente a lo anterior».
    - **Tests:** `PreviousVersionTests` (el cálculo de requisitos e historias, sin nada antes, y por la API con `Fake`: la primera versión y una ronda no tienen comparación; la segunda, generada tras reescribir un requisito, se compara con ese borrador, se guarda una vez y llega al informe y al CSV). En `FullFlowTests`, la cabecera del CSV termina ahora en las columnas del paso 115 (antes, en las del 100). 1.024 tests en verde.
- [x] 116. Prueba real con clínicas en dos iteraciones. (27-10 y 03-11-2026; ejecutada por Claude por la API. Resultados en `docs/resultados-paso-116/README.md`.)
  - Una segunda iteración de documentos ficticios para el proyecto de clínicas, sobre el flujo ya validado de la v0.11, con novedades, cambios y contradicciones con la iteración 1 puestos a propósito y listados como referencia, como en `ejemplos/README.md`. (Aprobado por el autor el 26-10-2026, frente a partir en dos tandas los documentos que ya hay.)
  - Ronda centrada, actualización del catálogo y del backlog, y lo mismo desde cero. Se mide el coste y el tiempo, cuánto de lo validado se conserva igual y cuántos de los cambios puestos a propósito se detectan. La ronda centrada se mide también con la lectura completa frente a la lectura relacionada.
  - Resultados en `docs/evolucion-prompt-requisitos.md` (v0.13) y `docs/costes/costes.md`.
  - **Cómo se hace (27-10-2026, autor):** los dos caminos en el mismo proyecto, «Clinicas Veterinarias v0.11», uno detrás de otro: primero actualizar (ronda centrada, catálogo y backlog), después desde cero (ronda con todas, catálogo y backlog), que el paso 115 compara con lo actualizado. No se programa «copiar un proyecto». Lo ejecuta Claude por la API, a petición del autor: responde con la hoja de respuestas del cliente, escrita antes de ver ninguna salida, y deja anotada cada decisión al revisar las propuestas.
  - **Lectura completa frente a lectura relacionada (Claude, con permiso del autor):** una ronda centrada experimental que lo lee todo, `POST /projects/{id}/question-rounds/read-all` (`ReadAllRound`, trabajo `questions-read-all`): los mismos prompts v0.13 y las mismas entradas, salvo que en `input/relacionados.txt` van todos los fragmentos anteriores y no solo los relacionados (los requisitos relacionados no cambian). Como el nuevo arbitraje, deja su resultado y su informe, no cuenta como ronda ni para actualizar, y sus preguntas no se añaden al proyecto. Test en `FocusedRoundFlowTests`; 1.025 tests en verde.
  - **Un fallo del paso 112, arreglado durante la prueba (27-10-2026):** una pregunta enlazada a una respuesta se rechazaba siempre (`REF-PREG`), porque el plan de una ronda no lleva preguntas respondidas; el contexto de un plan centrado añade las respondidas de su foco (`FocusPlan.AnsweredCodes`). 1.026 tests.
  - **Resultados (03-11-2026):**
    - Las dos rondas centradas, la normal (C) y la que lo lee todo (A), detectan las 8 preguntas esperadas y enlazan las 4 que contradicen una respuesta.
    - Los dos caminos recogen los 14 cambios que llegan al catálogo.
    - Actualizar conserva idéntico el 91 % de los requisitos y el 95 % de las historias validadas; desde cero, el 0 %.
    - Actualizar no ahorra dinero (5,85 $ y 29 min, frente a 5,44 $ y 48 min con un intento fallido de LUNA), porque consulta mucho lo anterior. Lo que ahorra es revisión: unas 25 propuestas por entregable en lugar de volver a revisarlo entero.
    - Con esta iteración pequeña, leerlo todo (0,95 $) sale más barato que la lectura relacionada (1,67 $).
    - Incidencias:
      - LUNA numera las preguntas centradas desde P-064;
      - el error de esquema no dice su línea;
      - nginx corta a los 60 s el lanzamiento de una ronda centrada o una actualización y devuelve 504.
    - Propuestas de mejora para decidir con el autor, en el documento de resultados.

- [x] 122. Las mejoras de la prueba real (03-11-2026; el autor eligió 1, 2 y 3 de las propuestas de `docs/resultados-paso-116/README.md`, y la 5 es el paso 123; la 4 no se hace, como recomendaba Claude: con una sola medición no se sabe si el coste es estable, y cambiaría la lectura relacionada que decidió el autor).
  - **nginx** (`frontend/nginx.conf`): `proxy_read_timeout 5m` en `/projects` y `/jobs`. Lanzar una ronda centrada o una actualización en clínicas calcula embeddings durante más de 60 s, y nginx devolvía 504 aunque la tarea se lanzaba.
  - **`ID-SEQ` en las rondas centradas** (`PlatformFixes`, una corrección automática más, con su aviso): si las preguntas nuevas de A o B van seguidas desde otro número (P-064, P-065…), se renumeran desde P-001. Solo sus ids: `contradice` y las revisiones citan preguntas del proyecto, que no cambian. Cualquier otro hueco sigue siendo un error. Los prompts no cambian, como en el paso 89.
  - **El error de esquema con su línea** (`PhaseTask.Locate`): `SCHEMA requisitos/74/notacion` recibe en la reparación la línea del elemento de esa ruta que tiene id (el más profundo), como los demás errores.
  - Tests: `FocusedRoundTests` (la renumeración y lo que no toca) y `PhaseTaskTests` (la línea del error de esquema). 1.028 tests en verde.
- [x] 123. La lectura relacionada con una documentación grande (03 y 04-11-2026; con un documento público de PURE, como recomendaba Claude y eligió el autor, frente a ampliar clínicas o dejarlo como limitación). Resultados en `docs/resultados-paso-123/README.md`.
  - **Documento:** PURE v2.0 (Zenodo 7118517, CC BY 4.0), PEPPOL D2.1 (127 páginas, unos 96.000 tokens, en inglés), partido por páginas en dos iteraciones (1–90 y 91–127). Proyecto «PURE PEPPOL VCD (paso 123)», en inglés; solo las dos rondas centradas en la iteración 2.
  - **Resultado:**
    - Leerlo todo falla entero: A y B llenan los 128.000 tokens de contexto y la plataforma los detiene al compactar por segunda vez sin haber escrito, también al repetirse (unos 1,35 $ estimados, sin resultado).
    - La lectura relacionada termina a la primera, sin reparaciones, por 0,87 $. Saca 10 preguntas en inglés, con un tercio de sus fragmentos de la iteración 1. Es la primera ronda del flujo en inglés.
    - Con el paso 116: leerlo todo es una optimización para la documentación pequeña, y la lectura relacionada vale para cualquier tamaño.
    - LUNA no propuso ninguna pregunta.
    - Las duraciones de las repeticiones de una tarea no son reales: se guardan con la hora de inicio del primer intento.

**Hecho cuando:** en un proyecto con el catálogo y el backlog validados, se añade una segunda iteración, se lanza una ronda centrada en ella y se actualizan el catálogo y el backlog. Cada elemento dice de qué iteración viene, lo modificado y lo eliminado llevan su motivo y se revisan antes de validar, lo que no cambia sigue igual, y los modos de siempre dan exactamente lo mismo que antes.

## Hito 23: evaluación

Material para el capítulo de evaluación de la memoria. Era el hito 22 hasta el 26-10-2026, cuando el autor puso delante las iteraciones del proyecto. Sus pasos conservan su número. Incluye también los resultados del paso 116 (actualizar frente a empezar de cero).

El 27-10-2026 el autor añade los pasos 117 a 121, que propuso Claude al repasar la propuesta del TFG: ningún paso medía la hipótesis (que la verificación cruzada mejora la fiabilidad frente a un solo modelo), y el paso 65 apunta a que puede no mejorarla (la unión se parece mucho a A, y de las 15 contradicciones abiertas 2 las detecta solo A y ninguna solo B). Un resultado negativo también vale para la memoria, pero hay que medirlo. Se hacen en este orden: 117 antes que cualquier ejecución de la evaluación; después 105, 104 (usa lo que genera el 105, en inglés), 106, 118, 119, 120 y 121. El 103 se hizo antes, con la v0.9, y sus resultados se citan con esa versión. Los pasos 118 a 121 llevan sus instrucciones, separadas en lo que hace el autor y lo que hace Claude, en su propio apartado, más abajo.

- [x] 103. Terminar el experimento de si el árbitro prefiere a su propio modelo (`docs/experimento-sesgo-arbitro.md`). (16-10-2026: no hay preferencia; la unión se parece a A porque A propone más y casi todo lo de B ya está en A. LUNA, 2 arbitrajes válidos de 6 en el catálogo.)
- [x] 117. Congelar la versión que se evalúa (27-10-2026, autor). Así los resultados son comparables entre sí y el tribunal puede repetir el paso 105 con la misma versión. Unos 15 minutos, sin coste. Se hace con el hito 22 cerrado y antes de la primera ejecución de la evaluación.
  **Hecho el 04-11-2026** por Claude, a petición del autor; detalle en `docs/evaluacion/version.md`.
  - **Prueba en inglés:** la ronda de preguntas normal (v0.12) con el documento del paso 105, porque la del paso 123 era una ronda centrada (v0.13). Salió válida a la primera, sin reparaciones, en 4 minutos y por 0,50 $: 39 preguntas en inglés (18 de las dos propuestas, 19 solo de A y 2 solo de B). LUNA propuso 20. No hizo falta corregir nada. El proyecto se borró y su resultado está en `docs/evaluacion/prueba-ingles-paso-117/`.
  - **Comprobado:** `git status` limpio y todos los tests en verde. Las imágenes de Docker (03-11-2026) son del código del paso 122 (`d0dc673`), el último con código, y los 30 prompts y esquemas del repositorio están byte a byte en la imagen. Claude Code 2.1.283.
  - **Etiqueta `evaluacion-1`:** creada el 04-11-2026 en el commit de este paso (`6d34025`), sin push, con el visto bueno del autor.
  **Qué haces tú** (o Claude, si se lo pides):
  1. **Antes de congelar, haz una prueba barata en inglés** (añadido el 27-10-2026). El flujo en inglés no se ha probado nunca con modelos: los prompts lo admiten (`idioma`), pero ningún test ni ninguna ejecución real lo ha usado.
     - Crea el proyecto del paso 105 con el idioma en inglés, sube el documento de PURE y lanza solo la ronda de preguntas. Cuesta en torno a 1 $.
     - Si algo falla, se corrige ahora y no a mitad de la evaluación.
     - Ese proyecto se borra después, y el 105 se hace ya con la versión congelada.
  2. **Comprueba que todo está guardado:** `git status` debe salir limpio, todos los tests en verde y Docker reconstruido con ese código.
  3. **Crea la etiqueta:** `git tag evaluacion-1`, sin push.
  4. **Rellena `docs/evaluacion/version.md`** con:
     - el commit (`git rev-parse HEAD`);
     - la versión de los prompts (la v0.12 en los modos de siempre y la v0.13 en los centrados y las actualizaciones);
     - los modelos de cada papel: A, B, árbitro, sugerencias y mockup;
     - los umbrales del emparejamiento (0,90 y 0,70);
     - `Catalog:MaxParallelLots`;
     - la versión de Claude Code, con `claude --version` dentro del contenedor de la API (es el único comando que se lanza ahí; nada que imprima variables de entorno).
  5. **Si durante la evaluación un error obliga a cambiar código o prompts:** se crea `evaluacion-2`, se anota en el mismo fichero qué cambió y por qué, y se repiten las ejecuciones afectadas. Cada resultado dice con qué etiqueta se obtuvo.
- [x] 105. El flujo completo con un documento público del dataset PURE (27-10-2026, detalles de Claude). Hasta ahora todo se ha probado con documentos que hizo Claude o que son internos. Un documento público, ajeno al proyecto, es una prueba que el tribunal puede repetir. Coste: como un flujo de clínicas, unos 7–9 $, y algo más si el documento es más largo.
  **Hecho el 04-11-2026** por Claude por la API, a petición del autor, con `evaluacion-1`. Resultados en `docs/evaluacion/pure.md`.
  - **Resultado:** todo a la primera, sin reparaciones, en 46 minutos y por 9,29 $ (el mockup, 6,81 $; el autor aprobó lanzarlo tras saber que la estimación subía a 6–12 $ solo para él). Salen 40 preguntas, 122 requisitos, 34 historias y un mockup de 21 pantallas, todo en inglés. LUNA propone en todas las fases.
  - **Al hacerlo:**
    - 39 de las 40 sugerencias son suposiciones y 28 son decisiones del cliente, muchas sin opciones; se concretó la regla de «la primera opción» (en `pure.md`), y lo que solo el cliente puede aportar llega al catálogo como `[TBD]`;
    - validar exige marcar como revisadas las discrepancias (7 en el catálogo y 7 en el backlog), sin editar nada más;
    - los textos fijos de la plataforma (narrativa «Como… quiero… para…», «Tarea», Word y PDF, el prefijo «A decidir por el cliente:») siguen en español en un proyecto en inglés: mejora futura, en «Después», punto 6.
  - **El documento no se copia en el repositorio** (lleva el copyright de Microcare y el repositorio aún no tiene licencia): se anota cómo descargarlo y su SHA-256.
  **Qué es PURE:** 79 documentos de requisitos públicos (sobre todo especificaciones SRS en inglés), de Ferrari, Spagnolo y Gnesi, RE 2017. Están en [Zenodo](https://zenodo.org/records/1414117); hay una versión más reciente, que es la que se usa.
  **Documento elegido** (04-11-2026, propuesto por Claude y aprobado por el autor): `2005 - microcare.pdf`, el SRS del *Voucher Management System* de Microcare. Tiene 33 páginas y unos 11.000 tokens, de la versión 2.0 (Zenodo, registro 7118517, CC BY 4.0, la misma del paso 123).
  - **Por qué:** es una especificación industrial, no un ejercicio de clase, con varios módulos y usuarios con permisos (creación de vales, ventas, reclamaciones, devoluciones, opiniones de clientes, informes y seguridad), y el texto se extrae bien.
  - **Alternativa:** `2008 - vub.pdf`, un trabajo de clase.
  - **Descartados:** `keepass` (un solo usuario), `puget sound` (marcado como «Confidential») y `cctns` (corto y con datos policiales).
  - Ya se usó en la prueba en inglés del paso 117.
  **Qué haces tú:**
  1. **Elige un documento** con estas cuatro condiciones:
     - en inglés;
     - una aplicación con usuarios y funciones, no un sistema embebido ni una norma;
     - entre 10 y 40 páginas, para que quepa en el tope de documentación y cueste como clínicas;
     - en un formato que admita Concilia (`.pdf`, `.docx`, `.html` o `.xml`; el `.doc` antiguo no se admite: si solo está así, ábrelo con Word y guárdalo como `.docx`).
     Anota en `docs/evaluacion/pure.md` cuál es, de dónde se descarga y su licencia.
  2. **Comprueba la licencia** en la página de Zenodo. Si permite copiarlo, se guarda en `ejemplos/pure/` con su atribución; si no, solo se anota cómo descargarlo.
  3. **Crea el proyecto «PURE · nombre del documento»** y, en su configuración, pon el idioma en **inglés** (`en`). Así el catálogo y el backlog salen en inglés y valen para el paso 104.
  4. **Sube el documento, cierra la documentación y recorre el flujo entero:**
     - una ronda de preguntas;
     - sugerencias con «Aprobar todas las sugerencias» y, en las que queden («A decidir por el cliente:»), la primera opción que proponga la sugerencia;
     - el catálogo, validado sin editar;
     - el backlog, validado sin editar;
     - el mockup.
     Sin editar, para que cualquiera que lo repita obtenga lo mismo.
  5. **Descarga** el catálogo (Word y PDF), el backlog (Gherkin) y el CSV del informe del proyecto, y guárdalos en `docs/evaluacion/pure/`.
  **Qué hace Claude:**
  - Escribe `docs/evaluacion/pure.md`: el documento, las cifras del informe (tokens, minutos, coste, de A, de B, unión y discrepancias), qué fue bien y qué mal, y el bloque en palabras sencillas.
  - **Mira si LUNA propone preguntas** (04-11-2026, autor, como recomendaba Claude): en la ronda centrada en inglés del paso 123 su propuesta fue válida, pero vacía. En lugar de repetir aquella ronda, se comprueba aquí y en las repeticiones del paso 119 si vuelve a pasar con un documento en inglés.
  - Regenera `docs/costes/costes.md`. (`costes.cs` muestra ya por su nombre los proyectos «PURE …», 04-11-2026.)
- [x] 104. Métricas externas de calidad (27-10-2026, detalles de Claude). Mide la calidad con herramientas de otros y no solo con las de Concilia. Las dos trabajan en inglés: se aplican al catálogo y al backlog del paso 105, y a las tres propuestas de cada fase (SOL, LUNA y la unión), lo que completa la comparación del paso 120. Unas 3–4 horas, la mayor parte en instalar y probar las herramientas; sin coste.
  **Hecho el 04-11-2026** por Claude, a petición del autor; instalación, criterios y resultados en `docs/evaluacion/metricas-externas.md`.
  - **Herramientas, en `C:\TFG\herramientas\`:**
    - AQUSA (`aqusa-core` `3043743`) con Python 3.8.20, `nltk` 3.4.5 y `yattag` 1.11.1; `uv` no ofrece la 3.7 para Windows;
    - Vale 3.24.0 con `vale-ears` 0.1.0, con el SHA-256 comprobado. No distingue mayúsculas. Su vocabulario anula `ears.Shall` si se activa, así que no se activa.
  - **Script:** `scripts/evaluacion/metricas-externas.cs`, con dos órdenes:
    - `extraer`, que lanza el autor, porque lee `data/runs/`, a la que Claude no tiene acceso;
    - `medir`.

    Historias: «As {como}, I want to {quiero}, so that I can {para}».
  - **Resultado, tras revisar cada aviso a mano:**
    - requisitos EARS con un defecto real: SOL 5,1 por cada 100, LUNA 9,6 y la unión 7,0;
    - historias con dos acciones: SOL 6,0, LUNA 21,4 y la unión 17,6.

    Las 6 historias de la unión con dos acciones juntan cada una 2 o 3 historias atómicas de SOL. En bruto las herramientas exageran: 63 de 90 avisos de Vale y 8 de 23 de AQUSA son falsas alarmas.
  - **No se hizo** lo de clínicas en inglés («si da tiempo»): habría que generarlo de nuevo, con coste.
  - **AQUSA** (Lucassen, Dalpiaz y otros, Universidad de Utrecht, revista *Requirements Engineering*, 2016) revisa historias de usuario con parte de los 13 criterios del marco QUS: bien formadas («As a… I want… so that…»), atómicas (una sola cosa), mínimas (sin texto de más), únicas, uniformes… Es una herramienta en Python.
  - **`vale-ears`** (`tbhb/vale-ears`, MIT) es un estilo para el corrector Vale con cuatro reglas:
    - `ears.Syntax`: cada frase con *shall* sigue uno de los cinco patrones EARS;
    - `ears.Shall`: no se usa *must*, *will*, *should* o *may* en lugar de *shall*;
    - `ears.PassiveVoice`: sin voz pasiva;
    - `ears.WeakWords`: sin palabras vagas como *fast* o *usually*.
  **Lo que dejó el paso 105** (04-11-2026): la narrativa que monta la plataforma está en español («Como… quiero… para…»), así que la entrada de AQUSA se escribe a partir de los campos `como`, `quiero` y `para` del backlog con la plantilla inglesa «As a… I want… so that…». Y los requisitos sin condición empiezan en minúscula («the Voucher Management System SHALL…»): mirar si `vale-ears` lo cuenta como error.
  **Cómo son las herramientas** (comprobado por Claude en sus repositorios el 27-10-2026; corrige un aviso de la mañana, que decía que no se encontraba `RELabUU/aqusa-core`):
  - **`RELabUU/aqusa-core`** existe: es la versión de línea de comandos de AQUSA, con licencia MIT.
    - Lee un fichero de texto con una historia por línea, en `./input`.
    - Se ejecuta con `python aqusacore.py -i <fichero> -o <salida> -f txt` (o `html`).
    - Se instala con `pip install -r requirements.txt`.
    - Está probado con Python 3.7, que ya no tiene soporte, así que puede no funcionar con un Python actual. Si falla, se usa un entorno con Python 3.7 (por ejemplo, con `uv` o con una imagen de Docker `python:3.7`).
  - **`vale-ears`** se instala:
    - añadiendo el paquete (`ears.zip` de su última versión) en `Packages` de un `.vale.ini`, con `BasedOnStyles = ears`;
    - ejecutando `vale sync`.
    Vale es un único ejecutable que se descarga para Windows. Es de junio de 2026 y casi no lo usa nadie, y su página no dice si reconoce las palabras clave en mayúsculas, que es como las escribe Concilia.
  Por eso lo primero es probar que funcionan, en una hora como mucho. Si alguna no funciona, se anota y se busca otra en la lista `nlp4re-tools`; no se escribe una propia.
  **Qué haces tú** (o Claude, si se lo pides):
  1. **Instala AQUSA** en un entorno de Python aparte, fuera del repositorio, y pruébalo con tres historias escritas a mano: una buena, una con dos cosas a la vez y una sin «so that».
  2. **Instala Vale y `vale-ears`** y pruébalo con dos frases: una EARS buena («WHEN the user cancels an appointment, the system shall…») y una mala.
  3. **Comprueba si Vale distingue mayúsculas.** Concilia escribe las palabras clave de EARS en mayúsculas («WHEN», «IF … THEN»).
  4. **Anota las versiones y los comandos** en `docs/evaluacion/metricas-externas.md`.
  **Qué hace Claude:**
  - El script `scripts/evaluacion/metricas-externas.cs`, que saca de los trabajos del paso 105 las historias de SOL, de LUNA y de la unión.
    - Las historias se escriben como «As a {como}, I want {quiero}, so that {para}»: en Concilia se guardan en tres campos sin esas palabras.
    - Los requisitos se escriben uno por línea, **solo los de notación EARS**. Los MASTeR usan *should* y *may* a propósito, y `ears.Shall` los marcaría como error.
    - Después ejecuta AQUSA y Vale y cuenta los defectos por criterio y por propuesta.
  - Resultados en `docs/evaluacion/metricas-externas.md`: defectos por cada 100 historias o requisitos de SOL, LUNA y la unión, con ejemplos y el bloque en palabras sencillas.
  - Si da tiempo, lo mismo con el proyecto de clínicas generado en inglés.
  - Limitación: AQUSA tiene una precisión del 72 % según sus autores, así que sus avisos se cuentan, pero no se dan por buenos sin mirarlos.
- [ ] 106. Una prueba editando de verdad, como un analista (27-10-2026, detalles de Claude). Hasta ahora todas las respuestas fueron sugerencias de la IA aceptadas tal cual y nadie editó el catálogo ni el backlog, así que la métrica de cambios del usuario (paso 64) no tiene datos. Incluye los pasos 49 (segunda ronda de preguntas) y 53 (editar el catálogo), que siguen pendientes, y se hace con ReservaSalas porque esos dos pasos son de ese ejemplo. Coste: unos pocos dólares, porque ReservaSalas es pequeño. Tiempo: el que tardes en revisar, unas 2–3 horas.
  **Qué haces tú:**
  1. **Crea el proyecto «ReservaSalas analista»,** sube `prompts/generacion_requisitos/ejemplo/fuentes/DOC-01.txt` y cierra la documentación.
  2. **Primera ronda de preguntas.** Respóndelas tú, como lo haría un cliente, aunque te ayudes de las sugerencias, que puedes editar.
  3. **Segunda ronda (paso 49).** Lánzala y apunta si repite algo ya preguntado.
  4. **Catálogo (paso 53).** Genéralo y revísalo entero, como un analista:
     - corrige lo que esté mal;
     - borra lo que sobre;
     - añade lo que falte;
     - revisa cada discrepancia (marca si era un problema de verdad).
     Después, valídalo.
  5. **Backlog.** Igual: genéralo, revísalo entero y valídalo.
  6. **Mientras revisas,** apunta en `docs/evaluacion/analista.md` el tiempo que tardas en cada fase y, en una frase, los tipos de error que más corriges.
  **Qué hace Claude:**
  - Con el informe de cada trabajo (cambios del usuario: añadidos, borrados y modificados, emparejados por contenido) y tus notas, escribe los resultados:
    - en `docs/evaluacion/analista.md`;
    - en la sección v0 de `docs/evolucion-prompt-requisitos.md`, que es lo que piden los pasos 49 y 53;
    - el bloque en palabras sencillas.
  - Marca como hechos los pasos 49 y 53.
  - Cuánto corrige un analista y cuánto tarda es lo que mejor dice si la automatización ahorra trabajo, que es la pregunta del título del TFG.

### Los pasos 118 a 121: Concilia frente a un solo modelo

Decidido el 27-10-2026: el autor propuso comparar una ejecución de SOL, una de LUNA y la de los dos con el árbitro en el proyecto grande (clínicas), y dejó a Claude los detalles. Lo que hay que saber antes de empezar:

- **No hacen falta ejecuciones aparte de SOL y de LUNA.** Cada trabajo de Concilia ya lleva una ejecución de SOL sola (A) y una de LUNA sola (B): el prompt no menciona al otro modelo ni al árbitro (decisión del 1-10-2026), así que la propuesta de A es exactamente lo que daría SOL lanzado solo. Un trabajo da las tres cosas que se comparan, SOL, LUNA y la unión del árbitro, y las tres parten de la misma entrada.
- **Se compara fase a fase**, no el flujo entero de un modelo contra el de Concilia: así un fallo en las preguntas no se arrastra al catálogo y se sabe de qué fase viene cada diferencia.
- **Se mide frente a una referencia, no contando elementos.** En el paso 65, SOL propuso 299 requisitos, LUNA 192 y la unión 209: la unión tiene menos que SOL porque el árbitro quita repeticiones, y contar no dice cuál es mejor.
- **Tres medidas, en palabras sencillas:**
  - **recall** (cobertura): de lo que debía estar, cuánto está;
  - **precisión:** de lo que propone, cuánto está bien (lo dice su fuente, no está repetido y no sobra);
  - **F1:** un solo número que junta las dos (su media armónica: solo es alto si las dos lo son).
- **Coste:** «SOL solo» cuesta las ejecuciones de A del trabajo; «LUNA solo», las de B; Concilia, todas (A, B y árbitro). En minutos, Concilia tarda lo que el más lento de A y B más el árbitro, porque A y B van en paralelo.
- **Cierra la decisión abierta de la referencia del catálogo y del backlog:** ni la versión validada del paso 106 (que se queda para medir los cambios del usuario) ni una lista del proyecto mediano, sino una lista de funcionalidades del proyecto grande hecha por el autor y una muestra revisada a mano.

- [ ] 118. Preparar la referencia (27-10-2026, autor). Los proyectos de ejemplo, su lista de contradicciones y huecos y la comprobación del paso 65 los hizo Claude, que también desarrolla la plataforma (`docs/resultados-paso-65/README.md`, «Ni independientes ni reales»). La referencia de la evaluación la revisa o la hace el autor. Unas 3–4 horas, sin coste.
  **Qué haces tú:**
  1. **Revisa las 26 «trampas» del proyecto grande** (G1–G26 en `ejemplos/README.md`). De cada una, comprueba en los documentos de `ejemplos/grande-clinicas-veterinarias/` que la contradicción o el hueco está como dice la tabla y que el tratamiento esperado es el correcto. Hay cuatro tipos:
     - una contradicción **abierta** debe preguntarse;
     - una **resuelta** en un documento posterior no debe preguntarse, y el catálogo debe recoger la decisión final;
     - un **hueco** de un solo documento debe estar en el catálogo;
     - lo **fuera de alcance** no debe estar en el catálogo.
     Si algo no cuadra, corrígelo en la tabla y añade debajo una línea «Revisado por el autor el DD-MM-AAAA: …».
  2. **Haz la lista de funcionalidades de clínicas**, en `docs/evaluacion/referencia-clinicas.md`. Es lo que se usa para medir la cobertura del catálogo y del backlog.
     - Léela de los documentos, **sin mirar ningún catálogo generado**, para no copiar lo que dicen los modelos.
     - Una funcionalidad es algo que un usuario hace con el sistema o que el sistema hace solo, como «Cancelar una cita desde la app» o «Avisar de una vacuna próxima». Un detalle de una funcionalidad no es una funcionalidad: «con 12 h de antelación» va dentro de «Cancelar una cita».
     - Una por línea, con un id, el nombre y el documento donde aparece: `F01 · Cancelar una cita desde la app · 04-reunion-recepcion.srt`. Saldrán unas 40–60.
  3. **Haz lo mismo con la lista de cambios de la segunda iteración de clínicas** (paso 116): revísala como en el punto 1.
- [ ] 119. Las ejecuciones: 3 repeticiones de cada fase (27-10-2026, autor). Con una sola ejecución no se distingue una diferencia real de una casualidad, como en el experimento del sesgo del árbitro. Con la versión congelada del paso 117 y la configuración de siempre: A `SS - GPT-6-SOL`, B `SS - GPT-6-LUNA` y árbitro SOL.
  - **Coste estimado** (con `docs/costes/costes.md`): una vuelta de preguntas, catálogo y backlog de clínicas con la v0.11 costó unos 7–9 $ y unos 35–45 minutos. Las 3 repeticiones rondan los 20–30 $ y 2–3 horas, casi todo de espera. Se confirma con la primera; si se dispara, se hacen 2 y se dice.
  - **Por qué cada fase se repite desde la misma entrada:** para que lo que cambie venga solo de esa fase. El catálogo sale de las mismas preguntas respondidas, y el backlog, del mismo catálogo validado. Las preguntas, en cambio, se repiten en proyectos distintos, porque una segunda ronda en el mismo proyecto ya ve las preguntas de la primera.
  **Qué haces tú** (solo se lanza una tarea cada vez en toda la plataforma):
  1. **Comprueba la configuración.** En «Ejecuciones», el primer trabajo debe decir SOL en A y en el árbitro, y LUNA en B.
  2. **Preguntas.** Crea tres proyectos, «Clínicas eval 1», «Clínicas eval 2» y «Clínicas eval 3». En cada uno:
     - sube los 21 archivos de `ejemplos/grande-clinicas-veterinarias/` (sin `.zip`);
     - pulsa «Cerrar documentación»;
     - lanza **una** ronda de preguntas. Solo la primera ronda.
  3. **Responde las preguntas, solo en «Clínicas eval 1».** Pide las sugerencias de toda la ronda y pulsa «Aprobar todas las sugerencias».
     - Las que queden («A decidir por el cliente:») respóndelas con la primera opción que proponga la sugerencia. Así cualquiera puede repetirlo.
     - No edites ninguna respuesta.
  4. **Catálogo.** En «Clínicas eval 1», genera el catálogo **3 veces** seguidas, sin editarlo entre medias.
     - Cada generación sustituye al borrador, pero las propuestas de A y B y la unión de cada trabajo se quedan guardadas en sus carpetas.
     - Después, valida el último tal cual, sin editarlo.
  5. **Backlog.** En «Clínicas eval 1», genera el backlog **3 veces** seguidas, sin editarlo.
  6. **Si un trabajo falla** (A o B sin salida válida), relánzalo y apúntalo: cuántos fallan también es un dato de fiabilidad.
  7. **Avisa a Claude** cuando estén los 9 trabajos válidos (3 de preguntas, 3 de catálogo y 3 de backlog).
  **Qué hace Claude:**
  - El script `scripts/evaluacion/repeticiones.cs`, que lee los trabajos de la API por el nombre de los proyectos, como `costes.cs`.
  - De cada fase y repetición, saca el informe de siempre: tokens, minutos, coste, elementos de A, de B y de la unión, y discrepancias.
  - Mide **cuánto se parecen entre sí las repeticiones**: de cada dos repeticiones de A, qué porcentaje de elementos de una tiene par «mismo» o «parecido» en la otra, con `ProposalMatcher` y el modelo de embeddings de `models/`, como `medir-fragmentos-relacionados.cs`. Lo mismo con B y con la unión.
  - Así se sabe si la unión del árbitro es más estable que cada modelo por separado. Hay un indicio: con la v0.10, la unión del catálogo de clínicas osciló entre 202 y 222 requisitos, frente a 209–291 antes.
  - **Cuenta en cada backlog las historias de la unión que juntan varias de una misma propuesta** (04-11-2026, tras el paso 104), con la comparación de cada trabajo. En los backlogs que ya había iba de un 0 % a un 57 %, y en PURE esas uniones crearon historias con dos acciones (`docs/evaluacion/metricas-externas.md`). Con 3 repeticiones del mismo catálogo se sabrá si es lo normal o variación.
  - Resultados en `docs/evaluacion/repeticiones.md`, con el bloque en palabras sencillas, y en `docs/costes/costes.md`, que se regenera.
- [ ] 120. Concilia frente a un solo modelo (27-10-2026, autor). La comparación que responde a la hipótesis. Usa los trabajos del paso 119 y la referencia del 118. Unas 5 horas de revisión, que se pueden repartir en varios días; sin coste en dinero.
  **Qué hace Claude primero:**
  - El script `scripts/evaluacion/un-solo-modelo.cs`. Antes de dártelo, Claude lo prueba con los trabajos ya guardados de «Clinicas Veterinarias v0.11», que no cuentan para los resultados.
  - Con `dotnet run scripts/evaluacion/un-solo-modelo.cs -- hojas`, prepara en `docs/evaluacion/revision/` las hojas de revisión, una por cada cosa que hay que revisar. Son CSV que se abren en Excel.
  - **Revisión a ciegas:** en las hojas no se ve si un elemento es de SOL, de LUNA o de la unión, y el orden está mezclado, para que no influya lo que esperas. El origen queda en `claves.json`, que no hace falta abrir.
  - Los elementos con el mismo texto en dos propuestas (la unión copia muchos de A) se revisan una sola vez.
  - Se revisa la **primera repetición** de cada fase, porque las otras dos ya se usan en el paso 119 para ver cuánto varía. Si da tiempo, el script prepara también las otras dos y las suma.
  **Qué haces tú: rellenar las hojas.** En cada fila ya está todo lo necesario, sin buscar en los documentos.
  1. **`trampas-preguntas.csv`.** Una fila por cada trampa abierta o resuelta y cada propuesta de preguntas (SOL, LUNA y unión). El script pone al lado las 3 preguntas que más se parecen.
     - Marca `sí` si alguna pregunta trata esa trampa y `no` si no.
     - En una resuelta, `sí` es un error, porque se pregunta lo que ya está decidido.
  2. **`trampas-catalogo.csv`.** Lo mismo con las resueltas, los huecos y lo fuera de alcance, frente a cada catálogo y con sus 3 requisitos más parecidos.
     - Marca `sí` si el catálogo lo trata como debe: recoge la decisión final o el hueco, o no incluye lo fuera de alcance.
  3. **`cobertura.csv`.** Una fila por cada funcionalidad de tu lista y cada catálogo y backlog, con los 3 requisitos o historias más parecidos.
     - Marca `sí` si está cubierta, `en parte` o `no`.
  4. **`muestra.csv`.** 30 requisitos al azar de cada catálogo (SOL, LUNA y unión; 90 en total, con semilla fija para que salgan los mismos si se repite), cada uno con el texto de los fragmentos que cita. Marca tres columnas:
     - `fiel`: `sí`, `en parte` o `no`, según lo diga su fuente;
     - `repetido`: `sí` si otro requisito del mismo catálogo dice lo mismo (el script te pone al lado el más parecido de ese catálogo);
     - `sobra`: `sí` si no es un requisito, por ejemplo una nota del proyecto o un detalle de diseño.
  5. **`discrepancias.csv`.** Las discrepancias que marcó el árbitro en la unión de la primera repetición: las suyas y las de `ArbiterDeviations`.
     - Marca `sí` si de verdad hay algo que revisar y `no` si es una falsa alarma.
     - En el paso 65 hubo entre 0 y 2 por fase, así que esta hoja es corta.
  **Qué hace Claude después:**
  - Con `dotnet run scripts/evaluacion/un-solo-modelo.cs -- calcular`, lee las hojas rellenas y `claves.json` y escribe `docs/evaluacion/un-solo-modelo.md`. Por fase, una tabla de SOL solo, LUNA solo y Concilia con:
    - el recall de las trampas y de la cobertura;
    - la precisión de la muestra (fiel, sin repetir y sin sobrar);
    - el F1, con el recall de la cobertura y la precisión de la muestra;
    - el coste en dólares y los minutos;
    - cuántas discrepancias eran de verdad.
  - Cada cifra lleva con cuántos casos se calculó. Debajo va el bloque en palabras sencillas: qué se comprobó, qué demuestra y si la diferencia de coste merece la pena.
  - **Limitaciones**, que se dicen en la memoria:
    - un solo proyecto, ficticio;
    - la referencia la hace el autor;
    - en el paso 65 la unión ya trató bien las 26 trampas, así que las trampas apenas distinguen la unión de SOL; las diferencias saldrán sobre todo de la cobertura y de la muestra;
    - pocas discrepancias.
  Adelanta al hito 23 la parte de evaluación del punto 3 de «Después» (precision, recall y F1 frente a listas de referencia). La aprobación por umbral se queda allí.
- [ ] 121. SOL ‖ SOL (27-10-2026, autor). Responde a si lo que aporta el patrón viene de usar dos modelos distintos o simplemente de hacer dos intentos. Es la configuración más cara, porque B pasa de LUNA a SOL, que cuesta unas 20 veces más: las 3 repeticiones rondarían los 30–50 $.
  **Qué haces tú:**
  1. **Cambia el modelo B.** En `.env`, pon `RUNNER_MODEL_B` a `SS - GPT-6-SOL`. Después, reinicia la API con `docker compose up -d api`, sin tareas en marcha.
  2. **Repite el paso 119** en tres proyectos nuevos, «Clínicas SOL-SOL 1», «Clínicas SOL-SOL 2» y «Clínicas SOL-SOL 3».
     - Haz **primero una sola vuelta** (preguntas, catálogo y backlog) y mira su coste en `docs/costes/costes.md` antes de lanzar las otras dos.
     - Si no compensa, se queda en una y se dice.
  3. **Rellena las hojas nuevas** que prepare el script, como en el paso 120. Solo saldrán los elementos que no se revisaron ya, porque los textos iguales se reutilizan.
  4. **Al terminar, deja `RUNNER_MODEL_B` como estaba** (vacío, para que vuelva a ser LUNA) y reinicia la API.
  **Qué hace Claude:**
  - Los mismos dos scripts, con el nombre de los proyectos SOL-SOL.
  - Una tabla más en `docs/evaluacion/un-solo-modelo.md`: SOL solo, Concilia SOL ‖ LUNA y Concilia SOL ‖ SOL, con su coste.

**Hecho cuando:** el capítulo de evaluación tiene datos de los experimentos (sesgo del árbitro, calidad con métricas externas, cambios del usuario, variación entre repeticiones, Concilia frente a un solo modelo y SOL ‖ SOL), todos con la versión congelada del paso 117 y frente a la referencia revisada por el autor, de la actualización por iteraciones (paso 116) y de al menos un documento público.

## Después

Por este orden:

1. Mejorar los prompts fase a fase sobre el flujo que ya funciona. En el catálogo, se retoma el hito 7 con los prompts v2 del autor.
2. ~~Árbitro con embeddings.~~ Pasa al hito 17 (16-10-2026).
3. Aprobación por umbral y evaluación: precision, recall y F1 frente a la versión validada y a listas de referencia hechas a mano. (27-10-2026, autor: la evaluación pasa al hito 23, paso 120; aquí queda la aprobación por umbral.)
4. Redactar la memoria: sin capítulo de Metodología; 3.1 especificación de la solución, 3.2 implementación y 4 evaluación. (Sustituido el 22-10-2026 por el autor: 1 Introducción; 2 Desarrollo, con 2.1 especificación del sistema, 2.2 construcción del sistema y 2.3 validación de la solución; 3 Conclusiones. Empezada el 04-11-2026; ver «Lo que queda, en orden», punto 14.)
5. **Mejoras futuras de las iteraciones del proyecto** (decididas con el autor el 04-11-2026, como recomendaba Claude: no se hacen antes de evaluar; van a la memoria como hallazgos y trabajo futuro):
   - **Leer todo cuando la documentación es pequeña y la lectura relacionada cuando es grande.** Con unos 26.000 tokens, leerlo todo costó 0,95 $ frente a 1,67 $ con la misma detección (paso 116); con unos 96.000, leerlo todo no cabe y solo funciona la lectura relacionada (paso 123). No se hace ahora porque solo hay una ejecución de cada caso (el coste de una misma fase varía mucho), habría que fijar un umbral que no se ha medido, y cambiar la lectura justo antes de congelar la versión (paso 117) obligaría a repetir las mediciones. La lectura relacionada funciona en todos los tamaños; leerlo todo solo ahorraría céntimos en los proyectos pequeños.
   - **Un CSV para Jira o Azure DevOps solo con lo nuevo de una iteración,** para no duplicar lo ya importado. Era la decisión abierta del hito 22; se cierra sin hacerlo porque solo sirve a quien ya importó el backlog y después lo actualiza, un caso que no entra en la evaluación.
   - **La duración real de cada repetición de una tarea:** hoy las repeticiones se guardan al final, con la hora de inicio del primer intento, y todas salen con la duración total (la del trabajo sí es real; pasos 116 y 123).
6. **Los textos de la plataforma en el idioma del proyecto** (paso 105, 04-11-2026): en un proyecto en inglés, el contenido de los modelos sale en inglés, pero no lo que pone la plataforma:
   - la narrativa de las historias («Como… quiero… para…») y «Tarea» en la web, la comparación y Gherkin;
   - los títulos, leyendas y anexos de Word y PDF;
   - el prefijo «A decidir por el cliente:», que pide así el prompt de sugerencias.
   No se hace durante la evaluación porque cambia el código (sería `evaluacion-2`), aunque no obligaría a repetir ninguna ejecución con modelos: las exportaciones salen de lo guardado.
7. **Que el árbitro del backlog no junte historias atómicas de una misma propuesta** (pasos 104 y 105, 04-11-2026). Se decide con las repeticiones del paso 119. En PURE, las 6 historias de la unión con dos acciones juntan cada una 2 o 3 historias atómicas de SOL; en los demás backlogs, el árbitro junta varias de A en una de un 0 % a un 57 % de las veces. Dos piezas:
   - **la más fiable, sin IA:** la plataforma marca como discrepancia la historia de la unión que junta varias de una misma propuesta, como ya hace con las demás desviaciones del árbitro (paso 88);
   - **en el prompt:** pedir al árbitro que no junte historias de A que ya son atómicas solo porque B tiene una que las cubre.
   No se hace durante la evaluación, porque cambiaría la versión congelada.
