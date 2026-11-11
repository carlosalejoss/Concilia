# Paso 145: SOL 6.1 frente a SOL 6

Prueba del 11-11-2026, pedida por el autor antes del paso 128, para decidir si A, el árbitro y las sugerencias pasan de `SS - GPT-6-SOL` a `SS - GPT-6.1-SOL`.

**Por qué se planteó:**
- SOL 6.1 es bastante más capaz en las fuentes públicas: 55 en el índice de Artificial Analysis frente a 44, y 60,9 en Terminal-Bench 4.1 frente a 50,3.
- Cuesta lo mismo por token (2 / 10 $) y su caché es más barata (0,10 $ frente a 0,20 $).
- Artificial Analysis da SOL 6 por sustituido.
- En contra, escribe a la mitad de velocidad (57 tokens/s frente a 116), y el 14-10-2026 agotó el tiempo dos veces: como árbitro y en el diseño del mockup.

El autor pidió comparar solo datos objetivos (tiempo, coste, fiabilidad y cifras), sin revisar a mano el contenido.

## Cómo se hizo

Proyecto de clínicas (21 archivos), prompts v0.12 y Claude Code 2.1.283, con el código de `main` del 10-11-2026 (`3737ad7`).

**Qué cambia y qué no:**
- **Cambia** el modelo de A, el árbitro y las sugerencias. Se cambiaba recreando la API con `RUNNER_MODEL_A`, `RUNNER_MODEL_ARBITER` y `RUNNER_MODEL_SUGGESTIONS`, siempre sin ninguna tarea en marcha, y sin tocar `.env`.
- **B es LUNA** en todo, así que no influye en la comparación: no ve nada de A.

**Cada fase de los dos modelos parte de la misma entrada**, como en el paso 119:
1. **Preguntas:** una ronda en cada proyecto, «Clínicas SOL 6» y «Clínicas SOL 6.1». Tienen que ser proyectos distintos, porque una segunda ronda ve las preguntas de la primera.
2. **Sugerencias:** en cada proyecto, con su modelo. Todas resultaron suposiciones, así que «Aprobar todas» no aprueba ninguna. Se guardaron tal cual (origen `ia`).
3. **Catálogo:** los dos en «Clínicas SOL 6.1», con las mismas 92 preguntas respondidas: primero con SOL 6.1 y después con SOL 6.
4. **Validación:** el de SOL 6 (329 requisitos), sin editarlo. Solo se marcaron como revisadas sus 21 discrepancias.
5. **Backlog:** los dos desde ese mismo catálogo validado.

**Incidencia del montaje.** El primer catálogo con SOL 6.1 se bloqueó a los 48 segundos:
- **La causa:** la plataforma toma el nombre del proyecto como nombre del sistema («el sistema Clínicas SOL 6.1»), así que la ronda preguntó si la documentación, que habla de «Plataforma Huellas», era la de ese sistema (P-091). Su respuesta se guardó tal cual, «A decidir por el cliente: confirmar…», y SOL 6.1 se negó a escribir el catálogo con esa duda abierta.
- **El arreglo, en los dos proyectos:** el nombre del sistema pasó a ser «la Plataforma Huellas» y la pregunta se respondió con una confirmación expresa (P-091 en uno, P-001 en el otro).
- **Lo que no se sabe:** si SOL 6 se habría bloqueado igual. Comprobarlo costaba otro catálogo entero.

El intento bloqueado se cuenta aparte.

**Límites:**
- Es una sola ejecución por modelo y fase. El mismo catálogo varió antes entre 2,06 y 3,92 $.
- Las duraciones dependen también de la carga de la pasarela.
- No hay tokens de caché por separado: hasta el paso 128 la plataforma solo guarda el total de entrada. Todo va a precio completo, que para los dos modelos es el mismo (2 / 10 $).

## Resultados

### Por trabajo

Minutos del trabajo entero, con A y B en paralelo y después el árbitro. `*`: incluye ejecuciones sin dato de consumo, estimadas con `UsageEstimator` (paso 95). `≥`: con ejecuciones sin dato ni estimación.

