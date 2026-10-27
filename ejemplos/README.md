# Proyectos de ejemplo

Dos proyectos **ficticios** con la documentación que entregaría un cliente en la fase de análisis, en formatos distintos, para probar Concilia de principio a fin (paso 65 del plan). Las organizaciones, personas, direcciones, teléfonos y correos son inventados; los dominios son `.example`.

| Proyecto | Tamaño | Archivos | Texto extraído |
| --- | --- | --- | --- |
| [Banco de alimentos de la Asociación Despensa Común](mediano-banco-de-alimentos/) | Mediano | 7 | unos 5.900 tokens |
| [Plataforma del Grupo Veterinario Huellas](grande-clinicas-veterinarias/) | Grande | 21 | unos 23.400 tokens |

Como referencia, ReservaSalas (`prompts/generacion_requisitos/ejemplo/fuentes/DOC-01.txt`) es pequeño y el documento C del autor tenía unos 40.000 tokens.

Para probarlos, crea un proyecto en la web y sube todos los archivos de su carpeta en el paso Documentos.

## Mediano: banco de alimentos

Una asociación que reparte alimentos a unas 180 familias derivadas por los Servicios Sociales. Quiere una aplicación web para:
- las familias y sus derivaciones;
- las citas de recogida;
- el almacén, con sus caducidades;
- los turnos de voluntarios;
- los informes para el Ayuntamiento, la federación y los donantes.

| Archivo | Formato | Qué es |
| --- | --- | --- |
| `01-acta-reunion-arranque.docx` | Word | Acta de la reunión de arranque: objetivos, perfiles, reglas y alcance |
| `02-entrevista-coordinadora.vtt` | Transcripción | Entrevista con la coordinadora sobre el día a día del almacén |
| `03-correo-junta-informes-y-donantes.eml` | Correo | La junta detalla los informes y los certificados de donación (con un adjunto) |
| `04-hoja-control-actual.xlsx` | Excel | La hoja de cálculo que usan hoy: familias, entradas, turnos, lotes y notas |
| `05-notas-analista.md` | Markdown | Notas de la analista: dominio, dudas y requisitos no funcionales |
| `06-protocolo-de-reparto.pdf` | PDF | Protocolo interno de reparto aprobado por la junta |
| `07-donantes.csv` | CSV | Lista de donantes |

## Grande: red de clínicas veterinarias

Cuatro clínicas, una con hospital y urgencias 24 horas, que sustituyen cuatro programas aislados por una plataforma común con:
- historia clínica única, agenda y urgencias con triaje;
- hospitalización, vacunas, farmacia y stock, y recetas;
- facturación, planes de salud y aseguradoras;
- app del cliente, laboratorio externo, informes y migración de datos.

| Archivo | Formato | Qué es |
| --- | --- | --- |
| `01-especificacion-funcional.docx` | Word | Especificación funcional preliminar (v0.3), el documento central |
| `02-presentacion-arranque.pptx` | PowerPoint | Presentación de la reunión de arranque, con notas del orador |
| `03-reunion-equipo-veterinario.vtt` | Transcripción | Reunión con veterinarios: historia clínica, hospitalización y stock |
| `04-reunion-recepcion.srt` | Subtítulos | Reunión con recepción: citas, cobros y recordatorios |
| `05-correo-gerencia-planes-de-salud.eml` | Correo | La gerente detalla los planes de salud y las aseguradoras |
| `06-tarifas-productos-y-clinicas.xlsx` | Excel | Tarifas de servicios, productos y datos de las clínicas |
| `07-politica-proteccion-de-datos.pdf` | PDF | Política interna de protección de datos |
| `08-api-laboratorio-borrador.yaml` | YAML | Borrador de la API del laboratorio externo |
| `09-incidencias-sistema-actual.csv` | CSV | Incidencias del sistema actual |
| `10-intranet-procedimientos.html` | HTML | Página de procedimientos de recepción de la intranet |
| `11-acta-reunion-contabilidad-y-facturacion.docx` | Word | Acta sobre facturación, cierre de caja, exportación contable y domiciliaciones |
| `12-protocolo-urgencias-y-hospital.md` | Markdown | Protocolo de urgencias, triaje y hospitalización |
| `13-correo-labvet-dudas-integracion.eml` | Correo | Respuestas del laboratorio sobre su API |
| `14-casos-de-uso-app-cliente.docx` | Word | Casos de uso de la app del cliente |
| `15-reunion-gerencia-informes.vtt` | Transcripción | Reunión con gerencia: informes y decisiones pendientes |
| `16-migracion-mapeo-de-datos.xlsx` | Excel | Mapeo de campos del programa antiguo a la plataforma |
| `17-protocolos-vacunacion.json` | JSON | Protocolos de vacunación y desparasitación |
| `18-exportacion-sistema-antiguo.xml` | XML | Exportación de prueba del programa antiguo |
| `19-catalogo-diagnosticos.csv` | CSV | Catálogo de diagnósticos del grupo |
| `20-taller-agenda-y-citas.vtt` | Transcripción | Taller sobre agenda, guardias y citas |
| `21-requisitos-tecnicos-y-seguridad.pdf` | PDF | Requisitos técnicos y de seguridad del soporte informático |

