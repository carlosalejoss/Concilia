# Trazabilidad del proyecto (Concilia)

Visión de alto nivel de cómo se ha construido el TFG, del 28-09-2026 al 26-10-2026: qué se hizo en cada etapa, qué se decidió y qué cambió de criterio. El detalle de cada día está en `bitacora-detallada.md`; el estado vigente, en `CLAUDE.md` del repositorio.

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
| 15-10 y 16-10 | Paso 65 con el proyecto grande de ejemplo | Flujo completo (unos 105 min y al menos 5,2 M tokens de entrada, el 63 % en el mockup); las 26 contradicciones puestas a propósito se tratan como se esperaba; A propone mucho más que B y la unión se parece a A |
| 19-10 | Hito 17, paso 86 | Servicio de embeddings en local: el modelo multilingüe en el contenedor de la API, con los mismos vectores que sentence-transformers |
| 19-10 | Hito 18, pasos 90 a 94 | Reparar editando, trabajos interrumpidos cerrados, parar las ejecuciones que compactan sin escribir, sugerencias prudentes y estilo propio del mockup (prompts v0.11) |
| 19-10 | Hito 18, paso 95 y costes | Estimación del consumo de las ejecuciones sin dato y tablas de coste en tiempo y dinero por fase y versión |
| 19-10 | Hito 18, paso 89 | La plataforma corrige sola cuatro errores de formato que hacían fallar a LUNA, con un aviso |
| 19-10 | Hito 17, paso 88 | Árbitro con embeddings (prompts v0.10): parte del emparejamiento de la plataforma y lo que hace en contra de los números se marca como discrepancia |
| 19-10 | Hito 17, paso 87 | Emparejamiento de A y B por significado; umbrales 0,90 y 0,70 calibrados con 48 pares etiquetados por el autor; el árbitro, medido por primera vez frente a un criterio humano |
| 20-10 | Hito 19, paso 96 | El catálogo se descarga en Word y en PDF, con sus fuentes y preguntas en anexos |
| 22-10 | Hito 19, paso 97 | El backlog se descarga como ficheros `.feature` de Gherkin, comprobados con el analizador oficial |
| 22-10 | Hito 19, paso 98 | El backlog se descarga como CSV para importarlo en Jira o en Azure DevOps; el hito 19 queda hecho |
| 22-10 | Hito 18b, pasos 107 y 108 | El árbitro del catálogo consulta solo las fuentes que necesita y el del backlog cita por su id los criterios que conserva (prompts v0.12): menos de la mitad de coste en el catálogo y el doble de criterios en el backlog |
| 22-10 | Hito 18, prueba real | Flujo completo de clínicas con los prompts v0.11 sin errores internos y con reparaciones que caben; mockups sin `.zip` con estilos distintos en dos proyectos; el hito 18 queda completado |
| 23-10 | Hito 21, paso 101 | Cada versión del catálogo, el backlog y el mockup se guarda con su fecha y su origen, y se ve en un historial |
| 23-10 | Hito 21, paso 102 | Cada versión se puede ver, comparar con la actual y recuperar; el hito 21 queda hecho |
| 26-10 | Cierre del hito 21 | El autor revisa en el navegador el historial de versiones (ver, comparar y volver, también del mockup): funciona todo |
| 26-10 | Diseño del hito 22 | Iteraciones del proyecto: se acuerdan con el autor las decisiones y los pasos 109 a 116, sin código; la evaluación pasa a ser el hito 23 |
| 26-10 | Hito 22, paso 109 | La documentación se agrupa en iteraciones que se cierran con «Cerrar documentación»; los proyectos que ya existen quedan igual que antes, en su «Iteración 1» |
| 26-10 | Hito 22, paso 110 | Ids estables en el catálogo y el backlog: guardar ya no renumera, borrar deja hueco y un número no se vuelve a dar |
| 26-10 | Hito 22, paso 111 | Cada pregunta, requisito e historia dice de qué iteración sale, con filtro; cada entregable, qué iteraciones incluye y cuáles le faltan; Word, PDF y los CSV llevan la iteración |
| 05-11 | Hito 23, paso 106 | La prueba como analista con ReservaSalas (cierra los pasos 49 y 53): Claude responde como cliente y genera; el autor revisa el catálogo y el backlog y no cambia nada en unos 19 minutos. Las 9 discrepancias eran DEBERÍA frente a DEBERÁ. La segunda ronda no repite preguntas. 1,11 $ |
| 04-11 | Hito 23, paso 104 | Métricas externas sobre lo del paso 105: AQUSA y Vale con `vale-ears`. Tras revisar cada aviso, SOL escribe más atómico que LUNA (historias con dos acciones: 6 frente a 21 por cada 100) y la unión queda entre los dos (18), porque el árbitro junta historias de SOL. Siete de cada diez avisos de Vale son falsas alarmas |
| 04-11 | Hito 23, paso 105 | El flujo completo en inglés con un documento público de PURE (Microcare), sin editar: todo a la primera, 46 minutos y 9,29 $ (6,81 $ el mockup). Los textos fijos de la plataforma siguen en español: mejora futura |
| 04-11 | Hito 23, paso 117 | Versión congelada para evaluar (`docs/evaluacion/version.md`): código del paso 122, prompts v0.12 y v0.13, Claude Code 2.1.283. Antes, la ronda de preguntas normal en inglés con el documento de PURE del paso 105 (Microcare): válida a la primera por 0,50 $, sin cambios en el código |
| 03 y 04-11 | Hito 22, paso 123 | La lectura relacionada con una documentación grande (un documento público de PURE, unos 96.000 tokens, en inglés): leerlo todo no cabe en el contexto y falla; la lectura relacionada funciona a la primera por 0,87 $. Primera ronda del flujo en inglés |
| 03-11 | Hito 22, paso 122 | Las mejoras de la prueba real: nginx espera 5 minutos al lanzar tareas, la plataforma renumera las preguntas centradas que empiezan en otro número y el error de esquema dice su línea en la reparación |
| 27-10 y 03-11 | Hito 22, paso 116 | Prueba real con una segunda iteración de clínicas: los dos caminos detectan todos los cambios puestos a propósito; actualizar conserva el 91 % de los requisitos y el 95 % de las historias validadas (desde cero, el 0 %), por un coste parecido; leerlo todo salió más barato que la lectura relacionada. Un fallo del paso 112 arreglado (`REF-PREG` con las preguntas enlazadas) |
| 27-10 | Hito 22, paso 115 | Generar desde cero, frente a lo anterior: cada requisito o historia generado dice si es igual, cambiado o nuevo respecto a la versión de antes, por significado, y lo que desapareció se lista aparte |
| 27-10 | Hito 22, paso 114 | Actualizar el backlog validado con lo que cambió en el catálogo: la plataforma calcula el impacto sin IA y los modelos solo trabajan sobre él; lo modificado o eliminado lo revisa el usuario |
| 27-10 | Hito 22, paso 113 | Actualizar el catálogo validado con una iteración nueva: un conjunto de cambios; lo nuevo se añade y lo modificado o eliminado lo revisa el usuario, con el historial de cada requisito |
| 26-10 | Hito 22, paso 112 | Rondas de preguntas centradas en una iteración nueva (prompts v0.13), con el tope de lo relacionado medido con clínicas; los modos de siempre, vigilados por una instantánea de la v0.12 |
| 27-10 | Hito 23, nuevos pasos (sin código) | Al repasar la propuesta del TFG se ve que ningún paso medía la hipótesis; el autor añade los pasos 117 a 121: versión congelada, referencia revisada por el autor, repeticiones, Concilia frente a un solo modelo y SOL ‖ SOL |
| 04-11 | Memoria, capítulo 1 | Introducción redactada en `memoria/tfg/memoria.tex` (fuera de git): objetivos O1 a O6, alcance, contexto, métodos y herramientas, con 18 referencias comprobadas |
| 04-11 | Memoria, capítulo 2 (2.1 y 2.2) | Especificación sobre el diagrama de alto nivel y construcción con los diagramas de despliegue, estados y secuencia, comprobados contra el código; la 2.3, vacía hasta que el autor diga qué quiere |
| 04-11 y 05-11 | Memoria: revisión como tribunal | Resumen, capturas reales de la web, lista de acrónimos, comparación con otras aproximaciones y 14 anexos (instalación, cronología, decisiones, modelo de datos, API, prompts, validaciones, calibración, pruebas, proyectos de ejemplo, coste, incidencias y ejemplos de entregables) |
| 05-11 | Repositorio: solo el autor en los commits | Historial reescrito sin la línea `Co-Authored-By` de Claude (mismo contenido, autor y fechas); hashes citados actualizados (`evaluacion-1`: `bbe1167`, código del paso 122: `f063fd6`); `attribution` vacío en `.claude/settings.json` |
| 04-11 | Memoria, lista de contenido | `docs/contenido-memoria.md`: lo que tiene peso en el proyecto y debe estar en la memoria, por secciones, con lo imprescindible marcado; en el borrador faltan el experimento del sesgo, la versión congelada y las opciones de lectura de las iteraciones |
| 06-11 | Hito 24, el backlog en GitHub (paso 124, en la rama `feat/github-export`) | GitHub no importa archivos: un `.zip` con un script que crea los issues con `gh`, como sub-issues; al repetirlo crea lo nuevo y avisa del resto. Prueba real con ReservaSalas: 67 issues con su jerarquía en 5 minutos, y repetir no crea nada |
| 06-11 a 09-11 | LasCuarenta, proyecto universitario del autor, por el flujo completo (caso adicional, no sustituye a los pasos 118 a 121) | 13 rondas, 142 requisitos y 40 historias, sin reparaciones ni cambios del autor al validar; 21,95 $ y 80 minutos de modelos (el 71 % en preguntas y sugerencias). El tiempo del autor no se midió (unas 3–4 horas por su estimación, sin mockup; solo leyó las discrepancias). Ficha en `docs/evaluacion/lascuarenta.md`. |
| 10-11 | Cambio de rumbo: el modelo B por OpenCode Go (sin código) | LUNA no tiene capacidad suficiente: B pasa a un modelo de otro fabricante con OpenCode Go (solo B; lo demás sigue en SOL). La evaluación 1 se interrumpe y se repetirá desde cero (hitos 25 a 27). Todo en `main`: rama de GitHub integrada y borrada, historia antigua guardada en un `.bundle`. El coste para una empresa, parte central de la memoria |
| 20-10 | Documentación de la API (fuera del plan) | Swagger UI con los 42 endpoints documentados, que se actualiza sola y que los tests mantienen completa |

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

