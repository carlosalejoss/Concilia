# Cambios de los prompts v0

## v0.9, el mockup es una aplicación que funciona, no un índice de pantallas (2026-10-15)

El mockup v0.8 del documento C se generó (19 pantallas), pero con un menú lateral que listaba todas las pantallas: el prompt de diseño pedía «el menú con un enlace por pantalla del mapa». El autor quiere un prototipo completo y funcional; sí le gustó que siguiera el estilo del mockup de referencia. Decidido por el autor a propuesta de Claude, con inicio de sesión y los usuarios de prueba a la vista para entrar con un clic:

- `mockup_diseno.md`, §2 «Una aplicación que funciona»:
  - empieza en el inicio de sesión, con los usuarios de prueba a la vista;
  - cada rol entra en su aplicación, con su aspecto y su navegación, siguiendo la referencia de estilo si la tiene;
  - el menú de cada aplicación lleva solo sus secciones principales, nunca un enlace por pantalla, y al resto se llega por acciones (botones, tarjetas, filas, ventanas);
  - ningún id de pantalla ni de historia a la vista;
  - una sola fuente de datos para todos los roles.
- El mapa recoge la navegación: `inicio` (el inicio de sesión) y, en cada pantalla, `aplicacion`, `roles`, `menu` (si es una entrada de menú) y `desde` (las pantallas desde las que se llega). La plataforma comprueba que desde el inicio de sesión se llega a todas (`MAPA-INICIO`, `MAPA-APP`, `MAPA-ROL`, `MAPA-DESDE`, `MAPA-ACCESO`).
- El armazón tiene `entrar(usuario)`, `salir()` y la sesión en curso, en lugar del selector de rol.
- `mockup_pantalla.md` recibe `{{NAVEGACION}}`: su aplicación y roles, desde dónde se llega y a qué pantallas debe llevar, cada una con `mostrarPantalla('…')`. La plataforma comprueba esos enlaces al juntar (`NAV-FALTA`) y, si falta alguno, se corrige como los demás problemas (`mockup_correccion.md`).
- Un mockup diseñado con una versión anterior no se puede reanudar: se genera de nuevo.

## v0.8, la plataforma junta y revisa el mockup (2026-10-15)

Tras la primera prueba real del mockup v0.6 con `SS - GPT-6-SOL` (documento C, 19 pantallas): el diseño y las 19 pantallas salieron válidos, pero la revisión del conjunto tenía que leer entero el prototipo (el armazón y 19 pantallas de 200–370 líneas), no cabía en 128k y Claude Code la paró («autocompact is thrashing»). Decidido por el autor a propuesta de Claude:

- `mockup_integracion.md` desaparece. La revisión la hace la plataforma, sin modelo: junta el armazón y las pantallas, comprueba la página como antes y lo que no encaja entre las partes (`MockupCoherence`: `ID-DUP`, `JS-GLOBAL`, `NAV`).
- Si encuentra problemas, una ejecución por parte afectada con el prompt nuevo `mockup_correccion.md`: lee la parte tal como está (`input/original.html`), el mapa y, si es una pantalla, el armazón, y escribe la parte corregida entera con un solo `Write`. Nunca lee el prototipo entero. La plataforma vuelve a comprobarlo todo.
- **Reanudar:** un mockup que falla deja en la carpeta del trabajo el diseño y las pantallas aceptadas. «Reanudar» crea un trabajo nuevo que parte de ellos y solo ejecuta lo que falta, mientras el backlog validado no cambie.
- Los demás prompts no cambian.

## v0.7, el árbitro no se queda sin contexto y los fragmentos no son números de línea (2026-10-14)

Tras una prueba real del autor con un proyecto de 8 documentos (unos 40.000 tokens) y propuestas de unos 230 requisitos:
- **El árbitro del catálogo no cabía en 128k.** Leía las dos propuestas (con sangría, unas 2.000–2.900 líneas cada una) y además toda la documentación, aunque era opcional. Compactó el contexto seis veces, volvió a leerlo todo después de cada una y agotó los 30 turnos (1,6 M tokens de entrada).
- **LUNA citaba números de línea como fragmentos** (`DOC-06#F830` por la línea 830, donde está `DOC-06#F537`), en tres trabajos seguidos y también en su reparación.

Cambios:
- Las propuestas que lee el árbitro y el catálogo que lee el backlog van con **un elemento por línea** (`ReadingPlan.OnePerLine`): cada requisito, pregunta o historia en una línea compacta. Lo que no cabe en 1.800 caracteres se abre un nivel más, para que `Read` no corte ninguna línea. Ocupan alrededor de una quinta parte menos.
- `arbitro_catalogo.md` y `arbitro_preguntas.md`: la documentación no se lee entera; se consultan solo los tramos necesarios. El plan de lectura de un fichero para consultar dice ahora «no lo leas entero» y, en la documentación, en qué línea empieza cada documento.
- `catalogo.md`, `preguntas.md`, `sugerir_respuestas.md` y los árbitros: el id de un fragmento es el que va entre corchetes, no el número de línea que muestra `Read`.
- El error `REF-FRAG` de un fragmento que no existe, cuando su número es una línea del fichero, dice qué fragmento hay en esa línea, para que la reparación lo corrija.
- Ampliado el 15-10-2026: el mockup también lee con un elemento por línea el backlog, el mapa de pantallas y las historias de cada pantalla. Con sangría, el backlog de 9 épicas del documento C ocupaba 2.743 líneas.
- Plataforma (no es del prompt): si solo uno de los dos modelos termina con una salida válida, el trabajo falla en lugar de dar esa salida como resultado sin verificación cruzada (decisión del autor).

