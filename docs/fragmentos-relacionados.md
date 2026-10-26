# Fragmentos relacionados: medición del tope (paso 112)

Generado por `scripts/evaluacion/medir-fragmentos-relacionados.cs` el 26-10-2026 con el proyecto «Clinicas Veterinarias v0.11»: 21 documentos, 896 fragmentos y unos 28.051 tokens de documentación. Embeddings en local (paraphrase-multilingual-MiniLM-L12-v2@e8f8c21 (ONNX fp32)), en 29 s para todos los fragmentos.

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

