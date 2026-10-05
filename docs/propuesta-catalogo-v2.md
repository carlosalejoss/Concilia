# Propuesta para el catálogo v2: de arriba abajo

Encargo para rediseñar la generación del catálogo de requisitos y redactar sus prompts. Sustituye el enfoque por lotes de las versiones 1.x. El historial de versiones y sus cifras está en `docs/evolucion-prompt-requisitos.md`.

## Por qué

La v1.4 se midió el 05-10-2026 con el documento técnico y funcional interno (608 fragmentos, 22 lotes). Del documento solo pueden darse cifras agregadas.

| | SOL | LUNA |
|---|---|---|
| Requisitos | 255 | 214 |
| Preguntas abiertas | 118 | 86 |
| Lotes perdidos | 0 | 2 de 22 |

Las cifras de forma son buenas: 100 % de citas literales y de fidelidad de cifras. El problema es el **tamaño**. Un catálogo de más de 200 requisitos y una lista de 100 preguntas no se pueden revisar ni contestar.

**No se fija un número de requisitos ni de preguntas.** El tamaño tiene que salir del documento: todas las funcionalidades, cada una una sola vez, y solo lo que debe estar en un catálogo. Un sistema puede dar 50 requisitos y otro 150. Lo que se controla es que el catálogo cumpla tres condiciones:

1. **Completo:** no falta ninguna funcionalidad del documento.
2. **Sin repeticiones:** cada funcionalidad y cada comportamiento aparece una sola vez, aunque el documento lo describa en varios sitios.
3. **Solo lo que es requisito:** nada de diseño interno, notas del proyecto ni detalles que pertenecen a otro requisito.

Las preguntas siguen la misma lógica: solo las que de verdad hay que contestar para construir el sistema, cada una una sola vez.

La causa no está en las reglas del prompt sino en el proceso, que va **de abajo arriba**:

- **Se trabaja por trozos.** El documento se trocea en lotes por secciones, cada lote se trabaja por separado y el validador exige que cada fragmento quede tratado (C10). El modelo saca algo de cada trozo.
- **Hay duplicados entre lotes.** El documento describe una misma funcionalidad en varios sitios: resumen, matriz de permisos, pantalla, backend, API y anexo. Cada sitio cae en un lote distinto, y ningún lote ve a los demás. En la v1.4, una misma operación aparece hasta 6 y 7 veces en cada modelo.
- **Las preguntas se multiplican por lote.** Cada lote abre sus propias dudas, también repetidas, y 22 lotes por 4 o 5 preguntas dan unas 100.
- **Cada versión mejora menos.** La regla de observabilidad de la v1.4 tuvo un efecto parcial (SOL) o nulo (LUNA). La de las preguntas de errores tuvo efectos opuestos en cada modelo.

Ninguna regla puede pedir a un lote que no repita lo que hacen otros lotes que no ve.

## Idea

Hacerlo como un analista: primero decidir **qué hace el sistema** (inventario de funcionalidades) y después escribir los requisitos de cada funcionalidad una sola vez, con el detalle en los criterios de aceptación.

**"Completo" pasa a significar que ninguna funcionalidad del documento se queda sin requisito**, no que cada fragmento genere algo.

## Diseño

### Fase 1: inventario global (una llamada por modelo)

**Entrada:** el documento entero, fragmentado con sus ids, y el índice.

El documento grande ocupa unos 25.000–30.000 tokens. Con unos 15.000 de contexto base de Claude Code y el prompt, cabe de sobra en la ventana de 128.000. Los lotes de la v1.x existían por el tope de **salida** cuando salían más de 1.000 requisitos, no por la entrada.

**Salida:** `inventario.json`, con cuatro listas.

- **Funcionalidades.** Para cada una:
  - `id`, `name` y una descripción breve (una frase);
  - actores;
  - `kind`: `functional`, `integration`, `quality` (atributo de calidad exigido) o `constraint` (restricción impuesta de arquitectura o tecnología);
  - `fragments`: todos los fragmentos que hablan de ella, estén en la sección que estén. Así se juntan aquí las descripciones repetidas en varias partes del documento.
- **Secciones y fragmentos descartados.** Con los motivos de la v1.3, que siguen valiendo: `context`, `design_detail`, `project_note`, etc.
- **Preguntas de alcance y contradicciones globales.** Las que afectan a qué entra en el sistema.

**Nivel de una funcionalidad:** lo que un usuario o un sistema externo reconocería como una función del producto (una pantalla, un proceso o una integración), como en la v1.4. No se fija un número: salen las que tenga el documento, sin repetir ninguna aunque se describa en varios sitios.

**Validación de la plataforma:**
- **Ids:** existen, sin repetidos.
- **Cobertura:** cada fragmento del documento está en alguna funcionalidad o descartado. Es el C10 trasladado a esta fase.
- **Posibles repeticiones:** aviso cuando dos funcionalidades comparten muchos fragmentos o tienen nombres muy parecidos.

La fase 1 admite **una reparación**, como hoy.

### Entre fases: la plataforma reparte

La plataforma agrupa las funcionalidades en lotes según el tope de salida estimado. **Cada funcionalidad va a un único lote.** Si el catálogo completo cabe en una salida, hay un único lote.

Cada lote recibe:
- sus funcionalidades, con sus fragmentos;
- el índice;
- el inventario completo, solo nombres e ids, para saber qué cubren los demás y no invadirlo.

