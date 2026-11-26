# Referencia de clínicas: lista de funcionalidades

Paso 118 del plan. La hace el autor leyendo los documentos de `ejemplos/grande-clinicas-veterinarias/`, **sin mirar ningún catálogo ni backlog generado**. Sirve para medir la cobertura del catálogo y del backlog en el paso 120 (SOL solo, Kimi solo y Concilia).

**Hecha por:** el autor, el 26-11-2026. Claude comprobó después, contra los documentos y sin mirar salidas de los modelos, que cada línea es cierta y que el formato se lee bien; no añadió ni quitó funcionalidades, y el único cambio (F09, la prioridad sin decidir, como dice G15) lo decidió el autor.

## Cómo se escribe

- **Una funcionalidad por línea,** con un id, el nombre y el documento donde aparece: `F01 · Cancelar una cita desde la app · 04-reunion-recepcion.srt`.
- **Una funcionalidad** es algo que un usuario hace con el sistema o que el sistema hace solo, como «Cancelar una cita desde la app» o «Avisar de una vacuna próxima».
- **Un detalle no es una funcionalidad:** «con 12 h de antelación» va dentro de «Cancelar una cita».
- Si aparece en varios documentos, basta con uno, el más claro; se pueden poner varios separados por comas.
- Saldrán unas 40–60.
- Lo que los documentos dejan fuera se apunta con «(fuera de alcance)» al final: queda en la referencia, pero no cuenta en la cobertura del paso 120, porque un catálogo que lo deja fuera hace bien (lo miden las trampas).

## Documentos

`01-especificacion-funcional.docx`, `02-presentacion-arranque.pptx`, `03-reunion-equipo-veterinario.vtt`, `04-reunion-recepcion.srt`, `05-correo-gerencia-planes-de-salud.eml`, `06-tarifas-productos-y-clinicas.xlsx`, `07-politica-proteccion-de-datos.pdf`, `08-api-laboratorio-borrador.yaml`, `09-incidencias-sistema-actual.csv`, `10-intranet-procedimientos.html`, `11-acta-reunion-contabilidad-y-facturacion.docx`, `12-protocolo-urgencias-y-hospital.md`, `13-correo-labvet-dudas-integracion.eml`, `14-casos-de-uso-app-cliente.docx`, `15-reunion-gerencia-informes.vtt`, `16-migracion-mapeo-de-datos.xlsx`, `17-protocolos-vacunacion.json`, `18-exportacion-sistema-antiguo.xml`, `19-catalogo-diagnosticos.csv`, `20-taller-agenda-y-citas.vtt`, `21-requisitos-tecnicos-y-seguridad.pdf`.

## Funcionalidades