| Fase | Modelo | Resultado | Minutos | Entrada | Salida | Coste (USD) | Reparaciones |
|---|---|---|---|---|---|---|---|
| Preguntas | SOL 6 | válido | 7,6 | 420.028 | 51.106 | 1,01 | 1 (de B) |
| Preguntas | SOL 6.1 | válido | 24,9 | 971.640 | 72.931 | 2,37 | 0 |
| Sugerencias | SOL 6 | válido | 2,3 | 145.805 | 13.000 | 0,42 | 0 |
| Sugerencias | SOL 6.1 | válido | 11,0 | 177.257 | 26.360 | 0,62 | 0 |
| Catálogo | SOL 6 | válido | 13,3 | 919.693* | 154.649* | 2,46* | 1 (de B) |
| Catálogo | SOL 6.1 | válido | 28,7 | 1.057.733* | 116.017* | 2,67* | 1 (de B) |
| Catálogo, bloqueado (montaje) | SOL 6.1 | fallido | 5,9 | ≥ 371.036 | ≥ 35.730 | ≥ 0,33 | 1 (de B) |
| Backlog | SOL 6 | **fallido** | 22,6 | 734.005* | 73.153* | 1,63* | 3 |
| Backlog | SOL 6.1 | **fallido** | 30,8 | 2.989.067* | 118.012* | 6,59* | 2 |

**Total de la prueba: 18,10 $**: 5,52 $ con SOL 6 y 12,58 $ con SOL 6.1, contando el intento bloqueado. LUNA supone céntimos.

### Por papel

Minutos de la ejecución más larga del papel.

| Fase | Papel | SOL 6: minutos | SOL 6.1: minutos | SOL 6: entrada / salida | SOL 6.1: entrada / salida | SOL 6: USD | SOL 6.1: USD |
|---|---|---|---|---|---|---|---|
| Preguntas | A | 2,8 | 14,6 | 124.596 / 15.285 | 130.671 / 30.480 | 0,40 | 0,57 |
| Preguntas | Árbitro | 2,8 | 10,2 | 209.254 / 17.175 | 765.547 / 26.166 | 0,59 | 1,79 |
| Sugerencias | Sugerencias | 2,3 | 11,0 | 145.805 / 13.000 | 177.257 / 26.360 | 0,42 | 0,62 |
| Catálogo | A | 4,1 | 15,1 | 218.237 / 32.612 | 212.188 / 30.459 | 0,76 | 0,73 |
| Catálogo | Árbitro | 5,3 | 13,5 | 444.203* / 75.759* | 644.972* / 61.513* | 1,65* | 1,91* |
| Backlog | A | 18,6 (la primera, sin actividad 15 min) | 15,3 (con una reparación) | 189.030* / 34.421* | 199.732* / 60.898* | 0,72* | 1,01* |
| Backlog | Árbitro | 2,5 (dos intentos, fallidos) | 15,5 (dos intentos, fallidos) | 404.272* / 6.968* | 2.659.920* / 23.620* | 0,88* | 5,56* |

### Fiabilidad

- **SOL 6.1 no agotó ningún tiempo máximo.** Su ejecución más larga duró 15,5 minutos, frente a los 30 en total y 15 sin salida que tiene cada fase. Ya no pasa lo del 14-10-2026, gracias a las mejoras del runner de después. Pero va justo: con la mitad de velocidad, una tarea algo mayor rozaría el tope.
- **Reparaciones de SOL:** una de SOL 6.1 en el backlog, por escribir `quier` en lugar de `quiero` en una historia (`SCHEMA`). SOL 6 no necesitó ninguna.
- **Los dos backlogs fallaron igual.** El árbitro empezó a compactar el contexto dos veces sin haber escrito la salida, en los dos intentos y con los dos modelos. El catálogo validado tiene 329 requisitos (los de clínicas tenían antes unos 200–220), y unir los dos backlogs no cabe en 128.000 tokens. Es un límite de la plataforma con catálogos grandes, no de un modelo, y queda anotado como problema a resolver.
- **En el backlog de SOL 6, A y B se quedaron 15 minutos sin salida a la vez** y terminaron en la repetición. Que fuera en los dos modelos a la vez apunta a la pasarela.
- **Bloqueo:** SOL 6.1 se negó a seguir con una respuesta pendiente sobre la identidad del sistema (ver «Incidencia del montaje»).
- **LUNA** necesitó una reparación en 4 de sus 7 trabajos y su corrección `REF-LINEA` actuó 8 veces en la ronda de SOL 6.1. Son datos para el paso 132.

