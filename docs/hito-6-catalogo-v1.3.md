# Hito 6: catálogo v1.3 (menos requisitos, menos reparaciones, menos tokens)

Cubre los pasos 29 a 34 de `docs/plan-de-construccion.md`. Léelo junto con `CLAUDE.md` y `docs/hito-5-catalogo.md`. Si algo choca con ellos, dilo antes de aplicarlo.

## Por qué

La primera ejecución grande del hito 5 (un documento técnico y funcional de 608 fragmentos, 22 lotes) dio catálogos válidos pero inútiles en la práctica:

| | SOL | LUNA |
|---|---|---|
| Requisitos | 1.133 | 862 |
| Preguntas abiertas | 271 | 154 |
| Requisitos de secciones que no son especificación (diseño técnico, historia, deuda, recetas) | 53 % | 54 % |
| Lotes que necesitaron reparación | 13 de 22 | 17 de 22 |
| Salida gastada en intentos que acabaron fallando | 46 % | 72 % |
| Tokens (entrada + salida) | 3,1 M | 4,1 M |

Las causas, por orden de peso:

1. El prompt no tiene forma de decir "esto no es un requisito" para los detalles de implementación ni para lo que habla del proyecto en lugar del sistema.
2. La unidad de requisito es demasiado fina: cada acción, cada valor por defecto y cada cifra de interfaz es un requisito.
3. El validador trata como error, y por tanto como reparación del lote entero, cosas que la plataforma puede corregir sola (recuentos, numeración, vínculos, comillas en las citas).
4. Una pregunta `missing_error_handling` por cada operación que puede fallar.
5. El prompt (48 KB) y campos de salida que no usa nadie se pagan en cada lote y en cada reparación.

Además, el prompt v1.2 empuja a los dos modelos a coincidir ("garantizan que ambos modelos corten igual"), y eso va contra el objetivo del TFG: comparar dos catálogos **independientes**.

## Decisiones del autor (1-10-2026)

- **Independencia.** El prompt no menciona al otro modelo ni al árbitro. Cada modelo recibe las mismas pautas y construye su catálogo como si fuera el único. El árbitro se diseñará en consecuencia (emparejamiento de varios a varios).
- **EARS y MASTeR se mantienen,** con los slots y el texto generado por la plataforma (D-23).
- **Tipos de requisito, con id por tipo:**

  | Código | Tipo | Criterio |
  |---|---|---|
  | `RA` | Arquitectura | Requisitos generales de la arquitectura y del entorno (entorno web, navegadores, plataforma). |
  | `RT` | Técnico | Tecnología impuesta (usar ASP.NET Core y EF Core con SQL Server). |
  | `RF` | Funcional | Lo que el sistema hace para sus usuarios. |
  | `RI` | Integración | Sistemas externos, APIs, protocolos, frecuencia, responsables y estado de la decisión. |
  | `RNF` | No funcional | Atributos de calidad: rendimiento, seguridad, disponibilidad, trazabilidad, escalabilidad, accesibilidad, privacidad, mantenibilidad, portabilidad. |

  Ids: `RF-001`, `RNF-001`, `RA-001`… Cada tipo numera por separado.
- **Prioridad:** fuera de esta versión.
- **Reparaciones:** se aplican las medidas propuestas (normalizador, aceptación parcial, menos salida).
- **Deduplicación por modelo** tras unir los lotes, antes del árbitro.
- **Preguntas:** agrupar y limitar.
- **Objetivo general:** menos requisitos sin perder mucha información y muchos menos tokens. El resto del diseño lo decide la implementación (este documento).

## Decisiones del autor (2-10-2026): cobertura primero