F01 · Dar de alta y modificar la ficha de un cliente (contacto, preferencias de comunicación y consentimientos) · 01-especificacion-funcional.docx, 10-intranet-procedimientos.html, 16-migracion-mapeo-de-datos.xlsx  
F02 · Dar de alta y modificar la ficha de un paciente, con varios clientes (uno principal) e historial de peso con gráfica · 01-especificacion-funcional.docx, 04-reunion-recepcion.srt, 16-migracion-mapeo-de-datos.xlsx, 18-exportacion-sistema-antiguo.xml  
F03 · Buscar clientes y pacientes en todas las clínicas por nombre, teléfono o microchip (también con el lector USB) · 01-especificacion-funcional.docx, 10-intranet-procedimientos.html, 21-requisitos-tecnicos-y-seguridad.pdf  
F04 · Fusionar fichas duplicadas (solo responsables de clínica, con registro de quién lo hizo) · 01-especificacion-funcional.docx, 04-reunion-recepcion.srt, 09-incidencias-sistema-actual.csv, 18-exportacion-sistema-antiguo.xml  
F05 · Mostrar avisos en la ficha en toda la plataforma (agresivo, deuda, alergias, tres citas no presentadas) · 01-especificacion-funcional.docx, 02-presentacion-arranque.pptx, 09-incidencias-sistema-actual.csv, 15-reunion-gerencia-informes.vtt, 18-exportacion-sistema-antiguo.xml  
F06 · Configurar clínicas, salas, horarios, festivos y agendas por veterinario y sala (horario semanal por clínica, peluquería de Montaña, huecos reservados a recepción, quirófano por la mañana) · 01-especificacion-funcional.docx, 06-tarifas-productos-y-clinicas.xlsx, 20-taller-agenda-y-citas.vtt  
F07 · Dar una cita en recepción (tipo con duración por defecto, veterinario o «cualquiera», sin solapes) · 01-especificacion-funcional.docx, 09-incidencias-sistema-actual.csv, 10-intranet-procedimientos.html, 20-taller-agenda-y-citas.vtt  
F08 · Llevar los estados de la cita y la sala de espera (llegada, en consulta, no presentada), con pantalla de turno que solo muestra el nombre de la mascota · 01-especificacion-funcional.docx, 10-intranet-procedimientos.html, 20-taller-agenda-y-citas.vtt  
F09 · Lista de espera con aviso a recepción cuando se libera un hueco compatible (la prioridad para clientes con plan, sin decidir) · 01-especificacion-funcional.docx, 05-correo-gerencia-planes-de-salud.eml, 14-casos-de-uso-app-cliente.docx  
F10 · Cambiar o cancelar una cita desde la app (hasta 12 h antes) · 01-especificacion-funcional.docx, 04-reunion-recepcion.srt, 14-casos-de-uso-app-cliente.docx, 15-reunion-gerencia-informes.vtt  
F11 · Cambiar o cancelar una cita por teléfono o en recepción · 01-especificacion-funcional.docx, 04-reunion-recepcion.srt, 15-reunion-gerencia-informes.vtt  
F12 · Registrar la llegada de una urgencia sin cita y ordenar la cola por triaje en cinco niveles (preparado para Huellas Norte) · 01-especificacion-funcional.docx, 03-reunion-equipo-veterinario.vtt, 12-protocolo-urgencias-y-hospital.md, 15-reunion-gerencia-informes.vtt  
F13 · Registrar una visita en la historia clínica única del grupo, con plantillas por tipo de visita · 01-especificacion-funcional.docx, 03-reunion-equipo-veterinario.vtt, 16-migracion-mapeo-de-datos.xlsx, 18-exportacion-sistema-antiguo.xml  
F14 · Codificar diagnósticos con el catálogo del grupo (por área y especie, con «Otro» en texto libre y marca de uso estadístico) y ampliar el catálogo · 01-especificacion-funcional.docx, 03-reunion-equipo-veterinario.vtt, 19-catalogo-diagnosticos.csv  
F15 · Adjuntar archivos a la historia clínica (imágenes, PDF, hasta 50 MB; las imágenes DICOM, mostrarlas o enlazarlas está sin decidir) · 01-especificacion-funcional.docx, 04-reunion-recepcion.srt, 07-politica-proteccion-de-datos.pdf, 21-requisitos-tecnicos-y-seguridad.pdf  
F16 · Cerrar (firmar) una visita y añadirle anotaciones posteriores fechadas y firmadas · 01-especificacion-funcional.docx, 03-reunion-equipo-veterinario.vtt  
F17 · Recoger consentimientos informados con firma en tableta o en papel escaneado · 01-especificacion-funcional.docx  
F18 · Registrar vacunas y desparasitaciones con lote y próxima dosis propuesta (la antirrábica, con los datos del registro oficial) · 01-especificacion-funcional.docx, 16-migracion-mapeo-de-datos.xlsx, 17-protocolos-vacunacion.json  
F19 · Configurar protocolos de vacunación y desparasitación por especie, edad y clínica · 01-especificacion-funcional.docx, 03-reunion-equipo-veterinario.vtt, 06-tarifas-productos-y-clinicas.xlsx, 17-protocolos-vacunacion.json  
F20 · Imprimir la cartilla de vacunación y descargarla desde la app · 01-especificacion-funcional.docx, 14-casos-de-uso-app-cliente.docx  
F21 · Enviar el recordatorio automático de vacuna (configurable; por defecto 7 días antes y el día anterior si hay cita) · 01-especificacion-funcional.docx, 04-reunion-recepcion.srt, 09-incidencias-sistema-actual.csv, 15-reunion-gerencia-informes.vtt, 17-protocolos-vacunacion.json  
F22 · Ingresar un paciente en el hospital, en una jaula o box libre (aislamiento para infecciosos) · 01-especificacion-funcional.docx, 06-tarifas-productos-y-clinicas.xlsx, 12-protocolo-urgencias-y-hospital.md  
F23 · Llevar la hoja de hospitalización por horas: planificar tareas, marcarlas hechas, cambiar el tratamiento y dejar la nota de traspaso de turno · 01-especificacion-funcional.docx, 03-reunion-equipo-veterinario.vtt, 09-incidencias-sistema-actual.csv, 12-protocolo-urgencias-y-hospital.md  
F24 · Mostrar en la sala de hospitalización las tareas próximas y las retrasadas · 01-especificacion-funcional.docx, 03-reunion-equipo-veterinario.vtt, 12-protocolo-urgencias-y-hospital.md  
F25 · Escribir el informe diario de hospitalización para el cliente (con fotos opcionales) y que lo siga desde la app · 01-especificacion-funcional.docx, 03-reunion-equipo-veterinario.vtt, 12-protocolo-urgencias-y-hospital.md, 14-casos-de-uso-app-cliente.docx  
F26 · Dar el alta de hospitalización (informe, receta, cita de revisión, factura y aviso a la clínica habitual) · 01-especificacion-funcional.docx, 12-protocolo-urgencias-y-hospital.md  
F27 · Mantener el catálogo único de productos y el tarifario común con precios por clínica · 01-especificacion-funcional.docx, 06-tarifas-productos-y-clinicas.xlsx, 16-migracion-mapeo-de-datos.xlsx  
F28 · Llevar el stock por clínica y lote con caducidad, descontando del lote que antes caduca · 01-especificacion-funcional.docx, 12-protocolo-urgencias-y-hospital.md, 16-migracion-mapeo-de-datos.xlsx  
F29 · Avisar del stock bajo mínimo, proponer el pedido y darle entrada con el precio de compra por lote · 01-especificacion-funcional.docx, 06-tarifas-productos-y-clinicas.xlsx, 09-incidencias-sistema-actual.csv, 15-reunion-gerencia-informes.vtt  
F30 · Traspasar stock entre clínicas (aprobación solo para los controlados) · 01-especificacion-funcional.docx, 03-reunion-equipo-veterinario.vtt, 09-incidencias-sistema-actual.csv, 15-reunion-gerencia-informes.vtt  
F31 · Hacer el inventario físico con ajuste de diferencias y motivo · 01-especificacion-funcional.docx, 16-migracion-mapeo-de-datos.xlsx  
F32 · Llevar el libro de registro de estupefacientes y medicamentos de uso veterinario exclusivo · 01-especificacion-funcional.docx, 09-incidencias-sistema-actual.csv  
F33 · Emitir una receta desde la visita (imprimirla, enviarla en PDF o dispensarla) · 01-especificacion-funcional.docx, 12-protocolo-urgencias-y-hospital.md  
F34 · Exportar el fichero mensual de recetas de antibióticos para la plataforma oficial · 01-especificacion-funcional.docx, 03-reunion-equipo-veterinario.vtt, 09-incidencias-sistema-actual.csv  
F35 · Integración automática con la plataforma oficial de prescripción (fuera de alcance) · 01-especificacion-funcional.docx, 03-reunion-equipo-veterinario.vtt  
F36 · Pasar al cargo del cliente los servicios y productos de la visita, revisables por recepción antes de cobrar · 01-especificacion-funcional.docx, 04-reunion-recepcion.srt, 09-incidencias-sistema-actual.csv, 12-protocolo-urgencias-y-hospital.md  
F37 · Hacer presupuestos que el cliente acepta por escrito y se convierten en cargos · 01-especificacion-funcional.docx, 05-correo-gerencia-planes-de-salud.eml  
F38 · Emitir facturas simplificadas, completas y rectificativas con series por clínica · 01-especificacion-funcional.docx, 11-acta-reunion-contabilidad-y-facturacion.docx  
F39 · Cobrar en la clínica (efectivo, tarjeta con datáfono, transferencia) · 01-especificacion-funcional.docx, 11-acta-reunion-contabilidad-y-facturacion.docx, 21-requisitos-tecnicos-y-seguridad.pdf  
F40 · Pagar desde la app los cargos pendientes, la cuota del plan y el depósito de hospitalización · 01-especificacion-funcional.docx, 02-presentacion-arranque.pptx, 14-casos-de-uso-app-cliente.docx  
F41 · Conceder pagos fraccionados (máximo 3 plazos; las excepciones, gerencia) · 01-especificacion-funcional.docx, 11-acta-reunion-contabilidad-y-facturacion.docx  
F42 · Controlar las deudas (en la ficha, recordatorio al dar cita y listado por antigüedad) · 01-especificacion-funcional.docx, 11-acta-reunion-contabilidad-y-facturacion.docx, 16-migracion-mapeo-de-datos.xlsx  
F43 · Hacer el cierre de caja diario por clínica (doble en Huellas Centro) · 01-especificacion-funcional.docx, 11-acta-reunion-contabilidad-y-facturacion.docx  
F44 · Exportar el fichero contable mensual · 01-especificacion-funcional.docx, 02-presentacion-arranque.pptx, 11-acta-reunion-contabilidad-y-facturacion.docx  
F45 · Definir los planes de salud, sus servicios y sus precios · 01-especificacion-funcional.docx, 02-presentacion-arranque.pptx, 05-correo-gerencia-planes-de-salud.eml  
F46 · Aplicar al cobrar lo incluido en el plan y avisar cuando se agota un servicio · 01-especificacion-funcional.docx, 05-correo-gerencia-planes-de-salud.eml  
F47 · Cobrar las cuotas de los planes por remesa y cargar los recibos devueltos (suspensión y baja) · 01-especificacion-funcional.docx, 11-acta-reunion-contabilidad-y-facturacion.docx  
F48 · Registrar la póliza de la mascota y dividir la factura entre aseguradora y cliente · 01-especificacion-funcional.docx, 05-correo-gerencia-planes-de-salud.eml, 11-acta-reunion-contabilidad-y-facturacion.docx  
F49 · Enviar a cada aseguradora la relación mensual de facturas y seguir sus cobros pendientes · 01-especificacion-funcional.docx, 05-correo-gerencia-planes-de-salud.eml, 11-acta-reunion-contabilidad-y-facturacion.docx  
F50 · Registrarse en la app vinculándose a la ficha con un código, y gestionar preferencias (idioma, canales, comunicaciones comerciales) y baja · 01-especificacion-funcional.docx, 14-casos-de-uso-app-cliente.docx  
F51 · Consultar en la app mascotas, vacunas, citas, facturas y planes · 01-especificacion-funcional.docx, 02-presentacion-arranque.pptx, 14-casos-de-uso-app-cliente.docx  
F52 · Pedir cita desde la app · 01-especificacion-funcional.docx, 02-presentacion-arranque.pptx, 14-casos-de-uso-app-cliente.docx, 20-taller-agenda-y-citas.vtt  
F53 · Chat y videoconsulta con la clínica (fuera de alcance) · 01-especificacion-funcional.docx, 02-presentacion-arranque.pptx, 04-reunion-recepcion.srt, 05-correo-gerencia-planes-de-salud.eml  
F54 · Enviar comunicaciones con plantillas comunes por el canal del cliente y registrarlas en su ficha · 01-especificacion-funcional.docx, 04-reunion-recepcion.srt, 09-incidencias-sistema-actual.csv, 12-protocolo-urgencias-y-hospital.md  
F55 · Enviar el recordatorio de cita 24 h antes, con botón para confirmar o cancelar · 01-especificacion-funcional.docx, 20-taller-agenda-y-citas.vtt  
F56 · Consultar el cuadro de mando de gerencia y los informes de clínica y de cobertura vacunal, exportables a hoja de cálculo · 01-especificacion-funcional.docx, 02-presentacion-arranque.pptx, 03-reunion-equipo-veterinario.vtt, 15-reunion-gerencia-informes.vtt  
F57 · Gestionar usuarios, roles por clínica y permisos · 01-especificacion-funcional.docx, 07-politica-proteccion-de-datos.pdf  
F58 · Registrar en auditoría los accesos a historias clínicas y los cambios en precios, facturas y stock · 01-especificacion-funcional.docx, 07-politica-proteccion-de-datos.pdf, 21-requisitos-tecnicos-y-seguridad.pdf  
F59 · Enviar solicitudes de análisis al laboratorio · 01-especificacion-funcional.docx, 02-presentacion-arranque.pptx, 08-api-laboratorio-borrador.yaml, 13-correo-labvet-dudas-integracion.eml  
F60 · Recibir los resultados del laboratorio, guardar el PDF en la historia y avisar al veterinario · 01-especificacion-funcional.docx, 02-presentacion-arranque.pptx, 04-reunion-recepcion.srt, 08-api-laboratorio-borrador.yaml, 09-incidencias-sistema-actual.csv, 13-correo-labvet-dudas-integracion.eml  
F61 · Iniciar sesión con segundo factor, con bloqueo tras intentos fallidos y cierre por inactividad · 01-especificacion-funcional.docx, 07-politica-proteccion-de-datos.pdf, 21-requisitos-tecnicos-y-seguridad.pdf  
F62 · Migrar los datos de las cuatro instalaciones actuales · 01-especificacion-funcional.docx, 02-presentacion-arranque.pptx, 16-migracion-mapeo-de-datos.xlsx, 18-exportacion-sistema-antiguo.xml  
F63 · Tienda en línea de productos (fuera de alcance) · 01-especificacion-funcional.docx  
F64 · Nóminas y turnos del personal (fuera de alcance) · 01-especificacion-funcional.docx  
F65 · Contabilidad completa (fuera de alcance) · 01-especificacion-funcional.docx  
F66 · Aplicar descuentos con autorización según el porcentaje · 04-reunion-recepcion.srt, 10-intranet-procedimientos.html  
F67 · Revisar y publicar los resultados de laboratorio para que el cliente los vea en la app · 04-reunion-recepcion.srt, 14-casos-de-uso-app-cliente.docx  
F68 · Señal económica para quien no se presenta (fuera de alcance) · 04-reunion-recepcion.srt, 15-reunion-gerencia-informes.vtt  
F69 · Dar de baja un plan antes de los 12 meses calculando lo que debe por la permanencia · 05-correo-gerencia-planes-de-salud.eml, 09-incidencias-sistema-actual.csv, 11-acta-reunion-contabilidad-y-facturacion.docx  
F70 · Cambiar de plan automáticamente por edad, avisando un mes antes · 05-correo-gerencia-planes-de-salud.eml  
F71 · Pedir a la aseguradora la autorización previa de una cirugía · 05-correo-gerencia-planes-de-salud.eml, 09-incidencias-sistema-actual.csv, 11-acta-reunion-contabilidad-y-facturacion.docx  
F72 · Enviar una consulta no urgente desde la app · 05-correo-gerencia-planes-de-salud.eml, 14-casos-de-uso-app-cliente.docx  
F73 · Bloquear y anonimizar datos al vencer los plazos de conservación y entregar la historia en PDF (derechos de protección de datos) · 07-politica-proteccion-de-datos.pdf  
F74 · Marcar un paciente como fallecido (cancela citas y recordatorios) · 09-incidencias-sistema-actual.csv, 10-intranet-procedimientos.html, 12-protocolo-urgencias-y-hospital.md  
F75 · Atender un animal sin propietario identificado y facturarlo al ayuntamiento o a la protectora · 12-protocolo-urgencias-y-hospital.md  
F76 · Pedir un depósito al ingresar (50 %, con excepciones) · 12-protocolo-urgencias-y-hospital.md, 14-casos-de-uso-app-cliente.docx  
F77 · Solicitar un plan de salud desde la app (la clínica confirma el alta con el mandato) · 14-casos-de-uso-app-cliente.docx  
F78 · Programa de puntos (fuera de alcance) · 15-reunion-gerencia-informes.vtt  
F79 · Llevar el calendario de guardias del hospital · 20-taller-agenda-y-citas.vtt  
F80 · Reasignar en bloque las citas de un veterinario y avisar a los clientes afectados · 20-taller-agenda-y-citas.vtt  
F81 · Dar una cita seguida para varias mascotas · 20-taller-agenda-y-citas.vtt  
F82 · Consultar la agenda y las fichas del día sin conexión · 21-requisitos-tecnicos-y-seguridad.pdf  

