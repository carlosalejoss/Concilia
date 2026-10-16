# Trazabilidad del proyecto (Concilia)

Visión de alto nivel de cómo se ha construido el TFG, del 28-09-2026 al 16-10-2026: qué se hizo en cada etapa, qué se decidió y qué cambió de criterio. El detalle de cada día está en `bitacora-detallada.md`; el estado vigente, en `CLAUDE.md` del repositorio.

## El proyecto en una frase

Plataforma web que, a partir de la documentación de un sistema, genera preguntas para el cliente, un catálogo de requisitos (EARS/MASTeR), un backlog ágil y un mockup navegable. Las tres primeras fases usan verificación cruzada: dos modelos de IA (A y B) trabajan cada tarea por separado y un árbitro compara y une sus propuestas.

- **Nombre:** ReqArbiter hasta el 13-10-2026; desde entonces, Concilia (de *conciliar* dos versiones).
- **Stack:** React 19 + TypeScript + Vite + Tailwind; .NET 10 (ASP.NET Core y EF Core); PostgreSQL; Docker Compose.
- **Modelos:** los de la empresa, a través de su pasarela LiteLLM (`SS - GPT-6-SOL`, `SS - GPT-6-LUNA`), ejecutados con Claude Code (`claude -p`) como agente.
- **Forma de trabajo:** un plan por hitos y pasos (`docs/plan-de-construccion.md`), un commit por paso, y un resumen de bitácora al final de cada sesión. El autor decide; Claude Code propone, implementa y documenta.
- **Trazabilidad:** hasta el 15-10-2026 el resumen de cada sesión se daba en el chat y el autor lo llevaba a su proyecto de Claude. Desde ese día, Claude Code lo escribe directamente en `docs/bitacora-detallada.md` y mantiene al día este fichero.

## Línea de tiempo

| Fechas | Etapa | Resultado |
| --- | --- | --- |
| 28-09 | Hitos 0 a 3 | Repositorio, esqueleto en Docker, subida de archivos y primera llamada a un modelo desde la web |
| 29-09 | Hitos 3 y 4, inicio del 5 | Runner real con la pasarela; A y B en paralelo; prompt de requisitos v1.2 portado a C# |
| 30-09 | Cierre del hito 5 | Primer catálogo real; prueba con un documento grande que revela el exceso de requisitos |
| 01-10 y 02-10 | Hito 6, catálogo v1.3 | Menos de la mitad de requisitos y casi sin reparaciones; historial del prompt para la memoria |
| 05-10 | Prompt v1.4 y propuesta del catálogo v2 | El proceso por lotes toca techo; se cierra el hito 6 |
| 08-10 | Iteración v0, hito 8 | Cambio de rumbo: flujo completo con prompts básicos; base A ‖ B → árbitro |
| 09-10 | Hitos 9 a 12 y 12b | Preguntas, catálogo, backlog y mockup; primera ejecución completa; lectura por partes |
| 13-10 | Hito 14 y ajustes | Interfaz nueva; mockup por partes; flujo completo con el documento B; nombre Concilia |
| 14-10 | Hitos 15 y 16 | Más tipos de archivo, referencia de estilo, mockup como aplicación, árbitro con documentos grandes |
| 15-10 | Mockup v0.8 y v0.9, hito 13 | Mockup navegable de verdad; test del flujo completo, informes y proyectos de ejemplo |
| 15-10 y 16-10 | Paso 65 con el proyecto grande de ejemplo | Catálogo, backlog y mockup de 38 pantallas; una tarea cada vez en toda la plataforma |

## Etapas

### 1. Cimientos (28-09, hitos 0 a 2)

- **Hito 0 (antes de la conversación con Claude Code):** repositorio, herramientas, credenciales fuera del repositorio y prueba de humo de `claude -p` contra la pasarela. El autor aporta el prompt v1 del catálogo con su esquema y validador en Python.
- **Hito 1:** solución .NET con API, Core y tests; web con Vite y React; Docker Compose con `db`, `api` y `web`; PostgreSQL con EF Core; integración continua mínima.
- **Hito 2:** subida de archivos con extracción de texto de `.txt`, `.md`, `.docx` y `.pdf`.
- **Decisión:** PostgreSQL como base de datos.

### 2. El runner y los dos modelos (28-09 y 29-09, hitos 3 y 4)

