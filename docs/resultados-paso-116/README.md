# Paso 116: prueba real de las iteraciones del proyecto con clínicas

Prueba real del hito 22 sobre «Clinicas Veterinarias v0.11», el proyecto grande de ejemplo con el catálogo y el backlog de la iteración 1 validados. La ejecutó Claude por la API, a petición del autor, el 27-10-2026 y el 03-11-2026, con `SS - GPT-6-SOL` como A y árbitro y `SS - GPT-6-LUNA` como B. Prompts v0.12 en los modos de siempre y v0.13 en la ronda centrada y las actualizaciones.

## Qué se hizo

1. **Una segunda iteración de documentos ficticios** (`DOC-22` a `DOC-27`, unos 2.500 tokens; la iteración 1 tiene unos 23.400), con 18 cambios puestos a propósito y su resultado esperado: [`ejemplos/README.md`](../../ejemplos/README.md), «Grande: segunda iteración».
2. **Una hoja de respuestas del cliente**, escrita antes de lanzar nada: [`hoja-de-respuestas.md`](hoja-de-respuestas.md). Con ella se respondió y se revisó igual en los dos caminos.
3. **Opción A frente a C:** la ronda centrada experimental que lo lee todo (A) y la ronda centrada normal (C), sobre el mismo estado del proyecto.
4. **Camino «actualizar»:** ronda centrada (C) → respuestas → actualización del catálogo → revisión y validación → actualización del backlog → revisión y validación.
5. **Camino «desde cero»**, después y en el mismo proyecto (decisión del autor): ronda con todas las iteraciones → respuestas → catálogo desde cero → validación → backlog desde cero → validación. El paso 115 compara cada uno con lo actualizado.

Detalle de cada decisión: [`respuestas-camino-actualizar.md`](respuestas-camino-actualizar.md), [`revision-catalogo-actualizacion.md`](revision-catalogo-actualizacion.md), [`revision-backlog-actualizacion.md`](revision-backlog-actualizacion.md), [`respuestas-camino-desde-cero.md`](respuestas-camino-desde-cero.md) y [`revision-camino-desde-cero.md`](revision-camino-desde-cero.md). Las salidas de los árbitros están en [`salidas/`](salidas/).

## Resultados

### 1. Lo validado que se conserva

Un elemento se conserva si su texto es idéntico al de la versión validada de la iteración 1. Los ids no se comparan, porque desde cero se numera otra vez desde 1.

| | Iteración 1 validada | Actualizar | Desde cero |
| --- | --- | --- | --- |
| Catálogo | 235 requisitos (versión 1) | 244; **213 idénticos (91 %)** | 241; **1 idéntico (0 %)** |
| Backlog | 94 historias (versión 7) | 102; **89 idénticas (95 %)** | 89; **ninguna (0 %)** |

Por significado (paso 115, frente a lo actualizado y validado):
- **Catálogo:** 183 iguales, 58 cambiados, ningún nuevo y ninguno desaparecido.
- **Backlog:** 8 iguales, 69 cambiadas, 12 nuevas y 20 desaparecidas, que se reparten en otras historias o dejan de estar.

Desde cero el catálogo dice casi lo mismo con otras palabras, y el backlog se reorganiza entero.

### 2. Los cambios puestos a propósito