### 7. Hito 13: flujo de punta a punta y evaluación (15-10 y 16-10, completado)

- **Paso 63:** test automático del flujo completo a través de la API, de los documentos al mockup.
- **Paso 64:** informe automático por trabajo (cuánto aporta cada modelo, qué cambia el usuario al validar, tokens y tiempos) y CSV por proyecto: la base del capítulo de evaluación.
- **Paso 66:** README y documentación al día.
- **Preparación del paso 65:** dos proyectos ficticios de análisis (mediano: banco de alimentos, 7 archivos; grande: red de clínicas veterinarias, 21 archivos) en formatos variados, con contradicciones y huecos puestos a propósito como referencia para evaluar las preguntas.
- **Trazabilidad:** bitácora detallada y este fichero, recuperados de toda la conversación, en `docs/` y mantenidos al día en cada sesión.
- **Paso 65 cerrado (16-10):** flujo completo con los dos proyectos de ejemplo; resultados en `docs/resultados-paso-65/`. Mediano: 2,23 M tokens de entrada en 55 minutos, sin reparaciones. Grande: al menos 5,18 M en 105 minutos. Las 38 contradicciones y huecos se tratan como se esperaba, el mockup es el 63–70 % del coste y la unión se parece a A.
- **Primer catálogo del proyecto grande de ejemplo:** 299 y 192 requisitos, unión de 209 en 12 minutos. A raíz de él, la plataforma quita la marca `sigue` de las citas (evita una reparación), el informe avisa cuando el consumo es un mínimo (las ejecuciones detenidas no lo dan) y la consola deja de decir «Failed» al detener una ejecución con la salida ya escrita.
- **Mockup del proyecto grande de ejemplo:** 38 pantallas con las 109 historias (1,54 MB). La P34 falló por un 504 puntual de la pasarela y se recuperó al reanudar al día siguiente (16-10), aprovechando las otras 37.
- **Una tarea cada vez en toda la plataforma:** mientras se ejecuta una, la API rechaza cualquier otro lanzamiento y la web apaga los botones de lanzar y dice qué tarea está en marcha y en qué proyecto.
- **Preguntas:** botón «Aprobar todas las sugerencias» de una ronda, visible solo cuando la IA ha sugerido la respuesta de todas sus preguntas pendientes.
- **Resultados del proyecto grande de ejemplo (16-10):** el flujo completo trata como se esperaba las 26 contradicciones y huecos puestos a propósito. Pero la unión del árbitro se parece mucho a la propuesta de A, lo que abre la pregunta clave de la sección siguiente.

### 8. Hito 17: embeddings en local (19-10, completado)

- **Paso 86, servicio de embeddings en la API:**
  - **Modelo:** `paraphrase-multilingual-MiniLM-L12-v2` en fp32 (decisión del autor, frente al cuantizado), con ONNX Runtime y su tokenizador SentencePiece, dentro de la API.
  - **Cómo se calcula:** el vector de un texto es la media de los vectores de sus tokens, como en sentence-transformers, y la similitud entre dos textos es el coseno de sus vectores.
  - **De dónde sale el modelo:** la imagen de Docker lo descarga al construirse, con el commit de Hugging Face fijado y su SHA-256 comprobado. En marcha no envía nada fuera.
- **Comprobado frente a Python:**
  - con 14 textos de prueba, los mismos ids de tokens que Hugging Face y una diferencia máxima de 9,4·10⁻⁷ con los vectores de sentence-transformers;
  - el mismo requisito en español y en inglés da 0,84–0,89, y requisitos distintos, 0,06–0,29;
  - 500 textos tardan unos 6 s en CPU.
- **Para la memoria, en palabras sencillas:**
  - **Qué se calcula:** un embedding convierte un texto en 384 números que sitúan su significado en un «mapa». Las frases que dicen lo mismo quedan cerca aunque no compartan palabras ni idioma.
  - **Cómo se comprobó:** este modelo se usa normalmente desde Python, con las bibliotecas de sus autores (sentence-transformers). Se calcularon en Python los vectores de 14 frases de prueba, y el código en C# de la plataforma da los mismos números, con diferencias de menos de una millonésima.
  - **Qué demuestra:** la implementación es fiel al modelo original.
  - **Es repetible:** la comprobación es un test automático, y la referencia se puede regenerar con `tests/fixtures/embeddings/generar_referencia.py`.
- **Decisión del autor:** los tests que necesitan el modelo (470 MB, fuera de git) se saltan si no está, también en GitHub Actions.

- **Paso 87, emparejamiento de A y B por significado (sin LLM):**
  - **Cómo funciona:** cada elemento de A se compara con todos los de B (y al revés) y se une con el más parecido. Según la similitud, el par es «lo mismo», «parecido» o «distinto»; varios elementos pueden unirse con el mismo, como hace el árbitro. Endpoint `GET /jobs/{id}/matching`.
  - **Decisión del autor: dos umbrales,** no uno. Por debajo del bajo, el elemento solo está en su propuesta; entre los dos, se parece pero no es lo mismo, y es la franja candidata a «discrepancia grave» en el paso 88.
  - **Decisión del autor: cómo calibrarlos (opción D)** entre cuatro opciones:
    - A, un árbitro; B, el consenso de varios árbitros; C, una muestra etiquetada por el autor; D, C y B juntas;
    - se descartó usar solo el árbitro porque sería circular: los embeddings sirven para comprobar al árbitro y no se pueden calibrar con él;
    - en la D, el criterio del autor fija los umbrales y el consenso de los árbitros los comprueba;
    - solo con proyectos ficticios.
  - **Muestra:** 48 pares de los proyectos ficticios, 8 por tramo de similitud, etiquetados a mano por el autor como «mismo» (9), «parecido» (17) o «distinto» (22), sin ver la similitud ni lo que hizo el árbitro.
  - **Decisión del autor (19-10-2026): «lo mismo» desde 0,90 y «parecido» desde 0,70.** Los intervalos con más aciertos eran 0,89–0,92 y 0,70–0,72. Así se cierra la elección de los umbrales; cómo marcan la discrepancia grave se decide en el paso 88.
  - **Resultados** (`docs/calibracion-embeddings/resultados.md`):
    - **Frente al criterio del autor:** coinciden en 37 de 48 pares con las tres etiquetas (77 %). Por fase, preguntas 17 de 18, historias 12 de 16 y requisitos 8 de 14.
    - **Frente al consenso de los árbitros de clínicas** (5 arbitrajes del catálogo y 6 de las preguntas): de lo que juntan casi todos los árbitros, los embeddings ven «mismo o parecido» el 99 % en el catálogo y el 82 % en las preguntas. Lo que los árbitros dejan separado en el catálogo cae casi siempre en la franja «parecido», la franja dudosa.
    - **El árbitro frente al criterio del autor (resultado nuevo):** juntó los 9 pares que el autor marcó como «mismo» y solo 2 de los 22 «distintos». Junta también 12 de los 17 «parecidos», conservando el detalle de cada uno.
    - **Acuerdo por elemento con el árbitro** («¿está también en la otra propuesta?») en los 9 trabajos originales de los proyectos ficticios: 89 %.
  - **Cambio de criterio:** un elemento cuenta como presente en la otra propuesta si su par es «mismo» o «parecido», no solo «mismo», porque los datos muestran que el árbitro también junta lo parecido.
  - **Dónde fallan los embeddings:** con textos que comparten casi todas las palabras pero dicen otra cosa (similitud de 0,91 con dos requisitos que el autor vio distintos), y con lo mismo dicho en estilo telegráfico (0,69 con dos que el autor vio iguales).
- **Para la memoria, en palabras sencillas:**
  - **Qué se hizo:** la plataforma compara sola, sin preguntar a ningún modelo de IA, qué propuso cada modelo y decide qué dicen lo mismo.
  - **Cómo se eligió el criterio:** para saber desde qué parecido dos textos «dicen lo mismo», el autor clasificó a mano 48 pares sin ver los números, y se eligieron los umbrales que mejor coinciden con su criterio (0,90 y 0,70).
  - **Qué se comprobó:** que el resultado coincide con lo que deciden casi todos los árbitros.
  - **Resultado añadido:** de paso, se midió por primera vez al árbitro contra un criterio humano. Junta todo lo que el autor ve igual y casi nada de lo que ve distinto.