- **Runner:** proceso hijo `claude -p` con el prompt por la entrada estándar, salida en stream-json, latido, tiempo máximo y veredicto final obligatorio (`VEREDICTO: {...}`). Proveedores `Fake` (para tests) y `LiteLlm`.
- **Seguridad:** entorno construido de cero para cada ejecución, `CLAUDE_CONFIG_DIR` vacío, sin heredar credenciales; la clave y la dirección de la pasarela nunca llegan a logs, eventos ni base de datos.
- **Hallazgo y cambio de criterio:** `--allowedTools` no bastaba (SOL llegó a usar `Bash`); desde entonces se pasan también `--tools "Read,Write"` y `--disable-slash-commands`. Además de cerrar el agujero, el consumo de entrada bajó mucho.
- **Hito 4:** la misma tarea a SOL y LUNA en paralelo, con consolas en vivo lado a lado.
- **Dato verificado:** ventana de 128k tokens de entrada (`CLAUDE_CODE_MAX_CONTEXT_TOKENS=128000`).

### 3. El catálogo de requisitos por lotes (29-09 a 05-10, hitos 5 y 6)

- **Hito 5:** prompt v1.2 (del autor), fragmentador y validador portados a C#, documentación dividida en lotes, hasta 5 lotes a la vez por modelo, reparación con los errores del validador y unión de los lotes. Con el ejemplo ReservaSalas, A y B dan catálogos válidos de 13 requisitos.
- **Problemas resueltos:** LUNA agotaba los 32k de salida razonando y su `Write` llegaba cortado → tope de salida a 64k y corte inmediato de una llamada a herramienta incompleta. Se verifica que la pasarela acepta más de 32k de salida.
- **Prueba con un documento técnico interno grande (v1.2):** 1.133 y 862 requisitos, más de la mitad procedentes de secciones que no son especificación, y entre el 46 % y el 72 % de la salida gastada en intentos fallidos.
- **Decisiones del autor (01-10), que dan lugar a la v1.3:**
  - A y B son independientes: el prompt no menciona al otro modelo ni al árbitro (**cambio de criterio:** antes se buscaba que «ambos modelos corten igual»);
  - ids por tipo de requisito (`RF`, `RNF`, `RA`, `RT`, `RI`); la prioridad queda fuera;
  - menos requisitos, menos preguntas y menos reparaciones, manteniendo EARS y MASTeR.
- **Catálogo v1.3 (02-10):** un requisito por capacidad con criterios de aceptación etiquetados, inventario de funcionalidades («cobertura primero»), descartes de diseño y de notas de proyecto, normalizador de la plataforma, rechazo por requisito con aceptación parcial del lote y una sola reparación. Con el documento grande: 456 y 284 requisitos (−60 % y −67 %), y la salida perdida baja al 3 % y al 10 %.
- **`docs/evolucion-prompt-requisitos.md`:** historial de versiones del prompt con *qué hace*, *resultados* y *qué mejorar*; desde el 02-10 se actualiza en cada cambio del prompt. Es material directo para la memoria.
- **v1.4 (05-10):** 255 y 214 requisitos con la mitad de tokens, pero todavía lejos de algo revisable. **Conclusión:** el límite está en el proceso por lotes; se cierra el hito 6.
- **Propuesta del catálogo v2 (hito 7, 05-10):** de arriba abajo, primero un inventario global y después requisitos por funcionalidad. El autor descarta fijar un número objetivo de requisitos. Se implementan sus validadores y la unión, pero el hito queda **aplazado** el 08-10.

### 4. Cambio de rumbo: la iteración v0 (08-10 a 09-10, hitos 8 a 12)

- **Decisión del autor (08-10):** antes de pulir el catálogo, que el flujo entero funcione con prompts muy básicos: proyecto → archivos → rondas de preguntas → catálogo → validación → backlog → validación → mockup.
- **Decisiones de la v0:**
  - preguntas, catálogo y backlog con A ‖ B → árbitro; árbitro `SS - GPT-6-SOL`, solo LLM (los embeddings, más adelante), con las propuestas anónimas y en orden aleatorio;
  - sin umbral numérico de discrepancia: el árbitro marca `discrepancia` y el usuario revisa al validar;
  - backlog épica → historia → criterios Gherkin → tarea (**cierra** la decisión abierta sobre el nivel feature);
  - el catálogo exige al menos una ronda terminada y ninguna pregunta pendiente; las fases siguientes solo ven la versión validada;
  - mockup con un solo modelo; con la suscripción del autor solo para ese rol (**cambio de criterio:** antes estaba prohibido cualquier proveedor de suscripción).