### Fase 2: requisitos por funcionalidad

**Los requisitos de cada funcionalidad.** Uno por cada comportamiento distinto, y no más:
- su comportamiento principal;
- las variantes que cambian el comportamiento de verdad, por condición o por estado;
- los atributos de calidad que la fuente le exija.

Lo que no cambia el comportamiento no es un requisito aparte: otro actor que hace lo mismo, un campo más, un mensaje o un límite. Cuántos requisitos salgan depende de la funcionalidad, no de una cifra.

**El detalle va en criterios etiquetados**, como en la v1.3:
- los roles y permisos son criterios `[RULE]`, no un requisito por actor;
- los errores son criterios `[ERROR]`;
- los límites son `[LIMIT]`;
- los mensajes son `[MESSAGE]`.

**Tipos:**
- las funcionalidades `quality` dan `RNF`, una por atributo;
- las de tipo `constraint` dan `RA` o `RT`;
- las de tipo `functional` e `integration` dan `RF` o `RI`.

Se conservan:
- EARS y MASTeR con slots y texto generado por la plataforma;
- las citas literales;
- la obligatoriedad;
- la no invención;
- la independencia de A y B.

**Solo las preguntas necesarias.** Las que **bloquean**: contradicción, alcance, valor sin el que no se puede construir, ambigüedad que cambia el comportamiento o actor que falta.
- Una duda se pregunta **una sola vez**, aunque afecte a varios requisitos o a varias funcionalidades; ante la duda, ninguna.
- **No se piden preguntas «¿qué pasa si falla?»** (`missing_error_handling`). La plataforma ya sabe qué funcionalidad no tiene ningún criterio `[ERROR]` y lo lista ella aparte, sin ocupar preguntas.

### Después: la plataforma une

- **Unión de lotes:** como hoy, renumerando por tipo. No hace falta deduplicar entre lotes: cada funcionalidad está en uno solo.
- **Orden de las preguntas:** se muestran por tipo (contradicción, alcance, valor, ambigüedad y actor), para contestar primero las que más cambian el catálogo. No se recorta ninguna.
- **Funcionalidades sin requisito:** C20, como hoy, provoca la reparación del lote.

## Esquema (v2)

Dos esquemas en lugar de uno:

- **`inventory_v2.schema.json`** (fase 1): `capabilities` con `description`, `actors` y `kind`; `discarded_statements`, `discarded_sections`, `open_questions` de alcance y contradicción, y `verdict`.
- **`requirements_v2.schema.json`** (fase 2): igual que el 1.3, con estos cambios:
  - las funcionalidades no se reescriben: los requisitos las referencian por `capability_ids`;
  - sin `discarded_statements` ni `discarded_sections`, porque los descartes son de la fase 1;
  - sin el tipo de pregunta `missing_error_handling`;
  - sin `self_check` en el veredicto: en las ejecuciones reales nunca coincidió con el validador, y gasta tokens.

## Qué no cambia

- El runner (`claude -p` con Read y Write), el veredicto y la pasarela.
- Que A y B trabajen sin saber del otro. El árbitro podrá comparar ya los inventarios de la fase 1, y es una ventaja: comparar 40 funcionalidades es mucho más fácil que comparar 200 requisitos. Cómo lo haga sigue siendo una decisión abierta.
- El fragmentador, los ids de fragmento, el índice y el validador en Python como referencia del de C#.

## Riesgos

- **Razonamiento de LUNA en la fase 1:** con el documento entero, puede razonar mucho antes de escribir. El inventario es corto (unos 5.000–10.000 tokens de salida), pero hay que vigilar el tope de salida (64.000) y el tiempo de inactividad del runner.
- **Documentos más grandes que la ventana:** quedan fuera de esta versión. Más adelante, la fase 1 podría hacerse por partes y unir los inventarios.
- **Funcionalidades olvidadas en la fase 1:** el riesgo se concentra ahí. Se mide con la lista de referencia hecha a mano y comparando los inventarios de A y B.

## Cómo se medirá

- **Documentos:** el documento grande, ReservaSalas y 1 o 2 de PURE. Dos ejecuciones por configuración.
- **Métricas:** las de `docs/evolucion-prompt-requisitos.md`, más estas:
  - **completo:** funcionalidades de la lista de referencia hecha a mano que faltan en el inventario;
  - **sin repeticiones:** requisitos y preguntas repetidos (mismo comportamiento o misma duda más de una vez);
  - **solo lo que es requisito:** requisitos que deberían haberse descartado, revisados a mano en una muestra;
  - funcionalidades del inventario, requisitos por funcionalidad y preguntas por tipo, como descripción, no como objetivo.
- **Medición automática:** un informe de métricas generado por la plataforma, no contado a mano.

## Qué se pide como entrega

1. **`prompt_inventario_v2.md`:** prompt de la fase 1, con su anexo de reparación.
2. **`prompt_requisitos_v2.md`:** prompt de la fase 2, partiendo de la v1.4. Debería ser **más corto**, porque pierde el inventario, los descartes, N4 y la autoverificación.
3. **Los dos esquemas v2 y la entrada v2 del `CHANGELOG.md`.**
4. **Ejemplos:** un ejemplo de inventario y un ejemplo de catálogo para ReservaSalas (en español y en inglés), como en la v1.3.

Si alguna regla necesita una comprobación automática nueva, hay que indicarlo: el validador de referencia en Python y su port en C# se cambian a la vez.