- **Paso 88, árbitro con embeddings (prompts v0.10):**
  - **Cómo funciona:** antes de llamar al árbitro, la plataforma empareja las dos propuestas y le da el resultado en `input/emparejamiento.txt` (`1:RF-003 2:RF-001 mismo 0.94`). El árbitro parte de ahí: los «mismo» se unen, los «parecido» los decide él, y los «sin par» siguen las reglas de siempre. Es el diseño original del TFG.
  - **Decisiones del autor, entre las opciones que se le explicaron:**
    1. **El emparejamiento es orientativo, no obligatorio:** el árbitro puede apartarse si la fuente lo justifica, y la plataforma le enseña al usuario dónde lo hizo. Se descartó obligarlo, porque los embeddings también se equivocan, y pasarlo solo como pista, porque no se comprobaría nada.
    2. **Qué marca la «discrepancia grave»** (cierra la decisión abierta desde el principio): lo que el árbitro hace en contra de los números, es decir, unir algo que no tiene nada «mismo» ni «parecido» en la otra propuesta o dejar separado un par «mismo». Se suma a la discrepancia que marca el propio árbitro, que se mantiene porque los embeddings no ven contradicciones («1 hora» y «24 horas» salen «mismo»). Se descartó marcar toda la franja «parecido»: habrían sido 91 de 209 requisitos en el catálogo grande.
    3. **Las preguntas reciben el emparejamiento sin marcar nada:** no tienen discrepancia, y las desviaciones solo se cuentan en el informe.
  - **Estimación con los trabajos de antes:** entre 0 y 10 discrepancias de la plataforma por trabajo, y 21 en el backlog del proyecto grande.
  - **Cambio de criterio:** sustituye al «árbitro solo con el LLM» de la iteración v0 (08-10-2026).
  - **Prueba real (19-10-2026, lanzada por el autor):** 3 nuevos arbitrajes de SOL v0.10 sobre las mismas propuestas del catálogo de clínicas.
    - **La unión es más estable:** 202, 213 y 222 requisitos, frente a 209, 220 y 291 con la v0.9. Con 3 ejecuciones por versión es una señal, no una prueba.
    - **Las desviaciones no bajan:** unas 8 por ejecución, frente a las 7 que la plataforma habría marcado en la v0.9. El árbitro discrepa de los números en los mismos puntos aunque los tenga delante, así que son diferencias de criterio, y tiene sentido que las revise el usuario.
    - **Ninguna discrepancia del propio árbitro:** lo que el usuario revisaría (unos 8 de 212 requisitos) sale todo de la plataforma.
    - **Fiabilidad:** una de las tres ejecuciones necesitó una reparación. El primer intento agotó sus turnos sin escribir la salida, con unos 1,15 M tokens de entrada.
    - **Sin dato de consumo:** las ejecuciones válidas se detuvieron al compactar, igual que en el experimento.
- **Para la memoria, en palabras sencillas:** ahora el árbitro no empieza de cero. La plataforma le dice primero qué elementos de los dos modelos se parecen, con un criterio fijo y medido. El árbitro decide y redacta, y si contradice a los números, el usuario lo ve y lo revisa.

### 9. Hito 18: fiabilidad y calidad (19-10; completado el 22-10 con su prueba real)

- **Paso 89, correcciones automáticas de formato:**
  - **Por qué:** como árbitro del catálogo, LUNA terminó 2 de 6 intentos, y todos sus fallos fueron de formato, no de criterio. Cada uno obliga a una reparación, que en un catálogo grande es cara o no cabe en el contexto.
  - **Decisión del autor:** que la plataforma corrija sola, con un aviso, los errores con una única solución evidente, como ya hacía con las claves repetidas y la marca `sigue`:
    - una cita que es un número de línea pasa a ser el fragmento de esa línea, si es del mismo documento;
    - un id cuyo prefijo no coincide con su tipo hace renumerar el catálogo por tipo;
    - un elemento olvidado por el árbitro se añade donde está su par «mismo» del emparejamiento (aprovecha el paso 88);
    - una pantalla del mockup escrita como documento entero se queda solo con su contenido.
  - **Decisión del autor: los prompts no cambian en este paso,** para poder medir el efecto de las correcciones por separado (los del árbitro acababan de cambiar en la v0.10).
  - **Fuera de este paso:** el JSON mal formado y la salida cortada por tamaño, que no tienen arreglo mecánico (paso 90, reparar editando).
  - **Cierra** dos decisiones abiertas: corregir las citas que son números de línea y quitar las etiquetas de documento de las pantallas.
- **Para la memoria, en palabras sencillas:** algunos errores de los modelos son como erratas: tienen una sola corrección posible. En lugar de pedir al modelo que repita todo el trabajo, la plataforma los corrige sola y deja constancia de que lo hizo.

- **Coste en tiempo y dinero (petición del autor):** los cambios se justifican también por lo que cuestan.
  - **Precios** de la pasarela (captura del autor), en dólares por millón de tokens: SOL 2 de entrada y 10 de salida; LUNA 0,10 y 0,50, es decir, 20 veces más barata.
  - **Paso 95, estimar las ejecuciones sin dato:** muchas ejecuciones grandes no tienen consumo, porque la plataforma las detiene antes de que Claude Code lo dé.
    - La plataforma reconstruye, a partir de la consola guardada, cuánto creció el contexto turno a turno (cada Read con su tramo, cada Write y las compactaciones), y lo convierte en tokens con dos coeficientes ajustados en las 125 ejecuciones que sí tienen dato.
    - Error sobre el total, estimando cada ejecución sin ella: −0,7 % en el mockup y −3,5 % en las fases.
  - **Tiempo de los embeddings:** el de reloj y el de CPU se guardan con cada arbitraje. El catálogo grande tarda unos 13 s (54 s de CPU) y 0 $.
  - **Tablas** en `docs/costes/costes.md`, que se regeneran con `scripts/evaluacion/costes.cs`.
  - **Resultados:**
    - el flujo completo cuesta 22,25 $ y 105 minutos en el proyecto grande, y 8,76 $ y 55 minutos en el mediano;
    - el mockup es el 69–74 % del coste;
    - B (LUNA) es el 0,5 % del coste del flujo grande, así que la verificación cruzada con un modelo barato añade muy poco;
    - un arbitraje v0.10 a la primera cuesta unos 1,40 $, frente a 1,58–1,98 $ con la v0.9.
  - **Para la memoria, en palabras sencillas:** cada cambio se puede defender con lo que cuesta. Cuando falta el dato, se estima con un método comprobado contra los casos en que sí se conoce.

- **Pasos 90 a 94** (decisiones del autor del 19-10-2026, todas como recomendaba Claude, entre las opciones que se le explicaron con datos):
  - **90, reparar editando (prompts v0.11):** la reparación ya no lleva pegada la salida anterior ni la reescribe entera. La salida se queda en su fichero, revisada y con un elemento por línea; cada error dice su línea, y el modelo corrige solo esas líneas con Edit. Ataca los dos fallos de LUNA que no se podían arreglar: la reparación que no cabía en 128k y la reescritura cortada por el tope de salida.
  - **91, trabajos interrumpidos:** al arrancar, la API cierra como fallidos los trabajos que un reinicio dejó a medias, sin relanzarlos, y un mockup interrumpido se puede reanudar.
  - **92, compactar sin escribir:** una ejecución que compacta por segunda vez sin haber escrito su salida se detiene y se repite una vez desde cero. El caso real costó 1,15 M tokens (unos 2,5 $) sin escribir nada; su repetición sí escribió.
  - **93, sugerencias prudentes (prompts v0.11):**
    - «Aprobar todas» solo aprueba lo que sale de la documentación;
    - una decisión del cliente se sugiere como «A decidir por el cliente» con las opciones;
    - el catálogo marca, sin bloquear, los requisitos que se apoyan en una suposición de la IA aceptada tal cual.
  - **94, estilo del mockup:** sin `.zip`, cada proyecto recibe una de 8 direcciones de estilo, que el modelo adapta al sector.
  - **Pendiente:** las pruebas reales, que lanza el autor: el flujo completo del proyecto grande sin errores internos ni reparaciones que no caben, y dos mockups sin `.zip` con estilos distintos.
- **Prueba real (en curso, 20 a 22-10-2026):** flujo completo en un proyecto nuevo con los ficheros del proyecto grande de ejemplo.
  - **Preguntas:** 0,94 $, frente a 2,31 $ con la v0.9. Las 63 sugerencias salen como suposición, 40 de ellas «A decidir por el cliente».
  - **El primer catálogo falló por un error del paso 92:** contaba avisos de compactación en lugar de compactaciones. Se corrigió el 20-10.
  - **El segundo catálogo terminó:** 235 requisitos, 12 discrepancias (todas de la plataforma) y 55 marcados como suposición.
  - **Las reparaciones editando de LUNA y del árbitro funcionan:** la del árbitro de un catálogo grande, que antes no cabía, gastó 4.700 tokens de salida.
  - **Coste del catálogo:** unos 3,9 $, frente a 2,70 $, porque el árbitro consulta casi toda la documentación. Queda como mejora del prompt.
  - **Backlog (22-10):** 121 historias en 13 épicas, sin reparaciones, 8 minutos y 1,67 $ (antes 1,43 $). 17 discrepancias, 16 de ellas de la plataforma. El autor lo validó sin cambios.
  - **Hallazgo del backlog:** el árbitro conserva las historias, pero recorta los criterios de aceptación cuanto más grande es el backlog: en el proyecto grande deja uno por historia (121 de los 237 que proponía A). Queda como mejora prioritaria del prompt del árbitro del backlog.
  - **Error de medida corregido (22-10):** los trabajos que cerró `InterruptedJobs` (paso 91) duraban hasta el arranque que los cerró, días después; un mockup que trabajó 12 minutos contaba 101 horas. Ahora duran hasta su última ejecución. En marcha desde la reconstrucción del 22-10 (10:05); las tablas de coste ya salen bien.
  - **Mockup (22-10):** 40 pantallas con las 121 historias, todas a la primera, sin reparaciones ni compactaciones; 17,15 $ y 52 minutos (antes 15,46 $ y 73). Cuesta lo mismo por pantalla (0,43 $) y sigue la dirección de estilo asignada («cálida y cercana»). El autor: «muy buena pinta».
  - **Mockup del banco de alimentos (22-10):** con el mismo backlog de 45 historias, 24 pantallas a la primera, sin reparaciones ni compactaciones; 7,23 $ y 20 minutos (antes 20 pantallas, 6,48 $ y 36). Sigue su dirección, «institucional y sobria»: menú lateral oscuro, azul petróleo, fondo gris claro y tablas. Distinto del de clínicas.
  - **Hito 18 completado (22-10):** se cumplen las dos condiciones, el flujo completo sin errores internos ni reparaciones que no caben y estilos distintos sin `.zip`.
  - **Mejoras que deja la prueba** para la próxima versión de los prompts: que el árbitro consulte menos documentación y que no pierda criterios de aceptación en los backlogs grandes.
