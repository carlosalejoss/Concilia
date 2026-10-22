# Trazabilidad del proyecto (Concilia)

Visión de alto nivel de cómo se ha construido el TFG, del 28-09-2026 al 19-10-2026: qué se hizo en cada etapa, qué se decidió y qué cambió de criterio. El detalle de cada día está en `bitacora-detallada.md`; el estado vigente, en `CLAUDE.md` del repositorio.

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
| 22-10 | Hito 18, prueba real | Flujo completo de clínicas con los prompts v0.11 sin errores internos y con reparaciones que caben; mockups sin `.zip` con estilos distintos en dos proyectos; el hito 18 queda completado |
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

### 9b. Hito 18b: el árbitro tras la prueba real (22-10, en curso)

- **Por qué:** la prueba real de la v0.11 dejó dos mejoras: el árbitro del catálogo consulta casi toda la documentación (34 tramos, compacta y se repite) y el del backlog se queda con un criterio por historia, porque el contexto se le llena al leer y el backlog entero tiene que caber en una respuesta de 64.000 tokens.
- **Paso 107 (prompts v0.12):** el árbitro del catálogo recibe `input/fuentes.txt`, con los fragmentos que citan los requisitos que tiene que decidir (sin par «mismo», o «mismo» con cifras distintas), en lugar de la documentación entera.
- **Paso 108:** el árbitro del backlog citará por su id los criterios y las tareas que conserva, y la plataforma pondrá el texto.
- **Se mide** con un nuevo arbitraje de los trabajos de clínicas, sobre las mismas propuestas.
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
  - **Pendiente:** importar los dos CSV en un Jira y un Azure DevOps de verdad. Necesita cuentas en esas herramientas, así que lo hace el autor.
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
| v0.12 | 22-10 | El árbitro del catálogo consulta solo las fuentes que necesita (paso 107) |

El detalle y las mediciones de cada versión están en `docs/evolucion-prompt-requisitos.md`.

## Decisiones que siguen abiertas

- ~~Umbral de «discrepancia grave»~~: cerrado por el autor el 19-10-2026. Dos umbrales del emparejamiento (0,90 y 0,70, paso 87), y la discrepancia la marcan las desviaciones del árbitro respecto al emparejamiento (paso 88). El origen de los embeddings lo decidió el autor el 16-10-2026: un modelo local.
- Abaratar las pantallas del mockup y un posible historial de mockups.

## Siguientes pasos

Acordados con el autor el 16-10-2026 y recogidos en `docs/plan-de-construccion.md` como hitos 17 a 22:

1. ~~Terminar el experimento del árbitro~~ Cerrado el 16-10-2026: no hay preferencia (paso 103).
2. ~~**Hito 17, embeddings en local**~~: modelo multilingüe en la API (paso 86), emparejamiento de A y B con los umbrales calibrados (paso 87) y árbitro con embeddings con su prueba real (paso 88), todo el 19-10-2026. Cierra la decisión abierta del origen de los embeddings y da una referencia objetiva al experimento del sesgo.
3. ~~**Hito 18, fiabilidad y calidad**~~ (pasos 89 a 95, 19-10-2026, prompts v0.11, con las tablas de coste), completado el 22-10-2026 con su prueba real.
4. ~~**Hito 19, exportar**~~: el catálogo a Word y PDF (paso 96, 20-10-2026), y el backlog a Gherkin (paso 97) y a CSV para Jira o Azure DevOps (paso 98), los dos el 22-10-2026. Falta que el autor pruebe la importación en Jira y Azure DevOps.
5. ~~**Hito 20, trazabilidad visual y comparación de A, B y el árbitro**~~: la trazabilidad en un panel lateral (paso 99) y la comparación de A, B y el árbitro, con los embeddings guardados una sola vez para el informe y el CSV (paso 100), los dos el 22-10-2026. Falta que el autor revise la comparación en el navegador.
6. **Hito 21, historial de versiones de los entregables.**
7. **Hito 22, evaluación:** métricas externas, un documento de PURE y una prueba editando de verdad.
8. **Después:** hito 7 (catálogo v2) y redactar la memoria (sin capítulo de Metodología; 3.1 especificación de la solución, 3.2 implementación y 4 evaluación).

- **Por decidir (16-10-2026):** herramientas externas candidatas tras una búsqueda en GitHub (detalle en la bitácora): embeddings multilingües en local (árbitro y prueba del sesgo), AQUSA para las historias, el analizador de Gherkin, `vale-ears` para EARS en inglés y el dataset PURE como documento público.
- **Aparcado (15-10-2026):** rehacer el mockup del proyecto grande de ejemplo con la suscripción del autor (Sonnet 5.5) y la skill `frontend-design`, para ver si mejora la calidad; se retoma tras el paso 65. Riesgos: agotar el límite de 5 horas de la suscripción (se puede reanudar) y cambiar dos cosas a la vez, el modelo y las skills.

## Cifras de seguimiento

- **Commits:** 187 entre el 28-09 y el 20-10-2026.
- **Tests de C#:** 289 (30-09), 339 (02-10), 620 (08-10), 759 (13-10), 835 (16-10), 890 (19-10; 6 de ellos se saltan sin el modelo de embeddings), 897 (20-10, paso 96), 903 (20-10, con la corrección del paso 92 y los de la documentación de la API), 908 (22-10, paso 97), 912 (22-10, paso 98 con su revisión) 918 (22-10, paso 99) y 937 (22-10, paso 100).