| # | Tema | Se espera | Ronda A (lo lee todo) | Ronda C (centrada) | Catálogo actualizado | Catálogo desde cero |
| --- | --- | --- | --- | --- | --- | --- |
| I2-1 | Teleconsulta | N | P | P | RF-204, 206, 207 (N) | RF-170 a 175 |
| I2-2 | Pedidos desde la app | N | P | P | RF-208 a 210 (N), RF-100 (M) | RF-177, RT-002 |
| I2-3 | Huellas Puerto | M | P (enlazada a RF-187) | P (enlazada a RF-001 y RF-187) | RF-001, 040, 118, 138, 139, 187 (M) | RF-044, 045, 107, 124 |
| I2-4 | Encuesta | N | P | P | RF-211 (N), RF-183 (M) | RF-179, 181 |
| I2-5 | 24 horas para cancelar | P enlazada; M | **P enlazada a P-004** | **P enlazada a P-004** | RF-062, 172 (M) | RF-036, 037, 038 |
| I2-6 | Señal | N o M | P | P | RF-203 (N) | RF-041 |
| I2-7 | Segundo factor | M | P (enlazada a RNF-006) | P (enlazada a RNF-006) | RNF-006 (M) | RNF-008 |
| I2-8 | Hospitalización en la fase 1 | P enlazada; M | **P enlazada a P-002** | **P enlazada a P-002** | RF-078, 079, 083, 091, 093 (M) | RF-077, 078 |
| I2-9 | Urgencias a 100 € | P enlazada o M | **P enlazada a P-059** | **P enlazada a P-059** | RF-136, 140 (M) | RF-109 |
| I2-10 | Precio de la teleconsulta | P | P | P | RF-205 (N) | RF-172 |
| I2-11 | Conservación | P enlazada; M | **P enlazada a P-001** | **P enlazada a P-001** | RF-024 (M) | RF-194, 196 |
| I2-12 | Horario de Puerto | P | P | P | RF-187 (M) | RF-045 |
| I2-13 | Cita para varias mascotas | E | P (enlazada a P-042) | P (enlazada a P-042) | RF-059 (E) | no está |
| I2-14 | Fotos en el informe | M o E | P (enlazada a RF-090) | P (enlazada a RF-090) | RF-090 (M) | RF-090 (sin fotos) |
| I2-15 | Quién ve la encuesta | P | P | P | RF-212 (N) | RF-180 |
| I2-16 | Teleconsulta y plan | P | P | P | RF-205 (N) | RF-172 |
| I2-17 | Plan Adulto, igual | — | — | — | sin cambios | — |
| I2-18 | Aislamiento y disponibilidad, igual | — | — | — | sin cambios | — |

P: pregunta; N: requisito nuevo; M: modificación propuesta; E: eliminación propuesta.

- **Las dos rondas centradas detectan las 8 preguntas esperadas** (I2-5, 8, 9, 10, 11, 12, 15 y 16), y las 4 que contradicen una respuesta salen **enlazadas a esa respuesta**, sin reescribirla. Las dos enlazan además a los requisitos validados que contradicen: 11 enlaces en A y 10 en C.
- **Los dos catálogos recogen los 14 cambios que llegan al catálogo**, y ninguno toca lo que no cambia (I2-17 e I2-18).
- **La revisión del camino «actualizar»:** las 23 propuestas del catálogo (22 modificaciones y 1 eliminación) y las 22 del backlog (21 y 1) corresponden todas a un cambio I2 o a una respuesta. Ninguna se rechazó y ninguna toca algo que la iteración 2 no cambia.
- **La ronda con todas las iteraciones del camino «desde cero» no es comparable:** vio respondidas las preguntas de la ronda centrada, porque se hizo después en el mismo proyecto (decisión 1 del autor). Solo hizo 9 preguntas, 7 de ellas de la iteración 1 que se habían quedado sin hacer.

### 3. Coste y tiempo

| Camino | Trabajo | Resultado | Entrada | Salida | Coste (USD) | Minutos | Reparaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A frente a C | Ronda que lo lee todo (A) | válida | 479.748 | 46.865 | 0,95 | 5 | 1 |
| Actualizar | Ronda centrada (C) | válida | 860.932 | 57.277 | 1,67 | 9 | 0 |
| Actualizar | Sugerencias de respuesta | válidas | 174.635 | 9.156 | 0,44 | 1 | 0 |
| Actualizar | Actualización del catálogo | válida | 1.249.680 | 97.932 | 2,53 | 12 | 0 |
| Actualizar | Actualización del backlog | válida | 798.513* | 51.006* | 1,22* | 7 | 0 |
| Desde cero | Ronda con todas las iteraciones | válida | 396.468 | 22.470 | 0,71 | 3 | 0 |
| Desde cero | Sugerencias de respuesta | válidas | 176.092 | 4.975 | 0,40 | 1 | 0 |
| Desde cero | Catálogo, primer intento | **fallido (LUNA)** | 438.617* | 68.846* | 0,78* | 23 | 2 |
| Desde cero | Catálogo | válido | 778.741* | 133.420* | 2,17* | 14 | 1 |
| Desde cero | Backlog | válido | 527.043 | 78.936 | 1,38 | 7 | 0 |

