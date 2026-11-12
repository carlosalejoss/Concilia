# Paso 130: elegir el modelo B con el proyecto de clínicas

Pruebas del 11 y el 12-11-2026, con los prompts de siempre (v0.12). A, el árbitro y las sugerencias, SOL 6 por la pasarela. **La elección es del autor** (sección «Recomendación»).

**Candidatos:** Kimi K3.1 Pro y Gemini 4 Argon (paso 127). Claude Haiku 5.5 era la reserva y no entra: Kimi y Argon funcionaron en la prueba de humo (paso 129). LUNA es la referencia.

## Cómo se hizo

- **Preguntas:** una ronda de clínicas (21 archivos) por candidato, cada una en su proyecto: «Clínicas B Kimi» y «Clínicas B Argon», con el nombre del sistema «la Plataforma Huellas».
  - **Referencia con LUNA:** la ronda de «Clínicas SOL 6» del paso 145 (11-11-2026), con los mismos prompts y la misma configuración de A y del árbitro.
- **Catálogo:** los tres en «Clínicas SOL 6», con las mismas 45 preguntas respondidas: Kimi, LUNA y Argon como B.
- **Cambio de B:** recreando la API con `RUNNER_MODEL_B` (y `RUNNER_PROVIDER_B=LiteLlm` para LUNA) en el entorno, sin tocar `.env`, siempre sin nada en marcha.
- **Antes de cada tarea con B,** su coste estimado frente a lo que quedaba de cada límite de Go. Todo cupo.
- Es una sola ejecución de cada uno: una señal, no una prueba. La evaluación 2 lo medirá con repeticiones y con la referencia del autor.

## El incidente de Argon y su arreglo

**El primer catálogo de Argon** (12-11, de 08:44 a 09:06) no llegó a terminar:
- leyó toda la documentación, pero la respuesta en la que escribe el catálogo se cortaba cada 5 minutos y Claude Code la reintentaba (08:52, 08:57 y 09:02). Se paró a mano;
- **los intentos cortados se pagan:** según la consola de OpenCode, unos 44.000 tokens de salida cada uno, 1,71 $ del límite (3,42 $ a precio de lista) sin ningún resultado. Va como ajuste manual en `precios.json` (`_go.ajustes`), porque la plataforma no puede registrarlo.

**La causa,** según la documentación de Claude Code («Streaming idle watchdogs»):
- con una pasarela propia (`ANTHROPIC_BASE_URL`), Claude Code aborta una respuesta si pasan 5 minutos sin recibir ningún byte;
- Argon, por la ruta de Google de OpenCode, no envía nada mientras genera, ni texto ni los *pings* que mantienen viva la conexión.

**El arreglo,** en `OpenCodeGoProvider`, solo para los procesos de OpenCode Go:
- `CLAUDE_STREAM_IDLE_TIMEOUT_MS=900000` (15 minutos, el mismo tiempo sin salida que ya da el runner a una fase) y `API_FORCE_IDLE_TIMEOUT=0`;
- los procesos de la pasarela (SOL y LUNA) no cambian;
- las condiciones de la comparación tampoco: mismos prompts, contexto y tope de salida.

Con el arreglo, **el catálogo de Argon terminó válido**, pero tardó 29,1 minutos, a punto del tope de 30.

## Resultados

### Ronda de preguntas

| | Kimi | Argon | LUNA (referencia) |
|---|---|---|---|
| Resultado | válido a la primera | válido a la primera | válido, con 1 reparación de B |
| Minutos del trabajo (de B) | 14,1 (11,3) | 8,9 (5,6) | 7,6 (4,7) |
| Preguntas de B | 35 | 17 | 11 |
| Unión: de ambos / solo A / solo B | 59: 22 / 24 / **13** | 58: 19 / 31 / 8 | 45: 10 / 34 / 1 |
| Solo de B según los embeddings | 11 | 6 | 2 |
| Acuerdo del árbitro con los embeddings | 82 % | 79 % | 93 % |
| Coste de B, precio de lista | 0,32 $ | 0,72 $ (0,36 $ del límite) | 0,02 $ |
| Coste del trabajo para una empresa | 0,89 $ | 1,31 $ | 1,01 $ |

### Catálogo (mismas 45 respuestas)

| | Kimi | Argon (con el arreglo) | LUNA (referencia) |
|---|---|---|---|
| Resultado | válido a la primera | válido a la primera (tras el incidente) | válido a la primera |
| Minutos del trabajo (de B) | 21,8 (15,4) | 29,1 (24,3) | 14,6 (8,5) |
| Requisitos de A / de B | 270 / 225 | 253 / **107** | 260 / 208 |
| Unión | 223 | 144 | 235 |
| De ambos / solo A / solo B | **210 / 6** / 7 | 105 / 37 / 2 | 187 / 41 / 7 |
| Solo de B según los embeddings | 5 | 0 | 0 |
| Acuerdo del árbitro con los embeddings | **95,8 %** | 86,4 % | 91,9 % |
| Discrepancias / desviaciones del árbitro | 1 / 11 | 2 / 9 | 2 / 3 |
| Correcciones automáticas en la salida de B | ninguna | ninguna | `REF-LINEA` |
| Compactaciones de B | ninguna | 1 | ninguna |
| Tokens de B (entrada / salida) | 132.410\* / 34.240\* | 117.950 / 89.651 | 115.253 / 36.279 |
| Coste de B, precio de lista | 0,61 $\* (0,53 $ en la consola de OpenCode) | 2,19 $ (1,10 $ del límite) | 0,03 $ |
| Coste del trabajo para una empresa | 2,19 $\* | 3,75 $\* | 1,87 $\* |

`*`: con ejecuciones detenidas al compactar tras escribir su salida, que no tienen dato de consumo y se estiman (paso 95).

