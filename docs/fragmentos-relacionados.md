# Fragmentos relacionados: medición del tope (paso 112)

Generado por `scripts/evaluacion/medir-fragmentos-relacionados.cs` el 26-10-2026 con el proyecto «Clinicas Veterinarias v0.11»: 21 documentos, 896 fragmentos y unos 28.051 tokens de documentación. Embeddings en local (paraphrase-multilingual-MiniLM-L12-v2@e8f8c21 (ONNX fp32)), en 38 s para todos los fragmentos.

Un fragmento anterior está **relacionado** con una iteración nueva si su parecido con alguno de sus fragmentos llega al umbral (0,70 es «parecido», el umbral del emparejamiento). Los tokens son los estimados de sus líneas en `input/documentacion.txt`.

## Cada documento tras los anteriores

| Caso | Nuevos (tokens) | Anteriores (tokens) | Relacionados ≥ 0,70 | Tokens | % de lo anterior | ≥ 0,75 | ≥ 0,80 | ≥ 0,85 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| DOC-02 tras los 1 anteriores | 61 (1.662) | 166 (5.349) | 26 | 613 | 16 % | 17 | 6 | 1 |
| DOC-03 tras los 2 anteriores | 66 (1.544) | 227 (7.011) | 9 | 257 | 4 % | 5 | 1 | 0 |
| DOC-04 tras los 3 anteriores | 56 (1.182) | 293 (8.555) | 18 | 500 | 6 % | 3 | 0 | 0 |
| DOC-05 tras los 4 anteriores | 25 (904) | 349 (9.737) | 14 | 318 | 4 % | 7 | 3 | 1 |
| DOC-06 tras los 5 anteriores | 47 (1.593) | 374 (10.641) | 5 | 128 | 1 % | 2 | 0 | 0 |
| DOC-07 tras los 6 anteriores | 31 (926) | 421 (12.234) | 9 | 273 | 2 % | 5 | 1 | 0 |
| DOC-08 tras los 7 anteriores | 9 (1.077) | 452 (13.160) | 4 | 130 | 1 % | 1 | 0 | 0 |
| DOC-09 tras los 8 anteriores | 16 (759) | 461 (14.237) | 7 | 238 | 2 % | 2 | 1 | 0 |
| DOC-10 tras los 9 anteriores | 25 (650) | 477 (14.996) | 17 | 377 | 4 % | 4 | 1 | 0 |
| DOC-11 tras los 10 anteriores | 42 (1.349) | 502 (15.646) | 13 | 379 | 3 % | 10 | 7 | 4 |
| DOC-12 tras los 11 anteriores | 43 (1.672) | 544 (16.995) | 21 | 613 | 4 % | 11 | 6 | 3 |
| DOC-13 tras los 12 anteriores | 20 (606) | 587 (18.667) | 10 | 269 | 2 % | 4 | 1 | 1 |
| DOC-14 tras los 13 anteriores | 36 (1.064) | 607 (19.273) | 15 | 483 | 2 % | 6 | 2 | 0 |
| DOC-15 tras los 14 anteriores | 50 (1.090) | 643 (20.337) | 9 | 172 | 1 % | 4 | 4 | 1 |
| DOC-16 tras los 15 anteriores | 24 (802) | 693 (21.427) | 2 | 72 | 0 % | 0 | 0 | 0 |
| DOC-17 tras los 16 anteriores | 3 (732) | 717 (22.229) | 6 | 242 | 1 % | 4 | 1 | 0 |
| DOC-18 tras los 17 anteriores | 27 (1.344) | 720 (22.961) | 1 | 11 | 0 % | 0 | 0 | 0 |
| DOC-19 tras los 18 anteriores | 74 (1.823) | 747 (24.305) | 0 | 0 | 0 % | 0 | 0 | 0 |
| DOC-20 tras los 19 anteriores | 49 (1.147) | 821 (26.128) | 22 | 579 | 3 % | 8 | 4 | 1 |
| DOC-21 tras los 20 anteriores | 26 (776) | 870 (27.275) | 19 | 344 | 2 % | 11 | 4 | 4 |

## Tandas de varios documentos

| Caso | Nuevos (tokens) | Anteriores (tokens) | Relacionados ≥ 0,70 | Tokens | % de lo anterior | ≥ 0,75 | ≥ 0,80 | ≥ 0,85 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Tanda: DOC-15 a DOC-21 tras DOC-01 a DOC-14 | 253 (7.714) | 643 (20.337) | 57 | 1.364 | 9 % | 27 | 13 | 6 |
| Tanda: DOC-11 a DOC-21 tras DOC-01 a DOC-10 | 394 (12.405) | 502 (15.646) | 90 | 2.428 | 18 % | 50 | 26 | 12 |