- **Prioridad.** Que no falte ninguna funcionalidad pesa más que el detalle de cada una. Toda funcionalidad de la fuente debe quedar, como mínimo, explicada en un requisito, de forma que se pueda implementar. Ante la duda entre funcionalidad propia y detalle de otra, requisito propio.
- **Errores.** Un error o rechazo que describe la fuente va como criterio `[ERROR]` de la funcionalidad a la que afecta, no como requisito propio. Solo es un requisito propio si la reacción es en sí otra funcionalidad (por ejemplo, apuntar al empleado en una lista de espera y avisarle cuando la sala quede libre). Para los errores, sustituye a la práctica de la v1.2 de un requisito `unwanted_behaviour` por error.
- **Inventario de funcionalidades.** Cada modelo escribe, antes de sus requisitos, la lista de funcionalidades que encuentra en el lote (`capabilities`), y cada requisito indica cuáles cubre. Sirve para que **su propio** catálogo no se deje ninguna: la plataforma comprueba que cada funcionalidad del inventario acaba en algún requisito. Se mantiene la independencia del 1-10-2026: el prompt no menciona al otro modelo ni al árbitro, y comparar inventarios, si se hace, es cosa del árbitro.
- **Criterios etiquetados.** Cada criterio de aceptación empieza por una etiqueta de su dimensión (`[DATA]`, `[RULE]`…). Cuesta unos pocos tokens por criterio y permite medir el detalle de cada funcionalidad.
- **Funcionalidad sin requisito = reparación.** Si una funcionalidad del inventario se queda sin ningún requisito aceptado (C20), el lote se repara, igual que si supera el 20 % de rechazos.
- **Verbos paraguas** (decisión delegada en Claude): las operaciones agrupadas por U2 se nombran en la respuesta cuando son cuatro o menos; un verbo como "gestionar" solo se admite con más de cuatro y con un criterio `[OPERATION]` por operación (U7).

Origen: propuestas de Claude en la conversación del 2-10-2026, aceptadas por el autor. Las dimensiones de las etiquetas se inspiran en el metamodelo de MASTeR (actor, verbo de proceso, objeto, detalles y tipos de condición; Großer et al. 2024). La investigación compara la completitud de la **redacción** de cada requisito; no trata la cobertura de funcionalidades del catálogo, que depende del procedimiento de extracción y no de la notación.

## Qué no cambia

- Notaciones EARS (seis patrones) y MASTeR (Functional, Property y Condition), con el árbol de decisión D1–D5.
- Slots y render por la plataforma (D-23); el modelo no escribe la frase del requisito.
- Trazabilidad: cada requisito cita fragmentos con trozos literales.
- Fragmentos con id estable y lotes de secciones completas (D-24).
- Veredicto final `VEREDICTO:` y escritura de `output/catalogo.json` con Write.
- Los scripts de Python siguen siendo la referencia del validador y del fragmentador; el C# se porta de ellos.

## Cambios en el prompt (v1.3)

### Independencia

- ROL: se quitan las frases sobre el otro modelo, el árbitro y la comparación. Queda: experto en EARS y MASTeR, sin interacción, catálogo fiel, trazable y conforme a las plantillas.
- §6: se quita "garantizan que ambos modelos corten igual". Las reglas de granularidad existen para que el catálogo sea útil y revisable, no para coincidir con nadie.

### Qué es un requisito (alcance)

La documentación puede ser una especificación de un sistema por construir o la descripción de uno ya construido. En los dos casos se extrae **qué debe hacer o cumplir el sistema**, nunca **cómo está construido**. Dos motivos de descarte nuevos:

- `design_detail`: cómo está hecho el sistema. Nombres de ficheros, clases, componentes, tablas, columnas, claves de configuración o de almacenamiento, parámetros internos, versiones de librerías, estructura del código, estilos visuales concretos (colores, tamaños, animaciones).
- `project_note`: lo que habla del proyecto y no del sistema. Historia, decisiones pasadas, deuda técnica, errores conocidos, riesgos, pruebas, despliegue, proceso de trabajo, guías para desarrolladores.

Las restricciones tecnológicas o de arquitectura **impuestas** sí son requisitos (`RT`, `RA`), pero solo al nivel en que se imponen ("aplicación web", "ASP.NET Core con SQL Server"), no cada detalle de cómo se usan.

**Descartes por sección.** Una sección entera que no contiene requisitos se descarta con una sola entrada `{ "section": "§16", "reason": "project_note" }` en lugar de una entrada por fragmento. Ahorra mucha salida en documentos técnicos.

Para no perder funcionalidades por un descarte: solo se descarta entera una sección del propio lote, y solo si ningún fragmento describe algo que el sistema haga para sus usuarios o para otros sistemas. Si un fragmento mezcla cómo está hecho el sistema con lo que hace, se descarta el cómo y se extrae el qué.

