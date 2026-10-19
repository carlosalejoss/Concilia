# Evolución del prompt de generación de requisitos

Historial de las versiones del prompt que genera el catálogo de requisitos (EARS/MASTeR), con lo que hacía cada una, los resultados que obtuvo y lo que había que mejorar. Es material para la memoria del TFG: cada versión responde a los problemas medidos en la anterior.

El detalle técnico de cada cambio está en `prompts/generacion_requisitos/CHANGELOG.md`; aquí se explica el porqué y el efecto.

## Resumen

| Versión | Fecha | Idea principal | Medida con modelos | Resultado clave |
|---|---|---|---|---|
| v1 | 28-09-2026 | Primer prompt EARS/MASTeR, respuesta en JSON | No | El validador dejaba pasar 16 tipos de error |
| v1.1 | 29-09-2026 | Contrato con la plataforma: fragmentos, `Write`, veredicto, árbol de decisión | No | Base para ejecutar con modelos |
| v1.2 | 29-09-2026 | Reglas más precisas (obligatoriedad, cifras, citas) y ejemplo en inglés | Sí | Perfecto en un documento pequeño; inservible en uno grande |
| v1.3 | 02-10-2026 | Independencia, cobertura, un requisito por capacidad, tipos, reparación parcial | Sí | En el documento grande: un 60–67 % menos de requisitos, la mitad de tokens y casi sin salida perdida; quedan detalles técnicos y lotes perdidos de LUNA |
| v1.4 | 05-10-2026 | Observabilidad, nivel del inventario, sin descartes enteros de lo citado, R-h, N4 por funcionalidad, una sola escritura | Sí | 255 y 214 requisitos y 118 y 86 preguntas: aún muy lejos de lo revisable; el límite es el proceso por lotes, no el prompt (se pasa a la v2, de arriba abajo) |
| v2 | 05-10-2026 | De arriba abajo: inventario global de funcionalidades (fase 1) y requisitos por funcionalidad, cada una en un solo lote (fase 2) | Pendiente | Pendiente |
| v0.10 | 19-10-2026 | El árbitro parte del emparejamiento por significado de la plataforma (`input/emparejamiento.txt`, embeddings en local); puede apartarse si la fuente lo justifica, y la plataforma marca como discrepancia en el catálogo y el backlog dónde lo hizo | Sí (proyecto grande de ejemplo, catálogo, 19-10-2026) | Con las mismas propuestas, 3 arbitrajes de SOL válidos de 3: unión de 202 a 222 requisitos (media 212,3), frente a 209–291 (media 240) con la v0.9. Las desviaciones no bajan (7,7 de media, frente a las 7,0 que habría marcado la v0.9) |
| v0.9 | 15-10-2026 | Mockup como aplicación que funciona: inicio de sesión con usuarios de prueba, una aplicación por rol, menús solo con las secciones principales y navegación por acciones, comprobada por la plataforma | Sí (documento C, 15-10-2026) | Documento C: 40 min, 25 pantallas, las 46 historias, 866 KB, una sola corrección; 3,47 M tokens de entrada y 548.000 de salida. Proyecto grande de ejemplo: 38 pantallas y las 109 historias (1,54 MB), con una pantalla perdida por un 504 de la pasarela y recuperada al reanudar. Al autor le convencen los dos |
| v0.8 | 15-10-2026 | Mockup: la plataforma junta y revisa el prototipo (ids, globales de JavaScript y navegación) y solo corrige con un modelo las partes con problemas; se puede reanudar un mockup fallido | Pendiente | Pendiente |
| v0.7 | 14-10-2026 | El árbitro consulta la documentación por tramos y lee las propuestas con un elemento por línea; un fragmento no es un número de línea (`REF-FRAG` dice qué fragmento hay en esa línea); sin la salida de un solo modelo como resultado | Sí (documento C, 14-10-2026; proyecto grande de ejemplo, 15-10-2026) | El árbitro cabe y lee las propuestas en pocas llamadas, pero termina compactando después de escribir (la plataforma lo corta y acepta la salida). Con el proyecto de ejemplo: 299 y 192 requisitos, unión de 209 en 12 min, con dos reparaciones por citas mal copiadas (la marca `sigue` y un número de línea) |
| v0.6 | 14-10-2026 | El mockup es una sola aplicación construida por pasos: diseño (mapa de pantallas y armazón), una ejecución por pantalla y una revisión del conjunto; pensado para `SS - GPT-6-SOL` | Pendiente | Pendiente |
| v0.5 | 14-10-2026 | Referencia de estilo del mockup: un `.zip` con un mockup del cliente; el modelo lee sus `.html` y `.css` y la plataforma incrusta sus imágenes y fuentes; el resto de prompts no cambia | Pendiente | Pendiente |
| v0.4 | 13-10-2026 | Mockup por partes: una base y un fichero por épica, que la plataforma junta; el resto de prompts no cambia | Sí (documento B, 53 historias, 13-10-2026) | El mockup se genera (166 KB, las 53 historias) en 23 min, pero gasta 1,49 M de entrada y 204.000 de salida, el 43 % del límite de 5 h de la suscripción, y al autor no le convence el resultado |
| v0.3 | 09-10-2026 | El árbitro ve los ids de cada propuesta con su número delante (`1:P-003`, `2:RF-001`); la plataforma los quita antes de validar | Sí (documento B, flujo completo, 13-10-2026) | Los tres árbitros aciertan a la primera, sin reparación; el catálogo y el backlog del árbitro compactan el contexto una vez; el mockup de 53 historias no cabe en 64.000 tokens de salida |
| v0.2 | 09-10-2026 | La v0.1 con los argumentos exactos de cada llamada a `Read` (`offset` y `limit`) en lugar de rangos de líneas | Sí (documento B, una ronda, 09-10-2026) | A y B leen exactamente los tramos y LUNA ya no repara; el árbitro vuelve a confundir los ids de las propuestas (tercera vez de tres) |
| v0.1 | 09-10-2026 | Lectura por partes (team leader): la documentación va a `input/documentacion.txt` y el modelo la lee con `Read` en los tramos que da la plataforma, que comprueba que no queda ninguna línea sin leer | Sí (documento B, una ronda, 09-10-2026) | Funciona y lee todo, pero con un documento que cabe entero cuesta un 50 % más de tokens y de tiempo que la v0 y añade dos reparaciones: LUNA cuenta las líneas desde 0 y se deja la última, y el árbitro confundió los ids de las propuestas |
| v0 | 08-10-2026 | Prompt muy básico para tener el flujo completo (iteración v0): documento entero sin lotes, respuestas del cliente como entrada, frase EARS/MASTeR escrita por el modelo y árbitro que une A y B | Sí (ReservaSalas, 09-10-2026; documento B, 09-10-2026) | Flujo completo en unos 5 minutos de modelo, todo válido a la primera; 25 requisitos (13 en la v1.2), porque las 11 respuestas eran suposiciones de la IA aceptadas sin cambios y añadieron funcionalidad que el documento no tiene |

## Cómo se mide

