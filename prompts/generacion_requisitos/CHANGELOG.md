# Cambios del prompt de generación de requisitos

## v2 (2026-10-05)

Cambio de proceso, no solo de prompt. La v1.4 (05-10-2026, documento técnico interno) dio 255 y 214 requisitos y 118 y 86 preguntas, con la misma operación repetida hasta 6 y 7 veces por modelo: el límite era el trabajo por lotes de secciones, en el que ningún lote ve a los demás. La v2 genera el catálogo **de arriba abajo**, en dos fases: primero un inventario global de funcionalidades con el documento entero y después los requisitos de cada funcionalidad, cada una en un solo lote. No se fija un número de requisitos ni de preguntas: el catálogo tiene que estar completo, sin repeticiones y con solo lo que es requisito. La propuesta está en `docs/propuesta-catalogo-v2.md` y la evidencia en `docs/evolucion-prompt-requisitos.md`. Los prompts, los anexos, los esquemas y los ejemplos se redactaron en el proyecto de Claude.

**Carpeta**
- Nuevos `prompt_inventario_v2.md`, `anexo_reparacion_inventario_v2.md`, `prompt_requisitos_v2.md`, `anexo_reparacion_requisitos_v2.md`, `inventory_v2.schema.json` y `requirements_v2.schema.json`.
- `ejemplo/` (ReservaSalas, `es`): nuevos `fuentes_v2/DOC-01.txt` y `fragmentos_v2.json`, con los 14 fragmentos de la v1.3 idénticos y 7 más. Los nuevos son una matriz de permisos, una sección de backend, una de deuda técnica y un resumen, para que el ejemplo tenga una función descrita en varios sitios. También `inventario_ejemplo_v2.json` y `catalogo_ejemplo_v2.json`.
- `ejemplo_en/` (LibraryLoan, `en`): `inventario_ejemplo_v2.json` y `catalogo_ejemplo_v2.json` sobre el `fragmentos.json` actual.
- No cambian `fragmentos.json` ni los ejemplos v1.3, que usa `test_validador.py`. Los ficheros v1.4 se mantienen mientras la plataforma ejecute la v1.4.

**Fase 1: prompt de inventario (nuevo)**
- **Entradas:** el documento **entero** (todos los fragmentos), el índice, el glosario y las dudas resueltas. Estas dos no estaban en la propuesta. Sin las dudas, el inventario volvería a preguntar lo contestado en la fase de dudas iniciales.
- **Nivel:** una funcionalidad es lo que un usuario o un sistema externo reconocería como una función del producto, como en la v1.4. No son funcionalidades propias, sino fragmentos de la suya:
  - otro actor que hace lo mismo;
  - un dato, un límite, un mensaje o un error;
  - una variante por condición o estado;
  - la calidad exigida a esa función;
  - la misma función descrita en otra sección.
- **Unión de repeticiones:** la misma función descrita en el resumen, los permisos, la pantalla, el backend, la API o un anexo es una sola, con todos sus fragmentos. Un fragmento puede pertenecer a varias funcionalidades.
- **`kind`**, con `description` y `actors`:
  - `functional` e `integration`: su `name` va en infinitivo;
  - `quality`: un atributo de calidad del sistema en su conjunto, o de una función que no está en el inventario; una por atributo;
  - `constraint`: arquitectura, entorno o tecnología impuestos, o normativa; una por tema;
  - en `quality` y `constraint`, el `name` es un sustantivo y `actors` va vacío.
  - La calidad exigida a una función concreta va en los fragmentos de esa función, no en una `quality`, para que no se escriba dos veces.
- **Descartes:** aquí pasan los motivos de la v1.3, la observabilidad, `project_note` y el descarte por sección de la v1.4.
  - Un descarte es siempre del fragmento **entero** (sin `quote`): la parte de un fragmento útil que explica cómo está hecho el sistema simplemente no se cita en la fase 2.
  - Un fragmento técnico que solo repite algo ya asignado se descarta; si añade algo observable, se asigna.