**Índice del documento.** Cada lote recibe un hueco nuevo, `{{INDICE}}`, con los títulos de todas las secciones del documento (no solo las del lote), para que el modelo sepa en qué parte está ("Deuda técnica", "Catálogo de la API"…).

### Unidad de requisito: capacidad con criterios de aceptación

Sustituye a G1–G4 y G6:

- **U0.** Cobertura antes que detalle: cada funcionalidad de la fuente queda en al menos un requisito. Ante la duda entre funcionalidad propia y detalle de otra, requisito propio. Una funcionalidad descrita a medias se escribe igualmente con lo que dice la fuente, y lo que falta va en una pregunta abierta.
- **U1.** Un requisito expresa **una** capacidad, comportamiento, restricción o atributo de calidad del sistema, con **una** respuesta principal (EARS) o **una** propiedad (MASTeR).
- **U2.** Las acciones del mismo actor sobre el mismo objeto bajo la misma condición forman **un** requisito ("permitir al coordinador crear, editar y cancelar las actividades" es un requisito, no tres).
- **U3.** Los detalles de un requisito van en `acceptance_criteria`, no como requisitos propios: campos, valores por defecto, límites, formatos, orden, mensajes, textos de interfaz y cada una de las acciones de U2.
- **U4.** Una cifra asociada a un comportamiento es un criterio. Solo es un requisito `RNF` propio si es un atributo de calidad del sistema (rendimiento, disponibilidad, capacidad, seguridad…).
- **U5.** Se mantienen G5 (lo mismo en varios fragmentos = un requisito con varias citas) y la separación de condiciones alternativas que EARS no admite en un solo disparador.
- **U6.** Comportamientos de actores distintos, o condiciones que cambian la respuesta, son requisitos distintos. Excepción: los errores y rechazos que describe la fuente son criterios `[ERROR]` de su funcionalidad, salvo que la reacción sea en sí otra funcionalidad.
- **U7. Sin verbos paraguas.** Con cuatro operaciones o menos, la respuesta las nombra todas (`response` en EARS; `process_verb` en MASTeR, que admite varios infinitivos coordinados: "crear, editar y cancelar"). Con más de cuatro, se admite un verbo paraguas ("gestionar", "administrar", "mantener", "manejar" / "manage", "administer", "maintain", "handle") y cada operación va en un criterio `[OPERATION]`. Motivo: la frase del requisito es lo que se lee, lo que compara el árbitro y de donde saldrá el backlog; "gestionar las actividades" no dice si incluye eliminar, y dos catálogos con la misma frase pueden cubrir operaciones distintas sin que se note.

Criterios de aceptación:

- Frases cortas y verificables (máximo 200 caracteres cada una), en el idioma del catálogo, en texto libre: **no** llevan plantilla EARS ni MASTeR.
- Sin términos vagos (C5) y con cifras copiadas de la fuente (N2), como los slots.
- Salen de los fragmentos que cita el requisito; si un criterio sale de otro fragmento, ese fragmento se añade a `source_trace`.
- Un requisito puede no tener criterios.
- Cada criterio empieza por una etiqueta y un espacio:
  - `[OPERATION]`: una de las acciones agrupadas por U2. Obligatorio, una por operación, cuando la respuesta usa un verbo paraguas (U7); si la respuesta ya las nombra, solo hace falta para el detalle propio de una operación.
  - `[DATA]`: campos, datos de entrada o salida, valores por defecto y formatos.
  - `[RULE]`: reglas de negocio, orden o cálculos que no cambian la respuesta principal.
  - `[LIMIT]`: cifras, máximos, mínimos y plazos de un comportamiento (U4).
  - `[MESSAGE]`: mensajes, textos de interfaz y avisos al usuario.
  - `[ERROR]`: errores o rechazos que describe la fuente y cómo reacciona el sistema.

  Las etiquetas son códigos fijos en inglés, sea cual sea el idioma del catálogo (convención de identificadores de `CLAUDE.md`; la web las traduce). Los 200 caracteres se cuentan sin la etiqueta.