### Contenido, solo en cifras

| | SOL 6 | SOL 6.1 |
|---|---|---|
| Preguntas de A | 45 | 90 |
| Preguntas de la unión | 45 (10 de ambas, 34 solo de A, 1 solo de B) | 92 (11 de ambas, 79 solo de A, 2 solo de B) |
| Requisitos de A, con las mismas 92 respuestas | 305 | 234 |
| Requisitos de la unión | 329 (211 de ambas, 94 solo de A, 24 solo de B) | 190 (126 de ambas, 62 solo de A, 2 solo de B) |
| Discrepancias y desviaciones del árbitro en el catálogo | 11 y 11 | 21 y 8 |
| Requisitos que se apoyan en una suposición de la IA | 217 | 145 |
| Acuerdo del árbitro con los embeddings (catálogo) | 83,0 % | 84,1 % |

LUNA dio 249 requisitos en el catálogo de SOL 6 y 144 en el de SOL 6.1 con la misma entrada: lo que aporta B también varía mucho entre ejecuciones.

## Lo que dicen los datos

- **Tiempo:** SOL 6.1 tarda entre 2,2 y 4,8 veces más por trabajo, y entre 3,6 y 5,2 veces más por ejecución de A o de sugerencias. Un flujo de preguntas, sugerencias y catálogo pasa de unos 23 minutos a unos 65.
- **Coste:** cuesta lo mismo por token, pero escribe más y su árbitro lee más. Con los mismos pasos que salieron bien:
  - preguntas, sugerencias y catálogo suman 3,89 $ con SOL 6 y 5,66 $ con SOL 6.1 (+46 %);
  - el árbitro de preguntas leyó 3,7 veces más, porque había el doble de preguntas.
- **Volumen:**
  - en preguntas, SOL 6.1 hace el doble (90 frente a 45), y eso es el doble de trabajo para el cliente o el analista, que tiene que responderlas;
  - en el catálogo, con las mismas respuestas, hace menos requisitos (234 frente a 305), y la unión queda en 190 frente a 329.

  Sin revisar el contenido no se puede decir cuál es mejor; solo que son muy distintos.
- **Fiabilidad:** parecida. Ninguno agota el tiempo y los dos fallan igual en el backlog de un catálogo tan grande.

## Decisión

**El autor decide quedarse con SOL 6 (11-11-2026), como recomendaba Claude.** A, el árbitro, las sugerencias y el mockup siguen con `SS - GPT-6-SOL`.

**Por qué:**
- SOL 6.1 tarda entre 2,2 y 4,8 veces más por trabajo y cuesta un 46 % más en las fases que salieron bien, sin ganar fiabilidad.
- El flujo de preguntas, sugerencias y catálogo pasaría de 23 a 65 minutos. Si B acaba siendo Kimi K3.1 Pro, que también es lento, se alargaría aún más.
- Ni esta prueba ni el uso con SOL 6 muestran un problema de capacidad que SOL 6.1 resuelva.
- El doble de preguntas supone el doble de trabajo para quien las responde.

**Se vigila:** que la empresa no retire SOL 6 de la pasarela. Si lo hace, esta prueba dice lo que cuesta cambiar.

**Se anota, sin decidir todavía:** el backlog de un catálogo de unos 330 requisitos no cabe en el contexto del árbitro con ninguno de los dos modelos. Va en el plan como problema abierto.

## En palabras sencillas

Se probó si la versión nueva del modelo principal (SOL 6.1), que en las pruebas públicas razona mejor, compensa frente a la que usamos (SOL 6). Se hizo la misma tarea con los dos, partiendo de lo mismo, y se midió cuánto tardan y cuánto cuestan. SOL 6.1 tarda entre dos y cinco veces más y cuesta casi la mitad más. Además, hace el doble de preguntas al cliente y menos requisitos, así que su resultado es bastante distinto. Los dos fallaron igual al montar el backlog de un catálogo muy grande, lo que es un límite de la plataforma y no de un modelo.
