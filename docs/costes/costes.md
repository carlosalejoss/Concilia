# Coste en tiempo y en dinero

Generado por `scripts/evaluacion/costes.cs` el 12-11-2026 10:43, con los datos guardados de cada ejecución. Material para justificar los cambios del TFG también por lo que cuestan.

**Cómo leer las cifras:**
- **Tokens:** los que da Claude Code al terminar cada ejecución. Una ejecución que la plataforma detuvo antes del final no tiene dato y se **estima** a partir de su consola (paso 95; error en `estimador.md`). Las cifras con estimación llevan `*`; las que tienen ejecuciones sin dato ni estimación son un mínimo y llevan `≥`.
- **Coste:** tokens × precio del modelo (sección 1), en dólares. Desde el paso 128 la entrada leída de la caché se cobra a su precio, que es lo que de verdad se paga (autor, 10-11-2026). Las ejecuciones de antes no guardaban la caché aparte y van a precio completo. Los embeddings se calculan en local y no cuestan dinero, solo tiempo.
- **A precio completo:** toda la entrada al precio de entrada, como se calculaba hasta el 10-11-2026. Solo sirve para comparar con lo de antes.
- **Suscripción:** los modelos de OpenCode Go (el modelo B desde el hito 25) se pagan con la suscripción Go (10 $ al mes). Su coste se calcula por token, con los precios por uso de OpenCode: es lo que pagaría una empresa que pagase por uso.
- **Minutos:** desde que se crea el trabajo hasta que termina. No sirven si el equipo se suspendió durante el trabajo.

## 1. Precios

| Modelo | Entrada (USD por millón de tokens) | Salida (USD por millón de tokens) | Lectura de caché (USD por millón de tokens) | Se paga con |
| --- | --- | --- | --- | --- |
| SS - GPT-6-SOL | 2,00 | 10,00 | 0,20 | pago por uso |
| SS - GPT-6-LUNA | 0,10 | 0,50 | 0,01 | pago por uso |
| SS - GPT-6.1-SOL | 2,00 | 10,00 | 0,10 | pago por uso |
| kimi-k3.1-pro | 2,00 | 10,00 | 0,20 | suscripción OpenCode Go; el coste es por token |
| gemini-4-argon | 4,00 | 20,00 | 0,20 | suscripción OpenCode Go; el coste es por token |
| claude-haiku-5-5 | 0,50 | 2,50 | 0,05 | suscripción OpenCode Go; el coste es por token |

## 2. Cada trabajo del proyecto grande de ejemplo (Clinicas Veterinarias, también en sus nuevas pruebas)

Los trabajos de un nuevo arbitraje sin tiempo de embeddings son anteriores al paso 100: su emparejamiento se guarda desde entonces y ya no se vuelve a calcular (medido después, tardaba entre 15 y 37 s).