## Resumen (relacionados desde 0,70)

| Medida | Fragmentos | Tokens |
| --- | ---: | ---: |
| Un documento nuevo: mediana | 9 | 269 |
| Un documento nuevo: percentil 90 | 21 | 579 |
| Un documento nuevo: máximo | 26 | 613 |
| Todos los casos: mediana | 10 | 273 |
| Todos los casos: percentil 90 | 26 | 613 |
| Todos los casos: máximo | 90 | 2.428 |

## Requisitos del catálogo validado relacionados con lo nuevo

Para el punto 1 de la revisión de los prompts v0.13: qué requisitos del catálogo validado (235 requisitos, unos 24.856 tokens en `input/catalogo.json`) tendría que leer el modelo para ver si la documentación nueva los contradice. En cada caso, el «catálogo anterior» son los requisitos que no citan nada de lo nuevo. **a)** los que citan algún fragmento relacionado (desde 0,70); **b)** los que se parecen por significado a algún fragmento nuevo (desde 0,70); **a o b**, cualquiera de los dos.

| Caso | Catálogo anterior (tokens) | a) por sus fuentes (tokens) | b) por significado (tokens) | a o b (tokens) | % de lo anterior |
| --- | ---: | ---: | ---: | ---: | ---: |
| DOC-02 tras los 1 anteriores | 232 (24.546) | 12 (1.226) | 2 (157) | 13 (1.309) | 6 % |
| DOC-03 tras los 2 anteriores | 228 (23.989) | 4 (482) | 4 (472) | 8 (954) | 4 % |
| DOC-04 tras los 3 anteriores | 229 (24.126) | 9 (989) | 13 (1.481) | 20 (2.252) | 9 % |
| DOC-05 tras los 4 anteriores | 219 (22.981) | 4 (483) | 0 (0) | 4 (483) | 2 % |
| DOC-06 tras los 5 anteriores | 221 (23.211) | 3 (313) | 0 (0) | 3 (313) | 1 % |
| DOC-07 tras los 6 anteriores | 220 (23.200) | 4 (375) | 19 (2.338) | 22 (2.607) | 10 % |
| DOC-08 tras los 7 anteriores | 229 (24.178) | 0 (0) | 1 (91) | 1 (91) | 0 % |
| DOC-09 tras los 8 anteriores | 233 (24.591) | 5 (567) | 1 (134) | 6 (701) | 3 % |
| DOC-10 tras los 9 anteriores | 228 (24.156) | 2 (215) | 4 (517) | 5 (598) | 2 % |
| DOC-11 tras los 10 anteriores | 214 (22.513) | 1 (81) | 0 (0) | 1 (81) | 0 % |
| DOC-12 tras los 11 anteriores | 207 (21.656) | 9 (1.004) | 16 (1.578) | 23 (2.405) | 11 % |
| DOC-13 tras los 12 anteriores | 231 (24.441) | 1 (123) | 0 (0) | 1 (123) | 0 % |
| DOC-14 tras los 13 anteriores | 207 (21.758) | 5 (518) | 11 (1.215) | 13 (1.420) | 6 % |
| DOC-15 tras los 14 anteriores | 226 (23.793) | 3 (375) | 2 (213) | 5 (588) | 2 % |
| DOC-16 tras los 15 anteriores | 223 (23.544) | 2 (256) | 0 (0) | 2 (256) | 1 % |
| DOC-17 tras los 16 anteriores | 223 (23.539) | 1 (80) | 1 (91) | 2 (171) | 1 % |
| DOC-18 tras los 17 anteriores | 233 (24.588) | 0 (0) | 0 (0) | 0 (0) | 0 % |
| DOC-19 tras los 18 anteriores | 234 (24.752) | 0 (0) | 0 (0) | 0 (0) | 0 % |
| DOC-20 tras los 19 anteriores | 220 (23.287) | 10 (1.007) | 16 (1.775) | 24 (2.618) | 11 % |
| DOC-21 tras los 20 anteriores | 219 (23.433) | 3 (264) | 0 (0) | 3 (264) | 1 % |
| Tanda: DOC-15 a DOC-21 tras DOC-01 a DOC-14 | 170 (18.068) | 16 (1.645) | 16 (1.715) | 28 (2.965) | 16 % |
| Tanda: DOC-11 a DOC-21 tras DOC-01 a DOC-10 | 95 (9.780) | 18 (1.833) | 21 (2.080) | 31 (3.157) | 33 % |