\* Con estimación de las ejecuciones que la plataforma detuvo con la salida ya escrita (paso 95).

| Camino | Coste (USD) | Minutos | Sin el intento fallido |
| --- | --- | --- | --- |
| **Actualizar** (ronda C, sugerencias, catálogo y backlog) | **5,85** | **29** | — |
| **Desde cero** (ronda, sugerencias, catálogo y backlog) | **5,44** | **48** | 4,66 $ y 25 min |

Aparte, para la prueba: dos rondas que lo leían todo que no valen (0,88 $ y 1,80 $, ver «Incidencias»).

- **Actualizar no sale más barato que empezar de cero** con esta iteración. La ronda centrada (1,67 $) costó más del doble que la ronda con todas (0,71 $), y la actualización del catálogo (2,53 $), algo más que el catálogo desde cero (2,17 $). El backlog sí sale algo más barato actualizando (1,22 $ frente a 1,38 $).
- **Por qué:** los prompts de actualizar hacen leer entera la documentación nueva, lo relacionado y los requisitos relacionados, y dejan consultar por tramos la documentación y el catálogo enteros. SOL consulta mucho: en la actualización del catálogo, A leyó unos 780.000 tokens, frente a los 209.000 de A desde cero. Cada consulta vuelve a enviar todo el contexto. Desde cero se lee todo una sola vez.
- **Lo que sí ahorra actualizar es trabajo del usuario:** hay que revisar 23 propuestas y 5 discrepancias en el catálogo, y 22 propuestas y 2 discrepancias en el backlog, con lo demás ya validado. Desde cero hay que revisar de nuevo 241 requisitos (21 con discrepancia) y 89 historias (20), todos con otras palabras.

### 4. Opción A (leerlo todo) frente a C (lo nuevo y lo relacionado)

| | A, lo lee todo | C, centrada |
| --- | --- | --- |
| Preguntas (de A, de B, unión) | 28, 16, **29** | 27, 13, **28** |
| Preguntas esperadas detectadas | **8 de 8** | **8 de 8** |
| Enlazadas a una respuesta o a un requisito | 11 | 10 |
| Tokens de entrada (A, B, árbitro) | 175.818, 158.577, 145.353 | 292.744, 185.306, 382.882 |
| Coste | **0,95 $** | **1,67 $** |
| Reparaciones | 1 (LUNA numera desde P-064) | 0 |

- **Con esta iteración, leerlo todo es igual de bueno y más barato.** La iteración 2 es pequeña y la 1 cabe entera sin problema (unos 23.400 tokens): leerla de una vez cuesta menos que consultarla por tramos.
- **La opción C se diseñó para documentaciones grandes,** donde leerlo todo no cabe o se compacta, como el documento B, con más de 80.000 tokens. Esta prueba no lo mide. Es una sola ejecución de cada una, y el coste de una misma fase varía mucho entre ejecuciones.

### 5. Incidencias

