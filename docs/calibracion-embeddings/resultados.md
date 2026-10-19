# Resultados de la calibración (paso 87)

Generado por `scripts/calibracion-embeddings/calibrar.cs` el 19-10-2026 con 48 pares etiquetados por el autor (0 sin clave) y los arbitrajes guardados del proyecto de clínicas veterinarias.

## 1. Frente al criterio del autor

Etiquetas de la muestra: distinto 22, mismo 9, parecido 17.

Umbrales propuestos por la calibración: «lo mismo» desde 0,90 y «parecido» desde 0,71. En cada caso, el centro del intervalo de umbrales con más aciertos: 0,89–0,92 para el alto y 0,70–0,72 para el bajo.

**Umbrales fijados por el autor, los que usan las tablas siguientes: «lo mismo» desde 0,90 y «parecido» desde 0,70.**

### Umbral alto: «mismo» frente al resto

| Umbral | Aciertos | Embeddings «mismo», autor «no mismo» | Embeddings «no mismo», autor «mismo» |
| --- | --- | --- | --- |
| 0,40 | 9 de 48 (19 %) | 39 | 0 |
| 0,45 | 12 de 48 (25 %) | 36 | 0 |
| 0,50 | 17 de 48 (35 %) | 31 | 0 |
| 0,55 | 20 de 48 (42 %) | 28 | 0 |
| 0,60 | 25 de 48 (52 %) | 23 | 0 |
| 0,65 | 27 de 48 (56 %) | 21 | 0 |
| 0,70 | 31 de 48 (65 %) | 16 | 1 |
| 0,75 | 32 de 48 (67 %) | 14 | 2 |
| 0,80 | 37 de 48 (77 %) | 9 | 2 |
| 0,85 | 39 de 48 (81 %) | 7 | 2 |
| 0,90 | 41 de 48 (85 %) | 3 | 4 |
| 0,95 | 39 de 48 (81 %) | 1 | 8 |

### Umbral bajo: «distinto» frente al resto

| Umbral | Aciertos | Embeddings «mismo o parecido», autor «distinto» | Embeddings «distinto», autor «mismo o parecido» |
| --- | --- | --- | --- |
| 0,40 | 26 de 48 (54 %) | 22 | 0 |
| 0,45 | 29 de 48 (60 %) | 19 | 0 |
| 0,50 | 32 de 48 (67 %) | 15 | 1 |
| 0,55 | 35 de 48 (73 %) | 12 | 1 |
| 0,60 | 38 de 48 (79 %) | 8 | 2 |
| 0,65 | 38 de 48 (79 %) | 7 | 3 |
| 0,70 | 42 de 48 (88 %) | 2 | 4 |
| 0,75 | 39 de 48 (81 %) | 2 | 7 |
| 0,80 | 34 de 48 (71 %) | 2 | 12 |
| 0,85 | 32 de 48 (67 %) | 2 | 14 |
| 0,90 | 28 de 48 (58 %) | 1 | 19 |
| 0,95 | 24 de 48 (50 %) | 0 | 24 |

### Las tres etiquetas con los umbrales propuestos

| Autor \ Embeddings | mismo | parecido | distinto |
| --- | --- | --- | --- |
| mismo | 5 | 3 | 1 |
| parecido | 2 | 12 | 3 |
| distinto | 1 | 1 | 20 |

Coinciden en 37 de 48 pares (77 %).

Por fase: backlog 12 de 16; catálogo 8 de 14; preguntas 17 de 18.

Pares en los que no coinciden (para revisarlos a mano):

| Par | Fase | Similitud | Autor | Embeddings |
| --- | --- | --- | --- | --- |
| M16 | backlog | 0,478 | parecido | distinto |
| M24 | catálogo | 0,553 | parecido | distinto |
| M48 | catálogo | 0,621 | parecido | distinto |
| M13 | catálogo | 0,686 | mismo | distinto |
| M47 | backlog | 0,740 | mismo | parecido |
| M45 | catálogo | 0,869 | mismo | parecido |
| M33 | backlog | 0,897 | mismo | parecido |
| M18 | backlog | 0,898 | distinto | parecido |
| M20 | catálogo | 0,908 | distinto | mismo |
| M39 | preguntas | 0,930 | parecido | mismo |
| M17 | catálogo | 0,955 | parecido | mismo |

