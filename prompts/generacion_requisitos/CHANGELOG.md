# Cambios del prompt de generación de requisitos

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