**A Argon hay que sumarle el intento fallido:** 3,42 $ a precio de lista (1,71 $ del límite) más los 0,50 $ de la parte de SOL de ese trabajo.

### Revisión rápida de una muestra

Ocho requisitos de B de cada catálogo, elegidos al azar con semilla fija (130), comprobados contra el texto de la documentación del proyecto.

- **Kimi.** Los 8 se apoyan en la documentación:
  - la autorización previa de Mascota Protegida para cirugías;
  - la tarea «retrasada» a los 30 minutos, con aviso en la sala y en la tableta;
  - el lector de microchip USB que actúa como teclado;
  - la analítica previa de la limpieza dental;
  - la migración de las alergias y de las alertas clínicas;
  - la auditoría de precios, facturas y stock;
  - el cifrado y el IVA.

  Requisitos atómicos, en EARS y en buen español.
- **Argon.** Los 8 se apoyan en la documentación:
  - el descuento del lote que antes caduca (FEFO);
  - la sesión de recepción a los 15 minutos;
  - los 2 segundos con 60 usuarios;
  - la API de LabVet, con una consulta cada 10 minutos por clínica;
  - la auditoría durante dos años;
  - la aprobación de los traspasos de estupefacientes.

  Pero son muy largos: un requisito junta varias reglas (descontar el lote, cargar al cliente y convertir unidades; auditar accesos, el motivo de acceder a otra clínica y la conservación). Eso explica que escriba 107 requisitos, la mitad que los demás, y que 37 requisitos de SOL se queden sin pareja. Buen español.
- **LUNA.** Cinco de ocho se apoyan en la documentación (planes, restauración de copias cada trimestre, 2 segundos, cancelación de un plan antes de 12 meses, consentimiento comercial al migrar). Tres son dudas abiertas convertidas en requisito («mantener pendiente de definición si…»), y uno menciona un «almacén separado» del hospital que no aparece en la documentación. Puede venir de una respuesta; no se ha comprobado.

## Lo que dicen los datos

- **Kimi K3.1 Pro es el que mejor hace de B.**
  - Aporta en las preguntas: 13 solo suyas, frente a 8 de Argon y 1 de LUNA.
  - En el catálogo cubre casi todo lo que ve SOL: 6 requisitos sin pareja, frente a 37 de Argon y 41 de LUNA. El árbitro sigue mejor el emparejamiento (95,8 %), y sus requisitos son atómicos y fieles a la fuente.
  - Es el más barato de los dos: 0,61 $ de B por catálogo, frente a 2,19 $.
  - Su punto débil es la velocidad: 15,4 minutos de B en el catálogo, frente a 8,5 de LUNA.
- **Gemini 4 Argon** es fiel a la fuente y no inventa, pero:
  - junta varias reglas en cada requisito: 107, la mitad, y cuesta emparejarlos con los de SOL;
  - razona mucho, casi 90.000 tokens de salida en el catálogo: es 3,6 veces más caro que Kimi en B y el más lento (24,3 minutos de B, el trabajo a punto del tope);
  - necesitó el arreglo del tiempo de espera, y su primer intento costó 3,42 $ a precio de lista sin resultado;
  - y su precio en el límite se dobla el 30-11-2026, cuando acaba el descuento.
- **LUNA,** la referencia, confirma por qué se cambia: aporta casi nada propio y deja sin cubrir 41 requisitos de SOL.

## Recomendación de Claude

**Kimi K3.1 Pro** como modelo B. Cumple la condición de capacidad, es el que más aporta a la verificación cruzada y el más barato de los dos. Su coste en el flujo para una empresa queda en unos 9–10 $ en las fases de preguntas, sugerencias, catálogo y backlog, frente a unos 6,8 $ con LUNA (estimación del paso 127; el paso 131 da la cifra real). La lentitud se vigila: si un catálogo grande rozara los 30 minutos, se sube el tiempo total en la configuración.

## Decisión

**El autor elige Kimi K3.1 Pro (`kimi-k3.1-pro`) como modelo B (12-11-2026),** como recomendaba Claude.
- **Configuración:** la de su `.env` (`RUNNER_PROVIDER_B=OpenCodeGo` y `RUNNER_MODEL_B=kimi-k3.1-pro`).
- **Argon y Haiku** quedan descartados. Sus límites de Go no se vuelven a usar.
- **Siguiente:** el paso 131, el flujo de clínicas con Kimi frente al de LUNA.

## Coste del paso 130

**OpenCode Go, a precio de lista:**
- Kimi: 0,32 $ (preguntas) + 0,53 $ (catálogo, según la consola) = 0,85 $;
- Argon: 0,72 $ (preguntas) + 3,42 $ (intento cortado) + 2,19 $ (catálogo) = 6,33 $, unos 3,17 $ del límite con el descuento;
- la ronda de Argon que se paró el 11-11 por la tarde, unos céntimos.

**Pasarela:** las partes de SOL de cada trabajo y el catálogo con LUNA, unos 6,7 $. Detalle en `docs/costes/consumo.md`.

## En palabras sencillas

Se probaron los dos candidatos haciendo el mismo trabajo que LUNA, partiendo de lo mismo.
- **Kimi** encontró más preguntas que nadie y, en el catálogo, cubrió casi todo lo que veía el modelo principal, con requisitos claros y fieles a los documentos. Es más lento, pero barato.
- **Gemini** no terminaba el catálogo, porque tardaba tanto pensando que el programa daba la conexión por perdida. Con un ajuste lo terminó, pero tardó casi 30 minutos, costó más del triple y metió varias reglas en cada requisito, así que su catálogo encaja peor con el del modelo principal.

Por eso se recomienda Kimi.