- **Preguntas, solo globales:** las que afectan al alcance o a más de una funcionalidad. Tipos `scope`, `contradiction`, `ambiguity` y `missing_actor` (la propuesta solo pedía las dos primeras). Ids `QI-…`. Ninguna pregunta sobre una sola funcionalidad ni "¿qué pasa si falla?".
- **Salida:** `output/inventario.json`, con una sola escritura, y la línea de veredicto de siempre.
- **Anexo:** C10 (fragmentos sin tratar, comprobando antes que no son una funcionalidad ya inventariada), IDS y C6.

**Fase 2: prompt de requisitos (a partir de la v1.4)**
- **Más corto:** 30.630 bytes, frente a 32.217 (−5 %).
  - Pierde el inventario (§5), los descartes y `discarded_sections`, N4, la autoverificación y `self_check`.
  - Gana las entradas nuevas, los tipos por `kind`, las objeciones y las reglas de preguntas.
- **Entradas nuevas:**
  - `{{INVENTARIO}}`: todas las funcionalidades, solo ids, `kind` y nombres, para no invadir las de otros lotes.
  - `{{PREGUNTAS_INVENTARIO}}`.
  - `{{FUNCIONALIDADES}}`: las del lote, con actores y descripción.
  - En `{{DOCUMENTACION}}`, cada fragmento lleva el atributo `caps` con sus funcionalidades del lote.
- **Tipos por `kind`:**
  - `functional` e `integration` necesitan un `RF` o `RI` y admiten `RNF` para su calidad;
  - `quality` necesita un `RNF`;
  - `constraint` necesita un `RA`, `RT` o `RNF` de normativa.
  - El mapeo de la propuesta no recogía la normativa (D2) ni la calidad de una función concreta.
- **Unidad:** las reglas U se renumeran de U1 a U8.
  - U0 desaparece: la cobertura la garantiza el inventario.
  - **U2:** un requisito por comportamiento distinto, y no más (el principal, las variantes que cambian la respuesta y la calidad exigida).
  - **U4 cambia respecto a la v1.4:** actores distintos que hacen lo mismo son **un** requisito. La respuesta nombra a los actores como la fuente y los permisos que difieren van en `[RULE]`. En la v1.4 eran requisitos distintos, y LUNA partía los permisos en un requisito por rol.
  - Los errores siguen siendo `[ERROR]` (U7).
- **Fragmentos compartidos:** de un fragmento que pertenece a varias funcionalidades solo se escribe lo que toca al lote.
- **Objeciones** (`capability_objections`, `duplicate` | `not_a_requirement` | `future`): si el inventario se equivocó, la fase 2 lo declara en vez de forzar un requisito. No estaba en la propuesta. Sin esto, C20 obligaba a escribir algo para cada funcionalidad: el "sacar algo de cada trozo" de la v1.x, ahora por funcionalidad.
- **Preguntas:**
  - solo las que bloquean;
  - una por duda;
  - sin repetir las `QI-…` ni las dudas resueltas; un requisito puede enlazar una `QI-…` en `open_question_ids`;
  - sin `missing_error_handling`;
  - ante la duda, ninguna, salvo la `missing_value` obligatoria de N3.
- **Se conserva:**
  - EARS y MASTeR con slots y frase generada por la plataforma;
  - el árbol D1–D5, R-a a R-h y la obligatoriedad;
  - N1–N3 y N5–N7 (se mantienen los códigos; N4 se retira);
  - las citas literales, los tipos y los criterios etiquetados;
  - una sola escritura;
  - la independencia: no se menciona al otro modelo ni al árbitro, y se habla de "el inventario" sin decir de dónde viene, para que valga igual si algún día el árbitro fusiona los inventarios.
- **Anexo:** sin C10 ni edición del inventario. Ante un C20, se escribe el requisito o, si de verdad no lo hay, una objeción.

**Esquemas 2.0**
- `inventory_v2.schema.json`:
  - `capabilities` (`id`, `name`, `description` ≤ 200, `kind`, `actors`, `fragments` con al menos uno);
  - `discarded_statements` sin `quote`, `discarded_sections` y `open_questions` (`QI-…`, cuatro tipos, `related_capabilities`);
  - `verdict` (`status`, `issues`, `unprocessed_fragments`).
- `requirements_v2.schema.json`: el 1.3, con estos cambios:
  - sin `capabilities`, `discarded_statements`, `discarded_sections` ni `verdict.self_check`;
  - nuevo `capability_objections`; `duplicate_of` es obligatorio si y solo si `reason` es `duplicate`;
  - pregunta sin `missing_error_handling`;
  - `verdict.uncovered_capabilities` sustituye a `unprocessed_fragments`.
