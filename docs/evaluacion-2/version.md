# Versión que se evalúa (evaluación 2)

Paso 133 del plan. Todas las ejecuciones del hito 27 se hacen con la versión de la etiqueta `evaluacion-2`, y cada resultado dice con cuál se obtuvo. Así los resultados son comparables entre sí y el tribunal puede repetir el paso 134 con la misma versión.

La evaluación 1 (SOL ‖ LUNA, etiqueta `evaluacion-1`) está descrita en `docs/evaluacion/version.md` y se interrumpió el 10-11-2026, cuando el modelo B pasó de LUNA a Kimi K3.1 Pro. Sus resultados son el «antes» de esta evaluación.

**Desde la etiqueta no se cambian el código ni los prompts.** Si un error obliga a cambiarlos, se para, lo decide el autor, se crea la etiqueta siguiente (`evaluacion-3`), se añade aquí una sección con qué cambió y por qué, y se repiten las ejecuciones afectadas.

## `evaluacion-2` (17-11-2026)

### Código

- **La etiqueta** `evaluacion-2` está en el commit de este paso, que lleva a la vez los últimos cambios de código (los de abajo) y esta documentación: el código que se evalúa es exactamente el de la etiqueta (`git rev-parse evaluacion-2`). Es local: no se ha subido al remoto.
- **Cambios de código de este paso,** antes de congelar («Después», punto 11, del plan): tres tests que fallaban al azar o según la consola y una carrera real en el mockup.
  - **`ClaudeCliRunnerTests.Compacting_after_writing_the_output_stops_the_run_at_once`** (fallaba 1 de cada 4 baterías desde el 23-10; reproducido el 16-11, 1 de 4). Tenía 2 segundos de tiempo total y, con la máquina cargada, el proceso falso tardaba más en arrancar: se agotaba el tiempo antes de llegar a la compactación. Ahora tiene 60 segundos y comprueba que se para antes de 15, como el test del `Write` cortado. Lo mismo en `Compacting_twice_before_writing_the_output_stops_the_run`, con el mismo riesgo. Solo cambian los tests.
  - **`MockupTaskTests.Problems_between_the_screens_are_fixed_one_part_at_a_time`** (fallaba 1 de cada 4 baterías el 13-11). No se pudo reproducir (25 de 25 aislado y 4 de 4 en batería), pero el código tiene una carrera que también puede darse en producción: las correcciones del mockup van en paralelo (hasta 3), y una copia toda la carpeta del prototipo y lee `app.html` mientras otra, ya aceptada, escribe allí su parte (`MockupTask.Keep`). En Windows esa copia puede fallar con un fichero en uso o leer una parte a medio escribir. Arreglo: un cerrojo corto alrededor de lo que se toma de esa carpeta y de lo que se escribe en ella; solo copia unos pocos ficheros pequeños, y la plataforma ejecuta un trabajo cada vez. Decisión del autor, como recomendaba Claude (17-11-2026). Solo afecta al mockup.
  - **`GitHubScriptTests`** (10 tests): fallaban todos si la batería se lanzaba desde PowerShell, porque el test arranca el `bash.exe` de Git sin poner su carpeta en el PATH y el script no encuentra `dirname`. Desde Git Bash pasaban. Ahora el test añade la carpeta de `bash` al PATH. Solo cambia el test.
- **Las imágenes de Docker** se construyeron con ese código el 17-11-2026, con `docker compose build --pull`:

  | Imagen | Id | Construida |
  | --- | --- | --- |
  | `concilia-api` | `2eb89aab59a6` | 17-11-2026, 08:30 |
  | `concilia-web` | `4b9606bba152` | 13-11-2026, 11:05 (la web no ha cambiado desde entonces: la construcción reutilizó la imagen) |

  Comprobado el 17-11-2026: los 30 prompts y esquemas que se incrustan (los 17 prompts y los 13 esquemas de `prompts/v0/`) están, byte a byte, dentro de la `Concilia.Core.dll` de la imagen de la API.
