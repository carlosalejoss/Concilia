# Paso 123: la opción C con una documentación grande

Complemento del paso 116, a petición del autor (03-11-2026). En el paso 116, con clínicas (unos 26.000 tokens en total), leerlo todo salió más barato que la opción C con la misma detección. Faltaba saber qué pasa con una documentación grande, el caso para el que se diseñó la opción C. Lo ejecutó Claude por la API el 03 y el 04-11-2026, con `SS - GPT-6-SOL` como A y árbitro y `SS - GPT-6-LUNA` como B, prompts v0.13 y la versión del paso 122.

## El documento

- **Fuente:** el dataset PURE, versión 2.0 (Ferrari, Spagnolo y Gnesi, «PURE: a Dataset of Public Requirements Documents», RE 2017), en Zenodo: registro 7118517, DOI 10.5281/zenodo.7118517. Licencia **CC BY 4.0**.
- **Documento:** `2009 - peppol.pdf`, el entregable D2.1 de PEPPOL, con los requisitos funcionales y no funcionales del *Virtual Company Dossier*: una aplicación europea para que las empresas reúnan los certificados que piden los concursos públicos. Tiene 127 páginas y unos 96.000 tokens de texto, en inglés.
  - Se eligió por ser el más grande de los que describen una aplicación con usuarios. Los otros dos de más de 60.000 tokens son sistemas técnicos.
  - Ninguno de los documentos que se podían usar antes era grande: clínicas tiene unos 23.400 tokens, y el documento B, unos 23.000.
- **Cambio sobre el original:** partido en dos PDF por páginas con `pypdf`, sin tocar nada más, donde empieza la sección 4:
  - **Iteración 1:** páginas 1–90 (secciones 1 a 3: visión y requisitos funcionales, legales y técnicos), unos 71.000 tokens.
  - **Iteración 2:** páginas 91–127 (sección 4: conceptos para los pilotos), unos 25.000 tokens.
- Los PDF no se guardan en el repositorio. Para repetirlo:
  1. descargar `requirements.zip` del registro de Zenodo;
  2. partir `req/2009 - peppol.pdf` por esas páginas;
  3. crear un proyecto en inglés;
  4. subir cada parte en su iteración y cerrar las dos.

## Qué se hizo

Proyecto «PURE PEPPOL VCD (paso 123)», con el idioma en inglés y el sistema «the Virtual Company Dossier system». Solo se miden las dos rondas centradas en la iteración 2, sobre el mismo estado del proyecto: sin preguntas anteriores ni catálogo, para que lo único distinto sea la lectura.

1. **Opción A, la ronda que lo lee todo** (experimental, del paso 116): A y B leen enteras las dos partes, unos 96.000 tokens.
2. **Opción C, la ronda centrada:** A y B leen entera la parte 2 y los fragmentos de la parte 1 relacionados con ella (embeddings desde 0,70, con un tope de 20.000 tokens). El resto de la parte 1 se puede consultar.

Un primer lanzamiento de la ronda centrada se detuvo a los pocos segundos reiniciando la API, porque el autor tenía que irse y el equipo pierde la conexión. Se repitió al día siguiente.

## Resultados

| | Opción A, lo lee todo | Opción C, centrada |
| --- | --- | --- |
| Resultado | **fallida**: ni A ni B escriben su salida | **válida a la primera** |
| Qué pasó | A (SOL) y B (LUNA) llenan los 128.000 tokens de contexto leyendo, compactan dos veces sin escribir y la plataforma los detiene (paso 92); al repetirse, igual | A y B terminan en unos 2 minutos sin compactar; el árbitro, en 1 |
| Ejecuciones | 4 fallidas (2 de A y 2 de B), sin árbitro | 3, sin reparaciones |
| Tokens de entrada | unos 1.317.000* | 548.088 |
| Tokens de salida | unos 14.800* | 24.691 |
| Coste | unos 1,35 $*, **sin resultado** | **0,87 $** |
| Minutos | 3 | 3 |
| Preguntas | — | 10 (13 de A, ninguna de B; el árbitro descarta 3) |
| Fragmentos citados | — | 31 de la parte nueva y 13 de la anterior |

\* Estimado (paso 95): las ejecuciones detenidas no tienen dato de consumo.

- **Con una documentación grande, leerlo todo no funciona:** no cabe en el contexto de 128.000 tokens. La opción C sí funciona, y además aprovecha lo anterior que hace falta: un tercio de los fragmentos que citan las preguntas son de la parte 1.
- **Junto con el paso 116:**
  - con documentación pequeña (unos 26.000 tokens), leerlo todo es igual de bueno y más barato;
  - con documentación grande (unos 96.000), solo funciona la opción C.
  La opción C es la que vale para cualquier tamaño, y leerlo todo, una optimización para los proyectos pequeños.
- **Es la primera ronda del flujo en inglés,** y funciona: las preguntas salen en inglés y citan bien los fragmentos.
- **LUNA no propuso ninguna pregunta.** Su propuesta es válida, pero vacía. Con una sola ejecución no se sabe si es por el inglés, por el documento o por azar.
- **Las duraciones de las repeticiones no son reales:** la plataforma guarda cada repetición al final de la tarea, con la hora de inicio del primer intento, así que todos los intentos salen con la duración total. La duración del trabajo sí es real. Pasa desde que existen los reintentos.

## En palabras sencillas

Cuando llega documentación nueva, la plataforma puede hacer que la IA lo lea todo o solo lo nuevo y lo que tiene relación con ello. Con un proyecto pequeño, leerlo todo salía más barato. Con un documento público grande, de unas 127 páginas, leerlo todo ya no le cabe a la IA en la memoria de trabajo: se atasca y no llega a escribir nada. En cambio, leyendo solo lo nuevo y lo relacionado, termina a la primera y hace preguntas que se apoyan también en lo antiguo.

## Qué hay que mejorar

**Decisión del autor (04-11-2026), como recomendaba Claude:** no se hace nada antes de evaluar. Las tres quedan como mejoras futuras en la sección «Después» de `docs/plan-de-construccion.md`.

- **Leer todo cuando la documentación es pequeña y la opción C cuando es grande:** queda como hallazgo y trabajo futuro.
  - Solo hay una ejecución de cada caso.
  - Habría que fijar un umbral sin medir.
  - Cambiar la lectura antes de congelar la versión obligaría a repetir las mediciones.
- **Lo de LUNA, que no propuso ninguna pregunta:** no se repite esta ronda; se mira en el paso 105 (PURE, en inglés) y en las repeticiones del paso 119.
- **La hora real de cada repetición de una tarea,** para que las duraciones del informe sirvan también por intento: mejora futura.