- Como en la 1.3, `capability_ids` no lleva `minItems` en el esquema: un requisito sin funcionalidad es el aviso C21, no un rechazo.

**Validador: comprobaciones nuevas o cambiadas.** Se cambian a la vez el validador de referencia en Python y su port en C#; `test_validador.py` necesita mutaciones para cada una.

*Inventario (nuevo: `validar_inventario.py` y su port):*
- **SCHEMA:** un fallo en la raíz es error del inventario y provoca la reparación. Un elemento que no cumple el esquema se ignora con aviso; sus fragmentos quedan sin tratar y los recoge C10.
- **IDS (error del inventario):** `CAP-…` o `QI-…` repetidos. Las referencias de `related_capabilities` a ids inexistentes y los fragmentos que no existen se quitan (`FIX`).
- **C10 de la fase 1 (error del inventario, umbral 0):** cada fragmento `DOC-…` está en una funcionalidad, en un descarte, en una sección descartada o en la cita de una pregunta. Cualquier fragmento sin tratar provoca la reparación, no a partir del 10 % como en la v1.x: en la v2, un fragmento sin asignar no llega a ningún lote. Lo que quede tras la reparación se lista en el informe. Las `DUDAS#…` no cuentan.
- **C6:** citas de las preguntas (igual que hoy).
- **Exclusividad (`FIX`, lógica de la v1.4):**
  - un fragmento que está en una funcionalidad y en un descarte pierde el descarte;
  - una sección descartada con algún fragmento asignado se sustituye por descartes enteros de sus demás fragmentos.
- **REP (aviso):** posible repetición, cuando dos funcionalidades tienen un Jaccard de fragmentos ≥ 0,5 o nombres normalizados muy parecidos. No cuentan los fragmentos que comparten 3 funcionalidades o más (resúmenes, matrices). Los umbrales son iniciales y hay que calibrarlos.
- **KIND (aviso):** `actors` no vacío en una `quality` o `constraint`.
- **VERDICT:** `partial` si y solo si `unprocessed_fragments` no está vacío.
- **Normalizador:** renumera `CAP-` y `QI-` en orden; desde aquí son fijos.

*Requisitos (`validar_catalogo.py` pasa a la v2):*
- **Entradas nuevas:** el inventario normalizado y los ids de las funcionalidades del lote (parámetros nuevos).
- **C10 desaparece en esta fase:** la cobertura de fragmentos es de la fase 1.
- **C23 (aviso nuevo):** un fragmento del lote que no cita ningún requisito ni pregunta. Mide la pérdida de detalle sin obligar a citarlo.
- **C20 generalizado (reparación):** cada funcionalidad del lote sin objeción tiene al menos un requisito aceptado del tipo que exige su `kind`. Hoy solo cuenta `RF`/`RI` (`FUNCTIONAL_TYPES`), así que saltaría en toda `quality` y `constraint`. Los ejemplos v2, pasados por el validador v1.3, lo confirman: su único fallo es C20 en esas funcionalidades.
- **C21 (aviso), ahora para todos los tipos.** El normalizador rellena `capability_ids` cuando el lote tiene una sola funcionalidad y quita las que no son del lote (`FIX`).
- **C24 (aviso nuevo):** el tipo del requisito no lo admite el `kind` de su funcionalidad.
- **Objeciones** (`OBJECTION[n]`):
  - `capability_id` debe ser del lote, y `duplicate_of` debe existir en el inventario y ser distinto; si no, se ignora la objeción con aviso;
  - **C25 (aviso nuevo):** un requisito vinculado a una funcionalidad objetada;
  - las objeciones se listan en el informe.
- **Referencias:** las `QI-…` del inventario son válidas en `open_question_ids`; el normalizador no las quita como ids desconocidos.
- **Retirados:** `SELF_CHECK`, la métrica `autoverificacion_coincide` y las correcciones de descartes, que pasan al validador del inventario. `missing_error_handling` lo rechaza ya el esquema.
- **VERDICT:** `partial` si y solo si `uncovered_capabilities` no está vacío.
- **Informe nuevo, en sustitución de N4:** las funcionalidades `functional` e `integration` sin ningún criterio `[ERROR]` en sus requisitos aceptados. Es una lista, no preguntas.

