# Propuesta para el prompt de requisitos v1.4

Encargo para redactar `prompt_requisitos_v1.4.md` a partir de la v1.3 (`prompts/generacion_requisitos/prompt_requisitos_v1.3.md`), con los resultados de la primera ejecución real de la v1.3. El historial completo y las cifras están en `docs/evolucion-prompt-requisitos.md`.

## Contexto

La v1.3 se probó el 02-10-2026 con la pasarela real: SOL y LUNA, 22 lotes, sobre un documento técnico y funcional interno de 608 fragmentos que describe un sistema ya construido. Es el mismo documento con el que se midió la v1.2. Del documento solo pueden darse cifras agregadas: no se puede citar su contenido.

**Lo que la v1.3 consiguió, y hay que conservar:**
- **Volumen:** un 60–67 % menos de requisitos (SOL de 1.133 a 456; LUNA de 862 a 284).
- **Detalles de implementación en el texto:** del 33 % y el 30 % al 9 % y el 2 %.
- **Reparaciones:** los lotes reparados bajan de 13 a 4 (SOL) y de 17 a 8 (LUNA), y la salida perdida en intentos fallidos, del 46–72 % al 3–10 %.
- **Tokens:** aproximadamente la mitad.
- **Piezas nuevas:** los modelos usan las seis etiquetas de criterio, el inventario y los tipos.

**Lo que no cambia en la v1.4:**
- independencia de los modelos (el prompt no menciona al otro modelo ni al árbitro);
- EARS y MASTeR con slots y render por la plataforma;
- tipos `RF`/`RI`/`RNF`/`RA`/`RT`;
- criterios etiquetados e inventario;
- cobertura antes que detalle (U0);
- formato de salida: **sin cambios en el esquema 1.3**, salvo que una propuesta lo justifique.

**Restricción de tamaño:** el prompt v1.3 ocupa 32 KB (la v1.2, 48 KB) y se paga en cada lote y en cada reparación. La v1.4 no debería crecer; si se añade texto, compénsalo acortando otra parte.

## Propuestas para el prompt

### 1. Comportamiento interno escrito como requisito funcional (prioridad alta)

**Evidencia.**
- SOL saca el 38 % de sus requisitos (171) de las secciones de diseño técnico: arquitectura, backend, catálogo de la API, frontend y despliegue. LUNA, el 17 % (48).
- La mayoría son comportamientos internos que ningún usuario ni sistema externo observa, escritos como `RF`: cabeceras HTTP de seguridad, CORS, sondas de salud, publicación de OpenAPI, migraciones al arrancar, registro de peticiones, limitación de tasa, formato de errores, identificador de correlación.
- La regla `design_detail` de la v1.3 se aplica al detalle del texto (nombres de ficheros, claves, colores), pero no a "el sistema hace X" cuando X es interno.

**Cambio propuesto (§3).**
- Añadir un criterio de **observabilidad**: un requisito describe lo que un usuario, un responsable del negocio o un sistema externo integrado puede observar, usar o exigir.
- Un comportamiento interno de la implementación es `design_detail`, aunque esté escrito como "el sistema hace…". Las excepciones:
  - la fuente lo **impone** como restricción: `RT` o `RA` al nivel en que se impone;
  - lo presenta como **atributo de calidad** exigido (seguridad, disponibilidad, trazabilidad…): `RNF`, uno por atributo, no uno por mecanismo.
- **Usar el índice:** si la sección del fragmento describe cómo está construido el sistema (arquitectura, backend, API, frontend, modelo de datos, despliegue, configuración), parte de que su contenido es `design_detail`, salvo restricciones impuestas o atributos de calidad.
- **"No hagas esto":** un antiejemplo genérico, como "El sistema DEBERÁ enviar cabeceras de seguridad en cada respuesta HTTP" → `design_detail`, o como mucho un `RNF` de seguridad si la fuente lo exige como cualidad.

**Medida esperada:** que baje el porcentaje de requisitos sacados de secciones técnicas, sobre todo en SOL, sin perder funcionalidades del usuario.

### 2. Secciones sobre el proyecto (prioridad media; revisar antes con el autor)

**Evidencia.** El 20 % de los requisitos de SOL y el 29 % de los de LUNA salen de las secciones finales: historia y decisiones, especificación frente a implementación, deuda técnica y riesgos, recetas para desarrolladores y anexos. La v1.3 ya tiene `project_note`, pero se aplica poco fuera de las secciones descartadas enteras.

**Cambio propuesto (§3, descartes).** Precisar que lo que describe **el proyecto, su historia o su código** es `project_note`, aunque mencione funciones del sistema:
- decisiones pasadas;
- errores conocidos;
- deuda;
- riesgos;
- cómo modificar el sistema;
- diferencias entre lo especificado y lo construido.

Solo es requisito si la fuente dice que el sistema **debe** hacer algo que no esté ya en otra parte del documento. En ese caso, una pregunta `scope` o `contradiction` puede ser más fiel que un requisito.

**Antes de redactarlo:** el autor tiene que confirmar si parte de esos requisitos eran legítimos. Por ejemplo, una sección de "especificación frente a implementación" puede contener requisitos que el sistema construido no cumple.

### 3. Nivel del inventario de funcionalidades (prioridad alta)

**Evidencia.**
- SOL escribió 420 funcionalidades para 456 requisitos, y LUNA 272 para 284: el inventario va casi uno a uno con los requisitos.
- Así no sirve para lo que se diseñó: detectar funcionalidades que se pierden entre la lista y la redacción.
- Además, C20 (funcionalidad sin `RF`/`RI`) provocó todas las peticiones de reparación: 4 en SOL y 12 en LUNA.

