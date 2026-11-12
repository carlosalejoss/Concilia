# Consumo de los modelos

Generado por `scripts/evaluacion/costes.cs` el 12-11-2026 10:33, con los datos guardados de cada ejecución (paso 144). Se regenera tras cada tarea, junto con `costes.md`.

**Cómo leer las cifras:**
- **Todo lo que se ha ejecutado,** también los experimentos y los intentos fallidos: todo cuenta en el límite.
- **Coste:** tokens × precio por token de `precios.json` (sección 1 de `costes.md`), con la entrada leída de la caché a su precio. «A precio completo», como se calculaba hasta el 10-11-2026.
- **OpenCode Go** se paga con la suscripción Go (10 $ al mes), con un límite en dólares a precio de lista por modelo. La cifra es lo que pagaría una empresa por uso y lo que gasta del límite. Gemini 4 Argon tiene un 50 % de descuento hasta el 30-11-2026: la sección 1 descuenta del límite el precio rebajado, como hace OpenCode (comprobado con su consola); las demás secciones dan el precio de lista.
- `*`: con ejecuciones sin dato de consumo, estimadas a partir de su consola (paso 95). `≥`: con ejecuciones sin dato ni estimación, así que es un mínimo.

## 1. Lo que queda de cada límite de OpenCode Go

Mes de Go desde el 11-11-2026 (suscripción del 11-11-2026). La semana son los últimos 7 días y las 5 horas, las últimas 5 horas: OpenCode no dice si sus ventanas son móviles o fijas, y se comprueba con su consola.

| Modelo | Mes: gastado / límite | Mes: queda | Semana: gastado / límite | Semana: queda | 5 horas: gastado / límite | 5 horas: queda |
| --- | --- | --- | --- | --- | --- | --- |
| kimi-k3.1-pro | 1,00* / 15,00 | 14,00 | 1,00* / 7,50 | 6,50 | 0,61* / 3,00 | 2,39 |
| gemini-4-argon | 3,44*† / 30,00 | 26,56 | 3,44*† / 15,00 | 11,56 | 3,19*† / 6,00 | 2,81 |
| claude-haiku-5-5 | 0,02 / 15,00 | 14,98 | 0,02 / 7,50 | 7,48 | 0,00 / 3,00 | 3,00 |

`†`: incluye consumo que la plataforma no registra (ejecuciones cortadas o canceladas, sin evento final), tomado de la consola de OpenCode (`_go.ajustes` en `precios.json`):

- 12-11-2026 08:47, gemini-4-argon: 1,71 $ del límite (3,42 $ a precio de lista). Paso 130: 3 intentos cortados del catálogo de Argon (cada 5 minutos, unos 44.000 tokens de salida cada uno), parado a las 09:06.

## 2. Gasto total, por proveedor y modelo

