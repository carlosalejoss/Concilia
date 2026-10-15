# Notas de análisis — Despensa Digital

Autora: Lucía Pardo (Estudio Norte Software). Última revisión: 15 de septiembre de 2026.

## Resumen del dominio

- **Familia beneficiaria:** unidad que recibe lotes. Tiene una persona de referencia, miembros con edad, dietas o alergias y una derivación de Servicios Sociales con fecha de caducidad.
- **Derivación:** documento de Servicios Sociales que autoriza a atender a una familia durante un tiempo. Sin derivación vigente no hay lote.
- **Lote:** conjunto de productos que se entrega en cada recogida. Su composición depende del tamaño de la familia y se adapta a las alergias y a lo que haya en el almacén.
- **Recogida:** cita en una franja de 15 minutos, los martes o jueves de 17:00 a 20:00, con un máximo de cuatro familias por franja.
- **Entrada de almacén:** producto donado, con origen, donante, cantidad, kilos y caducidad.
- **Turno de voluntariado:** franja con una tarea (preparar o repartir) y un número de voluntarios necesario.

## Decisiones que hay que tomar con la asociación

1. **Frecuencia de recogida.** El acta dice cada quince días; Marta dice que las familias de tres o más vienen cada semana; el correo de la junta dice cada quince días sin excepciones, con ampliación a criterio de la coordinadora si lo pide la trabajadora social.
2. **Caducidad de la derivación.** Seis meses según Servicios Sociales; la hoja actual usa un año. ¿Qué pasa con las derivaciones antiguas?
3. **Entrega de cortesía** cuando la derivación ha caducado: ¿se permite una, como hacen ahora, o se bloquea?
4. **Recordatorios:** SMS (acta) o WhatsApp (correo de la junta).
5. **Funcionamiento sin conexión** en el almacén: la cobertura es mala al fondo.
6. **Curso de manipulador de alimentos:** validez del certificado (¿cuatro años?) y si se debe impedir apuntarse a un turno de frescos sin él.
7. **Supermercado solidario con puntos:** ¿entra en esta versión?
8. **Sustituciones de productos** cuando falta algo en un lote: ¿las propone la aplicación?, ¿con qué reglas?
9. **Persona autorizada para recoger:** ¿se registra solo el nombre o también el DNI?

## Requisitos no funcionales que intuimos

- Datos personales y de salud (alergias, dietas): son de categoría especial; acceso restringido por perfil y registro de quién consulta.
- Uso desde móvil y tablet en el almacén; pantallas sencillas para voluntarios mayores.
- La asociación no tiene personal técnico: la aplicación tiene que ser fácil de mantener y alojarse en un servicio gestionado.
