# Propuesta: prompts v0.14 (paso 143)

Propuesta de Claude del 13-11-2026, para que el autor la revise antes de tocar código, como en los pasos 112 a 114. El paso pide decidir qué se mejora con lo que fallaron SOL y el nuevo B en los pasos 130 y 131, más lo ya conocido. Lo de las otras fases se ha mirado con datos, y en ellas no se propone cambiar nada.

## En palabras sencillas

El árbitro del backlog ya no cabe en la memoria del modelo con el nuevo B, porque B propone muchas más historias y el árbitro lee las dos propuestas enteras, con todos sus criterios, además del catálogo entero. La propuesta es que lea un resumen de cada historia, consulte el texto completo de un criterio solo cuando lo necesite y reciba únicamente los requisitos que tiene que comprobar. Así leería menos de la mitad. Además, se le pide que no junte en una historia varias historias pequeñas que un modelo había separado. El resto de los prompts no cambia.

## Lo que se ha mirado

| Candidato (plan, paso 143) | Qué dicen los datos | Propuesta |
| --- | --- | --- |
| **El árbitro del backlog no cabe** (pasos 145 y 131) | Con el estimador de la plataforma (3,5 caracteres por token): una historia entera ocupa unos **378 tokens**; las propuestas de Kimi en clínicas (140 + 105 historias), unos **93.000**, frente a unos 68.000 las de LUNA (96 + 85), que cupieron justo. El catálogo validado ocupa unos **24.200** (228 requisitos) y unos **29.900** (329). Con el prompt y el contexto base, el árbitro de Kimi necesitaba unos 140.000 de 128.000. Quitar solo el catálogo no basta: quedaría al borde y sin sitio para escribir la salida. | **A** |
| **Que no junte historias atómicas de una misma propuesta** («Después», punto 7) | En los 13 backlogs guardados, 147 de 848 historias de la unión (17 %) juntan 2 o más de una misma propuesta; en PURE, 14 de 34. Desde el paso 141 la plataforma las marca como discrepancia, pero el árbitro sigue haciéndolo. | **B** |
| **39 de las 40 sugerencias de PURE eran suposiciones** (paso 105) | Las preguntas piden justo lo que el documento no dice (formatos de códigos, reglas de pago, permisos…). Las 39 suposiciones citan entre 1 y 10 fragmentos y explican el conflicto o el hueco («The example project and group-batch codes conflict with their stated lengths…»). El paso 105 ya lo dio por coherente: con un cliente de verdad, serían respuestas suyas. Tratar más respuestas como «de la documentación» haría que «Aprobar todas» aprobase suposiciones. | No cambiar |
| **Inventario de funcionalidades antes de los requisitos** (idea del hito 7, «solo si faltan funcionalidades») | Sin la referencia del autor (paso 118) no hay forma de saber si faltan funcionalidades. Lo que sí se ve es que con Kimi la unión gana requisitos: 17 solo de B y solo 7 de SOL sin pareja (paso 131). Es un cambio grande del catálogo, sin un fallo medido que lo pida. | No hacer |
| **Avisos falsos de un par «mismo» con cifras distintas** («Qué hay que mejorar» de la v0.12) | Los embeddings ven iguales «un año» y «ocho años». Si el árbitro los separa, hace bien, pero la plataforma lo marca como desviación «separa iguales». | **C**, opcional |

## A. El árbitro del backlog lee menos

**Qué cambia** (solo `arbitro_backlog.md` y lo que la plataforma le prepara):

1. **Las propuestas, en forma compacta:** `input/propuesta_1.txt` y `input/propuesta_2.txt`, una línea por historia:

   ```
   1:HU-012 «Anular una cita» · como recepcionista · quiero anular una cita · para liberar el hueco · requisitos: RF-031, RNF-004 · criterios: 1:CA-027 «Anulación con aviso», 1:CA-028 «Cita ya pasada» (criterios.txt, líneas 61–62) · tareas: 1:T-044, 1:T-045
   ```

   Unos 100 tokens por historia en lugar de 378: las propuestas de Kimi pasarían de unos 93.000 a unos 25.000.
2. **El texto de cada criterio y tarea, para consultar:** `input/criterios.txt`, una línea por criterio (`1:CA-027 Dado … Cuando … Entonces …`) y por tarea. Cada historia dice en qué líneas están los suyos. Se lee **por tramos**, como la documentación del árbitro del catálogo (v0.7):
   - siempre, los de las historias que une con un par `parecido` o `sin-par`, para combinar o marcar una discrepancia;
   - en un par `mismo`, solo si dos escenarios parecen el mismo y hay que decidir si uno repite al otro.
3. **Solo los requisitos que hay que comprobar:** `input/requisitos.txt`, con los requisitos que citan las historias sin un par `mismo`, en lugar del catálogo entero. Es lo mismo que hizo el paso 107 con las fuentes del árbitro del catálogo.
4. **Fuera del prompt la comprobación de la cobertura** («cada `RF` y `RI` del catálogo está en al menos una historia»): ya la hace la plataforma (aviso `COB`), y el árbitro no puede crear historias que no estén en ninguna propuesta.

