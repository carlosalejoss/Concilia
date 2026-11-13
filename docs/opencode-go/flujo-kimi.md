# Paso 131: el flujo de clínicas con Kimi K3.1 Pro como modelo B, frente a LUNA

Pruebas del 11 y el 12-11-2026, con los prompts de siempre (v0.12):
- **la configuración:** A, el árbitro y las sugerencias con SOL 6 por la pasarela, y B con Kimi K3.1 Pro (`kimi-k3.1-pro`) por OpenCode Go, el modelo elegido en el paso 130;
- **sin mockup:** es de SOL y no depende de B.

## Cómo se hizo

**Flujo con Kimi,** en «Clínicas B Kimi» (21 archivos, sistema «la Plataforma Huellas»):
1. la ronda de preguntas, la misma del paso 130;
2. las sugerencias, guardadas tal cual (59 suposiciones);
3. el catálogo, validado sin editar: solo se marcaron como revisadas sus 7 discrepancias;
4. el backlog.

**Referencia con LUNA, fase a fase.** No hay un flujo completo con LUNA, la v0.12 y la misma documentación: el del 03-11 en «Clinicas Veterinarias v0.11» incluye la segunda iteración del paso 116. Por eso se usa:
- **preguntas, sugerencias y catálogo:** los de «Clínicas SOL 6» (pasos 145 y 130), con la v0.12, la misma documentación y A, árbitro y sugerencias con SOL 6;
- **backlog:** el del 23-10 en «Clinicas Veterinarias v0.11», con la v0.12 y solo la iteración 1. Parte de otro catálogo validado (235 requisitos), así que sus cifras de contenido no se comparan una a una.

**Límites:**
- Es una ejecución de cada uno.
- Las respuestas difieren: 59 preguntas con Kimi frente a 45 con LUNA, porque Kimi hizo más preguntas.
- Las duraciones dependen de la carga de la pasarela y de OpenCode.
- La evaluación 2 lo medirá con repeticiones.

**Control del límite de Go:** antes de cada tarea, su coste estimado frente a lo que quedaba.
- **Lo que se descubrió:** el límite de Go es uno solo para toda la cuenta, y cada modelo gasta de él su coste dividido por su límite mensual. Las ventanas son fijas. `consumo.md` lo calcula así desde el 12-11-2026 (commit `c620db8`), y cuadra con la consola de OpenCode.
- **Por eso el backlog se lanzó a las 13:39,** tras reiniciarse la ventana de 5 horas: en la anterior quedaba menos de lo que costaba.

## Resultados, fase a fase

Coste para una empresa que pagase por uso: la caché a su precio y a precio de lista, también B, que se pagó con la suscripción Go. `*`: con ejecuciones detenidas al compactar tras escribir su salida, estimadas (paso 95).

| Fase | | Kimi como B | LUNA como B |
|---|---|---|---|
| **Preguntas** | resultado | válido a la primera | válido, B con 1 reparación |
| | minutos del trabajo (de B) | 14,1 (11,3) | 7,6 (4,7) |
| | preguntas de B / de la unión | 35 / 59 | 11 / 45 |
| | de ambos / solo A / solo B | 22 / 24 / **13** | 10 / 34 / 1 |
| | coste del trabajo (de B) | 0,89 $ (0,32 $) | 1,01 $ (0,02 $) |
| **Sugerencias** (solo SOL) | minutos | 2,7 | 2,3 |
| | preguntas / coste | 59 / 0,28 $ | 45 / 0,42 $ |
| **Catálogo** | resultado | válido a la primera | válido a la primera, B con `REF-LINEA` |
| | minutos del trabajo (de B) | 25,8 (20,3) | 14,6 (8,5) |
| | requisitos de A / de B / de la unión | 265 / 223 / 228 | 260 / 208 / 235 |
| | de ambos / solo A / solo B | 204 / **7** / 17 | 187 / 41 / 7 |
| | acuerdo del árbitro con los embeddings | **95,7 %** | 91,9 % |
| | discrepancias / desviaciones | 1 / 6 | 2 / 3 |
| | coste del trabajo (de B) | 2,18 $\* (0,61 $\*) | 1,87 $\* (0,03 $) |
| **Backlog** | resultado | **fallido: el árbitro no cabe** (3 intentos) | válido a la primera |
| | minutos del trabajo (de B) | 25,3 (22,0) | 6,7 (3,3) |
| | historias de A / de B / de la unión | 140 / 105 / — | 96 / 85 / 94 |
| | de ambos / solo A / solo B | — | 72 / 14 / 8 |
| | coste del trabajo (de B) | 2,22 $\* (0,81 $\*) | 1,38 $ (0,03 $) |

## Conclusiones

- **Kimi aporta de verdad:**
  - en las preguntas, 13 solo suyas frente a 1;
  - en el catálogo, 17 requisitos solo suyos, y solo 7 de SOL se quedan sin pareja (con LUNA, 41).