- Las etiquetas son también la lista de comprobación al escribir cada funcionalidad: qué operaciones tiene, con qué datos, reglas y límites, qué mensajes da y qué errores describe la fuente. Una dimensión que la fuente no trata se deja sin criterio; no se inventa.

### Inventario de funcionalidades (`capabilities`)

Antes de los requisitos, el modelo escribe en el catálogo la lista de funcionalidades que encuentra en el lote: lo que el sistema hace para un actor (usuario u otro sistema) o por sí solo. Una entrada por funcionalidad, al nivel de U2 (actor, objeto y condición), no una por acción.

```json
"capabilities": [
  { "id": "CAP-001", "name": "reservar una sala", "actor": "el empleado", "fragments": ["DOC-01#F002", "DOC-01#F004"] }
]
```

- `name`: verbo en infinitivo y objeto, como máximo 80 caracteres, en el idioma del catálogo.
- `actor`: quien usa la funcionalidad; `null` si el sistema actúa solo.
- `fragments`: los fragmentos donde aparece.
- Va **antes** de `requirements` en el JSON: el modelo genera en orden, así que primero hace la lista y después redacta.
- Cada requisito `RF` o `RI` lleva `capability_ids` con al menos una funcionalidad. Los `RNF`, `RA` y `RT` pueden llevarla (por ejemplo, el tiempo de respuesta de una funcionalidad concreta) o dejarla vacía.
- Lo que la fuente presenta como futuro o fuera de alcance no entra en el inventario: va como pregunta `scope`.
- El prompt presenta el inventario como un paso para que el catálogo no se deje ninguna funcionalidad. No menciona al otro modelo ni al árbitro.
- Coste estimado: 20–30 tokens de salida por funcionalidad. Se mide en el paso 34.

### Tipo, notación y árbol de decisión

El campo `type` sustituye a `kind` y a `out_of_subset`. El árbol D1–D5 se mantiene y queda ligado al tipo:

| Rama | Tipo | Notación |
|---|---|---|
| D1 atributo de calidad | `RNF` | MASTeR `property` (valor `[POR DEFINIR]` / `[TBD]` si la fuente no lo da) |
| D2 entorno, plataforma, tecnología o normativa | `RA` o `RT` (normativa: `RNF`) | EARS `ubiquitous` si es SHALL; MASTeR `functional` autónomo si es SHOULD/MAY |
| D3 SHOULD o MAY | `RF`, `RI` o `RNF` | MASTeR `functional` |
| D4 condición sobre datos o varios disparadores | `RF`, `RI` o `RNF` | MASTeR `functional` con condición |
| D5 resto | `RF`, `RI` o `RNF` | EARS |

`RI` usa `master_activity: "interface"` ("SER CAPAZ DE") cuando el sistema actúa a petición de otro sistema.

`RNF` en D3–D5 (prompt v1.3, 2-10-2026): un atributo de calidad expresado como comportamiento ("cifrar las contraseñas", "registrar cada acceso") sigue el árbol desde D2 con tipo `RNF`. Sin esto, un requisito de seguridad o de trazabilidad tendría que clasificarse como `RF`. C12 lo admite.

### Preguntas abiertas

- **N4 nueva.** Como máximo **una** pregunta `missing_error_handling` por proceso o pantalla (no por requisito), vinculada a todos sus requisitos, y solo si la fuente no describe ningún error de ese proceso (ningún criterio `[ERROR]`).
- **Regla general.** Solo se formulan preguntas cuya respuesta cambiaría o completaría un requisito. Las que comparten tema se agrupan en una.
- Los vínculos requisito ↔ pregunta los completa la plataforma (basta con declararlos en un lado).

### Salida más corta

Se quitan del requisito: `kind`, `notation_rationale`, `escalation_reason` (se deduce de la rama), `out_of_subset` (lo dice el tipo). Se quita `verdict.counts` (los recuentos los calcula la plataforma). Se añaden `type`, `acceptance_criteria`, `capabilities` y `capability_ids`.

### Prompt más corto

- §11: un único ejemplo compacto por caso (RF en EARS con criterios, RNF en MASTeR, RT, RI y un descarte por sección), en lugar de los ejemplos y antiejemplos actuales.
- Listas de señales y términos vagos en forma compacta.
- Objetivo: la mitad de tamaño (unos 25 KB).