- **Para la memoria, en palabras sencillas:** el hito 18 hace la plataforma más robusta frente a los fallos de los modelos (corrige lo que tiene una sola corrección, repara cambiando solo lo que está mal y no malgasta ejecuciones perdidas). También la hace más honesta con el usuario: no da por buena una suposición sin que la mire, y los prototipos no salen todos iguales.

### 9b. Hito 18b: el árbitro tras la prueba real (22-10, hecho)

- **Por qué:** la prueba real de la v0.11 dejó dos mejoras: el árbitro del catálogo consulta casi toda la documentación (34 tramos, compacta y se repite) y el del backlog se queda con un criterio por historia, porque el contexto se le llena al leer y el backlog entero tiene que caber en una respuesta de 64.000 tokens.
- **Paso 107 (prompts v0.12):** el árbitro del catálogo recibe `input/fuentes.txt`, con los fragmentos que citan los requisitos que tiene que decidir (sin par «mismo», o «mismo» con cifras distintas), en lugar de la documentación entera.
- **Paso 108 (prompts v0.12):** el árbitro del backlog cita por su id los criterios y las tareas que conserva tal cual (`1:CA-012`) y escribe enteros solo los que combina; la plataforma pone el texto, numera y avisa de cuántos no cita.
- **Medido el 22-10** con un nuevo arbitraje del catálogo y otro del backlog de clínicas, con SOL y las mismas propuestas:
  - **catálogo:** 298 líneas de documentación consultadas en lugar de 1.242, a la primera (antes, tres intentos), 1,43 $ en lugar de 3,20 $ y 7 minutos en lugar de 11,5. La unión, 277 requisitos, es más atómica: separa, por ejemplo, el cambio de plan al año y a los ocho años, que la v0.11 había juntado. La plataforma marca 44 desviaciones, casi todas falsas, porque los embeddings no distinguen «un año» de «ocho años»;
  - **backlog:** 239 criterios en lugar de 121, 152 historias en lugar de 121 y 153 tareas, por 1,23 $ en lugar de 1,05 $. Cita por su id 238 criterios y escribe entero uno.
- **Mejoras que deja:** no marcar como desviación un par «mismo» con cifras distintas, repetir el arbitraje del catálogo para ver la variación, y que el árbitro no lea su conversación guardada después de compactar.
- **Para la memoria, en palabras sencillas:** en vez de darle al árbitro toda la biblioteca, la plataforma le da solo las páginas que tiene que comprobar.

### 10. Hito 19: exportar los entregables (20-10 y 22-10, hecho)

- **Paso 96, el catálogo a Word y PDF** (decisiones del autor, todas como recomendaba Claude):
  - **Contenido:** una portada con el estado, una sección por tipo y, en cada requisito, su texto, notación, obligatoriedad, fuentes y preguntas por id. El texto de los fragmentos y las preguntas con su respuesta van en dos anexos, una sola vez cada uno, para que el documento sea corto y siga siendo trazable.
  - **Borrador:** también se puede exportar, con «BORRADOR» en la portada y en el pie de cada página.
  - **Marcas:** sí «Suposición de la IA» y «Discrepancia sin revisar», que importan a quien lee; no el origen A/B ni la explicación del árbitro, que son internos y tendrán su vista en el hito 20.
  - **Librerías:** Word con DocumentFormat.OpenXml (MIT), que ya se usaba para leer `.docx`. PDF con MigraDoc, que lleva la fuente de la web (IBM Plex Sans, OFL) porque el contenedor no tiene fuentes. Descartadas QuestPDF (gratis solo para empresas con menos de 1 M$ de ingresos, el mismo problema que JsonSchema.Net), convertir con LibreOffice (unos 500 MB más en la imagen) e imprimir desde el navegador (no se puede comprobar con tests).
  - **Cambio de licencia encontrado al implementarlo:** PDFsharp y MigraDoc pasaron a AGPL-3.0 en la versión 6.5.0, del 14-10-2026. **Decisión del autor:** quedarse en la 6.2.4, la última MIT, fijada en el proyecto para que no se actualice.
  - **Comprobado:** el `.docx` pasa el validador de OpenXml sin errores y Word lo abre; el PDF se vuelve a leer con PdfPig. Un catálogo de 250 requisitos sale en unos 20 ms en Word y 0,3 s en PDF.
- **Paso 97, el backlog a Gherkin (22-10):**
  - **Decisiones del autor:** palabras clave en inglés con el texto tal cual, y todos los ficheros en un `.zip`.
  - **Forma:** un fichero por épica; cada épica es una `Feature`, cada historia una `Rule` (con su «Como… quiero… para…» y sus tareas) y cada criterio de aceptación un `Scenario`. Los ids van como etiquetas, para que una herramienta de pruebas pueda filtrar por historia o por requisito.
  - **Comprobado con el analizador oficial de Cucumber** (MIT), cada fichero antes de entregarlo. Al probarlo apareció algo que no se esperaba: el analizador no rechaza nunca una línea de texto libre, pero algunas cambian el sentido sin avisar (una línea que empieza por `Scenario:` crea un escenario de más). La plataforma las protege.
- **Paso 98, el backlog a CSV (22-10):**
  - **Decisiones del autor:** un CSV para cada herramienta, porque cada una espera otras columnas, y en Azure DevOps el proceso Agile (épica, historia de usuario y tarea), el que trae por defecto.
  - **Jira:** cada fila tiene un número y el número de su padre (la épica de una historia, la historia de una subtarea). La descripción de la historia lleva su narrativa, los criterios en Gherkin y los requisitos que cubre.
  - **Azure DevOps:** el título de cada elemento va en la columna de su nivel (Title 1, 2 o 3), y los criterios en su campo propio, *Acceptance Criteria*.
  - **En los dos,** los ids de Concilia van como etiquetas, para poder volver de cada elemento a su origen.
  - **Importado en Jira (23-10, autor):** el CSV del backlog de clínicas v0.11 entra en un Jira Cloud real con su jerarquía (épica → historia → subtarea), los acentos, la descripción con los criterios en Gherkin y las etiquetas con los ids, sin errores. El importador para quien no es administrador admite 250 elementos por fichero: creó 251 de 255, y las 4 subtareas que faltaban entraron con un segundo CSV. **Mejora posible, sin decidir:** que Concilia reparta el CSV en ficheros de 250 elementos como máximo, por épicas completas.
  - **Azure DevOps:** no se prueba en un Azure DevOps real. El autor da el hito 19 por cerrado el 23-10-2026.
- **Para la memoria, en palabras sencillas (CSV):** el backlog que sale de la plataforma se puede cargar de una vez en las dos herramientas de gestión de proyectos más usadas, con la estructura de épicas, historias y tareas ya montada.
- **Para la memoria, en palabras sencillas (Gherkin):** los criterios de aceptación salen en el formato que leen las herramientas de pruebas automáticas, de modo que el equipo de desarrollo puede usarlos como punto de partida de sus pruebas sin copiarlos a mano.
- **Para la memoria, en palabras sencillas:** lo que genera la plataforma ya no se queda dentro de la web. El catálogo se descarga como un documento normal, que se puede enviar al cliente, y cada requisito sigue diciendo de qué parte de la documentación sale y en qué respuesta del cliente se apoya.

### 11. Hito 20: trazabilidad visual y comparación de A, B y el árbitro (22-10, hecho)

- **Paso 99, la trazabilidad (22-10):** el autor dejó las decisiones a Claude.
  - **Dónde:** un panel lateral que se abre con «Trazabilidad» en cada requisito del catálogo y cada historia del backlog. Dentro del panel se pasa de un requisito a una historia que lo cubre, y de ahí a otro de sus requisitos, con «Volver», sin salir del paso ni perder lo que se está editando.
  - **Qué enseña:** desde un requisito, el texto de cada fragmento que cita (con su documento y su sección), cada pregunta con su respuesta y las historias que lo cubren. Desde una historia, sus requisitos y, a través de ellos, sus fragmentos y preguntas, cada uno una vez y con los requisitos que lo citan. Marca lo citado que no existe: un fragmento escrito a mano que no está en la documentación, o una historia que cita un requisito que ya no está en el catálogo.
  - **Datos:** los que ya había, sin guardar nada nuevo. Se construye con el mismo contenido que las exportaciones del hito 19, así que el panel y el Word dicen lo mismo. Enseña lo guardado, así que el botón se oculta mientras hay cambios sin guardar.
  - **Fuera, por ahora:** una vista de matriz de todo el catálogo (cuántas fuentes, preguntas e historias tiene cada requisito).
- **Para la memoria, en palabras sencillas:** de cualquier requisito se llega en un clic a la frase exacta de la documentación de la que sale y a la respuesta del cliente en que se apoya, y de cualquier historia, a los requisitos que la justifican. Es lo que permite a un analista comprobar que nada de lo generado se lo ha inventado la IA.