- **Más fiable:** ninguna reparación ni corrección automática en la salida de B.
- **El coste para una empresa no sube tanto como se temía:**
  - las preguntas salen incluso más baratas (0,89 $ frente a 1,01 $), porque el árbitro y las sugerencias trabajaron con menos;
  - el catálogo sube unos 0,30 $.
- **El tiempo sí sube:** preguntas y catálogo tardan casi el doble (40 minutos frente a 22), por la lentitud de Kimi.
- **El backlog no termina:** A y B salen válidos, pero el árbitro de la v0.12 no cabe en 128.000 tokens (ver «Backlog»). Con los prompts de siempre, el flujo con Kimi se queda en el catálogo validado. Lo arregla el árbitro que lee solo los requisitos que necesita, en los prompts v0.14 (paso 143).

## Revisión rápida del catálogo de Kimi

Ocho requisitos de B al azar (semilla 131), comprobados contra la documentación:
- **Se apoyan en ella:**
  - las visitas a ingresados (12–13 y 18–19 h, dos personas);
  - el libro de registro de estupefacientes;
  - el segundo factor para gerencia y contabilidad;
  - el máximo de dos citas por la app;
  - las recetas en PDF;
  - la dosis administrada en hospitalización.
- **No aparecen en los documentos:** las facturas separadas entre varios titulares. Probablemente vienen de las respuestas a las preguntas, que también son fuente.

Requisitos atómicos y en buen español.

Lo que aporta solo Kimi (17) son detalles reales de la documentación que A no recogió:
- el triaje inicial de recepción;
- las pautas de vacunación de gatos, conejos y hurones;
- la conservación y prueba de las copias;
- las impresoras;
- la cancelación en LabVet;
- las tarifas y el catálogo inicial de productos.

Las tres de tarifas y productos son largas (cargan muchos precios a la vez).

## Backlog

Lanzado el 12-11-2026 a las 13:39, con el catálogo validado de 228 requisitos.

- **A (SOL)** terminó en 4,0 minutos, con 140 historias.
- **B (Kimi)** terminó en 22,0 minutos, con 105 historias. Escribió su salida y la plataforma lo paró al empezar a compactar, con la salida válida (sus tokens son estimados).
- **El árbitro (SOL) falló tres veces,** en 3 minutos cada vez:
  - **intento 1:** leyó todas sus entradas, compactó el contexto y terminó sin escribir `output/backlog.json` ni el veredicto;
  - **intentos 2 y 3:** la plataforma los paró al empezar a compactar por segunda vez sin haber escrito la salida (paso 92).

**Hallazgo: el árbitro del backlog de la v0.12 no cabe con las propuestas de Kimi, aunque el catálogo sea menor que en el paso 145.** Lee entero el catálogo validado, las dos propuestas y el emparejamiento, además de su prompt con las respuestas. En cuanto termina de leerlo, el contexto se llena y Claude Code compacta.

Lo que leyó antes de compactar, en líneas de cada fichero (la consola no guarda el texto entero de lo leído):

| Entrada | Con Kimi (fallido) | Con LUNA (23-10, válido) |
|---|---|---|
| catálogo validado | 232 (228 requisitos) | 239 (235 requisitos) |
| propuesta de A / de B | 235 / 193 | 205 / 173 |
| emparejamiento | 168 | 122 |
| respuestas en el prompt | 59 | 45 |

- **El catálogo no es lo que cambia:** con LUNA era incluso algo mayor (235 requisitos frente a 228). Cambian las propuestas, sobre todo la de B, y con ellas el emparejamiento.
- **Los dos estaban al borde:** con LUNA cupo, y con unas 80 líneas más ya no.
- **Por eso el aviso que se puso en el paso 145** (avisar con más de unos 250 requisitos) **no sirve:** el tamaño del catálogo no predice si el árbitro cabe. Lo que hay que mirar es todo lo que lee, y eso solo se sabe después de A y B. Corregido en el plan.

**Coste del intento:** 2,22 $ para una empresa (0,45 $ de A, 0,96 $ del árbitro y 0,81 $ de Kimi), todo estimado salvo A. Con la caché a su precio.

**No se repitió:** un nuevo arbitraje recibiría las mismas entradas y lo más probable es que fallase igual, y no deja el backlog en el proyecto. El backlog con Kimi se mide en el paso 143, con el árbitro que lee solo los requisitos que necesita.

## Además: el árbitro del backlog y las historias atómicas

**No se pudo medir** («Después», punto 7, y paso 141): sin unión no hay historias que junten varias de una misma propuesta. Con el mismo método, el backlog de referencia con LUNA junta 10 historias de A (11 %) y 5 de B (5 %), las cifras del paso 104. Se mide en el paso 143.
