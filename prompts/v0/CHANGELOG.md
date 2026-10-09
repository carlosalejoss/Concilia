# Cambios de los prompts v0

## v0.1, lectura por partes (2026-10-09)

Propuesta del team leader y aprobada por el autor (hito 12b del plan). Redactada por Claude y pendiente de revisión del autor.

- La documentación, el catálogo validado, el backlog validado y las dos propuestas del árbitro ya no van dentro del prompt. Van a `input/` y el modelo los lee por partes con `Read`, como Claude Code en local. Se quitan los marcadores `DOCUMENTACION`, `CATALOGO`, `BACKLOG`, `PROPUESTA_1` y `PROPUESTA_2`.
- Sección nueva `# LECTURA` en todos los prompts, con el marcador `{{LECTURA}}`: la plataforma lista los ficheros, su número de líneas y los tramos que hay que leer (uno por llamada a `Read`, con `offset` y `limit`).
- A y B leen enteros sus ficheros. El árbitro lee enteras las dos propuestas (y el catálogo en el backlog) y consulta la documentación solo si lo necesita.
- La documentación pasa a un fragmento por línea: `[DOC-01#F007 §2.5] texto`.
- La última línea de cada prompt permite Read, solo para esos ficheros, además de Write.
- No cambian las reglas de cada fase, los esquemas ni las comprobaciones de `validar_v0.py`. Se añade una comprobación de la plataforma, `LECTURA`, que pide la reparación si queda alguna línea sin leer.

## v0, sugerencias de respuesta (2026-10-09)

Propuesto por Claude a petición del autor y pendiente de su revisión.

- Prompt nuevo `sugerir_respuestas.md`: para las preguntas pendientes, un solo modelo propone una respuesta, dice si se basa en la documentación o es una suposición, cita los fragmentos y explica en qué se basa. Nunca contesta por el cliente: la plataforma solo guarda la sugerencia, y la respuesta es la que el usuario guarda.
- Esquema `esquemas/sugerencias.schema.json` y comprobaciones en `validar_v0.py` (código nuevo `FALTA`: pregunta pendiente sin sugerencia). Ejemplo en `ejemplo/sugerencias.json` y ocho casos nuevos en `test_v0.py` (46 en total).

## v0 (2026-10-08)

Primera versión, propuesta por Claude para la iteración v0 (D-65 a D-74) y pendiente de revisión del autor (paso 41).

- Siete prompts (preguntas, catálogo y backlog para A y B y para el árbitro, más el mockup) y un anexo de reparación común.
- Seis esquemas JSON (draft 2020-12), solo de estructura. Las reglas entre campos están en `validar_v0.py`.
- Ejemplo completo de ReservaSalas en `ejemplo/`: dos propuestas y la salida del árbitro de cada fase, más las respuestas del cliente.
- El catálogo es un subconjunto deliberado de la v1.4: tipos (`RF`, `RNF`, `RA`, `RT`, `RI`), EARS por defecto, MASTeR para cualidades medibles y para `should` y `may`, y obligatoriedad. El modelo escribe la frase entera (sin slots) y no hay lotes, inventario, criterios ni preguntas dentro del catálogo.
- El árbitro de cada fase recibe las dos propuestas anónimas en orden aleatorio (D-69), empareja por significado y devuelve, por elemento, de qué ids de cada propuesta sale (`de`) y una `explicacion`; en catálogo y backlog, también `discrepancia`. Lo que no pasa a la unión va en los descartes con su motivo, así que no se pierde nada sin rastro.
