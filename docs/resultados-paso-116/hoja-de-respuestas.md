# Paso 116: hoja de respuestas del cliente y reglas de la prueba

Escrita por Claude el 27-10-2026, **antes de lanzar ninguna ejecución** de la prueba. Se sube en un commit propio para que conste. Sirve para que las respuestas y las revisiones sean las mismas en los dos caminos (actualizar y desde cero) y no dependan de lo que propongan los modelos. Las preguntas y los cambios esperados son los de [`ejemplos/README.md`](../../ejemplos/README.md), «Grande: segunda iteración».

## Respuestas del cliente

Cuando una pregunta trata de uno de estos temas, se responde con este texto, aunque esté formulada de otra manera:

| Tema | Respuesta |
| --- | --- |
| I2-1, detalles de la teleconsulta | Lo que dice el correo de gerencia: solo seguimientos, visita presencial con esa mascota en los 30 días anteriores, 15 minutos, huecos que elige cada veterinario, pago al reservar en la app y sin grabación. |
| I2-2, detalles de los pedidos | Lo que dice el correo: alimentación e higiene, pago en la app, la clínica lo guarda 48 horas y, si no se recoge, se anula y se devuelve el dinero; ningún medicamento se vende por la app. |
| I2-3, Huellas Puerto | Abre en enero de 2027, sin hospital ni urgencias, con las tarifas de Huellas Centro, y debe estar en la plataforma desde el primer día como las demás. |
| I2-5, plazo para cancelar | Vale lo del acta de seguimiento: 24 horas antes de la cita desde la app. Sustituye a las 12 horas. Por teléfono, como hasta ahora. |
| I2-6, señal | 10 € a partir de la segunda falta sin avisar en los últimos seis meses; se paga al reservar la siguiente cita (app o recepción) y se descuenta de la factura de esa visita. |
| I2-7, segundo factor | Obligatorio para todo el personal, dentro y fuera de las clínicas; para los clientes, nada. |
| I2-8, hospitalización | Sí: pasa a la primera fase, solo en Huellas Centro. |
| I2-9, urgencias nocturnas y festivos | 100 € desde el 1 de enero de 2027; hasta entonces, 95 €. |
| I2-10, precio de la teleconsulta | 25 € en general y 20 € para los clientes con un plan de salud activo. |
| I2-11, conservación de la historia clínica | Se sigue el informe de la asesora legal: identificable cinco años desde la última visita y después anonimizada, como dice el informe. |
| I2-12, horario de Huellas Puerto | De lunes a sábado: de lunes a viernes, de 9:00 a 20:00, y los sábados, de 9:00 a 14:00. |
| I2-13, cita para varias mascotas | Confirmado: no hay reserva conjunta; cada mascota tiene su cita. |
| I2-14, fotos en el informe | Confirmado: el informe diario de hospitalización es solo texto, sin fotos. |
| I2-15, resultados de la encuesta | Los ve gerencia, de todas las clínicas, y el responsable de cada clínica, solo los de la suya. |
| I2-16, teleconsulta y plan de salud | No está incluida en los planes: se paga aparte, con el precio para clientes con plan (20 €). |

## Preguntas que no están en la hoja

1. Si la sugerencia de la IA sale de la documentación (no empieza por «A decidir por el cliente:»), se aprueba tal cual.
2. Si no, se responde: «El cliente no lo ha decidido. Para esta versión, la opción más sencilla de las que propones.» Esta respuesta tampoco la redacta Claude: es la misma siempre.
3. Una pregunta que repite otra ya respondida se descarta con el motivo «Repetida: ya respondida en P-xxx».

## Revisión de las propuestas de las actualizaciones

- Una modificación o eliminación propuesta se **acepta** si corresponde a un cambio de la tabla de `ejemplos/README.md` o a una respuesta de esta hoja, y se **rechaza** si no (por ejemplo, si cambia algo que la iteración 2 no toca).
- Un requisito o una historia nuevos que no correspondan a ningún cambio se dejan, porque no se pueden rechazar sin editarlos, y se cuentan aparte como «nuevos sin referencia».
- Cada decisión se anota en `docs/resultados-paso-116/` con su id, la decisión y el cambio I2 al que corresponde.
- Las discrepancias del árbitro se marcan como revisadas sin cambiar su contenido, como en el paso 65, y se cuentan.

## Orden de la prueba

Todo en «Clinicas Veterinarias v0.11» (proyecto 33), con el catálogo y el backlog validados de la iteración 1:

1. Nueva iteración, subir `DOC-22` a `DOC-27` y cerrar la documentación.
2. Ronda que lo lee todo (opción A), experimental: no cambia el proyecto.
3. Ronda centrada en la iteración 2 (opción C), responder con esta hoja.
4. Actualizar el catálogo con la iteración 2, revisar las propuestas y validar.
5. Actualizar el backlog, revisar las propuestas y validar.
6. Ronda con todas las iteraciones, responder con esta hoja.
7. Catálogo desde cero y validarlo; backlog desde cero y validarlo.
