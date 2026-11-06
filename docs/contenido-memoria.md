# Contenido de la memoria: qué tiene que estar

Lista de lo que pesa en el proyecto y debería aparecer en la memoria del TFG, sacada de `docs/`, del README, del código y de los tests el 04-11-2026. Va ordenada según la estructura de la memoria (1 Introducción; 2.1 Especificación; 2.2 Construcción; 2.3 Validación; 3 Conclusiones). Cada punto dice de qué documento salen sus cifras.

**★ = imprescindible.** El resto entra si cabe o va a un anexo. La memoria es de tipo B (unas 10.000 palabras sin resumen, índices, bibliografía ni anexos) y el borrador ronda ya las 9.550 con la 2.3 sin escribir, así que hay que elegir y llevar el detalle a los anexos.

## 1. Introducción: problema, hipótesis y forma de trabajo

- ★ **El problema:**
  - un modelo de IA puede olvidar, inventar o contradecirse, y no hay una forma barata de saberlo;
  - en un análisis real, la documentación llega en formatos muy distintos y en varias tandas.
- ★ **La hipótesis:**
  - la verificación cruzada (A ‖ B → árbitro) es más fiable que un solo modelo;
  - la pregunta del título: si ahorra trabajo al analista (`README.md`).
- **Alcance:**
  - tres fases con árbitro (preguntas, catálogo y backlog) y el mockup con un solo modelo;
  - fuera de alcance: autenticación, varios usuarios y prioridad P0–P3.
- **Contexto técnico:**
  - modelos solo a través de la pasarela LiteLLM, con una ventana de 128k tokens;
  - SOL cuesta 20 veces más que LUNA (`docs/costes/costes.md`);
  - solo datos ficticios o públicos; el documento B está autorizado y va anonimizado.
- ★ **Método de trabajo:**
  - un plan por hitos (0 a 23) y unos 120 pasos, con un commit por paso: 259 commits en unas 5 semanas (`docs/plan-de-construccion.md`);
  - el autor decide y se apoya en Claude Code para implementar (anexo VII);
  - trazabilidad documental: bitácora, trazabilidad, evolución de los prompts y costes;
  - dos principios: «medir antes de cambiar» y «justificar cada cambio por su coste».
- **Referencias base:**
  - notaciones y normas: EARS, MASTeR/SOPHIST e ISO/IEC/IEEE 29148;
  - Großer et al. 2024, Gherkin y QUS/AQUSA;
  - PURE (Ferrari, Spagnolo y Gnesi, RE 2017) y sentence-transformers;
  - para el experimento del sesgo, la literatura sobre la preferencia de un modelo por sus propias salidas (*self-preference*) cuando hace de juez.

## 2.1 Especificación del sistema

- ★ **El flujo.** Documentos → rondas de preguntas → catálogo → validación → backlog → validación → mockup:
  - el catálogo exige al menos una ronda terminada y ninguna pregunta pendiente;
  - cada fase solo ve la versión validada de la anterior;
  - un cambio desactualiza en cascada los pasos siguientes.
- ★ **El patrón de verificación cruzada:**
  - A y B son independientes: el prompt no menciona al otro modelo ni al árbitro;
  - el árbitro recibe las propuestas anónimas y en orden aleatorio, y empareja de varios a varios;
  - clasifica cada elemento como de ambas, solo de A, solo de B, discrepancia o descartado;
  - si A o B fallan, el trabajo falla: no hay resultado de un solo modelo;
  - las discrepancias las decide el usuario.
- **Entregables:**
  - catálogo en EARS (por defecto) y MASTeR (no funcionales y obligatoriedad), con tipos RA, RT, RF, RI y RNF;
  - backlog: épica → historia → criterios en Gherkin → tarea;
  - mockup navegable con inicio de sesión, roles y referencia de estilo `.zip`.
