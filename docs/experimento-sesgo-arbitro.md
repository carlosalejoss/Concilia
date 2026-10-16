# Experimento: ¿el árbitro prefiere la propuesta de su propio modelo?

Material para el capítulo de evaluación de la memoria. **Prioridad alta** (autor, 16-10-2026): afecta al valor de la verificación cruzada, que es la idea central del TFG.

## Origen

En el flujo completo del proyecto grande de ejemplo (clínicas veterinarias, ficticio, de `ejemplos/`, 15-10-2026), A y el árbitro eran `SS - GPT-6-SOL` y B era `SS - GPT-6-LUNA`. La unión del árbitro se parecía mucho a la propuesta de A:

| Fase | Propone A | Propone B | Unión | De ambas | Solo de A | Solo de B |
| --- | --- | --- | --- | --- | --- | --- |
| Preguntas | 50 | 17 | 55 | 11 | 38 | 6 |
| Catálogo | 299 | 192 | 209 | 180 | 28 | 1 |
| Backlog | 109 | 54 | 109 | 89 | 20 | 0 |

## Pregunta e hipótesis

¿Por qué la unión se parece tanto a A?

- **H1, A es más completo:** SOL cubre más de la documentación, y casi todo lo que propone B ya está en A.
- **H2, el árbitro prefiere a su propio modelo:** el árbitro también es SOL. Aunque las propuestas le llegan anónimas («Propuesta 1» y «Propuesta 2») y en orden aleatorio, podría reconocer y favorecer el estilo de su modelo.

Si es H2, el resultado de la verificación cruzada depende de qué modelo hace de árbitro más que de lo que aportan los dos, y la memoria tiene que reconocerlo como una limitación.

## Diseño

**Se cambia solo el árbitro.** La funcionalidad «Volver a arbitrar» (16-10-2026) repite únicamente el árbitro de un trabajo terminado sobre **las mismas propuestas de A y B**, con el modelo que se elija, y sin cambiar nada del proyecto. Si se relanzara la fase entera, A y B también cambiarían, y no se sabría a qué se debe la diferencia.

**Configuraciones:**
- árbitro `SS - GPT-6-SOL`, el mismo modelo que A;
- árbitro `SS - GPT-6-LUNA`, el mismo modelo que B.

**Fases:**
- **Preguntas:** es donde más difieren A y B (38 elementos solo de A y 6 solo de B), así que una preferencia se notaría más.
- **Catálogo:** es la fase más representativa del sistema.
- **Backlog, no se incluye:** sus propuestas dependen del catálogo validado, y con dos fases independientes ya hay base para comparar.

**Repeticiones: 3 arbitrajes por configuración y fase.** El mismo árbitro, con las mismas propuestas, no da siempre el mismo resultado, por dos motivos: el muestreo del modelo y el orden de las propuestas, que se sortea en cada arbitraje. Con una sola ejecución no se distingue una diferencia real de una casualidad; con 3 se ve cuánto varía cada configuración por sí sola. Es el mínimo razonable sin gastar demasiados tokens. Si los resultados salen muy dispersos, se añaden más.

**El arbitraje original cuenta como uno de los 3 de SOL:** es SOL juzgando esas mismas propuestas, con un orden también sorteado. Por eso, por fase se lanzan 3 arbitrajes nuevos con LUNA y 2 con SOL.

**Trabajos de origen (proyecto «Clinicas Veterinarias»):**
- Preguntas: `9637d55f-5c00-4bb5-a040-aeb01856a4cb` (15-10-2026, 08:52).
- Catálogo: `2b5e1fe9-e828-48a1-aaf5-0c63077c8cad` (15-10-2026, 10:11).

## Cómo se mide

Por cada arbitraje, el informe del trabajo (también en el CSV del proyecto, con las columnas `fase`, `modelo_arbitro` y `arbitraje_de`) da:
- el tamaño de la unión;
- los elementos que vienen de ambas propuestas, solo de A y solo de B;
- las discrepancias y los descartados;
- los tokens y la duración.

**La métrica principal es lo que entra solo de B**, en número y como porcentaje de la unión. Las propuestas son las mismas en todos los arbitrajes, así que cualquier diferencia viene del árbitro. No se compara quién predomina, sino **cuánto cambia la parte de cada uno al cambiar de árbitro**: un árbitro neutral también daría más peso a A, porque A propone más.

**Cómo se interpreta:**

| Resultado con LUNA de árbitro, frente a SOL | Conclusión |
| --- | --- |
| Lo que entra solo de B se solapa con los valores de SOL | H1: A es más completo; no hay preferencia |
| Lo que entra solo de B sube claramente, sin solaparse con SOL | H2: el árbitro prefiere a su propio modelo |
| Sube, pero A sigue predominando | Pasan las dos cosas: A es más completo y además hay algo de preferencia |