*Unión (`CatalogMerger`):*
- No renumera `CAP-…`. El catálogo final incluye las funcionalidades y los descartes del inventario.
- **Preguntas:** une las `QI-…` del inventario y las `Q-…` de los lotes, y renumera todas como `Q-…`.
  - Fusiona las del mismo tipo con texto idéntico tras normalizar (`FIX`), una red de seguridad: en la v1.4, unas 15 preguntas de LUNA eran iguales salvo el requisito.
  - Las ordena por tipo: `contradiction`, `scope`, `missing_value`, `ambiguity`, `missing_actor`. No se recorta ninguna.
- Quita los `domain_terms` repetidos.

**Plataforma (fuera del validador)**
- **Runner:** una llamada de fase 1 por modelo, con `output/inventario.json` y una reparación. Hay que vigilar el tope de salida (64.000) y la inactividad en LUNA.
- **Tamaño de la entrada:** las etiquetas `<f id=… sec=…>` añaden unos 7.000 tokens a los 25.000–30.000 del documento grande. Sigue cabiendo en 128.000.
- **`LotPlanner`:** agrupa las funcionalidades por salida estimada; cada una va a un único lote. La estimación hay que calibrarla.
- **`PromptBuilder` y `fragmentar.py`:**
  - marcadores `{{INVENTARIO}}`, `{{PREGUNTAS_INVENTARIO}}`, `{{FUNCIONALIDADES}}` e `{{INVENTARIO_PREVIO}}`;
  - atributo `caps` en el bloque de documentación de la fase 2.
- **Versiones:** registrar `inv-2.0` y `req-2.0`.
- **Pendientes de la v1.4 que siguen valiendo:**
  - validar el catálogo o el inventario aunque el modelo se declare `bloqueada`;
  - generar el informe de métricas automáticamente;
  - diagnosticar por qué LUNA tarda más.

**Comprobación de los ejemplos (05-10-2026)**
- Los cuatro JSON cumplen sus esquemas v2.
- C10 de la fase 1: 0 fragmentos sin tratar y ninguno asignado y descartado a la vez.
- C23: ningún fragmento sin citar.
- C20 generalizado: cubierto en los dos ejemplos.
- Validador v1.3 sobre los catálogos convertidos al formato 1.3: 0 rechazos (C2, C5, C6, C7, C12, C15, N2, N3) y frases generadas correctas. El único aviso esperado es C20 en `quality`/`constraint`, por el cambio descrito arriba.
- El objeto `RF-001` y la tabla de fragmentos de los prompts coinciden con los ficheros de ejemplo.

## v1.4 (2026-10-05)

Ajustes de la v1.3 tras su primera ejecución real (02-10-2026, SOL y LUNA, 22 lotes del documento técnico interno). La propuesta y la evidencia están en `docs/propuesta-prompt-v1.4.md` y `docs/evolucion-prompt-requisitos.md`. El prompt y el anexo se redactaron en el proyecto de Claude.

Se conserva todo lo que funcionó en la v1.3: independencia, EARS y MASTeR con slots y render, tipos, criterios etiquetados, inventario y cobertura antes que detalle. **El esquema no cambia (1.3).** El prompt no crece: 32.217 bytes, frente a 32.203 de la v1.3.

**Carpeta**
- Nuevos `prompt_requisitos_v1.4.md` y `anexo_reparacion_v1.4.md`. Los ficheros v1.3 siguen en el historial de git.

**Prompt**
- §3, **observabilidad** (propuesta 1). Un requisito describe lo que un usuario, un responsable del negocio o un sistema externo integrado puede observar, usar o exigir.
  - Un comportamiento interno de la implementación es `design_detail` aunque esté escrito como "el sistema hace…" (cabeceras HTTP, CORS, comprobaciones de salud, migraciones, registros, limitación de tasa, formato de errores…). Excepciones: lo que la fuente impone (`RT`/`RA`) y lo que exige como atributo de calidad (`RNF`, uno por atributo, no uno por mecanismo).
  - Si el índice muestra que la sección describe cómo está construido el sistema, se parte de que es `design_detail`.
  - Una API que usan sistemas externos es `RI`; la que solo usa el propio frontend, no.
  - Evidencia: el 38 % de los requisitos de SOL y el 17 % de los de LUNA salían de las secciones de diseño técnico, casi todos comportamientos internos escritos como `RF`.