- **Hito 8:** tarea de fase genérica, ejecución A ‖ B → árbitro, estado del flujo y página por pasos.
- **Hitos 9 a 11:** preguntas (contestar, descartar, reabrir) con sugerencias de respuesta de la IA (un solo modelo, con el origen registrado: `usuario`, `ia`, `ia-editada`); catálogo y backlog como entregables que se editan y se validan en la web.
- **Primera ejecución completa con ReservaSalas (09-10).** **Hallazgo:** aceptar sin revisar las sugerencias de la IA casi duplica el catálogo y mete datos que el documento no tiene.
- **Hito 12:** mockup en un `.html` autocontenido, mostrado en un iframe aislado.
- **Hito 12b, lectura por partes (propuesta del team leader, aprobada por el autor):** la documentación ya no va dentro del prompt; el modelo la lee por tramos desde `input/` y la plataforma comprueba que no deja ninguna línea sin leer (**cambio de criterio:** sustituye «toda la documentación en una llamada»). Prompts v0.1 a v0.3: plan de lectura con `offset` y `limit` exactos e ids numerados para el árbitro.

### 5. Prueba con un documento real e interfaz (13-10, hito 14)

- **Autorización del autor** para usar un documento técnico-funcional interno («documento B»), anonimizado en la documentación.
- **Flujo completo con el documento B:** 35 preguntas, 178 requisitos, 53 historias y mockup. **Hallazgos:** leer por partes cuesta un 50 % más con documentos que caben enteros; el árbitro es el run más caro; el mockup con la suscripción gasta casi la mitad del límite de 5 horas.
- **Mockup v0.4:** se escribe por partes (base más un archivo por épica) que la plataforma junta.
- **Hito 14:** interfaz nueva con barra lateral, barra de pasos, modo oscuro y un plan de diseño propio; después, borrar proyectos, buscador, portada, lector de documentos y una pantalla de preguntas más ligera.
- **Nombre:** Concilia. Se conservan el nombre de la base de datos y del proyecto de Docker para no perder datos.

### 6. Ampliación y mockup como aplicación (14-10 y 15-10, hitos 15 y 16)

- **Hito 15:** documentación en `.xlsx`, `.csv`, `.pptx`, `.html`, `.eml`, `.msg`, `.vtt`, `.srt`, `.json`, `.yaml` y `.xml`, con vista previa por tipo. Un `.zip` en Documentos es la referencia de estilo del mockup (el modelo saca el estilo él solo y la plataforma incrusta las imágenes).
- **La web no avanza sola de paso:** solo lo cambia el usuario.
- **Árbitro con documentos grandes (prompts v0.7):** propuestas con un elemento por línea y consulta de la documentación por tramos, porque el árbitro se quedaba sin contexto.
- **Cambio de criterio:** si A o B fallan, el trabajo falla; ya no se acepta la salida de un solo modelo «sin verificación cruzada».
- **Runner más robusto:** detiene una ejecución que empieza a compactar con la salida ya escrita y acepta una salida válida escrita antes de agotar el tiempo; la consola se guarda en disco.
- **Modelos:** `SS - GPT-6.1-SOL` resulta demasiado lento en respuestas largas; se vuelve a SOL 6.0 para A, B y el mockup.
- **Mockup v0.6 a v0.9 (hito 16):** un único prototipo de toda la aplicación, construido en varias ejecuciones (diseño, una por pantalla y correcciones). La revisión del conjunto pasa a hacerla la plataforma (**cambio de criterio:** con 19 pantallas no cabía en el contexto de un modelo), se puede reanudar un mockup fallido y, en la v0.9, es una aplicación navegable con inicio de sesión y usuarios de prueba, no un índice de pantallas. Resultado con el documento C: 25 pantallas, 866 KB, unos 40 minutos; el autor lo da por bueno. Sin máximo de pantallas (aviso por encima de 25) y hasta 10 MB.

### 7. Hito 13: flujo de punta a punta y evaluación (15-10, en curso)

