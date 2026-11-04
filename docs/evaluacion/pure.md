# Paso 105: el flujo completo con un documento público (PURE)

Hasta ahora todo se había probado con documentos hechos por Claude o internos. Este es un documento público, ajeno al proyecto, con el que el tribunal puede repetir la prueba. Lo ejecutó Claude por la API el 04-11-2026, a petición del autor, con la versión `evaluacion-1` (`docs/evaluacion/version.md`): A `SS - GPT-6-SOL`, B `SS - GPT-6-LUNA`, árbitro, sugerencias y mockup SOL, prompts v0.12.

## El documento

- **Fuente:** el dataset PURE, versión 2.0 (Ferrari, Spagnolo y Gnesi, «PURE: a Dataset of Public Requirements Documents», RE 2017), en Zenodo: registro 7118517, DOI 10.5281/zenodo.7118517. Licencia del dataset: **CC BY 4.0**.
- **Documento:** `req/2005 - microcare.pdf`, la *Software Requirements Specification* del *Voucher Management System* de Microcare Ltd (diciembre de 2005).
  - Es un sistema para un programa de vales de salud en Uganda: se crean y venden vales, los centros sanitarios atienden a los pacientes y reclaman el pago, y se gestionan devoluciones, opiniones de los clientes, informes y usuarios con permisos.
  - Tiene 33 páginas, unos 11.000 tokens, en inglés. SHA-256: `116bea5470a773e4b5917e2e27b06a8df8c7e8da9f336752b4423b09c36471f0`.
- **Por qué este:** de los documentos de PURE de 10 a 40 páginas, es una especificación industrial y no un ejercicio de clase, con varios módulos y usuarios con permisos, y el texto se extrae bien (877 líneas, con títulos y listas). Alternativa: `2008 - vub.pdf`, un trabajo de clase. Descartados: `keepass` (un solo usuario), `puget sound` (marcado como «Confidential») y `cctns` (corto y con datos policiales).
- **No se copia en el repositorio,** como en el paso 123. La licencia del dataset lo permitiría, pero el PDF lleva «Copyright © 2005 by Microcare Ltd» y el repositorio todavía no tiene licencia. Para repetirlo, se descarga `requirements.zip` del registro de Zenodo y se usa el PDF tal cual (el SHA-256 de arriba lo comprueba). Si el autor lo prefiere, se puede añadir a `ejemplos/pure/` con su atribución.

## Qué se hizo

Proyecto «PURE · microcare», con el idioma en inglés (`en`) y el sistema «the Voucher Management System». Se subió el PDF, se cerró la documentación y se recorrió el flujo:

1. **Una ronda de preguntas.**
2. **Sugerencias de las 40 preguntas** y «Aprobar todas las sugerencias», que solo aprueba las que salen de la documentación (1).
3. **Las 39 que quedaron,** todas suposiciones de la IA:
   - **11 propuestas** («We propose…»), guardadas tal cual (origen `ia`);
   - **28 decisiones del cliente** («A decidir por el cliente:»), respondidas con la primera opción, como dice el plan (origen `ia-editada`). Muchas no ofrecían opciones, sino que pedían al cliente un dato (la matriz de tratamientos, los permisos, los plazos…). La regla aplicada:
     - en cada elección entre opciones, la primera;
     - un «whether X» sin alternativa, «sí»;
     - lo que solo el cliente puede aportar, «still to be decided by the client».
     Las 28 respuestas están en `pure/respuestas-decisiones-cliente.json`, para repetirlo igual.
4. **El catálogo, validado sin editar:** solo se marcaron como revisadas («La he revisado») sus 7 discrepancias, que la plataforma exige revisar antes de validar.
5. **El backlog, validado igual,** con sus 7 discrepancias marcadas como revisadas.
6. **El mockup.**

El informe confirma que no se editó nada: 122 requisitos y 34 historias conservados, ninguno modificado, añadido ni borrado.

## Resultados

Todo salió a la primera: ninguna reparación, ninguna ejecución fallida ni detenida.

| Fase | Minutos | Tokens de entrada | Tokens de salida | Coste | A | B | Unión | De ambas | Solo A | Solo B | Discrepancias |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Preguntas | 4,4 | 189.700 | 37.575 | 0,57 $ | 37 | 23 | 40 | 20 | 17 | 3 | 0 (3 desviaciones) |
| Sugerencias | 2,0 | 48.748 | 12.848 | 0,23 $ | — | — | 40 | — | — | — | — |
| Catálogo | 9,6 | 304.654 | 82.243 | 0,98 $ | 142 | 115 | 122 | 98 | 17 | 7 | 7 (1 del árbitro y 6 desviaciones); 1 descartado |
| Backlog | 4,8 | 233.619 | 49.260 | 0,72 $ | 50 | 28 | 34 | 27 | 6 | 1 | 7 desviaciones |
| Mockup | 25,4 | 1.431.393 | 394.238 | 6,81 $ | — | — | 21 pantallas | — | — | — | — |
| **Total** | **46** | **2.208.114** | **576.164** | **9,29 $** | | | | | | | |

