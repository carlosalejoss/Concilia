# Versión que se evalúa

Paso 117 del plan. Todas las ejecuciones del hito 23 se hacen con la versión de una etiqueta, y cada resultado dice con cuál se obtuvo. Así los resultados son comparables entre sí y el tribunal puede repetir el paso 105 con la misma versión.

Si durante la evaluación un error obliga a cambiar código o prompts, se crea la etiqueta siguiente (`evaluacion-2`…), se añade aquí una sección con qué cambió y por qué, y se repiten las ejecuciones afectadas.

## `evaluacion-1` (04-11-2026)

### Código

- **La etiqueta** `evaluacion-1` está en el commit del paso 117, `bbe1167` (04-11-2026), que solo añade documentación. Es local: no se ha subido al remoto.
- **El código** es el del paso 122, commit `f063fd6` (03-11-2026, 13:34). Desde entonces solo han cambiado `docs/` y `CLAUDE.md`.
- **Hashes nuevos (05-11-2026):** el autor reescribió el historial para quitar de los mensajes la línea `Co-Authored-By` de Claude. Cambiaron todos los hashes, pero no el contenido, el autor ni las fechas de ningún commit: `bbe1167` era `6d34025` y `f063fd6` era `d0dc673`. Las referencias de la documentación se actualizaron entonces.
- **Las imágenes de Docker** se construyeron con ese código:

  | Imagen | Id | Construida |
  | --- | --- | --- |
  | `concilia-api` | `4013b9e31a86` | 03-11-2026, 13:44 |
  | `concilia-web` | `cb4ca6e92e95` | 03-11-2026, 13:46 |

  Comprobado el 04-11-2026: los 30 prompts y esquemas de `prompts/v0/` del repositorio están, byte a byte, dentro de la `Concilia.Core.dll` de la imagen de la API.
- **Tests,** el 04-11-2026, con `git status` limpio:
  - 1.028 tests de .NET, en verde;
  - `npm run lint` y `npm run build` de la web, sin errores;
  - los scripts de referencia de Python: `test_fragmentar.py` (16), `test_validador.py` (105) y `prompts/v0/test_v0.py` (46).

### Prompts

| Modo | Versión |
| --- | --- |
| Los de siempre: rondas de preguntas, sugerencias, catálogo y backlog desde cero, sus árbitros y el mockup | v0.12 |
| Las rondas centradas en unas iteraciones y la actualización del catálogo y del backlog | v0.13 |

La plataforma guarda la versión de cada ejecución (`PromptVersion` en `Runs`).

### Modelos

| Papel | Proveedor | Modelo |
| --- | --- | --- |
| A | `LiteLlm` (la pasarela) | `SS - GPT-6-SOL` |
| B | `LiteLlm` | `SS - GPT-6-LUNA` |
| Árbitro | `LiteLlm` | `SS - GPT-6-SOL` |
| Sugerencias de respuesta | `LiteLlm` | `SS - GPT-6-SOL` |
| Mockup | `LiteLlm` | `SS - GPT-6-SOL` |

Sacados de las ejecuciones guardadas, sin leer `.env`. A, B y el árbitro, de la prueba en inglés de este paso; las sugerencias, de la última (03-11-2026); el mockup, del último (26-10-2026). Los valores por defecto de `docker-compose.yml` son los mismos, salvo el mockup (`Fake` y `claude-sonnet-5-5`), que se cambia en `.env`.

### Configuración

Son los valores por defecto del código. Los tres marcados con * se pasan por `docker-compose.yml` y se podrían cambiar en `.env` (que no se lee); el valor por defecto del compose es el mismo.

| Valor | | Dónde |
| --- | --- | --- |
| Umbral «lo mismo» del emparejamiento | 0,90 | `Embeddings:SameThreshold` |
| Umbral «parecido» | 0,70 | `Embeddings:SimilarThreshold` |
| Modelo de embeddings | `paraphrase-multilingual-MiniLM-L12-v2`, ONNX fp32, commit de Hugging Face `e8f8c211` | `src/Concilia.Api/Dockerfile` |
| Lotes en paralelo del catálogo v1.4 | 5 | `Catalog:MaxParallelLots` |
| Pantallas del mockup en paralelo | 3 | `Mockup:MaxParallelScreens` |
| Tope de salida por respuesta | 64.000 tokens | `Runner:MaxOutputTokens`* |
| Contexto que asume Claude Code | 128.000 tokens | `CLAUDE_CODE_MAX_CONTEXT_TOKENS` |
| Turnos por ejecución de una fase | 30 | `Runner:PhaseMaxTurns`* |
| Tope de documentación de una fase | 300.000 tokens estimados | `Runner:MaxDocumentationTokens`* |
| Fragmentos relacionados en los modos centrados | desde 0,70, hasta 20.000 tokens | `RelatedContext.MaxTokens` |