## Formato de salida (esquema 1.3)

```json
{
  "schema_version": "1.3",
  "system": "el sistema ReservaSalas",
  "language": "es",
  "capabilities": [
    { "id": "CAP-001", "name": "reservar una sala", "actor": "el empleado", "fragments": ["DOC-01#F002", "DOC-01#F004"] }
  ],
  "requirements": [
    {
      "id": "RF-001",
      "type": "RF",
      "capability_ids": ["CAP-001"],
      "source_trace": [
        { "fragment": "DOC-01#F002", "quote": "El empleado puede reservar una sala indicando fecha, hora de inicio, hora de fin y número de asistentes" },
        { "fragment": "DOC-01#F004", "quote": "Si la sala ya está reservada en esa franja, se rechaza la reserva y se informa al empleado" }
      ],
      "obligation": "SHALL",
      "obligation_basis": "implicit",
      "obligation_evidence": null,
      "decision_branch": "D5",
      "notation": "EARS",
      "ears_pattern": "ubiquitous",
      "master_template": null,
      "master_activity": null,
      "master_condition": null,
      "escalation_fallback": null,
      "slots": { "system": "el sistema ReservaSalas", "feature": null, "states": [], "trigger": null, "trigger_keyword": null, "response": "permitir al empleado reservar una sala" },
      "acceptance_criteria": [
        "[DATA] El empleado indica la fecha, la hora de inicio, la hora de fin y el número de asistentes.",
        "[ERROR] Si la sala ya está reservada en esa franja, se rechaza la reserva y se informa al empleado."
      ],
      "origin": "explicit",
      "derived_from": [],
      "open_question_ids": []
    }
  ],
  "open_questions": [],
  "discarded_statements": [{ "fragment": "DOC-01#F001", "quote": null, "reason": "business_goal", "note": "…" }],
  "discarded_sections": [{ "section": "§16", "reason": "project_note" }],
  "domain_terms": [],
  "verdict": { "status": "complete", "self_check": { "…": true }, "issues": [], "unprocessed_fragments": [] }
}
```

`reason` admite además `design_detail` y `project_note`. En `discarded_sections`, `reason` admite `context`, `heading`, `out_of_scope`, `design_detail` y `project_note`. `verdict.self_check` añade `all_capabilities_covered` a las claves de la v1.2 (`quotes_are_literal`, `figures_from_source`, `all_fragments_accounted`, `no_vague_terms`, `ears_only_shall`, `decision_tree_consistent`). El error de la sala ocupada (F004), que en la v1.2 era un requisito `unwanted_behaviour` propio, es ahora un criterio `[ERROR]` de la funcionalidad de reservar.

## Normalizador (nuevo, antes de validar)

Corrige lo que es mecánico, en lugar de pedírselo al modelo. Cada corrección queda en el informe como aviso `FIX`, para medir cuántas hace.

