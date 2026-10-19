# Calibración de los umbrales del emparejamiento por significado (paso 87)

Material del hito 17 para elegir los dos umbrales que deciden si un elemento de A y uno de B dicen **lo mismo**, se **parecen** o son **distintos**. Se sigue la opción D, que eligió el autor el 19-10-2026:

1. **Calibrar:** el autor etiqueta a mano una muestra de pares, y los umbrales se eligen para coincidir con su criterio, que no depende de ningún LLM.
2. **Comprobar:** se mira si esos umbrales coinciden con lo que decide la mayoría de los árbitros en el proyecto de clínicas veterinarias, que tiene varios arbitrajes de SOL y de LUNA sobre las mismas propuestas.

Solo se usan los proyectos ficticios: banco de alimentos, clínicas veterinarias y ReservaSalas.

## Ficheros

| Fichero | Qué es |
| --- | --- |
| `muestra-para-etiquetar.csv` | 48 pares con sus dos textos y una columna `etiqueta` vacía. Es lo único que hay que abrir para etiquetar. |
| `muestra-clave.json` | De qué trabajo sale cada par, qué texto es de A y cuál de B, su similitud y si el árbitro los juntó. **No lo abras hasta terminar de etiquetar**, para que no te influya. |
| `../../scripts/calibracion-embeddings/muestra.cs` | El script que generó la muestra, con la aplicación en marcha: `dotnet run scripts/calibracion-embeddings/muestra.cs`. Con los mismos datos da la misma muestra. |
| `../../scripts/calibracion-embeddings/calibrar.cs` | El script que calcula los resultados a partir de las etiquetas: `dotnet run scripts/calibracion-embeddings/calibrar.cs -- http://localhost:8080 docs/calibracion-embeddings/muestra-para-etiquetar.csv 0.90 0.70`. |
| `resultados.md` | Las tablas completas que genera `calibrar.cs`. |
| [`resultados-y-decision.md`](resultados-y-decision.md) | **El resumen:** la decisión del autor (0,90 y 0,70), los resultados en palabras sencillas y los límites. |

**Decisión del autor (19-10-2026):** «lo mismo» desde 0,90 y «parecido» desde 0,70. Ver [`resultados-y-decision.md`](resultados-y-decision.md).

## Cómo se eligieron los pares

- **Candidatos:** de cada trabajo original de preguntas, catálogo y backlog, cada elemento de A con su elemento más parecido de B, y al revés.
- **Por qué no al azar:** casi todos los pares son claramente distintos y no enseñan nada.
- **Reparto:** 8 pares por cada tramo de similitud (0,4–0,5, 0,5–0,6… 0,9–1,0), con las tres fases por turnos. Ahí es donde están las dudas.
- **Sin pistas:** el orden de los pares es aleatorio, y el texto de A va unas veces primero y otras segundo, para que no se note de qué modelo es cada uno.

## Cómo etiquetar

Abre `muestra-para-etiquetar.csv` con Excel y escribe en la columna `etiqueta` una de estas tres palabras:

| Etiqueta | Cuándo |
| --- | --- |
| `mismo` | Dicen o piden lo mismo, aunque con otras palabras. Si te quedas solo con uno, no pierdes nada importante. |
| `parecido` | Coinciden en parte de lo que dicen o piden, pero cada uno tiene algo que el otro no: otro actor, otra condición, otro detalle importante. Juntarlos tiene sentido, conservando ese detalle. |
| `distinto` | Dicen cosas diferentes, **aunque sean del mismo tema**. Juntarlos no tendría sentido: son dos elementos. |

Lo que cuenta es lo que dicen, no el tema. Ejemplos inventados, que no están en la muestra:

| Texto 1 | Texto 2 | Etiqueta |
| --- | --- | --- |
| El empleado puede cancelar su reserva. | El empleado puede cancelar su reserva hasta una hora antes. | `parecido`: la misma acción, y el segundo añade un límite |
| El empleado puede cancelar su reserva. | El sistema avisará al organizador cuando se cancele una reserva. | `distinto`: el mismo tema, pero una acción del empleado y un aviso del sistema |
| ¿Quién puede aprobar una reserva? | ¿Cuánto tiempo antes hay que pedir la reserva? | `distinto`: el mismo tema, preguntas diferentes |

Una pregunta que ayuda a decidir: **«¿podría un analista razonable juntarlos en un solo elemento?»**

- Sí, sin perder nada → `mismo`.
- Sí, pero conservando algo de cada uno → `parecido`.
- No, son dos cosas aunque sean del mismo tema → `distinto`.

Guarda el fichero con el mismo nombre y en formato «CSV UTF-8 (delimitado por comas)», para que se conserven las tildes. Si un par te parece imposible de decidir, déjalo vacío: se quedará fuera del cálculo.
