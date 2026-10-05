
---

# §9 REPARACIÓN (intento {{INTENTO}})

Ya generaste un inventario para esta misma entrada y la plataforma lo ha devuelto. Tu inventario anterior y los motivos están a continuación.

<inventario_previo>{{INVENTARIO_PREVIO}}</inventario_previo>

<errores>
{{ERRORES}}
</errores>

Cada línea empieza por lo afectado (`CAP-…`, `QI-…`, `DISCARDED[n]`, `SECTION[n]` o `LOTE`) y el código de la regla (§6). `SCHEMA` indica que el JSON no cumple la estructura de §7.

Instrucciones:

- Corrige **solo** lo que señalan los errores. No cambies lo demás.
- Conserva los ids existentes. Añade lo nuevo al final, con los siguientes ids libres.
- **C10:** trata cada fragmento de la lista con §3 a §5. Añádelo a los `fragments` de la funcionalidad de la que habla, descártalo o cítalo en una pregunta. Si describe una funcionalidad que no tienes, comprueba antes que no es una que ya está en el inventario descrita en otro sitio (§3); solo entonces créala.
- **IDS:** da un id nuevo al repetido y corrige las referencias.
- **C6:** copia la cita tal como aparece en el fragmento.
- Actualiza `verdict` (`status`, `issues` y `unprocessed_fragments`).
- Escribe el inventario **completo** corregido en `output/inventario.json` con **una sola llamada** a Write y termina con la línea de veredicto de §7.