## Notas del autor

- **Salen 82 líneas, más de las 40–60 previstas.** El proyecto es grande y, con la regla del botón, el menú o lo automático, cada documento añade funciones propias. Para no pasarme más, he juntado en una sola línea:
  - las pantallas con el proceso al que sirven: la pantalla de turno va con la sala de espera (F08) y la nota de traspaso de turno con la hoja de hospitalización (F23);
  - el catálogo de productos con el tarifario (F27), porque los dos son datos maestros que mantiene gerencia;
  - el registro en la app con las preferencias y la baja (F50);
  - los tres informes de la especificación con el cuadro de mando (F56);
  - el bloqueo y la anonimización de datos con la entrega de la historia en PDF (F73), porque los dos salen de la política de protección de datos.
- **Orden de las líneas.** Primero van las funciones de la especificación, en su orden. Después, las nuevas que salen del 02 al 21, en el orden en que aparecen. En cada línea, los documentos van de menor a mayor número.
- **Canales separados.** He separado la app de la recepción cuando los documentos los tratan distinto:
  - cambiar o cancelar una cita: 12 h desde la app y lo que decida recepción por teléfono (F10 y F11);
  - pedir cita (F07 y F52);
  - cobrar en la clínica y pagar en la app (F39 y F40);
  - definir un plan y solicitarlo desde la app (F45 y F77).