Cada versión se mide con el validador determinista de la plataforma (`validar_catalogo.py` y su port en C#), que no usa ningún modelo:

- **Calidad:** proporción de requisitos sin errores de sintaxis, redacción, decisión, trazabilidad y fidelidad de cifras; citas literales; cobertura de fragmentos.
- **Volumen:** requisitos, preguntas abiertas, descartes, y desde la v1.3 requisitos por tipo, funcionalidades del inventario y criterios por etiqueta.
- **Coste:** tokens de entrada y salida, lotes reparados, salida gastada en intentos fallidos y duración.
- **Comparación entre modelos:** cuántos requisitos de SOL (A) y LUNA (B) se parecen. Hasta que exista el árbitro se mide con una aproximación: parejas de requisitos que comparten al menos el 60 % de sus palabras significativas (emparejamiento voraz). Subestima el parecido real.
- **Origen de los requisitos** en el documento grande: la sección principal del fragmento que cita cada requisito, agrupada en funcional (secciones 2–5), diseño técnico (6–12) y notas del proyecto (13–18).
- **Requisitos con detalles de implementación:** su texto contiene identificadores de código, rutas de la API, métodos HTTP, colores, medidas de interfaz o nombres de tecnologías. Es una heurística: sirve para comparar versiones, no como cifra exacta.

Documentos de prueba:

- **ReservaSalas** (`prompts/generacion_requisitos/ejemplo/`): ficticio, 14 fragmentos, un lote. Se puede citar sin restricciones.
- **Documento técnico y funcional interno:** un documento real de unos 95 KB, 608 fragmentos y 22 lotes, que describe un sistema ya construido. **Solo cifras agregadas y anonimizadas**: no se cita su contenido.
- **PURE:** documentos públicos de requisitos. Pendiente de elegir 1 o 2 (paso 34).

**Precauciones.** Los modelos no dan siempre el mismo resultado: una sola ejecución es una muestra, no una medida. Para la memoria conviene repetir cada configuración 2 o 3 veces y dar medias o rangos. La v1.3 cambia varias cosas a la vez: su efecto global se puede medir, pero atribuirlo a cada cambio exigiría una ablación (por ejemplo, la v1.3 sin inventario).

---

## v1 (28-09-2026)

### Qué hacía

- **Rol y alcance:** el modelo actúa como experto en EARS y MASTeR y convierte la documentación en requisitos.
- **Salida:** el modelo respondía **solo con JSON** en su mensaje, sin escribir ficheros, y redactaba él mismo el texto de cada requisito (`text`).
- **Trazabilidad:** cada requisito citaba su origen como documento + localizador + cita.
- **Notación:** EARS por defecto y MASTeR para no funcionales, sin un árbol de decisión explícito.

### Resultados

No llegó a ejecutarse con modelos. Al revisarlo frente a los informes de investigación del proyecto y probar el validador con catálogos manipulados, aparecieron **16 tipos de error que el validador no detectaba**: por ejemplo, propiedades con una justificación incoherente, slots con puntuación, obligatoriedad sin evidencia o citas a un localizador falso.

### Qué había que mejorar

- **Contrato con la plataforma.** "Solo JSON" no encajaba con el runner, que ejecuta Claude Code y necesita un fichero de salida y un veredicto final.
- **Texto redactado por el modelo.** El texto que escribía el modelo podía no corresponder a sus propios slots, y no había forma de comprobarlo.
- **Trazabilidad débil.** Un localizador como "sección 2" no se podía verificar automáticamente.
- **Elección de notación sin reglas.** Dos ejecuciones podían elegir notaciones distintas para el mismo texto sin que hubiera un criterio que comprobar.

## v1.1 (29-09-2026)

### Qué hacía

- **Contrato con el runner (D-23):** el modelo escribe `output/catalogo.json` con `Write` y termina con `VEREDICTO:`. El texto del requisito **lo genera la plataforma** a partir de los slots.
- **Entrada fragmentada (D-24):** la documentación llega en fragmentos con id estable (`DOC-01#F001`). Cada cita es `fragmento + trozo literal`, y el validador comprueba que el trozo está en ese fragmento. La plataforma divide los documentos largos en lotes.
- **Árbol de decisión D1–D5:** cuándo se escala de EARS a MASTeR, registrado en cada requisito.
- **Reglas de redacción de los slots (R-a a R-f)** y términos vagos agrupados según ISO/IEC/IEEE 29148.
- **Validador ampliado:** coherencia del árbol, evidencia de obligatoriedad, formato de slots, negaciones, origen, vínculos en los dos sentidos y métricas por categoría.
- **Anexo de reparación** para reintentar con la lista de errores.

### Resultados

Sin ejecución con modelos todavía. El validador detectaba 30 mutaciones con su código esperado, incluidas las 16 que la v1 dejaba pasar.

### Qué había que mejorar

- **Medir con documentos públicos en inglés (PURE)** exigía reglas de obligatoriedad que cubrieran el inglés ("can", "may", "will").
- **Cifras y unidades:** nada impedía que el modelo convirtiera "4 horas" en "240 minutos" o inventara un umbral.
- **Citas triviales** ("la reserva") pasaban por literales.
- **Nombres del dominio con palabras "vagas"** ("seguro de cancelación", "Flexible Loan plan") daban falsos positivos de término vago.

## v1.2 (29-09-2026)

### Qué hacía

- **Obligatoriedad** en español e inglés: capacidad ("puede", "can") como `SHALL` implícito, "the system may" como `MAY`, el futuro simple como comportamiento.
- **Cifras copiadas tal cual** (N2) y comprobadas por el validador.
- **Citas** de al menos 3 palabras.
- **Términos de dominio** declarados (`domain_terms`).
- **Una pregunta "¿qué pasa si falla?" por requisito** cuya operación pueda fallar (N4).
- **Ejemplo en inglés** (LibraryLoan).
- **Granularidad muy fina** para que "ambos modelos corten igual": cada acción, cada valor por defecto y cada cifra es un requisito.

### Resultados

**ReservaSalas, pasarela real (30-09-2026), un lote:**

| | SOL (A) | LUNA (B) |
|---|---|---|
| Catálogo | válido, 13 requisitos, 3 preguntas | válido, 13 requisitos, 3 preguntas |
| Métricas de calidad | todas al 100 % | todas al 100 % |
| Reparaciones | 0 | 0 |
| Tokens entrada / salida | 34.145 / 5.591 | 34.085 / 8.115 |

Los dos modelos coincidieron en notación, patrón y obligatoriedad de los 13 requisitos.

**Documento técnico interno, pasarela real (01-10-2026), 608 fragmentos, 22 lotes, 5 lotes a la vez por modelo:**

| | SOL (A) | LUNA (B) |
|---|---|---|
| Lotes en el catálogo | 22 de 22 | 20 de 22 (2 fallidos tras dos reparaciones) |
| Requisitos · preguntas · descartes | 1.133 · 271 · 254 | 862 · 154 · 287 |
| Requisitos de secciones que no son especificación | 53 % | 54 % |
| Requisitos con detalles de implementación (heurística) | 33 % | 30 % |
| Preguntas "¿qué pasa si falla?" | 108 | 66 |
| Lotes reparados | 13 de 22 | 17 de 22 |
| Salida gastada en intentos que fallaron | 46 % | 72 % |
| Tokens entrada / salida | 2,36 M / 0,79 M | 2,86 M / 1,28 M |
| Duración | 14 min | 22 min |
| Validador del catálogo unido | válido, 33 avisos | válido, 48 avisos |

- **Origen de los requisitos:**
  - secciones funcionales: 47 % en SOL y 45 % en LUNA;
  - diseño técnico (datos, arquitectura, API, frontend, despliegue): 38 % y 39 %;
  - notas del proyecto (historia, deuda, riesgos, recetas): 15 % en los dos.
- **Coincidencia entre A y B:** con una similitud de palabras del 60 % solo se emparejaron 407 requisitos.
- **Causa de las reparaciones de LUNA:** sobre todo citas no literales (C6, en 24 de sus 27 intentos fallidos).

Antes, en la primera ejecución con este documento, apareció además un problema de la plataforma, no del prompt. LUNA gastaba los 32.000 tokens de salida por defecto razonando y su `Write` llegaba cortado. Se resolvió subiendo el tope a 64.000 y cortando la ejecución en cuanto el `Write` llega incompleto.

### Qué había que mejorar

1. **Qué es un requisito.** El prompt no podía decir "esto no es un requisito" para los detalles de implementación ni para lo que habla del proyecto, así que más de la mitad de los requisitos sobraban.
2. **Granularidad.** Cada acción y cada detalle eran un requisito: cientos de requisitos difíciles de revisar.
3. **Rigidez del validador.** Trataba como error, y por tanto como reparación del lote entero, cosas que la plataforma puede corregir sola: recuentos, numeración, vínculos, comillas en las citas. Eso disparaba el coste.
4. **Demasiadas preguntas:** una por operación que pudiera fallar.
5. **Coste fijo:** un prompt de 48 KB y campos que nadie usaba se pagaban en cada lote y en cada reparación.
6. **Independencia.** El prompt empujaba a los dos modelos a coincidir ("garantizan que ambos modelos corten igual"), justo lo contrario de lo que necesita la verificación cruzada.
7. **Falta de tipos:** no distinguía requisitos de arquitectura, técnicos, funcionales, de integración y no funcionales.

## v1.3 (02-10-2026)

### Qué hace

- **Independencia.** No menciona al otro modelo ni al árbitro: cada modelo construye su catálogo como si fuera el único.
- **Cobertura antes que detalle.** El modelo escribe primero un **inventario de funcionalidades** (`capabilities`) y cada requisito funcional o de integración dice cuáles cubre. Si una funcionalidad del inventario se queda sin requisito (C20), el lote se repara.
- **Un requisito por capacidad.** Las acciones del mismo actor sobre el mismo objeto se agrupan, sin verbos paraguas con cuatro operaciones o menos. Los detalles van como **criterios de aceptación etiquetados** (`[OPERATION]`, `[DATA]`, `[RULE]`, `[LIMIT]`, `[MESSAGE]`, `[ERROR]`). Los errores que describe la fuente son criterios `[ERROR]`, no requisitos propios.
- **Tipos con id propio:** `RF`, `RI`, `RNF`, `RA`, `RT` (`RF-001`, `RNF-001`…).
- **Alcance:** descartes nuevos `design_detail` y `project_note`, descarte de secciones enteras y un **índice del documento** en cada lote para saber en qué parte está.
- **Preguntas limitadas:** como máximo una "¿qué pasa si falla?" por proceso o pantalla.
- **Salida y prompt más cortos:** sin campos que nadie usaba; el prompt pasa de 48 KB a 32 KB.
- **Validador por niveles:**
  - un normalizador corrige lo mecánico (ids, vínculos, puntuación, nombre del sistema, citas);
  - un requisito con errores se **rechaza** sin tirar el lote;
  - el lote solo se repara si está roto, si se rechaza más del 20 % o si hay C20;
  - como máximo una reparación.

En los ejemplos de referencia, ReservaSalas pasa de 13 a 9 requisitos y LibraryLoan de 8 a 6, con la misma cobertura de fragmentos.

### Resultados

**Documento técnico interno, pasarela real (02-10-2026), una ejecución.** El mismo documento, los mismos 22 lotes y 5 lotes a la vez por modelo que en la v1.2:

| | v1.2 SOL | v1.3 SOL | v1.2 LUNA | v1.3 LUNA |
|---|---|---|---|---|
| Lotes en el catálogo | 22 de 22 | 22 de 22 | 20 de 22 | **18 de 22** |
| Requisitos | 1.133 | **456** (−60 %) | 862 | **284** (−67 %)¹ |
| Por tipo | — | RF 414 · RNF 16 · RI 12 · RT 10 · RA 4 | — | RF 268 · RI 8 · RNF 5 · RA 3 |
| Funcionalidades del inventario (cubiertas) | — | 420 (419) | — | 272 (265) |
| Criterios por requisito | — | 1,54 | — | 1,75 |
| Criterios por etiqueta | — | DATA 289 · RULE 206 · ERROR 62 · OPERATION 61 · LIMIT 56 · MESSAGE 29 | — | DATA 222 · RULE 145 · OPERATION 51 · LIMIT 40 · ERROR 24 · MESSAGE 14 |
| Origen: funcional / diseño técnico / proyecto | 47 / 38 / 15 % | 42 / 38 / 20 % | 45 / 39 / 15 % | 54 / 17 / 29 % |
| Requisitos con detalles de implementación | 33 % | **9 %** | 30 % | **2 %** |
| Descartes (y secciones enteras) | 254 | 432 (+19 secciones) | 287 | 343 (+18 secciones) |
| Preguntas abiertas | 271 | 202 | 154 | 163 |
| De ellas, "¿qué pasa si falla?" | 108 | 37 | 66 | 77 |
| Lotes reparados | 13 de 22 | **4 de 22** | 17 de 22 | **8 de 22** |
| Salida gastada en intentos que fallaron | 46 % | **3 %** | 72 % | **10 %** |
| Tokens entrada / salida | 2,36 M / 0,79 M | **1,06 M / 0,47 M** | 2,86 M / 1,28 M | **1,78 M / 0,90 M** |
| Duración | 14 min | 10 min | 22 min | 33 min² |
| Casi duplicados dentro del modelo (parejas) | 29 | 29 | 19 | 16 |
| Parejas A–B (similitud de palabras ≥ 60 %) | 407 | 133 | — | — |

¹ Sobre 18 lotes. ² Tres lotes de LUNA esperaron 15 minutos sin respuesta antes de darse por perdidos.

- **Lo que ha funcionado:**
  - **Volumen:** los requisitos bajan a menos de la mitad. Los detalles de implementación casi desaparecen del texto: se descartan como `design_detail` (195 y 188 fragmentos) o pasan a criterios.
  - **Coste:** los tokens de salida bajan un 41 % en SOL y un 30 % en LUNA, y los de entrada un 55 % y un 38 %.
  - **Reparaciones:** casi no se pierde salida. Todas las reparaciones pedidas lo fueron por C20 (una funcionalidad del inventario sin requisito), y el catálogo del intento que las pidió se conservó.
  - **Nuevas piezas en uso:** los modelos usan las seis etiquetas de criterio, el inventario y los tipos de requisito.
- **Lotes perdidos de LUNA:** pierde 4 lotes.
  - **Tres por inactividad:** los lotes 10, 11 y 12 murieron a la vez, a los 15 minutos exactos sin ninguna salida, mientras otros intentos de LUNA sí terminaron en 17 y 23 minutos. Apunta a un atasco temporal de la pasarela, no al prompt.
  - **Uno por turnos:** el lote 16 agotó los 10 turnos, con 90.000 tokens de salida.
- **Coincidencia entre A y B:** baja de 407 a 133 parejas por similitud de palabras. Era de esperar: la v1.3 deja de forzar que los dos modelos corten igual, y además agrupa más. El árbitro tendrá que emparejar por significado y de varios a varios.
- **Autoverificación:** los modelos declaran como correctos aspectos que el validador encuentra mal ("autoverificación coincide: no").

### Qué hay que mejorar

1. **Requisitos de infraestructura tratados como funcionales.** SOL saca todavía el 38 % de sus requisitos de las secciones de diseño técnico: comportamientos internos que el usuario no ve (cabeceras HTTP, CORS, comprobaciones de salud, OpenAPI, migraciones, registros) escritos como `RF`. La regla de `design_detail` se aplica al detalle del texto, pero no a "el sistema hace X" cuando X es interno. Opciones:
   - en el prompt, una regla para lo que no ve ningún usuario ni otro sistema externo: `design_detail`, o `RT`/`RA` si es una restricción impuesta;
   - en la plataforma, la selección de secciones del paso 33.
2. **Secciones de notas del proyecto.** El 20–29 % de los requisitos sale de las secciones 13–18 (historia, especificación frente a implementación, deuda, recetas, anexos). Parte puede ser legítimo; hay que revisarlo a mano antes de cambiar el prompt.
3. **El inventario va casi uno a uno con los requisitos** (420 funcionalidades para 456 requisitos en SOL): no agrupa al nivel de funcionalidad. Además, C20 provoca todas las reparaciones. Conviene:
   - precisar en el prompt el nivel del inventario (lo que un usuario reconocería como una función del sistema);
   - medir si C20 aporta lo que cuesta.
4. **Duplicados entre lotes:** por ejemplo, la exportación a CSV o la gestión de usuarios aparecen en varios lotes (paso 32).
5. **C17 muy frecuente** (27 en SOL y 67 en LUNA): el modelo descarta un fragmento entero (o su sección) y lo cita en un requisito. Hay que aclarar en el prompt cuándo se descarta solo una parte, o tratarlo en el normalizador.
6. **Errores de redacción en las condiciones** que ninguna regla automática detecta:
   - condiciones que empiezan por "si" y dan "SI si la pantalla…" (el normalizador podría quitar esa palabra inicial);
   - modo verbal tras SI ("SI el usuario esté autorizado").
7. **Preguntas "¿qué pasa si falla?":** en LUNA suben de 66 a 77; la regla N4 no se aplica igual en los dos modelos.
8. **Plataforma, lotes perdidos por inactividad o por turnos:** un lote que se queda sin respuesta de la pasarela se pierde sin reintento. Conviene reintentar una vez la inactividad (no es un fallo del modelo) y revisar si 10 turnos bastan.
9. **Medición:** es una sola ejecución con un documento interno.
   - Repetir la v1.3 y medir ReservaSalas y PURE (paso 34).
   - Los requisitos rechazados por lote no se ven desde la API (están en el `informe.json` de cada ejecución); la web del paso 33 debe mostrarlos para medirlos.
10. **Comprobaciones pendientes:** los 200 caracteres de cada criterio no los comprueba ningún código.

## v1.4 (05-10-2026)

Ajustes de la v1.3 tras su primera ejecución real. Propuesta y evidencia en `docs/propuesta-prompt-v1.4.md`. El prompt y el anexo los redactó el autor en el proyecto de Claude. El esquema sigue en la 1.3 y el prompt no crece (32.217 bytes, frente a 32.203).

### Qué hace

- **Observabilidad (§3):** un requisito describe lo que un usuario, un responsable del negocio o un sistema externo integrado puede observar, usar o exigir. Un comportamiento interno de la implementación es `design_detail`, salvo que la fuente lo imponga (`RT`/`RA`) o lo exija como atributo de calidad (`RNF`, uno por atributo). Si el índice muestra que la sección describe cómo está construido el sistema, se parte de que es `design_detail`. Responde a la mejora 1 de la v1.3.
- **`project_note` (§3):** lo que habla del proyecto, su historia o su código es nota del proyecto aunque mencione funciones del sistema. Si el texto dice qué debe hacer el sistema y lo construido difiere, el requisito es lo que debe hacer, con una pregunta `contradiction`. Responde a la mejora 2.
- **Nivel del inventario (§5):** una funcionalidad es lo que un usuario o un sistema externo reconocería como una función del producto (una pantalla, un proceso o una integración), con varios requisitos. Responde a la mejora 3.
- **Descartes enteros (§3):** un fragmento del que sale algo no se descarta entero; una sección solo se descarta entera si no se cita ninguno de sus fragmentos. Responde a la mejora 5.
- **R-h (§6):** las condiciones no empiezan por su palabra clave. Responde a la mejora 6.
- **N4 por funcionalidad (§8):** como máximo una pregunta "¿qué pasa si falla?" por funcionalidad del inventario, y ante la duda, ninguna. Responde a la mejora 7.
- **Una sola escritura (§2 y §10):** el catálogo se escribe con una sola llamada a `Write`, después de autoverificar. Motivo: en la ejecución de la v1.3, 8 de los 27 intentos de LUNA escribieron el catálogo más de una vez (el 36 % de lo escrito acabó sobrescrito, y un lote agotó los 10 turnos así), y 2 de 26 en SOL (6 %).
- **Anexo:** ante un C20, una entrada del inventario que solo era un detalle o una variante se une a otra funcionalidad.

Cambios en la plataforma que acompañan a la v1.4 (validador de referencia en Python y su port en C#):

- **Palabra clave inicial (red de seguridad de R-h):** el normalizador quita la palabra clave del principio de `feature`, `states`, `trigger` y `condition` (si, cuando, mientras, donde, en cuanto; if, when, while, where, as soon as, as long as), sin distinguir mayúsculas y solo si la sigue un espacio. Lo anota como `FIX`. Así ya no sale «SI si la reserva dura…».
- **Descartes enteros de lo citado (red de seguridad de §3; sustituye al aviso C17):** el normalizador quita el descarte entero (`quote` null) de un fragmento que citan un requisito o una pregunta que cumplen el esquema. Una sección descartada con algún fragmento citado se sustituye por descartes enteros de sus fragmentos no citados, con el motivo de la sección. Las dos correcciones se anotan como `FIX` en `DISCARDED[i]` y `SECTION[i]`. El aviso C17 desaparece: lo que antes se contaba como C17 se mide ahora con esos `FIX`. El §9 del prompt v1.4 aún nombra C17 como aviso; para el modelo no cambia nada.
- **Reintento por inactividad (runner):** un lote que termina sin ninguna salida durante el tiempo de inactividad (`HeartbeatLost`) se repite una vez con el mismo prompt. En la ejecución de la v1.3, tres lotes de LUNA murieron así a la vez, a los 15 minutos, mientras otros intentos de LUNA terminaban en 17 y 23 minutos: la causa era la pasarela, no el prompt. No cuenta como reparación.

### Resultados

Ejecución del 05-10-2026 con el documento técnico y funcional interno (608 fragmentos, 22 lotes), SOL (A) y LUNA (B), una sola ejecución. Las cifras salen de la página del trabajo; las marcadas con ~ se contaron a mano y son aproximadas. Faltan los tokens, la duración y las reparaciones por lote.

| | SOL v1.3 | SOL v1.4 | LUNA v1.3 | LUNA v1.4 |
|---|---|---|---|---|
| Lotes en el catálogo | 22 de 22 | 22 de 22 | 18 de 22 | 20 de 22 |
| Requisitos | 456 | 255 | 284 | 214 |
| Preguntas abiertas | — | 118 | — | 86 |
| Preguntas «¿qué pasa si falla?» | 37 | ~55 | 77 | ~43 |
| Requisitos de secciones de diseño técnico | 38 % | ~23 % | 17 % | ~23 % |
| Requisitos de las secciones finales del proyecto | 20 % | ~13 % | 29 % | — |
| Citas literales | — | 623/623 | — | 521/521 |
| Cobertura de fragmentos | — | 607/608 | — | 548/552 (de sus lotes) |
| Proporción MASTeR | — | 9 % | — | 11 % |
| «SI si» en el texto | sí | no | sí | no |

- **Volumen:** baja otra vez (−44 % SOL, −25 % LUNA), pero sigue muy lejos de lo que un usuario puede revisar.
- **Duplicados:** una misma operación aparece hasta 6 y 7 veces por modelo, porque el documento la describe en el resumen, la matriz de permisos, la pantalla, el backend, la API y el anexo, y cada sitio cae en un lote distinto. LUNA, además, parte los permisos en un requisito por rol.
- **Preguntas:** N4 por funcionalidad tiene efectos opuestos: LUNA baja y SOL sube. Unas 15 preguntas de LUNA son iguales palabra por palabra salvo el requisito.
- **Observabilidad:** efecto parcial en SOL y nulo en LUNA. Siguen saliendo requisitos sobre cabeceras, cookies o nombres de permisos internos.
- **Especificación frente a implementación:** LUNA aplica la regla de la v1.4: requisito con pregunta `contradiction`. SOL saca los mismos requisitos sin la pregunta.
- **Correcciones nuevas:** ningún «SI si». La sustitución de secciones descartadas con fragmentos citados actúa 4 veces en SOL y 2 en LUNA.
- **Lotes perdidos de LUNA:** el lote 1 escribió un catálogo y se declaró `bloqueada` («catálogo incompleto»); la plataforma no valida el catálogo en ese caso y lo pierde entero. El motivo del lote 19 está sin registrar.

### Qué hay que mejorar

La v1.4 confirma que el límite ya no está en las reglas del prompt sino en el proceso por lotes: un lote no puede evitar repetir lo que hacen otros lotes que no ve, ni las preguntas pueden tener un tope si cada lote abre las suyas. El autor no fija un número de requisitos ni de preguntas: el catálogo tiene que estar completo, sin repeticiones y con solo lo que es requisito, y el tamaño sale del documento. Se pasa a la v2, de arriba abajo: `docs/propuesta-catalogo-v2.md`.

Pendientes de la plataforma que siguen valiendo en la v2: validar el catálogo cuando el modelo se declara `bloqueada`, diagnosticar con datos por qué LUNA tarda más y pierde lotes, y un informe de métricas automático para no contar a mano.

## v2 (05-10-2026)

Cambio de proceso, no solo de prompt. Propuesta en `docs/propuesta-catalogo-v2.md`; los prompts, los anexos, los esquemas 2.0 y los ejemplos los redactó el autor en el proyecto de Claude, y su detalle está en la entrada v2 de `prompts/generacion_requisitos/CHANGELOG.md`.

### Qué hace

- **Dos fases.** Fase 1: con el documento entero, cada modelo escribe un inventario de funcionalidades (`functional`, `integration`, `quality`, `constraint`), los descartes y las preguntas globales (`QI-`). Fase 2: los requisitos de cada funcionalidad, cada una en un solo lote, con el detalle en criterios etiquetados.
- **Sin cifras objetivo.** El catálogo tiene que estar completo (ninguna funcionalidad sin requisito), sin repeticiones y con solo lo que es requisito; el tamaño sale del documento.
- **Actores que hacen lo mismo son un requisito** (U4); los permisos que difieren van en `[RULE]`. En la v1.4 eran requisitos distintos.
- **Objeciones:** si el inventario se equivocó, la fase 2 lo declara (`duplicate`, `not_a_requirement`, `future`) en vez de forzar un requisito.
- **Preguntas:** solo las que bloquean, cada duda una vez, sin `missing_error_handling`. Las funcionalidades sin criterio `[ERROR]` las lista la plataforma.
- **El prompt de requisitos es más corto:** 30.630 bytes frente a 32.217 (−5 %).

**Validador del inventario (nuevo: `validar_inventario.py` y `InventoryValidator`):**

- Un elemento que no cumple el esquema se ignora con aviso; un fallo en la raíz es error del inventario.
- **IDS:** un `CAP-` o `QI-` repetido es error. Las referencias a funcionalidades y los fragmentos que no existen se quitan (`FIX`).
- **C10 con umbral 0:** todo fragmento de documento tiene que estar en una funcionalidad, un descarte, una sección descartada o la cita de una pregunta. En la v1.x se toleraba hasta el 10 %; en la v2 un fragmento sin asignar no llega a ningún lote.
- **C6:** las citas de las preguntas, como en la v1.x (rechazan la pregunta).
- **Exclusividad (`FIX`):** un fragmento asignado a una funcionalidad pierde su descarte; una sección descartada con fragmentos asignados se sustituye por descartes de los demás. Es la lógica de la v1.4, ahora entre funcionalidades y descartes.
- **REP (aviso):** posible repetición si dos funcionalidades tienen un Jaccard de fragmentos ≥ 0,5 (sin contar los fragmentos de 3 o más funcionalidades, como un resumen o una matriz de permisos) o de palabras del nombre ≥ 0,75 (en minúsculas, sin tildes, sin artículos ni preposiciones). Umbrales iniciales, pendientes de calibrar.
- **KIND (aviso):** una `quality` o `constraint` con actores. **VERDICT:** `failed` es error; `partial` sin fragmentos sin procesar, o al revés, es aviso.
- **Renumeración** de `CAP-` y `QI-` en orden: desde aquí los ids son fijos.
- Ambos ejemplos (ReservaSalas y LibraryLoan) validan sin errores ni avisos; 22 mutaciones con paridad Python/C#.

**Validador de requisitos v2 (`validar_catalogo.py` en su sitio y `CatalogValidatorV2`):**

- Recibe el inventario normalizado y las funcionalidades del lote; los fragmentos del lote son los de sus funcionalidades, más las dudas.
- **Desaparecen** C10 (la cobertura es de la fase 1), SELF_CHECK, la métrica `autoverificacion_coincide` y las correcciones de descartes (pasan al inventario). `missing_error_handling` lo rechaza ya el esquema.
- **C20 por kind (reparación):** cada funcionalidad del lote sin objeción necesita un requisito aceptado del tipo que exige su kind: `RF`/`RI` (functional, integration), `RNF` (quality), `RA`/`RT`/`RNF` (constraint). En la v1.3 solo contaban `RF`/`RI`, y saltaría en toda `quality` y `constraint`.
- **C21 para todos los tipos (aviso):** requisito sin funcionalidad. El normalizador quita las funcionalidades que no son del lote y, en un lote de una sola funcionalidad, completa un `capability_ids` vacío (`FIX`).
- **C23 (aviso nuevo):** fragmentos del lote que no cita ningún requisito ni pregunta. Mide la pérdida de detalle sin obligar a citarlo todo.
- **C24 (aviso nuevo):** el tipo del requisito no lo admite el kind de su funcionalidad (`RNF` vale en las cuatro; `RF`/`RI` solo en functional e integration; `RA`/`RT` solo en constraint).
- **Objeciones:** una de una funcionalidad que no es del lote, o un `duplicate_of` que no existe o es la misma, se ignora con aviso (`OBJECTION`). Una válida exime a su funcionalidad del C20; **C25 (aviso nuevo)** si aun así un requisito la cita.
- **Preguntas del inventario:** una `QI-` vale en `open_question_ids` y conserva su id; el vínculo inverso lo completará la unión.
- **VERDICT:** `partial` si y solo si `uncovered_capabilities` no está vacío.
- **Informe nuevo, en lugar de N4:** las funcionalidades `functional`/`integration` sin ningún criterio `[ERROR]`. Es una lista, no preguntas.
- Ambos ejemplos validan sin errores ni avisos; sus textos generados están en `catalogo_ejemplo_v2.enriquecido.json`. 75 mutaciones con paridad Python/C#: las comprobaciones de cada requisito de la v1.3 pasadas a los ejemplos v2, más una por cada comprobación nueva. Los fixtures v1.3 quedan congelados para el validador v1.3, que sigue usando la plataforma.

**Unión de los lotes (`CatalogMergerV2`, solo en C#, como la de la v1.3):**

- Las funcionalidades y los descartes vienen del inventario; los requisitos se renumeran por tipo entre lotes. No hace falta deduplicar requisitos: cada funcionalidad está en un solo lote.
- Las preguntas `QI-` del inventario y las `Q-` de los lotes forman una sola lista. Las del mismo tipo con el mismo texto tras normalizar se fusionan (`FIX`), como red de seguridad: en la v1.4, unas 15 preguntas de LUNA eran iguales salvo el requisito. Se ordenan por tipo (contradicción, alcance, valor, ambigüedad, actor), no se recorta ninguna y todas se renumeran `Q-`.
- Toda pregunta final lleva `related_capabilities` y `related_requirements`, con el enlace completado en los dos sentidos.
- Las objeciones de los lotes se juntan, los términos de dominio repetidos se quitan y los veredictos se combinan.

**Refactor previo en C#:** las comprobaciones de cada requisito, que no cambian entre la v1.3 y la v2, pasan a `RequirementChecks`, compartido por los dos validadores. Hasta el paso 38 conviven el validador v1.3 (el que usa la plataforma) y los v2.

### Resultados

Pendiente: la plataforma todavía ejecuta la v1.4.

### Qué hay que mejorar

Pendiente de los resultados.

---

## Cómo mantener este documento

Cada vez que cambie el prompt de requisitos (su texto, su anexo, su esquema o el validador que lo acompaña) o se mida una versión:

1. **Versión nueva:** una sección con **Qué hace**, **Resultados** y **Qué hay que mejorar**, y una fila en el resumen.
2. **Resultados:** las mismas métricas de "Cómo se mide", con fecha, documento, modelos y si fue una sola ejecución o varias.
3. **Lo que había que mejorar en la versión anterior** se enlaza con lo que cambia en la nueva.
4. **Documentos internos:** solo cifras agregadas y anonimizadas.

## v0.10 (19-10-2026)

Cambian los tres prompts del árbitro. Detalle en `prompts/v0/CHANGELOG.md`.

### Qué hace

- **Antes de llamar al árbitro, la plataforma empareja las dos propuestas por significado,** con los embeddings en local y los umbrales que calibró el autor en el paso 87: «mismo» desde 0,90, «parecido» desde 0,70. El árbitro recibe ese emparejamiento en `input/emparejamiento.txt`. Ya no tiene que buscar él solo qué elementos se corresponden: los pares «mismo» se unen, los «parecido» los decide él y los «sin par» se mantienen o se descartan con las reglas de siempre.
- **El emparejamiento es orientativo** (decisión del autor): el árbitro puede apartarse si la fuente lo justifica. La plataforma busca dónde lo hizo:
  - unir algo sin nada parecido en la otra propuesta;
  - dejar separado un par «mismo».

  En el catálogo y el backlog, esos elementos son discrepancias que el usuario revisa al validar, con la explicación de la plataforma. En las preguntas solo se cuentan.
- **Las discrepancias de contenido** (una cifra o una regla distinta) las sigue marcando el árbitro, porque los embeddings miden cuánto se parecen dos textos, no si se contradicen.

**Por qué:**
- es el diseño original del TFG, simplificado en la iteración v0;
- con las mismas propuestas, la unión del catálogo del proyecto grande de ejemplo salió de 209, 220 y 291 requisitos según la ejecución del árbitro (`docs/experimento-sesgo-arbitro.md`). Un emparejamiento fijo de partida debería hacerla más estable.

**Cuántas discrepancias se esperan.** Se estimó con los 9 trabajos originales de los proyectos ficticios, con los árbitros de antes:
- con la opción elegida (lo que el árbitro hace en contra de los números), entre 0 y 10 por trabajo, y 21 en el backlog del proyecto grande;
- marcar toda la franja «parecido» habría dado 91 de 209 requisitos en el catálogo grande, y se descartó.

### Resultados

**Proyecto grande de ejemplo** (clínicas veterinarias, ficticio), catálogo, 19-10-2026. Tres nuevos arbitrajes con `SS - GPT-6-SOL` sobre las mismas propuestas de A (299 requisitos) y B (192) que los del experimento del sesgo. Tabla completa en `docs/resultados-arbitro-v0.10/clinicas-veterinarias-catalog-v0.md`, generada con `scripts/evaluacion/comparar-arbitrajes.cs`.

| Árbitro y prompt | Válidos | Unión (mín.–máx., media) | Solo A | Solo B (media) | Desviaciones (media) | Discrepancias del árbitro | Minutos |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SOL v0.10 | 3 de 3 | 202–222, 212,3 | 26–37 | 1,3 | 7,7 | 0 | 5, 5 y 11 |
| SOL v0.9 | 3 de 3 | 209–291, 240,0 | 27–30 | 2,7 | 7,0 (las que habría marcado) | 0 | 12, 6 y 6 |
| LUNA v0.9 | 2 de 5 cerrados | 204–209, 206,5 | 27 | 1,5 | 7,5 (las que habría marcado) | 0 | 27 y 12 |

- **La unión es más estable:** con la v0.10 varía 20 requisitos entre ejecuciones (202–222), frente a 82 con la v0.9 (209–291). Con solo 3 ejecuciones por versión es una señal, no una prueba: sin la ejecución de 291, la v0.9 se movía en 209–220.
- **Las desviaciones no bajan:** el árbitro se aparta del emparejamiento en unos 7 u 8 requisitos por ejecución, con la v0.10 y con la v0.9. Tenerlo como guía no hace que lo siga más en los puntos donde discrepa: son diferencias de criterio que se repiten, y por eso tiene sentido enseñárselas al usuario, como decidió el autor.
- **Discrepancias del propio árbitro:** ninguna en ninguna ejecución. En este catálogo, todo lo que el usuario tendría que revisar sale de la plataforma (unos 8 requisitos de 212).
- **Fiabilidad:** dos ejecuciones a la primera. En la tercera, el primer intento terminó sin escribir la salida tras 98 turnos y unos 1,15 M tokens de entrada, y la reparación la escribió. Esos 11 minutos son de ese intento.
- **Consumo:** los intentos válidos no tienen dato, porque la plataforma los detuvo al compactar con la salida ya escrita (el mismo problema que en el experimento; paso 95).

### Qué hay que mejorar

- **Más repeticiones** para confirmar la estabilidad, y repetirlo con las preguntas, que son más baratas.
- **El intento que agotó los turnos sin escribir** (1,15 M tokens de entrada perdidos). Conviene saber si consultó la documentación de más, ahora que también lee el emparejamiento. Se relaciona con el paso 92: parar las ejecuciones que compactan varias veces antes de escribir.
- **El consumo de las ejecuciones detenidas,** sin el cual no se puede comparar el coste de la v0.9 y la v0.10 (paso 95).

## v0.8 (15-10-2026)

Solo cambia el mockup. Detalle en `prompts/v0/CHANGELOG.md`.

### Qué pasó con el mockup v0.6 y v0.7

Documento C (backlog validado de 9 épicas), 14 y 15-10-2026, con la referencia de estilo de un `.zip`. Las horas no sirven como medida: el trabajo se interrumpió por la noche y se volvió a arrancar al día siguiente.

| Ejecución | Modelo | Resultado | Tokens entrada / salida | Turnos |
|---|---|---|---|---|
| Diseño | `SS - GPT-6.1-SOL` | Tiempo agotado (45 min), sin eventos guardados | — | — |
| Diseño | `SS - GPT-6-SOL` | Válido: mapa de **19 pantallas** y armazón | 233.920 / 31.397 | 12 |
| 19 pantallas | `SS - GPT-6-SOL` | **Las 19 válidas a la primera**, sin reparación | entre 88.307 y 147.493 / entre 12.573 y 29.152 cada una | 11–13 |
| Revisión del conjunto | `SS - GPT-6-SOL` | **Falla**: lee el armazón y las 19 pantallas, compacta tres veces y Claude Code la para («autocompact is thrashing») | 326.083 / 2.255 | 37 |

- **Construir por pasos funciona:** con contexto pequeño en cada ejecución, el diseño y cada pantalla salen válidos a la primera.
- **La revisión del conjunto no escala:** leer el prototipo entero no cabe en 128k a partir de unas pocas pantallas.
- **La 6.1 es demasiado lenta** para las respuestas largas del diseño, como ya pasó con el catálogo.
- Coste del diseño y las pantallas: unos 2,43 M tokens de entrada y 417.000 de salida.

### Qué hace

- **La plataforma junta y revisa:** además de las comprobaciones de la página, busca `id` repetidos, nombres globales de JavaScript repetidos y navegación a pantallas que no existen. Son los fallos típicos de partes escritas por separado.
- **Correcciones solo donde hace falta:** una ejecución por parte con problemas, que lee solo esa parte, el mapa y el armazón.
- **Reanudar:** un mockup fallido se reanuda desde sus pasos aceptados, sin repetirlos.

### Resultados

Documento C, 15-10-2026, `SS - GPT-6-SOL`: se reanudó el trabajo anterior, reutilizando el diseño y las 19 pantallas sin llamar al modelo.

- La plataforma encontró problemas en **una sola pantalla** (P19: un `id` que ya usaba otra parte y enlaces a pantallas que no existían). Su corrección tardó unos 3 minutos (149.497 tokens de entrada y 17.248 de salida).
- **Mockup generado:** 19 pantallas, las 46 historias, 736 KB, sin avisos.
- Juntar y revisar el prototipo con la plataforma cuesta unos segundos, frente a la revisión del conjunto de la v0.7, que no cabía en el contexto.

### Qué hay que mejorar

- **Al autor no le sirve como prototipo:** tiene un menú lateral con todas las pantallas, porque el prompt de diseño pedía «un enlace por pantalla del mapa». Es un índice de pantallas, no una aplicación que se pueda usar. Sí siguió bien el estilo del mockup de referencia. Se corrige en la v0.9.

## v0.9 (15-10-2026)

Solo cambia el mockup. Detalle en `prompts/v0/CHANGELOG.md`.

### Qué hace

- **Una aplicación que funciona:** se entra por el inicio de sesión con un usuario de prueba, cada rol ve su aplicación y se navega por menús de secciones y por acciones, como en el sistema real.
- **La navegación está en el mapa y se comprueba:** desde el inicio de sesión se tiene que llegar a todas las pantallas, y cada pantalla tiene que llevar de verdad a las que dice el mapa.

### Resultados

Documento C (backlog validado de 9 épicas y 46 historias, con la referencia de estilo de un `.zip`), 15-10-2026, `SS - GPT-6-SOL`, generado desde cero.

| Paso | Resultado | Tokens entrada / salida | Turnos |
|---|---|---|---|
| Diseño, intento 1 | Falla: 26 pantallas, el máximo es 25 | 174.330 / 32.645 | 11 |
| Diseño, reparación | Válido: inicio de sesión y 25 pantallas en total | 29.188 / 5.923 | 3 |
| 25 pantallas | **Todas válidas a la primera** | entre 85.189 y 299.389 / entre 10.688 y 43.250 cada una | 11–14 |
| Plataforma | Junta el prototipo y encuentra problemas en una sola parte | — | — |
| 1 corrección | Válida | 119.908 / 14.920 | 12 |
| **Total** | **Mockup generado: 25 pantallas, las 46 historias, 866 KB, sin avisos** | **3.465.323 / 548.080** | — |

- **Tiempo:** 40 minutos de principio a fin, con hasta 3 pantallas a la vez.
- **El autor lo da por bueno:** «esto ya se parece mucho a lo que yo busco». Se entra por el inicio de sesión y se navega como en la aplicación real.
- **La navegación pide más pantallas** que el índice de la v0.8 (25 frente a 19): aparecen la del inicio de sesión y las pantallas de detalle a las que se llega con acciones.

**Proyecto grande de ejemplo** (clínicas veterinarias, ficticio, de `ejemplos/`: backlog validado de 15 épicas y 109 historias, sin referencia de estilo), 15 y 16-10-2026, `SS - GPT-6-SOL`, hasta 3 pantallas a la vez:

| Paso | Resultado | Tokens entrada / salida |
|---|---|---|
| Diseño | Válido a la primera: 38 pantallas (aviso por encima de 25) | incluido en el total |
| 37 pantallas | Válidas; 4 tras una reparación (`FRAGMENTO`: la pantalla traía `<!doctype>`, `<html>`… que ya pone el armazón) | incluido en el total |
| P34 | **Falla: 504 de la pasarela** en los 10 reintentos de Claude Code (unos 18 min); el trabajo termina sin mockup | sin dato real |
| Reanudación (16-10) | Solo repite la P34: válida en 2,5 min | 68.530 / 18.906 |
| Plataforma | Junta las 38 pantallas sin encontrar problemas: ninguna corrección | — |
| **Total** | **Mockup generado: 38 pantallas, las 109 historias, 1,54 MB** | **3.283.920 / 888.795** |

- **Tiempo:** 71 minutos el primer trabajo y 2,5 minutos la reanudación.
- **El autor lo da por bueno:** «se ha creado el mockup muy completo».
- **Reanudar funciona en un caso real:** las 37 pantallas aceptadas y el diseño se aprovechan, y solo se repite la que falló. Sin esa opción, el fallo de la pasarela habría obligado a repetir más de una hora de trabajo.
- **El 504 fue puntual:** la P34 escribió 18.906 tokens, menos que otras pantallas (hasta 36.000), y al reanudar terminó a la primera.
- **Coste por historia, más bajo que con el documento C:** unos 30.000 tokens de entrada por historia, frente a unos 75.000. El backlog es más del doble de grande, pero cada pantalla cubre más historias.

### Qué hay que mejorar

- **Tamaño:** 866 KB, cerca del límite de 1 MB. Con un backlog mayor se pasaría. (Resuelto el mismo día, autor: el límite pasa a 10 MB, con un aviso por encima de 2 MB.)
- **Coste:** unos 3,5 M tokens de entrada, el 70 % en las pantallas (unos 115.000 de media cada una, sobre todo por leer el armazón entero). Se podría pasar a cada pantalla solo la parte del armazón que usa: el CSS de su aplicación y las funciones comunes.
- **Reparaciones evitables:** 4 de 38 pantallas escribieron un documento HTML completo en lugar de solo su parte. La plataforma podría quitar `<!doctype>`, `<html>`, `<head>` y `<body>` ella misma, como hace con la marca `sigue`, en lugar de pedir una reparación.
- **Un trabajo interrumpido no se puede reanudar:** si la API se reinicia con el mockup en marcha, el trabajo se queda sin fecha de fin y «Reanudar» no está disponible. Pendiente de que lo decida el autor: cerrar los trabajos interrumpidos al arrancar la API.
- **Máximo de pantallas:** con la navegación real, el diseño se pasó de 25 en el primer intento. (Resuelto el mismo día, autor: no hay máximo; las que tenga la aplicación real, con un aviso por encima de 25.)

## v0.7 (14-10-2026)

Corrige dos fallos de la primera prueba real con un proyecto grande («documento C», un proyecto de prueba del autor: 8 documentos, entre ellos hojas de cálculo, y unos 40.000 tokens de documentación; `SS - GPT-6-SOL` y `SS - GPT-6-LUNA`, prompts v0.5, 14-10-2026). Detalle en `prompts/v0/CHANGELOG.md`.

### Qué pasó con la v0.5

| Trabajo de catálogo | A (SOL) | B (LUNA) | Árbitro (SOL) |
|---|---|---|---|
| 1 | Válido: 136.236 / 21.134 tokens, 6 turnos | Válido tras reparar (esquema): 167.733 / 14.830 | **Falla: agota los 30 turnos**. Compacta seis veces y vuelve a leerlo todo después de cada una: 1.591.624 / 9.469 tokens |
| 2 | Válido: 229 requisitos | Falla dos veces: `REF-FRAG` (`DOC-06#F830`) | No se lanza; el resultado fue la salida de A sola |
| 3 | Válido: 233.791 / 26.764 | Falla dos veces: `REF-FRAG` (131 errores, `DOC-06#F552`…) | No se lanza; otra vez la salida de A sola |

- **El árbitro no cabe en la ventana.** Cada propuesta ocupa unas 2.000–2.900 líneas de JSON con sangría (unos 230 requisitos y 91.000 caracteres), y SOL lee además toda la documentación, aunque es opcional. Cada compactación le hace perder lo leído y empezar de nuevo, incluso desde el `offset` 0.
- **LUNA cita números de línea como fragmentos:** `DOC-06#F830` es la línea 830 de `input/documentacion.txt`, donde está `DOC-06#F537`; `DOC-02#F250` es la línea 250, donde está `DOC-02#F021`. La reparación, que solo decía «no existe», no lo corrigió.

### Qué hace

- **Propuestas con un elemento por línea:** un requisito por línea en lugar de un valor por línea con sangría, alrededor de una quinta parte menos.
- **La documentación del árbitro se consulta por tramos,** con la línea en la que empieza cada documento, en lugar de leerla entera.
- **Un fragmento no es un número de línea:** lo dicen los prompts y, si pasa, el error `REF-FRAG` dice qué fragmento hay en esa línea.
- **Sin la salida de un solo modelo:** si A o B fallan, el trabajo falla (decisión del autor).

### Resultados

Documento C, catálogo, 14-10-2026, una ejecución: A `SS - GPT-6-SOL`, B `SS - GPT-6-LUNA` y árbitro `SS - GPT-6.1-SOL` (primera vez que se usa la 6.1).

| Ejecución | Resultado | Tokens entrada / salida | Turnos |
|---|---|---|---|
| A | Válido a la primera | 231.528 / 24.017 | 6 |
| B, intento 1 | Falla: cita `DOC-06#F455 sigue` (el id con la marca de continuación) | 135.654 / 27.637 | 6 |
| B, intento 2 | Válido | 187.867 / 19.021 | 6 |
| Árbitro | **Tiempo agotado (30 min)** después de escribir el catálogo unido | sin evento final | — |

- **Un elemento por línea funciona:** las propuestas ocupan unas 200 líneas cada una, frente a 2.000–2.900 con sangría, y el árbitro las lee en 4 llamadas a `Read`.
- **La consulta por tramos funciona a medias:** el árbitro va a tramos concretos de la documentación, pero suma unas 600 de las 838 líneas.
- **LUNA ya no cita números de línea.** Su nuevo error es copiar la marca `sigue` en el id; la reparación lo corrige.
- **El árbitro escribe el catálogo y se atasca después:** al ir a contestar con el veredicto, Claude Code intenta compactar el contexto siete veces sin terminar, hasta agotar los 30 minutos. El catálogo unido estaba escrito, pero una ejecución con el tiempo agotado no se revisaba.

Cambios de la plataforma tras esta prueba (sin cambiar los prompts):
- **El runner para una ejecución en cuanto Claude Code empieza a compactar con todas las salidas esperadas ya escritas** (`CompactingAfterOutput`). Solo faltaba la línea del veredicto: se revisa la salida como si hubiera contestado, con su reparación si no es válida.
- **Una ejecución que agota el tiempo (o se queda sin actividad) después de escribir su salida se revisa:** si la salida es válida, se acepta con un aviso. Si no lo es, sigue sin repararse, como hasta ahora.

**Proyecto grande de ejemplo** (clínicas veterinarias, ficticio, de `ejemplos/`: 21 archivos de 13 formatos, unos 23.400 tokens), catálogo, 15-10-2026, una ejecución. Los prompts del catálogo son los de la v0.7 (los del trabajo dicen v0.9, que solo cambia el mockup). A y árbitro `SS - GPT-6-SOL`, B `SS - GPT-6-LUNA`:

| Ejecución | Resultado | Tokens entrada / salida | Turnos |
|---|---|---|---|
| A, intento 1 | Falla: cita `DOC-17#F003 sigue` (esquema y `REF-FRAG`) | 186.752 / 32.399 | 5 |
| A, intento 2 | Válido; **detenida al empezar a compactar** después de escribir | sin dato | — |
| B, intento 1 | Falla: cita `DOC-03#F285`, un número de línea (en esa línea está `DOC-03#F055`), y 2 errores más | 120.330 / 34.870 | 5 |
| B, intento 2 | Válido | 189.278 / 24.511 | 5 |
| Árbitro | Válido; **detenido al empezar a compactar** después de escribir | sin dato | — |

- **Elementos:** A propone 299 requisitos y B 192. La unión tiene 209: 180 de ambas, 28 solo de A y 1 solo de B, ninguna discrepancia y 3 descartados.
- **Duración:** 12 minutos el trabajo entero.
- **El corte al compactar funciona,** y dos veces en el mismo trabajo: el intento 2 de A y el árbitro escriben su salida, Claude Code empieza a compactar antes del veredicto y la plataforma acepta la salida. La compactación llega al final porque el contexto suma la base, el prompt, la documentación leída y el propio `Write` del catálogo, unos 32.000 tokens en SOL, y la reparación añade el catálogo anterior con sus errores.
- **Las dos reparaciones las provocan citas mal copiadas,** no el contenido: la marca `sigue` en SOL y un número de línea en LUNA, que vuelve a aparecer aunque no se vio en la prueba anterior. Cada reparación cuesta unos 190.000 tokens de entrada.
- **El coste del informe se queda corto:** las ejecuciones detenidas no tienen dato de consumo, porque Claude Code solo lo da en su evento final. A través de la pasarela, el consumo de cada mensaje llega a 0 (comprobado en las capturas reales de `tests/fixtures/stream-json/`), así que no se puede sumar por mensajes. Los 496.360 tokens de entrada y 91.780 de salida del informe son de A1, B1 y B2.

Cambios de la plataforma tras esta prueba (sin cambiar los prompts):
- **La plataforma quita la marca `sigue`** de los fragmentos citados antes de validar y de guardar la salida (`FragmentCitations`), igual que quita los prefijos de las propuestas del árbitro. Habría evitado la reparación de A.
- **El informe dice cuántas ejecuciones no tienen dato de consumo** (columna `ejecuciones_sin_consumo` en el CSV), y la web avisa de que los tokens son un mínimo.
- **La consola ya no dice «Terminada: Failed»** en una ejecución detenida después de escribir su salida: dice que se detuvo y que la plataforma revisa la salida.

### Qué hay que mejorar

- Si el árbitro sigue sin caber con propuestas aún más grandes, hará falta un árbitro por partes (por tipo de requisito o por documento).
- Parar una ejecución que compacta varias veces seguidas antes de escribir su salida, en lugar de dejar que agote los turnos (cada compactación cuesta casi 100.000 tokens). Pendiente de que lo decida el autor.
- ~~Quitar la marca `sigue` de un id citado en la plataforma, en lugar de gastar una reparación.~~ Hecho el 15-10-2026 (`FragmentCitations`).
- LUNA sigue citando a veces números de línea como fragmentos. Corregirlo en la plataforma (cuando el fragmento no existe y la línea con ese número es inequívoca) sería decidir por el modelo: pendiente de que lo decida el autor.
- Las ejecuciones detenidas no tienen dato de consumo. Solo se podría estimar a partir del texto leído y escrito; hoy el informe lo marca como mínimo.

## v0.6 (14-10-2026)

Solo cambia el mockup, por decisión del autor: un único prototipo que reúna todas las funcionalidades, construido poco a poco con muchas llamadas y no en una sola ejecución. Detalle en `prompts/v0/CHANGELOG.md`.

### Qué hace

- **Una aplicación, no un catálogo de historias.** El diseño decide las pantallas que tendría el sistema real y reparte en ellas todas las historias. Una pantalla reúne lo que el usuario hace en el mismo sitio, como listar, crear, modificar y cancelar.
- **Construido por pasos.** Primero el diseño (mapa y armazón), después una ejecución por pantalla sobre el armazón (hasta 3 a la vez) y al final una revisión del conjunto que corrige con Edit lo que no encaje. La web muestra cada paso con su consola («Pantalla 3 de 7»).
- **Contexto pequeño en cada paso.** Cada ejecución lee solo lo que necesita: el diseño, el backlog; cada pantalla, el mapa, el armazón y sus historias. Así cabe en los 128k de `SS - GPT-6-SOL` sin compactar, frente a 1,49 M de entrada y una compactación del mockup v0.4 en una sola sesión.
- **Comprobaciones en cada paso:** el mapa cubre todas las historias, cada pantalla marca las suyas con `data-hu` y la página juntada tiene todas las pantallas e historias.

### Resultados

Pendiente de la primera prueba con `SS - GPT-6-SOL`.

### Qué hay que mejorar

Pendiente de la primera prueba.

## v0.5 (14-10-2026)

Solo cambia el prompt del mockup, por decisión del autor: si el cliente ya tiene un mockup, el nuestro debe seguir su estilo. Detalle en `prompts/v0/CHANGELOG.md`.

### Qué hace

- **Referencia de estilo.** El mockup del cliente se sube como `.zip` en Documentos, marcado como «estilo». Solo lo usa el mockup.
- **El modelo saca el estilo.** Recibe las páginas y hojas de estilo en `input/estilo/` y las consulta con `Read` sin tener que leerlas enteras. La plataforma no resume colores ni fuentes (decisión del autor).
- **Imágenes y fuentes incrustadas.** El modelo las usa por su ruta y la plataforma las mete en el `.html` como `data:`, así que el mockup sigue siendo un único fichero sin recursos externos. Una ruta desconocida da el error `ESTILO`.

### Resultados

Pendiente de probarlo con un modelo y un mockup de referencia.

### Qué hay que mejorar

Pendiente de la primera prueba. Siguen abiertos la calidad y el coste del mockup de la v0.4.

## v0.4 (13-10-2026)

Solo cambia el prompt del mockup, aprobado por el autor tras el fallo del mockup con el documento B. Detalle en `prompts/v0/CHANGELOG.md`.

### Qué hace

- **El mockup se escribe por partes.** El modelo escribe una base y un fichero por épica, cada uno con su propio `Write`. Ninguna respuesta tiene que llevar la página entera, así que el tamaño del backlog ya no choca con los 64.000 tokens de salida.
- **La plataforma junta y comprueba.** Pone los ficheros de las épicas en el lugar de `<!-- VISTAS -->` y valida la página igual que antes.
- **La reparación es parcial.** Los ficheros del intento anterior pasan a la carpeta de la reparación, y el modelo reescribe solo los que tienen errores.
- **Consola sin ruido.** Las líneas `thinking_tokens` de Claude Code ya no aparecen en la consola.

### Resultados

Backlog validado del documento B: 9 épicas y 53 historias. Sonnet 5.5 con la suscripción del autor (13-10-2026).

| Intento | Resultado | Tiempo | Turnos | Tokens entrada / salida |
|---|---|---|---|---|
| 1 (v0.4) | Falla: límite de sesión de la suscripción (429), con la base y 3 épicas escritas | 21 min | 14 | 748.341 / 184.645 |
| 2 (v0.4) | **Generado**: 166 KB, las 53 historias, 1 compactación | 23 min | 20 | 1.489.928 / 203.848 |

- **Escribir por partes funciona:** la base y una escritura por épica, sin pasar de 64.000 tokens en ninguna respuesta.
- **Pero cuesta mucho.** Cada épica ocupa unos 20.000–40.000 tokens de salida contando el razonamiento, porque el prompt pide una interfaz usable con la interacción de cada criterio. El contexto se llena y se compacta, y el mockup consumió el 43 % del límite de 5 horas de la suscripción del autor. El primer intento lo agotó.
- **Al autor no le convence el resultado,** aunque lo da por bueno de momento.
- Hubo un aviso falso (`HU-EXTRA` con `${h[0]}`): el validador tomaba por vista un `data-hu` construido en JavaScript. Corregido.

### Qué hay que mejorar

- **Calidad y coste del mockup,** que el autor deja para más adelante. Hay dos opciones:
  - **Vistas más sencillas:** la pantalla de cada historia con sus elementos y sus mensajes como estados visibles, e interacción solo donde un criterio pide un rechazo o un aviso.
  - **Un run por épica,** sin compactación y repitiendo solo las épicas que fallen.
- La consola sigue mostrando como aviso las líneas `status` de la compactación.

## v0.3 (09-10-2026)

Aprobado por el autor tras el tercer fallo del árbitro con los ids de las propuestas. Detalle en `prompts/v0/CHANGELOG.md`.

### Qué hace

- **Ids con el número de su propuesta.** En los ficheros que lee el árbitro, cada pregunta, requisito o historia lleva su id con el número de su propuesta delante (`1:P-003`, `2:RF-001`), y el árbitro los copia así en `de` y en los descartes. El id dice de qué propuesta sale, así que ya no puede ponerlo en la otra sin que se note.
- **La plataforma deshace el cambio.** Quita a cada id el prefijo de su propia propuesta antes de validar y guarda la salida sin prefijos: esquemas, validador y flujo no cambian. Un id con el prefijo de la otra propuesta es un error `ARB-DE` que lo dice tal cual, para que la reparación sepa qué corregir.
- **No toca el orden de lectura.** El *position bias* que preocupaba al autor (que el árbitro prefiera la primera propuesta que lee) sigue neutralizado por el orden aleatorio de las propuestas.

### Resultados

Flujo completo con el documento B (13-10-2026, proyecto nuevo): preguntas, sugerencias de respuesta (todas aceptadas sin cambios), catálogo, backlog y mockup. A = SOL, B = LUNA, árbitro = SOL; mockup con Sonnet 5.5.

| Fase | A (SOL) | B (LUNA) | Árbitro (SOL) | Resultado |
|---|---|---|---|---|
| Preguntas | 132.358 / 11.061, 5 turnos | 84.504 / 8.659, 5 turnos | 91.548 / 8.755, 11 turnos | 35 preguntas (8 de ambas, 26 solo de la 1, 1 solo de la 2) |
| Sugerencias (SOL) | | | | 148.823 / 11.840, 5 turnos; 35 sugerencias |
| Catálogo | 167.722 / 22.179, 5 turnos | 109.511 / 27.113, 5 turnos | 563.544 / 33.312, 15 turnos, 1 compactación | 178 requisitos (152 de ambas, 23 solo de la 1, 3 solo de la 2), sin descartes ni discrepancias |
| Backlog | 135.601 / 25.559, 5 turnos | 81.738 / 18.648, 5 turnos | 406.428 / 30.615, 11 turnos, 1 compactación | 9 épicas, 53 historias y 148 criterios, todas de ambas |
| Mockup (Sonnet 5.5) | | | | Falla: 220.862 / 256.408 tokens, 7 turnos, 32 min |

Tokens de entrada / salida de cada run.

- **Los ids con prefijo funcionan.** Los tres árbitros terminan a la primera, sin reparación y sin ningún `ARB-DE`. Antes había fallado tres veces de tres.
- **El árbitro es ahora el run más caro.** En el catálogo lee dos propuestas de unos 90 requisitos y consulta la documentación: 563.544 tokens de entrada en 15 turnos, con una compactación. En el backlog, 406.428, también con una compactación, y su línea de veredicto no llegó (se aceptó la salida, que era válida).
- **El catálogo crece mucho:** 178 requisitos frente a los 100 del proyecto anterior con el mismo documento. Las 35 respuestas eran sugerencias de la IA aceptadas sin cambios, el efecto que ya se vio con ReservaSalas.
- **El mockup no cabe.** Con 53 historias, Sonnet piensa unos 30.000 tokens antes de escribir y el `.html` entero no entra en 64.000 tokens de salida. Claude Code corta cada respuesta y la ejecución acaba en «Claude's response exceeded the 64000 output token maximum». Además, Claude Code emite una línea `thinking_tokens` cada pocos tokens de razonamiento, y la consola del run la mostraba como aviso, cientos de veces.

### Qué hay que mejorar

- **El mockup tiene que escribirse por partes**, como ya se previó en el paso 62: un fichero por épica, que la plataforma junta en un solo `.html`.
- **Las líneas `thinking_tokens` no son avisos.** La consola no debería mostrarlas una a una.
- **El coste del árbitro con proyectos grandes.** Si crece más, puede leer la documentación solo en las partes que citan las propuestas, en lugar de consultarla libremente.

## v0.2 (09-10-2026)

Primer cambio que sale de la comparación de la v0.1 con un documento real, aprobado por el autor. Detalle en `prompts/v0/CHANGELOG.md`.

### Qué hace

- El plan de lectura (`{{LECTURA}}`) da los argumentos exactos de cada llamada a `Read`, en lugar de rangos de líneas, y el prompt pide hacer exactamente esas llamadas, recordando que la primera línea es la 1.
- No cambia nada más. Se descartaron, de momento, dos propuestas:
  - que el árbitro lea una propuesta entera antes que la otra: no hay pruebas de que ayude, y el orden ya se sortea en cada trabajo;
  - elegir entre todo en el prompt y lectura por partes según el tamaño: recupera los dos caminos que el autor descartó.

### Resultados

Una ronda de preguntas con el documento B (09-10-2026, proyecto nuevo):

| Run | Tokens entrada / salida | Turnos | Tiempo |
|---|---|---|---|
| A (SOL) | 131.222 / 11.999 | 5 | 108 s |
| B (LUNA) | 85.314 / 21.361 | 5, sin reparación | 161 s |
| Árbitro (SOL) | 169.859 / 19.308 | 13 + 4 (una reparación) | 128 s |
| Total | 386.395 / 52.668 | | |

- **El cambio funciona.** A y B piden exactamente `offset` 1, 243 y 446 con sus `limit`, y LUNA lee las 613 líneas a la primera.
- **El árbitro vuelve a confundirse** en su primer intento: cita `P-017` en la propuesta 2, que no lo tiene (28 errores `ARB-DE` y `ARB-FALTA`). Es la tercera vez de tres con la lectura por partes (preguntas v0.1, catálogo v0.1 y preguntas v0.2), y ninguna con la v0. Ya no parece variación del modelo: lo provoca leer las dos propuestas desde ficheros con los mismos ids. Lleva a la v0.3.
- 34 preguntas (4 de ambas, 18 solo de la 1, 12 solo de la 2).

### Qué hay que mejorar

- Si el árbitro sigue confundiendo los ids de las dos propuestas, habrá que separarlas mejor.
- El *position bias* (preferir la primera opción que se lee) solo se neutraliza en el conjunto de ejecuciones, gracias al orden aleatorio. Quitarlo en cada run exige llamar dos veces al árbitro con el orden cambiado, a doble coste: queda como trabajo futuro.

## v0.1 (09-10-2026)

Propuesta del team leader y aprobada por el autor, después de que un documento más grande no cupiera en la ventana. Es la v0 con otra forma de recibir la entrada. Las reglas del catálogo no cambian. Detalle en `prompts/v0/CHANGELOG.md` y en el hito 12b de `docs/plan-de-construccion.md`.

### Qué hace

- **La documentación no va en el prompt.** Va a `input/documentacion.txt`, un fragmento por línea (`[DOC-01#F007 §2.5] texto`), y el modelo la lee por partes con `Read`, como Claude Code en local. Si la ventana se llena, Claude Code compacta el contexto (resume lo leído) y sigue.
- **La plataforma marca los tramos.** El prompt lleva el número de líneas y los tramos que hay que leer, de unos 40.000 caracteres como mucho, para que cada uno quepa en una llamada a `Read`.
- **Lectura comprobada.** La plataforma registra cada llamada a `Read` y, si queda alguna línea sin leer, pide la reparación con las líneas que faltan (código `LECTURA`). Las compactaciones quedan como aviso en el run.
- **Límites.** Ya no hay rechazo por el presupuesto de 80.000 tokens, que ahora solo se aplica al prompt. Hay un tope de documentación de 300.000 tokens estimados por proyecto y hasta 30 turnos por run.

### Resultados

Primera comparación (09-10-2026): una ronda de preguntas con el documento B, en un proyecto nuevo para cada versión. A = SOL, B = LUNA y árbitro = SOL. El documento B es técnico-funcional, interno y anonimizado (solo cifras agregadas): 613 líneas en `input/documentacion.txt`, unos 27.000 tokens estimados. Cabe entero en la ventana. La ronda falló al principio por el error que contaba dos veces el documento (commit `7410b24`), no por su tamaño.

Las cifras de cada run suman todos sus intentos. El tiempo es el del run entero.

| | v0 (todo en el prompt, paso 67) | v0.1 (lectura por partes, paso 71) |
|---|---|---|
| A (SOL): tokens entrada / salida, turnos, tiempo | 87.483 / 9.988, 2 turnos, 79 s | 130.916 / 12.410, 5 turnos, 101 s |
| B (LUNA) | 85.851 / 15.873, 2 turnos, 116 s | 101.559 / 11.468, 5 + 4 turnos (una reparación por `LECTURA`), 94 s |
| Árbitro (SOL) | 99.056 / 7.705, 2 turnos, 52 s | 175.457 / 26.447, 8 + 7 turnos (una reparación por ids inventados), 156 s |
| Total de la ronda | 272.390 / 33.566 tokens, unos 2 min 50 s | 407.932 / 50.325 tokens (+50 %), unos 4 min 20 s |
| Lectura | — | A, todo a la primera; B, todo menos la última línea (repara); árbitro, las dos propuestas enteras y la mitad de la documentación como consulta |
| Compactaciones | — | Ninguna |
| Preguntas (de ambas · solo 1 · solo 2) | 28 (12 · 15 · 1) | 23 (4 · 15 · 4) |

Lo que muestra:

- **La lectura por partes funciona.** Los modelos siguen los tramos del prompt con `offset` y `limit`, y la comprobación de lectura detecta lo que se queda sin leer.
- **Con un documento que cabe, solo cuesta.** Cada tramo es un turno más, y en cada turno se reenvía el contexto acumulado: A gasta un 50 % más de entrada que en la v0 sin leer nada distinto. La ventaja (que no haya tope) solo aparece por encima de unos 80.000–100.000 tokens, y este documento no llega.
- **LUNA cuenta las líneas desde 0.** Pide `offset` 0, 242 y 445 donde el prompt decía los tramos 1–242, 243–445 y 446–613, así que nunca lee la última línea y hace falta una reparación (barata: 17.485 tokens). Lo provoca dar los tramos como rangos de líneas: el modelo los traduce a `offset` a su manera.
- **El árbitro confundió las propuestas en el primer intento.** Citó en `de` ids que no existían en la propuesta 2 (28 errores `ARB-DE` y `ARB-FALTA`); la reparación lo corrigió. En la v0 no pasó. Con una sola ronda no se puede saber si lo causa leer las propuestas desde dos ficheros, con los resultados de `Read` intercalados, o si es variación del modelo.
- **Las preguntas cambian entre versiones,** como cambian entre dos ejecuciones de la misma versión. La diferencia (28 frente a 23) no se puede atribuir a la lectura por partes con una sola ronda.

### Qué hay que mejorar

Se sabe de antemano:

- Leer por partes no hace infinita la ventana. Por encima de unos 100.000 tokens habrá compactaciones, y con cada una puede perderse detalle (cifras, ids de fragmentos) de lo leído antes.
- Cada tramo es un turno, y en cada turno se reenvía el contexto acumulado, aunque casi todo va en caché. El consumo de tokens crece con el tamaño del documento más deprisa que en la v0.
- Para corpus muy grandes la salida no es leer más, sino dividir el trabajo: por documento o por funcionalidad, como proponía la v2.

Tras la primera comparación (09-10-2026):

- **Dar los tramos como `offset` y `limit` exactos** («`offset` 1, `limit` 242; `offset` 243, `limit` 203…») en lugar de rangos de líneas, para que ningún modelo los traduzca a su manera. Evitaría la reparación de LUNA.
- **Separar mejor las dos propuestas para el árbitro,** por ejemplo pidiéndole que lea una entera antes de la otra. Hay que comprobarlo con más rondas.
- **Elegir por tamaño.** Meter el documento en el prompt cuando cabe holgadamente y leer por partes solo cuando no cabe. Ahorraría el 50 % en los documentos pequeños, pero vuelve a tener dos caminos, que el autor descartó (punto 11). Queda anotado para decidirlo con más medidas.

## v0 (08-10-2026)

No sigue a la v2: es un prompt aparte, mucho más simple, para la iteración v0 (D-65). El autor decidió tener el TFG completo de punta a punta (preguntas, catálogo, backlog y mockup) con prompts básicos antes de seguir mejorando este. La v2 queda aplazada, no descartada. Lo propuso Claude (D-73). Está en `prompts/v0/catalogo.md`, junto al prompt del árbitro (`arbitro_catalogo.md`), y el detalle está en `prompts/v0/CHANGELOG.md`.

### Qué hace

- **Entrada:** el documento entero, sin lotes ni índice, y las respuestas del cliente de la fase de preguntas, que prevalecen sobre la documentación. Ya no lleva dudas resueltas vacías: la fase de preguntas existe antes que el catálogo.
- **Salida mínima:** por requisito, `id`, `tipo` (`RF`, `RNF`, `RA`, `RT`, `RI`), `notacion`, `obligatoriedad`, `texto`, `fuentes` (fragmentos) y `preguntas` (respuestas en que se apoya). Sin inventario, criterios, descartes, preguntas abiertas ni autoverificación.
- **El modelo escribe la frase entera**, con las keywords en mayúsculas. Es un paso atrás respecto a D-23 (la plataforma generaba la frase a partir de los slots), aceptado para v0 (D-74).
- **Notación reducida:** EARS (seis patrones) siempre con `shall`; MASTeR solo para cualidades medibles y para `should` y `may`. Sin árbol de decisión.
- **Prioridades:** fidelidad, cobertura y sin repeticiones. Con el documento entero, la repetición entre lotes de la v1.x no puede darse.
- **Árbitro:** une los dos catálogos por significado, dice de qué ids de cada propuesta sale cada requisito, marca las discrepancias que la fuente no resuelve para que las revise el cliente y lista los descartes con su motivo.
- **Validación de la plataforma** (`prompts/v0/validar_v0.py`): esquema, ids, tipo, EARS solo con `shall`, referencias a fragmentos y respuestas y, en el árbitro, que ningún requisito de las propuestas se pierda sin motivo. Su port a C# (`PhaseValidator`, paso 42) da las mismas líneas en el mismo orden en los 38 casos de `prompts/v0/test_v0.py` (los nueve ejemplos y una mutación por código); solo se comparan resumidas las de esquema, porque las dos librerías las redactan distinto. Una salida con errores se repara una vez, con el prompt original y el anexo común (`anexo_reparacion.md`).
- **Tamaño:** 5.808 bytes, frente a 32.217 de la v1.4.

### Resultados

El ejemplo de `prompts/v0/ejemplo/` valida sin errores. Queda la medida del paso 65, con un segundo documento público.

**Primera ejecución completa (09-10-2026).** Documento: `DOC-01.txt` de ReservaSalas (14 fragmentos). A = SOL, B = LUNA, árbitro = SOL; sugerencias de respuesta = SOL. Una sola ronda de preguntas. Cifras del registro de cada run en la plataforma:

| Fase | Run | Tokens entrada / salida | Duración |
|---|---|---|---|
| Preguntas | A (SOL) | 6.971 / 3.691 | 31 s |
| | B (LUNA) | 6.808 / 6.348 | 45 s |
| | Árbitro (SOL) | 11.776 / 2.572 | 23 s |
| Sugerencias de respuesta | SOL | 9.128 / 4.089 | 30 s |
| Catálogo | A (SOL) | 12.781 / 9.371 | 58 s |
| | B (LUNA) | 12.603 / 10.959 | 69 s |
| | Árbitro (SOL) | 24.284 / 6.574 | 36 s |
| Backlog | A (SOL) | 19.207 / 10.068 | 66 s |
| | B (LUNA) | 18.229 / 8.898 | 53 s |
| | Árbitro (SOL) | 40.903 / 9.698 | 51 s |
| **Total** | 10 runs | **162.690 / 72.268** | **unos 5 min y 20 s de modelo** |

- **Fiabilidad:** los 10 runs terminan `Done` al primer intento, en 2 turnos, sin reparaciones ni avisos del validador.
- **Preguntas:** 11, todas pendientes de una cifra o de una decisión que el documento no da. 10 vienen de las dos propuestas y 1 solo de A; el árbitro no descarta ninguna.
- **Respuestas:** las 11 son sugerencias de la IA (todas `suposicion`) aceptadas sin cambios (origen `ia`).
- **Catálogo:** 25 requisitos (21 `RF`, 2 `RNF`, 1 `RI` y 1 `RT`). Los 25 salen de las dos propuestas, sin discrepancias ni descartes. 24 se apoyan en alguna respuesta y 5 no citan ningún fragmento (`RF-003`, `RF-014`, `RF-015`, `RF-017`, `RF-020`). Se validó 78 s después de generarse y sin cambios.
- **Backlog:** 6 épicas, 16 historias, 34 criterios y 41 tareas. Las 16 historias salen de las dos propuestas, sin discrepancias ni descartes. Cubren los 22 requisitos funcionales (`RF` y `RI`), y los `RNF` aparecen como criterios de las historias afectadas. Se validó 2 s después de generarse y sin cambios.

**Lo que muestra.** Hito 5 con la v1.2 y el mismo documento: 13 requisitos. Aquí, 25. Lo que falta en el documento lo rellenaron las suposiciones de la IA, y al aceptarlas sin revisar se convirtieron en requisitos con la misma fuerza que los del cliente:

- **Funcionalidad nueva.** `RF-003` (consultar la disponibilidad) sale de la sugerencia de P-008, que hablaba del tiempo de respuesta de esa consulta; el documento no la menciona. Con P-011, una nota de proyecto («el administrador revisará cada mes las reservas que no se hayan utilizado», F014) se convirtió en un registro de llegada (`RF-020`) y un informe mensual (`RF-021`).
- **Cifras inventadas.** Los 2 s, los 3 s y los 100 usuarios simultáneos de `RNF-001` y `RNF-002`.
- **Alcance ampliado.** La exportación CSV, «deseable» en la fuente (F011), pasa a `shall`, porque la sugerencia de P-009 propuso incluirla en esta versión.

La trazabilidad sí funciona: cada requisito cita las preguntas en que se apoya y cada respuesta guarda su origen `ia`. Pero ni el catálogo ni el editor lo hacen visible.

Esta ejecución no mide la revisión humana (nada se editó) ni la repetición de preguntas entre rondas (hubo una sola ronda).

### Qué hay que mejorar

Se sabe de antemano, porque se ha simplificado a propósito:

- Sin inventario, la cobertura depende de cada modelo y del árbitro; el validador no puede comprobarla.
- Sin citas literales ni slots, la fidelidad y la conformidad a las plantillas solo se revisan a mano.
- Los términos vagos y las cifras inventadas no se detectan automáticamente.

Tras la primera ejecución completa (09-10-2026):

- **Sugerencias de respuesta.** El prompt pide «lo más habitual y prudente», pero la IA propuso funcionalidad nueva (consulta de disponibilidad, registro de llegada) y amplió el alcance (CSV en esta versión). Habría que pedirle la respuesta mínima que resuelve la duda, sin funciones que el documento no menciona, y la opción conservadora en las preguntas de alcance.
- **Origen de las respuestas en el catálogo.** El catálogo trata igual una respuesta del cliente que una suposición de la IA aceptada sin cambios. Opciones: que el editor marque los requisitos que se apoyan en respuestas `ia`, o que el prompt reciba el origen de cada respuesta. Decide el autor.
- **El árbitro no aportó nada medible en este documento:** 100 % de elementos de las dos propuestas, ninguna discrepancia y ningún descarte. Con un documento tan pequeño y respuestas tan concretas, A y B convergen. Hay que comprobarlo con el documento del paso 65.
- **Detalles de forma:**
  - el nombre del sistema se repite en cada requisito tal como está en el proyecto («el sistema ReservaSalasHito911.v2»): conviene un nombre de producto;
  - la compatibilidad con navegadores sale como `RT`, aunque es más bien un `RNF` de portabilidad;
  - algunos criterios técnicos son tautológicos (CA-028: «el sistema se sincroniza… queda sincronizado»).