**Referencia independiente (pendiente):** con embeddings multilingües ejecutados en local, se podría emparejar por significado lo que proponen A y B sin ningún LLM. Así se sabría cuántas propuestas de B no tienen equivalente en A y, por tanto, cuántas podría haber incluido el árbitro.

## Estado (16-10-2026): cerrado

| Fase | SOL (original + 2) | LUNA (3) |
| --- | --- | --- |
| Preguntas | ✅ 3 de 3 | ✅ 3 de 3 |
| Catálogo | ✅ 3 de 3 | 2 válidos de 6 intentos; se cierra con 2, como se acordó |

## Incidencias

- **16-10-2026, catálogo con LUNA de árbitro, segundo intento:** el árbitro escribió un `de` con `"propuesta_2"` dos veces. La revisión de la plataforma falló con un error interno, porque `JsonNode` no admite claves repetidas, en lugar de pedir una reparación. Se corrigió el mismo día (`CLAVE-DUP`). Ese arbitraje no cuenta y se repite.
- **16-10-2026, catálogo con LUNA de árbitro, tercer intento:** el primer intento del árbitro necesitó reparación. La reparación (el prompt original más el catálogo anterior entero, unos 30.000 tokens, y sus errores) no cabía en el contexto junto con las dos propuestas. Claude Code intentó compactar, la compactación falló (`too_few_groups`) y las peticiones a la pasarela empezaron a fallar. No cuenta. Arreglo del mismo día: la plataforma junta ella misma las claves repetidas cuando son listas, sin reparación. Pendiente de decidir: reparar editando solo los elementos con errores, en lugar de reescribir la salida entera.
- **Dato para la evaluación:** como árbitro del catálogo, **LUNA falla más que SOL.** De 3 intentos, 1 terminó bien y 2 fallaron por errores de formato que la reparación no pudo arreglar. SOL terminó a la primera. Si solo se cuentan los arbitrajes válidos, LUNA se compara en sus mejores ejecuciones, así que la tasa de fallos se declara junto a los resultados.

- **16-10-2026, catálogo con LUNA de árbitro, quinto intento:**
  - el primer intento leyó toda la documentación y se quedó 15 minutos sin actividad (`HeartbeatLost`), y se repitió;
  - el segundo necesitó una reparación;
  - la reparación siguió con 7 errores, como `RF-066 TIPO` (un id `RF-` con tipo `RNF`).
  No cuenta.
- **16-10-2026, catálogo con LUNA de árbitro, sexto y último intento:**
  - el primer intento escribió un catálogo de 191 KB y el `Write` llegó cortado, porque la respuesta agotó el tope de salida (`TruncatedToolCall`);
  - la reparación escribió un JSON mal formado (`'{' is an invalid start of a property name`, línea 62).
  No cuenta. Como se acordó, el experimento se cierra con 2 arbitrajes válidos de LUNA en el catálogo.
- **Recuento final de LUNA como árbitro del catálogo:** 2 válidos de 6 intentos. SOL: 3 de 3. Los fallos de LUNA son de formato o de tamaño (clave repetida, reparación que no cabe, tipos mal puestos, salida cortada, JSON mal formado), no de criterio.

## Resultados

Proyecto grande de ejemplo («Clinicas Veterinarias»), 15 y 16-10-2026. En todos los arbitrajes de una fase las propuestas son las mismas: en las preguntas, 50 de A y 17 de B; en el catálogo, 299 de A y 192 de B. Las horas son UTC.

### Preguntas

| Árbitro | Arbitraje | Unión | De ambas | Solo de A | **Solo de B** | Descartadas | Reparaciones | Tiempo |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SOL | Original (15-10, 08:52) | 55 | 11 | 38 | **6** | 1 | — | — |
| SOL | Nuevo (16-10, 10:16) | 55 | 11 | 38 | **6** | 0 | 0 | 2,5 min |
| SOL | Nuevo (16-10, 10:19) | 55 | 11 | 38 | **6** | 0 | 0 | 2,5 min |
| LUNA | Nuevo (16-10, 07:37) | 52 | 11 | 35 | **6** | 3 | 0 | 3,4 min |
| LUNA | Nuevo (16-10, 08:11) | 55 | 11 | 38 | **6** | 0 | 0 | 6,9 min |
| LUNA | Nuevo (16-10, 10:10) | 54 | 10 | 38 | **6** | 2 | 1 | 4,6 min |

- **Lo que entra solo de B es idéntico en los 6 arbitrajes: 6 de las 17 preguntas de B.** Las otras 11 se unen con preguntas de A («de ambas»). No queda ninguna pregunta de B fuera de la unión que un árbitro de su mismo modelo pudiera rescatar.
- **SOL es totalmente estable** (55 / 11 / 38 / 6 en los tres arbitrajes). **LUNA varía un poco:** descarta entre 0 y 3 preguntas, pero siempre de A, y necesitó una reparación en uno de los tres.
- **Coste:** los arbitrajes nuevos de preguntas usaron entre 0,18 y 0,30 M tokens de entrada cada uno. El original costó 1,09 M porque incluye a A y a B.