## Contradicciones y huecos puestos a propósito

Como en un proyecto real, los documentos no siempre coinciden. Esta lista es la **referencia para evaluar la fase de preguntas**: lo ideal es que las preguntas cubran estos puntos. En el grande, algunos se resuelven en un documento posterior (lo indica la columna «Se resuelve en»); en ese caso, lo ideal es que el catálogo recoja la decisión final.

### Mediano

| # | Tema | Qué dice cada documento | Se resuelve en |
| --- | --- | --- | --- |
| M1 | Frecuencia de recogida | Acta: cada 15 días. Entrevista: cada semana las familias de 3 o más. Correo: cada 15 días sin excepciones, ampliable a criterio de la coordinadora si lo pide la trabajadora social. | No (el correo es lo más reciente) |
| M2 | Caducidad de la derivación | Acta: 6 meses. Hoja de cálculo: 1 año. Entrevista: hay familias antiguas con 1 año. | No |
| M3 | Atención con la derivación caducada | Acta: solo con derivación vigente. Protocolo: una entrega como máximo. Entrevista: «una más» en la práctica. | No |
| M4 | Canal de los recordatorios | Acta: SMS. Correo: WhatsApp si es posible. | No |
| M5 | Funcionamiento sin conexión en el almacén | Entrevista: cobertura mala al fondo; sin decidir. | No |
| M6 | Certificado de manipulador de alimentos | Entrevista: validez desconocida (¿4 años?). Protocolo: obligatorio para frescos. ¿Se impide apuntarse sin él? | No |
| M7 | Supermercado solidario con puntos | Entrevista: idea de la junta; alcance sin decidir. | No |
| M8 | Sustituciones en los lotes | Entrevista: quiere que la aplicación las proponga; sin reglas. | No |
| M9 | Persona que recoge en lugar de la familia | Entrevista: registrarla. Protocolo: nombre y DNI. | Protocolo |
| M10 | Voluntarios que se apuntan solos a los turnos y aviso si falta gente | Solo en el correo. | — |
| M11 | Valor estimado del certificado de donación | Correo: precio medio por kilo y tipo que fija la junta; sin valores. | No |
| M12 | Registro de la caducidad de las entradas | Protocolo: si es inferior a tres meses. Entrevista: de lo fresco y de supermercados. | No |

### Grande

