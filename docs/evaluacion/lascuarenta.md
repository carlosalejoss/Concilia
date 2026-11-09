# LasCuarenta: un proyecto propio por el flujo completo

Caso adicional de la evaluación, añadido por el autor el 06-11-2026. LasCuarenta es un proyecto universitario suyo, de otra asignatura, y por eso conserva su nombre. **No sustituye a los pasos 118 a 121,** que son los que miden la hipótesis: no hay referencia con la que comparar y solo hay una ejecución.

## En palabras sencillas

El autor pasó por Concilia la documentación de un proyecto que conoce y que no se preparó para esta prueba. La plataforma llegó de los documentos al mockup sin ningún error ni reparación, por 21,95 $ y 80 minutos de trabajo de los modelos.

Esto demuestra que el flujo completo funciona con una documentación ajena a los ejemplos, con parte técnica incluida. **No demuestra que el catálogo y el backlog sean correctos:** el autor solo leyó las discrepancias, así que no haber cambiado nada al validar no dice nada sobre su calidad.

## Condiciones

- **Documentación:** 4 archivos (análisis funcional, arquitectura, `api.yaml` y README), unos 29.500 tokens, en español.
- **Fechas:** del 06 al 09-11-2026.
- **Versión:** código de la rama `feat/github-export`, no la etiqueta `evaluacion-1`. Los prompts son idénticos a los de esa etiqueta (v0.12, sin diferencias salvo los `CHANGELOG`).
- **Modelos:** SOL (A y árbitro), LUNA (B) y SOL para el mockup, por la pasarela.

## Coste de los modelos

| Fase | Trabajos | Tokens de entrada | Tokens de salida | Coste (USD) | Minutos |
| --- | ---: | ---: | ---: | ---: | ---: |
| Preguntas | 13 | 5.540.125 | 302.863 | 9,96 | 41 |
| Sugerencias de respuesta | 17 | 2.734.809 | 20.274 | 5,67 | 9 |
| Catálogo | 1 | 478.052* | 86.892* | 1,32* | 8 |
| Backlog | 1 | 316.922 | 62.776 | 0,88 | 5 |
| Mockup | 1 | 878.448 | 235.456 | 4,11 | 17 |
| **Total** | **33** | **9.948.356*** | **708.261*** | **21,95*** | **80** |

\* Incluye una ejecución del catálogo sin dato de consumo, estimada (paso 95). Tabla completa en `docs/costes/costes.md`.

## Resultado

| Fase | A | B | Unión | De ambas | Solo A | Solo B | Descartados | Desviaciones del árbitro |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Preguntas, ronda 1 | 32 | 21 | 35 | 17 | 14 | 4 | 1 | 0 |
| Catálogo | 192 | 134 | 142 | 121 | 17 | 4 | 1 | 8 |
| Backlog | 41 | 32 | 40 | 29 | 9 | 2 | 0 | 2 |

- **Preguntas:** 13 rondas y 94 preguntas (35, 11, 9, 7, 8, 4, 4, 3, 3, 1, 6, 1 y 2). 81 las respondió el autor, 8 son sugerencias de la IA aceptadas tal cual, 4 sugerencias editadas y 1 descartada.
- **Reparaciones:** ninguna en todo el flujo.
- **Validación:** catálogo (142 requisitos) y backlog (40 historias) validados sin ningún elemento cambiado, añadido ni borrado. El mockup sigue en borrador.

## Tiempo del autor

**No se midió.** Son aproximaciones del autor, dadas después y «muy aproximadas».

| Bloque | Tiempo aproximado | Qué significa |
| --- | --- | --- |
| Responder las preguntas | 2–3 horas | Estimación del autor |
| Revisar el catálogo | 20 minutos | Lo que podría tardar en leerlo todo; solo leyó las discrepancias |
| Revisar el backlog | 35 minutos | Lo que podría tardar en leerlo todo; solo leyó las discrepancias |
| Revisar el mockup | — | No revisado |

Suma: de 3 a 4 horas aproximadamente, sin el mockup, frente a los 80 minutos de los modelos. **Estas cifras no se pueden usar como medida del esfuerzo real.** Las de revisión son hipotéticas. Además, 20 minutos para 142 requisitos son unos 8 segundos por requisito: sirven para una lectura rápida, no para una revisión a fondo.

## Opinión del autor

Sobre el mockup, una impresión general: «no está mal». No lo revisó con detalle.

## Observaciones

1. **Las rondas convergen, que es lo que se busca.** El objetivo del autor es repetir rondas hasta llegar a 0 preguntas, o hasta que las que salgan sean tan poco relevantes que se pueda pasar al catálogo. Las preguntas nuevas bajan de 35 a 1 o 2 (35, 11, 9, 7, 8, 4, 4, 3, 3, 1, 6, 1 y 2), con un repunte a 6 en la undécima ronda. No se llegó a 0: el autor pasó al catálogo tras una última ronda con 2. Ese camino tiene un coste: preguntas y sugerencias son el 71 % del total, unos 0,77 $ por ronda, porque cada ronda vuelve a leer toda la documentación aunque ya queden pocas dudas. El coste es parte del método, no un fallo; lo que se podría mejorar es que las últimas rondas salgan más baratas.
2. **LUNA aporta poco en las últimas rondas:** en las siete últimas propone 0 o 1 preguntas, y SOL entre 0 y 6.
3. **Un aviso confuso:** al revisar la discrepancia de RF-068, el aviso de la plataforma citaba un requisito que se había unido a otro (paso 126 del plan).

## Límites

- Sin referencia: no se puede calcular cobertura ni precisión.
- Sin revisión completa: la validación no confirma el contenido.
- Una sola ejecución, con un código que no es la versión congelada.
- No se han pasado AQUSA ni `vale-ears`.