| Proyecto | Fecha | Trabajo | Prompt | Resultado | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) | A precio completo (USD) | Minutos | Reparaciones | Embeddings (s / s de CPU) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Clinicas Veterinarias | 15-10 10:52 | Preguntas | v0.9 | válido | 5 | 1.092.194 | 62.659 | 2,31 | 2,31 | 8 | 2 | — |
| Clinicas Veterinarias | 15-10 11:01 | Sugerencias de respuesta | v0.9 | válido | 1 | 97.511 | 15.900 | 0,35 | 0,35 | 3 | 0 | — |
| Clinicas Veterinarias | 15-10 12:11 | Catálogo | v0.9 | válido | 5 | 1.016.590* | 181.689* | 2,70* | 2,70* | 12 | 2 | — |
| Clinicas Veterinarias | 15-10 12:27 | Backlog | v0.9 | válido | 3 | 377.048* | 113.551* | 1,43* | 1,43* | 9 | 0 | — |
| Clinicas Veterinarias | 15-10 12:38 | Mockup | v0.9 | fallido | 43 | 3.215.390 | 869.889 | 15,13 | 15,13 | 71 | 4 | — |
| Clinicas Veterinarias | 16-10 08:31 | Mockup | v0.9 | válido | 1 | 68.530 | 18.906 | 0,33 | 0,33 | 3 | 0 | — |
| Clinicas Veterinarias | 16-10 09:37 | Nuevo arbitraje (preguntas) | v0.9 | válido | 1 | 198.375 | 23.777 | 0,03 | 0,03 | 3 | 0 | — |
| Clinicas Veterinarias | 16-10 09:42 | Nuevo arbitraje (catálogo) | v0.9 | válido | 2 | 726.818* | 98.122* | 0,12* | 0,12* | 27 | 1 | — |
| Clinicas Veterinarias | 16-10 10:11 | Nuevo arbitraje (preguntas) | v0.9 | válido | 1 | 299.163 | 49.664 | 0,05 | 0,05 | 7 | 0 | — |
| Clinicas Veterinarias | 16-10 10:20 | Nuevo arbitraje (catálogo) | v0.9 | fallido | 1 | 382.753* | 42.261* | 0,06* | 0,06* | 10 | 0 | — |
| Clinicas Veterinarias | 16-10 10:39 | Nuevo arbitraje (catálogo) | v0.9 | fallido | 1 | 557.206* | 41.757* | 0,08* | 0,08* | 90 | 0 | — |
| Clinicas Veterinarias | 16-10 12:10 | Nuevo arbitraje (preguntas) | v0.9 | válido | 2 | 254.668 | 38.570 | 0,04 | 0,04 | 5 | 1 | — |
| Clinicas Veterinarias | 16-10 12:15 | Nuevo arbitraje (preguntas) | v0.9 | fallido | 1 | 23.614* | 1.742* | 0,06* | 0,06* | 0 | 0 | — |
| Clinicas Veterinarias | 16-10 12:16 | Nuevo arbitraje (preguntas) | v0.9 | válido | 1 | 175.635 | 17.602 | 0,53 | 0,53 | 2 | 0 | — |
| Clinicas Veterinarias | 16-10 12:19 | Nuevo arbitraje (preguntas) | v0.9 | válido | 1 | 208.313 | 17.970 | 0,60 | 0,60 | 3 | 0 | — |
| Clinicas Veterinarias | 16-10 12:22 | Nuevo arbitraje (catálogo) | v0.9 | válido | 1 | 681.916* | 61.261* | 1,98* | 1,98* | 6 | 0 | — |
| Clinicas Veterinarias | 16-10 12:31 | Nuevo arbitraje (catálogo) | v0.9 | válido | 1 | 448.676* | 68.420* | 1,58* | 1,58* | 6 | 0 | — |
| Clinicas Veterinarias | 16-10 12:39 | Nuevo arbitraje (catálogo) | v0.9 | válido | 2 | 567.402* | 96.231* | 0,10* | 0,10* | 12 | 1 | — |
| Clinicas Veterinarias | 16-10 13:05 | Nuevo arbitraje (catálogo) | v0.9 | fallido | 3 | 665.013* | 86.438* | 0,11* | 0,11* | 27 | 2 | — |
| Clinicas Veterinarias | 16-10 13:36 | Nuevo arbitraje (catálogo) | v0.9 | fallido | 2 | 492.460* | 47.693* | 0,07* | 0,07* | 15 | 1 | — |
| Clinicas Veterinarias | 19-10 12:10 | Nuevo arbitraje (catálogo) | v0.10 | válido | 1 | 413.825* | 57.650* | 1,40* | 1,40* | 5 | 0 | — |
| Clinicas Veterinarias | 19-10 12:15 | Nuevo arbitraje (catálogo) | v0.10 | válido | 1 | 414.181* | 56.086* | 1,39* | 1,39* | 5 | 0 | — |
| Clinicas Veterinarias | 19-10 12:20 | Nuevo arbitraje (catálogo) | v0.10 | válido | 2 | 1.879.107* | 77.418* | 4,53* | 4,53* | 11 | 1 | — |
| Clinicas Veterinarias v0.11 | 20-10 09:38 | Preguntas | v0.11 | válido | 3 | 374.678 | 57.613 | 0,94 | 0,94 | 8 | 0 | 3,8 / 9,7 |
| Clinicas Veterinarias v0.11 | 20-10 09:46 | Sugerencias de respuesta | v0.11 | válido | 1 | 155.133 | 18.696 | 0,50 | 0,50 | 3 | 0 | — |
| Clinicas Veterinarias v0.11 | 20-10 10:11 | Catálogo | v0.11 | fallido | 5 | 966.970* | 73.525* | 2,06* | 2,06* | 8 | 2 | — |
| Clinicas Veterinarias v0.11 | 20-10 11:37 | Catálogo | v0.11 | válido | 6 | 1.637.686* | 124.088* | 3,92* | 3,92* | 17 | 3 | 12,5 / 45,6 |
| Clinicas Veterinarias v0.11 | 22-10 09:00 | Backlog | v0.11 | válido | 3 | 447.072* | 120.292* | 1,67* | 1,67* | 8 | 0 | 7,1 / 35,7 |
| Clinicas Veterinarias v0.11 | 22-10 09:10 | Mockup | v0.11 | válido | 41 | 3.696.191 | 975.684 | 17,15 | 17,15 | 52 | 0 | — |
| Clinicas Veterinarias v0.11 | 22-10 13:44 | Nuevo arbitraje (catálogo) | v0.12 | válido | 1 | 370.349* | 68.682* | 1,43* | 1,43* | 8 | 0 | 30,2 / 130,8 |
| Clinicas Veterinarias v0.11 | 22-10 13:54 | Nuevo arbitraje (backlog) | v0.12 | válido | 1 | 233.175* | 76.711* | 1,23* | 1,23* | 6 | 0 | 4,9 / 20,5 |
| Clinicas Veterinarias v0.11 | 23-10 13:39 | Backlog | v0.12 | válido | 3 | 503.629 | 91.015 | 1,40 | 1,40 | 7 | 0 | 6,3 / 16,2 |
| Clinicas Veterinarias v0.11 | 26-10 08:44 | Mockup | v0.12 | válido | 52 | 4.612.510 | 1.145.839 | 20,68 | 20,68 | 64 | 0 | — |
| Clinicas Veterinarias v0.11 | 27-10 13:04 | Preguntas centradas, leyéndolo todo (experimental) | v0.13 | fallido | 4 | 495.358 | 41.749 | 0,88 | 0,88 | 4 | 2 | — |
| Clinicas Veterinarias v0.11 | 27-10 13:14 | Preguntas centradas, leyéndolo todo (experimental) | v0.13 | válido | 6 | 1.251.126 | 62.696 | 1,80 | 1,80 | 8 | 3 | 3,4 / 13,5 |
| Clinicas Veterinarias v0.11 | 27-10 13:24 | Preguntas centradas, leyéndolo todo (experimental) | v0.13 | válido | 4 | 479.748 | 46.865 | 0,95 | 0,95 | 5 | 1 | 1,7 / 7,9 |
| Clinicas Veterinarias v0.11 | 27-10 13:31 | Preguntas centradas | v0.13 | válido | 3 | 860.932 | 57.277 | 1,67 | 1,67 | 9 | 0 | 0,8 / 3,1 |
| Clinicas Veterinarias v0.11 | 27-10 13:41 | Sugerencias de respuesta | v0.12 | válido | 1 | 174.635 | 9.156 | 0,44 | 0,44 | 1 | 0 | — |
| Clinicas Veterinarias v0.11 | 27-10 13:44 | Actualización del catálogo | v0.13 | válido | 3 | 1.249.680 | 97.932 | 2,53 | 2,53 | 12 | 0 | 0,7 / 2,6 |
| Clinicas Veterinarias v0.11 | 03-11 12:10 | Actualización del backlog | v0.13 | válido | 3 | 798.513* | 51.006* | 1,22* | 1,22* | 7 | 0 | 2,4 / 4,4 |
| Clinicas Veterinarias v0.11 | 03-11 12:17 | Preguntas | v0.12 | válido | 3 | 396.468 | 22.470 | 0,71 | 0,71 | 3 | 0 | 0,1 / 0,6 |
| Clinicas Veterinarias v0.11 | 03-11 12:20 | Sugerencias de respuesta | v0.12 | válido | 1 | 176.092 | 4.975 | 0,40 | 0,40 | 1 | 0 | — |
| Clinicas Veterinarias v0.11 | 03-11 12:22 | Catálogo | v0.12 | fallido | 4 | 438.617* | 68.846* | 0,78* | 0,78* | 23 | 2 | — |
| Clinicas Veterinarias v0.11 | 03-11 12:45 | Catálogo | v0.12 | válido | 4 | 778.741* | 133.420* | 2,17* | 2,17* | 14 | 1 | 12,1 / 48,4 |
| Clinicas Veterinarias v0.11 | 03-11 13:00 | Backlog | v0.12 | válido | 3 | 527.043 | 78.936 | 1,38 | 1,38 | 7 | 0 | 2,5 / 10,0 |