- **Paso 63:** test automático del flujo completo a través de la API, de los documentos al mockup.
- **Paso 64:** informe automático por trabajo (cuánto aporta cada modelo, qué cambia el usuario al validar, tokens y tiempos) y CSV por proyecto: la base del capítulo de evaluación.
- **Paso 66:** README y documentación al día.
- **Preparación del paso 65:** dos proyectos ficticios de análisis (mediano: banco de alimentos, 7 archivos; grande: red de clínicas veterinarias, 21 archivos) en formatos variados, con contradicciones y huecos puestos a propósito como referencia para evaluar las preguntas.
- **Trazabilidad:** bitácora detallada y este fichero, recuperados de toda la conversación, en `docs/` y mantenidos al día en cada sesión.
- **Primer catálogo del proyecto grande de ejemplo:** 299 y 192 requisitos, unión de 209 en 12 minutos. A raíz de él, la plataforma quita la marca `sigue` de las citas (evita una reparación), el informe avisa cuando el consumo es un mínimo (las ejecuciones detenidas no lo dan) y la consola deja de decir «Failed» al detener una ejecución con la salida ya escrita.
- **Mockup del proyecto grande de ejemplo:** 38 pantallas con las 109 historias (1,54 MB). La P34 falló por un 504 puntual de la pasarela y se recuperó al reanudar al día siguiente (16-10), aprovechando las otras 37.
- **Una tarea cada vez en toda la plataforma:** mientras se ejecuta una, la API rechaza cualquier otro lanzamiento y la web apaga los botones de lanzar y dice qué tarea está en marcha y en qué proyecto.
- **Preguntas:** botón «Aprobar todas las sugerencias» de una ronda, visible solo cuando la IA ha sugerido la respuesta de todas sus preguntas pendientes.

## Evolución de los prompts

| Versión | Fecha | Idea principal |
| --- | --- | --- |
| Catálogo v1 a v1.2 | 28-09 y 29-09 | Prompt del autor con esquema, fragmentos citados y validador estricto |
| Catálogo v1.3 | 02-10 | Independencia de A y B, un requisito por capacidad, inventario, normalizador y aceptación parcial |
| Catálogo v1.4 | 05-10 | Ajustes sobre la v1.3; el proceso por lotes toca techo |
| Catálogo v2 | 05-10 (aplazado) | De arriba abajo: inventario global y requisitos por funcionalidad |
| v0 | 08-10 | Prompts básicos para las cinco fases del flujo |
| v0.1 a v0.3 | 09-10 | Lectura de la documentación por partes, con un plan de lectura exacto |
| v0.4 | 13-10 | Mockup por partes |
| v0.5 | 14-10 | Referencia de estilo del mockup |
| v0.6 y v0.7 | 14-10 | Mockup como una sola aplicación; árbitro que consulta la documentación por tramos |
| v0.8 y v0.9 | 15-10 | La plataforma junta y revisa el mockup; mockup navegable con inicio de sesión |

El detalle y las mediciones de cada versión están en `docs/evolucion-prompt-requisitos.md`.

## Decisiones que siguen abiertas

- Origen de los embeddings y umbral de «discrepancia grave», para el árbitro con embeddings.
- Si se paran las ejecuciones que compactan varias veces antes de escribir su salida.
- Que un reinicio de la API cierre los trabajos a medias para poder reanudarlos.
- Si la plataforma corrige las citas que son números de línea (LUNA) cuando la línea es inequívoca.
- Si se estima el consumo de las ejecuciones detenidas antes del final.
- Si la plataforma quita las etiquetas de documento (`<!doctype>`, `<html>`…) de una pantalla del mockup en lugar de pedir una reparación.
- Abaratar las pantallas del mockup y un posible historial de mockups.
- Prompt de sugerencias más conservador y marcar en el catálogo lo que viene de una respuesta de la IA.

## Siguientes pasos

1. **Paso 65:** ejecutar los dos proyectos de ejemplo, analizar sus CSV y cerrar el hito 13.
2. Retomar el hito 7 (catálogo v2) como mejora del prompt del catálogo.
3. Árbitro con embeddings.
- **Aparcado (15-10-2026):** rehacer el mockup del proyecto grande de ejemplo con la suscripción del autor (Sonnet 5.5) y la skill `frontend-design`, para ver si mejora la calidad; se retoma tras el paso 65. Riesgos: agotar el límite de 5 horas de la suscripción (se puede reanudar) y cambiar dos cosas a la vez, el modelo y las skills.
4. Redactar la memoria: sin capítulo de Metodología; 3.1 especificación de la solución (ciclo de vida de alto nivel), 3.2 implementación (tripas y prompts) y 4 Evaluación.

## Cifras de seguimiento

- **Commits:** 149 entre el 28-09 y el 16-10-2026.
- **Tests de C#:** 289 (30-09), 339 (02-10), 620 (08-10), 759 (13-10) y 830 (15-10).
