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

**Índice del documento.** Cada lote recibe un hueco nuevo, `{{INDICE}}`, con los títulos de todas las secciones del documento (no solo las del lote), para que el modelo sepa en qué parte está ("Deuda técnica", "Catálogo de la API"…).

### Unidad de requisito: capacidad con criterios de aceptación

Sustituye a G1–G4 y G6:

- **U1.** Un requisito expresa **una** capacidad, comportamiento, restricción o atributo de calidad del sistema, con **una** respuesta principal (EARS) o **una** propiedad (MASTeR).
- **U2.** Las acciones del mismo actor sobre el mismo objeto bajo la misma condición forman **un** requisito ("gestionar las actividades": crear, editar, cancelar y asignar fecha).
- **U3.** Los detalles de un requisito van en `acceptance_criteria`, no como requisitos propios: campos, valores por defecto, límites, formatos, orden, mensajes, textos de interfaz y cada una de las acciones de U2.
- **U4.** Una cifra asociada a un comportamiento es un criterio. Solo es un requisito `RNF` propio si es un atributo de calidad del sistema (rendimiento, disponibilidad, capacidad, seguridad…).
- **U5.** Se mantienen G5 (lo mismo en varios fragmentos = un requisito con varias citas) y la separación de condiciones alternativas que EARS no admite en un solo disparador.
- **U6.** Comportamientos de actores distintos, o condiciones que cambian la respuesta, son requisitos distintos.

Criterios de aceptación:

- Frases cortas y verificables (máximo 200 caracteres cada una), en el idioma del catálogo, en texto libre: **no** llevan plantilla EARS ni MASTeR.
- Sin términos vagos (C5) y con cifras copiadas de la fuente (N2), como los slots.
- Salen de los fragmentos que cita el requisito; si un criterio sale de otro fragmento, ese fragmento se añade a `source_trace`.
- Un requisito puede no tener criterios.

### Tipo, notación y árbol de decisión

El campo `type` sustituye a `kind` y a `out_of_subset`. El árbol D1–D5 se mantiene y queda ligado al tipo:

| Rama | Tipo | Notación |
|---|---|---|
| D1 atributo de calidad | `RNF` | MASTeR `property` (valor `[POR DEFINIR]` / `[TBD]` si la fuente no lo da) |
| D2 entorno, plataforma, tecnología o normativa | `RA` o `RT` (normativa: `RNF`) | EARS `ubiquitous` si es SHALL; MASTeR `functional` autónomo si es SHOULD/MAY |
| D3 SHOULD o MAY | `RF` o `RI` | MASTeR `functional` |
| D4 condición sobre datos o varios disparadores | `RF` o `RI` | MASTeR `functional` con condición |
| D5 resto | `RF` o `RI` | EARS |

`RI` usa `master_activity: "interface"` ("SER CAPAZ DE") cuando el sistema actúa a petición de otro sistema.

### Preguntas abiertas

- **N4 nueva.** Como máximo **una** pregunta `missing_error_handling` por proceso o pantalla (no por requisito), vinculada a todos sus requisitos, y solo si la fuente no describe ningún error de ese proceso.
- **Regla general.** Solo se formulan preguntas cuya respuesta cambiaría o completaría un requisito. Las que comparten tema se agrupan en una.
- Los vínculos requisito ↔ pregunta los completa la plataforma (basta con declararlos en un lado).

### Salida más corta