## 3. El flujo completo de cada proyecto, por fase

Solo los trabajos del flujo, no los experimentos (los nuevos arbitrajes y la ronda que lo lee todo del paso 116). Si una fase se repitió, cuentan todas sus ejecuciones: es lo que costó llegar al resultado.

| Proyecto | Fase | Trabajos | Tokens de entrada | Tokens de salida | Coste (USD) | A precio completo (USD) | Minutos |
| --- | --- | --- | --- | --- | --- | --- | --- |
| ReservaSalasHito911.v2 | Preguntas | 2 | 92.349 | 20.086 | 0,23 | 0,23 | 3 |
| ReservaSalasHito911.v2 | Sugerencias de respuesta | 1 | 9.128 | 4.089 | 0,06 | 0,06 | 1 |
| ReservaSalasHito911.v2 | Catálogo | 1 | 49.668 | 26.904 | 0,24 | 0,24 | 2 |
| ReservaSalasHito911.v2 | Backlog | 1 | 78.339 | 28.664 | 0,32 | 0,32 | 2 |
| **ReservaSalasHito911.v2** | **Total** | 5 | **229.484** | **79.743** | **0,86** | **0,86** | **7** |
| Proyecto interno 1 | Preguntas | 1 | 407.932 | 50.325 | 1,02 | 1,02 | 4 |
| Proyecto interno 1 | Sugerencias de respuesta | 1 | 142.409 | 7.741 | 0,36 | 0,36 | 1 |
| Proyecto interno 1 | Catálogo | 1 | 723.409 | 97.995 | 1,67 | 1,67 | 8 |
| Proyecto interno 1 | Backlog | 1 | 397.380 | 77.418 | 0,99 | 0,99 | 5 |
| Proyecto interno 1 | Mockup | 2 | ≥ 0 | ≥ 0 | sin precio | sin precio | 18 |
| **Proyecto interno 1** | **Total** | 6 | **≥ 1.671.130** | **≥ 233.479** | **sin precio** | **sin precio** | **37** |
| Proyecto interno 2 | Preguntas | 1 | 308.410 | 28.475 | 0,66 | 0,66 | 3 |
| Proyecto interno 2 | Sugerencias de respuesta | 1 | 148.823 | 11.840 | 0,42 | 0,42 | 2 |
| Proyecto interno 2 | Catálogo | 1 | 840.777 | 82.604 | 2,04 | 2,04 | 8 |
| Proyecto interno 2 | Backlog | 1 | 623.767 | 74.822 | 1,66 | 1,66 | 7 |
| Proyecto interno 2 | Mockup | 3 | 2.459.131 | 644.901 | sin precio | sin precio | 76 |
| **Proyecto interno 2** | **Total** | 7 | **4.380.908** | **842.642** | **sin precio** | **sin precio** | **96** |
| Proyecto interno 3 | Preguntas | 1 | 527.092 | 42.163 | 1,12 | 1,12 | 5 |
| Proyecto interno 3 | Sugerencias de respuesta | 1 | 209.329 | 11.014 | 0,53 | 0,53 | 4 |
| Proyecto interno 3 | Catálogo | 6 | ≥ 5.122.926 | ≥ 412.411 | ≥ 8,98 | ≥ 8,98 | 102 |
| Proyecto interno 3 | Backlog | 1 | ≥ 163.988 | ≥ 52.756 | ≥ 0,53 | ≥ 0,53 | 17 |
| Proyecto interno 3 | Mockup | 5 | ≥ 6.844.663 | ≥ 1.070.277 | ≥ 24,39 | ≥ 24,39 | 1215 |
| **Proyecto interno 3** | **Total** | 14 | **≥ 12.867.998** | **≥ 1.588.621** | **≥ 35,55** | **≥ 35,55** | **1343** |
| Clinicas Veterinarias | Preguntas | 1 | 1.092.194 | 62.659 | 2,31 | 2,31 | 8 |
| Clinicas Veterinarias | Sugerencias de respuesta | 1 | 97.511 | 15.900 | 0,35 | 0,35 | 3 |
| Clinicas Veterinarias | Catálogo | 1 | 1.016.590* | 181.689* | 2,70* | 2,70* | 12 |
| Clinicas Veterinarias | Backlog | 1 | 377.048* | 113.551* | 1,43* | 1,43* | 9 |
| Clinicas Veterinarias | Mockup | 2 | 3.283.920 | 888.795 | 15,46 | 15,46 | 73 |
| **Clinicas Veterinarias** | **Total** | 6 | **5.867.263*** | **1.262.594*** | **22,25*** | **22,25*** | **105** |
| Banco De Alimentos | Preguntas | 1 | 138.773 | 33.944 | 0,42 | 0,42 | 4 |
| Banco De Alimentos | Sugerencias de respuesta | 1 | 38.430 | 10.232 | 0,18 | 0,18 | 2 |
| Banco De Alimentos | Catálogo | 1 | 269.279 | 62.410 | 0,91 | 0,91 | 8 |
| Banco De Alimentos | Backlog | 1 | 212.321 | 63.142 | 0,78 | 0,78 | 5 |
| Banco De Alimentos | Mockup | 2 | 3.234.706 | 723.197 | 13,70 | 13,70 | 56 |
| **Banco De Alimentos** | **Total** | 6 | **3.893.509** | **892.925** | **15,99** | **15,99** | **75** |
| Clinicas Veterinarias v0.11 | Preguntas | 2 | 771.146 | 80.083 | 1,66 | 1,66 | 10 |
| Clinicas Veterinarias v0.11 | Sugerencias de respuesta | 3 | 505.860 | 32.827 | 1,34 | 1,34 | 5 |
| Clinicas Veterinarias v0.11 | Catálogo | 4 | 3.822.014* | 399.879* | 8,94* | 8,94* | 62 |
| Clinicas Veterinarias v0.11 | Backlog | 3 | 1.477.744* | 290.243* | 4,44* | 4,44* | 22 |
| Clinicas Veterinarias v0.11 | Mockup | 2 | 8.308.701 | 2.121.523 | 37,83 | 37,83 | 117 |
| Clinicas Veterinarias v0.11 | Preguntas centradas | 1 | 860.932 | 57.277 | 1,67 | 1,67 | 9 |
| Clinicas Veterinarias v0.11 | Actualización del catálogo | 1 | 1.249.680 | 97.932 | 2,53 | 2,53 | 12 |
| Clinicas Veterinarias v0.11 | Actualización del backlog | 1 | 798.513* | 51.006* | 1,22* | 1,22* | 7 |
| **Clinicas Veterinarias v0.11** | **Total** | 17 | **17.794.590*** | **3.130.770*** | **59,62*** | **59,62*** | **243** |
| PURE PEPPOL VCD (paso 123) | Preguntas centradas | 2 | ≥ 556.675 | ≥ 25.562 | ≥ 0,90 | ≥ 0,90 | 3 |
| **PURE PEPPOL VCD (paso 123)** | **Total** | 2 | **≥ 556.675** | **≥ 25.562** | **≥ 0,90** | **≥ 0,90** | **3** |
| PURE · microcare | Preguntas | 1 | 189.700 | 37.575 | 0,57 | 0,57 | 4 |
| PURE · microcare | Sugerencias de respuesta | 1 | 48.748 | 12.848 | 0,23 | 0,23 | 2 |
| PURE · microcare | Catálogo | 1 | 304.654 | 82.243 | 0,98 | 0,98 | 10 |
| PURE · microcare | Backlog | 1 | 233.619 | 49.260 | 0,72 | 0,72 | 5 |
| PURE · microcare | Mockup | 1 | 1.431.393 | 394.238 | 6,81 | 6,81 | 25 |
| **PURE · microcare** | **Total** | 5 | **2.208.114** | **576.164** | **9,29** | **9,29** | **46** |
| ReservaSalas analista | Preguntas | 2 | 116.640 | 16.759 | 0,29 | 0,29 | 3 |
| ReservaSalas analista | Sugerencias de respuesta | 1 | 16.200 | 6.011 | 0,09 | 0,09 | 1 |
| ReservaSalas analista | Catálogo | 1 | 109.161 | 43.419 | 0,39 | 0,39 | 6 |
| ReservaSalas analista | Backlog | 1 | 90.908 | 29.944 | 0,33 | 0,33 | 3 |
| **ReservaSalas analista** | **Total** | 5 | **332.909** | **96.133** | **1,11** | **1,11** | **12** |
| LasCuarenta | Preguntas | 13 | 5.540.125 | 302.863 | 9,96 | 9,96 | 41 |
| LasCuarenta | Sugerencias de respuesta | 17 | 2.734.809 | 20.274 | 5,67 | 5,67 | 9 |
| LasCuarenta | Catálogo | 1 | 478.052* | 86.892* | 1,32* | 1,32* | 8 |
| LasCuarenta | Backlog | 1 | 316.922 | 62.776 | 0,88 | 0,88 | 5 |
| LasCuarenta | Mockup | 1 | 878.448 | 235.456 | 4,11 | 4,11 | 17 |
| **LasCuarenta** | **Total** | 33 | **9.948.356*** | **708.261*** | **21,95*** | **21,95*** | **80** |
| Clínicas SOL 6 | Preguntas | 1 | 420.028 | 51.106 | 1,01 | 1,01 | 8 |
| Clínicas SOL 6 | Sugerencias de respuesta | 1 | 145.805 | 13.000 | 0,42 | 0,42 | 2 |
| Clínicas SOL 6 | Catálogo | 4 | 2.126.971* | 430.522* | 8,31* | 9,15* | 87 |
| **Clínicas SOL 6** | **Total** | 6 | **2.692.804*** | **494.628*** | **9,74*** | **10,59*** | **97** |
| Clínicas SOL 6.1 | Preguntas | 1 | 971.640 | 72.931 | 2,37 | 2,37 | 25 |
| Clínicas SOL 6.1 | Sugerencias de respuesta | 1 | 177.257 | 26.360 | 0,62 | 0,62 | 11 |
| Clínicas SOL 6.1 | Catálogo | 3 | 2.348.462* | 306.396* | 5,45* | 5,45* | 48 |
| Clínicas SOL 6.1 | Backlog | 2 | 3.723.072* | 191.165* | 8,22* | 8,22* | 53 |
| **Clínicas SOL 6.1** | **Total** | 7 | **7.220.431*** | **596.852*** | **16,67*** | **16,67*** | **137** |
| ReservaSalas humo Kimi | Preguntas | 1 | 55.912 | 12.832 | 0,18 | 0,24 | 4 |
| **ReservaSalas humo Kimi** | **Total** | 1 | **55.912** | **12.832** | **0,18** | **0,24** | **4** |
| ReservaSalas humo Argon | Preguntas | 1 | 51.385 | 26.991 | 0,54 | 0,60 | 6 |
| **ReservaSalas humo Argon** | **Total** | 1 | **51.385** | **26.991** | **0,54** | **0,60** | **6** |
| ReservaSalas humo Haiku | Preguntas | 1 | 83.213 | 14.114 | 0,14 | 0,20 | 2 |
| **ReservaSalas humo Haiku** | **Total** | 1 | **83.213** | **14.114** | **0,14** | **0,20** | **2** |
| Clínicas B Kimi | Preguntas | 1 | 438.734 | 51.136 | 0,89 | 1,39 | 14 |
| Clínicas B Kimi | Sugerencias de respuesta | 1 | 155.858 | 15.387 | 0,28 | 0,47 | 3 |
| **Clínicas B Kimi** | **Total** | 2 | **594.592** | **66.523** | **1,16** | **1,85** | **17** |
| Clínicas B Argon | Preguntas | 2 | 459.989* | 64.895* | 1,50* | 2,03* | 11 |
| **Clínicas B Argon** | **Total** | 2 | **459.989*** | **64.895*** | **1,50*** | **2,03*** | **11** |

