
---

# §12 REPARACIÓN (intento {{INTENTO}})

Ya generaste un catálogo para este mismo lote y la plataforma lo ha devuelto. Tu catálogo anterior y los motivos están a continuación.

<catalogo_previo>{{CATALOGO_PREVIO}}</catalogo_previo>

<errores>
{{ERRORES}}
</errores>

Cada línea empieza por lo afectado (`RF-…`, `RNF-…`, `RA-…`, `RT-…`, `RI-…`, `Q-…`, `CAP-…`, `OBJECTION[n]`, `DOMAIN[n]` o `LOTE`) y el código de la regla (§3 a §9). `SCHEMA` indica que el JSON no cumple la estructura de §10. Solo se listan los motivos de la reparación:

- errores del lote (JSON no válido, estructura, fichero cortado);
- requisitos rechazados, si superaron el 20 %;
- funcionalidades del lote sin ningún requisito aceptado del tipo que exige su `kind` y sin objeción (C20).

Instrucciones:

- Corrige **solo** lo que señalan los errores. No cambies lo demás.
- Conserva los ids existentes. Añade lo nuevo al final, con los siguientes ids libres de su tipo.
- **Requisito rechazado:** corrígelo según su código. Si el cambio afecta a la rama del árbol de §5, actualiza todos los campos que dependen de ella (tipo, notación, plantilla, slots).
- **C20:** escribe el requisito que cubre esa funcionalidad, con lo que diga la fuente y del tipo que exige su `kind` (§3). Si de verdad no debe tener requisitos (es la misma que otra, solo describe diseño interno o es futura), decláralo en `capability_objections` (§3) en lugar de forzar un requisito.
- **C5** sobre una palabra que forma parte del nombre de una entidad del dominio: declárala en `domain_terms` (N7), no reescribas la fuente. Si la palabra califica algo, aplica N3.
- **N2:** copia la cifra tal como aparece en la fuente, o cita el fragmento que la contiene. Nunca elimines una cifra que sí está en la fuente.
- Actualiza `verdict` (`status`, `issues` y `uncovered_capabilities`).
- Escribe el catálogo **completo** corregido en `output/catalogo.json` con **una sola llamada** a Write y termina con la línea de veredicto de §10.