1. **Un fallo de la plataforma del paso 112, arreglado** (`fix:` del 27-10-2026). Una pregunta enlazada a una respuesta (`contradice` P-001) se rechazaba siempre con `REF-PREG`, porque el plan de una ronda no lleva preguntas respondidas. La primera ronda que lo leía todo falló por eso, y la segunda corrió sin el arreglo porque la API no se había reconstruido (Docker Hub falló sin que se viera): las reparaciones quitaron las preguntas enlazadas y no vale para medir. Desde entonces, tras cada reconstrucción se comprueba la fecha de la imagen.
2. **LUNA numera las preguntas de las rondas centradas desde P-064**, siguiendo las del proyecto, en lugar de desde P-001 como pide el prompt. Pasó en las 3 rondas que lo leían todo, y no en la centrada. La reparación lo corrigió dos veces y una no.
3. **LUNA falló el primer catálogo desde cero:** se quedó 15 minutos sin actividad, se repitió, escribió `should` en `notacion` (solo vale EARS o MASTeR) y la reparación terminó diciendo «hecho» sin corregirlo. El error de esquema dice la posición en el JSON (`requisitos/74/notacion`) y no la línea, como los demás errores.
4. **nginx corta a los 60 s** y lanzar una ronda centrada o una actualización en clínicas tarda más (embeddings de lo relacionado). Devuelve 504 aunque la tarea sí se lanza. En la web parecería un error.
5. **Desde cero deja más cosas sin decidir:** 18 «[POR DEFINIR]» nuevos en el catálogo (29 frente a 11) y 14 en el backlog (38 frente a 24). Ante la respuesta «el cliente no lo ha decidido: la opción más sencilla de las que propones», la actualización eligió esa opción, y desde cero lo dejó sin definir.

## En palabras sencillas

Cuando el cliente manda documentos nuevos, hay dos formas de seguir: rehacer el análisis desde cero o actualizar lo que ya está validado. Se probaron las dos con el mismo proyecto y los mismos documentos nuevos, en los que se habían escondido 18 cambios conocidos.

- **Los dos caminos encuentran todos los cambios.** Las preguntas señalan además qué respuesta anterior contradice cada novedad, y no la reescriben.
- **Actualizar conserva el trabajo hecho:** 9 de cada 10 requisitos y casi todas las historias validadas quedan igual, y el usuario solo revisa unas 25 propuestas de cambio en cada entregable.
- **Desde cero lo reescribe todo,** aunque diga casi lo mismo, y obliga a revisarlo entero otra vez.
- **En dinero cuestan lo mismo,** unos 5–6 $, porque actualizar consulta mucho lo anterior. Su ventaja está en el tiempo del usuario.
- **Leer solo lo nuevo y lo relacionado no ahorró nada:** con una documentación de este tamaño, leerla entera sale más barato. Queda por probar con una documentación grande.

## Qué hay que mejorar

Propuestas para decidir con el autor. **Decisión del autor (03-11-2026):** la 1, la 2 y la 3 se hacen en el paso 122; la 5 es el paso 123; la 4 no se hace, como recomendaba Claude: con una sola medición no se sabe si el coste es estable, y cambiaría la opción C.

1. **nginx:** subir `proxy_read_timeout` en las rutas que lanzan tareas (por ejemplo, a 5 minutos), para que la web no muestre un 504 cuando la tarea sí se ha lanzado.
2. **La numeración de LUNA en las rondas centradas:**
   - o una corrección automática más (`PlatformFixes`, solo en las rondas centradas), que renumere desde P-001 unas preguntas nuevas seguidas que empiezan en otro número;
   - o decirlo más claro en el prompt.
   La plataforma ya las renumera al guardarlas.
3. **El error de esquema con su línea:** que diga la línea del fichero, como los demás errores, para que la reparación por Edit la encuentre.
4. **El coste de actualizar:** limitar lo que A y B consultan, o probar a darles la documentación entera de una vez cuando sea pequeña: con menos de unos 30.000 tokens, leerla entera es más barato.
5. **Medir la opción C con una documentación grande** antes de darla por buena, y repetir las rondas para conocer la variación.
6. **Las revisiones de preguntas pendientes** (reformular, resuelta, obsoleta) no se probaron: en el proyecto no había ninguna pendiente.
