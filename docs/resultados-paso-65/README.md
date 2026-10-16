# Resultados del paso 65: flujo completo con dos proyectos de ejemplo

Material para el capítulo de evaluación de la memoria. El flujo v0 completo (preguntas → sugerencias → catálogo → backlog → mockup) se ejecutó con los dos proyectos ficticios de `ejemplos/`, del 15 al 16-10-2026:
- A, árbitro, sugerencias y mockup: `SS - GPT-6-SOL`;
- B: `SS - GPT-6-LUNA`;
- prompts v0.9.

Los datos de cada trabajo están en los CSV de esta carpeta, exportados desde la web («Descargar el informe del proyecto»):
- [`informe-proyecto-mediano.csv`](informe-proyecto-mediano.csv): banco de alimentos, 7 archivos, unos 5.900 tokens de documentación;
- [`informe-proyecto-grande.csv`](informe-proyecto-grande.csv): red de clínicas veterinarias, 21 archivos en 13 formatos, unos 23.400 tokens.

## Coste y tiempo

| Fase | Mediano: entrada / salida | Mediano: tiempo | Grande: entrada / salida | Grande: tiempo |
| --- | --- | --- | --- | --- |
| Preguntas | 0,14 M / 34.000 | 4 min | 1,09 M / 63.000 | 8 min |
| Sugerencias de respuesta | 0,04 M / 10.000 | 2 min | 0,10 M / 16.000 | 3 min |
| Catálogo | 0,27 M / 62.000 | 8 min | ≥ 0,50 M / ≥ 92.000 | 12 min |
| Backlog | 0,21 M / 63.000 | 5 min | ≥ 0,21 M / ≥ 60.000 | 9 min |
| Mockup | 1,57 M / 334.000 | 36 min | 3,28 M / 889.000 | 73 min |
| **Total** | **2,23 M / 504.000** | **55 min** | **≥ 5,18 M / ≥ 1,12 M** | **105 min** |
| Reparaciones | 0 | | 8 (2 en preguntas, 2 en catálogo, 4 en el mockup) | |

- **El mockup se lleva la mayor parte del coste:** el 70 % de la entrada en el mediano y el 63 % en el grande.
- **El coste crece más deprisa que la documentación:** el grande tiene 4 veces más documentación que el mediano y cuesta 2,3 veces más en total, pero 8 veces más en la fase de preguntas.
- **El grande da cifras mínimas:** 3 de sus ejecuciones se detuvieron al compactar con la salida ya escrita, y no dejan dato de consumo.
- **Una pantalla del mockup del grande falló** por un 504 de la pasarela y se recuperó al reanudar al día siguiente. Su tiempo y su coste están incluidos.

## Qué aporta cada modelo

| Fase | Proyecto | Propone A | Propone B | Unión | De ambas | Solo de A | Solo de B | Discrepancias | Descartados |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Preguntas | Mediano | 27 | 21 | 29 | 17 | 10 | 2 | 0 | 0 |
| Preguntas | Grande | 50 | 17 | 55 | 11 | 38 | 6 | 0 | 1 |
| Catálogo | Mediano | 120 | 68 | 100 | 65 | 34 | 1 | 0 | 1 |
| Catálogo | Grande | 299 | 192 | 209 | 180 | 28 | 1 | 0 | 3 |
| Backlog | Mediano | 45 | 28 | 45 | 45 | 0 | 0 | 1 | 0 |
| Backlog | Grande | 109 | 54 | 109 | 89 | 20 | 0 | 2 | 0 |

**En los dos proyectos y en las tres fases, A (SOL) propone más que B (LUNA), y la unión del árbitro se parece mucho a A.** Lo que aporta solo B es muy poco: entre 0 y 6 elementos por fase. Puede deberse a que A es más completo o a que el árbitro, que también es SOL, prefiere la propuesta de su mismo modelo. Es la pregunta clave de la evaluación, y se estudia en [`../experimento-sesgo-arbitro.md`](../experimento-sesgo-arbitro.md).

## Contradicciones y huecos puestos a propósito

Cada proyecto lleva contradicciones y huecos entre sus documentos, listados en [`ejemplos/README.md`](../../ejemplos/README.md). Se comprobó, leyendo las preguntas y buscando en el catálogo validado, si el flujo los trata como debe:
- una contradicción **abierta** debe preguntarse;
- una **resuelta** en un documento posterior no debe preguntarse, y el catálogo debe recoger la decisión final;
- un **hueco** que solo aparece en un documento debe estar en el catálogo;
- lo que queda **fuera de alcance** no debe estar en el catálogo.

| Tipo | Mediano (M1–M12) | Grande (G1–G26) |
| --- | --- | --- |
| Abiertas, ¿se preguntan? | 10 de 10. 9 las detectan los dos modelos; M11 (valor del certificado de donación), solo A y en parte | 5 de 5. 4 las detectan los dos modelos; G17 (sin conexión), solo A |
| Resueltas, ¿decisión final en el catálogo, sin volver a preguntar? | 1 de 1 (M9: quien recoge en lugar de la familia, con nombre y DNI) | 14 de 14 (por ejemplo, cancelar con 12 h, el Plan Adulto del correo o el recordatorio de 7 días y del día anterior) |
| Huecos de un solo documento, ¿en el catálogo? | 1 de 1 (M10: voluntarios que se apuntan solos y aviso si falta gente) | 6 de 6, 3 de ellos preguntados además para concretar |
| Fuera de alcance, ¿fuera del catálogo? | — | 1 de 1 (el programa de puntos) |
| **Total** | **12 de 12** | **26 de 26** |

## Qué hizo el usuario

- **Preguntas:** todas las respuestas son sugerencias de la IA aceptadas sin cambios (29 en el mediano y 55 en el grande), casi todas marcadas como «suposición». Las contradicciones abiertas las resolvió la IA, no un cliente, y esas suposiciones pasaron al catálogo.
- **Catálogo y backlog:** se validaron sin cambios (100 y 45 en el mediano, 209 y 109 en el grande). La métrica de cambios del usuario no aporta nada en estas ejecuciones.

## Conclusiones

1. **El flujo completo funciona de principio a fin** con documentación variada (13 formatos en el grande) y sin intervención técnica. La única incidencia fue un fallo de la pasarela, y se recuperó al reanudar.
2. **La fase de preguntas detecta las contradicciones abiertas** (15 de 15 entre los dos proyectos) y no vuelve a preguntar lo que ya resuelven los documentos. El catálogo recoge las decisiones finales y los huecos.
3. **La verificación cruzada aporta poco en lo que solo encuentra B:** casi todo sale de A. Pero 2 de las 15 contradicciones abiertas solo las detectó A, y ninguna solo B. Queda por saber si es por el árbitro (ver el experimento).
4. **El coste está en el mockup.** Las fases de análisis (preguntas, catálogo y backlog) cuestan entre 0,6 y 1,9 M de tokens de entrada; el mockup, entre 1,6 y 3,3 M.

## Limitaciones

- **Ni independientes ni reales:** los proyectos, la lista de contradicciones y la comprobación los hizo Claude, que también desarrolla la plataforma. La comprobación se hizo por palabras clave y leyendo, y debe revisarla el autor.
- **Una sola ejecución por proyecto,** sin repeticiones.
- **Las respuestas a las preguntas no las dio un cliente,** sino la IA.
- **Sin cambios del usuario** en el catálogo ni en el backlog.
- **Sin documento público:** el autor decidió el 15-10-2026 sustituirlo por los dos proyectos ficticios. Un documento del dataset PURE sería el siguiente paso, si se quiere una prueba que el tribunal pueda reproducir.