- **Paso 100, la comparación de A, B y el árbitro (22-10):** decisiones del autor, todas como recomendaba Claude.
  - **Qué se ve:** en «Ejecuciones», bajo el informe de cada trabajo con árbitro, una tabla de tres columnas, A | árbitro | B. Cada fila es un elemento de la unión con lo que juntó de cada modelo, un elemento descartado con el motivo del árbitro o algo que el árbitro no clasificó. Junto a cada elemento de A y de B, lo más parecido de la otra propuesta según los embeddings («mismo», «parecido» o «distinto», con la similitud). Se marcan las filas en las que el árbitro no coincide con los embeddings, y se puede filtrar y buscar.
  - **Informe y CSV:** junto a las cifras del árbitro, las de los embeddings: lo que solo está en A, lo que solo está en B y el porcentaje de elementos en que coinciden con el árbitro.
  - **El cálculo, una sola vez:** la parte lenta (los embeddings) se guarda con cada trabajo, sin los umbrales ni el árbitro, que se aplican al usarla. Los trabajos nuevos la guardan al arbitrar, donde ya se calculaba; los antiguos, la primera vez que se pide (unos segundos cada uno). Si cambia el modelo de embeddings, se recalcula.
- **Para la memoria, en palabras sencillas (paso 100):** en cada trabajo se ve, elemento por elemento, qué propuso cada modelo y qué hizo el árbitro con ello, y al lado lo que dice una medida independiente del árbitro (la cercanía de significado). Así se puede comprobar, caso por caso, si el árbitro ha juntado lo que es igual y separado lo que es distinto.

### 12. Hito 21: historial de versiones de los entregables (23-10, cerrado el 26-10)

- **Paso 101, guardar cada versión (23-10):** decisiones del autor, todas como recomendaba Claude.
  - **Qué se guarda:** cada vez que el catálogo, el backlog o el mockup se genera, se guardan cambios o se valida, una copia entera, con su número, su origen (generada, editada, validada), su estado, el trabajo del que sale y la fecha. Del mockup, también su página. Un guardado que no cambia nada no crea versión, ni tampoco reabrir. Se guardan todas.
  - **Lo que ya había:** cada entregable que existía antes del historial recibe su primera versión al arrancar la API («anterior al historial»). La copia de trabajo no cambia, así que las exportaciones, la trazabilidad, la comparación y el informe siguen igual.
  - **Dónde se ve:** «Historial de versiones», plegable, en el catálogo, el backlog y el mockup, con la versión actual marcada.
- **Para la memoria, en palabras sencillas:** antes, generar de nuevo o editar un entregable borraba el anterior para siempre; ahora cada estado por el que pasa queda guardado con su fecha y su motivo, como el historial de un documento compartido.
- **Paso 102, ver, comparar y volver (23-10):** decisiones del autor como recomendaba Claude.
  - **Ver:** cada versión anterior se abre en un panel lateral, solo para leer; la de un mockup, en una pestaña aparte.
  - **Comparar:** de la versión a lo que hay ahora, los requisitos o historias iguales, modificados (como eran y como son), añadidos y borrados. Se emparejan por su contenido, igual que en el informe de cambios del usuario, porque los ids se renumeran al guardar.
  - **Volver:** el entregable recupera esa versión en borrador, aunque estuviera validada, porque las respuestas pueden haber cambiado; hay que validarlo de nuevo. Es una versión nueva, así que la que había queda en el historial y se puede deshacer. Los pasos siguientes quedan desactualizados, como al generarlo.
  - **Desactualizada:** cada versión recuerda si algo en lo que se basaba cambió después; al volver a ella, vuelve su aviso.
- **Para la memoria, en palabras sencillas (paso 102):** si una nueva generación sale peor o una edición se equivoca, se compara con lo anterior y se recupera en un clic, sin perder nada: también lo que se deja atrás queda guardado.

### 13. Hito 22: iteraciones del proyecto (diseño y pasos 109 a 116, 26-10 a 03-11)

- **El problema:** cuando a un proyecto ya analizado le llega documentación nueva, hoy solo se puede generar todo de nuevo. Se pierde lo que el usuario editó y validó, y se paga otra vez todo el análisis.
- **La idea del autor:** la documentación se agrupa en iteraciones («Iteración 1», «Iteración 2»…, con nombre), que se cierran con «Cerrar documentación». En preguntas, catálogo y backlog se elige entre rehacerlo con todas o trabajar solo sobre las iteraciones marcadas, y cada elemento dice de qué iteración viene y qué cambió y por qué. El mockup se sigue generando con todo.
- **Decisiones del autor (26-10-2026), con las propuestas de Claude que aprobó:**
  - con una iteración abierta no se lanzan las fases; solo se reabre la última, y reabrirla desactualiza como subir un archivo;
  - ids estables en el catálogo y el backlog: hasta ahora se renumeraban al guardar, y sin ids estables no se puede seguir un requisito de una iteración a otra;
  - la iteración de cada elemento la calcula la plataforma por sus fuentes, no el modelo;
  - la **lectura relacionada** (en el diseño, opción C): obligatoria la documentación nueva y los fragmentos antiguos que se le parecen según los embeddings; el resto, consultable. Se eligió frente a la lectura completa (A, cada vez más caro) y frente a «solo lo nuevo» (B, puede pasar por alto contradicciones con lo antiguo);
  - la actualización es un conjunto de cambios (nuevos, modificados y eliminados, con motivo), y lo que modifica o elimina la IA es una propuesta que el usuario revisa antes de validar;
  - una respuesta del cliente nunca se reescribe: si la nueva documentación la contradice, se crea una pregunta nueva enlazada;
  - actualizar el catálogo exige el catálogo validado, una ronda de preguntas sobre las iteraciones elegidas y ninguna pendiente;
  - el hito va antes de la evaluación, que pasa a ser el hito 23.
- **Requisito del autor:** nada de lo que ya funciona deja de funcionar. Los modos de siempre usan exactamente el mismo prompt y las mismas entradas (lo comprobará un test), los proyectos existentes pasan a una «Iteración 1» cerrada y lo único que cambia a la vista es la numeración estable y el clic de cerrar la documentación.
- **Paso 109, iteraciones en Documentos (26-10):** tabla de iteraciones y la iteración de cada archivo; los documentos se suben a la iteración abierta, y mientras esté abierta (con algún documento) no se lanzan rondas, sugerencias, catálogo ni backlog. Solo se reabre la última, y reabrirla desactualiza como subir un archivo. La «Iteración 1» de los proyectos que ya existen se crea al arrancar la API y no en la migración, para poder probarla: un test comprueba que el proyecto da los mismos documentos, con la misma numeración, y el mismo flujo que antes. Tres huecos del plan, decididos por el autor: se puede eliminar una iteración creada por error (la última, abierta y vacía); un proyecto existente sin documentos recibe la «Iteración 1» abierta; y el catálogo v1.4 experimental y el nuevo arbitraje no se bloquean. Los prompts y las entradas de las fases no cambian.
- **Paso 110, ids estables (26-10):** al guardar el catálogo o el backlog, cada requisito, historia, criterio y tarea conserva su id; lo nuevo toma el siguiente número de su tipo, borrar deja un hueco y cambiar el tipo de un requisito le da un id nuevo. Un número ya dado en el historial del borrador no se vuelve a dar, para que un id nombre siempre lo mismo. Generar desde cero sigue numerando seguido, y la regla `ID-SEQ` (ids seguidos) solo se aplica a lo que escriben los modelos. Es la base para decir «RF-012, modificado en la iteración 2».
- **Paso 111, la iteración de cada elemento (26-10, revisado por el autor en el navegador):** la plataforma calcula de qué iteración sale cada pregunta (sus fuentes), cada requisito (sus fuentes y preguntas) y cada historia (sus requisitos), sin coste de modelos y también para lo que añade el usuario a mano; como un elemento puede apoyarse en varias, cada uno tiene una lista. La web lo muestra, con un filtro, en cuanto el proyecto tiene más de una iteración, y también en el panel de trazabilidad. Cada catálogo, backlog, mockup y versión guarda qué iteraciones incluye, y la web avisa de las cerradas que le faltan. Word y PDF dicen la iteración de cada requisito, y los CSV de Jira y Azure DevOps llevan la etiqueta `iteracion-N`.
- **Paso 112, preguntas centradas (26-10; revisado por el autor en el navegador el 27-10):** antes de tocar nada, un test guarda lo que da la v0.12 (prompt y ficheros de cada fase) y vigila que los modos de siempre no cambien. El tope de lo relacionado se midió con clínicas (con un documento nuevo, una decena de fragmentos y unos cientos de tokens; con una tanda de 11, 90 fragmentos y unos 2.400) y el autor lo fijó en 20.000 tokens. Los prompts v0.13 los revisó el autor antes del código: el catálogo validado llega como requisitos relacionados de lectura obligatoria. Una ronda centrada lee lo nuevo, lo anterior que se le parece y los requisitos validados relacionados; pregunta lo nuevo y las contradicciones (con una pregunta nueva enlazada, porque una respuesta nunca se reescribe) y propone revisar las pendientes: la reformulación se aplica con su historial, y «resuelta» y «obsoleta» las decide el usuario.
- **Paso 113, actualizar el catálogo (27-10, revisado por el autor en el navegador):** con el catálogo validado y una ronda sobre la iteración nueva, los dos modelos proponen solo los cambios que pide lo nuevo: requisitos nuevos, modificados y eliminados, cada uno con su motivo. El árbitro los une; la plataforma alinea por id las modificaciones y eliminaciones y avisa si un «nuevo» ya estaba en el catálogo. Lo nuevo se añade con el siguiente número de su tipo; lo modificado o eliminado queda como propuesta que el usuario acepta o rechaza antes de validar, y cada requisito guarda su historial. Lo que el usuario había editado se conserva, porque los cambios se aplican sobre su copia de trabajo.
- **Paso 114, actualizar el backlog (27-10, revisado por el autor en el navegador):** antes de llamar a los modelos, la plataforma compara el catálogo del que salió el backlog con el validado ahora y calcula, sin IA, qué requisitos cambiaron, cuáles se eliminaron y cuáles son nuevos sin historia, y qué historias se ven afectadas. Los modelos solo trabajan sobre eso: historias nuevas, modificadas (conservando por su id los criterios y tareas que siguen valiendo) y eliminadas. Como en el catálogo, lo nuevo se añade y lo modificado o eliminado lo acepta o rechaza el usuario, con el historial de cada historia.
- **Paso 115, desde cero frente a lo anterior (27-10, revisado por el autor en el navegador):** cuando se genera el catálogo o el backlog desde cero sobre uno anterior, la plataforma compara cada elemento nuevo con la versión de justo antes, por significado y con los mismos umbrales que entre A y B, y lo marca como igual, cambiado o nuevo; lo de antes que no aparece se lista como desaparecido. No cuesta tokens: los embeddings se calculan en local, una vez por trabajo. Las cifras van al informe y al CSV, y son las que medirá el paso 116 para comparar actualizar con empezar de cero.
- **Paso 116, prueba real (27-10 y 03-11; detalle en `docs/resultados-paso-116/README.md`):**
  - **Qué se probó:** Claude escribió una segunda iteración de clínicas, de 6 documentos con 18 cambios puestos a propósito, y una hoja de respuestas del cliente. Con ellas ejecutó por la API, a petición del autor, los dos caminos sobre el mismo proyecto: actualizar y empezar desde cero. También probó una ronda que lo lee todo (lectura completa) frente a la centrada (C).
  - **Detección:** las dos rondas detectan las 8 preguntas esperadas y enlazan a su respuesta las 4 que la contradicen, y los dos catálogos recogen los 14 cambios.
  - **Lo validado que se conserva:** actualizar deja idénticos el 91 % de los requisitos y el 95 % de las historias; desde cero, ninguno, aunque por significado el catálogo diga casi lo mismo.
  - **Coste:** no baja (5,85 $ frente a 5,44 $), porque actualizar consulta mucho lo anterior. Con una iteración tan pequeña, leerlo todo (0,95 $) salió más barato que la lectura relacionada (1,67 $).
  - **Un fallo del paso 112, arreglado:** las preguntas enlazadas a una respuesta se rechazaban siempre.
