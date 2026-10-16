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

## Estado (16-10-2026)

| Fase | SOL (original + 2) | LUNA (3) |
| --- | --- | --- |
| Preguntas | original ✅, 0 de 2 nuevos | 1 de 3 |
| Catálogo | original ✅, 0 de 2 nuevos | 1 en curso |

## Resultados

Pendientes. Primer dato, sin valor estadístico todavía: en las preguntas, el primer arbitraje con LUNA dio una unión de 52 (11 de ambas, 35 solo de A y 6 solo de B, 3 descartadas), frente a 55 (11, 38 y 6, 1 descartada) del original con SOL. Lo que entra solo de B no cambia.

## Para la memoria

- **Método:** «Para aislar el efecto del árbitro se repitió solo el arbitraje sobre las mismas propuestas, cambiando el modelo del árbitro. Cada configuración se repitió tres veces para tener en cuenta la variabilidad de los modelos.»
- **Limitaciones a declarar:**
  - un solo proyecto, ficticio y generado con ayuda de Claude;
  - pocas repeticiones;
  - solo dos modelos, de la misma familia de la pasarela;
  - la métrica de origen depende de que el árbitro cite bien en `de` de qué propuesta sale cada elemento.
- **Si se confirma H2,** opciones de corrección para discutir: un árbitro de un modelo distinto al de A y B, o rotar los papeles entre ejecuciones.