## 2. Frente al consenso de los árbitros (clínicas veterinarias)

El consenso «juntar» es un par que juntan al menos el 80 % de los árbitros, y «no juntar», uno que juntan como mucho el 20 %; el resto es dudoso. Solo cuentan los pares que son el más parecido de uno de sus dos elementos.

El árbitro no solo junta lo que es «lo mismo»: también junta muchos pares «parecidos», conservando el detalle de cada uno (sección 3). Por eso se compara su consenso con dos lecturas de los embeddings: «mismo» (desde el umbral alto) y «mismo o parecido» (desde el umbral bajo).

| Fase | Arbitrajes | Pares | Consenso «juntar» | Consenso «no juntar» | Dudosos | «Juntar» que los embeddings ven «mismo» | «Juntar» que ven «mismo o parecido» | «No juntar» que no ven «mismo» | «No juntar» que ven «distinto» |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| catálogo | 5 (SS - GPT-6-LUNA: 2, SS - GPT-6-SOL: 3) | 315 | 248 | 57 | 10 | 158 de 248 (64 %) | 246 de 248 (99 %) | 55 de 57 (96 %) | 6 de 57 (11 %) |
| preguntas | 6 (SS - GPT-6-SOL: 3, SS - GPT-6-LUNA: 3) | 53 | 11 | 42 | 0 | 1 de 11 (9 %) | 9 de 11 (82 %) | 42 de 42 (100 %) | 38 de 42 (90 %) |

Similitud media de los pares según cuántos árbitros los juntan:

| Fase | Ninguno | Algunos | Todos |
| --- | --- | --- | --- |
| catálogo | 0,806 | 0,820 | 0,913 |
| preguntas | 0,547 | 0,871 | 0,759 |

## 3. El árbitro frente al criterio del autor

En los pares de la muestra, si el árbitro del trabajo original juntó los dos elementos.

| Autor | Juntados por el árbitro | No juntados |
| --- | --- | --- |
| mismo | 9 | 0 |
| parecido | 12 | 5 |
| distinto | 2 | 20 |

## 4. Acuerdo por elemento con el árbitro de cada trabajo

En los trabajos originales de los tres proyectos ficticios, para cada elemento de A y de B: ¿está también en la otra propuesta? Sí para los embeddings si su par es «mismo» o «parecido»; sí para el árbitro si lo juntó con alguno de la otra propuesta.

| Proyecto | Fase | Los dos sí | Solo embeddings | Solo árbitro | Los dos no | Acuerdo | Solo A (embeddings / árbitro) | Solo B (embeddings / árbitro) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Banco De Alimentos | preguntas | 31 | 1 | 5 | 11 | 88 % | 11 / 10 | 5 / 2 |
| Banco De Alimentos | catálogo | 152 | 31 | 0 | 5 | 84 % | 4 / 34 | 1 / 1 |
| Banco De Alimentos | backlog | 63 | 0 | 10 | 0 | 86 % | 10 / 0 | 0 / 0 |
| Clinicas Veterinarias | preguntas | 18 | 4 | 4 | 41 | 88 % | 37 / 38 | 8 / 6 |
| Clinicas Veterinarias | catálogo | 454 | 29 | 4 | 4 | 93 % | 7 / 28 | 1 / 1 |
| Clinicas Veterinarias | backlog | 122 | 9 | 21 | 11 | 82 % | 31 / 20 | 1 / 0 |
| ReservaSalasHito911.v2 | preguntas | 20 | 0 | 0 | 1 | 100 % | 1 / 1 | 0 / 0 |
| ReservaSalasHito911.v2 | catálogo | 49 | 0 | 0 | 0 | 100 % | 0 / 0 | 0 / 0 |
| ReservaSalasHito911.v2 | backlog | 28 | 0 | 1 | 0 | 97 % | 1 / 0 | 0 / 0 |
| **Total** | | 937 | 74 | 45 | 73 | **89 %** | | |

«Solo A» y «solo B» del árbitro son elementos de su unión que vienen de una sola propuesta; los de los embeddings, elementos sin ninguno «mismo» ni «parecido» en la otra.