### Catálogo

| Árbitro | Arbitraje | Unión | De ambas | Solo de A | **Solo de B** | Descartados | Reparaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SOL | Original (15-10, 10:11) | 209 | 180 | 28 | **1** | 3 | — |
| SOL | Nuevo (16-10, 10:22) | 220 | 183 | 30 | **7** | 0 | 0 |
| SOL | Nuevo (16-10, 10:31) | 291 | 264 | 27 | **0** | 3 | 0 |
| LUNA | Nuevo (16-10, 07:42) | 209 | 179 | 27 | **3** | 0 | 1 |
| LUNA | Nuevo (16-10, 10:39) | 204 | 177 | 27 | **0** | 0 | 1 |
| LUNA | Sexto intento (16-10, 11:36) | falló: salida cortada y después JSON mal formado | | | | | |

- **Lo que entra solo de B es muy poco con los dos árbitros:** 1, 7 y 0 con SOL (media 2,7) y 3 y 0 con LUNA (media 1,5; solo 2 arbitrajes válidos). LUNA, que es el modelo de B, no incorpora más propuestas de B que SOL; si acaso, menos.
- **Lo que entra solo de A es casi constante:** entre 27 y 30 con los dos árbitros.
- **El tamaño de la unión varía mucho con SOL** (209, 220 y 291) y poco con LUNA (209 y 204). La diferencia está en cuántos elementos fusiona: con 291, SOL junta menos y deja más requisitos separados. Con las mismas propuestas, el grado de fusión depende de cada ejecución: es una fuente de variabilidad que conviene declarar.
- **Fiabilidad:** SOL terminó los 3 arbitrajes a la primera. LUNA necesitó una reparación en los 2 válidos y falló en 4 intentos más (ver «Incidencias»).
- **Coste:** los arbitrajes del catálogo no tienen dato de consumo, porque todos se detuvieron al compactar con la salida ya escrita. Los de SOL tardaron unos 6 minutos; el válido de LUNA de las 10:39, 12 minutos con su reparación.

### Conclusión

**Se confirma H1: A es más completo, y no hay preferencia del árbitro por su propio modelo.** Con las mismas propuestas, cambiar el árbitro de SOL a LUNA no aumenta lo que entra solo de B:
- **en las preguntas:** 6 con los dos árbitros, en los 6 arbitrajes;
- **en el catálogo:** una media de 2,7 con SOL y de 1,5 con LUNA.

Que la unión se parezca a A se explica porque **A propone más** (50 frente a 17 preguntas y 299 frente a 192 requisitos) y porque **casi todo lo que propone B ya está en A** («de ambas»).

**Para la memoria:**
- **Positivo:** el diseño anónimo y en orden aleatorio de las propuestas no muestra sesgo de autopreferencia en estos datos.
- **A matizar:** la verificación cruzada aporta poco por lo que solo encuentra B, con estos dos modelos. Su valor está más en confirmar lo que coincide (el «de ambas») y en las discrepancias que en sumar elementos nuevos.
- **Hallazgo secundario:** LUNA es mucho menos fiable como árbitro de un catálogo grande (2 válidos de 6 intentos frente a 3 de 3 de SOL). En las preguntas, que son una salida pequeña, no falló ninguna vez: sus fallos aparecen cuando la salida es grande (un catálogo de 200 requisitos) y son de formato, no de criterio. Con estos modelos, SOL es la mejor elección de árbitro.

**Limitación de la muestra:** en el catálogo, LUNA solo tiene 2 arbitrajes válidos. La conclusión se apoya sobre todo en las preguntas (6 arbitrajes idénticos) y en que, en el catálogo, ningún valor de LUNA supera a los de SOL.

**Siguiente:** la referencia independiente con embeddings (hito 17, paso 87): cuántas propuestas de B no tienen ningún equivalente en A, sin depender de ningún LLM.

## Para la memoria

- **Método:** «Para aislar el efecto del árbitro se repitió solo el arbitraje sobre las mismas propuestas, cambiando el modelo del árbitro. Cada configuración se repitió tres veces para tener en cuenta la variabilidad de los modelos.»
- **Limitaciones a declarar:**
  - un solo proyecto, ficticio y generado con ayuda de Claude;
  - pocas repeticiones;
  - solo dos modelos, de la misma familia de la pasarela;
  - la métrica de origen depende de que el árbitro cite bien en `de` de qué propuesta sale cada elemento.
- **Si se confirma H2,** opciones de corrección para discutir: un árbitro de un modelo distinto al de A y B, o rotar los papeles entre ejecuciones.