- **Paso 123, la lectura relacionada con una documentación grande (03 y 04-11):** con un documento público de PURE de unos 96.000 tokens, en inglés, partido en dos iteraciones, leerlo todo no cabe en el contexto de 128.000 tokens: A y B se detienen sin escribir, también al repetirse. La lectura relacionada termina a la primera por 0,87 $, con preguntas que citan también la iteración anterior. Junto con el paso 116: leerlo todo es una optimización para los proyectos pequeños, y la lectura relacionada vale para cualquier tamaño.
- **Para la memoria, en palabras sencillas:** un análisis real no llega de una vez: el cliente manda documentos en varias tandas. Con las iteraciones, Concilia trabaja como un analista que recibe información nueva: se centra en lo nuevo, comprueba si choca con lo que ya se sabía y propone qué añadir, cambiar o quitar, sin tocar lo que el cliente ya validó y sin decidir nada por su cuenta.

### 14. Hito 23: diseño de la evaluación (27-10, sin código)

- **Por qué:** al redactar la propuesta del TFG y contrastarla con el código, se vio que ningún paso medía la hipótesis: que la verificación cruzada es más fiable que un solo modelo. El paso 65 apunta incluso a que puede no serlo, porque la unión se parece mucho a A (SOL) y B (LUNA) aporta muy poco. Un resultado negativo también vale para la memoria, pero hay que medirlo.
- **Lo que se añade (autor), con los detalles de Claude:** los pasos 117 a 121.
  - 117: una versión congelada antes de evaluar.
  - 118: la referencia, revisada o hecha por el autor:
    - las 26 trampas del proyecto grande;
    - una lista de funcionalidades escrita sin mirar los catálogos.
  - 119: 3 repeticiones de cada fase en clínicas, para ver cuánto varía.
  - 120: Concilia frente a un solo modelo:
    - recall, precisión, F1 y coste de SOL solo, LUNA solo y la unión;
    - la revisión se hace a ciegas, con hojas de cálculo que prepara un script.
  - 121: SOL ‖ SOL, para saber si lo que aporta el patrón viene de usar dos modelos distintos o de hacer dos intentos.
- **La idea clave:** no hacen falta ejecuciones aparte de cada modelo. Cada trabajo de Concilia ya lleva una propuesta de SOL sola y otra de LUNA sola, que son independientes y parten de la misma entrada que la unión.
- **Los pasos que ya existían** (104 a 106) pasan a explicar también qué hace el autor y qué hace Claude:
  - 105, PURE, se hace en inglés y antes que el 104, que mide sobre él AQUSA y `vale-ears`, solo en los requisitos EARS, porque los MASTeR usan *should* y *may* a propósito;
  - 106, la prueba como analista, va con ReservaSalas y cierra los pasos 49 y 53.
  Al comprobarlas, Claude vio dos riesgos: AQUSA (`RELabUU/aqusa-core`, que existe; un primer aviso del mismo día decía que no se encontraba) está probado con Python 3.7, y `vale-ears` es de junio de 2026. Por eso el paso 104 empieza probando que funcionan. Y como el flujo en inglés no se ha probado nunca con modelos, el paso 117 empieza con una ronda de preguntas en inglés (unos 1 $).
- **Coste estimado:** unos 20–30 $ las repeticiones, unos 30–50 $ SOL ‖ SOL y unos 7–9 $ PURE. En horas del autor, unas 3–4 h de referencia, unas 5 h de revisión y 2–3 h como analista.
- **Para la memoria, en palabras sencillas:** hasta ahora se sabía qué propone cada modelo y qué se queda el árbitro, pero no si el resultado es mejor que el de un solo modelo. Para saberlo hace falta una lista de lo que debería salir, hecha por una persona, y contar cuánto de esa lista encuentra cada uno, cuánto de lo que propone está bien y cuánto cuesta.

### 15. Hito 23: la evaluación (desde el 04-11, en curso)

- **Paso 117, la versión congelada (04-11):** todo lo que se mida en el hito 23 se hace con la versión de la etiqueta `evaluacion-1`, descrita en `docs/evaluacion/version.md`:
  - el código del paso 122 (`f063fd6`), con las imágenes de Docker del 03-11, comprobadas por dentro: los 30 prompts y esquemas del repositorio están byte a byte en la imagen;
  - los prompts v0.12 en los modos de siempre y v0.13 en los centrados y las actualizaciones;
  - A y árbitro `SS - GPT-6-SOL`, B `SS - GPT-6-LUNA`;
  - los umbrales 0,90 y 0,70, y Claude Code 2.1.283.

  La etiqueta está en el commit del paso, `bbe1167`, sin push.
- **La prueba en inglés de antes de congelar:** la ronda de preguntas normal (v0.12), que nunca se había usado en inglés, con el documento de PURE elegido para el paso 105: `2005 - microcare.pdf`, el SRS del *Voucher Management System* (33 páginas). Salió válida a la primera, en 4 minutos y por 0,50 $, con 39 preguntas en inglés. LUNA propuso 20, al contrario que en la ronda centrada del paso 123. No hizo falta cambiar nada.
- **Para la memoria, en palabras sencillas:** antes de medir, se fija una versión y no se toca, para que todas las mediciones sean comparables y cualquiera pueda repetirlas.
- **Paso 105, PURE en inglés (04-11):** el flujo completo con Microcare, sin editar nada (`docs/evaluacion/pure.md`).
  - Todo a la primera, en 46 minutos y por 9,29 $, de los que 6,81 $ son del mockup. La estimación del plan (7–9 $) no contaba bien el mockup; el autor aprobó lanzarlo con la nueva, de 6–12 $.
  - Salen 40 preguntas, 122 requisitos (24 con `[TBD]` donde decide el cliente), 34 historias y 21 pantallas, todo en inglés. LUNA propone en todas las fases.
  - 39 de las 40 sugerencias son suposiciones, 28 de ellas decisiones del cliente sin opciones claras: se concretó la regla de «la primera opción».
  - **Hallazgos:** los textos fijos de la plataforma siguen en español (mejora futura, sin cambiar la versión), y el árbitro del backlog adopta la granularidad de B.
- **Paso 104, métricas externas (04-11):** AQUSA (historias, criterios QUS) y Vale con `vale-ears` (requisitos EARS) sobre las tres propuestas del paso 105 (`docs/evaluacion/metricas-externas.md`).
  - Las herramientas se instalaron fuera del repositorio, con versiones fijadas.
  - La extracción la lanza el autor, porque lee `data/`.
  - Cada aviso se revisó a mano. En bruto exageran: 63 de 90 avisos de Vale son falsas alarmas (comas en las condiciones, pasivas en el disparador), y 8 de 23 de AQUSA.
  - **Lo básico lo cumplen todos:** *shall* y la forma de las historias.
  - **Requisitos con un defecto real:** 5,1 (SOL), 9,6 (LUNA) y 7,0 (unión) por cada 100.
  - **Historias con dos acciones:** 6,0, 21,4 y 17,6. Las 6 de la unión juntan historias atómicas de SOL: el árbitro del backlog pierde ahí una calidad que SOL tenía.
  - **En los demás backlogs,** sin coste: el árbitro junta varias historias de A en una de un 0 % a un 57 % de las veces, y con las mismas propuestas una vez lo hizo y otra no. Es variación, no una regla fija. Lo cuenta el paso 119; la mejora (marcarlo como discrepancia sin IA), en «Después», punto 7.
- **Paso 106, la prueba como analista (04 y 05-11):** con ReservaSalas y un reparto mixto que eligió el autor (`docs/evaluacion/analista.md`). Claude lanza las dos rondas, responde como cliente y genera; el autor revisa como analista.
  - **Resultado:** el autor no cambia nada (27 requisitos y 12 historias) en unos 19 minutos. Las 9 discrepancias del catálogo eran DEBERÍA frente a DEBERÁ, en requisitos que citan respuestas escritas como propuesta. La segunda ronda no repite preguntas y detecta una contradicción. Coste: 1,11 $.
  - **Límites:** el proyecto es muy pequeño y el revisor es el autor. La métrica de cambios del usuario sigue sin ediciones reales.

