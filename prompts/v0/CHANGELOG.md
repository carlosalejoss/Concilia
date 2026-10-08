# Cambios de los prompts v0

## v0 (2026-10-08)

Primera versión, propuesta por Claude para la iteración v0 (D-65 a D-74) y pendiente de revisión del autor (paso 41).

- Siete prompts (preguntas, catálogo y backlog para A y B y para el árbitro, más el mockup) y un anexo de reparación común.
- Seis esquemas JSON (draft 2020-12), solo de estructura. Las reglas entre campos están en `validar_v0.py`.
- Ejemplo completo de ReservaSalas en `ejemplo/`: dos propuestas y la salida del árbitro de cada fase, más las respuestas del cliente.
- El catálogo es un subconjunto deliberado de la v1.4: tipos (`RF`, `RNF`, `RA`, `RT`, `RI`), EARS por defecto, MASTeR para cualidades medibles y para `should` y `may`, y obligatoriedad. El modelo escribe la frase entera (sin slots) y no hay lotes, inventario, criterios ni preguntas dentro del catálogo.
- El árbitro de cada fase recibe las dos propuestas anónimas en orden aleatorio (D-69), empareja por significado y devuelve, por elemento, de qué ids de cada propuesta sale (`de`) y una `explicacion`; en catálogo y backlog, también `discrepancia`. Lo que no pasa a la unión va en los descartes con su motivo, así que no se pierde nada sin rastro.