- **Entradas:** 17 formatos (docx, pdf, xlsx, pptx, eml, msg, vtt, srt, yaml…), divididos en fragmentos con id citable (`DOC-01#F007`).
- **Preguntas:**
  - sugerencias de la IA con su origen registrado (`usuario`, `ia`, `ia-editada`);
  - sugerencias prudentes: «A decidir por el cliente» y la marca «Suposición de la IA».
- ★ **Iteraciones del proyecto, lo más diferencial** (hito 22):
  - modos «todas las iteraciones» y «centrarse en…»;
  - la lectura relacionada (lo nuevo y lo relacionado), frente a la lectura completa (leerlo todo) y a «solo lo nuevo»;
  - una respuesta del cliente nunca se reescribe;
  - lo que la IA modifica o elimina es una propuesta que el usuario revisa;
  - ids estables, y la iteración de cada elemento la calcula la plataforma.
- **Funciones de apoyo:**
  - trazabilidad requisito ↔ fragmento ↔ respuesta ↔ historia;
  - comparación A | árbitro | B;
  - historial de versiones (ver, comparar y recuperar);
  - informe por trabajo y CSV;
  - exportaciones.
- **Requisitos no funcionales:**
  - confidencialidad: secretos protegidos y embeddings calculados en local;
  - una tarea cada vez en toda la plataforma;
  - coste acotado y robustez frente a los fallos de los modelos;
  - despliegue en Docker y bibliotecas con licencias permisivas.

## 2.2 Construcción del sistema

- ★ **Arquitectura:**
  - React 19 + TypeScript + Vite + Tailwind; .NET 10 (Api, Core y Runner); PostgreSQL con EF Core; Docker Compose (db, api, web/nginx);
  - unas 27.600 líneas de C# en 210 ficheros, unas 12.000 de tests y unas 6.600 de TypeScript;
  - 42 endpoints con OpenAPI que se actualiza solo.
- ★ **El runner:**
  - Claude Code como agente (`claude -p`), con stream-json y veredicto obligatorio;
  - latido y tiempos máximos;
  - entorno construido de cero y `CLAUDE_CONFIG_DIR` vacío.
- ★ **Hallazgos del runner** (`docs/hito-3-runner.md`, `docs/hito-5-catalogo.md`):
  - `--allowedTools` no limitaba las herramientas: SOL usó `Bash`. Desde entonces se pasan `--tools "Read,Write"` y `--disable-slash-commands`, y de paso el consumo bajó mucho;
  - LUNA gastaba los 32k de salida razonando y su `Write` llegaba cortado: el tope se subió a 64k.
- ★ **Lectura por partes** (hito 12b):
  - las entradas van en `input/` y el modelo sigue un plan de lectura con `offset` y `limit` exactos;
  - la plataforma comprueba que no queda ninguna línea sin leer;
  - cuesta un 50 % más cuando el documento cabe entero, pero es imprescindible cuando no cabe.
- ★ **Validación determinista y reparación:**
  - JSON Schema, con un validador portado de Python que sigue siendo la referencia en la integración continua;
  - reparar editando (v0.11): el modelo corrige solo las líneas con error;
  - `PlatformFixes`: lo que tiene una sola corrección posible lo corrige la plataforma, con un aviso.
- **Robustez:**
  - reintento por inactividad y control de las compactaciones;
  - cierre de los trabajos interrumpidos y mockup reanudable;
  - consola guardada en disco.
- ★ **Embeddings en local** (hito 17, `docs/calibracion-embeddings/`):
  - MiniLM multilingüe en ONNX fp32, comprobado frente a sentence-transformers (diferencia de 9,4·10⁻⁷), con el SHA-256 fijado;
  - emparejamiento con umbrales 0,90 y 0,70, calibrados con 48 pares etiquetados por el autor (opción D, 77 % de acierto);
  - las desviaciones del árbitro respecto a ese emparejamiento son la «discrepancia grave»;
  - limitación: no distingue cifras distintas en frases casi iguales, como «1 año» y «8 años».
- **El árbitro de la v0.12** (hito 18b):
  - catálogo: consulta solo las fuentes que necesita (298 líneas en lugar de 1.242; 1,43 $ en lugar de 3,20 $);
  - backlog: cita por su id los criterios que conserva (239 criterios en lugar de 121).
