# Paso 127: candidatos para el modelo B por OpenCode Go

Estudio de Claude, del 10-11-2026, sin coste.

**Decisión del autor (10-11-2026):** los candidatos son **Kimi K3.1 Pro** y **Gemini 4 Argon**, y **Claude Haiku 5.5** queda de reserva. Haiku se prueba en el paso 129, pasa al 130 solo si Kimi o Argon fallan en el 129, y solo puede ser B si fallan los dos. Antes de decidir, el autor revisó los descartados con su propia comparación (ver «Revisión con la lista del autor»).

Todos los datos son del **10-11-2026**:
- la lista de modelos, los precios, los límites y la privacidad, de la documentación oficial de OpenCode Go;
- las comparativas, de Artificial Analysis y BenchLM, con páginas actualizadas ese mismo día.

La lista de Go cambia a menudo: antes de contratar (paso 129) hay que comprobar que los candidatos siguen ahí y con el mismo precio.

## Resumen

**Solo dos modelos de Go razonan claramente como SOL: Kimi K3.1 Pro y Gemini 4 Argon.** Los dos superan a SOL en el índice general de Artificial Analysis y en Terminal-Bench, la prueba de trabajo como agente con herramientas.

**No hay un tercero de ese nivel.** El siguiente es Claude Haiku 5.5, a medio camino entre LUNA y SOL. Va solo como reserva, con un reparo: no cumple del todo la condición obligatoria (ver «Decisiones del autor»).

| | Kimi K3.1 Pro | Gemini 4 Argon | Claude Haiku 5.5 (reserva) |
|---|---|---|---|
| Fabricante | Moonshot AI | Google | Anthropic |
| Id en Go | `kimi-k3.1-pro` | `gemini-4-argon` | `claude-haiku-5-5` |
| Índice de Artificial Analysis (SOL 44, LUNA 28) | **46** | **50** | 36 |
| Precio por millón de tokens (entrada / salida) | 2 / 10 $ | 4 / 20 $ (2 / 10 $ hasta el 30-11-2026) | 0,10 / 0,50 $ (0,50 / 2,50 $ con más de 100.000 tokens de contexto) |
| Límite de Go al mes | 15 $ | 30 $ | 15 $ |
| Protocolo | traducido (OpenAI Chat Completions) | traducido (Google) | **Anthropic, nativo** |
| Retención de datos | 0 días | 0 días | 30 días |
| Riesgo principal | lento: puede rozar los 30 minutos por ejecución | caro cuando acabe el descuento | por debajo de SOL |

## Cómo se ha medido «razona como SOL»

La condición es obligatoria (autor, 10-11-2026): el nuevo B tiene que razonar como SOL. El precio solo decide entre los que la cumplen.

**SOL es GPT-6 Sol** (OpenAI, septiembre de 2026). Artificial Analysis le da **44** en su índice general (versión 5.0.1, con el esfuerzo máximo; 42 con esfuerzo alto). A LUNA (GPT-6 Luna), **28** con el esfuerzo máximo.

**Qué pruebas separan a LUNA de SOL.** Se buscan las que separan a los dos modelos que ya conocemos, porque esa es la diferencia que no queremos repetir:
- **Índice general de Artificial Analysis** (razonamiento, conocimiento, código, agentes y documentos): SOL 44, LUNA 28. Separa.
- **Terminal-Bench 4.x** (trabajo como agente con herramientas, en varios pasos): SOL 50,3, LUNA 15,2. Separa mucho, y es lo más parecido a lo que hace B en Concilia, que trabaja con `Read`, `Write` y `Edit` durante muchos turnos.
- **AA-LCR** (razonar sobre unos 100.000 tokens de documentos): SOL 83,7, LUNA 83,3. **No separa**: LUNA lee igual de bien que SOL en esta prueba. Por eso no decide, aunque se da.

Regla aplicada: **un candidato tiene que igualar o superar a SOL en el índice y en Terminal-Bench.**

### Comparación con SOL y LUNA