### 16. Hito 24: el backlog en GitHub (06-11; integrado en `main` el 10-11)

El autor lo pide para incluirlo en la memoria si funciona. GitHub no tiene importador de archivos, así que Concilia da un `.zip` con un script que el usuario lanza con `gh`, sin que Concilia reciba ningún token. El estado vive en GitHub, en una marca invisible en cada issue con su id, su generación y un hash: repetir el script crea solo lo nuevo y lista lo modificado y eliminado con el comando que lo aplica, sin tocarlo. La generación hace falta porque una generación desde cero vuelve a numerar los ids. Prueba real con ReservaSalas (`docs/resultados-paso-124/README.md`): 67 issues con su jerarquía en 5 minutos, repetir no crea nada y una iteración simulada avisa de lo modificado y lo eliminado. Al autor le convence cómo se ven y pide borrar esos issues para importar el proyecto en el que trabaja. Se hizo en `feat/github-export` y se integró en `main` el 10-11-2026 (paso 125, adelantado por el cambio de rumbo de la etapa 17). La memoria ya lo incluye.

### 17. Cambio de rumbo: el modelo B por OpenCode Go (10-11, sin código)

- **Por qué:** LUNA no tiene capacidad suficiente para leer tanta información y procesarla con un resultado válido y útil:
  - como árbitro de un catálogo grande, 2 válidos de 6;
  - aporta casi nada a la unión: en clínicas, 6 preguntas, 1 requisito y ninguna historia;
  - más defectos con las métricas externas: EARS 9,6 frente a 5,1 de SOL, e historias con dos acciones 21,4 frente a 6,0;
  - propuesta vacía en PEPPOL.
  
  Además, SOL y LUNA son de la misma familia (GPT-6): con un modelo de otro fabricante, la verificación cruzada es más independiente, que es la base de la hipótesis.
- **Qué cambia:** solo el modelo B, con OpenCode Go (plan Go, 10 $ al mes).
  - A, árbitro, sugerencias y mockup siguen siendo SOL por la pasarela.
  - El runner sigue siendo Claude Code: OpenCode Go lo admite, y para B solo cambian la dirección y la clave.
  - Mismas condiciones para B: 128.000 tokens, los mismos prompts y planes de lectura.
- **Lo que se hizo por LUNA se queda:** son redes de seguridad comunes a todos los modelos que solo actúan si hay un error (tope de salida, corte del `Write` incompleto, reintento por inactividad, `offset`/`limit` exactos, correcciones automáticas, reparar editando). Lo que de verdad limita viene de los 128.000 tokens de la pasarela, que siguen para SOL. Se revisan con datos en el paso 132.
- **Plan:**
  - hito 25: estudio de candidatos, proveedor por papel, prueba de humo, elección con clínicas, flujo frente a LUNA y cierre;
  - hito 26: el paso 126 y lo de «Después» que el autor decida hacer antes;
  - hito 27: la evaluación 2 desde cero, comparada con la 1.
- **Coste:** desde ahora, en todo se mide el precio para una empresa que pagase por token. B también se calcula por token, con la nota de la suscripción.
- **Repositorio:** solo `main`. La rama de GitHub, integrada y borrada; la historia de antes de quitar el coautor, en `C:\TFG\concilia-historia-antigua.bundle`. `evaluacion-1` queda en local como marca histórica.
- **Memoria:** se trabaja en paralelo, con una sección temporal al final, «Cosas que añadir a la memoria», con este cambio, el coste para una empresa y la evaluación 2.

### Documentación de la API (20-10, a petición del autor, fuera del plan)