- **Tests,** el 17-11-2026, con los cambios de este paso:
  - 1.093 tests de .NET, en verde una vez desde PowerShell y tres seguidas desde Git Bash (una de ellas mientras se reconstruía la imagen, con la máquina cargada);
  - `oxlint` y `npm run build` de la web, sin errores;
  - los scripts de referencia de Python: `test_fragmentar.py` (16), `test_validador.py` (105) y `prompts/v0/test_v0.py` (46), estos dos en un entorno aparte con `jsonschema`.

### Prompts

| Modo | Versión |
| --- | --- |
| Los de siempre: rondas de preguntas, sugerencias, catálogo y backlog desde cero, sus árbitros y el mockup | v0.14 (solo cambia el árbitro del backlog respecto a la v0.12; los demás textos son los de la v0.12) |
| Las rondas centradas en unas iteraciones y la actualización del catálogo y del backlog | v0.13 |

La plataforma guarda la versión de cada ejecución (`PromptVersion` en `Runs`). Los cambios de cada versión están en `prompts/v0/CHANGELOG.md` y en `docs/evolucion-prompt-requisitos.md`.

### Modelos

| Papel | Proveedor | Modelo |
| --- | --- | --- |
| A | `LiteLlm` (la pasarela) | `SS - GPT-6-SOL` |
| B | `OpenCodeGo` (suscripción Go del autor) | `kimi-k3.1-pro` (Kimi K3.1 Pro, de Moonshot AI) |
| Árbitro | `LiteLlm` | `SS - GPT-6-SOL` |
| Sugerencias de respuesta | `LiteLlm` | `SS - GPT-6-SOL` |
| Mockup | `LiteLlm` | `SS - GPT-6-SOL` |

Sacados de las ejecuciones guardadas, sin leer `.env`: A, B y el árbitro, de la prueba en inglés de este paso; las sugerencias, de las últimas (12-11-2026, «Clínicas B Kimi»); el mockup, del último (09-11-2026). Los valores por defecto de `docker-compose.yml` son los mismos, salvo el mockup (`Fake` y `claude-sonnet-5-5`), que se cambia en `.env`. B va por OpenCode Go porque `RUNNER_PROVIDER_B` está vacío (paso 132).

**Para el paso 121 (SOL ‖ SOL)** se cambia solo la configuración, no el código: `RUNNER_PROVIDER_B=LiteLlm` y `RUNNER_MODEL_B=SS - GPT-6-SOL`. Al terminar, se dejan vacíos para que B vuelva a ser Kimi.

### Configuración

Son los valores por defecto del código. Los marcados con * se pasan por `docker-compose.yml` y se podrían cambiar en `.env` (que no se lee); el valor por defecto del compose es el mismo.

| Valor | | Dónde |
| --- | --- | --- |
| Umbral «lo mismo» del emparejamiento | 0,90 | `Embeddings:SameThreshold` |
| Umbral «parecido» | 0,70 | `Embeddings:SimilarThreshold` |
| Modelo de embeddings | `paraphrase-multilingual-MiniLM-L12-v2`, ONNX fp32, commit de Hugging Face `e8f8c211` | `src/Concilia.Api/Dockerfile` |
| Lotes en paralelo del catálogo v1.4 | 5 | `Catalog:MaxParallelLots` |
| Pantallas del mockup en paralelo | 3 | `Mockup:MaxParallelScreens` |
| Tope de salida por respuesta | 64.000 tokens | `Runner:MaxOutputTokens`* |
| Contexto que asume Claude Code | 128.000 tokens, también para B | `CLAUDE_CODE_MAX_CONTEXT_TOKENS` |
| Turnos por ejecución de una fase | 30 | `Runner:PhaseMaxTurns`* |
| Tope de documentación de una fase | 300.000 tokens estimados | `Runner:MaxDocumentationTokens`* |
| Fragmentos relacionados en los modos centrados | desde 0,70, hasta 20.000 tokens | `RelatedContext.MaxTokens` |
| Espera sin bytes de los procesos de OpenCode Go | 15 minutos (5 en los de la pasarela) | commit `dc30d8b`, paso 130 |

### Claude Code

`2.1.283`, con `claude --version` dentro del contenedor de la API (17-11-2026). Es la versión fijada en el Dockerfile, la misma de la evaluación 1.