| Prueba | SOL | LUNA | Kimi K3.1 Pro | Gemini 4 Argon | Claude Haiku 5.5 |
|---|---|---|---|---|---|
| Índice de Artificial Analysis v5.0.1 | 44 | 28 | **46** | **50** | 36 |
| Terminal-Bench 4.x (agente con herramientas) | 50,3 (4.1) | 15,2 (4.1) | **65,7** (4.0) | **57,4** (4.0) | 39,2 (4.0, del fabricante); 32,8 (Artificial Analysis) |
| AA-LCR (documentos largos) | 83,7 | 83,3 | 86,3 | 79,7 | 82,7 |
| GDPval-AA (tareas de oficina con documentos) | 50,4 | 46,6 | sin dato | 56,2 | 56,0 |
| Respuestas inventadas cuando no sabe (AA-Omniscience; menos es mejor) | 60,1 % | 79,3 % | sin dato | **4,3 %** | 34,2 % |
| Velocidad de salida (tokens/s) | 116 | sin dato | 32 | 66 | 254 |
| Segundos hasta el primer token de respuesta | 18 | sin dato | 157 | 37 | 182 |

Fuentes: el índice y la velocidad, de las fichas de Artificial Analysis; el resto, de las fichas de BenchLM, que recogen las de Artificial Analysis y las de cada fabricante. Las versiones de Terminal-Bench no son exactamente la misma (4.0 y 4.1), así que la comparación es aproximada; la distancia entre LUNA y los demás es tan grande que no cambia la conclusión.

**Lo que no se ha encontrado** en fuentes públicas, para ningún candidato: pruebas de seguir instrucciones largas, de escribir JSON con un esquema ni de español. Se comprueban en nuestras pruebas: el español y la línea del veredicto en el paso 129, y el JSON y la fidelidad a la fuente con el catálogo del paso 130.

## Los tres candidatos

### 1. Kimi K3.1 Pro (Moonshot AI)

- **Razonamiento:** 46 en el índice, 2 puntos por encima de SOL. En Terminal-Bench 4.0, 65,7 frente a 50,3 de SOL: el mejor de Go como agente. El mejor en documentos largos (AA-LCR 86,3).
- **Precio:** el mismo que SOL, 2 $ de entrada y 10 $ de salida por millón de tokens (caché, 0,20 $).
- **Privacidad:** no entrena con los datos y los guarda 0 días. Se sirve con las regiones por defecto (Europa y EE. UU.), sin activar «Global».
- **Pesos abiertos** (publicado el 23-10-2026, 2,8 billones de parámetros y 104.000 millones activos, 1 millón de tokens de contexto).
- **Riesgos:**
  - **Lento.** Artificial Analysis mide 32 tokens/s y 157 s hasta la primera respuesta (SOL: 116 y 18), y tarda 37,7 minutos de media en cada tarea de su índice. Una fase de Concilia tiene 15 minutos sin salida y 30 en total. Un catálogo de clínicas de A escribe unos 44.000 tokens: a 32 tokens/s son 23 minutos solo escribiendo. Puede agotar el tiempo. La velocidad depende de dónde lo sirva OpenCode, así que se mide en el paso 129; si hace falta más tiempo, es configuración (`Catalog:TotalTimeout`), no código.
  - **Protocolo traducido:** habla OpenAI Chat Completions, y OpenCode traduce las peticiones de Claude Code. Hay que comprobar que las llamadas a herramientas, el razonamiento y los tokens del evento final llegan bien (paso 129).

### 2. Gemini 4 Argon (Google)