- §3, **`project_note`** (propuesta 2, con el matiz que decidió el autor). Lo que habla del proyecto, su historia o su código es `project_note` aunque mencione funciones del sistema: decisiones pasadas, errores conocidos, deuda, riesgos, cómo modificar el sistema. Si el texto dice qué **debe** hacer el sistema y lo construido difiere, el requisito es lo que debe hacer, con una pregunta `contradiction`. Evidencia: el 20 % (SOL) y el 29 % (LUNA) de los requisitos salían de las secciones finales del documento.
- §3, **descartes enteros** (propuesta 4). Un fragmento del que sale algún requisito, criterio o pregunta no se descarta entero. Una sección solo se descarta entera si no describe nada que el sistema haga y no se cita ninguno de sus fragmentos. Evidencia: C17 aparecía 27 veces en SOL y 67 en LUNA.
- §5, **nivel del inventario** (propuesta 3). Una funcionalidad es lo que un usuario o un sistema externo reconocería como una función del producto (una pantalla, un proceso de negocio o una integración), y suele tener varios requisitos. No se crea una por requisito. U0 habla ahora de funcionalidades observables. El ejemplo de §11 tiene una funcionalidad cubierta por dos requisitos y citada por un `RNF`. Evidencia: 420 funcionalidades para 456 requisitos en SOL y 272 para 284 en LUNA; C20 provocó todas las reparaciones (4 en SOL y 12 en LUNA).
- §6, **regla R-h** (propuesta 5). `feature`, `states`, `trigger` y `condition` no empiezan por su keyword (si, cuando, mientras, donde, en cuanto · if, when, while, where, as soon as, as long as). Evidencia: frases como «SI si la pantalla tiene menos de…».
- §11, "No hagas esto" (propuesta 6) añade el modo verbal tras SI («SI el usuario esté autorizado»), junto al caso de R-h. No hay regla nueva: es R-f.
- §8, **N4** (propuesta 7). Como máximo una pregunta `missing_error_handling` por funcionalidad del inventario, solo si incluye una operación que puede fallar y ninguno de sus requisitos tiene un criterio `[ERROR]`. Ante la duda, no se abre. Evidencia: en LUNA estas preguntas subieron de 66 a 77 con la v1.3.
- §2 y §10, **una sola escritura** (propuesta 8). El catálogo se escribe con una sola llamada a Write, después de autoverificar, y no se relee ni se reescribe. Evidencia, medida en los registros de la ejecución del 02-10:
  - el lote 16 de LUNA llamó a Write en sus 10 turnos, sin ningún error;
  - 8 de los 27 intentos de LUNA escribieron el catálogo más de una vez, y el 36 % de lo que escribió acabó sobrescrito;
  - en SOL fueron 2 de 26 intentos y un 6 %.
- Recortes para que el prompt no crezca: tabla de tipos sin columna de ejemplos, §9 más breve, el orden de los slots solo en §6, ejemplo en inglés más corto, menos frases renderizadas en §11 y un fragmento menos en el ejemplo.

**Anexo**
- C20: si la entrada del inventario era un detalle o una variante de otra funcionalidad, se une a ella. Antes solo se contemplaba quitarla.
- El catálogo corregido se escribe con una sola llamada a Write.

**Sin cambios en el validador por el prompt.** Ninguna regla nueva necesita una comprobación automática. Quedan para Claude Code, según la propuesta:
- normalizador: quitar la keyword repetida al principio de `condition`, `trigger`, `states` y `feature` (`FIX`), como red de seguridad de R-h;
- validador: quitar el descarte entero de un fragmento citado (`FIX`) en lugar de solo avisar con C17;
- validador: decidir si se comprueban los 200 caracteres de cada criterio;
- runner: reintentar una vez un lote perdido por inactividad de la pasarela;
- pasos 32 y 33.

La plataforma debe cargar `prompt_requisitos_v1.4.md` y `anexo_reparacion_v1.4.md` y registrar la versión `req-1.4`.