- **Resultados de laboratorio.** Hay dos líneas: recibirlos y avisar al veterinario (F60), y que el veterinario los publique para el cliente (F67). Las hacen personas distintas en momentos distintos.
- **Decisiones posteriores dentro de cada línea.** He puesto en cada funcionalidad lo que se decidió después, no lo que decía la especificación al principio:
  - 12 h para cancelar desde la app;
  - recordatorio de vacuna 7 días antes y el día anterior;
  - traspasos libres, salvo los controlados;
  - fraccionamiento: las excepciones las autoriza gerencia.
- **Fuera de alcance.** He marcado así:
  - lo del apartado 8 de la especificación: tienda, chat y videoconsulta, nóminas y turnos, contabilidad completa;
  - la integración automática con la plataforma de prescripción, que la especificación deja para una fase posterior;
  - la señal económica, que gerencia deja fuera de la primera versión;
  - el programa de puntos, que es para el año siguiente.
- **Formulario de consulta no urgente (F72).** Va como funcionalidad normal y no como fuera de alcance: es lo que gerencia pone en lugar del chat.
- **Requisitos no funcionales.** No los he listado porque no son un botón ni algo que la plataforma haga sola: disponibilidad, rendimiento, copias de seguridad, uso en tabletas con guantes, idiomas, alojamiento en la UE, impresoras, soporte y pruebas de intrusión. Solo he apuntado dos: el trabajo sin conexión (F82), porque cambia lo que el usuario puede hacer, y el segundo factor dentro del inicio de sesión (F61).
- **Detalles, no líneas propias:**
  - la comunicación de la antirrábica al registro oficial, que sigue siendo manual;
  - la cancelación de solicitudes al laboratorio y la falta de webhooks;
  - la incineración;
  - las llamadas al cliente durante el ingreso;
  - la felicitación de cumpleaños;
  - los huecos preferentes para clientes con plan;
  - las urgencias de Huellas Norte en 2027.
- **Documentos sin líneas propias.** Estos documentos no añaden líneas nuevas: solo aparecen como fuente en líneas que ya existían.
  - El 06, 08, 16, 17, 18 y 19 son sobre todo datos.
  - En el 02, 03, 10, 11 y 13, todo lo que cuentan ya estaba en la lista; lo he añadido como detalle, por ejemplo las series rectificativas y el doble cierre de caja del 11.
- **Catálogo de diagnósticos (19), revisado entero.** No añade funcionalidades, solo detalles de F14. He visto tres cosas raras en los datos, que no son funcionalidades:
  - tiene 72 diagnósticos y «Otro (texto libre)», pero en la reunión con veterinarios (03) se habla de «unos 300»;
  - los cinco diagnósticos de exóticos tienen la misma lista de especies («conejo, hurón, reptil»), aunque cada uno es de una sola especie, como el insulinoma del hurón;
  - no hay ningún diagnóstico de aves, aunque Huellas Centro las atiende.