- **Razonamiento:** 50 en el índice, el más alto de Go. Terminal-Bench 4.0, 57,4. Destaca en no inventar: solo un 4,3 % de respuestas inventadas cuando no sabe, frente al 60,1 % de SOL y el 79,3 % de LUNA. Para un B que tiene que ser fiel a la fuente, es una ventaja.
- **Precio:** 4 $ de entrada y 20 $ de salida, el doble que SOL. Hasta el 30-11-2026 tiene un 50 % de descuento (2 / 10 $). A cambio, su límite en Go es el doble (30 $ al mes), así que en Go caben tantos flujos como con Kimi.
- **Privacidad:** no entrena con los datos y los guarda 0 días; regiones por defecto.
- **Publicado** el 30-09-2026, 1 millón de tokens de contexto, cerrado.
- **Riesgos:**
  - **Coste para una empresa:** el doble que Kimi o que SOL cuando acabe el descuento. Lo que cuesta la verificación cruzada sube.
  - **Protocolo traducido:** habla el protocolo de Google, el más distinto del de Anthropic. El mismo riesgo que Kimi, quizá mayor.

### 3. Claude Haiku 5.5 (Anthropic), de reserva

- **Razonamiento:** 36 en el índice: 8 puntos por encima de LUNA y 8 por debajo de SOL. En Terminal-Bench, 32,8–39,2 frente a 50,3 de SOL. **No cumple la regla.** En tareas de oficina con documentos (GDPval-AA, 56,0) supera a SOL, e inventa menos (34,2 %).
- **Por qué se propone, aun así:** es el único modelo de Go razonablemente bueno que habla el protocolo de Anthropic de forma nativa, el que usa Claude Code, sin traducción. Si Kimi y Argon fallan en el paso 129 por la traducción (herramientas rotas, eventos que la consola no lee, tokens que no llegan), sería la única salida sin cambiar de runner.
- **Precio:** muy barato: 0,10 / 0,50 $ hasta 100.000 tokens de contexto en la petición, 0,50 / 2,50 $ por encima.
- **Privacidad:** no entrena con los datos, pero los guarda 30 días.
- **Riesgo:** el mismo que LUNA, aunque menor: que no tenga capacidad suficiente. Por eso, si se aprueba, propongo que solo se elija en el paso 130 si los dos primeros fallan.

## Descartados

Lista de Go del 10-11-2026 (32 modelos). Índice de Artificial Analysis v5.0.1 cuando lo hay.

| Modelo | Fabricante | Motivo |
|---|---|---|
| GPT 6 Luna | OpenAI | Es LUNA: mismo fabricante y el modelo que se sustituye. |
| GPT 5.6 Luna | OpenAI | Mismo fabricante que SOL (familia GPT). |
| Iceman Contributor Free | sin dato | Entrena con los datos y es gratuito. |
| Muse Spark 1.3 Contributor, Muse Spark 1.2 Contributor | Meta | Entrenan con los datos. Además, Muse Spark 1.3 saca 37 en el índice y 14,9 en Terminal-Bench 4.1, al nivel de LUNA como agente. |
| Beam Free, LongCat 2.5 Free | sin dato, Meituan | Gratuitos, por tiempo limitado. Beam 1.0 saca 18 en el índice. LongCat-2.5 no tiene ninguna medición independiente, se sirve en China y exige la región «Global». |
| DeepSeek V4 Pro, DeepSeek V4.1 Flash | DeepSeek | Exigen activar la región «Global» (las reglas del plan piden Europa y EE. UU.), y su acuerdo de no guardar datos se renueva cada mes (hasta el 30-11-2026). En el índice v5.0.1, V4 Pro saca 23 (53 en la v4.1.1; las versiones no se comparan) y V4.1 Flash, 33. Los dos inventan más que LUNA (94,1 % y 96,5 %). |
| Grok 4.7 | xAI | 35: por debajo de SOL. Terminal-Bench 4.0, 25,8–38,0. Protocolo traducido (Responses) y 30 días de retención. |
| Tencent Hy4, Hy4 preview, Hy3 | Tencent | Hy4, 35: por debajo de SOL. Los otros son una versión previa y la generación anterior. |
| Qwen3.8 Max | Alibaba | 34: por debajo de SOL. Habla Anthropic de forma nativa y es fuerte en documentos largos (MRCRv2 92,9) y en seguir instrucciones (IFBench 82,8): es la alternativa a Haiku 5.5 como reserva, pero cuesta 2 / 6 $, más que Haiku, con menos datos públicos. |
| Qwen3.8 Flash, Qwen3.7 Plus | Alibaba | Versión pequeña y anterior de un fabricante cuyo mejor modelo ya queda por debajo. |
| GLM-5.3, GLM-5.2, GLM-5.3-Flash | Zhipu | GLM-5.3, 32 (esfuerzo máximo): por debajo de SOL. Los otros, anterior y pequeña. |
| MiMo-V2.6-Pro, MiMo-V2.5-Pro, MiMo-V2.6-Flash, MiMo-V2.5 | Xiaomi | MiMo-V2.6-Pro, 30: por debajo de SOL. Los otros, anterior o pequeños. |
| Step 5 Preview | StepFun | 29, casi como LUNA, y en versión previa. |
| MiniMax M3, MiniMax M3.1 Flash | MiniMax | MiniMax M3, 19: por debajo de LUNA. |
| Kimi K3 | Moonshot AI | Lo supera su sucesor, Kimi K3.1 Pro, que además es más barato (K3: 3 / 15 $). Sin dato en la v5.0.1 (43,6 en la v4.3.2, donde K3.1 Pro tenía 55). |
| Kimi K2.7 Code | Moonshot AI | Generación anterior, especializada en código. |
| LongCat-2.0 | Meituan | Sin calidad comprobable: no aparece en las comparativas públicas. |