Coste con los precios de `scripts/evaluacion/precios.json`; detalle en `docs/costes/costes.md`. Como referencia, el mismo flujo costó 22,25 $ y 105 minutos con clínicas (unos 23.400 tokens de documentación, v0.9) y 15,99 $ y 75 minutos con el banco de alimentos.

**El catálogo:**
- 122 requisitos: 110 `RF`, 9 `RNF`, 2 `RI` y 1 `RT`;
- 114 en EARS y 8 en MASTeR; 120 *shall* y 2 *should*;
- citan 156 fragmentos distintos.

44 llevan la marca «Suposición de la IA» (citan alguna de las 11 propuestas aceptadas tal cual), y 24 tienen `[TBD]` donde el cliente no ha decidido.

**El backlog:**
- 6 épicas, 34 historias, 102 criterios de aceptación y 76 tareas;
- todos los criterios y tareas, citados por su id por el árbitro (paso 108).

**El mockup:**
- 21 pantallas en inglés, en un `.html` de 718 KB que no carga nada de fuera;
- empieza en el inicio de sesión con las cuentas de prueba a la vista;
- 22 ejecuciones: el diseño y una por pantalla, sin correcciones.

## Qué fue bien

- **El flujo entero funciona en inglés a la primera,** con un documento que nadie del proyecto había visto: preguntas, sugerencias, catálogo, backlog y mockup salen en inglés, sin reparaciones.
- **LUNA propone en todas las fases** (23 preguntas, 115 requisitos, 28 historias). En la ronda centrada en inglés del paso 123 no propuso ninguna pregunta: no se repite aquí.
- **Lo que el cliente no ha decidido no se inventa:** las respuestas «still to be decided by the client» llegan al catálogo como `[TBD]` (24 requisitos), en lugar de convertirse en una cifra inventada.
- **Más barato que los proyectos de ejemplo,** por ser un documento más corto: 9,29 $ y 46 minutos.

## Qué fue mal o hay que mirar

- **Casi todo son suposiciones:** de las 40 sugerencias, solo 1 sale de la documentación. Las preguntas piden lo que el documento no dice (formatos de códigos, reglas de pago, permisos…), así que es coherente, pero el catálogo descansa mucho en respuestas de la IA: 44 requisitos con suposición y 24 con `[TBD]`. Con un cliente de verdad, esas 39 respuestas serían suyas.
- **«Primera opción» no basta como regla:** 28 de las 39 eran decisiones del cliente, y muchas no daban opciones, sino que pedían un dato. Hubo que concretar la regla (arriba) para poder seguir sin inventar.
- **La interfaz de la plataforma está en español, también en lo que exporta:**
  - el prefijo «A decidir por el cliente:» lo pide así el prompt de sugerencias y el modelo lo copia en español;
  - la narrativa de las historias la monta la plataforma con «Como… quiero… para…», también en Gherkin y en la vista de comparación, y las tareas salen como «Tarea T-001»;
  - los títulos, leyendas y anexos de Word y PDF van en español.

  El contenido de los modelos sí está en inglés. Corregirlo cambiaría el código (una `evaluacion-2`), pero no obligaría a repetir ninguna ejecución con modelos: las exportaciones se generan sin IA a partir de lo guardado. Para el paso 104 no es un obstáculo: la entrada de AQUSA se monta con los campos `como`, `quiero` y `para` del backlog y la plantilla inglesa «As a… I want… so that…».
- **Los requisitos sin condición empiezan en minúscula** («the Voucher Management System SHALL…»), porque el nombre del sistema se dio con su artículo en minúscula, como hace la plataforma por defecto en español («el sistema…»). Puede contar en `vale-ears` (paso 104).
- **El árbitro del backlog adopta la granularidad de B:**
  - cada historia de B está en una sola historia de la unión, y 14 historias de la unión juntan 2 o 3 de A (las 50 de A están todas, ninguna se pierde);
  - de los 176 criterios y 130 tareas de las historias que une, no cita 74 criterios y 54 tareas: pueden estar repetidos o combinados, según su aviso, o perdidos.

  Es lo que mide el paso 120 frente a la referencia.

## Archivos

En `docs/evaluacion/pure/`:

| Fichero | Qué es |
| --- | --- |
| `catalog.docx` y `catalog.pdf` | el catálogo validado |
| `backlog-gherkin.zip` | el backlog validado, un `.feature` por épica |
| `mockup.html` | el mockup |
| `informe-proyecto.csv` | el informe de cada trabajo del proyecto |
| `respuestas-decisiones-cliente.json` | las 28 respuestas a las decisiones del cliente |

## En palabras sencillas

Se tomó un documento de requisitos público, de una empresa real de 2005, que nadie del proyecto conocía, y se pasó por Concilia de principio a fin en inglés, sin corregir nada a mano. Todo salió a la primera y costó unos 9 dólares en tres cuartos de hora. Los dos modelos aportaron en todas las fases. Como el documento deja muchas cosas sin decir, la mayoría de las respuestas a las preguntas las propuso la IA, y lo que solo puede decidir el cliente quedó marcado como pendiente en lugar de inventarse. Lo que falta es traducir los textos fijos de la propia plataforma, que siguen en español.