## v0.6, el mockup como una sola aplicación, construida por pasos (2026-10-14)

Pedido por el autor: un único prototipo que reúna todas las funcionalidades del backlog, construido poco a poco con muchas llamadas, para probarlo con `SS - GPT-6-SOL` (128k de contexto). Hasta la v0.5 era una vista por historia, agrupadas por épica, en una sola ejecución.

- `mockup.md` desaparece. En su lugar hay tres prompts, y cada uno es una ejecución distinta del mismo trabajo:
  1. `mockup_diseno.md`: con el backlog, el mapa de pantallas (`output/prototipo/mapa.json`: las pantallas que tendría la aplicación real, en el orden del menú, con la descripción y las historias de cada una) y el armazón (`output/prototipo/app.html`: CSS, cabecera, menú, `const DATOS`, funciones comunes como `mostrarPantalla` y la línea `<!-- PANTALLAS -->`).
  2. `mockup_pantalla.md`, una ejecución por pantalla (hasta 3 a la vez): con el mapa, el armazón y solo sus historias, escribe `output/prototipo/pantallas/P01.html`, un `<section data-pantalla="P01">` que resuelve sus historias juntas. Cada historia tiene un elemento con `data-hu`, que admite varios ids (`data-hu="HU-004 HU-007"`).
  3. `mockup_integracion.md`: lee una copia de todo el prototipo y corrige con Edit lo que no encaja entre pantallas (navegación, datos, nombres repetidos, aspecto). Es la única ejecución con Edit.
- La plataforma guarda en la carpeta del trabajo lo que acepta de cada paso, y cada paso parte de ahí. Al final junta el armazón y las pantallas en `output/mockup.html` y lo comprueba como antes, más la comprobación de que cada pantalla del mapa está en la página.
- Si falla una pantalla (tras su reparación), la revisión del conjunto no se lanza y el trabajo falla.
- Cada paso tiene sus comprobaciones y una reparación, con los códigos nuevos `MAPA`, `MAPA-ID`, `MAPA-TITULO`, `MAPA-HU` y `PANTALLA`.
- Los demás prompts no cambian.

## v0.5, referencia de estilo del mockup (2026-10-14)

Decidido por el autor: si el cliente tiene ya un mockup, el nuestro debe seguir su estilo.

- El mockup del cliente se sube en Documentos como `.zip` y queda marcado como «estilo». No es documentación: no lo leen las fases, solo el mockup, y al subirlo solo se desactualiza el mockup.
- Al crear el trabajo de mockup, la plataforma descomprime cada `.zip` en la carpeta del trabajo (`estilo/{nombre}/`) con sus `.html`, `.css`, imágenes y fuentes. Deja fuera los scripts y lo que no sea estilo, las rutas que salen de la carpeta, `__MACOSX` y los ficheros ocultos. Las hojas de estilo minificadas llevan un salto de línea tras cada regla, para que `Read` no las corte.
- `mockup.md` tiene una sección nueva, §5 ESTILO (`{{ESTILO}}`), y el cierre pasa a §6. Con referencia, lista sus páginas y hojas de estilo para leer con `Read` (no enteras; hasta 120.000 caracteres) y las imágenes y fuentes que puede usar por su ruta (`input/estilo/…`; hasta 512 KB cada una y 4 MB en total). Pide que el prototipo parezca de la misma aplicación sin copiar sus textos ni pantallas. Sin referencia, pide un diseño sobrio y legible, como antes.
- El modelo saca el estilo de los ficheros él solo: la plataforma no resume colores ni fuentes.
- Al juntar las partes, la plataforma mete esas imágenes y fuentes en el `.html` como `data:`. Una ruta de `input/estilo/` que no está en la lista es un error nuevo, `ESTILO`, que va a la reparación. Las imágenes y fuentes incrustadas no cuentan para el límite de 1 MB.
- Los demás prompts no cambian.

## v0.4, mockup por partes (2026-10-13)

Aprobado por el autor después de que el mockup de un backlog de 53 historias no cupiera en 64.000 tokens de salida: Sonnet razonaba unos 30.000 tokens y el `.html` entero iba en un solo `Write`.

- `mockup.md` pide el prototipo en varios ficheros, cada uno con su propio `Write`: una base (`output/mockup/base.html`, con el CSS, el menú, `const DATOS`, las funciones comunes y una línea `<!-- VISTAS -->`) y un fichero por épica (`output/mockup/EP-01.html`…) con solo los `<section data-hu>` de sus historias.
- Marcador nuevo `{{FICHEROS}}`: la lista de los ficheros de las épicas, en orden, con las historias de cada uno.
- La plataforma junta los ficheros en `output/mockup.html` (`MockupTask.Assemble`) y lo comprueba como antes. Códigos nuevos: `FICHERO` (falta la base o una épica), `BASE` (la base no tiene `<!-- VISTAS -->`) y `FRAGMENTO` (una épica que es un documento entero).
- En una reparación, los ficheros del intento anterior siguen en `output/mockup/`: el modelo reescribe solo los que tienen errores.
- Los demás prompts no cambian.

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