## Revisión con la lista del autor (10-11-2026)

El autor comparó los modelos descartados en <https://opencode.ai/es/data/compare>, fijándose sobre todo en *reasoning* y en la longitud del contexto, y propuso nueve. Comprobación de Claude, el mismo día.

**Qué mide esa página.** Su versión en texto (`opencode.ai/data/compare/…/….md`, por ejemplo GPT-6 Sol frente a DeepSeek V4 Pro) da ficha técnica, precio y uso dentro de OpenCode: ventana de contexto, salida máxima, *Reasoning* «Sí/No», llamadas a herramientas, pesos abiertos, fecha, precio, tokens y usuarios. **No da ninguna prueba de calidad.**
- *Reasoning* dice si el modelo piensa antes de responder, no cómo de bien razona: todos los de la lista dicen «Sí», igual que SOL y LUNA.
- La ventana de contexto es casi la misma en todos (de 984.000 a 1,1 millones de tokens), y en Concilia B trabaja con 128.000 por decisión del autor, así que no distingue a ninguno.

**Por qué en algunas comparativas parecen iguales que SOL.** Las cifras que suben a un modelo al nivel de SOL suelen venir de:
- **Terminal-Bench 2.1**, una versión antigua y ya saturada: casi todos los fabricantes declaran un 86–91 % (GPT-5.6 Sol, 85,8 % en Vals). La versión difícil, la 4.x, es la que separa: SOL 50,3, LUNA 15,2.
- **Versiones antiguas del índice de Artificial Analysis** (v4.x), que BenchLM mezcla con la actual: DeepSeek V4 Pro tenía 53 en la v4.1.1 y tiene 23 en la v5.0.1. O del informe del propio fabricante: el 46,3 de MiMo-V2.6-Pro en BenchLM es de Xiaomi; Artificial Analysis le da 30.

**Comprobación, modelo a modelo.** Índice v5.0.1 de Artificial Analysis (SOL 44, LUNA 28) y Terminal-Bench 4.x (SOL 50,3, LUNA 15,2 y 12,6), con GDPval-AA (tareas de oficina con documentos; SOL 50,4) y la tasa de respuestas inventadas (SOL 60,1 %, LUNA 79,3 %) como contraste. Precios de Go.

