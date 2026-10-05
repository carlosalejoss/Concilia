
---

# §12 REPARACIÓN (intento {{INTENTO}})

Ya generaste un catálogo para esta misma entrada y la plataforma lo ha devuelto. Tu catálogo anterior y los motivos están a continuación.

<catalogo_previo>{{CATALOGO_PREVIO}}</catalogo_previo>

<errores>
{{ERRORES}}
</errores>

Cada línea empieza por lo afectado (`RF-…`, `RNF-…`, `RA-…`, `RT-…`, `RI-…`, `Q-…`, `CAP-…`, `DISCARDED[n]`, `DOMAIN[n]` o `LOTE`) y el código de la regla (§3 a §9). `SCHEMA` indica que el JSON no cumple la estructura de §10. Solo se listan los motivos de la reparación:

- errores del lote (JSON no válido, estructura, fichero cortado, C10);
- requisitos rechazados, si superaron el 20 %;
- funcionalidades del inventario sin ningún `RF` o `RI` aceptado (C20).

Instrucciones:

- Corrige **solo** lo que señalan los errores. No cambies lo demás.
- Conserva los ids existentes. Añade lo nuevo al final, con los siguientes ids libres de su tipo.
- **C10:** procesa los fragmentos sin tratar con §2 a §8.
- **Requisito rechazado:** corrígelo según su código. Si el cambio afecta a la rama del árbol de §6, actualiza todos los campos que dependen de ella (tipo, notación, plantilla, slots).
- **C20:** escribe el `RF` o `RI` que cubre esa funcionalidad, con lo que diga la fuente. Si la entrada del inventario era un error, corrígela y explícalo en `verdict.issues`: quítala si no es una funcionalidad observable del sistema (§3) o es futura, y únela a otra si solo era un detalle o una variante de ella (§5).
- **C5** sobre una palabra que forma parte del nombre de una entidad del dominio: declárala en `domain_terms` (N7), no reescribas la fuente. Si la palabra califica algo, aplica N3.
- **N2:** copia la cifra tal como aparece en la fuente, o cita el fragmento que la contiene. Nunca elimines una cifra que sí está en la fuente.
- Actualiza `verdict` (`self_check` e `issues`).
- Escribe el catálogo **completo** corregido en `output/catalogo.json` con **una sola llamada** a Write y termina con la línea de veredicto de §10.