### Prueba en inglés antes de congelar

En inglés, Kimi no se había probado nunca. Antes de congelar se lanzó la ronda de preguntas normal con el documento del paso 134, el mismo de la prueba del paso 117, para corregir cualquier fallo ahora y no a mitad de la evaluación. Resultado completo en `prueba-ingles-paso-133/` (`ronda-preguntas.json`, salida del árbitro con su emparejamiento, e `informe.json`).

- **Documento:** `2005 - microcare.pdf` del dataset PURE v2.0 (Zenodo, registro 7118517, CC BY 4.0), el SRS del *Voucher Management System* de Microcare. Descargado de nuevo el 16-11-2026; su SHA-256 coincide con el del paso 105 (`116bea54…71f0`).
- **Proyecto:** «PURE · prueba en inglés (paso 133)» (proyecto 51), idioma `en`, sistema «the Voucher Management System». **No se borra** (decisión del autor, como recomendaba Claude, 17-11-2026): en el paso 117 se borró, pero ahora borrarlo quitaría su consumo de Go de `docs/costes/consumo.md` y la cuenta del límite saldría corta.
- **Estimación antes de lanzarla:** unos 0,6–0,9 $ para una empresa y como mucho 0,30 $ de Kimi; quedaban 2,24 $ de Kimi en las 5 horas, 6,74 $ en la semana y 10,07 $ en el mes.

| | A (SOL) | B (Kimi) | Árbitro (SOL) | Total |
| --- | --- | --- | --- | --- |
| Resultado | hecho | hecho | hecho | **válida a la primera, sin reparaciones** |
| Tokens de entrada | 50.216 | 59.222 | 90.184 | 199.622 |
| De ellos, leídos de la caché | 30.697 | 42.210 | 58.055 | 130.962 |
| Tokens de salida | 9.311 | 11.374 | 16.755 | 37.440 |
| Coste para una empresa | 0,14 $ | 0,16 $ | 0,24 $ | **0,54 $** |
| A precio completo (sin caché) | 0,19 $ | 0,23 $ | 0,35 $ | 0,77 $ |
| Minutos | 1,6 | 3,2 | 2,2 | **5,5** |
| Preguntas | 34 | 29 | 40 | |

B se pagó con la suscripción Go; su cifra es lo que costaría pagando por uso, con los precios de lista de OpenCode, y es lo que gastó del límite de Go.

- **La unión:** 40 preguntas (22 de las dos propuestas, 11 solo de A y 7 solo de B), 1 descartada y ninguna con discrepancia; 4 desviaciones respecto al emparejamiento, que en las preguntas solo se cuentan. El emparejamiento y el árbitro coinciden en el 83 % de los elementos.
- **Las preguntas, las razones y las notas del árbitro salen en inglés,** concretas y con 119 fragmentos distintos citados, todos válidos.
- **Frente a la prueba del paso 117, con LUNA de B** (el mismo documento): B propone 29 preguntas frente a 20, y la unión toma 7 solo de B frente a 2; el acuerdo del emparejamiento con el árbitro sube del 70 % al 83 %. Cuesta casi lo mismo (0,54 $ frente a 0,50 $) y tarda algo más (5,5 minutos frente a 4,0), por Kimi.
- **No hizo falta corregir nada.**
- **Go después de la prueba:** quedan 2,08 $ de Kimi en las 5 horas, 6,58 $ en la semana y 9,91 $ en el mes (hasta el 11-12-2026).

### En palabras sencillas

Antes de medir, se fija una versión de Concilia y no se toca, para que todas las mediciones de la evaluación se hagan con la misma y cualquiera pueda repetirlas. Antes de fijarla se arreglaron tres pruebas automáticas que fallaban de vez en cuando o según desde dónde se lanzaran, para que un fallo al azar no haga dudar de la versión congelada; una de ellas destapó un fallo real del mockup, que también se arregló. Y se probó lo único que el nuevo modelo B nunca había hecho: trabajar con un documento en inglés. Funcionó a la primera, por medio dólar, y aportó más preguntas propias que LUNA con el mismo documento.