- **Ids:** renumera por tipo, en el orden de aparición (`RF-001`, `RF-002`…; `RNF-001`…), y reescribe todas las referencias. Las preguntas, `Q-001`…
- **Vínculos:** completa el sentido que falte entre requisitos y preguntas. Una referencia a un id que no existe se quita, con aviso.
- **Slots:** quita espacios y la puntuación final (`.,;:`). `slots.system` se iguala al nombre del sistema (antes, error C3).
- **Citas:** compara sin marcas de Markdown (`` ` ``, `*`, `_`), con comillas y guiones tipográficos unificados, espacios colapsados y sin distinguir mayúsculas. Si la cita no está en el fragmento citado pero sí, literal, en **un único** fragmento del lote, corrige el id del fragmento. Las entradas repetidas de `source_trace` se quitan.
- **Funcionalidades:** renumera `CAP-001`… en orden de aparición y reescribe `capability_ids`. Una referencia a una funcionalidad que no existe se quita, con aviso; un fragmento de `fragments` que no está en el lote, también.
- **Etiquetas de criterios:** unifica mayúsculas, corchetes y espacios (`[data]`, `DATA:` → `[DATA] `).
- **Veredicto:** los recuentos ya no los escribe el modelo; la plataforma los calcula para las métricas.

## Validador: errores por requisito y aceptación parcial

Tres niveles:

| Nivel | Qué | Efecto |
|---|---|---|
| Error del lote | JSON no válido, estructura raíz que no cumple el esquema, `Write` cortado, más del 10 % de los fragmentos del lote sin tratar (C10) | Reparación |
| Error de un elemento | Esquema del requisito o pregunta, C2, C5, C6 (tras normalizar), C7, C12, C15, N2, N3 | Ese requisito o pregunta se **rechaza**: sale del catálogo y queda en el informe con sus errores |
| Aviso | C13, C14, C16, C17, C19, C20, C21, C22, C9 y C11 corregidos, `FIX`, autoverificación que no coincide | Nada |

- **Aceptación parcial:** el lote se acepta si los requisitos rechazados son como mucho el **20 %** y no hay C20. Si no, se repara.
- C18 (orden de claves) desaparece.
- C4 (posible segundo verbo coordinado) desaparece: U7 pide enumerar las operaciones y el aviso saltaría siempre.
- Comprobaciones nuevas, todas como **aviso**, para que un fallo de forma no saque una funcionalidad del catálogo:
  - **C19:** criterio sin etiqueta válida tras normalizar.
  - **C20:** funcionalidad del inventario sin ningún requisito `RF` o `RI` aceptado que la cubra (porque el modelo no lo escribió o porque se rechazó). Un `RNF`, `RA` o `RT` que la cite no basta: la funcionalidad tiene que estar explicada.
  - **C21:** requisito `RF` o `RI` sin `capability_ids`.
  - **C22:** `response` o `process_verb` empieza por un verbo paraguas de la lista de U7 y el requisito tiene menos de cinco criterios `[OPERATION]` (o ninguno). Es un aviso porque la lista puede dar falsos positivos ("mantener la sesión abierta").
- **C20 provoca reparación** (decisión del autor, 2-10-2026), aunque sea un aviso: es la única forma de recuperar una funcionalidad perdida dentro de la misma ejecución, y con una sola reparación por lote el coste queda acotado. Si tras la reparación sigue habiendo C20, el lote se acepta con el aviso: perder el lote entero sería peor.
- El esquema JSON no debe convertir estos avisos en rechazos: admite `capability_ids` vacío (C21) y criterios sin etiqueta (C19); son el validador y el normalizador los que los tratan.
- Las métricas se calculan sobre el catálogo aceptado e incluyen los requisitos rechazados, las correcciones `FIX`, el recuento por tipo y los criterios por requisito.
- Las métricas añaden la cobertura del inventario (funcionalidades con al menos un `RF` o `RI` aceptado / funcionalidades del inventario) y los criterios por etiqueta.

## Reparación

- Solo por tres motivos: errores del lote, más del 20 % de requisitos rechazados o C20 (una funcionalidad del inventario sin requisito aceptado).
- Como máximo **una** reparación por lote y modelo (configurable; antes, dos). Se mide si hace falta más.
- El anexo lleva el catálogo previo y solo los errores que motivan la reparación.

## Plataforma

- **Unión por tipo:** se renumera cada tipo por separado (`RF-`, `RNF-`, `RA-`, `RT-`, `RI-`), `Q-` y `CAP-`.
- **Deduplicación por modelo,** tras unir los lotes:
  - **Automática** solo si son iguales: mismo tipo, misma notación y patrón, y slots iguales tras normalizar. Se funden en uno (citas, criterios y preguntas unidos).
  - **Funcionalidades:** se funden las de lotes distintos con el mismo `name` y `actor` tras normalizar (fragmentos unidos y referencias reescritas); las parecidas se señalan como aviso.
  - **Casi duplicados:** se señalan como aviso, sin fundir. Fundirlos bien exige similitud semántica, que depende del origen de los embeddings (decisión abierta).
- **Selección de secciones** al lanzar un trabajo: la web muestra los títulos de primer nivel y el usuario desmarca los que no son especificación. Los fragmentos de esas secciones no se envían a ningún modelo.
- **Lotes:** `OutputRatio` y `MaxOutputTokensPerLot` se recalibran con las mediciones del paso 34. Menos requisitos por fragmento permiten lotes mayores y repetir menos veces el prompt.
- **Web:** tipo, id por tipo, criterios de aceptación con su etiqueta, requisitos rechazados, correcciones `FIX` e inventario de funcionalidades con su cobertura (las no cubiertas, destacadas).

## Consecuencias para el árbitro (hito posterior)

- Los dos catálogos pueden tener granularidad distinta: el emparejamiento es de **varios a varios** (un requisito de A puede corresponder a dos de B).
- El árbitro recibe los catálogos ya deduplicados por modelo y los requisitos rechazados aparte.
- Cada catálogo trae su inventario de funcionalidades. Si el árbitro lo usa (por ejemplo, para emparejar primero por funcionalidad y después por requisito), se decide en su hito; los modelos no lo saben.

## Pasos

- **29.** Esta especificación, revisada por el autor.
- **30.** Referencia en Python:
  - esquema 1.3, con `capabilities`, `capability_ids` y criterios etiquetados;
  - prompt y anexo v1.3, redactados en el proyecto de Claude (D-15) y entregados en la carpeta antes de este paso;
  - normalizador, validador con niveles, aceptación parcial y C19–C22, y render;
  - `fragmentar.py` con el bloque de índice: un título por línea, con su etiqueta de sección delante (`§2.1 Gestión de reservas`), para todas las secciones del documento;
  - los dos ejemplos reescritos en 1.3;
  - mutaciones nuevas y CHANGELOG.

  La CI de los scripts, en verde.
- **31.** Port a C#:
  - normalizador, validador, render y `PromptBuilder` (índice);
  - revisión con aceptación parcial y una reparación;
  - unión por tipo, `FakeCatalog` y paridad con las mutaciones nuevas.
- **32.** Deduplicación por modelo de requisitos y funcionalidades: fusión de iguales y aviso de casi duplicados.
- **33.** Web: selección de secciones al lanzar el trabajo; tipo, criterios con etiqueta, rechazados, correcciones e inventario con su cobertura en la vista del catálogo.
- **34.** Medición de v1.2 frente a v1.3 con ReservaSalas, LibraryLoan, 1 o 2 documentos de PURE y el documento técnico del hito 5 (en privado).
  - Qué se mide: requisitos por tipo, preguntas, rechazos, correcciones, reparaciones, tokens, duración y coincidencia entre A y B.
  - Cobertura de funcionalidades de A, de B y de A∪B frente a una lista de referencia hecha a mano para cada documento, con un criterio escrito de qué cuenta como funcionalidad. Para la memoria conviene que la lista la revise un segundo anotador.
  - Coste del inventario y de las etiquetas en tokens de salida.
  - Con esas cifras se recalibran los lotes.

## Riesgos

- **Menos información.** Agrupar en criterios puede ocultar detalles que antes eran requisitos. La medición del paso 34 compara la cobertura de fragmentos y una muestra manual. El inventario y las etiquetas lo mitigan en parte.
- **Inventario incompleto.** Si el modelo no ve una funcionalidad, falta a la vez en el inventario y en los requisitos: el inventario solo detecta las que se pierden entre la lista y la redacción, o por rechazo. Lo que el modelo no ve lo cubren la trazabilidad por fragmentos (C10) y, más adelante, el árbitro.
- **Aceptación parcial.** Un modelo que falle mucho en un tipo de requisito lo perdería en silencio. Por eso los rechazados se muestran siempre.
- **Coste del cambio.** El esquema 1.3 invalida las fixtures y los ejemplos 1.2. Se regeneran desde Python.

## Copiar a `CLAUDE.md` (cuando el autor apruebe esta especificación)

- Catálogo v1.3, cobertura primero (2-10-2026): que no falte ninguna funcionalidad pesa más que el detalle. Cada modelo escribe su propio inventario de funcionalidades (`capabilities`) antes de los requisitos, y cada `RF`/`RI` indica cuáles cubre (`capability_ids`). Los errores que describe la fuente son criterios `[ERROR]` de su funcionalidad, no requisitos propios. Los criterios llevan etiqueta: `OPERATION`, `DATA`, `RULE`, `LIMIT`, `MESSAGE`, `ERROR`. Una funcionalidad del inventario sin requisito aceptado (C20) provoca la reparación del lote. Sin verbos paraguas con cuatro operaciones o menos (U7).