## v1.3 (2026-10-02)

Hito 6 (`docs/hito-6-catalogo-v1.3.md`), tras la primera ejecución grande del hito 5: 1.133 y 862 requisitos para un documento técnico, más de la mitad de secciones que no eran especificación, 13 y 17 de 22 lotes reparados, y entre el 46 % y el 72 % de la salida gastada en intentos fallidos. El prompt y el anexo los redactó el autor en el proyecto de Claude; esta entrada recoge también el esquema, el validador y el fragmentador.

**Carpeta**
- Nuevos `prompt_requisitos_v1.3.md`, `anexo_reparacion_v1.3.md`, `requirements_v1.3.schema.json` y, en cada ejemplo, `catalogo_ejemplo_v1.3.json` y `catalogo_ejemplo_v1.3.enriquecido.json`.
- Los ficheros v1.2 (`prompt_requisitos_v1.2.md`, `anexo_reparacion_v1.2.md`, `requirements.schema.json`, `catalogo_ejemplo.json` y su enriquecido) se mantienen mientras los use el código C#; se quitan en el paso 31.
- `validar_catalogo.py` y `test_validador.py` pasan a ser la v1.3; la v1.2 está en el historial de git (commit `54310cd`).

**Prompt**
- Independencia: no menciona al otro modelo ni al árbitro.
- Cobertura antes que detalle (U0): toda funcionalidad de la fuente queda en al menos un requisito.
- Inventario de funcionalidades (`capabilities`) antes de los requisitos; cada `RF`/`RI` indica cuáles cubre (`capability_ids`).
- Tipos `RF`, `RI`, `RNF`, `RA`, `RT`, con id por tipo (`RF-001`…). Sustituyen a `kind` y `out_of_subset`. El árbol D1–D5 se mantiene, ligado al tipo; `RNF` también en D2–D5.
- Un requisito por capacidad (U1–U6), con sus detalles como criterios de aceptación etiquetados (`[OPERATION]`, `[DATA]`, `[RULE]`, `[LIMIT]`, `[MESSAGE]`, `[ERROR]`). Los errores que describe la fuente son criterios `[ERROR]`, no requisitos `unwanted_behaviour`. Sin verbos paraguas con cuatro operaciones o menos (U7).
- Descartes nuevos `design_detail` y `project_note`, y descarte de una sección entera (`discarded_sections`).
- Índice del documento (`{{INDICE}}`).
- N4: como máximo una pregunta `missing_error_handling` por proceso o pantalla.
- Salida más corta: sin `notation_rationale`, `escalation_reason` ni `verdict.counts`.
- §9 indica qué rechaza un requisito, qué es aviso y qué repara el lote.

**Esquema 1.3**
- `capabilities`, `discarded_sections`, `type`, `capability_ids`, `acceptance_criteria` y `verdict.self_check.all_capabilities_covered`.
- Ids y referencias como cadenas libres: los renumera el normalizador. Los criterios sin etiqueta y los `RF`/`RI` sin `capability_ids` no son errores de esquema (son los avisos C19 y C21).

**Validador**
- Normalizador antes de validar, con aviso `FIX` por corrección: renumeración por tipo, `Q-` y `CAP-`; vínculos completados en los dos sentidos; referencias a ids inexistentes quitadas; puntuación final de los slots; `slots.system` (antes C3); etiquetas de criterios; citas comparadas sin Markdown ni mayúsculas, con el fragmento corregido si la cita está literal en uno solo; citas repetidas.
- Tres niveles. Error del lote (JSON, estructura raíz, C10 con más del 10 % de fragmentos sin tratar, `status` `failed`): reparación. Error de un elemento (esquema, C2, C5, C6, C7, C12, C15, N2, N3): se rechaza ese requisito o pregunta. Avisos: C13, C14, C16, C17, C19, C20, C21, C22, C9, C11, `FIX`, `VERDICT`, `SELF_CHECK`.
- Aceptación parcial: el lote necesita reparación si se rechaza más del 20 % de los requisitos o hay algún C20 (funcionalidad sin `RF`/`RI` aceptado). `repair_reasons` son las líneas del anexo.
- C5 y N2 se aplican también a los criterios. C12 comprueba el tipo frente a la rama.
- Desaparecen C3 (lo corrige el normalizador), C4, C18 y las comprobaciones de recuentos del veredicto.
- Salida: el catálogo aceptado, normalizado y renumerado, y la correspondencia entre los ids del modelo y los finales. Los mensajes de cada elemento usan el id que escribió el modelo, que es el que ve en la reparación.
- Métricas nuevas: requisitos escritos y rechazados, recuento por tipo, funcionalidades y su cobertura, criterios por requisito y por etiqueta, correcciones.
- `test_validador.py`: 65 casos (2 bases, idempotencia de la normalización y 61 mutaciones: 27 rechazos, 5 errores del lote, 16 avisos, 9 correcciones, 1 aceptación y 3 reparaciones).
- Paso 31: una etiqueta de criterio exige corchetes o dos puntos ("[DATA]", "DATA:"). Antes, un criterio sin etiqueta que empezara por "Error", "Data"… se tomaba por etiquetado.
- Paso 31: se retiran de la carpeta los ficheros v1.2 (prompt, anexo, `requirements.schema.json` y `catalogo_ejemplo.json` con su enriquecido); siguen en el historial de git.