| Proveedor | Modelo | Ejecuciones | Tokens de entrada | Tokens de salida | De ellos, leídos de la caché | Coste (USD) | A precio completo (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| suscripción del autor | claude-sonnet-5-5 | 5 | ≥ 2.459.131 | ≥ 644.901 | — | sin precio | sin precio |
| pasarela | SS - GPT-6-SOL | 468 | 60.069.836* | 8.621.825* | 952.868 | 204,64* | 206,36* |
| pasarela | SS - GPT-6.1-SOL | 15 | ≥ 5.819.406 | ≥ 312.408 | — | ≥ 14,76 | ≥ 14,76 |
| pasarela | SS - GPT-6-LUNA | 111 | ≥ 14.408.431 | ≥ 2.111.683 | 50.155 | ≥ 2,49 | ≥ 2,50 |
| OpenCode Go (suscripción) | gemini-4-argon | 5 | 237.264* | 137.861* | 62.784 | 3,47* | 3,71* |
| OpenCode Go (suscripción) | kimi-k3.1-pro | 3 | 253.005* | 58.132* | 51.200 | 1,00* | 1,09* |
| OpenCode Go (suscripción) | claude-haiku-5-5 | 1 | 42.258 | 5.704 | 24.162 | 0,02 | 0,04 |

«De ellos, leídos de la caché»: solo las ejecuciones desde el paso 128, que lo guardan aparte.

## 3. Gasto por proyecto

| Proyecto | Tareas | Pasarela (USD) | OpenCode Go (USD) | Total (USD) | A precio completo (USD) | Última tarea |
| --- | --- | --- | --- | --- | --- | --- |
| Clínicas SOL 6 | 6 | 6,89* | 2,85* | 9,74* | 10,59* | 12-11-2026 09:55 |
| Clínicas B Argon | 2 | 0,73* | 0,77* | 1,50* | 2,03* | 12-11-2026 08:35 |
| Clínicas B Kimi | 1 | 0,56 | 0,32 | 0,89 | 1,39 | 11-11-2026 13:44 |
| ReservaSalas humo Haiku | 1 | 0,12 | 0,02 | 0,14 | 0,20 | 11-11-2026 13:25 |
| ReservaSalas humo Argon | 1 | 0,10 | 0,45 | 0,54 | 0,60 | 11-11-2026 13:19 |
| ReservaSalas humo Kimi | 1 | 0,11 | 0,07 | 0,18 | 0,24 | 11-11-2026 13:14 |
| Clínicas SOL 6.1 | 7 | 16,67* | — | 16,67* | 16,67* | 11-11-2026 10:28 |
| LasCuarenta | 33 | 21,95* | — | 21,95* | 21,95* | 09-11-2026 10:29 |
| ReservaSalas analista | 5 | 1,11 | — | 1,11 | 1,11 | 05-11-2026 11:58 |
| PURE · microcare | 5 | 9,29 | — | 9,29 | 9,29 | 04-11-2026 09:43 |
| PURE PEPPOL VCD (paso 123) | 3 | ≥ 2,24 | — | ≥ 2,24 | ≥ 2,24 | 04-11-2026 08:10 |
| Clinicas Veterinarias v0.11 | 22 | 65,92* | — | 65,92* | 65,92* | 03-11-2026 13:00 |
| ReservaSalasHito911.v2 | 5 | 0,86 | — | 0,86 | 0,86 | 26-10-2026 12:00 |
| Banco De Alimentos | 6 | 15,99 | — | 15,99 | 15,99 | 22-10-2026 11:48 |
| Clinicas Veterinarias | 23 | 35,00* | — | 35,00* | 35,00* | 19-10-2026 12:20 |
| Proyecto interno 3 | 14 | ≥ 35,55 | — | ≥ 35,55 | ≥ 35,55 | 15-10-2026 09:14 |
| Proyecto interno 2 | 7 | 4,78 | — | 4,78 (más el mockup con la suscripción, sin precio) | 4,78 | 13-10-2026 12:04 |
| Proyecto interno 1 | 6 | 4,04 | — | 4,04 (más el mockup con la suscripción, sin precio) | 4,04 | 13-10-2026 08:44 |

## 4. Cada tarea, por proyecto

De la más reciente a la más antigua. Una fila por papel; «Ejecuciones» cuenta también los intentos repetidos.

### Clínicas SOL 6

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 12-11-2026 09:55 | Catálogo (`b2c79bc6`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 173.007 | 28.229 | 0,44 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 347.844* | 42.236* | 1,12* |
|  | |  | B | gemini-4-argon | OpenCode Go (suscripción) | 1 | 117.950 | 89.651 | 2,19 |
| | **Total de la tarea** | | | | | 3 | **638.801*** | **160.116*** | **3,75*** |
| 12-11-2026 09:29 | Catálogo (`b0aed511`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 173.519 | 28.553 | 0,44 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 407.960* | 59.143* | 1,41* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 115.253 | 36.279 | 0,03 |
| | **Total de la tarea** | | | | | 3 | **696.732*** | **123.975*** | **1,87*** |
| 12-11-2026 09:06 | Catálogo (`94886ca6`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 173.868 | 27.826 | 0,43 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 301.979* | 54.149* | 1,15* |
|  | |  | B | kimi-k3.1-pro | OpenCode Go (suscripción) | 1 | 132.410* | 34.240* | 0,61* |
| | **Total de la tarea** | | | | | 3 | **608.257*** | **116.215*** | **2,19*** |
| 12-11-2026 08:44 | Catálogo (`1eade3dc`) | fallido | A | SS - GPT-6-SOL | pasarela | 1 | 174.594 | 29.345 | 0,45 |
|  | |  | B | gemini-4-argon | OpenCode Go (suscripción) | 1 | 8.587* | 871* | 0,05* |
| | **Total de la tarea** | | | | | 2 | **183.181*** | **30.216*** | **0,50*** |
| 11-11-2026 08:34 | Sugerencias de respuesta (`3d63c18b`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 145.805 | 13.000 | 0,42 |
| | **Total de la tarea** | | | | | 1 | **145.805** | **13.000** | **0,42** |
| 11-11-2026 08:26 | Preguntas (`03bfcf0f`) | válido, 1 repetición | A | SS - GPT-6-SOL | pasarela | 1 | 124.596 | 15.285 | 0,40 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 209.254 | 17.175 | 0,59 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 86.178 | 18.646 | 0,02 |
| | **Total de la tarea** | | | | | 4 | **420.028** | **51.106** | **1,01** |

### Clínicas B Argon

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 12-11-2026 08:35 | Preguntas (`1a3e5dbc`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 80.797 | 13.555 | 0,23 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 225.881 | 21.878 | 0,36 |
|  | |  | B | gemini-4-argon | OpenCode Go (suscripción) | 1 | 90.082 | 25.978 | 0,72 |
| | **Total de la tarea** | | | | | 3 | **396.760** | **61.411** | **1,31** |
| 11-11-2026 13:59 | Preguntas (`f60766b2`) | fallido | A | SS - GPT-6-SOL | pasarela | 1 | 54.642* | 2.613* | 0,14* |
|  | |  | B | gemini-4-argon | OpenCode Go (suscripción) | 1 | 8.587* | 871* | 0,05* |
| | **Total de la tarea** | | | | | 2 | **63.229*** | **3.484*** | **0,19*** |

### Clínicas B Kimi

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 11-11-2026 13:44 | Preguntas (`4f20286c`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 124.608 | 12.653 | 0,23 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 209.453 | 19.323 | 0,34 |
|  | |  | B | kimi-k3.1-pro | OpenCode Go (suscripción) | 1 | 104.673 | 19.160 | 0,32 |
| | **Total de la tarea** | | | | | 3 | **438.734** | **51.136** | **0,89** |

### ReservaSalas humo Haiku

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 11-11-2026 13:25 | Preguntas (`ee6b1cff`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 10.551 | 3.712 | 0,05 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 30.404 | 4.698 | 0,07 |
|  | |  | B | claude-haiku-5-5 | OpenCode Go (suscripción) | 1 | 42.258 | 5.704 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **83.213** | **14.114** | **0,14** |

### ReservaSalas humo Argon

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 11-11-2026 13:19 | Preguntas (`3940d190`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 10.588 | 3.358 | 0,04 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 28.739 | 3.143 | 0,05 |
|  | |  | B | gemini-4-argon | OpenCode Go (suscripción) | 1 | 12.058 | 20.490 | 0,45 |
| | **Total de la tarea** | | | | | 3 | **51.385** | **26.991** | **0,54** |

### ReservaSalas humo Kimi

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 11-11-2026 13:14 | Preguntas (`f28e8bd9`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 10.904 | 3.965 | 0,05 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 29.086 | 4.135 | 0,06 |
|  | |  | B | kimi-k3.1-pro | OpenCode Go (suscripción) | 1 | 15.922 | 4.732 | 0,07 |
| | **Total de la tarea** | | | | | 3 | **55.912** | **12.832** | **0,18** |

### Clínicas SOL 6.1

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 11-11-2026 10:28 | Backlog (`fab8685d`) | fallido, 2 repeticiones | A | SS - GPT-6.1-SOL | pasarela | 2 | 199.732* | 60.898* | 1,01* |
|  | |  | Árbitro | SS - GPT-6.1-SOL | pasarela | 2 | 2.659.920* | 23.620* | 5,56* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 129.415 | 33.494 | 0,03 |
| | **Total de la tarea** | | | | | 5 | **2.989.067*** | **118.012*** | **6,59*** |
| 11-11-2026 10:05 | Backlog (`21edf843`) | fallido, 3 repeticiones | A | SS - GPT-6-SOL | pasarela | 2 | 189.030* | 34.421* | 0,72* |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 2 | 404.272* | 6.968* | 0,88* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 140.703* | 31.764* | 0,03* |
| | **Total de la tarea** | | | | | 6 | **734.005*** | **73.153*** | **1,63*** |
| 11-11-2026 09:51 | Catálogo (`3673d3a1`) | válido, 1 repetición | A | SS - GPT-6-SOL | pasarela | 1 | 218.237 | 32.612 | 0,76 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 444.203* | 75.759* | 1,65* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 257.253 | 46.278 | 0,05 |
| | **Total de la tarea** | | | | | 4 | **919.693*** | **154.649*** | **2,46*** |
| 11-11-2026 09:22 | Catálogo (`3cc5d298`) | válido, 1 repetición | A | SS - GPT-6.1-SOL | pasarela | 1 | 212.188 | 30.459 | 0,73 |
|  | |  | Árbitro | SS - GPT-6.1-SOL | pasarela | 1 | 644.972* | 61.513* | 1,91* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 200.573 | 24.045 | 0,03 |
| | **Total de la tarea** | | | | | 4 | **1.057.733*** | **116.017*** | **2,67*** |
| 11-11-2026 09:14 | Catálogo (`f72a1566`) | fallido, 1 repetición | A | SS - GPT-6.1-SOL | pasarela | 1 | 138.939 | 924 | 0,29 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 232.097 | 34.806 | 0,04 |
| | **Total de la tarea** | | | | | 3 | **371.036** | **35.730** | **0,33** |
| 11-11-2026 09:03 | Sugerencias de respuesta (`f61b69ad`) | válido | Sugerencias | SS - GPT-6.1-SOL | pasarela | 1 | 177.257 | 26.360 | 0,62 |
| | **Total de la tarea** | | | | | 1 | **177.257** | **26.360** | **0,62** |
| 11-11-2026 08:38 | Preguntas (`ebcc4aa1`) | válido | A | SS - GPT-6.1-SOL | pasarela | 1 | 130.671 | 30.480 | 0,57 |
|  | |  | Árbitro | SS - GPT-6.1-SOL | pasarela | 1 | 765.547 | 26.166 | 1,79 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 75.422 | 16.285 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **971.640** | **72.931** | **2,37** |

### LasCuarenta

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 09-11-2026 10:29 | Mockup (`2e7955c9`) | válido | Mockup | SS - GPT-6-SOL | pasarela | 13 | 878.448 | 235.456 | 4,11 |
| | **Total de la tarea** | | | | | 13 | **878.448** | **235.456** | **4,11** |
| 09-11-2026 10:18 | Backlog (`c1aaf6cb`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 89.247 | 22.680 | 0,41 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 141.630 | 17.604 | 0,46 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 86.045 | 22.492 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **316.922** | **62.776** | **0,88** |
| 09-11-2026 09:54 | Catálogo (`da4b5079`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 142.694 | 23.934 | 0,52 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 198.472* | 37.502* | 0,77* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 136.886 | 25.456 | 0,03 |
| | **Total de la tarea** | | | | | 3 | **478.052*** | **86.892*** | **1,32*** |
| 09-11-2026 09:53 | Sugerencias de respuesta (`2d2a8985`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 120.047 | 1.067 | 0,25 |
| | **Total de la tarea** | | | | | 1 | **120.047** | **1.067** | **0,25** |
| 09-11-2026 09:49 | Preguntas (`09abb4a7`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 120.845 | 6.763 | 0,31 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 71.268 | 1.611 | 0,16 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 120.845 | 6.539 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **312.958** | **14.913** | **0,48** |
| 09-11-2026 09:45 | Preguntas (`6b32ba90`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 120.444 | 5.854 | 0,30 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 63.695 | 797 | 0,14 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 120.472 | 16.970 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **304.611** | **23.621** | **0,46** |
| 09-11-2026 09:20 | Preguntas (`e671f9aa`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 229.749 | 9.922 | 0,56 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 151.756 | 3.198 | 0,34 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 118.577 | 4.891 | 0,01 |
| | **Total de la tarea** | | | | | 3 | **500.082** | **18.011** | **0,91** |
| 09-11-2026 09:15 | Preguntas (`8f74b6cc`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 228.193 | 5.587 | 0,51 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 56.436 | 708 | 0,12 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 118.415 | 15.494 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **403.044** | **21.789** | **0,65** |
| 09-11-2026 09:11 | Preguntas (`1c7566c5`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 226.790 | 8.008 | 0,53 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 61.355 | 2.437 | 0,15 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 117.390 | 8.417 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **405.535** | **18.862** | **0,70** |
| 09-11-2026 08:55 | Preguntas (`ba621ee2`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 225.124 | 7.339 | 0,52 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 146.821 | 2.180 | 0,32 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 116.448 | 11.605 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **488.393** | **21.124** | **0,86** |
| 09-11-2026 08:53 | Sugerencias de respuesta (`854cb3fd`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 218.752 | 1.028 | 0,45 |
| | **Total de la tarea** | | | | | 1 | **218.752** | **1.028** | **0,45** |
| 09-11-2026 08:46 | Preguntas (`a63dd928`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 219.554 | 9.510 | 0,53 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 110.064 | 2.650 | 0,25 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 113.747 | 7.465 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **443.365** | **19.625** | **0,80** |
| 06-11-2026 13:45 | Preguntas (`ce5fa229`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 216.434 | 7.837 | 0,51 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 66.668 | 2.780 | 0,16 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 112.244 | 8.885 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **395.346** | **19.502** | **0,69** |
| 06-11-2026 13:44 | Sugerencias de respuesta (`b44712a7`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 214.437 | 1.100 | 0,44 |
| | **Total de la tarea** | | | | | 1 | **214.437** | **1.100** | **0,44** |
| 06-11-2026 13:28 | Preguntas (`3c1dedab`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 212.610 | 8.112 | 0,51 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 146.574 | 7.032 | 0,36 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 110.133 | 14.104 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **469.317** | **29.248** | **0,89** |
| 06-11-2026 13:14 | Preguntas (`accae84e`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 208.673 | 7.453 | 0,49 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 65.087 | 2.883 | 0,16 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 108.480 | 12.230 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **382.240** | **22.566** | **0,67** |
| 06-11-2026 13:12 | Sugerencias de respuesta (`e2f8703a`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 206.414 | 1.894 | 0,43 |
| | **Total de la tarea** | | | | | 1 | **206.414** | **1.894** | **0,43** |
| 06-11-2026 13:11 | Sugerencias de respuesta (`8d1d0ec2`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 107.186 | 1.795 | 0,23 |
| | **Total de la tarea** | | | | | 1 | **107.186** | **1.795** | **0,23** |
| 06-11-2026 13:04 | Preguntas (`89035a4c`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 203.871 | 10.932 | 0,52 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 148.140 | 6.772 | 0,36 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 105.906 | 9.341 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **457.917** | **27.045** | **0,90** |
| 06-11-2026 13:03 | Sugerencias de respuesta (`45d42a9e`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 105.370 | 767 | 0,22 |
| | **Total de la tarea** | | | | | 1 | **105.370** | **767** | **0,22** |
| 06-11-2026 13:02 | Sugerencias de respuesta (`cf17617d`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 200.437 | 1.178 | 0,41 |
| | **Total de la tarea** | | | | | 1 | **200.437** | **1.178** | **0,41** |
| 06-11-2026 10:47 | Preguntas (`344e4638`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 197.606 | 9.214 | 0,49 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 197.346 | 4.867 | 0,44 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 102.398 | 10.200 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **497.350** | **24.281** | **0,95** |
| 06-11-2026 10:46 | Sugerencias de respuesta (`0d8a88fc`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 195.561 | 1.096 | 0,40 |
| | **Total de la tarea** | | | | | 1 | **195.561** | **1.096** | **0,40** |
| 06-11-2026 10:31 | Sugerencias de respuesta (`c11136d2`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 98.260 | 882 | 0,21 |
| | **Total de la tarea** | | | | | 1 | **98.260** | **882** | **0,21** |
| 06-11-2026 10:19 | Sugerencias de respuesta (`079aa69a`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 185.673 | 923 | 0,38 |
| | **Total de la tarea** | | | | | 1 | **185.673** | **923** | **0,38** |
| 06-11-2026 10:16 | Sugerencias de respuesta (`0400480d`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 184.432 | 844 | 0,38 |
| | **Total de la tarea** | | | | | 1 | **184.432** | **844** | **0,38** |
| 06-11-2026 09:36 | Sugerencias de respuesta (`318dff69`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 183.085 | 1.560 | 0,38 |
| | **Total de la tarea** | | | | | 1 | **183.085** | **1.560** | **0,38** |
| 06-11-2026 09:30 | Sugerencias de respuesta (`43d8ccc4`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 94.653 | 1.081 | 0,20 |
| | **Total de la tarea** | | | | | 1 | **94.653** | **1.081** | **0,20** |
| 06-11-2026 09:29 | Sugerencias de respuesta (`c9929fbe`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 177.837 | 1.447 | 0,37 |
| | **Total de la tarea** | | | | | 1 | **177.837** | **1.447** | **0,37** |
| 06-11-2026 09:26 | Sugerencias de respuesta (`0861b78a`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 175.575 | 1.560 | 0,37 |
| | **Total de la tarea** | | | | | 1 | **175.575** | **1.560** | **0,37** |
| 06-11-2026 09:25 | Sugerencias de respuesta (`e2a2c3d6`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 175.419 | 933 | 0,36 |
| | **Total de la tarea** | | | | | 1 | **175.419** | **933** | **0,36** |
| 06-11-2026 09:23 | Sugerencias de respuesta (`7d997fe3`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 91.671 | 1.119 | 0,19 |
| | **Total de la tarea** | | | | | 1 | **91.671** | **1.119** | **0,19** |
| 06-11-2026 09:07 | Preguntas (`097b6d03`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 176.334 | 9.240 | 0,45 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 210.653 | 14.264 | 0,56 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 92.980 | 18.772 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **479.967** | **42.276** | **1,03** |

### ReservaSalas analista

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 05-11-2026 11:58 | Backlog (`4edfe632`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 25.169 | 11.012 | 0,16 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 42.542 | 8.061 | 0,17 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 23.197 | 10.871 | 0,01 |
| | **Total de la tarea** | | | | | 3 | **90.908** | **29.944** | **0,33** |
| 04-11-2026 12:24 | Catálogo (`9ab07189`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 20.644 | 7.559 | 0,12 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 68.180 | 12.137 | 0,26 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 20.337 | 23.723 | 0,01 |
| | **Total de la tarea** | | | | | 3 | **109.161** | **43.419** | **0,39** |
| 04-11-2026 12:22 | Preguntas (`c757f759`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 14.232 | 750 | 0,04 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 27.101 | 865 | 0,06 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 14.296 | 2.315 | 0,00 |
| | **Total de la tarea** | | | | | 3 | **55.629** | **3.930** | **0,10** |
| 04-11-2026 12:21 | Sugerencias de respuesta (`6a9f1595`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 16.200 | 6.011 | 0,09 |
| | **Total de la tarea** | | | | | 1 | **16.200** | **6.011** | **0,09** |
| 04-11-2026 12:18 | Preguntas (`88ea7241`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 11.544 | 3.555 | 0,06 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 38.596 | 5.073 | 0,13 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 10.871 | 4.201 | 0,00 |
| | **Total de la tarea** | | | | | 3 | **61.011** | **12.829** | **0,19** |

### PURE · microcare

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 04-11-2026 09:43 | Mockup (`a904db4b`) | válido | Mockup | SS - GPT-6-SOL | pasarela | 22 | 1.431.393 | 394.238 | 6,81 |
| | **Total de la tarea** | | | | | 22 | **1.431.393** | **394.238** | **6,81** |
| 04-11-2026 09:36 | Backlog (`4db486c5`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 76.963 | 18.366 | 0,34 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 100.369 | 16.451 | 0,37 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 56.287 | 14.443 | 0,01 |
| | **Total de la tarea** | | | | | 3 | **233.619** | **49.260** | **0,72** |
| 04-11-2026 09:25 | Catálogo (`ebcacde4`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 60.329 | 17.112 | 0,29 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 183.094 | 29.738 | 0,66 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 61.231 | 35.393 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **304.654** | **82.243** | **0,98** |
| 04-11-2026 09:21 | Sugerencias de respuesta (`799ec7bc`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 48.748 | 12.848 | 0,23 |
| | **Total de la tarea** | | | | | 1 | **48.748** | **12.848** | **0,23** |
| 04-11-2026 09:16 | Preguntas (`dc67be99`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 50.396 | 9.777 | 0,20 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 103.653 | 14.976 | 0,36 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 35.651 | 12.822 | 0,01 |
| | **Total de la tarea** | | | | | 3 | **189.700** | **37.575** | **0,57** |

### PURE PEPPOL VCD (paso 123)

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 04-11-2026 08:10 | Preguntas centradas (`d3aa44f7`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 173.651 | 7.389 | 0,42 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 179.875 | 6.331 | 0,42 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 194.562 | 10.971 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **548.088** | **24.691** | **0,87** |
| 03-11-2026 13:58 | Preguntas centradas (`540b4dda`) | fallido | A | SS - GPT-6-SOL | pasarela | 1 | 8.587* | 871* | 0,03* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | ≥ 0 | ≥ 0 | ≥ 0,00 |
| | **Total de la tarea** | | | | | 2 | **≥ 8.587** | **≥ 871** | **≥ 0,03** |
| 03-11-2026 13:53 | Preguntas centradas, leyéndolo todo (experimental) (`f5a6a2d0`) | fallido, 2 repeticiones | A | SS - GPT-6-SOL | pasarela | 2 | 588.366* | 9.581* | 1,27* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 728.680* | 5.226* | 0,08* |
| | **Total de la tarea** | | | | | 4 | **1.317.046*** | **14.807*** | **1,35*** |

### Clinicas Veterinarias v0.11

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 03-11-2026 13:00 | Backlog (`19f8d067`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 174.316 | 27.925 | 0,63 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 243.563 | 23.549 | 0,72 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 109.164 | 27.462 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **527.043** | **78.936** | **1,38** |
| 03-11-2026 12:45 | Catálogo (`6fe013f7`) | válido, 1 repetición | A | SS - GPT-6-SOL | pasarela | 1 | 208.776 | 35.191 | 0,77 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 374.244* | 61.282* | 1,36* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 195.721 | 36.947 | 0,04 |
| | **Total de la tarea** | | | | | 4 | **778.741*** | **133.420*** | **2,17*** |
| 03-11-2026 12:22 | Catálogo (`74e56390`) | fallido, 2 repeticiones | A | SS - GPT-6-SOL | pasarela | 1 | 208.102 | 32.208 | 0,74 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 3 | 230.515* | 36.638* | 0,04* |
| | **Total de la tarea** | | | | | 4 | **438.617*** | **68.846*** | **0,78*** |
| 03-11-2026 12:20 | Sugerencias de respuesta (`5aaa087b`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 176.092 | 4.975 | 0,40 |
| | **Total de la tarea** | | | | | 1 | **176.092** | **4.975** | **0,40** |
| 03-11-2026 12:17 | Preguntas (`945abc03`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 172.864 | 8.362 | 0,43 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 114.381 | 3.919 | 0,27 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 109.223 | 10.189 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **396.468** | **22.470** | **0,71** |
| 03-11-2026 12:10 | Actualización del backlog (`3f6ee98e`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 228.957 | 16.344 | 0,62 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 196.503 | 15.495 | 0,55 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 373.053* | 19.167* | 0,05* |
| | **Total de la tarea** | | | | | 3 | **798.513*** | **51.006*** | **1,22*** |
| 27-10-2026 13:44 | Actualización del catálogo (`220fae39`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 779.212 | 33.536 | 1,89 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 210.072 | 16.811 | 0,59 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 260.396 | 47.585 | 0,05 |
| | **Total de la tarea** | | | | | 3 | **1.249.680** | **97.932** | **2,53** |
| 27-10-2026 13:41 | Sugerencias de respuesta (`d5474a28`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 174.635 | 9.156 | 0,44 |
| | **Total de la tarea** | | | | | 1 | **174.635** | **9.156** | **0,44** |
| 27-10-2026 13:31 | Preguntas centradas (`d5935386`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 292.744 | 15.109 | 0,74 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 382.882 | 12.994 | 0,90 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 185.306 | 29.174 | 0,03 |
| | **Total de la tarea** | | | | | 3 | **860.932** | **57.277** | **1,67** |
| 27-10-2026 13:24 | Preguntas centradas, leyéndolo todo (experimental) (`de8001c0`) | válido, 1 repetición | A | SS - GPT-6-SOL | pasarela | 1 | 175.818 | 14.141 | 0,49 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 145.353 | 14.353 | 0,43 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 158.577 | 18.371 | 0,03 |
| | **Total de la tarea** | | | | | 4 | **479.748** | **46.865** | **0,95** |
| 27-10-2026 13:14 | Preguntas centradas, leyéndolo todo (experimental) (`3ccafd50`) | válido, 3 repeticiones | A | SS - GPT-6-SOL | pasarela | 2 | 451.357 | 20.507 | 1,11 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 2 | 251.834 | 11.974 | 0,62 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 547.935 | 30.215 | 0,07 |
| | **Total de la tarea** | | | | | 6 | **1.251.126** | **62.696** | **1,80** |
| 27-10-2026 13:04 | Preguntas centradas, leyéndolo todo (experimental) (`e61059f8`) | fallido, 2 repeticiones | A | SS - GPT-6-SOL | pasarela | 2 | 339.689 | 17.657 | 0,86 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 155.669 | 24.092 | 0,03 |
| | **Total de la tarea** | | | | | 4 | **495.358** | **41.749** | **0,88** |
| 26-10-2026 08:44 | Mockup (`8581346d`) | válido | Mockup | SS - GPT-6-SOL | pasarela | 52 | 4.612.510 | 1.145.839 | 20,68 |
| | **Total de la tarea** | | | | | 52 | **4.612.510** | **1.145.839** | **20,68** |
| 23-10-2026 13:39 | Backlog (`390982be`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 155.277 | 28.554 | 0,60 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 247.480 | 28.022 | 0,78 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 100.872 | 34.439 | 0,03 |
| | **Total de la tarea** | | | | | 3 | **503.629** | **91.015** | **1,40** |
| 22-10-2026 13:54 | Nuevo arbitraje (backlog) (`257d6b65`) | válido | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 233.175* | 76.711* | 1,23* |
| | **Total de la tarea** | | | | | 1 | **233.175*** | **76.711*** | **1,23*** |
| 22-10-2026 13:44 | Nuevo arbitraje (catálogo) (`bb3493e4`) | válido | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 370.349* | 68.682* | 1,43* |
| | **Total de la tarea** | | | | | 1 | **370.349*** | **68.682*** | **1,43*** |
| 22-10-2026 09:10 | Mockup (`fd46e490`) | válido | Mockup | SS - GPT-6-SOL | pasarela | 41 | 3.696.191 | 975.684 | 17,15 |
| | **Total de la tarea** | | | | | 41 | **3.696.191** | **975.684** | **17,15** |
| 22-10-2026 09:00 | Backlog (`f7699afc`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 107.972 | 37.629 | 0,59 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 242.954* | 56.680* | 1,05* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 96.146 | 25.983 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **447.072*** | **120.292*** | **1,67*** |
| 20-10-2026 11:37 | Catálogo (`9a79d58e`) | válido, 3 repeticiones | A | SS - GPT-6-SOL | pasarela | 1 | 183.173 | 32.535 | 0,69 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 3 | 1.298.261* | 60.484* | 3,20* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 156.252 | 31.069 | 0,03 |
| | **Total de la tarea** | | | | | 6 | **1.637.686*** | **124.088*** | **3,92*** |
| 20-10-2026 10:11 | Catálogo (`35bccf99`) | fallido, 2 repeticiones | A | SS - GPT-6-SOL | pasarela | 1 | 185.542 | 34.195 | 0,71 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 2 | 623.503* | 7.035* | 1,32* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 157.925 | 32.295 | 0,03 |
| | **Total de la tarea** | | | | | 5 | **966.970*** | **73.525*** | **2,06*** |
| 20-10-2026 09:46 | Sugerencias de respuesta (`222209c5`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 155.133 | 18.696 | 0,50 |
| | **Total de la tarea** | | | | | 1 | **155.133** | **18.696** | **0,50** |
| 20-10-2026 09:38 | Preguntas (`86f6e36c`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 126.047 | 13.726 | 0,39 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 172.123 | 18.911 | 0,53 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 76.508 | 24.976 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **374.678** | **57.613** | **0,94** |

### ReservaSalasHito911.v2

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 26-10-2026 12:00 | Preguntas (`407bc52d`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 13.150 | 516 | 0,03 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 40.280 | 1.217 | 0,09 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 13.364 | 5.742 | 0,00 |
| | **Total de la tarea** | | | | | 3 | **66.794** | **7.475** | **0,13** |
| 09-10-2026 09:23 | Backlog (`6d9fe5ea`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 19.207 | 10.068 | 0,14 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 40.903 | 9.698 | 0,18 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 18.229 | 8.898 | 0,01 |
| | **Total de la tarea** | | | | | 3 | **78.339** | **28.664** | **0,32** |
| 09-10-2026 09:20 | Catálogo (`35d65a01`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 12.781 | 9.371 | 0,12 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 24.284 | 6.574 | 0,11 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 12.603 | 10.959 | 0,01 |
| | **Total de la tarea** | | | | | 3 | **49.668** | **26.904** | **0,24** |
| 09-10-2026 09:19 | Sugerencias de respuesta (`bf1b26b2`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 9.128 | 4.089 | 0,06 |
| | **Total de la tarea** | | | | | 1 | **9.128** | **4.089** | **0,06** |
| 09-10-2026 09:17 | Preguntas (`2c85a3b4`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 6.971 | 3.691 | 0,05 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 11.776 | 2.572 | 0,05 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 6.808 | 6.348 | 0,00 |
| | **Total de la tarea** | | | | | 3 | **25.555** | **12.611** | **0,10** |

### Banco De Alimentos

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 22-10-2026 11:48 | Mockup (`f1c8b431`) | válido | Mockup | SS - GPT-6-SOL | pasarela | 25 | 1.666.670 | 389.231 | 7,23 |
| | **Total de la tarea** | | | | | 25 | **1.666.670** | **389.231** | **7,23** |
| 16-10-2026 08:57 | Mockup (`e14d624f`) | válido | Mockup | SS - GPT-6-SOL | pasarela | 21 | 1.568.036 | 333.966 | 6,48 |
| | **Total de la tarea** | | | | | 21 | **1.568.036** | **333.966** | **6,48** |
| 16-10-2026 08:51 | Backlog (`1628caf6`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 57.696 | 22.664 | 0,34 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 105.233 | 20.964 | 0,42 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 49.392 | 19.514 | 0,01 |
| | **Total de la tarea** | | | | | 3 | **212.321** | **63.142** | **0,78** |
| 16-10-2026 08:43 | Catálogo (`294fe7bb`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 50.169 | 21.454 | 0,31 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 171.477 | 24.118 | 0,58 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 47.633 | 16.838 | 0,01 |
| | **Total de la tarea** | | | | | 3 | **269.279** | **62.410** | **0,91** |
| 16-10-2026 08:41 | Sugerencias de respuesta (`5ad41283`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 38.430 | 10.232 | 0,18 |
| | **Total de la tarea** | | | | | 1 | **38.430** | **10.232** | **0,18** |
| 16-10-2026 08:36 | Preguntas (`b2adf10a`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 29.717 | 8.366 | 0,14 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 80.577 | 10.501 | 0,27 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 28.479 | 15.077 | 0,01 |
| | **Total de la tarea** | | | | | 3 | **138.773** | **33.944** | **0,42** |

### Clinicas Veterinarias

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 19-10-2026 12:20 | Nuevo arbitraje (catálogo) (`8a12b48b`) | válido, 1 repetición | Árbitro | SS - GPT-6-SOL | pasarela | 2 | 1.879.107* | 77.418* | 4,53* |
| | **Total de la tarea** | | | | | 2 | **1.879.107*** | **77.418*** | **4,53*** |
| 19-10-2026 12:15 | Nuevo arbitraje (catálogo) (`3eb5d1a2`) | válido | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 414.181* | 56.086* | 1,39* |
| | **Total de la tarea** | | | | | 1 | **414.181*** | **56.086*** | **1,39*** |
| 19-10-2026 12:10 | Nuevo arbitraje (catálogo) (`41e09ae7`) | válido | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 413.825* | 57.650* | 1,40* |
| | **Total de la tarea** | | | | | 1 | **413.825*** | **57.650*** | **1,40*** |
| 16-10-2026 13:36 | Nuevo arbitraje (catálogo) (`cfa07ef9`) | fallido, 1 repetición | Árbitro | SS - GPT-6-LUNA | pasarela | 2 | 492.460* | 47.693* | 0,07* |
| | **Total de la tarea** | | | | | 2 | **492.460*** | **47.693*** | **0,07*** |
| 16-10-2026 13:05 | Nuevo arbitraje (catálogo) (`561edb20`) | fallido, 2 repeticiones | Árbitro | SS - GPT-6-LUNA | pasarela | 3 | 665.013* | 86.438* | 0,11* |
| | **Total de la tarea** | | | | | 3 | **665.013*** | **86.438*** | **0,11*** |
| 16-10-2026 12:39 | Nuevo arbitraje (catálogo) (`c12ed285`) | válido, 1 repetición | Árbitro | SS - GPT-6-LUNA | pasarela | 2 | 567.402* | 96.231* | 0,10* |
| | **Total de la tarea** | | | | | 2 | **567.402*** | **96.231*** | **0,10*** |
| 16-10-2026 12:31 | Nuevo arbitraje (catálogo) (`653bd966`) | válido | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 448.676* | 68.420* | 1,58* |
| | **Total de la tarea** | | | | | 1 | **448.676*** | **68.420*** | **1,58*** |
| 16-10-2026 12:22 | Nuevo arbitraje (catálogo) (`f9976759`) | válido | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 681.916* | 61.261* | 1,98* |
| | **Total de la tarea** | | | | | 1 | **681.916*** | **61.261*** | **1,98*** |
| 16-10-2026 12:19 | Nuevo arbitraje (preguntas) (`ad3aed77`) | válido | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 208.313 | 17.970 | 0,60 |
| | **Total de la tarea** | | | | | 1 | **208.313** | **17.970** | **0,60** |
| 16-10-2026 12:16 | Nuevo arbitraje (preguntas) (`5489fee1`) | válido | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 175.635 | 17.602 | 0,53 |
| | **Total de la tarea** | | | | | 1 | **175.635** | **17.602** | **0,53** |
| 16-10-2026 12:15 | Nuevo arbitraje (preguntas) (`f688640b`) | fallido | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 23.614* | 1.742* | 0,06* |
| | **Total de la tarea** | | | | | 1 | **23.614*** | **1.742*** | **0,06*** |
| 16-10-2026 12:10 | Nuevo arbitraje (preguntas) (`beb74e28`) | válido, 1 repetición | Árbitro | SS - GPT-6-LUNA | pasarela | 2 | 254.668 | 38.570 | 0,04 |
| | **Total de la tarea** | | | | | 2 | **254.668** | **38.570** | **0,04** |
| 16-10-2026 10:39 | Nuevo arbitraje (catálogo) (`0c660a9f`) | fallido | Árbitro | SS - GPT-6-LUNA | pasarela | 1 | 557.206* | 41.757* | 0,08* |
| | **Total de la tarea** | | | | | 1 | **557.206*** | **41.757*** | **0,08*** |
| 16-10-2026 10:20 | Nuevo arbitraje (catálogo) (`d584e8b4`) | fallido | Árbitro | SS - GPT-6-LUNA | pasarela | 1 | 382.753* | 42.261* | 0,06* |
| | **Total de la tarea** | | | | | 1 | **382.753*** | **42.261*** | **0,06*** |
| 16-10-2026 10:11 | Nuevo arbitraje (preguntas) (`b8964d6d`) | válido | Árbitro | SS - GPT-6-LUNA | pasarela | 1 | 299.163 | 49.664 | 0,05 |
| | **Total de la tarea** | | | | | 1 | **299.163** | **49.664** | **0,05** |
| 16-10-2026 09:42 | Nuevo arbitraje (catálogo) (`8bc75123`) | válido, 1 repetición | Árbitro | SS - GPT-6-LUNA | pasarela | 2 | 726.818* | 98.122* | 0,12* |
| | **Total de la tarea** | | | | | 2 | **726.818*** | **98.122*** | **0,12*** |
| 16-10-2026 09:37 | Nuevo arbitraje (preguntas) (`bd0772fb`) | válido | Árbitro | SS - GPT-6-LUNA | pasarela | 1 | 198.375 | 23.777 | 0,03 |
| | **Total de la tarea** | | | | | 1 | **198.375** | **23.777** | **0,03** |
| 16-10-2026 08:31 | Mockup (`e1cffff0`) | válido | Mockup | SS - GPT-6-SOL | pasarela | 1 | 68.530 | 18.906 | 0,33 |
| | **Total de la tarea** | | | | | 1 | **68.530** | **18.906** | **0,33** |
| 15-10-2026 12:38 | Mockup (`ca41356d`) | fallido, 4 repeticiones | Mockup | SS - GPT-6-SOL | pasarela | 43 | 3.215.390 | 869.889 | 15,13 |
| | **Total de la tarea** | | | | | 43 | **3.215.390** | **869.889** | **15,13** |
| 15-10-2026 12:27 | Backlog (`223f1efe`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 119.227 | 30.249 | 0,54 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 165.307* | 53.265* | 0,86* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 92.514 | 30.037 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **377.048*** | **113.551*** | **1,43*** |
| 15-10-2026 12:11 | Catálogo (`2b5e1fe9`) | válido, 2 repeticiones | A | SS - GPT-6-SOL | pasarela | 2 | 319.963* | 67.772* | 1,32* |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 387.019* | 54.536* | 1,32* |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 309.608 | 59.381 | 0,06 |
| | **Total de la tarea** | | | | | 5 | **1.016.590*** | **181.689*** | **2,70*** |
| 15-10-2026 11:01 | Sugerencias de respuesta (`a83591b2`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 97.511 | 15.900 | 0,35 |
| | **Total de la tarea** | | | | | 1 | **97.511** | **15.900** | **0,35** |
| 15-10-2026 10:52 | Preguntas (`9637d55f`) | válido, 2 repeticiones | A | SS - GPT-6-SOL | pasarela | 2 | 283.397 | 21.932 | 0,79 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 646.125 | 21.028 | 1,50 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 162.672 | 19.699 | 0,03 |
| | **Total de la tarea** | | | | | 5 | **1.092.194** | **62.659** | **2,31** |

### Proyecto interno 3

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 15-10-2026 09:14 | Mockup (`f01ed1b0`) | válido, 1 repetición | Mockup | SS - GPT-6-SOL | pasarela | 28 | 3.465.323 | 548.080 | 12,41 |
| | **Total de la tarea** | | | | | 28 | **3.465.323** | **548.080** | **12,41** |
| 15-10-2026 09:02 | Mockup (`8e2c813c`) | fallido | Mockup | SS - GPT-6-SOL | pasarela | 6 | 472.581* | 85.443* | 1,80* |
| | **Total de la tarea** | | | | | 6 | **472.581*** | **85.443*** | **1,80*** |
| 15-10-2026 08:58 | Mockup (`cbe99ccc`) | válido | Mockup | SS - GPT-6-SOL | pasarela | 1 | 149.497 | 17.248 | 0,47 |
| | **Total de la tarea** | | | | | 1 | **149.497** | **17.248** | **0,47** |
| 14-10-2026 13:57 | Mockup (`9c36e34e`) | fallido | Mockup | SS - GPT-6-SOL | pasarela | 21 | 2.757.262 | 419.506 | 9,71 |
| | **Total de la tarea** | | | | | 21 | **2.757.262** | **419.506** | **9,71** |
| 14-10-2026 13:10 | Mockup (`f31316bd`) | fallido | Mockup | SS - GPT-6.1-SOL | pasarela | 1 | ≥ 0 | ≥ 0 | ≥ 0,00 |
| | **Total de la tarea** | | | | | 1 | **≥ 0** | **≥ 0** | **≥ 0,00** |
| 14-10-2026 12:52 | Backlog (`61711593`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 97.957 | 31.547 | 0,51 |
|  | |  | Árbitro | SS - GPT-6.1-SOL | pasarela | 1 | ≥ 0 | ≥ 0 | ≥ 0,00 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 66.031 | 21.209 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **≥ 163.988** | **≥ 52.756** | **≥ 0,53** |
| 14-10-2026 12:10 | Catálogo (`ec421e86`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 139.632 | 24.146 | 0,52 |
|  | |  | Árbitro | SS - GPT-6.1-SOL | pasarela | 1 | 890.180 | 51.988 | 2,30 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 130.649 | 24.197 | 0,03 |
| | **Total de la tarea** | | | | | 3 | **1.160.461** | **100.331** | **2,85** |
| 14-10-2026 11:56 | Catálogo (`0a315c78`) | fallido, 1 repetición | A | SS - GPT-6.1-SOL | pasarela | 1 | ≥ 0 | ≥ 0 | ≥ 0,00 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 294.419 | 34.108 | 0,05 |
| | **Total de la tarea** | | | | | 3 | **≥ 294.419** | **≥ 34.108** | **≥ 0,05** |
| 14-10-2026 10:20 | Catálogo (`1383d034`) | fallido, 1 repetición | A | SS - GPT-6-SOL | pasarela | 1 | 231.528 | 24.017 | 0,70 |
|  | |  | Árbitro | SS - GPT-6.1-SOL | pasarela | 1 | ≥ 0 | ≥ 0 | ≥ 0,00 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 323.521 | 46.658 | 0,06 |
| | **Total de la tarea** | | | | | 4 | **≥ 555.049** | **≥ 70.675** | **≥ 0,76** |
| 14-10-2026 10:00 | Catálogo (`bad9f2cf`) | válido, 1 repetición | A | SS - GPT-6-SOL | pasarela | 1 | 233.791 | 26.764 | 0,74 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 319.892 | 52.275 | 0,06 |
| | **Total de la tarea** | | | | | 3 | **553.683** | **79.039** | **0,79** |
| 14-10-2026 09:53 | Catálogo (`f06978d7`) | válido, 1 repetición | A | SS - GPT-6-SOL | pasarela | 1 | 230.493 | 21.530 | 0,68 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 303.249 | 38.948 | 0,05 |
| | **Total de la tarea** | | | | | 3 | **533.742** | **60.478** | **0,73** |
| 14-10-2026 09:38 | Catálogo (`439142e1`) | fallido, 1 repetición | A | SS - GPT-6-SOL | pasarela | 1 | 136.236 | 21.134 | 0,48 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 1.591.624 | 9.469 | 3,28 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 297.712 | 37.177 | 0,05 |
| | **Total de la tarea** | | | | | 4 | **2.025.572** | **67.780** | **3,81** |
| 14-10-2026 09:28 | Sugerencias de respuesta (`cb90639f`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 209.329 | 11.014 | 0,53 |
| | **Total de la tarea** | | | | | 1 | **209.329** | **11.014** | **0,53** |
| 14-10-2026 09:22 | Preguntas (`90bdbb39`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 191.404 | 13.370 | 0,52 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 230.187 | 12.719 | 0,59 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 105.501 | 16.074 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **527.092** | **42.163** | **1,12** |

### Proyecto interno 2

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 13-10-2026 12:04 | Mockup (`1771f025`) | válido | Mockup | claude-sonnet-5-5 | suscripción del autor | 1 | 1.489.928 | 203.848 | sin precio |
| | **Total de la tarea** | | | | | 1 | **1.489.928** | **203.848** | **sin precio** |
| 13-10-2026 10:36 | Mockup (`cac79c24`) | fallido | Mockup | claude-sonnet-5-5 | suscripción del autor | 1 | 748.341 | 184.645 | sin precio |
| | **Total de la tarea** | | | | | 1 | **748.341** | **184.645** | **sin precio** |
| 13-10-2026 09:29 | Mockup (`e6aa5827`) | fallido | Mockup | claude-sonnet-5-5 | suscripción del autor | 1 | 220.862 | 256.408 | sin precio |
| | **Total de la tarea** | | | | | 1 | **220.862** | **256.408** | **sin precio** |
| 13-10-2026 09:08 | Backlog (`fd7a13c0`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 135.601 | 25.559 | 0,53 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 406.428 | 30.615 | 1,12 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 81.738 | 18.648 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **623.767** | **74.822** | **1,66** |
| 13-10-2026 08:58 | Catálogo (`9eb2241c`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 167.722 | 22.179 | 0,56 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 563.544 | 33.312 | 1,46 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 109.511 | 27.113 | 0,02 |
| | **Total de la tarea** | | | | | 3 | **840.777** | **82.604** | **2,04** |
| 13-10-2026 08:55 | Sugerencias de respuesta (`6e05401d`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 148.823 | 11.840 | 0,42 |
| | **Total de la tarea** | | | | | 1 | **148.823** | **11.840** | **0,42** |
| 13-10-2026 08:50 | Preguntas (`41d4d558`) | válido | A | SS - GPT-6-SOL | pasarela | 1 | 132.358 | 11.061 | 0,38 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 91.548 | 8.755 | 0,27 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 1 | 84.504 | 8.659 | 0,01 |
| | **Total de la tarea** | | | | | 3 | **308.410** | **28.475** | **0,66** |

### Proyecto interno 1

| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 13-10-2026 08:44 | Mockup (`8e461594`) | fallido | Mockup | claude-sonnet-5-5 | suscripción del autor | 1 | ≥ 0 | ≥ 0 | sin precio |
| | **Total de la tarea** | | | | | 1 | **≥ 0** | **≥ 0** | **sin precio** |
| 09-10-2026 13:08 | Mockup (`ebba7d49`) | fallido | Mockup | claude-sonnet-5-5 | suscripción del autor | 1 | ≥ 0 | ≥ 0 | sin precio |
| | **Total de la tarea** | | | | | 1 | **≥ 0** | **≥ 0** | **sin precio** |
| 09-10-2026 13:01 | Backlog (`f19c4295`) | válido, 1 repetición | A | SS - GPT-6-SOL | pasarela | 1 | 64.751 | 20.604 | 0,34 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 1 | 193.012 | 23.274 | 0,62 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 139.617 | 33.540 | 0,03 |
| | **Total de la tarea** | | | | | 4 | **397.380** | **77.418** | **0,99** |
| 09-10-2026 12:09 | Catálogo (`7bb7a6a6`) | válido, 2 repeticiones | A | SS - GPT-6-SOL | pasarela | 1 | 156.317 | 15.201 | 0,46 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 2 | 377.958 | 41.216 | 1,17 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 189.134 | 41.578 | 0,04 |
| | **Total de la tarea** | | | | | 5 | **723.409** | **97.995** | **1,67** |
| 09-10-2026 12:04 | Sugerencias de respuesta (`6b834260`) | válido | Sugerencias | SS - GPT-6-SOL | pasarela | 1 | 142.409 | 7.741 | 0,36 |
| | **Total de la tarea** | | | | | 1 | **142.409** | **7.741** | **0,36** |
| 09-10-2026 11:59 | Preguntas (`390a47e0`) | válido, 2 repeticiones | A | SS - GPT-6-SOL | pasarela | 1 | 130.916 | 12.410 | 0,39 |
|  | |  | Árbitro | SS - GPT-6-SOL | pasarela | 2 | 175.457 | 26.447 | 0,62 |
|  | |  | B | SS - GPT-6-LUNA | pasarela | 2 | 101.559 | 11.468 | 0,02 |
| | **Total de la tarea** | | | | | 5 | **407.932** | **50.325** | **1,02** |