| Lo que dice el autor | Índice | Terminal-Bench 4.x | GDPval-AA | Inventa | Precio en Go (entrada / salida) | Veredicto |
|---|---|---|---|---|---|---|
| Muse Spark 1.3: gran rendimiento y más barato que SOL | 37 | **14,9** (4.1) | 59,0 | 32,9 % | 0,10 / 0,20 $ | Más barato, **sí**. Gran rendimiento, **no**: como agente con herramientas está al nivel de LUNA. Además, Meta ya lo ha sustituido (Muse Wonder). |
| Beam 1.0: como SOL y muy barato | **18** | sin dato | sin dato | sin dato | gratis, por tiempo limitado | Barato, **sí**. Como SOL, **no**: por debajo de LUNA. |
| LongCat-2.5: como SOL y muy barato | sin dato independiente | 38,5 (4.0, del fabricante) | sin dato | sin dato | gratis, por tiempo limitado | Barato, **sí**. Como SOL, **no se puede comprobar**: nadie lo ha medido fuera de Meituan, y sus propias cifras lo dejan por debajo. Se sirve en China y exige la región «Global». |
| DeepSeek V4 Pro: rendimiento increíble y bastante barato | **23** (máx.) | sin dato | 54,5 | **94,1 %** | 1,32 / 3,96 $ (la mitad fuera de hora punta) | Barato, **sí**. Rendimiento increíble, **no** con la versión actual del índice. Es el que más inventa de todos. Exige la región «Global». |
| DeepSeek V4.1 Flash: muy bueno y muy barato | 33 (máx.) | 42,3 (4.0, del fabricante) | 55,0 | **96,5 %** | 0,15 / 0,60 $ (el doble en hora punta) | Muy barato, **sí**. Muy bueno, **a medias**: el mejor de esta lista como agente, pero por debajo de SOL, e inventa todavía más. Exige la región «Global». |
| Qwen3.8 Max: muy bueno y coste como SOL | 34 | sin dato (2.1: 67,4 en Vals) | sin dato | sin dato | 2 / 6 $ | Coste como SOL, **sí** (algo menos en la salida). Muy bueno, **a medias**: fuerte en documentos largos (MRCRv2 92,9) y en seguir instrucciones (IFBench 82,8), pero 10 puntos por debajo de SOL en el índice. |
| GLM: más barato que SOL y mejores características | 32 (máx.) | 41,2 (4.1); 41,9 (Artificial Analysis, 4.0) | 57,5 | 38,9 % | 1,40 / 4,40 $ | Más barato, **sí**. Mejores características, **a medias**: mejor que SOL en tareas de oficina y en no inventar; peor en el índice y como agente. |
| MiMo-V2.6-Pro: como SOL y muy barato | 30 | 34,8 (Artificial Analysis, 4.0) | 59,2 | 60,2 % | 0,435 / 0,87 $ | Muy barato, **sí**. Como SOL, **no**: 14 puntos por debajo en el índice y 15 como agente; mejor en tareas de oficina. |
| Kimi K3: buenas características y gratis | sin dato en la v5.0.1 | sin dato (2.1: 80,9 en Vals) | 51,6 | 53,2 % | **3 / 15 $** | Gratis, **no**: es el más caro de Go, más que SOL. Buenas características, **sí**, pero su sucesor Kimi K3.1 Pro (candidato 1) es mejor y más barato. |