**Cuánto leería:** unos 55.000–60.000 tokens en clínicas con Kimi, en lugar de unos 140.000, con sitio para consultar criterios y escribir la salida (unos 18.000 tokens con las citas por id del paso 108). Con el catálogo de 329 requisitos cambia poco, porque ya no lo lee entero.

**Qué se queda igual:** el formato de la salida (`output/backlog.json`), las citas por id de criterios y tareas (paso 108), el emparejamiento por embeddings y las desviaciones, lo que hacen A y B (`backlog.md` no cambia), la validación y el borrador.

**El riesgo:** en un par `mismo`, el árbitro deja de leer todos los criterios por defecto, y podría escapársele una contradicción entre dos criterios de nombre parecido. Lo compensan las consultas del punto 2 y las desviaciones que marca la plataforma. Se mide contando las discrepancias frente al backlog de LUNA del 23-10.

## B. No juntar historias atómicas de una misma propuesta

**Qué cambia** (en el §2 de `arbitro_backlog.md`): una historia unida no junta varias historias de una misma propuesta. Si una historia de una propuesta cubre varias de la otra, el árbitro mantiene las pequeñas, una por objetivo, y reparte entre ellas los criterios y las tareas de la grande, citándolos por su id. La grande va en el `de` de cada una de las pequeñas, de modo que no se pierde ni se descarta.

**Qué se queda igual:** la marca de la plataforma del paso 141, que sigue avisando si aun así lo hace; las demás reglas de unión.

**Comprobación técnica:** el validador ya admite una historia en el `de` de varias (el emparejamiento es de varios a varios), y las desviaciones también. Hay que comprobar que el aviso `ARB-CITA` cuenta bien los criterios repartidos entre varias historias.

## C. Opcional, solo de la plataforma: un par «mismo» con cifras distintas no es una desviación

Si el árbitro deja en dos elementos un par «mismo» cuyos textos llevan cifras distintas, la plataforma no lo marca como «separa iguales». Es el criterio de cifras que ya usa `fuentes.txt` (paso 107). No cambia ningún prompt y vale para el catálogo y el backlog. Si lo prefieres fuera del 143, va a «Después».

## Cómo se comprueba

1. **Tests**, sin modelos:
   - lo que la plataforma prepara para el árbitro (las líneas compactas, las líneas de cada historia en `criterios.txt` y los requisitos de las historias sin par `mismo`);
   - el validador con una historia grande repartida entre varias pequeñas;
   - la instantánea `V012SnapshotTests` del árbitro del backlog, actualizada a propósito, mientras las del resto de fases no cambian.
2. **Mediciones con modelos**, por este orden, de la más barata a la más cara. Antes de cada una, Claude estima el coste para una empresa, la parte de Go a precio de lista, y lo que queda de las ventanas de Go:
   1. **Nuevo arbitraje del backlog de «Clínicas SOL 6.1»** (paso 145, catálogo de 329 requisitos, A SOL y B LUNA): solo el árbitro, con SOL; nada de Go. Hoy falla.
   2. **Nuevo arbitraje del backlog de «Clínicas B Kimi»** (paso 131): las mismas propuestas de A y de Kimi con las que falló la v0.12; solo el árbitro, con SOL, y nada de Go. Es la comparación exacta frente al paso 131.
   3. **El backlog de «Clínicas B Kimi» como trabajo normal** (A SOL, B Kimi y el árbitro v0.14), para que el proyecto tenga su backlog y el flujo de clínicas con Kimi quede completo, frente al paso 131. Gasta Go: unos 0,6–0,8 $ de Kimi a precio de lista, según el paso 131.

   Un nuevo arbitraje no cambia el backlog del proyecto, por eso hace falta la tercera.
3. **Qué se compara**, frente al paso 131 (misma B, prompts v0.12) y al backlog de LUNA del 23-10:
   - si termina;
   - tokens leídos, compactaciones, reparaciones y correcciones automáticas;
   - historias y criterios conservados (el aviso `ARB-CITA`);
   - discrepancias, desviaciones y las historias que juntan varias de una misma propuesta (paso 141);
   - coste para una empresa y minutos.
4. **Preguntas y catálogo no se vuelven a medir:** sus prompts no cambian, y los resultados serían los del paso 131.

## Detalles

- **La versión:** las ejecuciones de las fases de siempre guardan `v0.14` a partir de este paso, aunque solo cambie el árbitro del backlog, como se hizo con la v0.12. Las rondas centradas y las actualizaciones siguen en `v0.13`.
- **Fuera de este cambio:** el árbitro de la actualización del backlog (`arbitro_backlog_actualizacion.md`), que la evaluación 2 no usa. Si después falla por tamaño, se le aplica lo mismo.
- **Documentación:** `prompts/v0/CHANGELOG.md`, `docs/evolucion-prompt-requisitos.md` (sección v0.14 con **Qué hace**, **Resultados** y **Qué hay que mejorar**), costes regenerados tras cada medición y la memoria.