**Cambio propuesto (§5).** Definir el nivel con un criterio claro:
- una funcionalidad es lo que un usuario o un sistema externo **reconocería como una función del producto** ("reservar una sala", "exportar el histórico de reservas", "gestionar usuarios y roles");
- normalmente corresponde a una pantalla, un proceso de negocio o una integración;
- una funcionalidad suele tener **varios** requisitos (su comportamiento principal, sus variantes por actor o condición, sus atributos de calidad);
- no se crea una funcionalidad por requisito.

Conviene añadir en el ejemplo de §11 una funcionalidad cubierta por dos requisitos.

**Medida esperada:** muchas menos funcionalidades que requisitos y menos reparaciones por C20.

### 4. Descartar un fragmento entero que luego se cita (prioridad media)

**Evidencia.** El aviso C17 aparece 27 veces en SOL y 67 en LUNA: el modelo descarta un fragmento entero (`quote: null`) o la sección que lo contiene, y después lo cita en un requisito.

**Cambio propuesto (§3).** Hacer explícita la regla:
- si de un fragmento sale algún requisito, criterio o pregunta, **no** se descarta entero: se descarta solo la parte del cómo, citándola (`quote` con el trozo), o no se descarta;
- una sección solo se descarta entera si **ninguno** de sus fragmentos se cita en el catálogo.

### 5. Palabra clave repetida al principio de las condiciones (prioridad media)

**Evidencia.** Algunos slots `condition`, `trigger` o `states` empiezan por la palabra que añade la plataforma, y la frase sale con la palabra repetida: "SI si la pantalla tiene menos de…", "SI si el usuario tiene un rol…".

**Cambio propuesto (§6, reglas de los slots).** Una regla nueva, R-h: `feature`, `states`, `trigger` y `condition` no empiezan por la palabra clave que les corresponde ni por sus equivalentes en minúsculas:
- en español: si, cuando, mientras, donde, en cuanto;
- en inglés: if, when, while, where, as soon as, as long as.

La plataforma la añade.

Añadir a "No hagas esto": `condition` = "si la reserva dura…" → «SI si la reserva dura…».

### 6. Modo verbal tras SI (prioridad baja)

**Evidencia.** Aparece "SI el usuario esté autorizado…": la regla R-f pide indicativo tras SI y no se cumple siempre.

**Cambio propuesto:** añadir este caso a "No hagas esto", junto al de R-h. No hace falta texto nuevo de regla.

### 7. Preguntas "¿qué pasa si falla?" (prioridad media)

**Evidencia.** En SOL bajaron de 108 a 37, pero en LUNA subieron de 66 a 77. La regla N4 ("como máximo una por proceso o pantalla") no se aplica igual en los dos modelos.

**Cambio propuesto (§8, N4).**
- Hacerla más operativa: el proceso o pantalla es la **funcionalidad del inventario**, así que hay como máximo una pregunta `missing_error_handling` por funcionalidad, y solo si ninguno de sus requisitos tiene un criterio `[ERROR]`.
- Añadir "ante la duda, no la abras".
- Esto liga N4 con el inventario de la propuesta 3.

### 8. Una sola escritura del catálogo (prioridad baja; a confirmar)

**Evidencia.** Un lote de LUNA agotó los 10 turnos con 90.000 tokens de salida. No sabemos aún si reescribió el catálogo varias veces.

**Cambio propuesto (§10), solo si se confirma:** "Escribe el catálogo completo con **una** llamada a Write. No lo releas ni lo reescribas por partes".

## Cambios en la plataforma (no van en el prompt; los hace Claude Code)

- **Normalizador:** quitar la palabra clave repetida al principio de `condition`, `trigger`, `states` y `feature` (corrección `FIX`), como red de seguridad de la propuesta 5.
- **Validador:** cuando un fragmento descartado entero se cita en un requisito aceptado, quitar el descarte entero (`FIX`) en lugar de solo avisar con C17. Encaja con la propuesta 4.
- **Validador:** decidir si se comprueban los 200 caracteres de cada criterio, y con qué código.
- **Runner:** reintentar una vez un lote perdido por inactividad de la pasarela. En la prueba, tres lotes de LUNA murieron a la vez a los 15 minutos sin ninguna salida, mientras otros intentos de LUNA terminaron en 17 y 23 minutos.
- **Paso 32:** deduplicación por modelo de los requisitos repetidos entre lotes (por ejemplo, la exportación o la gestión de usuarios aparecen en varios lotes).
- **Paso 33:** selección de secciones al lanzar el trabajo. Complementa la propuesta 1: el usuario puede excluir de antemano las secciones de implementación.

Si alguna propuesta del prompt necesita una comprobación automática nueva, hay que indicarlo: el validador de referencia en Python y su port en C# se cambian a la vez.

## Qué se pide como entrega

1. **`prompt_requisitos_v1.4.md`**, con los cambios anteriores y sin crecer respecto a la v1.3.
2. **`anexo_reparacion_v1.4.md`**, solo si cambia algo del anexo (por ejemplo, si C20 deja de reparar).
3. **La entrada v1.4 del `CHANGELOG.md`:** qué cambia y por qué, enlazado con la evidencia de arriba.
4. **Si se toca el esquema**, decirlo de forma explícita: hoy la propuesta no lo necesita.

## Cómo se medirá

- **Documentos y repeticiones:** el mismo documento interno, ReservaSalas y 1 o 2 documentos públicos de PURE, con dos ejecuciones por versión si el coste lo permite.
- **Métricas:** las de `docs/evolucion-prompt-requisitos.md` (requisitos por tipo y por origen, funcionalidades por requisito, avisos C17, preguntas por tipo, reparaciones, tokens).
- **Comprobación manual de cobertura:** que no se pierdan funcionalidades del usuario, con una lista de referencia hecha a mano.
