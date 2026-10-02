# Evolución del prompt de generación de requisitos

Historial de las versiones del prompt que genera el catálogo de requisitos (EARS/MASTeR), con lo que hacía cada una, los resultados que obtuvo y lo que había que mejorar. Es material para la memoria del TFG: cada versión responde a los problemas medidos en la anterior.

El detalle técnico de cada cambio está en `prompts/generacion_requisitos/CHANGELOG.md`; aquí se explica el porqué y el efecto.

## Resumen

| Versión | Fecha | Idea principal | Medida con modelos | Resultado clave |
|---|---|---|---|---|
| v1 | 28-09-2026 | Primer prompt EARS/MASTeR, respuesta en JSON | No | El validador dejaba pasar 16 tipos de error |
| v1.1 | 29-09-2026 | Contrato con la plataforma: fragmentos, `Write`, veredicto, árbol de decisión | No | Base para ejecutar con modelos |
| v1.2 | 29-09-2026 | Reglas más precisas (obligatoriedad, cifras, citas) y ejemplo en inglés | Sí | Perfecto en un documento pequeño; inservible en uno grande |
| v1.3 | 02-10-2026 | Independencia, cobertura, un requisito por capacidad, tipos, reparación parcial | Pendiente | Pendiente |

## Cómo se mide

Cada versión se mide con el validador determinista de la plataforma (`validar_catalogo.py` y su port en C#), que no usa ningún modelo:

- **Calidad:** proporción de requisitos sin errores de sintaxis, redacción, decisión, trazabilidad y fidelidad de cifras; citas literales; cobertura de fragmentos.
- **Volumen:** requisitos, preguntas abiertas, descartes, y desde la v1.3 requisitos por tipo, funcionalidades del inventario y criterios por etiqueta.
- **Coste:** tokens de entrada y salida, lotes reparados, salida gastada en intentos fallidos y duración.
- **Comparación entre modelos:** cuántos requisitos de SOL (A) y LUNA (B) se parecen.

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
| Requisitos con detalles de implementación o apariencia | 42 % | 36 % |
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
- **Coincidencia entre A y B:** con una similitud de palabras del 60 % solo se emparejaron 402 requisitos.
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

Pendiente: ejecución en curso con el documento técnico interno, y ReservaSalas.

| | SOL (A) | LUNA (B) |
|---|---|---|
| Lotes en el catálogo | | |
| Requisitos (por tipo) | | |
| Funcionalidades y su cobertura | | |
| Criterios por requisito | | |
| Requisitos de secciones que no son especificación | | |
| Preguntas abiertas | | |
| Requisitos rechazados | | |
| Lotes reparados | | |
| Salida gastada en intentos que fallaron | | |
| Tokens entrada / salida | | |
| Duración | | |
| Coincidencia entre A y B | | |

### Qué hay que mejorar

Pendiente de los resultados. Lo que ya se sabe:

- **Duplicados entre lotes** de un mismo modelo: cada lote no ve a los demás (paso 32).
- **Los 200 caracteres** de cada criterio no los comprueba ningún código.
- **La web** todavía no muestra criterios, inventario ni requisitos rechazados (paso 33).

---

## Cómo mantener este documento

Cada vez que cambie el prompt de requisitos (su texto, su anexo, su esquema o el validador que lo acompaña) o se mida una versión:

1. **Versión nueva:** una sección con **Qué hace**, **Resultados** y **Qué hay que mejorar**, y una fila en el resumen.
2. **Resultados:** las mismas métricas de "Cómo se mide", con fecha, documento, modelos y si fue una sola ejecución o varias.
3. **Lo que había que mejorar en la versión anterior** se enlaza con lo que cambia en la nueva.
4. **Documentos internos:** solo cifras agregadas y anonimizadas.