| # | Tema | Qué dice cada documento | Se resuelve en |
| --- | --- | --- | --- |
| G1 | Plazo para cancelar una cita | Especificación: 24 h. Recepción: 12 h por la app, 2 h por teléfono. Casos de uso: 12 h. | Reunión con gerencia: 12 h por la app |
| G2 | Plan Adulto | Especificación: 2 consultas al año, sin descuento en alimentación. Correo: consultas ilimitadas y 10 % también en alimentación. | Reunión con gerencia: lo del correo |
| G3 | Chat en la app | Presentación: incluido. Especificación: fuera de alcance. Correo: no, pero un formulario de consulta no urgente. | Correo y caso de uso CU-09 |
| G4 | Recordatorio de vacunas | Especificación y JSON: 15 días antes. Recepción: 7 días y el día anterior. | Reunión con gerencia: configurable, 7 días y el día anterior |
| G5 | Conservación de la historia clínica | Especificación: indefinida. Política de datos: 5 años desde la última visita. | No: queda abierto (asesora legal) |
| G6 | Traspasos de stock | Especificación: los aprueba el responsable. Veterinarios: cualquiera; solo los controlados con aprobación. | Reunión con gerencia: lo de los veterinarios, con registro |
| G7 | Urgencias en Huellas Norte | Especificación: cuestión abierta. Veterinarios: previsto. | Reunión con gerencia: aprobado para marzo de 2027 |
| G8 | Pagos fraccionados | Especificación: hasta 3 plazos con autorización del responsable. Contabilidad: excepciones solo con gerencia; Ribera dio 4 y 5. | Acta de contabilidad |
| G9 | Duración de la primera visita de cachorro | Especificación: consulta de 20 min. Intranet y taller: 30 min. | Taller |
| G10 | Protocolos de vacunación por clínica | Especificación: por especie y edad. Veterinarios y JSON: Montaña añade leishmaniosis. | JSON |
| G11 | Resultados visibles para el cliente | Especificación: se notifica «resultado disponible». Recepción y CU-05: solo tras revisarlos el veterinario. | Casos de uso |
| G12 | Resultados del laboratorio | Especificación: pregunta abierta. Correo de LabVet: estructurados solo hematología y bioquímica; PDF 24 h; sin webhooks. | Correo de LabVet |
| G13 | Fotos en el informe de hospitalización | Solo en la reunión con veterinarios. | — |
| G14 | Señal económica para quien no se presenta | Recepción: 10 € a partir de tres faltas. | Reunión con gerencia: no en la primera versión |
| G15 | Prioridad en la lista de espera para clientes con plan | Solo en el correo de gerencia. | — |
| G16 | Segundo factor de autenticación | Especificación: gerencia y contabilidad. Sistemas: todos los que accedan desde fuera de las clínicas. | No |
| G17 | Funcionamiento sin conexión | Especificación: nada. Sistemas: consulta de la agenda y fichas del día si cae la conexión (Montaña tiene cortes). | No |
| G18 | Idioma de las comunicaciones | Especificación: app en castellano e inglés. Recepción: recordatorios en el idioma del cliente. | No |
| G19 | Informes comparados con el año anterior | Gerencia los pide, pero las facturas antiguas no se migran. | Reunión con gerencia: el primer año, con la semana anterior |
| G20 | Margen de los productos | Requiere el precio de compra por lote, que hoy no se guarda. | Reunión con gerencia: anotarlo al dar entrada |
| G21 | Imágenes DICOM | Sistemas: mostrarlas o solo enlazarlas, sin decidir. | No |
| G22 | Doble cierre de caja en Huellas Centro | Solo en el acta de contabilidad. | — |
| G23 | Cita para varias mascotas a la vez | Solo en el taller de agenda. | — |
| G24 | Animales sin propietario identificado | Solo en el protocolo de urgencias; se facturan al ayuntamiento o a la protectora. | — |
| G25 | Depósito del 50 % al ingresar | Solo en el protocolo de urgencias, con excepciones. | — |
| G26 | Programa de puntos | Reunión con gerencia: para el año siguiente. | Fuera de alcance |

## Grande: segunda iteración

Para probar las iteraciones del proyecto (hito 22, paso 116), el grande tiene una **segunda tanda de documentos**, en [`grande-clinicas-veterinarias/iteracion-2/`](grande-clinicas-veterinarias/iteracion-2/). Llega después de que el catálogo y el backlog de la primera estén validados. Se suben como «Iteración 2» y, como siguen la numeración del proyecto, son `DOC-22` a `DOC-27`. Juntos tienen unos 2.500 tokens.

| Archivo | Formato | Qué es |
| --- | --- | --- |
| `22-acta-seguimiento-gerencia.docx` | Word | Acta de la reunión de seguimiento con gerencia (20-10-2026) |
| `23-correo-gerencia-teleconsulta-y-tienda.eml` | Correo | La gerente detalla la teleconsulta, los pedidos desde la app y el horario de la nueva clínica |
| `24-informe-asesoria-legal-conservacion.md` | Markdown | Informe de la asesora legal sobre la conservación de la historia clínica |
| `25-reunion-veterinarios-seguimiento.vtt` | Transcripción | Reunión de seguimiento con el equipo veterinario |
| `26-tarifas-2027.xlsx` | Excel | Cambios de tarifas para 2027 |
| `27-informe-auditoria-seguridad.html` | HTML | Conclusiones de la auditoría de seguridad |

### Cambios puestos a propósito

Esta lista es la **referencia para evaluar la ronda centrada y las actualizaciones** del paso 116. La columna «Se espera» dice qué debería salir:
- **P:** una pregunta; **P enlazada:** una pregunta enlazada a la respuesta o al requisito que contradice, sin reescribir la respuesta;
- **N:** un requisito o una historia nuevos;
- **M:** una modificación propuesta;
- **E:** una eliminación propuesta;
- **—:** nada, porque no cambia nada.