Se quitan del requisito: `kind`, `notation_rationale`, `escalation_reason` (se deduce de la rama), `out_of_subset` (lo dice el tipo). Se quita `verdict.counts` (los recuentos los calcula la plataforma). Se añaden `type` y `acceptance_criteria`.

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
  "requirements": [
    {
      "id": "RF-003",
      "type": "RF",
      "source_trace": [{ "fragment": "DOC-01#F004", "quote": "Si la sala ya está reservada en esa franja, se rechaza la reserva" }],
      "obligation": "SHALL",
      "obligation_basis": "implicit",
      "obligation_evidence": null,
      "decision_branch": "D5",
      "notation": "EARS",
      "ears_pattern": "unwanted_behaviour",
      "master_template": null,
      "master_activity": null,
      "master_condition": null,
      "escalation_fallback": null,
      "slots": { "system": "el sistema ReservaSalas", "feature": null, "states": [], "trigger": "la sala ya está reservada en esa franja", "trigger_keyword": "IF", "response": "rechazar la reserva" },
      "acceptance_criteria": ["Se informa al empleado del rechazo de la reserva."],
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

`reason` admite además `design_detail` y `project_note`.

## Normalizador (nuevo, antes de validar)

Corrige lo que es mecánico, en lugar de pedírselo al modelo. Cada corrección queda en el informe como aviso `FIX`, para medir cuántas hace.

- **Ids:** renumera por tipo, en el orden de aparición (`RF-001`, `RF-002`…; `RNF-001`…), y reescribe todas las referencias. Las preguntas, `Q-001`…
- **Vínculos:** completa el sentido que falte entre requisitos y preguntas. Una referencia a un id que no existe se quita, con aviso.
- **Slots:** quita espacios y la puntuación final (`.,;:`). `slots.system` se iguala al nombre del sistema (antes, error C3).
- **Citas:** compara sin marcas de Markdown (`` ` ``, `*`, `_`), con comillas y guiones tipográficos unificados, espacios colapsados y sin distinguir mayúsculas. Si la cita no está en el fragmento citado pero sí, literal, en **un único** fragmento del lote, corrige el id del fragmento. Las entradas repetidas de `source_trace` se quitan.
- **Veredicto:** los recuentos ya no los escribe el modelo; la plataforma los calcula para las métricas.

## Validador: errores por requisito y aceptación parcial

Tres niveles:

| Nivel | Qué | Efecto |
|---|---|---|
| Error del lote | JSON no válido, estructura raíz que no cumple el esquema, `Write` cortado, más del 10 % de los fragmentos del lote sin tratar (C10) | Reparación |
| Error de un elemento | Esquema del requisito o pregunta, C2, C5, C6 (tras normalizar), C7, C12, C15, N2, N3 | Ese requisito o pregunta se **rechaza**: sale del catálogo y queda en el informe con sus errores |
| Aviso | C4, C13, C14, C16, C17, C9 y C11 corregidos, `FIX`, autoverificación que no coincide | Nada |

- **Aceptación parcial:** el lote se acepta si los requisitos rechazados son como mucho el **20 %**. Si son más, se repara.
- C18 (orden de claves) desaparece.
- Las métricas se calculan sobre el catálogo aceptado e incluyen los requisitos rechazados, las correcciones `FIX`, el recuento por tipo y los criterios por requisito.

## Reparación

- Solo por errores del lote o por superar el 20 % de rechazos.
- Como máximo **una** reparación por lote y modelo (configurable; antes, dos). Se mide si hace falta más.
- El anexo lleva el catálogo previo y solo los errores que motivan la reparación.

## Plataforma

- **Unión por tipo:** se renumera cada tipo por separado (`RF-`, `RNF-`, `RA-`, `RT-`, `RI-`) y `Q-`.
- **Deduplicación por modelo,** tras unir los lotes:
  - **Automática** solo si son iguales: mismo tipo, misma notación y patrón, y slots iguales tras normalizar. Se funden en uno (citas, criterios y preguntas unidos).
  - **Casi duplicados:** se señalan como aviso, sin fundir. Fundirlos bien exige similitud semántica, que depende del origen de los embeddings (decisión abierta).
- **Selección de secciones** al lanzar un trabajo: la web muestra los títulos de primer nivel y el usuario desmarca los que no son especificación. Los fragmentos de esas secciones no se envían a ningún modelo.
- **Lotes:** `OutputRatio` y `MaxOutputTokensPerLot` se recalibran con las mediciones del paso 34. Menos requisitos por fragmento permiten lotes mayores y repetir menos veces el prompt.
- **Web:** tipo, id por tipo, criterios de aceptación, requisitos rechazados y correcciones `FIX`.

## Consecuencias para el árbitro (hito posterior)

- Los dos catálogos pueden tener granularidad distinta: el emparejamiento es de **varios a varios** (un requisito de A puede corresponder a dos de B).
- El árbitro recibe los catálogos ya deduplicados por modelo y los requisitos rechazados aparte.

## Pasos

- **29.** Esta especificación, revisada por el autor.
- **30.** Referencia en Python:
  - esquema 1.3;
  - prompt y anexo v1.3;
  - normalizador, validador con niveles y aceptación parcial, y render;
  - `fragmentar.py` con el bloque de índice;
  - los dos ejemplos reescritos en 1.3;
  - mutaciones nuevas y CHANGELOG.

  La CI de los scripts, en verde.
- **31.** Port a C#:
  - normalizador, validador, render y `PromptBuilder` (índice);
  - revisión con aceptación parcial y una reparación;
  - unión por tipo, `FakeCatalog` y paridad con las mutaciones nuevas.
- **32.** Deduplicación por modelo: fusión de iguales y aviso de casi duplicados.
- **33.** Web: selección de secciones al lanzar el trabajo; tipo, criterios, rechazados y correcciones en la vista del catálogo.
- **34.** Medición de v1.2 frente a v1.3 con ReservaSalas, LibraryLoan, 1 o 2 documentos de PURE y el documento técnico del hito 5 (en privado).
  - Qué se mide: requisitos por tipo, preguntas, rechazos, correcciones, reparaciones, tokens, duración y coincidencia entre A y B.
  - Con esas cifras se recalibran los lotes.

## Riesgos

- **Menos información.** Agrupar en criterios puede ocultar detalles que antes eran requisitos. La medición del paso 34 compara la cobertura de fragmentos y una muestra manual.
- **Aceptación parcial.** Un modelo que falle mucho en un tipo de requisito lo perdería en silencio. Por eso los rechazados se muestran siempre.
- **Coste del cambio.** El esquema 1.3 invalida las fixtures y los ejemplos 1.2. Se regeneran desde Python.