**Fragmentador**
- Índice del documento (`document_index`, `index_block`, `--bloque-indice`): una línea por sección, con su etiqueta y, si empieza con un título, el título. Los fragmentos no cambian.
- `test_fragmentar.py`: 16 casos.

## v1.2 (2026-09-29)

Revisión de la v1.1 antes de la primera medición con PURE (documentos en inglés). Solo entra lo que falsearía o bloquearía esa medición; lo que conviene decidir con datos queda pendiente (ver la justificación v1.2 en el proyecto de Claude).

**Carpeta**
- Solo queda la versión vigente: `prompt_requisitos_v1.2.md` y `anexo_reparacion_v1.2.md`. `prompt_requisitos_v1.md` y los ficheros v1.1 siguen en el historial de git (commits `466145f` y `2c4d128`). Sustituye a la nota de la v1 que conservaba `prompt_requisitos_v1.md` para la ablación: las variantes de la ablación se construyen a partir de la versión vigente.
- Nuevo `ejemplo_en/` (LibraryLoan, en inglés) y `test_fragmentar.py`.

**Prompt**
- Obligatoriedad (§7): la regla de capacidad cubre también el inglés ("can", "may", "is able to" con un actor como sujeto → `SHALL` implícito) y la forma impersonal ("se podrá"). "The system may…" es `MAY` explícito; "el sistema podrá…" es capacidad. "will" y el futuro simple describen comportamiento (`SHALL` implícito) salvo con marca de versión futura. Tabla de señales ampliada y alineada con el validador.
- Listas (§1): cada viñeta se lee con su frase introductoria; se citan los dos fragmentos y la señal modal de la introducción vale para cada elemento.
- Citas (§1, C6): al menos 3 palabras o el fragmento entero.
- N2: las cifras se copian como están en la fuente, sin convertir unidades ni pasar palabras a dígitos.
- N4: la pregunta `missing_error_handling` es genérica (una por requisito como máximo) y no propone fallos concretos. Se corrige la pregunta del ejemplo ReservaSalas, que inventaba uno.
- N7 y `domain_terms`: una palabra de C5 que forma parte del nombre de una entidad del dominio ("Flexible Loan plan", "seguro de cancelación") se declara y deja de contar como lenguaje vago.
- R-f: en `en`, las condiciones van en presente simple.
- §9: cada comprobación indica si es automática, solo aviso o solo del modelo. Se añade C15 (ya existía en el validador y faltaba en el prompt) y se retira la nota sobre C1.
- §11.4: ejemplo en inglés.

**Esquema 1.2**
- Nuevo `domain_terms` (obligatorio, puede ir vacío) y `verdict.self_check.figures_from_source`.

**Fragmentador**
- Un título numerado ("2. Functional requirements", "3.2 Gestión de reservas") es un fragmento propio. Antes se unía a la frase siguiente.
- Nuevo campo `source_ref` con el id de requisito de la fuente ("FR-12", "R7"), para cruzar con catálogos de referencia. El texto del fragmento no cambia.

