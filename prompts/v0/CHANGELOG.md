# Cambios de los prompts v0

## v0.3, ids con el número de su propuesta para el árbitro (2026-10-09)

Aprobado por el autor tras tres rondas con documento B en las que el árbitro, en su primer intento, citó en una propuesta ids que eran de la otra.

- En `input/propuesta_1.json` y `input/propuesta_2.json`, el id de cada elemento que el árbitro compara (pregunta, requisito o historia) lleva delante el número de su propuesta: `1:P-003`, `2:RF-001`. Los ids que apuntan a otra cosa (las respuestas que cita un requisito, los requisitos de una historia) no cambian.
- Los tres prompts de árbitro piden copiar los ids así en `de` y en los descartes, y escribir sin prefijo los ids de la salida unida. El ejemplo de salida muestra los prefijos.
- La plataforma quita el prefijo de cada propuesta antes de validar y guarda la salida sin prefijos, así que los esquemas, `validar_v0.py` y el resto del flujo no cambian. Un id con el prefijo de la otra propuesta es un error `ARB-DE` con un mensaje claro.

## v0.2, llamadas a Read exactas (2026-10-09)

Tras la primera comparación con un documento real (ver `docs/evolucion-prompt-requisitos.md`). Aprobado por el autor.

- `{{LECTURA}}` ya no da rangos de líneas («1–242») sino los argumentos exactos de cada llamada a `Read` («`offset` 1, `limit` 242; `offset` 243, `limit` 203»). Con rangos, LUNA contaba las líneas desde 0 y nunca leía la última, lo que obligaba a una reparación.
- La sección `# LECTURA` de todos los prompts pide hacer exactamente esas llamadas y recuerda que la primera línea es la 1.

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