Fuentes de esta tabla: las fichas de Artificial Analysis (Muse Spark 1.3, Kimi K3, GLM-5.2 y la tabla general) y de BenchLM (`deepseek-v4-pro`, `deepseek-v4-1-flash`, `muse-spark-1-3`, `mimo-v2-6-pro`, `glm-5-3`, `kimi-k3`, `qwen3-8-max`), todas del 10-11-2026; LongCat-2.5, de las notas de su lanzamiento, que recogen la tabla de Meituan (<https://www.drweb.de/longcat-2-5-meituan-chinesische-chips/>, <https://myclaw.ai/es/blog/longcat-2-5>).

**Lo que sí es cierto y conviene tener presente:** en GDPval-AA (tareas de oficina con documentos, lo más parecido a redactar requisitos de los que hay) casi todos superan a SOL (54,5–59,2 frente a 50,4). Pero esa prueba apenas separa a LUNA de SOL (46,6 frente a 50,4), y lo que falló con LUNA fue el trabajo largo con herramientas: leer por partes, escribir el JSON y reparar. Ahí, en Terminal-Bench 4.x, ninguno de los nueve llega a SOL.

**Conclusión: los dos candidatos no cambian.** Si se quiere un tercero por capacidad, el mejor de la lista del autor es **GLM-5.3**: el más cerca de SOL como agente (41–42 frente a 50), sin exigir «Global» y sin inventar tanto, a 1,40 / 4,40 $. Pero su protocolo también es traducido (Chat Completions), así que no cubre ese riesgo, y Claude Haiku 5.5 sí.

**Los dos criterios que el autor relaja:**
- **Mejores que SOL:** siempre se han admitido. La regla era igualar o superar a SOL, y los dos candidatos lo superan.
- **Que entrenen con los datos:** con documentos ficticios o públicos, el riesgo para la privacidad es bajo, así que no parece mal. Pero en la práctica no cambia nada:
  - los únicos de Go que entrenan con los datos son los Muse Spark Contributor (como agente, al nivel de LUNA) e Iceman Contributor Free (gratuito, sin datos públicos);
  - activarlo es un ajuste de todo el espacio de trabajo («Allow paid endpoints that train on request data»), así que valdría para cualquier documento que se subiera por error;
  - los gratuitos lo son «por tiempo limitado»: pueden desaparecer a mitad de la evaluación 2 y no tienen precio de lista con el que calcular el coste para una empresa.

## Protocolo y cliente

**Claude Code como cliente** (documentación de OpenCode, consultada el 10-11-2026):
- dirección `https://opencode.ai/inference/go/anthropic`, sin `/v1/messages` (lo añade Claude Code);
- la clave, en `ANTHROPIC_AUTH_TOKEN`, no en `ANTHROPIC_API_KEY`;
- el modelo, con el id de Go (`kimi-k3.1-pro`…), que el runner ya pasa con `--model`.

Coincide con lo que el plan prevé para el paso 128.

**Traducción:** según esa documentación, la dirección acepta cualquier modelo de Go y «traduce la petición para los modelos que no aceptan Anthropic Messages». No dice cómo trata las herramientas, el razonamiento, la caché ni el *streaming*.

**Lo que hay que comprobar en el paso 129** con cada candidato (ya está en el plan, salvo lo marcado como nuevo):
- que las llamadas a `Read`, `Write` y `Edit` funcionan y que la consola lee los eventos;
- que el evento final trae los tokens, también los de caché (nuevo: ver «Coste»);
- la velocidad (nuevo: el riesgo de Kimi);
- qué pasa al llegar al límite a mitad de una tarea: la documentación no lo dice, solo que con «Extra Usage» se sigue pagando y sin él se puede seguir con los modelos gratuitos;
- que el límite de cada modelo es independiente: la documentación lo da a entender («el límite mensual de cada modelo determina cómo cuenta su uso»), pero no lo dice expresamente.

## Coste

### Cuánto gasta B en un flujo

Un modelo del nivel de SOL como B gastará más o menos lo que gasta A, que es SOL haciendo la misma tarea. En el flujo completo de clínicas (preguntas, catálogo y backlog; `docs/costes/costes.md`, sección 5), **A gastó 722.587 tokens de entrada y 119.953 de salida**. LUNA, como B, 564.794 y 109.117, por 0,11 $.

| B | Precio (entrada / salida, $ por millón) | B en un flujo de clínicas | Flujo completo para una empresa (A + B + árbitro + sugerencias, sin mockup) |
|---|---|---|---|
| LUNA (hoy, real) | 0,10 / 0,50 | 0,11 $ | 6,79 $ |
| Kimi K3.1 Pro | 2 / 10 | 2,65 $ | 9,33 $ (+37 %) |
| Gemini 4 Argon, precio normal | 4 / 20 | 5,29 $ | 11,97 $ (+76 %) |
| Gemini 4 Argon, hasta el 30-11-2026 | 2 / 10 | 2,65 $ | 9,33 $ (+37 %) |
| Claude Haiku 5.5 | 0,10–0,50 / 0,50–2,50 | 0,13–0,66 $ | 6,81–7,34 $ |

A, el árbitro y las sugerencias se dejan como en ese flujo (2,64 $, 3,69 $ y 0,35 $). Es una estimación: el paso 131 da la cifra real.

**Las cifras son un techo.** Concilia suma a la entrada las lecturas de caché y las cobra a precio completo (`ResultUsage.TotalInputTokens`), y en una ejecución larga de Claude Code casi toda la entrada se repite de un turno a otro. En Go la caché cuesta un 90 % menos (Kimi, 0,20 $; Argon, 0,20 $ con el precio normal). Si OpenCode aplica la caché a lo que llega de Claude Code, lo gastado del límite será bastante menos. En el paso 129 se mira cuánta entrada llega como caché, y en el 128 conviene decidir si `precios.json` lleva también el precio de la caché (ver «Lo que tiene que decidir el autor»).

### El resto del proyecto en meses de Go

Lo que usará el modelo elegido como B de aquí al depósito, en tokens estimados como arriba (A como referencia):

| Paso | Qué hace B | Entrada | Salida |
|---|---|---|---|
| 129 | prueba de humo (cada candidato, con su propio límite) | 0,05 M | 0,01 M |
| 130 | una ronda de preguntas y un catálogo (cada candidato, con su propio límite) | 0,35 M | 0,05 M |
| 131 | un flujo de clínicas | 0,72 M | 0,12 M |
| 133 | la ronda de preguntas de PURE | 0,06 M | 0,01 M |
| 143 | uno o dos flujos de clínicas con la v0.14 | 0,72–1,44 M | 0,12–0,24 M |
| 134 | el flujo de PURE | 0,22 M | 0,05 M |
| 119 | tres flujos de clínicas | 2,17 M | 0,36 M |
| 136 | árbitro: 3 de preguntas y, si cabe, 3 de catálogo | 0,60–3,30 M | 0,06–0,26 M |
| **Total** | | **4,9–8,3 M** | **0,78–1,10 M** |

| Modelo elegido | Coste a precio de lista | Límite de Go al mes | Meses de Go | Lo que limita |
|---|---|---|---|---|
| Kimi K3.1 Pro | 17,6–27,6 $ | 15 $ | 1,2–1,8 (dos meses) | 7,50 $ a la semana: los tres flujos del paso 119 (unos 7,95 $) no caben en una semana; un flujo (2,65 $) cabe justo en las 5 horas (3 $). |
| Gemini 4 Argon | 35,2–55,2 $ | 30 $ | 1,2–1,8 (dos meses) | Lo mismo, con límites del doble (15 $ a la semana, 6 $ cada 5 horas). |
| Claude Haiku 5.5 | 4,4–6,9 $ | 15 $ | menos de uno | Nada. |

Coincide con lo que estimaba el plan (23–25 $, unos dos meses de Go con un modelo del nivel de SOL). Con la caché, menos. Antes de cada tarea con B se estimará su coste con las ejecuciones parecidas y se comparará con lo que queda, como dice el plan.

**Entre los dos que cumplen la condición, el precio favorece a Kimi K3.1 Pro:** el mismo coste que SOL para una empresa, la mitad que Argon. En meses de Go salen igual, porque Argon tiene el doble de límite.

## Decisiones del autor

1. **Los candidatos (10-11-2026):** Kimi K3.1 Pro y Gemini 4 Argon, los dos que cumplen la condición.
2. **El tercero (10-11-2026):** Claude Haiku 5.5, de reserva, como recomendaba Claude. Se valoraron tres opciones:
   - **solo dos candidatos:** respetaba la condición al pie de la letra, pero sin salida si la traducción del protocolo rompe a los dos a la vez;
   - **Claude Haiku 5.5 (elegido):** en capacidad, empatado con GLM-5.3 (índice 36 frente a 32; como agente, 33–39 frente a 41–42), y los dos por debajo de SOL. Pero es el único con el protocolo nativo de Claude Code, así que cubre el único riesgo que comparten Kimi y Argon. Si funciona y ellos no, además, demuestra que el problema es la traducción y no el runner. Su prueba de humo cuesta céntimos de su propio límite;
   - **GLM-5.3:** el mejor de la lista del autor como agente, pero pasa por la misma traducción que Kimi, así que no cubre ese riesgo.
3. **Mejores que SOL y que entrenen con los datos** (10-11-2026): al autor no le importa ninguna de las dos cosas. Lo primero ya se admitía, y lo segundo no cambia la elección (ver «Revisión con la lista del autor»). La norma de usar solo documentos ficticios o públicos con OpenCode Go sigue igual.
4. **Pendiente, para el paso 128: el precio de la caché.** La duda es si `precios.json` y `costes.cs` cobran las lecturas de caché a su precio o, como hoy, a precio completo. Hoy todo el coste de Concilia se calcula sin descuento de caché, también el de SOL, y cambiarlo solo para B haría las cifras incomparables. Claude recomienda dejarlo como hoy (un techo, comparable) y anotar aparte los tokens de caché para cuadrar con la consola de OpenCode.

## En palabras sencillas

Para elegir el nuevo B no se ha mirado el precio primero, sino si piensa tan bien como SOL. Se ha usado una idea simple: las pruebas públicas que distinguen a LUNA de SOL (la nota general de Artificial Analysis y una prueba de trabajar con herramientas durante muchos pasos) son las que importan, porque esa diferencia es justo la que hizo fallar a LUNA. Solo dos modelos de OpenCode Go sacan en esas pruebas lo mismo o más que SOL: Kimi K3.1 Pro, de la empresa china Moonshot AI, y Gemini 4 Argon, de Google. Kimi cuesta lo mismo que SOL y Gemini el doble. Los dos llegan a Claude Code a través de una traducción de OpenCode, que hay que probar antes de fiarse. Un tercer modelo, Claude Haiku 5.5, no llega al nivel de SOL, pero no necesita esa traducción; por eso se propone solo como plan de reserva.

## Fuentes (consultadas el 10-11-2026)

- OpenCode Go: <https://opencode.ai/docs/go> (modelos, precios, límites, protocolo, privacidad, regiones, «Extra Usage»).
- Claude Code con OpenCode: <https://opencode.ai/docs/console/harnesses/claude-code/>.
- Artificial Analysis, fichas y tabla general (índice v5.0.1, velocidad, precios):
  - <https://artificialanalysis.ai/leaderboards/models>
  - <https://artificialanalysis.ai/models/gpt-6-sol>
  - <https://artificialanalysis.ai/models/kimi-k3-1-pro>
  - <https://artificialanalysis.ai/models/gemini-4-argon>
  - <https://artificialanalysis.ai/models/claude-haiku-5-5>
  - <https://artificialanalysis.ai/models/mimo-v2-6-pro>
  - <https://artificialanalysis.ai/en/models/deepseek-v4-pro>
- BenchLM, fichas por modelo (Terminal-Bench, AA-LCR, GDPval-AA, AA-Omniscience; actualizadas el 10-11-2026):
  - <https://benchlm.ai/models/gpt-6-sol>
  - <https://benchlm.ai/models/gpt-6-luna>
  - <https://benchlm.ai/models/kimi-3-1-pro>
  - <https://benchlm.ai/models/gemini-4-argon>
  - <https://benchlm.ai/models/claude-haiku-5-5>
  - <https://benchlm.ai/models/qwen3-8-max>
  - <https://benchlm.ai/models/grok-4-7>
  - <https://benchlm.ai/models/kimi-k3>
- Sobre las versiones del índice (los 53 de DeepSeek V4 Pro y los 55 de Kimi K3.1 Pro son de la v4.x, no comparables con la v5.0.1): <https://officechai.com/ai/deepseek-v4-0813-pro-benchmarks/>, <https://rits.shanghai.nyu.edu/ai/kimi-k3-hits-third-on-artificial-analysis-open-weights-due-july-27/>.