- **Evolución del mockup:**
  - de un solo `.html` a una aplicación construida pantalla a pantalla;
  - la plataforma junta las partes y comprueba los ids, las globales de JavaScript y que se llega a todas las pantallas;
  - 8 direcciones de estilo;
  - es el 69–74 % del coste.
- **Exportaciones** (hito 19):
  - Word y PDF, con MigraDoc fijado en la 6.2.4 porque desde la 6.5.0 es AGPL;
  - Gherkin comprobado con el analizador oficial: una línea que empezaba por `Scenario:` creaba un escenario de más;
  - CSV para Jira, importado en un Jira Cloud real (límite de 250 elementos por fichero), y para Azure DevOps;
  - GitHub (hito 24, posterior a la versión evaluada): un `.zip` con un script que crea los issues con `gh`, 67 con su jerarquía en la prueba real.
- **Implementación de las iteraciones:** `FocusPlan`, el tope de 20.000 tokens de lo relacionado, `CatalogUpdate`, `BacklogImpact` (sin IA) y `PreviousVersion`.
- ★ **Evolución de los prompts** (`docs/evolucion-prompt-requisitos.md`):
  - por lotes: la v1.2 dio 1.133 y 862 requisitos con un documento grande, la v1.3 456 y 284, y la v1.4 255 y 214;
  - conclusión: el límite estaba en el proceso por lotes, no en el prompt. De ahí el cambio de rumbo a la v0 y, después, de la v0.1 a la v0.13.
- ★ **Calidad del software:**
  - 1.028 tests, con un test del flujo completo con el proveedor `Fake`;
  - una instantánea de la v0.12 vigila que los modos de siempre no cambien;
  - `OpenApiDocumentTests` e integración continua en tres trabajos (API, scripts de referencia en Python y frontend);
  - ningún test llama a la pasarela.
- **Medición integrada:**
  - informe por trabajo y `scripts/evaluacion/costes.cs`;
  - estimador del consumo ajustado con 125 ejecuciones: error del −0,7 % en el mockup y del −3,5 % en las fases (`docs/costes/estimador.md`).

## 2.3 Validación de la solución

**Ya hecho:**

- ★ **Paso 65** (`docs/resultados-paso-65/`):
  - dos proyectos ficticios: 7 archivos (5.900 tokens) y 21 archivos en 13 formatos (23.400 tokens);
  - 38 de 38 trampas tratadas como se esperaba;
  - coste: 8,76 $ y 55 minutos el mediano, 22,25 $ y 105 minutos el grande;
  - la unión se parece mucho a A.
- ★ **Experimento del sesgo del árbitro** (`docs/experimento-sesgo-arbitro.md`):
  - no hay preferencia del árbitro por su propio modelo: la unión se parece a A porque A propone más;
  - hallazgo secundario: LUNA es poco fiable como árbitro de un catálogo grande (2 válidos de 6).
- **El árbitro frente a un criterio humano:** junta los 9 de 9 pares «mismo» y solo 2 de 22 «distintos»; acuerdo por elemento del 89 %.
- **Estabilidad con la v0.10:** unión de 202–222 requisitos, frente a 209–291 con la v0.9.
- ★ **Coste** (`docs/costes/costes.md`):
  - B es el 0,5 % del coste del flujo: la verificación con un modelo barato es casi gratis;
  - el mockup domina el coste.
- ★ **Paso 116, iteraciones** (`docs/resultados-paso-116/`):
  - detección: 8 de 8 preguntas esperadas y los 14 cambios del catálogo;
  - actualizar conserva el 91 % de los requisitos y el 95 % de las historias validadas, frente al 0 % desde cero;
  - el coste es parecido: 5,85 $ frente a 5,44 $.
- **Paso 123** (`docs/resultados-paso-123/`): con 96.000 tokens, leerlo todo no cabe en el contexto y la lectura relacionada funciona por 0,87 $.
- ★ **Paso 105, PURE en inglés** (`docs/evaluacion/pure.md`):
  - un documento público que cualquiera puede repetir;
  - todo a la primera, en 46 minutos y por 9,29 $;
  - hallazgo: los textos fijos de la plataforma siguen en español.
