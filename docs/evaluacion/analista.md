# Paso 106: una prueba editando de verdad, como un analista

Hasta ahora todas las respuestas fueron sugerencias de la IA aceptadas tal cual, y nadie editó el catálogo ni el backlog: la métrica de cambios del usuario (paso 64) no tenía datos. Aquí el autor hace de analista con ReservaSalas, con la versión `evaluacion-1`. Cierra también los pasos 49 (segunda ronda de preguntas) y 53 (editar el catálogo).

## Preparación

Hecha por Claude el 04-11-2026, sin coste:
- proyecto 41, «ReservaSalas analista»;
- idioma `es`, sistema «el sistema ReservaSalas»;
- `prompts/generacion_requisitos/ejemplo/fuentes/DOC-01.txt` subido y documentación cerrada.

## Lo que hace Claude (reparto mixto, elegido por el autor el 04-11-2026)

El autor solo revisa y edita el catálogo y el backlog, que es lo que mide el paso. Claude lanza las rondas, responde como el cliente partiendo de las sugerencias y genera el catálogo y el backlog.

| Trabajo | Minutos | Coste | Resultado |
| --- | --- | --- | --- |
| Primera ronda | 1,9 | 0,19 $ | 16 preguntas: 10 de las dos propuestas, 6 solo de A |
| Sugerencias | 1,5 | 0,09 $ | 16, todas suposiciones; 11 son decisiones del cliente |
| Segunda ronda (paso 49) | 1,0 | 0,10 $ | 1 pregunta nueva, de las dos propuestas; ninguna repetida |
| Catálogo | 5,6 | 0,39 $ | 27 requisitos (26 de las dos propuestas, 1 solo de B), con 9 discrepancias marcadas por el árbitro y 12 apoyados en una suposición de la IA |
| Backlog (05-11-2026) | 2,8 | 0,33 $ | 12 historias en 6 épicas (10 de las dos propuestas, 2 solo de A, 1 descartada), 36 criterios y 49 tareas, sin discrepancias |

- **Respuestas como cliente:** 5 sugerencias aceptadas tal cual (origen `ia`) y 11 editadas con una decisión concreta (`ia-editada`); la de la segunda ronda, escrita sin sugerencia (`usuario`). Están en `analista/respuestas-ronda-1.json` y `analista/respuestas-ronda-2.json`.
  - Decisiones tomadas: plazos y límites de reserva concretos; el catering contratado pero fuera de esta versión; el modo festivo, del administrador y para toda la empresa; la sincronización del calendario, externa; el CSV, aplazado; el rendimiento, menos de 2 s con 200 usuarios.
- **Paso 49, la segunda ronda no repite nada.** Su única pregunta (P-017) detecta una contradicción entre la respuesta de la primera ronda (como mucho 4 horas) y el documento, que pide aprobación para reservas de más de 4 horas con más de 20 asistentes. El cliente la resuelve: más de 4 horas solo con más de 20 asistentes y aprobación.

## Notas del autor

Rellénalo mientras trabajas: lo que tardas tú en cada fase, no lo que tarda la IA, y en una frase los errores que más corriges.

| Fase | Minutos tuyos | Qué corriges más |
| --- | --- | --- |
| Catálogo: revisar, corregir, borrar, añadir y validar | 3minutos 20segundos | Solucionar discrepancias |
| Backlog: revisar, corregir y validar | 16minutos | nada |

Discrepancias del catálogo y del backlog: para cada una, ¿era un problema de verdad?

- En catálogo la discrepancia era en todas por temas de obligatoriedad (DEBERÍA/DEBERÁ, cosas así)
- En backlog no había discrepancias

Otras notas:

-

## Resultados

Con el informe de cada trabajo y las notas del autor. El catálogo se validó el 05-11-2026 a las 11:55 y el backlog a las 12:07 (hora peninsular). El CSV del informe está en `analista/informe-proyecto.csv`.

### Las cifras

| | Catálogo | Backlog |
| --- | --- | --- |
| Lo que dejó la IA | 27 requisitos, 9 discrepancias | 12 historias, 36 criterios, 49 tareas, ninguna discrepancia |
| Conservados sin cambios | 27 | 12 |
| Modificados / añadidos / borrados | 0 / 0 / 0 | 0 / 0 / 0 |
| Discrepancias que eran un problema de verdad | ninguna: las 9 eran de obligatoriedad | — |
| Minutos del autor | 3,3 | 16 |
| Minutos de la IA | 5,6 | 2,8 |

- **Todo el flujo:** 1,11 $ y unos 12 minutos de la IA (las dos rondas, las sugerencias, el catálogo y el backlog), más unos 19 minutos de revisión del autor.
- **Las 9 discrepancias del catálogo son la misma.** Una propuesta escribe DEBERÍA y la otra DEBERÁ, y el árbitro elige DEBERÁ en las 9. Todas citan respuestas aceptadas tal cual de las sugerencias (P-002, P-003, P-008 y P-009), que están escritas como propuesta («Proponemos…»). Un modelo lee la propuesta como una recomendación y el otro como una obligación. El autor las dio por buenas sin cambiar nada.
- **Paso 49 (segunda ronda):** no repite ninguna pregunta de la primera. Su única pregunta detecta una contradicción entre una respuesta de la primera ronda y el documento.

### Qué dicen

- **Con un proyecto pequeño, el analista no tuvo nada que corregir:** ni en el catálogo ni en el backlog. Revisar le llevó unos 19 minutos, frente a los 12 que tardó la IA en generarlo todo, y el trabajo de escribirlo desde cero no se ha medido.
- **Las discrepancias que marca el árbitro pueden ser solo de forma.** Aquí las 9 eran DEBERÍA frente a DEBERÁ, nacidas de respuestas escritas como propuesta. Revisarlas fue lo único que hizo el autor en el catálogo.
- **La métrica de cambios del usuario (paso 64) sigue sin ediciones reales.** Ahora tiene un dato, pero es «0 cambios tras una revisión humana», no un recuento de correcciones.

### Limitaciones

- **ReservaSalas es muy pequeño:** un documento de una página, 27 requisitos y 12 historias. Un analista con un proyecto real encontraría más que corregir.
- **El revisor es el autor del TFG,** que conoce bien el sistema y el ejemplo. No es un analista ajeno.
- **La revisión del catálogo fue rápida,** unos 7 segundos por requisito, y se centró en las discrepancias. No se sabe si una revisión más lenta habría encontrado algo.
- **Las respuestas de cliente las escribió Claude** (reparto mixto), así que no se mide cuánto tarda una persona en responder las preguntas.

## En palabras sencillas

El autor hizo de analista: revisó el catálogo y el backlog que generó Concilia para un sistema pequeño de reservas de salas, con permiso para corregir, borrar y añadir lo que quisiera. No cambió nada. Lo único que tuvo que mirar fueron 9 avisos de que los dos modelos no coincidían, y todos eran la misma diferencia de palabras: uno ponía «debería» y el otro «deberá». En total le llevó unos 19 minutos. Con un proyecto tan pequeño, eso dice que lo generado era correcto, pero no permite saber cuánto trabajo ahorra con un proyecto grande.