- **Qué hay:** el documento OpenAPI 3.1 de la API (`/openapi/v1.json`), generado por ASP.NET Core, y Swagger UI encima (`/swagger`, también en <http://localhost:8080/swagger> con Docker), con los colores de Concilia en claro y en oscuro. Los 42 endpoints van en 10 grupos en el orden del flujo, cada uno con su resumen, su descripción en español, sus parámetros y lo que devuelve en cada caso (incluidos los `400` con los mensajes de la web, los `404` y los `202` de las tareas en segundo plano).
- **Se actualiza sola, en tres capas:** el documento sale de los propios endpoints en cada petición; cada compilación en Debug reescribe la copia `docs/api/openapi.json`, así que los cambios de la API se ven en git; y `OpenApiDocumentTests` falla si un endpoint queda sin grupo, sin resumen o sin decir qué devuelve, o si la copia no coincide. CLAUDE.md pide documentar cada endpoint nuevo junto a su código.
- **Librerías:** `Microsoft.AspNetCore.OpenApi` (la oficial de .NET 10) y `Swashbuckle.AspNetCore.SwaggerUI` 10.2.3 (MIT), solo la interfaz; la 10.3 tenía un día y se descartó por prudencia.
- **Para la memoria, en palabras sencillas:** cualquiera puede ver qué ofrece la API y probarla desde el navegador sin leer el código, y esa descripción no se queda vieja: está escrita al lado de cada endpoint y los tests no dejan subir uno sin ella.

## Pregunta clave para la evaluación: ¿el árbitro prefiere a su propio modelo?

> **Prioridad alta (autor, 16-10-2026).** Afecta al valor de la verificación cruzada, que es la idea central del TFG. Diseño completo del experimento, estado y resultados en [`experimento-sesgo-arbitro.md`](experimento-sesgo-arbitro.md): es material directo para el capítulo de evaluación de la memoria.

**Lo observado** en el proyecto grande de ejemplo, con A y el árbitro con `SS - GPT-6-SOL` y B con `SS - GPT-6-LUNA`:

| Fase | Propone A | Propone B | Unión del árbitro | De ambas | Solo de A | Solo de B |
| --- | --- | --- | --- | --- | --- | --- |
| Preguntas | 50 | 17 | 55 | 11 | 38 | 6 |
| Catálogo | 299 | 192 | 209 | 180 | 28 | 1 |
| Backlog | 109 | 54 | 109 | 89 | 20 | 0 |

A propone mucho más que B, y la unión se parece mucho a A: lo que aporta solo B son 6 preguntas, 1 requisito y ninguna historia.

**Dos explicaciones posibles:**
1. **A es simplemente más completo:** SOL cubre más de la documentación y lo que propone B ya está en A.
2. **El árbitro prefiere la propuesta de su mismo modelo.** El árbitro también es SOL. Las propuestas le llegan anónimas y en orden aleatorio, pero podría reconocer y favorecer el estilo de su propio modelo.

**Por qué importa:** si pasa lo segundo, la verificación cruzada pierde parte de su valor. El resultado dependería de qué modelo hace de árbitro más que de lo que aportan los dos, y la memoria tendría que reconocerlo como una limitación o proponer cómo corregirlo.

**Cómo comprobarlo:** repetir una fase con el mismo documento cambiando solo el reparto de modelos, por ejemplo con `SS - GPT-6-LUNA` de árbitro (`RUNNER_MODEL_ARBITER`), y comparar qué parte de la unión viene de A y de B.
- Si con LUNA de árbitro la unión sigue pareciéndose a A, es que A es más completo.
- Si pasa a parecerse a B, hay preferencia del árbitro por su propio modelo.
- Para que sea comparable, conviene repetir cada configuración 2 o 3 veces, por la variabilidad entre ejecuciones.
- Lo más barato es hacerlo en la fase de preguntas o en la del backlog; el informe por trabajo ya da los recuentos necesarios.

**Experimento en curso (16-10-2026):** en preguntas y catálogo, 3 arbitrajes con SOL (el original más 2 nuevos) y 3 con LUNA sobre las mismas propuestas. Se repite 3 veces porque el mismo árbitro no da siempre el mismo resultado: así se distingue una diferencia real de una casualidad. El backlog se deja fuera porque depende del catálogo.

**Resultado (16-10-2026): no hay preferencia del árbitro por su propio modelo.** Con las mismas propuestas, lo que entra solo de B es igual con SOL y con LUNA de árbitro: 6 en los 6 arbitrajes de preguntas, y una media de 2,7 con SOL y 1,5 con LUNA en el catálogo. La unión se parece a A porque A propone más y casi todo lo que propone B ya está en A. Hallazgo secundario: LUNA es poco fiable como árbitro de un catálogo grande (2 válidos de 6 intentos, frente a 3 de 3 de SOL), por errores de formato y tamaño. Detalle en `experimento-sesgo-arbitro.md`.

**Referencia independiente con embeddings (paso 87, 19-10-2026):** sin ningún LLM de por medio, lo que solo está en B (sin nada «mismo» ni «parecido» en A) es muy poco: en clínicas, 8 preguntas, 1 requisito y 1 historia, frente a 6, 1 y 0 según el árbitro. Confirma la conclusión del experimento: la unión se parece a A porque casi todo lo que propone B ya está en A.

**Herramienta (16-10-2026):** «Volver a arbitrar» repite solo el árbitro de un trabajo terminado sobre las mismas propuestas de A y B, con el modelo que se elija, sin cambiar nada del proyecto. Así lo único que cambia es el árbitro. El plan es hacer 3 arbitrajes con SOL y 3 con LUNA sobre el proyecto grande y comparar las proporciones de A y B en la unión.

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
| v0.11 | 19-10 | Reparar editando, sugerencias prudentes y estilo propio del mockup por proyecto |
| v0.10 | 19-10 | El árbitro parte del emparejamiento por significado de la plataforma, y sus desviaciones se marcan como discrepancia |
| v0.12 | 22-10 | El árbitro del catálogo consulta solo las fuentes que necesita (paso 107) y el del backlog cita por su id los criterios y las tareas (paso 108) |
| v0.13 | 26-10 y 27-10 | Preguntas centradas en unas iteraciones (paso 112) y actualizar el catálogo (paso 113) y el backlog (paso 114) con ellas: lo nuevo, lo anterior relacionado y los requisitos validados relacionados; preguntas nuevas, contradicciones y revisiones de las pendientes (paso 112). Los prompts de siempre siguen en la v0.12. Medida en el paso 116 (03-11): detecta todos los cambios puestos a propósito y conserva lo validado, por un coste parecido al de empezar de cero |

El detalle y las mediciones de cada versión están en `docs/evolucion-prompt-requisitos.md`.

## Decisiones que siguen abiertas

- ~~Umbral de «discrepancia grave»~~: cerrado por el autor el 19-10-2026. Dos umbrales del emparejamiento (0,90 y 0,70, paso 87), y la discrepancia la marcan las desviaciones del árbitro respecto al emparejamiento (paso 88). El origen de los embeddings lo decidió el autor el 16-10-2026: un modelo local.
- Abaratar las pantallas del mockup. (El historial de mockups lo trae el hito 21: desde el paso 101 se guarda la página de cada mockup generado.)
- ~~Del hito 23: la referencia del catálogo y del backlog en el paso 120~~: cerrada por el autor el 27-10-2026. El proyecto grande, con una lista de funcionalidades hecha por el autor (cobertura), una muestra revisada a ciegas (precisión) y las 26 trampas revisadas. SOL solo y LUNA solo son las propuestas de A y B de cada trabajo de Concilia.
- ~~Del hito 22 (26-10-2026): si se hace un CSV solo con lo nuevo de una iteración~~: cerrada por el autor el 04-11-2026 sin hacerlo, como mejora futura. ~~El tope de fragmentos relacionados~~: cerrado por el autor el mismo día, 20.000 tokens, tras medirlo con clínicas (`docs/fragmentos-relacionados.md`).
- **Desde el 10-11-2026:**
  - qué modelo de OpenCode Go es el nuevo B (candidatos en el paso 127, elección en el 130);
  - antes o después de la evaluación 2, cada punto de «Después» del plan, con la recomendación de Claude en cada uno;
  - si el nuevo B puede hacer de árbitro en el experimento del sesgo (paso 136).

## Siguientes pasos

Acordados con el autor el 16-10-2026 y recogidos en `docs/plan-de-construccion.md` como hitos 17 a 22; el 26-10-2026 el autor añade las iteraciones del proyecto como hito 22 y la evaluación pasa a ser el 23:

1. ~~Terminar el experimento del árbitro~~ Cerrado el 16-10-2026: no hay preferencia (paso 103).
2. ~~**Hito 17, embeddings en local**~~: modelo multilingüe en la API (paso 86), emparejamiento de A y B con los umbrales calibrados (paso 87) y árbitro con embeddings con su prueba real (paso 88), todo el 19-10-2026. Cierra la decisión abierta del origen de los embeddings y da una referencia objetiva al experimento del sesgo.
3. ~~**Hito 18, fiabilidad y calidad**~~ (pasos 89 a 95, 19-10-2026, prompts v0.11, con las tablas de coste), completado el 22-10-2026 con su prueba real. ~~**Hito 18b**~~ (pasos 107 y 108, prompts v0.12), completado el mismo día.
4. ~~**Hito 19, exportar**~~: el catálogo a Word y PDF (paso 96, 20-10-2026), y el backlog a Gherkin (paso 97) y a CSV para Jira o Azure DevOps (paso 98), los dos el 22-10-2026. Importado en un Jira real por el autor (23-10-2026); Azure DevOps sin probar en real, y el hito, cerrado por el autor el mismo día.
5. ~~**Hito 20, trazabilidad visual y comparación de A, B y el árbitro**~~: la trazabilidad en un panel lateral (paso 99) y la comparación de A, B y el árbitro, con los embeddings guardados una sola vez para el informe y el CSV (paso 100), los dos el 22-10-2026. Revisado en el navegador por Claude y por el autor (23-10-2026): cerrado.
6. ~~**Hito 21, historial de versiones de los entregables**~~: cada versión se guarda con su fecha y su origen (paso 101) y se puede ver, comparar con la actual y recuperar (paso 102), los dos el 23-10-2026. Revisado en el navegador por el autor (26-10-2026): cerrado.
7. ~~**Hito 22, iteraciones del proyecto**~~ (pasos 109 a 116, completado el 03-11-2026 a falta de que el autor revise los resultados): ~~iteraciones en Documentos~~ (paso 109) y ~~ids estables~~ (paso 110), los dos el 26-10-2026; ~~la iteración de cada elemento~~ (paso 111, revisado por el autor el mismo día); ~~preguntas centradas~~ (paso 112, revisado el 27-10); ~~actualizar el catálogo y el backlog~~ (pasos 113 y 114, revisados el 27-10); ~~desde cero frente a lo anterior~~ (paso 115, revisado el 27-10); ~~la prueba real con clínicas en dos iteraciones~~ (paso 116, 03-11); ~~sus mejoras~~ (paso 122, 03-11); ~~la lectura relacionada con una documentación grande~~ (paso 123, 04-11).
8. **Hito 23, evaluación 1** (interrumpida el 10-11-2026 por el cambio del modelo B; lo hecho queda como evidencia del cambio y como el «antes»; ~~la versión congelada, paso 117~~, 04-11-2026, etiqueta `evaluacion-1`; ~~PURE en inglés, paso 105~~, 04-11-2026, 9,29 $; ~~métricas externas, paso 104~~, 04-11-2026; ~~la prueba como analista, paso 106~~, 05-11-2026; siguiente, el paso 118): métricas externas, un documento de PURE, una prueba editando de verdad y la comparación entre actualizar y empezar de cero. Desde el 27-10-2026 (autor), además: la versión congelada antes de evaluar (paso 117), la referencia revisada por el autor (118), 3 repeticiones de cada fase para ver cuánto varía (119), Concilia frente a un solo modelo con recall, precisión, F1 y coste, que es la comparación que responde a la hipótesis (120), y SOL ‖ SOL, para saber si lo que aporta el patrón viene de usar dos modelos distintos (121). Orden: 117, 105, 104, 106, 118, 119, 120 y 121. Todo lo pendiente, en orden, está en «Lo que queda, en orden» de `docs/plan-de-construccion.md`.
9. ~~**Hito 24, el backlog en GitHub**~~ (paso 124): probado en real, incluido en la memoria y con su guía de uso en `README.md`, reescrito el 09-11 con los pasos de Jira y Azure DevOps; el autor ya lo usa desde la web con su proyecto (09-11). Integrado en `main` el 10-11-2026 (paso 125).
- **Desde el 10-11-2026, en este orden** (detalle en «Lo que queda, en orden» del plan):
  - **hito 25, el modelo B por OpenCode Go** (pasos 127 a 132; el siguiente es el 127, estudio de los candidatos);
  - **hito 26, antes de la evaluación 2:** el paso 126 (que el aviso de un par «mismo» separado diga en qué elemento está el otro; sale de un falso aviso en LasCuarenta) y lo de «Después» que el autor decida hacer antes;
  - **hito 27, la evaluación 2** desde cero (pasos 133, 118, 134, 135, 119, 120, 121, 136 y 137).
10. **Después:** hito 7 (catálogo v2) y redactar la memoria. Estructura del autor (22-10-2026, sustituye a la del 14-10): 1 Introducción; 2 Desarrollo (2.1 especificación del sistema, 2.2 construcción del sistema y 2.3 validación de la solución); 3 Conclusiones. **La memoria, empezada el 04-11-2026:** redactadas la introducción (unas 2.070 palabras) y las secciones 2.1 y 2.2 (unas 4.400 palabras, con los cuatro diagramas); la 2.3 y las conclusiones, pendientes. Unas 6.500 de las 10.000 palabras.

- ~~**Por decidir (16-10-2026):** herramientas externas candidatas tras una búsqueda en GitHub~~. Todas decididas:
  - los embeddings en local (hito 17) y el analizador de Gherkin (paso 97), ya hechos;
  - AQUSA y `vale-ears` (paso 104) y el dataset PURE (paso 105), en el hito 23.
- **Aparcado (15-10-2026):** rehacer el mockup del proyecto grande de ejemplo con la suscripción del autor (Sonnet 5.5) y la skill `frontend-design`, para ver si mejora la calidad; se retoma tras el paso 65. Riesgos: agotar el límite de 5 horas de la suscripción (se puede reanudar) y cambiar dos cosas a la vez, el modelo y las skills.

## Cifras de seguimiento

- **Commits:** 269 entre el 28-09 y el 05-11-2026.
- **Tests de C#:** 289 (30-09), 339 (02-10), 620 (08-10), 759 (13-10), 835 (16-10), 890 (19-10; 6 de ellos se saltan sin el modelo de embeddings), 897 (20-10, paso 96), 903 (20-10, con la corrección del paso 92 y los de la documentación de la API), 908 (22-10, paso 97), 912 (22-10, paso 98 con su revisión) 918 (22-10, paso 99), 937 (22-10, paso 100), 943 (22-10, pasos 107 y 108), 948 (23-10, paso 101), 951 (23-10, paso 102), 966 (26-10, paso 109), 974 (26-10, paso 110) y 983 (26-10, paso 111), 990 (26-10, instantánea de la v0.12), 1.002 (26-10, paso 112), 1.013 (27-10, paso 113), 1.020 (27-10, paso 114), 1.024 (27-10, paso 115), 1.025 (27-10, ronda que lo lee todo del paso 116) , 1.026 (27-10, arreglo de `REF-PREG`) y 1.028 (03-11, paso 122).