- **Versión congelada `evaluacion-1`** (`docs/evaluacion/version.md`): garantiza la reproducibilidad.

**Pendiente, y es lo que responde a la hipótesis:**

- 104: AQUSA y `vale-ears`.
- 106: prueba como analista; sin ella, la métrica de cambios del usuario no tiene datos.
- 118: la referencia.
- 119: las repeticiones.
- ★★ 120: Concilia frente a un solo modelo (recall, precisión, F1 y coste).
- 121: SOL ‖ SOL.

**★ Amenazas a la validez, que hay que decir:**

- los proyectos ficticios y sus trampas los escribió Claude, que también desarrolla la plataforma;
- la referencia es de una sola persona;
- hay pocas repeticiones;
- las respuestas son de la IA aceptadas tal cual, no de un cliente real;
- el coste varía mucho entre ejecuciones.

## 3. Conclusiones

- ★ **Aportaciones propias:**
  - árbitro guiado por embeddings, con las desviaciones como señal para el usuario;
  - la plataforma corrige el formato en lugar de repetir el trabajo;
  - la lectura relacionada;
  - iteraciones con propuestas revisables;
  - medición del coste con el estimador.
- ★ **Lecciones:**
  - el proceso pesa más que el prompt;
  - los modelos fallan más en el formato que en el criterio;
  - un árbitro LLM necesita una referencia independiente;
  - el humano en el bucle no se puede quitar.
- **Trabajo futuro** («Después» de `docs/plan-de-construccion.md`):
  - catálogo v2 (aplazado), aprobación por umbral y leerlo todo cuando la documentación es pequeña;
  - textos de la plataforma en el idioma del proyecto y CSV solo con lo nuevo;
  - abaratar el mockup, partir el CSV de Jira en ficheros de 250 y probar Azure DevOps en real;
  - GitHub para cualquier usuario desde la web, organizaciones y Projects;
  - autenticación y varios usuarios.
- **Cambios de rumbo, como gestión del proyecto:**
  - 08-10, la iteración v0;
  - 26-10, se añade el hito 22;
  - 27-10, el hito 23 se amplía al ver que ningún paso medía la hipótesis.

## Aspectos profesionales transversales

- ★ **Seguridad y confidencialidad:**
  - `SecretRedactor`: ningún secreto en logs, eventos ni base de datos;
  - el entorno de cada ejecución, aislado;
  - el token de la suscripción, solo para el mockup;
  - el documento B, anonimizado.
- **Licencias:**
  - JsonSchema.Net y QuestPDF, descartados por sus condiciones para empresas;
  - MigraDoc pasó a AGPL, y se fijó la última versión MIT;
  - el copyright de los documentos de PURE impide copiarlos en el repositorio;
  - la licencia del repositorio sigue pendiente.
- **Reproducibilidad:** versión congelada, SHA-256 del modelo y de los documentos, proveedor `Fake` y semillas fijas.

## Qué falta en el borrador

Comprobado con una búsqueda de palabras clave en `memoria/tfg/memoria.tex` el 04-11-2026 (10:50):

- **El resultado del experimento del sesgo del árbitro:** la introducción cita el riesgo, pero no el experimento ni su resultado. Va en la 2.3.
- **La versión congelada `evaluacion-1`:** no aparece. Va al principio de la 2.3, como garantía de que las mediciones son comparables.
- **La lectura relacionada y su comparación con leerlo todo:** las iteraciones están, pero no las tres opciones de lectura ni los pasos 116 y 123.
- **Las amenazas a la validez:** hoy, solo en la valoración crítica. En la 2.3 conviene decirlas junto a los resultados.

Ya están cubiertos los embeddings y su calibración, las iteraciones, las exportaciones (Gherkin y Jira), la configuración y los secretos, el proveedor simulado y la calidad del software.