### Claude Code

`2.1.283`, con `claude --version` dentro del contenedor de la API (04-11-2026). Es la versión fijada en el Dockerfile y la que registran las 419 ejecuciones guardadas con versión, del 09-10 al 04-11-2026.

### Prueba en inglés antes de congelar

El flujo en inglés solo se había probado en la ronda centrada del paso 123 (prompts v0.13). Antes de congelar se probó la ronda de preguntas normal (v0.12), con el documento que se usará en el paso 105, para corregir cualquier fallo ahora y no a mitad de la evaluación. Resultado completo en `prueba-ingles-paso-117/` (`ronda-preguntas.json`, salida del árbitro con su emparejamiento, e `informe.json`).

- **Documento:** `2005 - microcare.pdf` del dataset PURE v2.0 (Zenodo, registro 7118517, CC BY 4.0). Es el SRS del *Voucher Management System* de Microcare (33 páginas, unos 11.000 tokens, en inglés). El texto se extrae bien: 877 líneas con los títulos y las listas.
- **Proyecto:** «PURE · prueba en inglés (paso 117)», idioma `en`, sistema «the Voucher Management System». Se borró después, como dice el plan.

| | A (SOL) | B (LUNA) | Árbitro (SOL) | Total |
| --- | --- | --- | --- | --- |
| Resultado | hecho | hecho | hecho | **válida a la primera, sin reparaciones** |
| Tokens de entrada | 50.484 | 48.368 | 93.128 | 191.980 |
| Tokens de salida | 8.408 | 9.939 | 11.910 | 30.257 |
| Coste | 0,18 $ | 0,01 $ | 0,31 $ | **0,50 $** |
| Minutos | 1,4 | 2,0 | 1,9 | **4,0** |
| Preguntas | 37 | 20 | 39 | |

- **La unión:** 39 preguntas (18 de las dos propuestas, 19 solo de A y 2 solo de B), ninguna descartada ni con discrepancia; 5 desviaciones respecto al emparejamiento, que en las preguntas solo se cuentan. El emparejamiento y el árbitro coinciden en el 70 % de los elementos.
- **Las preguntas, las razones y las notas del árbitro salen en inglés,** concretas y con 107 fragmentos distintos citados, todos válidos.
- **LUNA propone preguntas** (20). En la ronda centrada en inglés del paso 123 no propuso ninguna; con esta ronda, el inglés por sí solo no lo explica (era otro prompt y otro documento). Se sigue mirando en los pasos 105 y 119.
- **No hizo falta corregir nada.** Lo único que falló fue el cliente: el primer intento de crear el proyecto, con `curl` y el cuerpo en la línea de órdenes de Git Bash, llegó con la codificación rota y la API lo rechazó sin crear nada. Con el cuerpo en un fichero UTF-8 funcionó.

### Cambios después de la etiqueta que no tocan la aplicación

- **04-11-2026, paso 105:** `scripts/evaluacion/costes.cs` muestra por su nombre los proyectos «PURE …», que son públicos, en lugar de anonimizarlos. Es un script de evaluación: no cambia la API, la web ni los prompts.
- **04-11-2026, paso 104:** un script nuevo, `scripts/evaluacion/metricas-externas.cs`, que lee los resultados y ejecuta AQUSA y Vale fuera de la aplicación.
- **05-11-2026, paso 106:** `costes.cs` reconoce los proyectos de ReservaSalas por el prefijo «ReservaSalas», no solo «ReservaSalasHito911.v2», para que «ReservaSalas analista» no salga anonimizado.

### En palabras sencillas

Antes de medir, se fija una versión de Concilia y no se toca, para que todas las mediciones se hagan con la misma y cualquiera pueda repetirlas. Antes de fijarla, se probó una cosa que nunca se había usado: hacer las preguntas iniciales sobre un documento en inglés. Funcionó a la primera, por medio dólar, y los dos modelos aportaron preguntas.