**Validador**
- Errores nuevos: N2 (cifra de un slot que no está en los fragmentos citados), C6 (cita demasiado corta), C13 (evidencia que no es una señal de su obligatoriedad o que no aparece como palabra completa), C17 (fragmento descartado entero y citado), C5 sobre `domain_terms` mal declarados.
- Avisos nuevos: C4 (posible segundo verbo coordinado, solo `es`) y C18 (orden de claves).
- Métricas nuevas: `fidelidad_cifras` y `terminos_dominio`. `autoverificacion_coincide` incluye `figures_from_source`.
- `test_validador.py`: 46 casos sobre los dos ejemplos (2 bases, 42 errores, 2 avisos). `test_fragmentar.py`: 11 casos.

## v1.1 (2026-09-29)

Aplica las decisiones D-21 a D-24 de la bitácora, tras la revisión de la v1 frente a los informes de investigación del proyecto. La justificación completa está en el proyecto de Claude (`Justificacion_prompt_requisitos_v1.1.md`).

**Contrato con el runner (D-23)**
- El modelo escribe `output/catalogo.json` con Write, en JSON compacto, y termina con `VEREDICTO:`. En la v1 se pedía "solo JSON" como respuesta y se prohibía escribir ficheros, lo que no encajaba con el runner.
- El modelo ya no escribe `text`: lo genera el validador a partir de los slots (`--enriquecido`). Desaparece la comprobación C1.
- Anexo de reparación para los reintentos: se corrigen solo los errores señalados.

**Entrada fragmentada y lotes (D-24)**
- La documentación llega en fragmentos con id estable (`DOC-01#F001`, `DUDAS#D-001`), generados por `fragmentar.py`.
- Las trazas pasan de `source` + `locator` + `quote` a `fragment` + `quote`. El validador comprueba que la cita está en ese fragmento.
- C10 se puede verificar: todo fragmento del documento acaba en un requisito, una pregunta o un descarte. Nuevo motivo de descarte `heading`.
- Numeración local por lote; la plataforma renumera al unir.

**Árbol de decisión (D-21, D-22)**
- MAY significa "opcional". La funcionalidad anunciada para el futuro genera una pregunta `scope`, no un requisito.
- D2: las actividades de personas en las que el sistema no interviene se descartan (`human_activity`); si el sistema las media, son funcionales. "Proceso" queda para la normativa.
- D4: varios estados caben en EARS (`states` es una lista, según el ruleset oficial). Se escala a MASTeR solo con una condición lógica sobre datos (`data_condition`, LogicMASTeR) o con dos o más disparadores (`compound_trigger`, EventMASTeR).
- Nuevo campo `decision_branch` (D1 a D5). La degradación a EARS solo se admite en D1 y D4.

**Redacción**
- Nuevas reglas de slots: prohibiciones en positivo (R-e), subjuntivo tras CUANDO/MIENTRAS/DONDE/EN CUANTO e indicativo tras SI (R-f), slots autocontenidos (R-d).
- Términos vagos agrupados por las categorías de lenguaje ambiguo de ISO/IEC/IEEE 29148; se añade "seguro", que N3 citaba y C5 no incluía.
- N3: la característica es la nominalización literal del término vago ("la rapidez"); el modelo no elige la métrica.
- `subject_matter` admite `desde`, `en`, `para`, `durante` (el ejemplo oficial usaba "desde…" y la regla lo prohibía).
- Orden de claves: cita, obligatoriedad y justificación antes que la decisión.
- Protección contra instrucciones dentro de los documentos.

**Validador**
- Comprobaciones nuevas: C12 (coherencia del árbol), C13 (evidencia modal en el fragmento), C14 (formato de slots), C15 (negaciones), C16 (origen), C11 bidireccional, N3 con pregunta `missing_value`, estado del veredicto.
- La autoverificación del modelo se compara con la del validador (aviso y métrica `autoverificacion_coincide`).
- Métricas separadas: conformidad sintáctica, calidad de redacción, coherencia de decisión, trazabilidad, citas literales, cobertura de fragmentos, ramas usadas.
- `test_validador.py`: 30 mutaciones con su código esperado (incluye las 16 que la v1 dejaba pasar).

## v1 (2026-09-28)

Versión inicial. Se conserva `prompt_requisitos_v1.md` para la ablación; su esquema, validador y ejemplo están en el historial de git.
