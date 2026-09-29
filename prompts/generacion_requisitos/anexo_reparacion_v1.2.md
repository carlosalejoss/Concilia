
---

# §12 REPARACIÓN (intento {{INTENTO}} de 2)

Ya generaste un catálogo para esta misma entrada y el validador automático de la plataforma encontró errores. Tu catálogo anterior y los errores están a continuación.

<catalogo_previo>{{CATALOGO_PREVIO}}</catalogo_previo>

<errores>
{{ERRORES}}
</errores>

Cada error empieza por el id afectado (`REQ-…`, `Q-…`, `DISCARDED[n]`, `DOMAIN[n]`) y el código de la regla (C2, C5, N2, C12…, ver §3 a §9). `SCHEMA` indica que el JSON no cumple la estructura de §10.

Instrucciones:

- Corrige **solo** lo que señalan los errores. No cambies requisitos, preguntas ni descartes que no aparezcan en la lista, salvo para mantener coherentes las referencias cruzadas (C11) o los recuentos del veredicto.
- Conserva los ids existentes. Si un error `C10` señala fragmentos sin tratar, procésalos con §2 a §8 y añade sus requisitos, preguntas o descartes al final, con los siguientes ids libres.
- Si corregir un error exige cambiar la rama del árbol de §5, cambia todos los campos afectados (notación, plantilla, slots, `escalation_reason`…), no solo el que se cita.
- Un error C5 sobre una palabra que forma parte del nombre de una entidad del dominio se corrige declarando ese nombre en `domain_terms` (N7), no reescribiendo la fuente. Si la palabra califica algo, aplica N3.
- Un error N2 se corrige copiando la cifra tal como aparece en la fuente, o citando el fragmento que la contiene si falta en `source_trace`. Nunca elimines una cifra que sí está en la fuente.
- Actualiza `verdict` (recuentos, `self_check`, `issues`).
- Escribe el catálogo **completo** corregido en `output/catalogo.json` con Write y termina con la línea de veredicto de §10.