Las respuestas que se citan son las del proyecto de clínicas de la v0.11 («Clinicas Veterinarias v0.11»).

| # | Tema | Qué dice la iteración 2 | Contra qué choca | Se espera |
| --- | --- | --- | --- | --- |
| I2-1 | Teleconsulta de seguimiento | Correo: videollamada desde la app, solo seguimientos, visita presencial en los 30 días anteriores, 15 min, se paga al reservar, no se graba. Veterinarios: anotar en la historia y dar cita presencial desde la llamada. | Nada: es nueva | N |
| I2-2 | Pedidos desde la app con recogida en clínica | Correo: alimentación e higiene, pago en la app, reserva de 48 h y devolución si no se recoge; medicamentos no. | Nada: es nueva | N |
| I2-3 | Quinta clínica, Huellas Puerto | Acta: abre en enero de 2027, sin hospital ni urgencias, mismas tarifas que Centro, en la plataforma desde el primer día. Tarifas: lo mismo. | Los requisitos que hablan de cuatro clínicas | M (y N si hace falta) |
| I2-4 | Encuesta de satisfacción | Acta: al día siguiente de cada visita, tres valoraciones de 1 a 5 y un comentario; la media por clínica en el informe mensual. | Nada: es nueva | N |
| I2-5 | Plazo para cancelar desde la app | Acta: 24 horas, no 12. | La respuesta a P-004 (12 horas) y los requisitos validados de 12 horas (G1) | P enlazada; tras responder, M |
| I2-6 | Señal para quien no se presenta | Acta: 10 € a partir de la segunda falta sin avisar en seis meses; se paga al reservar la siguiente y se descuenta de su factura. | G14 (decidido: no en la primera versión) | N o M |
| I2-7 | Segundo factor | Auditoría: obligatorio para todo el personal, también dentro de las clínicas; para los clientes, nada. | El requisito validado de segundo factor solo para accesos desde fuera (G16) | M |
| I2-8 | Hospitalización en la primera fase | Acta: se adelanta a la fase 1, solo en Huellas Centro. Veterinarios: les parece bien. | La respuesta a P-002 (fase 2) | P enlazada; tras responder, M |
| I2-9 | Urgencias nocturnas y festivos | Tarifas: 100 € desde el 1 de enero de 2027 (antes 95 €). | La respuesta a P-059 (95 €) | P enlazada o M |
| I2-10 | Precio de la teleconsulta | Acta: 20 €. Correo y tarifas: 25 €. | Entre los documentos de la iteración 2 | P |
| I2-11 | Conservación de la historia clínica | Informe legal: identificable 5 años desde la última visita y después anonimizada, cada noche; lo indefinido «debe descartarse». | La respuesta a P-001 (indefinidamente) y su requisito validado (G5) | P enlazada; tras responder, M |
| I2-12 | Horario de Huellas Puerto | Acta: de lunes a sábado. Correo: de lunes a viernes. | Entre los documentos de la iteración 2 | P |
| I2-13 | Cita para varias mascotas | Acta: se descarta; una cita por mascota. | G23, la respuesta a P-042 y su requisito validado | E |
| I2-14 | Fotos en el informe de hospitalización | Veterinarios: se quitan; el informe, solo texto. | G13 y su requisito validado | M o E |
| I2-15 | Quién ve los resultados de la encuesta | Acta: «no se decide». | Hueco | P |
| I2-16 | ¿La teleconsulta cuenta para el plan de salud? | Veterinarios: «lo tiene que decidir Elena». | Hueco | P |
| I2-17 | Plan Adulto | Acta: se confirman las mismas condiciones. | Nada: coincide con G2 resuelto | — |
| I2-18 | Jaula de aislamiento y disponibilidad | Veterinarios y auditoría repiten lo que ya se decidió (P-036 y P-039). | Nada | — |

## Cómo se generaron

Los generó Claude con unos scripts de Python (python-docx, python-pptx, openpyxl y fpdf2) el 15-10-2026, a petición del autor, para el paso 65. El contenido es ficticio, y las contradicciones y huecos de la tabla anterior se incluyeron a propósito. La segunda iteración del grande la escribió Claude el 27-10-2026, para el paso 116, con python-docx y openpyxl para el Word y el Excel, a partir de las decisiones y respuestas ya validadas del proyecto de clínicas.