## 4. Antes y después: la misma fase con distintas versiones del prompt

Medias por trabajo, para la misma fase del mismo proyecto. Los nuevos arbitrajes (solo el árbitro, con las mismas propuestas) van aparte de los trabajos completos (A, B y árbitro).

| Proyecto | Fase | Modelos | Prompt | Trabajos (válidos) | Entrada (media) | Salida (media) | Coste medio (USD) | Coste (mín.–máx.) | Minutos (media) | Reparaciones (media) | Embeddings (s de CPU, media) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Banco De Alimentos | Mockup | Mockup 6-SOL | v0.9 | 1 (1) | 1.568.036 | 333.966 | 6,48 | 6,48–6,48 | 36 | 0,0 | — |
| Banco De Alimentos | Mockup | Mockup 6-SOL | v0.11 | 1 (1) | 1.666.670 | 389.231 | 7,23 | 7,23–7,23 | 20 | 0,0 | — |
| Clinicas Veterinarias | Nuevo arbitraje (catálogo) | Árbitro 6-SOL | v0.9 | 2 (2) | 565.296* | 64.840* | 1,78 | 1,58–1,98 | 6 | 0,0 | — |
| Clinicas Veterinarias | Nuevo arbitraje (catálogo) | Árbitro 6-SOL | v0.10 | 3 (3) | 902.371* | 63.718* | 2,44 | 1,39–4,53 | 7 | 0,3 | — |
| Clinicas Veterinarias v0.11 | Backlog | A 6-SOL · Árbitro 6-SOL · B 6-LUNA | v0.11 | 1 (1) | 447.072* | 120.292* | 1,67 | 1,67–1,67 | 8 | 0,0 | 35,7 |
| Clinicas Veterinarias v0.11 | Backlog | A 6-SOL · Árbitro 6-SOL · B 6-LUNA | v0.12 | 2 (2) | 515.336 | 84.976 | 1,39 | 1,38–1,40 | 7 | 0,0 | 13,1 |
| Clinicas Veterinarias v0.11 | Catálogo | A 6-SOL · Árbitro 6-SOL · B 6-LUNA | v0.11 | 2 (1) | 1.302.328* | 98.806* | 2,99 | 2,06–3,92 | 12 | 2,5 | 45,6 |
| Clinicas Veterinarias v0.11 | Catálogo | A 6-SOL · Árbitro 6-SOL · B 6-LUNA | v0.12 | 1 (1) | 778.741* | 133.420* | 2,17 | 2,17–2,17 | 14 | 1,0 | 48,4 |
| Clinicas Veterinarias v0.11 | Mockup | Mockup 6-SOL | v0.11 | 1 (1) | 3.696.191 | 975.684 | 17,15 | 17,15–17,15 | 52 | 0,0 | — |
| Clinicas Veterinarias v0.11 | Mockup | Mockup 6-SOL | v0.12 | 1 (1) | 4.612.510 | 1.145.839 | 20,68 | 20,68–20,68 | 64 | 0,0 | — |
| Clinicas Veterinarias v0.11 | Preguntas | A 6-SOL · Árbitro 6-SOL · B 6-LUNA | v0.11 | 1 (1) | 374.678 | 57.613 | 0,94 | 0,94–0,94 | 8 | 0,0 | 9,7 |
| Clinicas Veterinarias v0.11 | Preguntas | A 6-SOL · Árbitro 6-SOL · B 6-LUNA | v0.12 | 1 (1) | 396.468 | 22.470 | 0,71 | 0,71–0,71 | 3 | 0,0 | 0,6 |
| Clinicas Veterinarias v0.11 | Sugerencias de respuesta | Sugerencias 6-SOL | v0.11 | 1 (1) | 155.133 | 18.696 | 0,50 | 0,50–0,50 | 3 | 0,0 | — |
| Clinicas Veterinarias v0.11 | Sugerencias de respuesta | Sugerencias 6-SOL | v0.12 | 2 (2) | 175.364 | 7.066 | 0,42 | 0,40–0,44 | 1 | 0,0 | — |
| Proyecto interno 1 | Mockup | Mockup claude-sonnet-5-5 | v0.1 | 1 (0) | ≥ 0 | ≥ 0 | sin precio | — | 15 | 0,0 | — |
| Proyecto interno 1 | Mockup | Mockup claude-sonnet-5-5 | v0.2 | 1 (0) | ≥ 0 | ≥ 0 | sin precio | — | 3 | 0,0 | — |
| Proyecto interno 2 | Mockup | Mockup claude-sonnet-5-5 | v0.3 | 1 (0) | 220.862 | 256.408 | sin precio | — | 32 | 0,0 | — |
| Proyecto interno 2 | Mockup | Mockup claude-sonnet-5-5 | v0.4 | 2 (1) | 1.119.134 | 194.246 | sin precio | — | 22 | 0,0 | — |
| Proyecto interno 3 | Mockup | Mockup 6-SOL | v0.7 | 1 (0) | 2.757.262 | 419.506 | 9,71 | 9,71–9,71 | 1115 | 0,0 | — |
| Proyecto interno 3 | Mockup | Mockup 6-SOL | v0.8 | 2 (1) | 311.039* | 51.346* | 1,14 | 0,47–1,80 | 7 | 0,0 | — |
| Proyecto interno 3 | Mockup | Mockup 6-SOL | v0.9 | 1 (1) | 3.465.323 | 548.080 | 12,41 | 12,41–12,41 | 40 | 1,0 | — |
| ReservaSalasHito911.v2 | Preguntas | A 6-SOL · Árbitro 6-SOL · B 6-LUNA | v0 | 1 (1) | 25.555 | 12.611 | 0,10 | 0,10–0,10 | 1 | 0,0 | — |
| ReservaSalasHito911.v2 | Preguntas | A 6-SOL · Árbitro 6-SOL · B 6-LUNA | v0.12 | 1 (1) | 66.794 | 7.475 | 0,13 | 0,13–0,13 | 2 | 0,0 | 1,9 |

## 5. Coste por papel y modelo (Clinicas Veterinarias, flujo completo)

| Papel | Modelo | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) | A precio completo (USD) | Parte del coste |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Mockup | SS - GPT-6-SOL | 44 | 3.283.920 | 888.795 | 15,46 | 15,46 | 69,5 % |
| Árbitro | SS - GPT-6-SOL | 3 | 1.198.451* | 128.829* | 3,69* | 3,69* | 16,6 % |
| A | SS - GPT-6-SOL | 5 | 722.587* | 119.953* | 2,64* | 2,64* | 11,9 % |
| Sugerencias | SS - GPT-6-SOL | 1 | 97.511 | 15.900 | 0,35 | 0,35 | 1,6 % |
| B | SS - GPT-6-LUNA | 5 | 564.794 | 109.117 | 0,11 | 0,11 | 0,5 % |

