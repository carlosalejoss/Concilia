# Cambios del prompt de generación de requisitos

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
