# Bitácora detallada del TFG (Concilia)

Recopilación de todos los resúmenes para la bitácora que Claude Code fue dando durante el desarrollo, del 28-09-2026 al 15-10-2026, en orden cronológico. Sirve como fuente detallada; la visión de conjunto está en `trazabilidad-proyecto.md`.

## Cómo leer este fichero

- **Una sección por resumen**, con la fecha y la hora peninsular (CEST) en que se dio.
- **«Recuperada del chat»:** el resumen tal como se dio en la conversación, sin cambios de contenido. Solo se han bajado de nivel sus títulos para que encajen aquí.
- **«Sesión»:** desde el 15-10-2026, Claude Code escribe el resumen de cada sesión directamente aquí, sin darlo en el chat.
- **«Reconstruida»:** días o tramos en los que hubo trabajo pero no se dio un resumen con ese nombre. Se han reconstruido a partir del resto de la conversación, del historial de git y de `docs/plan-de-construccion.md`, y cada una dice de dónde sale.
- **Nombres:** el proyecto se llamó ReqArbiter hasta el 13-10-2026 y desde entonces Concilia. Los resúmenes anteriores usan el nombre antiguo (por ejemplo, `ReqArbiter.Api`, hoy `Concilia.Api`).
- **Hitos y pasos:** los números son los de `docs/plan-de-construccion.md`.
- **Decisiones y cambios de criterio:** cuando una decisión se sustituye, la anterior no se borra; aparece en su fecha y la nueva dice qué sustituye. El estado vigente está en `CLAUDE.md`.
- **Documentos internos:** aparecen anonimizados («documento técnico interno», «documento B», «documento C»), solo con cifras agregadas.

## Qué falta o está incompleto

- **Antes del 28-09-2026 por la mañana:** el hito 0 (pasos 1 a 5) y los primeros commits del autor se hicieron antes de esta conversación. Su entrada está reconstruida con el plan y git.
- **Trabajo hecho fuera de esta conversación:** la especificación del runner (`442a4cf`, 28-09), las pruebas de mutación del validador y el prompt v1.2 (`2c4d128`, 29-09), la aprobación de la especificación v1.3 (`54310cd`, 02-10) y la planificación de la iteración v0 con los prompts v0 (`d45059f` y `f90664a`, 08-10) los trajo el autor desde su propio proyecto de Claude. Aquí solo consta lo que dicen los commits y lo que se comentó después.
- **Días sin conversación:** 03-10, 04-10, 06-10, 07-10, 10-10, 11-10 y 12-10 (no hubo trabajo con Claude Code; tampoco hay commits).
- **Tramos reconstruidos:** 30-09 por la tarde, 01-10, 02-10 por la tarde, 08-10 por la mañana y 13-10 por la tarde.
- **Excluido:** la explicación completa de la plataforma preparada para la reunión con el tutor del 30-09-2026 (arquitectura, recorrido, guion de 5 minutos y preguntas probables). No es un resumen de bitácora; si hace falta, se puede recuperar aparte.

## 28-09-2026, 09:47 (reconstruida)

**Resumen para la bitácora (28-09-2026, antes de esta conversación): hito 0**

> Reconstruida a partir de `docs/plan-de-construccion.md` y del historial de git. Este trabajo se hizo antes de abrir la conversación con Claude Code, así que no hubo resumen de bitácora.

- **Qué se hizo:**
  - **Paso 1:** repositorio privado con README y `.gitignore` (commits `19a032c`, `ce51efe`, `3423478` y `5cc2475`, del autor).
  - **Paso 2:** herramientas instaladas y comprobadas: .NET 10, Node LTS, Docker y Git.
  - **Paso 3:** credenciales de la pasarela LiteLLM guardadas fuera del repositorio.
  - **Paso 4:** lista de modelos de la pasarela y una llamada de chat a cada uno.
  - **Paso 5:** prueba de humo de `claude -p` contra la pasarela, con y sin herramientas, capturando la salida en stream-json. Esas capturas, saneadas, sirvieron después como fixtures del parser del runner (paso 17).
  - Hito 0 marcado como completado en el plan (`db45c6d`).
  - El autor añadió el esquema JSON y el validador en Python del catálogo EARS/MASTeR (prompt v1, `466145f`), en `prompts/generacion_requisitos/`. Es la referencia que después se portó a C#.
- **Decisiones:** stack (React 19 + TypeScript + Vite + Tailwind, .NET 10, Docker), patrón A ‖ B → árbitro y modelos de la pasarela, tal como recogen el README y CLAUDE.md desde el principio.

---

## 28-09-2026, 10:58 (recuperada del chat)

### Resumen para la bitácora

**Qué se hizo (hito 1, paso 6):**
- Se creó la solución .NET 10 con `ReqArbiter.Api` (plantilla `web`, vacía), `ReqArbiter.Core` (biblioteca vacía) y `ReqArbiter.Tests` (xUnit).
- Se añadió el endpoint `GET /health` en la API, con un test de integración que lo comprueba y pasa.
- Todo quedó en un único commit: `feat: add .NET solution with Api, Core, tests and /health endpoint`.

**Decisiones tomadas:**
- Tests con xUnit y `Microsoft.AspNetCore.Mvc.Testing`.
- Los tests van en `tests/`, fuera de `src/`.
- La solución usa el formato `.slnx`, que es el que crea por defecto .NET 10.
- `/health` es un `MapGet` sencillo que devuelve JSON. Se descarta de momento `AddHealthChecks`; se puede reconsiderar en el paso 9 si se quiere comprobar también la base de datos.
- Se usa la plantilla `web` en lugar de `webapi`, para no arrastrar el ejemplo de WeatherForecast ni el paquete de OpenAPI.

**Cambios de criterio:**
- El README sitúa los tests dentro de `src/`, pero se han puesto en `tests/`. Queda pendiente actualizar ese esquema del README.

**Pendientes:**
- Paso 7: frontend con Vite, React 19, TypeScript y Tailwind. Probablemente necesitará CORS o un proxy de Vite para llamar a `/health`.
- Actualizar la estructura del repositorio en el README (carpeta `tests/`).
- Opcional: añadir un `.gitattributes` para fijar los finales de línea.
- Siguen abiertas las mismas decisiones. La base de datos debe elegirse antes del paso 8.

---

## 28-09-2026, 11:03 (recuperada del chat)

### Resumen para la bitácora (hasta aquí)

**Qué se hizo:**
- Paso 6: se creó la solución .NET, con la API, Core y los tests, y el endpoint `GET /health`. Commit `3dbc4fc`.
- Paso 7: se creó el frontend con Vite, React 19, TypeScript y Tailwind v4, con una página que muestra el estado de la API. Commit `f777306`.

**Decisiones tomadas:**
- xUnit para los tests, que van en `tests/`.
- Solución en formato `.slnx`.
- `/health` devuelve JSON.
- Tailwind v4 mediante su plugin de Vite.
- En desarrollo se usa el proxy de Vite en lugar de CORS.
- Se mantiene `oxlint`, el linter que trae la plantilla actual de Vite.

**Cambios de criterio:**
- Los tests están en `tests/` y no en `src/`, como indicaba el README.

**Pendientes:**
- Decidir la base de datos, que bloquea el paso 8.
- Actualizar la estructura del repositorio en el README.
- Opcional: añadir un `.gitattributes` para fijar los finales de línea (Git avisa de conversiones LF/CRLF).

---

## 28-09-2026, 11:46 (recuperada del chat)

### Resumen para la bitácora

**Qué se hizo (hito 1, pasos 6 a 11):**
- **Solución .NET y `/health`**: commit `3dbc4fc`.
- **Frontend Vite + React 19 + Tailwind v4**: commit `f777306`.
- **Decisión de base de datos**: commit `0766c71`.
- **Docker y compose con `db`, `api` y `web`**: commit `6931ed2`.
- **EF Core + PostgreSQL y migración de `Projects`**: commit `e3ab08e`.
- **Crear y listar proyectos**: commit `2d62d0e`.
- **Corrección de versiones de EF Core**: commit `a7a1ba5`.
- **CI en GitHub Actions**: commit `b0b1dbc`.

**Decisiones tomadas:**
- **Base de datos:** PostgreSQL (imagen `postgres:18-alpine`, con un volumen con nombre para que los datos persistan). Se cierra así esta decisión abierta.
- **Solución y tests:** xUnit, con los tests en `tests/`. Solución `.slnx`.
- **Desarrollo:** proxy de Vite en lugar de CORS; en Docker, nginx hace de proxy.
- **Migraciones:** se aplican al arrancar la API, controladas por configuración.
- **Carpetas:** el `DbContext` va en `Persistence/` (no en `Data/`, que choca con la regla de permisos). La entidad `Project` va en Core.
- **Validación del nombre:** obligatorio y de 200 caracteres como máximo, con errores en formato *problem details*.
- **Configuración del entorno:** `.env.example` incluye también `POSTGRES_PASSWORD`.
- **Solo la web publica un puerto** (8080); la API y la base de datos no son accesibles desde fuera de Docker.

**Cambios de criterio:**
- Base de datos: antes "PostgreSQL o SQL Server (por decidir)", ahora PostgreSQL.
- Tests en `tests/` en lugar de `src/`, como indicaba el README.
- `.env.example` añade `POSTGRES_PASSWORD` a las dos variables del plan.

**Pendientes:**
- **Comprobaciones finales del hito:** probar en el navegador con `docker compose up` y ver el primer resultado de CI tras el push.
- **Documentación:** actualizar el README (hito 1, puesta en marcha, estructura con `tests/`) y el estado en CLAUDE.md.
- **Opcional:** añadir un `.gitattributes` para los finales de línea.
- **Nombres de variables:** confirmar en el hito 3 que las variables `LITELLM_*` se pasan como `ANTHROPIC_*` al proceso `claude -p`.
- **Siguen abiertas:** origen de los embeddings, jerarquía del backlog, modelo árbitro, umbral de discrepancia grave y ventana de contexto.
- **Conector Microsoft 365:** necesita autorización desde la configuración de conectores de claude.ai. No lo he usado en esta sesión.

---

## 28-09-2026, 11:50 (recuperada del chat)

### Resumen para la bitácora (añadido a lo anterior)

**Qué se hizo:**
- Primer arranque real del hito 1 con Docker, con un `.env` local que no se sube al repositorio.
- Se documentó la puesta en marcha en el README. Commit `6fc5733`.

**Decisiones tomadas:**
- En local, `POSTGRES_PASSWORD` se genera aleatoria en el `.env`.
- El README documenta dos formas de trabajar: con Docker (puerto 8080) y sin Docker (API en 5292 y Vite en 5173).

**Cambios de criterio:**
- En la estructura del README, los tests pasan de `src/` a `tests/`.

**Pendientes:**
- Confirmar en el navegador el criterio de "hecho" del hito 1 y marcarlo.
- Comprobar el CI tras el primer push.
- Empezar el hito 2 (paso 12).

---

## 28-09-2026, 12:15 (recuperada del chat)

### Resumen para la bitácora (desde el último resumen)

**Qué se hizo:**
- **Hito 1 cerrado:** probado en el navegador por el autor. Commit `538ab9b`.
- **Paso 12:** subida de archivos con metadatos (nombre, tipo, tamaño y SHA-256). Commit `8c83084`.
- **Paso 13:** vista de proyecto con zona de arrastrar y soltar y lista de archivos. Commit `db2af97`.
- **Paso 14:** texto de `.txt` y `.md`, con detección de UTF-8 o Windows-1252. Commit `9534677`.
- **Paso 15:** límites de 10 MB y tipos permitidos (`.txt`, `.md`, `.pdf` y `.docx`). Commit `4ba7d02`.
- **Paso 15b (nuevo):** texto de `.docx` y `.pdf`. Commit `063b699`.

**Decisiones tomadas:**
- La interfaz no se pule hasta que exista el árbitro, como dice el plan (confirmado por el autor).
- En disco, los archivos se guardan con un GUID como nombre y el nombre original va a la base de datos. En Docker, `data/` se monta como carpeta del contenedor.
- El texto extraído se guarda como `{guid}.extracted.txt`, en UTF-8, junto al original.
- Librerías de extracción: DocumentFormat.OpenXml (MIT) y PdfPig (Apache-2.0). Se descarta iText por su licencia AGPL.
- Un archivo que no se puede leer se rechaza entero; no se guarda solo el original.
- nginx se queda con un techo de 30 MB; el límite real de 10 MB lo aplica la API.
- Sin librería de rutas en la web: el proyecto seleccionado es un estado de la aplicación.

**Cambios de criterio:**
- La extracción de PDF y DOCX se adelanta del "más adelante" del paso 14 al nuevo paso 15b, a propuesta del autor. Motivo: poder probar el catálogo con documentos reales en el hito 5.

**Pendientes:**
- Confirmar en el navegador el criterio de "hecho" del hito 2 y marcarlo como completado.
- Paso 17: el autor debe aportar las salidas stream-json del hito 0, ya saneadas.
- Los archivos subidos antes del paso 14 no tienen texto extraído; habría que volver a subirlos.
- Opcional: añadir un `.gitattributes`, vigilar el primer CI tras el push y, si hace falta, añadir rutas en la web.
- Siguen abiertas: origen de los embeddings, jerarquía del backlog, modelo árbitro, umbral de discrepancia grave y ventana de contexto.

---

## 28-09-2026, 13:06 (recuperada del chat)

### Resumen para la bitácora (desde el último resumen)

**Qué se hizo:**
- **Hito 2 cerrado:** probado por el autor en el navegador. Commit `086125b`.
- **Reglas de seguridad:** copiadas a CLAUDE.md. Commit `0986087`.
- **Pasos 16 a 19 del hito 3:** el contrato del runner, el parser de stream-json, el lanzamiento del proceso y el veredicto con reintentos. Commits `5e63ac1`, `6c92b83`, `7f609f6` y `eabde19`.
- **Tests:** son 111 en total.

**Decisiones tomadas:**
- Proyecto `ReqArbiter.Runner`; el contrato y la lógica pura van en Core.
- **Fixtures:** git los guarda byte a byte (`.gitattributes`, `tests/fixtures/** -text`) y tienen un README con su origen.
- **Fixtures sintéticos:** sin veredicto, veredicto inválido, bloqueada, salida cortada y reintento de la API (este último con forma supuesta). No se crea uno sintético de `is_error` porque ya existe el error real.
- **`FakeClaude`:** es una consola .NET que recibe sus instrucciones por stdin.
- **Carpetas de cada ejecución:** `input/`, `output/`, `claude-config/` y `home/`.
- **Estado de la ejecución:** nunca se deduce de `subtype`, porque el error 403 real llega con `subtype: "success"`.
- **Reintentos:** solo cuando el veredicto falta o no es válido, como máximo 2, y cada uno con un `RunId` nuevo enlazado al anterior.

**Cambios de criterio:**
- `FakeClaude` se adelanta del paso 19 al 18.
- Los eventos se publican con `Action<RunEvent>` en lugar de `IProgress<T>`.
- `RunResult` añade `ErrorMessage` y `VerdictError` a los campos de la especificación.

**Pendientes:**
- **Del autor:** revisar el núcleo, dar la versión de `claude --version`, comprobar si `CLAUDE_CODE_MAX_CONTEXT_TOKENS` tiene efecto y crear el `.env` real antes de su prueba del paso 20.
- **Del autor, abierta:** aclarar con el team leader la ventana real de contexto y de salida de los modelos.
- **Tras el push:** vigilar el CI en Linux (`FakeClaude` sin `DOTNET_ROOT`).
- **Después de la revisión:** pasos 20 a 22.

---

## 29-09-2026, 08:52 (recuperada del chat)

### Resumen para la bitácora (desde el último resumen)

**Qué se hizo:**
- Revisión del núcleo del runner por el autor: aprobado.
- **Ventana de los modelos:** registrada como decisión vigente. Commit `c61b406`.
- **Pasos 20, 21 y 22:** hechos. Commits `e687251`, `f2036fd` y `3c763f9`.
- **Corrección del presupuesto de entrada.** Commit `d52aea3`.
- **Tests:** son 128 en total.

**Decisiones tomadas:**
- **Imagen de la API:** Node 24, copiado de la imagen oficial, y `@anthropic-ai/claude-code@2.1.283`. Se ejecuta con el usuario sin privilegios `app` (`USER $APP_UID`).
- **Proveedor:** se elige con `RUNNER_PROVIDER` (Fake por defecto) y el modelo con `RUNNER_MODEL` (SOL por defecto). Con `LiteLlm` y sin variables, la API no arranca y el error nombra la variable sin mostrar su valor.
- **Presupuesto de entrada:** cuenta el prompt más el tamaño de los archivos de `input/`.
- **Reintentos:** cada uno se guarda como una fila `Run` propia (`RetryOfRunId`, `Attempt`), y sus eventos aparecen en la consola de la ejecución original marcados como `[reintento]`.
- **Eventos:** se guardan solo en memoria, con un máximo de 2000 por ejecución; se pierden al reiniciar la API.
- **Ejecuciones en marcha al arrancar:** las que quedaron en cola o en curso se marcan como fallidas.
- **SSE:** implementado a mano (`text/event-stream`). En nginx lleva `proxy_buffering off`.
- **Tests del paso 21:** solo de las piezas puras, porque el CI no tiene PostgreSQL. El flujo con base de datos se comprobó en Docker.

**Cambios de criterio:**
- La ventana de contexto deja de ser una decisión abierta.
- Corrección al paso 8: las imágenes de .NET 10 no se ejecutan sin privilegios por defecto, como se dijo entonces; ahora se indica el usuario de forma explícita.
- El presupuesto de entrada deja de medir solo el prompt.

**Incidencias:**
- **Error 500 al subir archivos:** las carpetas creadas cuando la API se ejecutaba como root no admitían escritura del usuario `app`. Se resolvió con un `chmod` puntual en la máquina del autor.
- **Consola:** la verificación de seguridad de comandos falló durante un rato, y el autor ejecutó a mano los tests y la construcción de la imagen.

**Pendientes:**
- **Del autor:** probar el flujo con `Fake` en el navegador y hacer la prueba real con SOL y LUNA. Hay que anotar la ventana de contexto que muestre la consola, 128000 o 200000; si sale 200000, `CLAUDE_CODE_MAX_CONTEXT_TOKENS` no tiene efecto.
- **Tras la prueba real:** marcar el hito 3 como completado.
- **Despliegue en Linux:** la carpeta `data/` montada podría no admitir escritura del usuario `app`.
- **Tras el push:** vigilar el CI en Linux (`FakeClaude` sin `DOTNET_ROOT`).
- **Siguen abiertas:** origen de los embeddings, jerarquía del backlog, modelo árbitro y umbral de discrepancia grave.

---

## 29-09-2026, 09:03 (recuperada del chat)

### Resumen para la bitácora (cierre del hito 3)

**Qué se hizo:**
- **Prueba en el navegador con `Fake`:** correcta, tras forzar la recarga (la versión anterior estaba en la caché del navegador).
- **Prueba real del autor:** `SS - GPT-6-SOL` y `SS - GPT-6-LUNA` terminan en `Done`, con veredicto `hecho` y `resumen.md`.
- **Ventana de contexto:** Claude Code asumió 128000 tokens, así que `CLAUDE_CODE_MAX_CONTEXT_TOKENS` tiene efecto.
- **Hito 3 cerrado.** Commit `8bc9387`.

**Decisiones tomadas:**
- La ventana verificada (128k con `CLAUDE_CODE_MAX_CONTEXT_TOKENS`) queda registrada en CLAUDE.md como hecho verificado.

**Cambios de criterio:**
- Ninguno en esta parte.

**Pendientes:**
- **Del autor:** confirmar que la API ha vuelto a `Fake` (`docker compose up -d`).
- **Datos de prueba:** confirmar que el documento usado en la prueba real es ficticio o está autorizado.
- **Salida máxima:** Claude Code la limita a 32k tokens aunque los modelos admiten 1M. Se revisará en el hito 5 si los catálogos se cortan.
- **Hito 4:** tareas con dos modelos en paralelo (pasos 23 y 24). Primero se presentará un plan.
- **Heredados:** vigilar el CI en Linux tras el primer push y los permisos de `data/` en un despliegue en Linux.
- **Siguen abiertas:** origen de los embeddings, jerarquía del backlog, modelo árbitro y umbral de discrepancia grave.

---

## 29-09-2026, 09:05 (recuperada del chat)

### Bitácora del TFG: hitos 1, 2 y 3

#### Qué se hizo

##### Hito 1: esqueleto que arranca en Docker (completado)

| Paso | Contenido | Commit |
|---|---|---|
| 6 | Solución .NET 10 con `ReqArbiter.Api`, `ReqArbiter.Core` y `ReqArbiter.Tests`; endpoint `GET /health` con un test de integración | `3dbc4fc` |
| 7 | Frontend con Vite, React 19, TypeScript y Tailwind v4; página con el estado de `/health` | `f777306` |
| — | Decisión de base de datos: PostgreSQL | `0766c71` |
| 8 | Dockerfiles de la API y la web, `docker-compose.yml` con `db`, `api` y `web`, y `.env.example` | `6931ed2` |
| 9 | EF Core con PostgreSQL; migración `CreateProjects` aplicada al arrancar | `e3ab08e` |
| 10 | Crear y listar proyectos desde la web; primer flujo de punta a punta | `2d62d0e` |
| — | Corrección: versiones de EF Core alineadas entre la API y los tests | `a7a1ba5` |
| 11 | GitHub Actions mínimo: compila y testea la API, y compila y pasa el linter del frontend | `b0b1dbc` |
| — | README con la puesta en marcha y la carpeta `tests/`; hito cerrado | `6fc5733`, `538ab9b` |

**Criterio de "hecho":** el autor comprobó en el navegador que un proyecto creado sigue ahí tras reiniciar los contenedores.

##### Hito 2: subir archivos, sin IA (completado)

| Paso | Contenido | Commit |
|---|---|---|
| 12 | Subida de archivos: se guardan en `data/projects/{id}/` y se registran nombre, tipo, tamaño y SHA-256 | `8c83084` |
| 13 | Vista de proyecto con zona de arrastrar y soltar y lista de archivos | `db2af97` |
| 14 | Extracción de texto de `.txt` y `.md` (UTF-8, o Windows-1252 como alternativa) y visualización en la web | `9534677` |
| 15 | Límite de 10 MB y tipos permitidos (`.txt`, `.md`, `.pdf`, `.docx`), con mensajes claros | `4ba7d02` |
| 15b | **Paso nuevo:** extracción de texto de `.docx` (DocumentFormat.OpenXml) y `.pdf` (PdfPig) | `063b699` |
| — | Hito cerrado | `086125b` |

**Criterio de "hecho":** el autor subió `.txt`, `.md`, `.docx` y `.pdf` y vio su texto en la web.

##### Hito 3: primera llamada a un modelo desde la web (completado)

| Paso | Contenido | Commit |
|---|---|---|
| — | Reglas de seguridad de `docs/hito-3-runner.md` copiadas a CLAUDE.md | `0986087` |
| 16 | Contrato del runner (`IRunnerProvider`, `RunRequest`, `RunResult`) y `FakeProvider` | `5e63ac1` |
| 17 | Proyecto `ReqArbiter.Runner`, parser de stream-json y fixtures reales y sintéticos | `6c92b83` |
| 18 | Lanzamiento del proceso con entorno explícito, latido, tiempo máximo, `SecretRedactor`, presupuesto de entrada y `FakeClaude` | `7f609f6` |
| 19 | Veredicto, evaluación de la ejecución y hasta dos reintentos si el veredicto falta o no es válido | `eabde19` |
| — | Revisión del núcleo del runner por el autor: aprobado | — |
| — | Ventana de contexto registrada como decisión | `c61b406` |
| 20 | `LiteLlmProvider`, selección de proveedor y Claude Code 2.1.283 en la imagen de la API | `e687251` |
| — | Corrección: el presupuesto de entrada cuenta también los archivos de `input/` | `d52aea3` |
| 21 | Entidad `Run`, cola (`Channel`), worker en segundo plano (`BackgroundService`), endpoints y progreso por SSE | `f2036fd` |
| 22 | Web: botón "Ejecutar resumen", lista de ejecuciones, consola en vivo y resultado | `3c763f9` |
| — | Hito cerrado y ventana de contexto verificada | `8bc9387` |

**Criterio de "hecho":**
- **Con `Fake`:** el flujo completo funciona en el navegador.
- **Con `LiteLlm`, en la prueba real del autor:** `SS - GPT-6-SOL` y `SS - GPT-6-LUNA` terminan en `Done`, con veredicto `hecho` y `resumen.md`.

**Tests:** hay 128 al terminar el hito 3.

#### Decisiones tomadas

**Stack y estructura**
- **Base de datos:** PostgreSQL (imagen `postgres:18-alpine`, con un volumen con nombre). Se cierra así esta decisión abierta.
- **Solución .NET:** formato `.slnx` y tests con xUnit en `tests/`, fuera de `src/`. `dotnet-ef` es una herramienta local del repositorio.
- **Proyectos:** `ReqArbiter.Core` (dominio y lógica pura), `ReqArbiter.Api` y `ReqArbiter.Runner`.
- **Frontend:** Tailwind v4 con su plugin de Vite y `oxlint` como linter. No se usa librería de rutas: el proyecto seleccionado es un estado de la aplicación.
- **Comunicación web-API:** en desarrollo, proxy de Vite en lugar de CORS; en Docker, nginx hace de proxy y solo publica el puerto 8080.
- **Interfaz:** no se pule hasta que exista el árbitro, como dice el plan (confirmado por el autor).

**API y datos**
- **`/health`:** un `MapGet` sencillo que devuelve JSON.
- **Migraciones:** se aplican al arrancar solo si lo pide la configuración (`Database__MigrateOnStartup`). Así el test de `/health` funciona sin base de datos.
- **Validación:** los errores se devuelven como *problem details*, con mensajes en español.
- **Enums:** se envían como texto en el JSON.

**Archivos**
- **Nombres en disco:** cada archivo se guarda con un GUID como nombre y el nombre original va a la base de datos. Así se evitan colisiones y rutas maliciosas.
- **Texto extraído:** se guarda como `{guid}.extracted.txt`, en UTF-8, junto al original. Un archivo que no se puede leer se rechaza sin guardar nada.
- **Librerías de extracción:** DocumentFormat.OpenXml (MIT) y PdfPig (Apache-2.0). Se descarta iText por su licencia AGPL. No hay OCR.
- **Límites:** 10 MB por archivo, aplicado en la API y avisado también en la web. nginx mantiene un techo de 30 MB.
- **Carpeta de datos:** en Docker, `./data` se monta en `/data`.

**Runner**
- **Publicación de eventos:** con `Action<RunEvent>` en lugar de `IProgress<T>`, para que lleguen en orden.
- **Fixtures:** git los guarda byte a byte (`.gitattributes`), y un README explica su origen y saneado.
  - **Reales:** `tools-sol`, `tools-luna`, `sin-herramientas` y `error-modelo-inexistente`.
  - **Sintéticos:** sin veredicto, veredicto inválido, bloqueada, salida cortada y reintento de la API (este último con forma supuesta).
- **Estado de una ejecución:** nunca se deduce de `subtype`, porque el error 403 real llega con `subtype: "success"`.
- **Proceso hijo:**
  - **Entorno explícito:** solo hereda `PATH` y las variables del sistema que hacen falta; `HOME` y `CLAUDE_CONFIG_DIR` son propios de cada ejecución.
  - **Argumentos:** se pasan con `ArgumentList`.
  - **Codificación:** stdin, stdout y stderr en UTF-8 sin BOM.
  - **Parada:** si hay que detenerlo, se mata el árbol de procesos completo.
- **Carpetas de cada ejecución:** `data/runs/{runId}/` con `input/`, `output/`, `claude-config/` y `home/`.
- **Secretos:** cada línea de salida pasa por el `SecretRedactor` antes de analizarla. Oculta la clave, la URL y el host.
- **Presupuesto de entrada:** unos 3,5 caracteres por token, contando el prompt y los archivos de `input/`.
- **Reintentos:** solo cuando el veredicto falta o no es válido (campo `VerdictError`), como máximo 2 veces. Cada reintento es un `Run` nuevo enlazado al anterior (`RetryOfRunId`). No se reintentan los errores de la API, los tiempos agotados ni el veredicto `bloqueada`.
- **Uso de tokens:** la entrada incluye los tokens cacheados.
- **`FakeClaude`:** consola .NET que sustituye a `claude` en los tests y recibe sus instrucciones por stdin.
- **Proveedor:** se elige con `RUNNER_PROVIDER`, `Fake` por defecto. El modelo se elige con `RUNNER_MODEL`, SOL por defecto. Con `LiteLlm` y sin variables, la API no arranca.
- **Imagen de la API:** Node 24 y `@anthropic-ai/claude-code@2.1.283`, con el usuario sin privilegios `app`.
- **Ejecuciones:** una a la vez, en segundo plano.
  - **Eventos:** se guardan en memoria, con un máximo de 2000 por ejecución, y llegan a la web por SSE (en nginx, `proxy_buffering off`).
  - **Reinicios:** las ejecuciones que quedaron en cola o en curso se marcan como fallidas al arrancar la API.
- **Tests del paso 21:** solo de las piezas puras, porque el CI no tiene PostgreSQL. El flujo con base de datos se comprueba en Docker.

**Ventana de los modelos:** confirmada por el team leader: 128k de entrada y 1M de salida. Se verificó que Claude Code respeta `CLAUDE_CODE_MAX_CONTEXT_TOKENS=128000`.

#### Cambios de criterio

Las decisiones anteriores se mantienen en el historial; aquí solo se indica qué cambió.

- **Base de datos:** antes "PostgreSQL o SQL Server (por decidir)"; ahora PostgreSQL.
- **Tests:** antes dentro de `src/`, según el README; ahora en `tests/`.
- **`.env.example`:** al plan (`LITELLM_BASE_URL`, `LITELLM_API_KEY`) se añaden `POSTGRES_PASSWORD`, `RUNNER_PROVIDER` y `RUNNER_MODEL`.
- **Extracción de PDF y DOCX:** antes "más adelante", según el paso 14; se adelanta al nuevo paso 15b a propuesta del autor.
- **`FakeClaude`:** se adelanta del paso 19 al 18.
- **`RunResult`:** añade `ErrorMessage` y `VerdictError` a los campos de la especificación.
- **Ventana de contexto:** deja de ser una decisión abierta.
- **Usuario de la API en Docker:** en el paso 8 se dijo por error que las imágenes de .NET 10 se ejecutan sin privilegios por defecto; ahora se indica el usuario de forma explícita (`USER $APP_UID`).
- **Presupuesto de entrada:** antes medía solo el prompt; ahora incluye los archivos de `input/`.

#### Incidencias

- **Permisos:** la regla que protege `./data/**` también bloqueaba una carpeta `Data/` de la API; se renombró a `Persistence/`.
- **Turno cortado:** un turno se cortó y dejó una migración duplicada y vacía, que se eliminó.
- **Versiones de EF Core:** estaban desalineadas entre la API y los tests (aviso MSB3277). Se corrigió fijando la versión.
- **Error 500 al subir archivos:** las carpetas creadas cuando la API se ejecutaba como root no admitían escritura del usuario `app`. Se resolvió con un `chmod` puntual en la máquina del autor.
- **Consola no disponible:** la verificación de seguridad de comandos falló durante un rato, y el autor ejecutó a mano los tests y la construcción de la imagen.
- **Caché del navegador:** la web mostraba la versión anterior hasta forzar la recarga con F5.
- **Codificación de los fixtures:** los grabados en la consola de Windows traen `ÔÇö` en lugar de `—`. Por eso el runner lee la salida siempre en UTF-8 explícito.

#### Pendientes

- **Del autor:**
  - Confirmar que la API ha vuelto a `Fake` (`docker compose up -d` tras comentar las líneas del `.env`).
  - Confirmar que el documento usado en la prueba real es ficticio o está autorizado.
- **Salida máxima:** Claude Code la limita a 32k tokens aunque los modelos admiten 1M. Se revisará en el hito 5 si los catálogos se cortan.
- **CI en Linux:** vigilar el primer resultado tras el push; los tests con `FakeClaude` podrían necesitar `DOTNET_ROOT`.
- **Despliegue en Linux:** la carpeta `data/` montada podría no admitir escritura del usuario `app`.
- **Mejoras opcionales:**
  - Añadir un `.gitattributes` general para los finales de línea.
  - Añadir una librería de rutas en la web si hace falta.
  - Sacar de la memoria los eventos de las ejecuciones antiguas.
  - Volver a extraer el texto de los archivos subidos antes del paso 14.
- **Hito 4:** tareas con SOL y LUNA en paralelo y vista A/B (pasos 23 y 24). Primero se presentará un plan.
- **Decisiones abiertas:** origen de los embeddings, jerarquía del backlog, qué modelo hace de árbitro y umbral de "discrepancia grave".

---

## 29-09-2026, 13:00 (recuperada del chat)

### Resumen para la bitácora (hito 5, pasos 25 y 26)

**Qué se hizo:**
- **Fragmentador, lotes, montaje del prompt y validador** portados a C#. El fragmentador y el validador se comprueban contra los scripts de Python de referencia.
- **Proyecto:** gana el nombre del sistema, el idioma y el glosario.
- **Tarea de catálogo:** sustituye a la de resumen.
- **Tests:** son 261 en total.

**Decisiones tomadas:**
- **Librería de JSON Schema:** LateApexEarlySpeed.Json.Schema (BSD-3). Se descarta JsonSchema.Net, cuyos binarios exigen una cuota a empresas con ingresos.
- **Lotes:** agrupan secciones completas, de uno o varios documentos.
- **Tiempos del catálogo:** latido de 15 minutos y tiempo máximo de 30.
- **Herramientas:** solo `Write` para el catálogo.
- **Prompt, anexo y esquema:** van incrustados en la DLL.
- **Imagen:** se construye desde la raíz del repositorio, con `.dockerignore`.
- **`FakeProvider`** (se implementa en el paso 27): catálogo de ejemplo para ReservaSalas; para otros documentos, un catálogo que descarta todo; y reparación simulada con `Fake:SimulateRepair`.

**Cambios de criterio:**
- La tarea de resumen desaparece.
- `GET /runs/{id}` ya no devuelve un resumen.

**Pendientes:**
- **Del autor:** revisar el núcleo; probar si `CLAUDE_CODE_MAX_OUTPUT_TOKENS` eleva el tope de 32k; elegir 1 o 2 documentos de PURE para la primera medición.
- **Pasos 27 y 28:** reparación, unión de lotes y vista del catálogo.

---

## 29-09-2026, 13:30 (recuperada del chat)

### Resumen para la bitácora (hito 5, primera prueba real de catálogo)

**Qué se hizo:**
- **Primera prueba real de catálogo:** el autor la hizo contra la pasarela, con un proyecto nuevo y 2 documentos Word.
  - La plataforma los dividió en **15 lotes**, que equivalen a 30 ejecuciones: 15 de SOL y 15 de LUNA.
- **Resultado de SOL, lote 1:**
  - Terminó bien, con veredicto `hecho` en 2 turnos.
  - Generó 37 requisitos y 25 preguntas abiertas.
  - Consumió 43.428 tokens de entrada y 23.237 de salida.
  - Solo usó `Write`.
- **Resultado de LUNA, lote 1:**
  - La llamada a `Write` falló con `InputValidationError`: el JSON de los argumentos no se podía leer (`__unparsedToolInput`, unos 11,9 KB).
  - Claude Code le devolvió el error y LUNA tuvo que volver a generar el catálogo entero.
- **Ritmo observado:** a los 4 minutos, SOL llevaba 2 lotes terminados y LUNA ninguno.
- **Explicación al autor:**
  - Para qué sirven los lotes: el tope de 32k tokens de salida de Claude Code obliga a trocear la documentación y unir después los catálogos parciales (paso 28).
  - Por qué va lento: el tiempo lo marca sobre todo la generación de la salida; los lotes de cada modelo van en orden; y LUNA ha tenido que repetir su primer lote.
- **Código:** no se hizo ningún cambio ni ningún commit.

**Observaciones para la memoria:**
- **La estimación de salida se quedó corta.**
  - El lote 1 de SOL generó 23k tokens, por encima de los 20k objetivo, aunque sigue por debajo del tope de 32k.
  - El margen es pequeño: refuerza la prueba pendiente de `CLAUDE_CODE_MAX_OUTPUT_TOKENS`.
- **Coste fijo por lote de unos 43k tokens de entrada:** contexto base de Claude Code más prompt con instrucciones y ejemplos. Con muchos lotes, este coste fijo pesa en el consumo total.
- **Primera diferencia de fiabilidad entre modelos:**
  - LUNA generó una llamada a herramienta con JSON mal formado al escribir un fichero grande.
  - Hay que vigilar si se repite.
  - Si el lote termina sin catálogo, la reparación del paso 27 lo cubrirá.

**Decisión propuesta (pendiente del autor): ejecutar lotes en paralelo dentro de cada modelo**
- **Cómo:** con un límite configurable (`Catalog:MaxParallelLots`, 3 por defecto propuesto). Con límite 1 se mantiene el comportamiento actual.
- **Ventajas:**
  - El tiempo total baja aproximadamente a un tercio.
  - El resultado no cambia, porque los lotes son independientes.
  - Es fácil de ajustar.
  - A y B siguen en igualdad de condiciones.
- **Desventajas:**
  - Más carga simultánea en la pasarela, con un posible límite de tokens por minuto desconocido.
  - Un error de prompt o de configuración gasta más tokens antes de detectarse.
  - Los tiempos por lote son menos limpios para medir.
  - La web tiene que mostrar varios lotes en curso a la vez.
  - Un corte de la pasarela afecta a varios lotes a la vez.
  - Hay algo más de código de concurrencia que explicar.
- **Recomendación:** activarlo con límite 3 para el uso normal y usar límite 1 cuando se quieran medir tiempos por lote.

**Cambios de criterio:**
- Ninguno todavía.
- Si se aprueba el paralelo, cambiaría la decisión del hito 4 de ejecutar los lotes en orden dentro de cada modelo. Aquella decisión se tomó cuando aún no se conocía la concurrencia que admite la pasarela.

**Pendientes:**
- **Del autor:**
  - decidir si se ejecutan los lotes en paralelo y con qué límite;
  - revisar el núcleo del hito 5 (fragmentador, lotes, prompt y validador) y dar el visto bueno al paso 27;
  - probar `CLAUDE_CODE_MAX_OUTPUT_TOKENS`;
  - elegir 1 o 2 documentos de PURE.
- **Pasos 27 y 28:** reparación de catálogos no válidos (cubrirá también lotes sin catálogo, como el caso de LUNA), unión de lotes y vista del catálogo en la web.
- **Métricas de esta prueba:** completarlas cuando termine el trabajo (lotes válidos, reparaciones, tiempo total y tokens por modelo).

---

## 29-09-2026, 13:35 (recuperada del chat)

### Resumen para la bitácora (hito 4: doble modelo, sin árbitro)

**Qué se hizo:**
- **Paso 23 (`ffde01f`): la misma tarea a A y B en paralelo.**
  - Nueva entidad **trabajo** (`Job`), que agrupa dos ejecuciones de la misma entrada: la de la posición A (`SS - GPT-6-SOL`) y la de la posición B (`SS - GPT-6-LUNA`).
  - Qué modelo ocupa cada posición es configuración (`Runner:ModelA` y `Runner:ModelB`, también desde `.env`).
  - Endpoints nuevos: `POST /projects/{id}/jobs`, `GET /projects/{id}/jobs` y `GET /jobs/{id}`.
  - Tests del `JobRunner` con un proveedor falso que usa una barrera. Así se comprueba que A y B arrancan a la vez de verdad, sin depender de tiempos.
- **Paso 24 (`bf70334`): vista A/B en la web.**
  - Dos columnas con la consola en vivo (SSE) y el resultado de cada modelo.
  - La lista de trabajos se recarga mientras haya ejecuciones sin terminar.
- **Prueba real contra la pasarela:** la hizo el autor con SOL y LUNA. Los dos terminaron con veredicto `hecho`.
- **Problema detectado en esa prueba:**
  - SOL usó `Bash` e intentó cargar el skill `claude-api`, aunque solo se habían permitido `Read` y `Write`.
  - La causa es que `--allowedTools` solo decide qué herramientas se aprueban sin preguntar; no quita las demás.
- **Corrección (`c9e156c`):**
  - El runner pasa también `--tools "Read,Write"` (las únicas herramientas que existen en la ejecución) y `--disable-slash-commands` (sin skills).
  - El autor lo verificó: los dos modelos solo usan `Read` y `Write`.
- **Cierre del hito (`a63c463`):** plan marcado y CLAUDE.md actualizado con lo verificado.

**Mediciones (tarea de resumen, mismo documento):**

| Modelo | Antes de la corrección | Después |
|---|---|---|
| SOL | 89.374 tokens de entrada, 5 turnos | 21.406, 3 turnos |
| LUNA | 59.217 tokens de entrada, 3 turnos | 21.409, 3 turnos |

LUNA no usaba herramientas de más y aun así su consumo bajó casi dos tercios. La explicación más probable es que el contexto base de cada turno se reduce al no cargarse el catálogo de herramientas y skills.

**Decisiones tomadas:**
- **Estructura del trabajo:** cada trabajo lanza A y B en paralelo, sin límite de concurrencia. El autor confirma que la pasarela aguanta ejecuciones simultáneas.
- **Reintentos:** cada modelo reintenta por su cuenta; los reintentos de A no afectan a B.
- **Herramientas:** se restringen con `--tools` además de `--allowedTools`, y los skills se desactivan.
- **Por decidir en el hito 5:** los lotes de un mismo modelo van en orden. Tiene sentido revisarlo tras la primera prueba real del catálogo.

**Cambios de criterio:**
- **Herramientas del modelo:** hasta el hito 3 se daba por hecho que `--allowedTools "Read,Write"` bastaba para limitarlas. Tras la prueba real se añaden `--tools` y `--disable-slash-commands`.

**Pendientes al cerrar el hito 4:**
- Volver al proveedor `Fake` en el `.env` después de las pruebas reales (el autor lo hizo).
- El árbitro que compara A y B queda para un hito posterior. Siguen abiertas las decisiones sobre él: qué modelo hace de árbitro, el origen de los embeddings y el umbral de "discrepancia grave".

**Nota:** entre el hito 4 y el 5, el autor añadió el prompt de requisitos v1.2 con un validador más estricto, un ejemplo en inglés y tests de mutaciones del validador (`2c4d128` y `dec0657`). Son la base del hito 5.

---

## 30-09-2026, 08:32 (recuperada del chat)

### Resumen para la bitácora (hito 5, pasos 27 y 28 y ajustes tras la primera prueba real)

**Qué se hizo:**
- **Diagnóstico de LUNA:** en cada respuesta agotaba los 32.000 tokens de salida, de los que unos 28.000 eran razonamiento. Su `Write` llegaba cortado y lo repetía hasta agotar los 30 minutos.
- **Tope de salida a 64.000 tokens** con `CLAUDE_CODE_MAX_OUTPUT_TOKENS` (`67fd451`).
  - El código de Claude Code 2.1.283 confirma que, para modelos que no reconoce, acepta hasta 128.000.
  - El "salida máxima 32000" de `modelUsage` es solo el valor por defecto. La consola muestra ahora los dos valores (`53949f3`).
- **Lotes en paralelo, hasta 5 por modelo** (`976c5d4`).
- **Corte inmediato** de la ejecución cuando una llamada a herramienta llega con JSON incompleto (`431c6a6`).
- **Paso 27, reparación** con los errores del validador (`1891787`). `VerdictRetryRunner` pasa a llamarse `RetryRunner`, con una política de reintento que decide quien lo usa.
- **Paso 28, unión de lotes, endpoint y vista del catálogo** en la web (`6f503da`).
- **Tests:** son 289.

**Decisiones tomadas:**
- **Paralelismo:** hasta 5 lotes a la vez por modelo, configurable.
- **Tope de salida por respuesta:** 64.000 tokens. `MaxOutputTokensPerLot` se queda en 20.000 porque el razonamiento también consume salida.
- **Corte del `Write` incompleto:** en lugar de esperar al tiempo máximo.
- **Qué se reintenta:** la reparación usa el prompt original más el anexo, sin acumular anexos. Los avisos, `bloqueada`, los tiempos agotados y los errores de la API no se reintentan.
- **Unión de lotes:**
  - solo entran los lotes `Done`;
  - los fallidos y los que siguen en curso se listan aparte;
  - no se deduplica;
  - el catálogo unido se guarda en `data/jobs/{id}/` cuando terminan todos los lotes del modelo.
- **`FakeProvider`:** escribe catálogos reales, y con `RUNNER_SIMULATE_REPAIR` simula una reparación.

**Cambios de criterio:**
- Los lotes de cada modelo dejan de ejecutarse en orden, como se decidió en el hito 4, y pasan a ir en paralelo.
- La salida máxima pasa de 32.000 (el valor por defecto de Claude Code) a 64.000.

**Pendientes:**
- **Del autor:** probar con `Fake` y con `LiteLlm` (ReservaSalas), anotar las métricas y cerrar el hito 5.
- **Del autor:** confirmar con el proyecto de los 2 Word que la pasarela deja pasar más de 32.000 tokens de salida.
- **Del autor:** elegir 1 o 2 documentos de PURE.
- **Decisión pendiente:** si se guardan los eventos de cada ejecución en disco.
- **Árbitro:** siguen abiertas qué modelo lo hace, el origen de los embeddings y el umbral de "discrepancia grave".

---

## 30-09-2026, 08:39 (recuperada del chat)

### Añadido a la bitácora: cierre del hito 5

**Qué se hizo:**
- **Prueba real con `LiteLlm`:** subiendo el `DOC-01.txt` de ReservaSalas, A (SOL) y B (LUNA) generan catálogos válidos, visibles en la web.
- **Cierre:** el hito 5 queda completado (`aafb579`).
- **Incidencia de configuración:** la API no arrancaba (HTTP 502) porque el `.env` tenía `RUNNER_PROVIDER=#LiteLlm`. En un `.env`, un `#` pegado al valor forma parte del valor. La API rechazó el proveedor desconocido al arrancar, como estaba previsto.

**Métricas:** las de la tabla anterior. Ninguno de los dos necesitó reparación y ambos tardaron 2 turnos.

**Observaciones:**
- **Los dos catálogos coinciden en estructura y difieren solo en matices de redacción.** Es un primer indicio de que el árbitro tendrá sobre todo discrepancias leves con este tipo de documento.
- **LUNA gasta más salida para el mismo resultado:** 8.115 tokens frente a 5.591, casi un 50 % más, probablemente por razonar más.
- **El nombre del sistema** se copia tal cual de la configuración del proyecto: una errata en ella llega a todos los requisitos.

**Pendientes:**
- **Pasarela:** verificar una respuesta de más de 32.000 tokens (proyecto de los 2 Word).
- **Eventos:** decidir si se guardan en disco.
- **PURE:** elegir 1 o 2 documentos.
- **Árbitro:** decisiones abiertas para el hito siguiente (modelo árbitro, embeddings y umbral de discrepancia grave).

---

## 30-09-2026, 13:53 (reconstruida)

**Resumen para la bitácora (30-09-2026, tarde): primera prueba con un documento grande**

> Reconstruida a partir de la conversación (no hubo un resumen con ese título) y del commit `1d40e33`.

- **Qué se hizo:**
  - Prueba del catálogo v1.2 con un documento técnico interno grande (Markdown, 608 fragmentos, 22 lotes), con 5 lotes por modelo a la vez.
  - Primeras reparaciones reales, todas resueltas en el segundo intento: LUNA, lote 1, cita no literal (C6); SOL, lote 3, recuentos del veredicto; SOL, lote 5, término vago (C5).
  - **Verificación pendiente cerrada:** el lote 2 de SOL terminó `Done` con 41.880 tokens de salida, así que la pasarela deja pasar más de 32.000. Anotado en CLAUDE.md (`1d40e33`).
- **Hallazgos:**
  - Con 6 de 22 lotes, SOL llevaba ya 405 requisitos y 72 preguntas, válidos y con métricas al 100 %: el catálogo apuntaba a unos 1.500 requisitos por modelo.
  - Causas del exceso: detalles de diseño e implementación tratados como requisitos, detalles visuales, parejas de la regla G4, repeticiones entre lotes y una regla G2 muy estricta.
  - La web mostraba «0 de 22» por un desfase de la recarga, no por un error.
- **Avisos:**
  - El documento era interno y real: Claude recomendó no citar su contenido en la memoria y usar solo cifras agregadas (más tarde, el 09-10, el autor autorizó el uso del «documento B» con la misma condición).
  - Si el portátil pierde la conexión con la pasarela, los lotes pendientes fallan y no hay forma de reanudar un trabajo (se propuso un botón de «reintentar lotes fallidos»).
- **Pendientes:** analizar el resultado completo y decidir cambios en el prompt.

---

## 01-10-2026, 09:48 (reconstruida)

**Resumen para la bitácora (01-10-2026): análisis de la v1.2 y especificación de la v1.3**

> Reconstruida a partir de la conversación (no hubo un resumen con ese título) y del commit `e555de2`.

- **Qué se hizo:**
  - **Análisis del documento grande con la v1.2** (cifras agregadas):
    - SOL: 22 de 22 lotes, 1.133 requisitos, 271 preguntas, 13 lotes reparados, 2,36 M de entrada y 0,79 M de salida, 14 minutos.
    - LUNA: 20 de 22 lotes (dos fallaron tras dos reparaciones), 862 requisitos, 154 preguntas, 17 lotes reparados, 2,86 M y 1,28 M, 22 minutos.
    - El 53 % (SOL) y el 54 % (LUNA) de los requisitos salían de secciones que no son especificación: diseño técnico y notas sobre el proyecto.
    - El 46 % de la salida de SOL y el 72 % de la de LUNA se gastó en intentos que acabaron fallando.
    - De las 271 preguntas de SOL, 108 eran del tipo «¿qué pasa si falla?».
  - **Especificación del hito 6, catálogo v1.3** (`docs/hito-6-catalogo-v1.3.md`, `e555de2`), con el plan (pasos 29 a 34) y CLAUDE.md al día.
- **Decisiones del autor:**
  - **Independencia de A y B:** el prompt no menciona al otro modelo ni al árbitro; cada uno construye su catálogo como si fuera el único, y el árbitro se diseña en consecuencia (emparejamiento de varios a varios).
  - Dejar de generar los requisitos que no deberían existir.
  - Menos reparaciones, porque tienen un coste económico.
  - Ids por tipo de requisito: `RA`, `RT`, `RF`, `RI`, `RNF`.
  - La prioridad (P0–P3) se ignora de momento.
  - Deduplicación por modelo tras unir los lotes, antes del árbitro.
  - Menos preguntas abiertas.
  - Mantener EARS y MASTeR por encima de todo.
- **Decisiones de Claude en la especificación (revisables):** un requisito por capacidad con criterios de aceptación; descartes `design_detail` y `project_note`; índice del documento en cada lote; normalizador de la plataforma para lo mecánico; rechazo por requisito con aceptación parcial del lote (hasta un 20 %); una reparación como máximo; salida más corta y prompt a la mitad; como mucho una pregunta «¿qué pasa si falla?» por proceso o pantalla.
- **Cambio de criterio:** se abandona el principio de la v1.2 de que «ambos modelos corten igual».
- **Pendientes:** revisión de la especificación por el autor; quién redacta el prompt v1.3; una carpeta `privado/` para el documento interno.

---

## 02-10-2026, 11:59 (recuperada del chat)

### Resumen para la bitácora (hito 6, pasos 29 y 30)

**Qué se hizo:**
- **Paso 29:** el autor aprobó la especificación del catálogo v1.3 con sus cambios del 2-10-2026. Son estos:
  - cobertura primero;
  - inventario de funcionalidades;
  - criterios etiquetados;
  - errores de la fuente como criterios `[ERROR]`;
  - C19–C22, con C20 como motivo de reparación;
  - regla U7;
  - `RNF` en las ramas D3–D5;
  - retirada de C4.

  CLAUDE.md lo recoge (`54310cd`).
- **Paso 30 (`82e2416`): referencia en Python de la v1.3.**
  - **Prompt y anexo:** los del autor.
  - **Esquema 1.3:** inventario, ids por tipo, criterios y descartes por sección.
  - **Normalizador:** renumeración, vínculos, slots, etiquetas y citas tolerantes, con el fragmento corregido si la cita está literal en otro.
  - **Validador por niveles:** error del lote, requisito o pregunta rechazados, y aviso; con aceptación parcial (reparación si se rechaza más del 20 % o hay C20) y C19–C22.
  - **Fragmentador:** índice del documento.
  - **Ejemplos:** los dos, reescritos en 1.3.
  - **Pruebas:** 60 casos de mutación; 16 de 16 y 64 de 64 en verde.
- **Ejemplos más pequeños:** ReservaSalas pasa de 13 a 9 requisitos y LibraryLoan de 8 a 6, con la misma cobertura de fragmentos.

**Decisiones tomadas:**
- Los ficheros v1.3 llevan versión en el nombre y los v1.2 se mantienen hasta el paso 31.
- C10 cuenta lo tratado por el modelo, también lo rechazado; la cobertura mide solo lo aceptado.
- Los mensajes llevan el id del modelo, para que coincida con lo que ve en la reparación.
- Los elementos que no son requisitos ni preguntas y no cumplen el esquema se ignoran con aviso.
- C22 reconoce también el verbo paraguas detrás de "permitir a <actor>".

**Cambios de criterio:**
- Los errores de la fuente pasan de requisitos `unwanted_behaviour` a criterios `[ERROR]`.
- C3, C4 y C18 desaparecen; C14 y C13 pasan de errores a avisos.
- Un error en un requisito ya no repara el lote entero: se rechaza solo ese requisito.
- Máximo una reparación (antes, dos).

**Pendientes:**
- **Decidir** si se comprueba el límite de 200 caracteres de los criterios, y con qué código.
- **Desbloquear `FakeClaude.exe`** en el portátil (27 tests de C# no se pueden ejecutar).
- **Paso 31:** port a C# del esquema, el normalizador, el validador, el índice y la aceptación parcial; regeneración de las fixtures desde Python 1.3; retirada de los ficheros v1.2.

---

## 02-10-2026, 12:45 (recuperada del chat)

### Resumen para la bitácora (hito 6, paso 31)

**Qué se hizo (`4d9cc24`):**
- Port a C# del catálogo v1.3: normalizador, validador por niveles, C19–C22, render, índice en el prompt, revisión con aceptación parcial y una reparación configurable, unión por tipo y `FakeCatalog` 1.3.
- Paridad exacta con Python en 61 casos, comparando el resultado completo, no solo los códigos.
- Retirada de los ficheros v1.2.
- 339 tests en C#, y 16 y 65 en Python, todos en verde.

**Decisiones tomadas:**
- El catálogo aceptado de cada intento se guarda aparte y es el que se une.
- La unión usa el último intento `Done` de cada lote.
- Un catálogo válido deja el intento `Done`, aunque pida reparación o falle la línea de veredicto.
- `Catalog:MaxRepairs` vale 1.
- El esquema se valida elemento a elemento.
- Una etiqueta de criterio exige corchetes o dos puntos (también en la referencia en Python).

**Cambios de criterio:**
- En la tarea de catálogo ya no se reintenta solo por la línea de veredicto (el hito 3 lo reintentaba).
- Máximo una reparación por lote (antes, dos).
- La unión renumera por tipo (antes, `REQ-` para todos).
- El intento que cuenta en un lote es el último `Done`, no el último.

**Pendientes:**
- **Paso 32:** deduplicación por modelo.
- **Paso 33:** web (selección de secciones; criterios, inventario, rechazados y correcciones; resumen de la tabla coherente con la decisión 2).
- **Paso 34:** medición v1.2 frente a v1.3.
- **Comprobar la imagen de Docker,** con Docker Desktop arrancado.
- **Que el autor revise las cuatro decisiones de este paso.**

---

## 02-10-2026, 13:29 (reconstruida)

**Resumen para la bitácora (02-10-2026, tarde): historial del prompt y resultados de la v1.3**

> Reconstruida a partir de la conversación y de los commits `6768a96` y `9f0c34f` (no hubo un resumen con ese título después del paso 31).

- **Qué se hizo:**
  - **`docs/evolucion-prompt-requisitos.md`** (`6768a96`): una sección por versión del prompt de requisitos con *Qué hace*, *Resultados* y *Qué hay que mejorar*, su tabla resumen y cómo se mide. Pensado como material para la memoria del TFG.
  - **Regla nueva en CLAUDE.md:** cada cambio del prompt (texto, anexo, esquema o validador) o cada medición con modelos actualiza ese documento en el mismo commit; de los documentos internos, solo cifras agregadas y anonimizadas.
  - **Resultados de la v1.3 con el documento grande** (`9f0c34f`), frente a la v1.2:
    - requisitos: SOL de 1.133 a 456 (−60 %) y LUNA de 862 a 284 (−67 %, sobre 18 lotes);
    - requisitos con detalles de implementación: del 33 % al 9 % (SOL) y del 30 % al 2 % (LUNA);
    - lotes reparados: de 13 a 4 (SOL) y de 17 a 8 (LUNA); salida perdida en intentos fallidos: del 46 % al 3 % y del 72 % al 10 %;
    - tokens: SOL 1,06 M / 0,47 M y LUNA 1,78 M / 0,90 M;
    - LUNA perdió 4 lotes: tres por inactividad de la pasarela (15 minutos sin salida) y uno por agotar los turnos;
    - parejas A–B por similitud de palabras: de 407 a 133, esperado al dejar de forzar que corten igual.
- **Precauciones de método acordadas para la memoria:** repetir cada configuración 2 o 3 veces, cambiar una cosa cada vez cuando se quiera atribuir una mejora y usar documentos públicos como evidencia principal.
- **Pendientes:** el catálogo sigue siendo demasiado grande para revisarlo; se plantean cambios para una v1.4.

---

## 05-10-2026, 09:46 (recuperada del chat)

### Resumen para la bitácora (05-10-2026)

**Qué se hizo**
- **Prompt de requisitos v1.4 integrado** (`7f04c01`).
  - La plataforma monta `req-1.4` con el esquema 1.3.
  - Se retiran del repositorio el prompt y el anexo v1.3 (quedan en el historial de git).
  - El texto del prompt y del anexo es el del autor, sin cambios.
  - Nueva sección v1.4 en `docs/evolucion-prompt-requisitos.md`; los resultados quedan pendientes.
- **Normalizador** (`9739332`).
  - Quita la palabra clave del principio de `condition`, `trigger`, `states` y `feature` y lo anota como `FIX`. Es la red de seguridad de la regla R-h; ya no sale «SI si…».
  - Hecho en Python y en C#, con 2 casos de prueba nuevos.
- **Validador** (`4859d56`).
  - Quita el descarte entero de un fragmento citado, con `FIX`.
  - Una sección descartada que tiene fragmentos citados se sustituye por descartes enteros de sus fragmentos no citados.
  - Hecho en Python y en C#; los fixtures de paridad pasan de 61 a 64.
- **Runner** (`e9ec11b`): un lote perdido por inactividad de la pasarela (`HeartbeatLost`) se repite una vez con el mismo prompt.

**Decisiones**
- Desaparece el aviso C17. Lo que antes contaba se mide ahora con los `FIX` de `DISCARDED[i]` y `SECTION[i]`.
- La repetición por inactividad no cuenta como reparación y no cambia la numeración del anexo.

**Cambios de criterio**
- Antes, ningún tiempo agotado se reintentaba (paso 27). Ahora `HeartbeatLost` se repite una vez por tarea; `TimedOut` sigue sin reintentarse.
- El aviso C17 de la v1.3 pasa a ser una corrección automática.

**Incidencias**
- Windows bloquea ("Acceso denegado") el `FakeClaude.exe` recién compilado durante unos minutos, así que fallan los 28 tests que lo lanzan.
- El commit 4 se hizo sin poder ejecutar esos tests, y queda anotado en su mensaje.

**Pendientes**
- Ejecutar los 28 tests del runner cuando se libere el ejecutable.
- Medir la v1.4 con el documento interno, ReservaSalas y uno o dos de PURE, y rellenar sus resultados.
- En la próxima versión del prompt, quitar la mención a C17 del §9.
- Pasos 32 (deduplicación por modelo), 33 (selección de secciones en la web) y 34 (medición).
- Decidir si se comprueban los 200 caracteres de cada criterio y si C20 debe seguir provocando reparación.

---

## 05-10-2026, 10:30 (recuperada del chat)

### Resumen para la bitácora (05-10-2026, segunda parte)

**Qué se hizo**
- Medición de la v1.4 en el documento grande:
  - SOL: 255 requisitos y 118 preguntas, con los 22 lotes;
  - LUNA: 214 requisitos y 86 preguntas, con 20 lotes de 22.
- Análisis de por qué no baja más.
- Propuesta del catálogo v2 y replanificación (hito 7).

**Decisiones**
- Se pasa a un proceso de arriba abajo: inventario global de funcionalidades y después requisitos por funcionalidad, cada una en un solo lote.
- Objetivo por modelo para un documento como el grande: 70–100 requisitos y 15–25 preguntas.
- Las preguntas «¿qué pasa si falla?» las lista la plataforma.
- Los prompts v2 los redacta el autor.

**Cambios de criterio**
- La cobertura se mide por funcionalidad, no por fragmento (sustituye al C10 de la v1.3 cuando se implemente).
- Los lotes dejan de ser secciones del documento y pasan a ser grupos de funcionalidades (sustituye la parte de lotes de D-24).
- Los pasos 32–34 del hito 6 (deduplicación, selección de secciones y medición) pasan al hito 7. La deduplicación deja de hacer falta.

**Hallazgos**
- Los duplicados (hasta 6–7 por operación) y la multiplicación de preguntas vienen del proceso por lotes, no del prompt.
- LUNA pierde lotes en parte por la plataforma: un catálogo escrito con veredicto `bloqueada` no se valida.
- El "sin actividad" de la v1.3 puede deberse a razonamientos largos de LUNA y no a la pasarela; está sin comprobar.
- Windows Defender for Endpoint bloquea `FakeClaude.exe` tras cada compilación.

**Pendientes**
- Prompts v2 del autor.
- Informe de métricas automático.
- Diagnóstico de LUNA.
- Decidir la corrección de `bloqueada`.
- Pedir la exclusión de Defender.
- Confirmar los 28 tests del runner.

---

## 05-10-2026, 10:31 (recuperada del chat)

### Bitácora del TFG: 05-10-2026

#### Qué se hizo

**1. Integración del prompt de requisitos v1.4** (`7f04c01`)
- La plataforma monta `req-1.4` con el esquema 1.3, sin cambiar el texto que redactó el autor.
- Se retiran del repositorio el prompt y el anexo v1.3; quedan en el historial de git.

**2. Tres cambios de la plataforma que acompañan a la v1.4.** Los dos primeros se hicieron a la vez en el validador de referencia en Python y en su port en C#, y la paridad entre ambos pasó de 61 a 64 casos.
- **Normalizador** (`9739332`): quita la palabra clave del principio de `condition`, `trigger`, `states` y `feature`, y lo anota como `FIX`. Ya no sale «SI si…».
- **Validador** (`4859d56`): ya no avisa con C17. Quita el descarte entero de un fragmento que se cita (`FIX`). Si se descartó entera una sección con fragmentos citados, la sustituye por descartes de los fragmentos no citados, con el motivo de la sección.
- **Runner** (`e9ec11b`): un lote que termina en `HeartbeatLost` se repite una vez con el mismo prompt. No cuenta como reparación ni cambia la numeración del anexo.

**3. Medición de la v1.4 con el documento técnico interno** (608 fragmentos, 22 lotes). Algunas cifras se contaron a mano y son aproximadas.

| | SOL v1.3 | SOL v1.4 | LUNA v1.3 | LUNA v1.4 |
|---|---|---|---|---|
| Lotes en el catálogo | 22/22 | 22/22 | 18/22 | 20/22 |
| Requisitos | 456 | 255 | 284 | 214 |
| Preguntas | — | 118 | — | 86 |
| Preguntas «¿qué pasa si falla?» | 37 | ~55 | 77 | ~43 |
| De secciones técnicas | 38 % | ~23 % | 17 % | ~23 % |
| «SI si» | sí | no | sí | no |

- Las citas literales y la fidelidad de cifras siguen al 100 %.
- Las correcciones nuevas funcionan: la de secciones descartadas actuó 4 veces en SOL y 2 en LUNA.

**4. Análisis y replanteamiento.**
- Propuesta del catálogo v2 en `docs/propuesta-catalogo-v2.md`.
- Resultados de la v1.4 registrados en `docs/evolucion-prompt-requisitos.md`.
- Cierre del hito 6 y nuevo hito 7 en el plan.
- Todo ello en el commit `b165209`.

#### Decisiones tomadas

- **Proceso de arriba abajo (catálogo v2).**
  - Fase 1: inventario global de funcionalidades con el documento entero (30–50 funcionalidades).
  - Fase 2: 1–3 requisitos por funcionalidad, con cada funcionalidad escrita en un solo lote y el detalle en los criterios de aceptación.
- **Objetivo por modelo** para un documento como el grande: **70–100 requisitos y 15–25 preguntas**.
- **"Completo"** significa que ninguna funcionalidad se queda sin requisito.
- **Preguntas solo bloqueantes**, como mucho una por funcionalidad. Las de «¿qué pasa si falla?» las lista la plataforma, no el modelo.
- **Esquemas v2:** dos, uno por fase, sin `self_check`.
- Los prompts v2 los redacta el autor en el proyecto de Claude.
- La v1.4 queda como prompt vigente hasta que exista la v2. No se sigue iterando el prompt por lotes.

#### Cambios de criterio (sin borrar los anteriores)

- **Reintentos:** antes ningún tiempo agotado se reintentaba (paso 27). Ahora `HeartbeatLost` se repite una vez por tarea; `TimedOut` sigue sin reintentarse.
- **Aviso C17 de la v1.3:** pasa a ser una corrección automática (`FIX`).
- **Cobertura:** se medirá por funcionalidad y no por fragmento. Sustituirá al C10 de la v1.3 cuando se implemente la v2.
- **Lotes:** dejan de ser secciones del documento y pasan a ser grupos de funcionalidades. Sustituye la parte de lotes de D-24.
- **Pasos 32–34 del hito 6** (deduplicación, selección de secciones y medición): pasan al hito 7. La deduplicación deja de hacer falta, porque cada funcionalidad está en un solo lote.

#### Hallazgos

- **Volumen:** los duplicados (una misma operación hasta 6–7 veces por modelo) y la multiplicación de preguntas vienen del proceso por lotes, no del prompt. Un lote no puede evitar repetir lo que hacen otros lotes que no ve.
- **Reglas de la v1.4:**
  - la de observabilidad tuvo un efecto parcial en SOL y nulo en LUNA;
  - la regla N4 (las preguntas de errores, por funcionalidad) tuvo efectos opuestos en cada modelo;
  - LUNA sí aplica requisito más pregunta de contradicción cuando lo especificado y lo construido difieren.
- **Lotes perdidos de LUNA:**
  - un catálogo escrito con veredicto `bloqueada` no se valida y se pierde entero;
  - el "sin actividad" de la v1.3 puede deberse a razonamientos largos de LUNA y no a la pasarela, porque los lotes empiezan de 5 en 5 y llegan juntos a los 15 minutos. Está sin comprobar.
- **LUNA tarda más:** razona más (hasta 28.000 de 32.000 tokens en el hito 5), reescribe el catálogo (8 de 27 intentos en la v1.3) y es más estricta consigo misma.
- **Entorno:** Microsoft Defender for Endpoint (corporativo) bloquea `FakeClaude.exe` durante un tiempo después de cada compilación. Por eso fallan 28 tests del runner con "Acceso denegado". El commit `e9ec11b` se hizo sin poder ejecutarlos, y queda anotado en su mensaje.

#### Pendientes

- **Autor:** redactar los prompts v2 (inventario y requisitos), los esquemas v2 y los ejemplos de ReservaSalas (paso 35).
- **Informe de métricas automático** de un trabajo (paso 36).
- **Diagnóstico de LUNA** con los datos de las ejecuciones: duración, silencios, tokens, reescrituras y motivo de cada lote perdido.
- **Decidir la corrección de `bloqueada`:** validar el catálogo cuando el modelo se declara bloqueado. Cambia el criterio del paso 31.
- **Pedir a quien gestiona el equipo** una exclusión de Defender para el repositorio, y confirmar los 28 tests del runner.
- **Prompt v1.4:** en la próxima versión, quitar la mención a C17 del §9.
- **Decisiones abiertas que siguen igual:**
  - origen de los embeddings;
  - jerarquía del backlog;
  - qué modelo hace de árbitro;
  - umbral de "discrepancia grave";
  - cómo comparará el árbitro los inventarios de la fase 1.

---

## 05-10-2026, 14:00 (recuperada del chat)

### Resumen para la bitácora (05-10-2026, tercera parte)

**Qué se hizo:** la validación completa del catálogo v2, con la referencia en Python y el port en C# a la vez.
- **Validador del inventario:** SCHEMA, IDS, C10 con umbral 0, C6, exclusividad (`FIX`), REP, KIND, VERDICT y renumeración de `CAP-` y `QI-`.
- **Validador de requisitos v2:** C20 por kind, C21 para todos los tipos, C23, C24, C25, objeciones, referencias `QI-` e informe de funcionalidades sin `[ERROR]`. Desaparecen C10 y SELF_CHECK.
- **Unión v2:** preguntas `QI-` y `Q-` en una sola lista, fusión de las idénticas, orden por tipo y enlaces completados en los dos sentidos.
- **Refactor previo en C#:** `RequirementChecks`, compartido por la v1.3 y la v2.
- **Tests:** 101 mutaciones nuevas con paridad Python/C#. La suite pasa de 349 a 564 tests.

**Decisiones:**
- La v1.3 (la de la plataforma) y la v2 conviven hasta el paso 38.
- REP por nombres: Jaccard de palabras ≥ 0,75, sin artículos ni preposiciones.
- Toda pregunta final lleva `related_capabilities` y `related_requirements`.
- La unión, solo en C#.
- Se generan los ejemplos enriquecidos v2.

**Cambios de criterio:**
- **C10:** pasa de la fase 2 a la fase 1, con umbral 0 en lugar del 10 %.
- **C20:** cuenta por kind y no solo con `RF`/`RI`.
- **C21:** vale para todos los tipos.
- **Autoverificación:** desaparece.
- **Preguntas «¿qué pasa si falla?»:** pasan a ser una lista de la plataforma.

**Pendientes:**
- Paso 38: ejecución en dos fases, `LotPlanner`, `PromptBuilder` y web.
- Informe de métricas automático.
- Calibrar REP.
- Test de tiempos del runner que falla de vez en cuando.
- Exclusión de Defender.
- Decisión sobre `bloqueada` con catálogo válido.

---

## 08-10-2026, 10:29 (reconstruida)

**Resumen para la bitácora (08-10-2026, mañana): planificación de la iteración v0**

> Reconstruida a partir del plan, de CLAUDE.md y de los commits `d45059f` y `f90664a`. La planificación se hizo fuera de esta conversación (el autor la trajo ya hecha).

- **Qué se hizo:**
  - **Pregunta del autor sobre la suscripción:** Claude confirmó en el código que la plataforma no usa ninguna cuenta ni suscripción de Claude. Claude Code es solo el programa cliente; los modelos son los de la empresa a través de la pasarela LiteLLM (`ANTHROPIC_BASE_URL` y `ANTHROPIC_AUTH_TOKEN`), con un entorno construido de cero y un `CLAUDE_CONFIG_DIR` vacío por ejecución.
  - **Planificación de la iteración v0** (`d45059f`): hitos 8 a 13, el flujo completo con prompts básicos (proyecto → archivos → rondas de preguntas → catálogo → validación → backlog → validación → mockup). Tiene prioridad sobre el hito 7, que queda aplazado.
  - **Paso 41, prompts v0** (`f90664a`): prompts, esquemas y ejemplo en `prompts/v0/`, con `validar_v0.py` como referencia del validador.
- **Decisiones del autor** (tabla de decisiones de la v0 en el plan):
  - prompts muy básicos en todas las fases; la calidad se trabaja después;
  - árbitro solo con LLM (los embeddings, más adelante), `SS - GPT-6-SOL`, con las propuestas anónimas en orden aleatorio;
  - sin umbral numérico de discrepancia: el árbitro marca `discrepancia` y el usuario la revisa al validar;
  - backlog: épica → historia → criterios de aceptación en Gherkin → tarea, sin nivel feature;
  - mockup con Claude Code, Sonnet 5.5 y el token de la suscripción del autor, solo para su uso;
  - validación del catálogo y del backlog por el usuario; las fases siguientes solo ven la versión validada;
  - el catálogo exige al menos una ronda terminada y ninguna pregunta pendiente;
  - sin lotes: toda la documentación en una llamada.
- **Cambios de criterio:**
  - Se sustituye «No existe proveedor de suscripción personal ni debe crearse»: se admite solo para el mockup.
  - Se cierra la jerarquía del backlog (sin feature) y el modelo del árbitro.
  - El hito 7 (catálogo v2) queda aplazado.

---

## 08-10-2026, 10:54 (recuperada del chat)

### Resumen para la bitácora (08-10-2026)

**Qué se hizo:** el hito 8 completo, la base común del flujo v0.
- **Paso 42:** tarea de fase genérica.
- **Paso 43:** A ‖ B → árbitro.
- **Paso 44:** estado del flujo.
- **Paso 45:** página por pasos y pestaña «Ejecuciones».

**Decisiones:**
- Paridad Python/C# del validador v0 con mutaciones exportadas (opción a).
- Si el árbitro falla, el trabajo falla.
- El endpoint de rondas de preguntas se adelanta del paso 46.
- Estado `Skipped` para el árbitro que no se llama.

**Errores encontrados y corregidos:** los marcadores con cifras (`{{PROPUESTA_1}}`) no se habrían rellenado nunca en los árbitros.

**Pendientes:**
- Comprobar el hito 8 con Docker y `Fake`.
- Hito 9: tabla `Questions`, responder y descartar, y prueba real.
- El test de tiempos del runner que falla de vez en cuando.

---

## 09-10-2026, 08:49 (recuperada del chat)

### Resumen para la bitácora (09-10-2026)

**Qué se hizo:**
- Pasos 50 a 52 (catálogo v0, entregables y su edición en la web) y 54 a 56 (backlog v0, su entregable y su edición en la web).
- Arreglo del test de tiempos del runner.
- Corrección del proxy de `/questions` (el 405 al guardar respuestas).

**Decisiones:**
- Los ids se renumeran al guardar.
- La marca de desactualizado solo se quita al volver a generar.
- El backlog se comprueba contra el catálogo de su trabajo.
- A la segunda ronda se le pasan también las preguntas pendientes.

**Cambios de criterio:** ninguno respecto a lo planificado.

**Pendientes:**
- Pruebas reales 49, 53 y 57, y sus resultados en la sección v0.
- Hito 12 (mockup).
- Decidir si el prompt de preguntas debe mencionar el estado «pendiente».

---

## 09-10-2026, 09:16 (recuperada del chat)

**Resumen para la bitácora (09-10-2026)**

- **Qué se hizo:**
  - Hitos 9, 10 y 11 completos salvo sus pruebas reales (pasos 46–48, 50–52 y 54–56): preguntas con sus acciones; catálogo y backlog v0 como entregables que se editan, se validan y se reabren en la web.
  - Correcciones:
    - columna `Slot` ampliada (el error 500 al lanzar rondas);
    - proxy de `/questions` (el error 405 al guardar una respuesta);
    - test de tiempos inestable.
  - Sugerencias de respuesta con IA (paso 49b).
- **Decisiones:**
  - **Tuyas:** las sugerencias las da un solo modelo, sin árbitro; no se guarda nada sin confirmación del usuario; queda rastro del origen de cada respuesta (`usuario`, `ia`, `ia-editada`).
  - **Mías, pendientes de tu visto bueno:**
    - SOL como modelo de las sugerencias, configurable;
    - las sugerencias no son un paso del flujo;
    - una petición de sugerencias a la vez por proyecto;
    - las preguntas pendientes también se pasan como «previas» en las rondas siguientes.
- **Cambios de criterio:** ninguno sobre decisiones anteriores. Las sugerencias son una ampliación del flujo v0, recogida en CLAUDE.md y en la tabla de decisiones del plan.
- **Pendientes:**
  - Pruebas reales 49, 53 y 57 con LiteLlm, con sus resultados en la sección v0 de `docs/evolucion-prompt-requisitos.md`.
  - Revisar el prompt de sugerencias.
  - Una prueba real de las sugerencias.
  - Hito 12 (mockup).

---

## 09-10-2026, 09:28 (recuperada del chat)

**Resumen para la bitácora (09-10-2026, continuación)**
- **Qué se hizo:**
  - Primera ejecución completa con modelos reales sobre ReservaSalas: preguntas, sugerencias, catálogo y backlog.
  - Resultados registrados en `docs/evolucion-prompt-requisitos.md`; paso 57 cerrado.
- **Hallazgos:**
  - Aceptar sin revisar las suposiciones de la IA casi duplica el catálogo (de 13 a 25 requisitos) y mete funcionalidad y cifras que el documento no tiene.
  - En el documento pequeño el árbitro no aportó nada medible.
- **Decisiones tomadas:** ninguna nueva.
- **Cambios de criterio:** ninguno.
- **Pendientes:**
  - Las dos decisiones de arriba (prompt de sugerencias y origen `ia` en el catálogo).
  - Segunda ronda de preguntas (paso 49).
  - Editar el catálogo (paso 53).
  - Hito 12 (mockup).

---

## 09-10-2026, 09:54 (recuperada del chat)

**Resumen para la bitácora (09-10-2026, continuación)**
- **Qué se hizo:** los pasos 59, 60 y 61 del hito 12.
- **Decisiones (mías, pendientes de tu visto bueno):**
  - La suscripción solo se admite en el rol del mockup, y la API se niega a arrancar si se configura para otro rol.
  - El mockup no se edita ni se valida.
  - El `.html` se sirve con una cabecera de seguridad (CSP) que lo aísla aunque se abra fuera del iframe.
  - En la reparación, el `.html` anterior solo se devuelve al modelo si ocupa menos de 200.000 caracteres.
  - A Claude Code no se le fija la ventana de contexto de Sonnet, porque ya la conoce.
- **Cambios de criterio:** ninguno nuevo. Se aplica la sustitución del 08-10-2026, que permite el proveedor de suscripción solo para el mockup.
- **Pendientes:**
  - Pasos 58 y 62, que haces tú.
  - Segunda ronda de preguntas (paso 49) y editar el catálogo (paso 53).
  - Hito 13.
  - Las dos decisiones abiertas de la sesión anterior: el prompt de sugerencias y el origen `ia` de las respuestas en el catálogo.

---

## 09-10-2026, 11:15 (recuperada del chat)

**Resumen para la bitácora (09-10-2026, continuación)**
- **Qué se hizo:**
  - Corrección del presupuesto de entrada: los archivos de `input/` se contaban dos veces.
  - Hito 12b, lectura por partes (pasos 68–70), y prompts v0.1.
- **Decisiones (autor, sobre la propuesta del team leader):**
  - lectura por partes en todas las fases;
  - a archivo solo lo grande;
  - un fragmento por línea, con continuación de las líneas largas;
  - comprobación de lectura completa con reparación;
  - compactación permitida, con aviso;
  - sin rechazo de la documentación por el presupuesto de 80.000; aviso a partir de 80.000 y tope de 300.000 configurable;
  - 30 turnos por run;
  - sin modo «todo en el prompt» configurable;
  - los prompts los redacto yo y los revisas tú.
- **Cambio de criterio:** «Sin lotes en v0: toda la documentación en una llamada; si no cabe, error» queda sustituida. Sigue sin haber lotes, pero el modelo lee la documentación por partes desde `input/`. La decisión anterior se conserva marcada como sustituida.
- **Pendientes:**
  - revisar los prompts v0.1;
  - pasos 67 y 71 (medida y comparación);
  - paso 62 (prueba real del mockup);
  - pasos 49 y 53;
  - hito 13;
  - las dos decisiones abiertas sobre las sugerencias de respuesta.

---

## 13-10-2026, 12:35 (recuperada del chat)

**Resumen para la bitácora (13-10-2026)**
- **Qué se hizo:**
  - Prompts v0.2 y v0.3, medidos con el documento B: llamadas exactas a `Read` e ids con el número de su propuesta para el árbitro. Los tres árbitros aciertan ya a la primera.
  - Límites del mockup: 30 minutos sin salida y 45 en total.
  - Prompts v0.4: el mockup se escribe por partes (una base y un archivo por épica) que la plataforma junta. Una reparación conserva las partes ya escritas.
  - La consola ya no muestra las líneas `thinking_tokens`.
  - Corregido el aviso falso `HU-EXTRA`.
  - Flujo completo con el documento B (proyecto 27): 35 preguntas, 178 requisitos, 53 historias y mockup generado.
- **Decisiones del autor:**
  - autorización para usar el documento B (interno) en el desarrollo; en la documentación aparece anonimizado;
  - `offset` y `limit` exactos en el plan de lectura;
  - ids con prefijo para el árbitro (sin tocar el orden de lectura);
  - no elegir entre «todo en el prompt» y lectura por partes según el tamaño, de momento;
  - límites del mockup a 30/45 minutos;
  - mockup por partes;
  - ocultar las líneas `thinking_tokens`;
  - dejar el mockup como está por ahora.
- **Cambios de criterio:** el mockup ya no se escribe en un solo `Write`: es una base más un archivo por épica (v0.4). La decisión anterior se conserva marcada como ampliada.
- **Hallazgos:**
  - Leer por partes cuesta un 50 % más con documentos que caben enteros.
  - Leer las propuestas desde archivos hacía que el árbitro mezclara los ids, hasta añadir los prefijos.
  - Aceptar todas las sugerencias de la IA casi duplica el catálogo (178 requisitos frente a 100).
  - El árbitro es el run más caro en catálogo y backlog (hasta 563.000 tokens de entrada).
  - El mockup cuesta mucho con la suscripción: el 43 % del límite de 5 horas.
- **Pendientes:**
  - mejorar el mockup (vistas más sencillas o un run por épica);
  - comprobar el `.html` sin conexión;
  - ocultar las líneas `status` de la consola;
  - hito 13;
  - las decisiones abiertas sobre las sugerencias de respuesta (prompt más conservador y marcar el origen `ia` en el catálogo);
  - pasos 49 (segunda ronda) y 53 (editar el catálogo).

---

## 13-10-2026, 12:59 (recuperada del chat)

**Resumen para la bitácora (13-10-2026, continuación)**
- **Qué se hizo:**
  - Hito 14, interfaz limpia y organizada (pasos 72–76), un commit por paso más uno de diseño, uno de estilo para la vista experimental y uno con la skill `frontend-design`.
  - Añadidas `lucide-react` y las fuentes de `@fontsource`.
- **Decisiones del autor:**
  - barra lateral, un paso cada vez con barra de pasos, actividad dentro de cada paso más la vista técnica aparte, URLs por proyecto y paso, iconos con `lucide-react` y modo oscuro;
  - plan de diseño (paleta, tipografías y marca de procedencia), aceptado con la idea de ajustarlo después;
  - la skill `frontend-design` se guarda en el repositorio (`.agents/` y `skills-lock.json`).
- **Cambios de criterio:** ninguno sobre decisiones anteriores; la interfaz se reorganiza sin cambiar el flujo ni la API.
- **Pendientes:**
  - revisar la interfaz en el navegador y ajustar el diseño;
  - mejorar el mockup (vistas más sencillas o un run por épica);
  - ocultar las líneas `status` de la consola;
  - hito 13;
  - las decisiones abiertas sobre las sugerencias de respuesta;
  - pasos 49 y 53.

---

## 13-10-2026, 13:53 (reconstruida)

**Resumen para la bitácora (13-10-2026, tarde): ajustes de la interfaz y cambio de nombre**

> Reconstruida a partir de la conversación y de los commits (no hubo un resumen con ese título).

- **Qué se hizo:**
  - **Borrar un proyecto** desde sus ajustes, con confirmación escribiendo su nombre; la API lo comprueba, no borra un proyecto con un trabajo en curso y borra todo lo suyo en la base de datos y en disco (`f07310f`).
  - **Portada** con el logotipo y una frase que explica la herramienta, **buscador de proyectos** junto al «+» y fechas sin hora en la barra lateral (`9ba0c93`).
  - **Lector de documentos** lateral con formato, **barra lateral minimizable** con transición, el logotipo lleva a la portada, **pantalla de preguntas más ligera** (sugerencia de la IA plegada, «para qué», fuentes y nota del árbitro más discretos, estado como icono) y el paso «Catálogo» pasa a llamarse «Requisitos» (`141d8c4`).
  - Iconos `<` y `>` para la barra lateral (`899cdd7`); barra lateral más oscura y sin barra de desplazamiento (`46408e0`).
  - **Icono de la pestaña** con el logotipo de la aplicación (`3061df9`), con el fichero renombrado para saltar la caché del navegador (`329ae0b`).
  - **Cambio de nombre a Concilia** (`9985aa5`): lo visible, los proyectos .NET y sus espacios de nombres (`Concilia.*`), la solución, las imágenes de Docker y las claves del navegador.
- **Decisiones del autor:**
  - nombre Concilia, de *conciliar* dos versiones (entre las propuestas: Contrapunto, Concilia, Cotejo, Cauce);
  - el resto de los ajustes de la interfaz, tal como los pidió.
- **Decisiones de Claude:** se mantienen la base de datos y el usuario de PostgreSQL (`reqarbiter`) y el nombre del proyecto de Docker (`name: reqarbiter`, que da nombre al volumen), para no perder datos.
- **Incidencias:** la extensión de C# de VS Code quitó referencias entre proyectos y creó `.csproj` vacíos al mover las carpetas; se deshizo y se comprobó (compila, pasan los 759 tests y se construyen las imágenes).
- **Pendientes:** renombrar la carpeta del repositorio cuando convenga; el navegador olvida una vez las preferencias guardadas.

---

## 15-10-2026, 10:49 (recuperada del chat)

**Resumen para la bitácora (14 y 15-10-2026)**

### Qué se hizo

- **Tipos de archivo (hito 15):**
  - se admiten también `.xlsx`, `.csv`, `.pptx`, `.html`, `.eml`, `.msg`, `.vtt`, `.srt`, `.json`, `.yaml` y `.xml`, convertidos a texto con títulos, listas y tablas;
  - vista previa según el tipo, con Markdown real.
- **Referencia de estilo del mockup:** un `.zip` con un mockup del cliente, que el mockup puede seguir o ignorar.
- **Flujo y web:**
  - la web ya no salta de paso sola;
  - si uno de los dos modelos falla, el trabajo falla en lugar de dar la salida de uno solo.
- **Catálogo con documentos grandes (prompts v0.7):**
  - propuestas con un elemento por línea;
  - el árbitro consulta la documentación por tramos;
  - el error `REF-FRAG` dice qué fragmento hay en cada línea, porque LUNA citaba números de línea.
- **Runner:**
  - para una ejecución que empieza a compactar cuando ya ha escrito su salida;
  - acepta una salida válida escrita antes de agotar el tiempo;
  - la consola se guarda en disco.
- **Mockup (hito 16, v0.6 a v0.9):**
  - una sola aplicación construida por pasos: diseño, una ejecución por pantalla, comprobaciones de la plataforma y correcciones solo donde hacen falta;
  - se puede reanudar un mockup fallido;
  - es una aplicación que se usa de verdad: inicio de sesión con usuarios de prueba, una aplicación por rol y navegación real comprobada;
  - sin máximo de pantallas y con un tope de 10 MB.
- **Hito 13:**
  - test del flujo completo (paso 63);
  - informe automático por trabajo y CSV del proyecto (paso 64);
  - README y documentación al día (paso 66);
  - dos proyectos ficticios de ejemplo, mediano y grande, con contradicciones puestas a propósito (preparación del paso 65).
- **Memoria:** estructura acordada (sin Metodología; 3.1 especificación, 3.2 implementación; 4 Evaluación).

### Decisiones

- `.zip` como estilo: se sube en Documentos, el modelo saca el estilo él solo y la plataforma incrusta las imágenes.
- SOL 6.0 para A, B y el mockup. La 6.1, como mucho de árbitro: es demasiado lenta en respuestas largas.
- El mockup empieza con un inicio de sesión que muestra los usuarios de prueba para entrar con un clic.
- Sin máximo de pantallas en el mockup (aviso por encima de 25) y página de hasta 10 MB (aviso por encima de 2 MB).

### Cambios de criterio

- La salida de un solo modelo ya no se acepta «sin verificación cruzada»: el trabajo falla.
- La revisión del conjunto del mockup la hace la plataforma, no un modelo, porque con 19 pantallas no cabía en 128k.
- Los tiempos agotados siguen sin reintentarse, pero una salida válida escrita antes de agotarse se acepta.
- El mockup pasa de un índice de pantallas a una aplicación navegable.

### Pendientes

- **Paso 65:** ejecutar los dos proyectos de ejemplo y analizar sus CSV.
- **Decisión tuya:** parar las ejecuciones que compactan varias veces antes de escribir su salida.
- **Fiabilidad:**
  - que un reinicio de la API cierre los trabajos a medias para poder reanudarlos;
  - quitar automáticamente la marca `sigue` de los fragmentos citados.
- **Mockup:** abaratar las pantallas (cada una lee el armazón entero) y un posible historial de mockups.
- **Abiertas:** conservación de datos de la memoria (`memoria/` sin versionar, con datos personales en la propuesta) y comprobar la tipología A/B de la propuesta.
- **Más adelante:** hito 7 (catálogo v2), árbitro con embeddings y redacción de la memoria.

---

## 15-10-2026, 12:08 (sesión)

**Resumen para la bitácora (15-10-2026, mediodía)**

- **Qué se hizo:**
  - Recuperación de todos los resúmenes de bitácora de la conversación con Claude Code (28-09 a 15-10) en `docs/bitacora-detallada.md`. Los tramos sin resumen se reconstruyeron a partir del resto de la conversación, de git y del plan.
  - Visión de alto nivel del proyecto en `docs/trazabilidad-proyecto.md`: línea de tiempo, etapas, evolución de los prompts, decisiones abiertas y siguientes pasos.
  - El autor movió los dos ficheros de `memoria/` a `docs/`, y quedaron en git (`cebff42`).
- **Decisiones del autor:**
  - Claude Code mantiene al día los dos ficheros al terminar cada sesión, sin que se le pida.
  - El resumen de bitácora ya no se da en el chat: se escribe directamente en `docs/bitacora-detallada.md`.
- **Cambio de criterio:** sustituye la regla del inicio del proyecto de imprimir el resumen para la bitácora en el chat para que el autor lo llevara a su proyecto de Claude. Ahora los dos ficheros de `docs/` son la fuente, y el autor se los pasa a su Claude cuando quiere.
- **Pendientes:**
  - **Paso 65:** ejecutar los dos proyectos de ejemplo, analizar sus CSV y cerrar el hito 13.
  - Las decisiones abiertas de la sesión anterior, sin cambios.

---

## 15-10-2026, 12:20 (sesión)

**Resumen para la bitácora (15-10-2026, doble barra de desplazamiento)**

- **Qué se hizo:** corregida la segunda barra de desplazamiento vertical de la web, que dejaba bajar muy por debajo del contenido.
  - **Causa:** las etiquetas ocultas para lectores de pantalla (`sr-only`, como el estado de cada pregunta) tienen posición absoluta. Como ningún contenedor estaba posicionado, colgaban de la página y no de la zona de contenido. Por eso escapaban del `overflow-hidden` del contenedor y estiraban la página hasta donde quedaba la última de una lista larga.
  - **Arreglo:** la zona de contenido (`<main>`) pasa a ser `relative`.
  - **Comprobación:** reproducción mínima en Chrome sin interfaz. Sin el cambio, la página medía 3.001 px con una ventana de 705; con el cambio, lo mismo que la ventana.
- **Decisiones:** ninguna nueva.
- **Cambios de criterio:** ninguno.
- **Pendientes:** el autor confirma el arreglo en el navegador; el paso 65 sigue pendiente.

---

## 15-10-2026, 12:40 (sesión)

**Resumen para la bitácora (15-10-2026, aprobar todas las sugerencias)**

- **Qué se hizo:** botón «Aprobar todas las sugerencias» en cada ronda de preguntas.
  - **Cuándo aparece:** solo si todas las preguntas pendientes de la ronda tienen una sugerencia de la IA y no se están generando sugerencias. Sin sugerencias, o con alguna pendiente sin ella, no aparece.
  - **Qué hace:** guarda la sugerencia de cada pregunta pendiente como su respuesta, tal cual y con origen `ia`, en una sola operación. Las respondidas y las descartadas no cambian. Como con cualquier respuesta, el catálogo, el backlog y el mockup quedan desactualizados si existían, y cualquier pregunta se puede reabrir o editar después.
  - **API:** `POST /question-rounds/{id}/accept-suggestions`. La regla está en `QuestionRules.AcceptSuggestions`, que rechaza la petición si alguna pendiente no tiene sugerencia y la API la rechaza también mientras se generan sugerencias. Hay 3 tests nuevos (827 en total).
- **Decisiones del autor:** aprobar todas de golpe, solo cuando están todas generadas.
- **Decisiones de Claude (revisables):**
  - sin ventana de confirmación;
  - si falta la sugerencia de alguna pendiente, no se aprueba ninguna, para que «todas» signifique todas;
  - «Sugerir respuestas» sigue disponible, en segundo plano, para pedirlas de nuevo.
- **Cambios de criterio:** ninguno. Amplía las sugerencias de respuesta del 09-10. Conviene recordar el hallazgo del 09-10: aceptar las sugerencias sin revisarlas casi duplica el catálogo.
- **Pendientes:**
  - reconstruir la API y la web cuando termine el catálogo en curso del proyecto 31;
  - el paso 65.

---

## 15-10-2026, 13:20 (sesión)

**Resumen para la bitácora (15-10-2026, catálogo del proyecto grande de ejemplo y tres mejoras)**

- **Qué se hizo:**
  - **Análisis del catálogo del proyecto grande de ejemplo** (clínicas veterinarias, ficticio):
    - A propone 299 requisitos y B 192; la unión tiene 209 (180 de ambas, 28 solo de A, 1 solo de B, 3 descartados), en 12 minutos;
    - dos reparaciones por citas mal copiadas: SOL con la marca `sigue` y LUNA con un número de línea;
    - el intento 2 de A y el árbitro se detuvieron al empezar a compactar después de escribir su salida, y la plataforma la aceptó. Es el mecanismo del 14-10 funcionando, aunque el mensaje de la consola («Terminada: Failed») asustaba.
  - **Tres mejoras pedidas por el autor:**
    1. **Informe:** cuenta las ejecuciones sin dato de consumo y avisa de que los tokens son un mínimo (y columna nueva en el CSV).
    2. **Marca `sigue`:** la plataforma la quita de los fragmentos citados antes de validar y de guardar (`FragmentCitations`).
    3. **Consola:** una ejecución detenida después de escribir su salida dice «Detenida…» y que la plataforma revisa la salida, sin «Failed».
  - Resultados y cambios anotados en `docs/evolucion-prompt-requisitos.md` (v0.7); 2 tests nuevos (829 en total).
- **Hallazgo:** no se puede sumar el consumo mensaje a mensaje. A través de la pasarela, cada mensaje del modelo trae el consumo a 0 (comprobado en las capturas reales `tools-sol.jsonl` y `tools-luna.jsonl`); el dato real solo llega en el evento final, que no existe si la plataforma corta la ejecución. Por eso la primera mejora quedó en avisar del mínimo, en lugar de dar la cifra completa como se propuso.
- **Decisiones del autor:** hacer las tres mejoras.
- **Cambios de criterio:**
  - Se cierra la decisión abierta de quitar la marca `sigue`: la quita la plataforma.
  - El coste de un trabajo con ejecuciones detenidas pasa a presentarse como un mínimo.
- **Pendientes:**
  - Decidir si la plataforma corrige las citas que son números de línea (LUNA) cuando la línea es inequívoca.
  - Decidir si se estima el consumo de las ejecuciones detenidas.
  - Reconstruir la API y la web cuando no haya trabajos en curso.
  - Paso 65.

---

## 15-10-2026, 14:10 (sesión)

**Resumen para la bitácora (15-10-2026, una tarea cada vez en toda la plataforma)**

- **Qué se hizo:**
  - **Pregunta del autor:** ¿se pueden ejecutar dos proyectos a la vez? No: `JobWorker` ejecuta un trabajo detrás de otro, en todos los proyectos. Un segundo trabajo se quedaba en cola sin decir nada.
  - **Bloqueo de lanzamientos** mientras hay una tarea en curso, en su proyecto o en cualquier otro:
    - **API:** `ActiveJobs` busca el trabajo con alguna ejecución en cola o en marcha. `GET /jobs/active` lo devuelve con su proyecto y un mensaje, y todos los puntos que crean trabajos lo rechazan con ese mensaje (rondas, catálogo, backlog, sugerencias, mockup y su reanudación, catálogo v1.4).
    - **Web:** `ActiveJobProvider` consulta la API cada 3 s. `LaunchButton` sustituye a los botones que lanzan tareas: mientras hay una en curso se ve apagado (opacidad y saturación más bajas, cursor de prohibido) y, al pulsarlo, sale abajo el aviso «Se está ejecutando X en el proyecto «Y». Espera a que termine para lanzar otra tarea.» durante 5 s.
  - Test nuevo de la API: con una ronda en marcha se rechazan los lanzamientos en el mismo proyecto y en otro, y al terminar se puede lanzar de nuevo (830 tests).
  - No se bloquean «Aprobar todas las sugerencias», contestar, descartar ni validar, porque no lanzan ninguna tarea.
- **Decisiones del autor:**
  - una sola tarea a la vez en toda la plataforma, sin ejecutar trabajos en paralelo;
  - bloquear los botones afectados con un tono apagado y un aviso de qué tarea se ejecuta y en qué proyecto.
- **Decisiones de Claude (revisables):**
  - el botón no se desactiva de verdad, para que al pulsarlo pueda mostrar el aviso;
  - el aviso se muestra abajo, en el centro de la página;
  - la API comprueba la tarea en curso antes que el estado del paso, para que el mensaje sea siempre el mismo.
- **Cambios de criterio:** ninguno sobre decisiones anteriores. Se descarta la propuesta de ejecutar varios trabajos a la vez.
- **Pendientes:** reconstruir la API y la web cuando termine el mockup en curso, y comprobarlo en el navegador; el paso 65.

---

## 15-10-2026, 14:30 (sesión)

**Resumen para la bitácora (15-10-2026, skills en el mockup: idea aparcada)**

- **Qué se hizo:** análisis de usar skills de Claude Code para mejorar el mockup.
  - **Dos formas:**
    - skills de verdad: quitar `--disable-slash-commands` y permitir la herramienta `Skill`, solo en el mockup;
    - meter las pautas de `frontend-design` en el prompt de diseño (v0.10).
  - **Coste estimado sobre el mockup v0.9** (3,47 M de entrada): menos de un 1 % más si las pautas van solo en el diseño, y un 9 % más si van también en cada pantalla.
  - **Propuesta del autor:** rehacer el mockup del proyecto grande con su suscripción (Sonnet 5.5) y la skill.
  - **Riesgos:**
    - agotar el límite de 5 horas: el v0.4 gastó el 43 % con 1,49 M de entrada (se puede reanudar);
    - cambiar dos cosas a la vez, el modelo y las skills, que impide atribuir la mejora a las skills.
- **Decisiones del autor:** aparcarlo hasta terminar el paso 65.
- **Cambios de criterio:** ninguno.
- **Pendientes:** paso 65; retomar después la prueba del mockup con skills (plan apuntado en la trazabilidad).

---

## 15-10-2026, 15:00 (sesión)

**Resumen para la bitácora (15-10-2026, mockup del proyecto grande de ejemplo)**

- **Qué se hizo:**
  - **Mockup del proyecto grande de ejemplo con SOL (v0.9):** el mapa tiene 38 pantallas, por encima del aviso de 25.
    - 37 terminan en 4–8 minutos cada una, con hasta 36.000 tokens de salida.
    - La P34 falla: la pasarela responde 504 (Gateway Timeout) en los 10 reintentos de Claude Code, unos 18 minutos sin avanzar. Los errores de la API no se reparan, así que el trabajo termina sin mockup y con la opción de reanudar.
    - El informe da a esa ejecución 3.420 tokens de entrada y 263 de salida, que no son reales: en un error de la API, el evento final solo trae el consumo de la última petición.
  - **API y web reconstruidas** con los cambios del día (aprobar todas las sugerencias, marca `sigue`, aviso de consumo mínimo, texto de la consola y una tarea cada vez), después de dejar terminar una ronda de preguntas en curso. Comprobado que responden `/health` y `/jobs/active`.
- **Hallazgos:**
  - Si la API se reinicia con un trabajo de mockup en marcha, ese trabajo no se puede reanudar, porque queda sin fecha de fin. La web además deja de ofrecer «Reanudar». Refuerza la decisión pendiente de cerrar los trabajos interrumpidos al reiniciar.
  - Que solo falle una pantalla de 38 apunta a que su respuesta tarda más de lo que la pasarela espera. Lo confirmará la reanudación.
- **Decisiones del autor:** dejar terminar la ronda en curso antes de reconstruir y reanudar el mockup al día siguiente.
- **Cambios de criterio:** ninguno.
- **Pendientes:**
  - Reanudar el mockup (16-10). Si la P34 vuelve a dar 504, partirla en el mapa o pedir más tiempo de espera a quien gestiona la pasarela.
  - Cerrar los trabajos interrumpidos al reiniciar la API (decisión pendiente).
  - Paso 65.

---

## 16-10-2026, 08:40 (sesión)

**Resumen para la bitácora (16-10-2026, mockup del proyecto grande reanudado)**

- **Qué se hizo:**
  - El autor reanudó el mockup del proyecto grande de ejemplo. Solo se repitió la P34, que terminó a la primera en 2,5 minutos (68.530 tokens de entrada y 18.906 de salida). La plataforma juntó las 38 pantallas sin pedir ninguna corrección.
  - **Resultado:** 38 pantallas, las 109 historias de 15 épicas y una página de 1,54 MB. Entre los dos trabajos, 3,28 M tokens de entrada y 889.000 de salida, en 71 + 2,5 minutos. El autor lo da por bueno: «muy completo».
  - Anotado en `docs/evolucion-prompt-requisitos.md` (v0.9).
- **Hallazgos:**
  - El 504 de ayer fue un fallo puntual de la pasarela, no el tamaño de la pantalla.
  - Reanudar ahorró repetir más de una hora de trabajo.
  - 4 de las 38 pantallas necesitaron una reparación por escribir un documento HTML completo (código `FRAGMENTO`); la plataforma podría quitar esas etiquetas ella misma.
- **Decisiones:** ninguna nueva.
- **Cambios de criterio:** ninguno.
- **Pendientes:**
  - Decidir si la plataforma quita las etiquetas de documento de las pantallas en lugar de pedir una reparación.
  - Cerrar los trabajos interrumpidos al reiniciar la API.
  - Paso 65: los dos CSV.

---

## 16-10-2026, 09:10 (sesión)

**Resumen para la bitácora (16-10-2026, paso 65: análisis del proyecto grande de ejemplo)**

- **Qué se hizo:** análisis del CSV del proyecto grande (guardado en `docs/resultados-paso-65/informe-proyecto-grande.csv`) y de sus preguntas y su catálogo frente a las 26 contradicciones y huecos puestos a propósito (G1–G26 de `ejemplos/README.md`).
- **Cifras del flujo completo:**
  - **Total:** al menos 5,18 M tokens de entrada y 1,12 M de salida (3 ejecuciones sin dato de consumo) y unos 105 minutos de ejecución.
  - **Por fases:**
    - preguntas: 1,09 M de entrada, 8 min, 2 reparaciones;
    - sugerencias: 0,10 M, 2,5 min;
    - catálogo: 0,50 M, 12 min, 2 reparaciones;
    - backlog: 0,21 M, 9 min, ninguna reparación;
    - mockup: 3,28 M (el 63 % de la entrada y el 79 % de la salida), 73 min.
- **Contradicciones (G1–G26):** el comportamiento es el esperado en las 26.
  - **Resueltas en un documento posterior (14):** ninguna se pregunta de nuevo, y el catálogo recoge la decisión final en todas. Por ejemplo, cancelar con 12 h por la app, el Plan Adulto del correo, el recordatorio configurable de 7 días y el día anterior, y las urgencias de Huellas Norte desde marzo de 2027.
  - **Abiertas (5: G5, G16, G17, G18 y G21):** las 5 salen como preguntas; 4 las detectan los dos modelos y 1 (G17, sin conexión) solo A.
  - **Huecos de un solo documento (6):** los 6 están en el catálogo, y 3 se preguntan además para concretar detalles.
  - **Fuera de alcance (G26, puntos):** no aparece en el catálogo, como debe ser.
- **Hallazgos para la evaluación:**
  - **A (SOL) propone mucho más que B (LUNA):** 50 frente a 17 preguntas, 299 frente a 192 requisitos y 109 frente a 54 historias. La unión del árbitro se parece mucho a A: lo que aporta solo B son 6 preguntas, 1 requisito y ninguna historia.
    - Puede ser que A sea más completo, o que el árbitro (también SOL) prefiera la propuesta de su mismo modelo aunque no sepa cuál es.
    - Para distinguirlo habría que repetir con otro reparto de modelos, por ejemplo el árbitro con LUNA.
  - **Las 55 respuestas son sugerencias de la IA aceptadas tal cual,** todas marcadas como suposición. Las 5 contradicciones abiertas las resolvió la IA y no el cliente, y esas suposiciones pasaron al catálogo.
  - **El catálogo y el backlog se validaron sin cambios** (209 y 109 conservados), así que la métrica de cambios del usuario no dice nada en este proyecto.
- **Límites del método:** los documentos de ejemplo, la lista G1–G26 y la comprobación los ha hecho Claude. El emparejamiento se hizo por palabras clave y leyendo, así que conviene que el autor lo revise.
- **Decisiones:** ninguna.
- **Cambios de criterio:** ninguno.
- **Pendientes:**
  - CSV del proyecto mediano y documento de resultados del paso 65, con los dos proyectos.
  - Valorar una ejecución con otro reparto de modelos para el posible sesgo del árbitro.

---

## 16-10-2026, 09:25 (sesión)

**Resumen para la bitácora (16-10-2026, pregunta clave: ¿el árbitro prefiere a su propio modelo?)**

- **Qué se hizo:** a petición del autor, la posible preferencia del árbitro por su propio modelo queda como **pregunta clave de la evaluación, con prioridad alta**. Tiene sección propia en `docs/trazabilidad-proyecto.md` y es el segundo de los siguientes pasos.
- **Lo observado (proyecto grande de ejemplo):** A (SOL) propone 50 preguntas, 299 requisitos y 109 historias; B (LUNA), 17, 192 y 54. Lo que aporta solo B a la unión son 6 preguntas, 1 requisito y ninguna historia.
- **Dos explicaciones posibles:**
  - A es más completo;
  - el árbitro, que también es SOL, prefiere la propuesta de su mismo modelo aunque le llegue anónima y en orden aleatorio.
- **Por qué importa:** afecta al valor de la verificación cruzada, la idea central del TFG.
- **Cómo comprobarlo:** repetir una fase (preguntas o backlog, las más baratas) con el mismo documento y otro reparto de modelos, por ejemplo LUNA de árbitro. Repetir cada configuración 2 o 3 veces y comparar qué parte de la unión viene de A y de B.
- **Decisiones del autor:** darle importancia en los documentos de seguimiento.
- **Cambios de criterio:** ninguno. Queda como decisión abierta cómo corregirlo si se confirma: un árbitro distinto de A y B, o rotar los papeles.
- **Pendientes:** CSV del proyecto mediano; diseñar y lanzar la prueba del reparto de modelos.

---

## 16-10-2026, 10:15 (sesión)

**Resumen para la bitácora (16-10-2026, volver a arbitrar)**

- **Qué se hizo:** la funcionalidad «Volver a arbitrar», para la pregunta clave de si el árbitro prefiere a su propio modelo.
  - **API:**
    - `POST /jobs/{id}/rearbitrate` con el modelo del árbitro, que se elige entre los configurados (`GET /jobs/arbiter-models`);
    - crea un trabajo `rearbitration` que copia las propuestas de A y B del trabajo original, sortea otra vez su orden y solo ejecuta el árbitro;
    - el resultado queda en la carpeta del trabajo, con su salida descargable y su informe.
  - **No toca el proyecto:** las preguntas, el catálogo y el backlog no cambian.
  - **Informe y CSV:** columnas nuevas `fase`, `modelo_arbitro` y `arbitraje_de`, para comparar arbitrajes de las mismas propuestas.
  - **Web:** en «Ejecuciones», al abrir una ronda, un catálogo o un backlog terminados aparece «Volver a arbitrar», con un desplegable del árbitro y el botón «Arbitrar de nuevo», que respeta la regla de una tarea cada vez. Además, la salida de cualquier trabajo se puede descargar en JSON.
  - **Tests:** uno nuevo de la API: ronda → nuevo arbitraje con LUNA → resultado e informe, con las mismas propuestas y sin cambiar las preguntas (831 tests).
- **Decisiones del autor:**
  - repetir solo el árbitro sobre las mismas propuestas, sin cambiar los resultados del proyecto;
  - poder descargar el resultado;
  - 3 arbitrajes con SOL y 3 con LUNA sobre el proyecto grande, y una tabla comparativa para la memoria.
- **Decisiones de Claude (revisables):**
  - el orden de las propuestas se sortea de nuevo en cada arbitraje, como en cualquier trabajo;
  - solo se pueden elegir los modelos ya configurados;
  - no se puede volver a arbitrar un nuevo arbitraje, solo el trabajo original.
- **Cambios de criterio:** ninguno.
- **Pendientes:**
  - desplegar cuando no haya tareas en curso;
  - lanzar los arbitrajes y escribir la tabla comparativa;
  - CSV del proyecto mediano.

---

## 16-10-2026, 11:30 (sesión)

**Resumen para la bitácora (16-10-2026, búsqueda de repositorios útiles)**

- **Qué se hizo:**
  - **Análisis de `chubozeko/EARS-Rule-Detection`:** trabajo de asignatura, solo en inglés, con 3 de las 6 plantillas EARS, errores de base y sin evaluación. No merece la pena integrarlo.
  - **Búsqueda de repositorios y herramientas** de generación y validación de requisitos, a petición del autor. Se aceptan herramientas solo en inglés, porque el catálogo se puede generar en inglés para comprobarlo.
- **Candidatos más útiles:**
  - **Modelo de embeddings multilingüe** (`paraphrase-multilingual-MiniLM-L12-v2`, ONNX, Apache 2.0) ejecutado en local desde .NET: resolvería que la pasarela no ofrece embeddings y daría una referencia objetiva para la prueba del sesgo del árbitro.
  - **AQUSA (`RELabUU/aqusa-core`, MIT):** calidad de historias de usuario según los criterios QUS, sobre el backlog en inglés.
  - **Analizador oficial de Gherkin (`cucumber/gherkin`, MIT, con implementación .NET):** exportar los criterios del backlog a ficheros `.feature` y validarlos.
  - **`vale-ears` (MIT):** comprobación barata de EARS sobre el catálogo en inglés.
  - **Dataset PURE (79 documentos públicos, CC BY 4.0):** candidato para el segundo documento público del paso 65.
- **Solo como estado del arte:** Paska, Smella, QuARS, NoRBERT, MARE, Elicitron, ReqInOne y la lista `nlp4re-tools` (126 herramientas). No se encontró código mantenido de varios de ellos.
- **Decisiones:**
  - **Del autor:** no añadir de momento la comprobación propia de la plantilla EARS, porque no lo ve claro.
  - Ninguna integración decidida todavía.
- **Cambios de criterio:** ninguno.
- **Pendientes:** que el autor elija qué candidatos integrar; los arbitrajes del proyecto grande; el CSV del mediano.

---

## 16-10-2026, 12:15 (sesión)

**Resumen para la bitácora (16-10-2026, diseño del experimento del sesgo del árbitro)**

- **Qué se hizo:** a petición del autor, el experimento queda documentado para la memoria en `docs/experimento-sesgo-arbitro.md`, con:
  - el origen, la pregunta y las dos hipótesis;
  - el diseño, con la justificación de cada decisión;
  - cómo se mide e interpreta, el estado y las limitaciones;
  - frases de método listas para la memoria.
- **Diseño acordado:**
  - **Se cambia solo el árbitro,** sobre las mismas propuestas de A y B, con «Volver a arbitrar».
  - **Configuraciones:** árbitro SOL (el modelo de A) y LUNA (el de B).
  - **Fases:** preguntas, donde más difieren A y B, y catálogo, la más representativa. El backlog queda fuera porque depende del catálogo.
  - **Repeticiones:** 3 por configuración y fase, porque el mismo árbitro no da siempre lo mismo (muestreo del modelo y orden sorteado de las propuestas). El arbitraje original cuenta como uno de los 3 de SOL.
  - **Métrica principal:** lo que entra solo de B en la unión y su variación entre árbitros, no quién predomina.
- **Estado:** preguntas, 1 de 3 con LUNA; catálogo, 1 con LUNA en curso; faltan los 2 nuevos con SOL de cada fase.
- **Primer dato** (sin valor estadístico): con LUNA de árbitro, lo que entra solo de B en las preguntas sigue en 6, como con SOL.
- **Decisiones del autor:** darle importancia en la documentación para la memoria; 3 repeticiones por configuración.
- **Cambios de criterio:** ninguno.
- **Pendientes:** completar los arbitrajes (2 con LUNA y 2 con SOL por fase) y escribir los resultados en el documento del experimento.

---

## 16-10-2026, 12:40 (sesión)

**Resumen para la bitácora (16-10-2026, transición entre modo claro y oscuro)**

- **Qué se hizo:**
  - **Revelado circular:** el tema nuevo aparece como un círculo que crece desde el botón de sol o luna hasta cubrir la página, en 450 ms. Usa la View Transitions API del navegador: una imagen de antes y otra de después, con la nueva recortada en un círculo que crece. Sin dependencias nuevas.
  - **Icono animado:** el sol y la luna están superpuestos en el botón; el del tema siguiente aparece y el otro gira y se encoge, en 500 ms.
  - **Sin la API, o con «reducir movimiento» activado en el sistema,** el cambio es instantáneo, como antes.
  - Web reconstruida sin tocar la API, con los arbitrajes en marcha.
- **Decisiones del autor:** opciones 3 (revelado circular) y 4 (icono animado) entre las cinco propuestas.
- **Decisiones de Claude (revisables):** duraciones de 450 y 500 ms y curva de aceleración estándar; no hizo falta ninguna skill.
- **Cambios de criterio:** ninguno.
- **Pendientes:** que el autor lo vea en el navegador; los arbitrajes del experimento.

---

## 16-10-2026, 13:05 (sesión)

**Resumen para la bitácora (16-10-2026, error interno por una clave repetida)**

- **Qué pasó:** un arbitraje del catálogo con LUNA terminó con «Error interno al ejecutar». LUNA escribió el catálogo unido y la ejecución se detuvo al compactar, pero un elemento tenía `"propuesta_2"` dos veces en su `de`. `JsonNode`, que usan la limpieza de la marca `sigue` (del 15-10), la de los prefijos del árbitro y el validador, lanza una excepción con las claves repetidas. La revisión se cayó en lugar de pedir una reparación.
- **Arreglo:** `JsonDuplicates` busca las claves repetidas con `JsonDocument`, que sí las admite, antes de cualquier otra comprobación. Las convierte en el error `CLAVE-DUP`, con la ruta (por ejemplo, `requisitos[12].de: propuesta_2`), y pide la reparación. Hay 2 tests nuevos (833 en total).
- **Decisión de Claude (revisable):** pedir la reparación en lugar de juntar los valores automáticamente, porque juntarlos sería decidir por el modelo. El código no existe en la referencia en Python, que se queda con la última clave.
- **Cambios de criterio:** ninguno.
- **Pendientes:** desplegar la API cuando no haya tareas en marcha; repetir el arbitraje fallido; los demás arbitrajes del experimento.

---

## 16-10-2026, 13:45 (sesión)

**Resumen para la bitácora (16-10-2026, la reparación del árbitro del catálogo no cabe)**

- **Qué pasó:** otro arbitraje del catálogo con LUNA necesitó reparación. La reparación pega en el prompt el catálogo anterior entero (unos 30.000 tokens) y no cabía en 128k junto con las dos propuestas. Claude Code intentó compactar, la compactación falló (`too_few_groups`) y las peticiones a la pasarela fallaron hasta agotar los reintentos.
- **Qué se hizo:** la plataforma junta ella misma las claves repetidas cuando todos sus valores son listas, con un aviso. Así este error ya no provoca una reparación. Si no son listas, sigue siendo `CLAVE-DUP` con reparación. Lo que se guarda va ya unido (`PhaseTask.Clean`, que también quita la marca `sigue`). Hay 3 tests nuevos (835 en total).
- **Hallazgo para la evaluación:** como árbitro del catálogo, LUNA falla más que SOL (2 de 3 intentos, por errores de formato). Se anota en el documento del experimento, para declarar la tasa de fallos junto a los resultados.
- **Cambio de criterio:** la decisión de la mañana (pedir la reparación ante una clave repetida, para no decidir por el modelo) se sustituye cuando los valores son listas: la plataforma los junta, porque la reparación de un catálogo grande no cabe en el contexto.
- **Pendientes:**
  - **Decisión del autor:** reparar editando solo los elementos con errores (con la herramienta `Edit` en las reparaciones), en lugar de reescribir la salida entera.
  - Desplegar cuando termine la tanda de arbitrajes de preguntas.
  - Repetir los arbitrajes del catálogo.

---

## 16-10-2026, 14:30 (sesión)

**Resumen para la bitácora (16-10-2026, paso 65 y cierre del hito 13)**

- **Qué se hizo:** análisis del CSV del proyecto mediano (banco de alimentos) y documento de resultados de los dos proyectos, en `docs/resultados-paso-65/README.md` (con los dos CSV). Paso 65 y hito 13 cerrados.
- **Mediano:**
  - **Coste y tiempo:** 2,23 M tokens de entrada y 504.000 de salida, en 55 minutos y sin ninguna reparación.
  - **Mockup:** el 70 % de la entrada, con 20 pantallas y las 45 historias.
  - **Lo que propone cada modelo:** preguntas 27 / 21 → 29; catálogo 120 / 68 → 100; backlog 45 / 28 → 45. Solo de B entran 2, 1 y 0.
  - **Contradicciones:** 12 de 12 tratadas como se esperaba (10 abiertas preguntadas; la resuelta y el hueco, en el catálogo).
- **Los dos proyectos juntos:**
  - 38 de 38 contradicciones y huecos tratados como se esperaba;
  - 15 de 15 contradicciones abiertas preguntadas, 2 solo por A y ninguna solo por B;
  - la unión se parece a A en las tres fases;
  - el mockup se lleva entre el 63 % y el 70 % del coste.
- **Limitaciones declaradas:** proyectos, lista y comprobación hechos por Claude; una sola ejecución; respuestas de la IA; sin cambios del usuario; sin documento público.
- **Decisiones:** cerrar el hito 13 con los dos proyectos ficticios, como pidió el autor el 15-10-2026.
- **Cambios de criterio:** ninguno.
- **Pendientes:** terminar el experimento del sesgo del árbitro; valorar un documento de PURE; las herramientas externas candidatas.

---

## 16-10-2026, 15:00 (sesión)

**Resumen para la bitácora (16-10-2026, plan de los hitos 17 a 22)**

- **Qué se hizo:** repaso de todo lo pendiente del proyecto y plan nuevo, acordado con el autor, en `docs/plan-de-construccion.md`:
  - **Hito 17, fiabilidad y calidad:** LUNA, reparar editando, trabajos interrumpidos, compactaciones, sugerencias más prudentes, variedad del mockup sin `.zip` y consumo de las ejecuciones detenidas. Varios pasos están marcados «por decidir».
  - **Hito 18, exportar:** el catálogo a Word y PDF, y el backlog a `.feature` y a CSV para Jira o Azure DevOps.
  - **Hito 19, embeddings en local:** modelo multilingüe con ONNX Runtime, emparejamiento de A y B por significado y árbitro con embeddings.
  - **Hito 20, trazabilidad visual y comparación** de A, B y el árbitro.
  - **Hito 21, historial de versiones** de los entregables.
  - **Hito 22, evaluación:** el experimento del sesgo, AQUSA y `vale-ears`, un documento de PURE y una prueba editando (con los pasos 49 y 53).
- **Revisado por el autor:** la interfaz del hito 14 y el mockup con el estilo de un `.zip` del hito 15 funcionan bien.
- **Observación del autor:** sin `.zip`, el mockup sale siempre con el mismo estilo y la misma organización. Es el paso 90.
- **Decisiones:**
  - **Del autor:** las 5 funcionalidades (exportar, trazabilidad, comparación, historial y métricas externas) y las mejoras propuestas. Queda fuera abaratar el mockup.
  - **De Claude:** el árbitro con embeddings pasa de «Después» al hito 19, por delante de la trazabilidad, que lo aprovecha.
- **Cambios de criterio:** el árbitro con embeddings deja de ser un «después» sin fecha y pasa a ser el hito 19.
- **Pendientes:** el último arbitraje del experimento; empezar el hito 17 por el paso que elija el autor.

---

## 16-10-2026, 15:20 (sesión)

**Resumen para la bitácora (16-10-2026, embeddings: decisión y prioridad)**

- **Qué se hizo:** explicación al autor de qué son los embeddings (convertir un texto en un vector de números que representa su significado, de forma que frases que dicen lo mismo con palabras distintas dan vectores parecidos) y para qué servirían en Concilia:
  - emparejar A y B fuera del LLM;
  - medir la discrepancia con un umbral;
  - un árbitro más barato y estable;
  - una referencia objetiva para el experimento del sesgo.
- **Decisiones del autor:**
  - usar los embeddings «al 100 %»;
  - **origen:** el modelo local `paraphrase-multilingual-MiniLM-L12-v2` (ONNX, Apache 2.0) con ONNX Runtime en el contenedor de la API, frente a un proveedor externo;
  - **prioridad:** el hito 19 pasa a ser el siguiente, justo después del experimento, por delante del 17 y el 18.
- **Cambios de criterio:** se cierra la decisión abierta desde el inicio del proyecto sobre el origen de los embeddings. Sigue abierto el umbral de discrepancia grave, que se calibrará con datos reales en el paso 97.
- **Pendientes:** el último arbitraje del experimento; empezar el hito 19 (paso 96, servicio de embeddings).

---

## 16-10-2026, 15:45 (sesión)

**Resumen para la bitácora (16-10-2026, estado del experimento y cambio de chat)**

- **Experimento del sesgo del árbitro:**
  - **Preguntas:** completo, con 3 arbitrajes de SOL y 3 de LUNA.
  - **Catálogo:** SOL completo (el original, 10:22 y 10:31 UTC); LUNA con 2 válidos de 5 intentos (07:42 y 10:39 UTC).
  - **Lo que entra solo de B:** en preguntas, 6 en los 6 arbitrajes; en catálogo, 1, 7 y 0 con SOL y 3 y 0 con LUNA.
  - **Lectura provisional:** A propone más y el árbitro no favorece a su propio modelo.
  - **Falta:** un último intento de LUNA en el catálogo; si falla, se cierra con 2 válidos.
- **Fallo del quinto intento de LUNA en el catálogo:** inactividad, reparación y 7 errores de tipo (`RF-066` con tipo `RNF`). La corrección mecánica del prefijo se añade al paso 85 como candidata.
- **Hallazgo:** como árbitro de un catálogo grande, LUNA es mucho menos fiable que SOL (2 de 5 frente a 3 de 3).
- **Decisión del autor:** seguir en un chat nuevo, porque este es muy largo. Toda la información para la memoria está en `docs/`, en git, en `CLAUDE.md` y en la memoria persistente.
- **Pendientes:**
  - el último arbitraje de LUNA y escribir los resultados del experimento;
  - empezar el hito 19 (o el 17, si el autor cambia el orden).

---

## 16-10-2026, 16:00 (sesión)

**Resumen para la bitácora (16-10-2026, plan reordenado)**

- **Qué se hizo:** a petición del autor, el plan queda en el orden de trabajo, para empezar sin saltos. Los hitos 17 a 22 se renumeran:

  | Antes | Ahora | Pasos |
  | --- | --- | --- |
  | 19 (embeddings) | **17** | 85–88 |
  | 17 (fiabilidad y calidad) | **18** | 89–95 |
  | 18 (exportar) | **19** | 96–98 |
  | 20, 21 y 22 | sin cambios | 99–106 |

- El plan tiene un apartado nuevo, «Dónde estamos», con lo que está en curso (el experimento, paso 103) y el siguiente paso (el 86, servicio de embeddings).
- `CLAUDE.md` y la trazabilidad, actualizados con los números nuevos. **Las entradas anteriores de esta bitácora conservan los números de entonces.**
- **Decisiones:** ninguna nueva; solo cambia la numeración.
- **Cambios de criterio:** ninguno.
- **Pendientes:** el último arbitraje de LUNA; empezar el paso 86 en un chat nuevo.

---

## 16-10-2026, 16:30 (sesión)

**Resumen para la bitácora (16-10-2026, experimento del árbitro cerrado)**

- **Qué se hizo:** cierre del experimento de si el árbitro prefiere a su propio modelo, con la tabla de todos los arbitrajes en `docs/experimento-sesgo-arbitro.md`.
- **Resultados (las mismas propuestas en todos los arbitrajes de cada fase):**
  - **Preguntas:** lo que entra solo de B es 6 en los 6 arbitrajes (3 de SOL y 3 de LUNA). SOL da exactamente lo mismo las tres veces; LUNA varía en los descartes.
  - **Catálogo:** solo de B entran 1, 7 y 0 con SOL (media 2,7) y 3 y 0 con LUNA (media 1,5). Solo de A, entre 27 y 30 siempre. El tamaño de la unión con SOL varía mucho (209, 220 y 291), porque cada ejecución fusiona más o menos.
- **Conclusión:** se confirma H1. A es más completo y el árbitro no favorece a su propio modelo. El diseño de propuestas anónimas en orden aleatorio no muestra sesgo. Con estos modelos, la verificación cruzada aporta poco por lo que solo encuentra B; su valor está en confirmar lo que coincide y en las discrepancias.
- **Hallazgo secundario:** LUNA, como árbitro de un catálogo grande, termina 2 de 6 intentos, frente a 3 de 3 de SOL. Todos sus fallos son de formato o de tamaño: clave repetida, reparación que no cabe, tipos mal puestos, salida cortada por el tope y JSON mal formado. En las preguntas, una salida pequeña, no falló ninguna vez.
- **Limitación:** en el catálogo, LUNA solo tiene 2 arbitrajes válidos. La conclusión se apoya sobre todo en las preguntas.
- **Decisiones del autor:** cerrar el catálogo con 2 arbitrajes válidos de LUNA, como se acordó antes del último intento.
- **Cambios de criterio:** ninguno. Se cierra la pregunta clave abierta el mismo día.
- **Pendientes:** seguir en un chat nuevo con el hito 17 (embeddings), paso 86. El paso 87 dará la referencia independiente del experimento.

---

## 19-10-2026, 09:00 (sesión)

**Resumen para la bitácora (19-10-2026, hito 17, paso 86: servicio de embeddings)**

- **Qué se hizo:** el servicio de embeddings en la API, en `Concilia.Core/Embeddings`:
  - `ITextEmbedder` (textos → vectores), con dos implementaciones: `OnnxTextEmbedder`, con el modelo real, y `FakeTextEmbedder`, que cuenta las palabras compartidas y sirve para los tests y el proveedor `Fake`;
  - `XlmRobertaTokenizer`: el tokenizador SentencePiece del modelo (Microsoft.ML.Tokenizers 2.0.0, MIT), con la traducción de ids de fairseq que hace Hugging Face (los trozos, un puesto más; `<s>`, `<pad>`, `</s>` y `<unk>`, en 0 a 3);
  - `VectorMath`: la similitud coseno y la media de los vectores de token sin el relleno (lo que hace sentence-transformers con este modelo);
  - el modelo, `paraphrase-multilingual-MiniLM-L12-v2` en fp32 con ONNX Runtime 1.30.0 (MIT). Se carga la primera vez que se usa, procesa los textos de 32 en 32 y los corta a 128 tokens.
- **Dónde está el modelo:**
  - **En Docker:** la imagen de la API descarga `model.onnx` y `sentencepiece.bpe.model` al construirse (`ADD --checksum`), con el commit de Hugging Face fijado (`e8f8c21`) y el SHA-256 comprobado.
  - **En local:** `scripts/descargar-modelo-embeddings.ps1` lo deja en `models/` (ignorado por git y por Docker).
  - **Configuración:** `Embeddings:Provider` es `Fake` por defecto y `Onnx` en `docker-compose.yml` (`EMBEDDINGS_PROVIDER`).
  - `POST /embeddings/similarity` devuelve la similitud de dos textos, para probarlo a mano.
- **Tests:** 19 nuevos (854 en total). Los 6 que usan el modelo se saltan si no está. Comparan con una referencia calculada en Python (`tests/fixtures/embeddings/generar_referencia.py`: tokenizador de Hugging Face y sentence-transformers 6.1.0 con los pesos originales), con 14 textos en español e inglés, espacios, tildes, caracteres de ancho completo, emojis y un texto de más de 128 tokens, y comprueban el relleno, los lotes y el corte a 128 tokens.
- **Resultados:**
  - **Fidelidad:** los mismos ids de tokens que Hugging Face en los 14 casos; diferencia máxima con los vectores de sentence-transformers de 9,4·10⁻⁷ (el redondeo de la referencia) y coseno mínimo de 1,0000000.
  - **Similitudes:** el mismo requisito en español y en inglés, 0,888 y 0,839; reservar frente a cancelar una reserva, 0,294; reservar frente a exportar un informe o el tiempo de respuesta, 0,063 y 0,091. Es una primera idea para el umbral del paso 87, no una calibración.
  - **Velocidad (CPU del portátil):** la primera llamada, con la carga del modelo, unos 6 s; 500 textos de unos 30 tokens, unos 6,4 s.
  - **En Docker:** imagen construida con otra etiqueta y probada en un contenedor aparte, sin tocar los que estaban en marcha. `POST /embeddings/similarity` da en Linux lo mismo que en Windows (0,888 y 0,063). La imagen de la API crece unos 530 MB: 475 del modelo y unos 55 de las bibliotecas nativas de ONNX Runtime para Linux.
- **Decisiones del autor:**
  - el modelo en fp32 (470 MB), no el cuantizado en int8 (120 MB), para dar los mismos vectores que la referencia de Python;
  - los tests que necesitan el modelo se saltan si no está, también en GitHub Actions, que no lo descarga.
- **Cambios de criterio:** ninguno.
- **Notas:**
  - dos problemas encontrados al probar la imagen y corregidos: un `ADD --chmod` remoto aplica el modo también a las carpetas que crea, que quedaban sin permiso de entrada (`drw-r--r--`). Ahora el modelo se descarga en una etapa propia y se copia con los permisos corregidos. Además, ONNX Runtime traía sus bibliotecas nativas de todas las plataformas (unos 240 MB), y la imagen solo conserva las de Linux;
  - el aviso de vulnerabilidad de `System.Security.Cryptography.Pkcs` 10.0.12 al compilar ya salía antes de este paso; no lo traen los paquetes nuevos;
  - `scripts/descargar-modelo-embeddings.ps1` se guarda con BOM, porque Windows PowerShell 5.1 lee como ANSI los scripts sin él y estropea las tildes.
- **Pendientes:** paso 87, emparejamiento de A y B por significado y calibración del umbral con datos reales.

---

## 19-10-2026, 11:00 (sesión)

**Resumen para la bitácora (19-10-2026, hito 17, paso 87: emparejamiento de A y B por significado y umbrales calibrados)**

- **Qué se hizo:**
  - **Emparejamiento sin LLM** (`Concilia.Core/Matching/ProposalMatcher`):
    - cada elemento de A se compara con todos los de B y al revés, y se une con el más parecido;
    - el par es «mismo», «parecido» o «distinto» según dos umbrales, y varios elementos pueden unirse con el mismo, como hace el árbitro;
    - el texto comparado es la pregunta, el texto del requisito o «título + como… quiero… para…» de la historia;
    - lo que juntó el árbitro sale de su `de`, con el orden de las propuestas del plan.
  - **`GET /jobs/{id}/matching`** (también con `?same=&similar=`): los pares, su similitud, su nivel y si el árbitro los juntó, más los recuentos y el acuerdo con el árbitro. La lectura de las propuestas de A y B que hacía el informe pasa a `JobProposals`, compartida por los dos.
  - **Scripts reproducibles** en `scripts/calibracion-embeddings/`, que leen la API en marcha:
    - `muestra.cs` saca la muestra para etiquetar, con semilla fija;
    - `calibrar.cs` calcula los resultados y escribe `docs/calibracion-embeddings/resultados.md`.
  - **Arreglo del paso 86** (commit propio): la web (nginx y Vite) no reenviaba `/embeddings` a la API.
  - **Contenedores:** se reconstruyeron la API y la web sin ninguna tarea en marcha; la imagen ya lleva el modelo.
- **Decisiones del autor:**
  1. **Dos umbrales**, no uno. Entre los dos, la franja «parecido»: se parece pero no es lo mismo, y es la candidata a «discrepancia grave» en el paso 88.
  2. **Calibrar solo con los proyectos ficticios:** banco de alimentos, clínicas veterinarias (con los arbitrajes de SOL y LUNA del experimento) y ReservaSalas.
  3. **Cómo calibrar: la opción D.** Se explicaron cuatro opciones antes de elegir:
     - A, un árbitro con calibración y comprobación separadas;
     - B, el consenso de varios árbitros;
     - C, una muestra etiquetada por el autor;
     - D, C para fijar los umbrales y B para comprobarlos.

     Se descartó calibrar solo con el árbitro porque es circular: los embeddings sirven para comprobarlo.
  4. **Criterio de etiquetado** (aclarado a petición del autor): lo que cuenta es lo que dicen, no el tema. Dos textos del mismo tema que dicen cosas distintas son «distinto». Escrito con ejemplos inventados en `docs/calibracion-embeddings/README.md`.
  5. **Umbrales fijados: «lo mismo» desde 0,90 y «parecido» desde 0,70.** La calibración proponía 0,90 y 0,71, los centros de los intervalos óptimos 0,89–0,92 y 0,70–0,72. Con 0,70 los resultados no cambian. Quedan en `Embeddings:SameThreshold` y `Embeddings:SimilarThreshold`.
- **Muestra:** 48 pares, 8 por tramo de similitud entre 0,4 y 1,0, con las tres fases por turnos y el texto de A unas veces primero y otras segundo. Por proyecto, 31 de clínicas, 15 del banco de alimentos y 2 de ReservaSalas; por fase, 18 preguntas, 16 historias y 14 requisitos. El autor los etiquetó sin ver la similitud ni lo que hizo el árbitro: 9 «mismo», 17 «parecido» y 22 «distinto».
- **Resultados** (`docs/calibracion-embeddings/resultados-y-decision.md`):
  - **Embeddings frente al autor:**
    - 37 de 48 pares coinciden con las tres etiquetas (77 %): preguntas 17 de 18, historias 12 de 16 y requisitos 8 de 14;
    - «mismo» frente al resto, 85 %; «distinto» frente al resto, 88 %.
  - **Frente al consenso de los árbitros de clínicas** (5 arbitrajes del catálogo, 3 de SOL y 2 de LUNA; 6 de las preguntas, 3 y 3). Consenso «juntar» es al menos el 80 % de los árbitros; «no juntar», como mucho el 20 %.
    - De lo que se junta, los embeddings ven «mismo o parecido» el 99 % en el catálogo (246 de 248) y el 82 % en las preguntas (9 de 11).
    - De lo que no se junta, ven «distinto» el 90 % en las preguntas, pero solo el 11 % en el catálogo, donde casi todo cae en la franja «parecido».
  - **El árbitro frente al autor (primera medida frente a un criterio humano):** juntó 9 de 9 «mismo», 12 de 17 «parecido» y 2 de 22 «distinto».
  - **Acuerdo por elemento con el árbitro** («¿está también en la otra propuesta?») en los 9 trabajos originales de los proyectos ficticios: 89 % (937 + 73 de 1.129 elementos).
  - **Referencia del experimento del sesgo:** en clínicas, solo en B según los embeddings hay 8 preguntas, 1 requisito y 1 historia, frente a 6, 1 y 0 según el árbitro. Las 2 preguntas de diferencia tienen similitud 0,62 y 0,64 y el árbitro las juntó. Confirma, sin LLM, que casi todo lo que propone B ya está en A. Añadido a `docs/experimento-sesgo-arbitro.md`.
  - **Dónde fallan los embeddings:**
    - textos con casi las mismas palabras que dicen otra cosa (0,91 con dos requisitos de integración que el autor vio distintos; el árbitro también los juntó);
    - lo mismo en estilo telegráfico (0,69).
- **Cambio de criterio:** el acuerdo con el árbitro contaba un elemento como presente en la otra propuesta solo si su par era «mismo». Ahora cuenta también «parecido», porque los datos muestran que el árbitro también junta lo parecido, conservando el detalle de cada uno. Así es coherente con «solo A» y «solo B», que son los «distinto».
- **Decisión abierta acotada, no cerrada:** quedan fijados los umbrales del emparejamiento. Cómo marcan la «discrepancia grave» se decide en el paso 88; la candidata es la franja «parecido».
- **Tests:** 10 nuevos del emparejamiento y la comprobación del endpoint en el test del flujo completo. 864 en total.
- **Pendientes:**
  - paso 88, árbitro con embeddings, que necesita decidir cómo marca la discrepancia grave;
  - **decidido por el autor después de la sesión:** el emparejamiento entra en el informe por trabajo y en el CSV del proyecto en el hito 20, paso 100, no ahora, porque calcularlo en cada informe cuesta unos segundos por trabajo. Anotado en el plan.

---

## 19-10-2026, 12:00 (sesión)

**Resumen para la bitácora (19-10-2026, hito 17, paso 88: árbitro con embeddings, prompts v0.10)**

- **Qué se hizo:**
  - **Emparejamiento para el árbitro:** antes de llamar al árbitro, la plataforma empareja por significado las dos propuestas (`ArbiterMatching`, con los umbrales 0,90 y 0,70 de la configuración). Le pasa el resultado en `input/emparejamiento.txt` (`MatchingFile`), con los números de las propuestas para que A y B sigan siendo anónimos: `1:RF-003 2:RF-001 mismo 0.94`, `… parecido 0.78` o `1:RF-020 sin-par`. Lo lee entero, como las propuestas, y la plataforma comprueba la lectura.
  - **Prompts v0.10** de los tres árbitros: el paso 1 de §2 es «parte del emparejamiento» (los «mismo» se unen, los «parecido» los decides tú, los «sin par» solo se unen si de verdad son lo mismo, y puedes apartarte si la fuente lo justifica, diciéndolo en `explicacion`). En el catálogo y el backlog, además, el aviso de que el emparejamiento no detecta contradicciones. Los valores de los umbrales no aparecen en el texto, para que no se desajusten si cambia la configuración.
  - **Desviaciones** (`ArbiterDeviations`): después del árbitro, la plataforma busca dónde unió algo sin nada «mismo» ni «parecido» en la otra propuesta, o dejó en dos elementos distintos un par «mismo». Descartar uno de los dos no cuenta.
    - Se guardan en `resultado-arbitro.json` como `desviaciones`, aparte de la salida del árbitro, que queda tal cual.
    - En los borradores del catálogo y del backlog, esos elementos quedan como discrepancia, con `[Plataforma] …` detrás de la explicación del árbitro.
    - El informe, el CSV (`desviaciones`) y la web las cuentan aparte de las discrepancias del árbitro.
  - **Tests:** 7 nuevos de las desviaciones y del fichero de emparejamiento; el árbitro de las tres fases recibe y lee el emparejamiento; el informe del flujo completo trae el recuento. 871 en total. Los 46 tests de la referencia en Python siguen pasando (el validador no cambia).
- **Decisiones del autor**, entre las opciones que se le explicaron con datos de los 9 trabajos de los proyectos ficticios:
  1. **El emparejamiento es orientativo (b).** Se descartó obligarlo (a), porque los embeddings también se equivocan (M20, 0,91, distintos para el autor) y las reparaciones son caras, y pasarlo solo como pista (c).
  2. **La discrepancia grave es D2:** lo que el árbitro hace en contra de los números, más la discrepancia que marca el propio árbitro, que se mantiene porque los embeddings no ven contradicciones.
     - D1 (toda la franja «parecido» unida) habría marcado 91 de 209 requisitos en el catálogo grande y 51 de 109 historias.
     - D2 se estimó en 0–10 por trabajo, y 21 en el backlog grande.
     - D3 (solo el LLM, como antes) marcaba 0–2.
  3. **Las preguntas reciben el emparejamiento y no marcan nada:** sus desviaciones solo se cuentan en el informe.
- **Cambios de criterio:**
  - sustituye al «árbitro solo con el LLM» de la iteración v0 (08-10-2026);
  - se cierra la decisión abierta del umbral de «discrepancia grave», con los umbrales del paso 87 y las desviaciones del 88.
- **Pendientes:**
  - **prueba real, que lanza el autor:** reconstruir la API y volver a arbitrar 2 o 3 veces el catálogo de clínicas con la v0.10. Comparar el tamaño de la unión (antes 209, 220 y 291 con las mismas propuestas), lo que entra solo de A y de B y las desviaciones, y anotarlo en `docs/evolucion-prompt-requisitos.md` (sección v0.10);
  - después, el hito 18.

---

## 19-10-2026, 12:30 (sesión)

**Resumen para la bitácora (19-10-2026, hito 18, paso 89: correcciones automáticas de formato)**

- **Qué se hizo:** `PlatformFixes`, que corrige sola cuatro errores de formato con una única solución evidente y deja un aviso con su código. Se aplica antes de comprobar cada intento (`PhaseTask.Review`) y a lo que se guarda (`PhaseTask.Clean`), igual que la unión de claves repetidas y la marca `sigue`:
  - **`REF-LINEA`:** un fragmento citado que no existe, cuyo número es una línea de `input/documentacion.txt` con un fragmento del mismo documento, pasa a ser ese fragmento. Con otro documento sigue siendo `REF-FRAG`, con la pista de qué fragmento hay en esa línea.
  - **`TIPO`** (catálogo de A, B y árbitro): si algún prefijo no coincide con su tipo, se toma el tipo como bueno y se renumera por tipo, como hace el borrador. Ningún otro elemento cita los ids propios.
  - **`ARB-FALTA`** (árbitro): un elemento de una propuesta que no está en ningún `de` ni en los descartes se añade al `de` del elemento donde está su par «mismo» de `input/emparejamiento.txt`. Si no tiene par «mismo», o sus pares están en varios elementos, sigue siendo un error. El contexto del validador lleva ahora el emparejamiento.
  - **`FRAGMENTO`** (pantalla del mockup): una pantalla escrita como documento entero se queda con el contenido de su `<body>`, con los `<style>` y `<script>` de su `<head>` dentro de la `<section>` raíz. Se corrige en el propio fichero, con un aviso.
  - **Tests:** 6 nuevos y 2 adaptados (el de la cita con número de línea y el de la pantalla envuelta en `<html>`). 877 en total.
- **Decisiones del autor** (con la recomendación de Claude):
  - las cuatro correcciones, la de `ARB-FALTA` solo con pares «mismo»;
  - los prompts no cambian en este paso, para poder medir por separado el efecto de las correcciones (los del árbitro acababan de cambiar en la v0.10).
- **Fuera del paso:** el JSON mal formado y la salida cortada por el tope, que no tienen arreglo mecánico (paso 90).
- **Cambios de criterio:** se cierran dos decisiones abiertas, corregir las citas que son números de línea y quitar las etiquetas de documento de las pantallas, que antes pedían reparación.
- **En paralelo**, mientras el autor lanzaba los arbitrajes de la prueba del paso 88:
  - `scripts/evaluacion/comparar-arbitrajes.cs` compara los arbitrajes de las mismas propuestas por modelo y versión del prompt;
  - `GET /jobs/{id}/matching` calcula también las desviaciones de los trabajos de antes del paso 88, es decir, lo que la plataforma habría marcado.
- **Pendientes:** terminar la prueba del paso 88 y anotar sus resultados; después, el paso 90 (por decidir).

---

## 19-10-2026, 13:00 (sesión)

**Resumen para la bitácora (19-10-2026, prueba real del paso 88 e hito 17 cerrado)**

- **Qué se hizo:**
  - el autor lanzó 3 nuevos arbitrajes con `SS - GPT-6-SOL` y los prompts v0.10 sobre las mismas propuestas del catálogo del proyecto grande de ejemplo (A 299 requisitos, B 192);
  - se reconstruyó la API con los pasos 88 y 89 cuando terminaron;
  - se compararon con los arbitrajes del experimento del sesgo con `scripts/evaluacion/comparar-arbitrajes.cs`, que ahora solo da por «en curso» un trabajo con una ejecución en cola o en marcha. Tabla en `docs/resultados-arbitro-v0.10/clinicas-veterinarias-catalog-v0.md`.
- **Resultados:**
  - **SOL v0.10:** 3 válidos de 3, unión de 202, 213 y 222 requisitos (media 212,3); solo A 26–37; solo B 1,3 de media; 7,7 desviaciones de media; 0 discrepancias del árbitro; 5, 5 y 11 minutos.
  - **SOL v0.9:** unión de 209, 220 y 291 (media 240); solo B 2,7; las desviaciones que habría marcado la plataforma, 7,0.
  - **LUNA v0.9:** 2 válidos (204 y 209), con 7,5 desviaciones de media.
- **Lectura:**
  - **La unión es más estable** con la v0.10: varía 20 requisitos, frente a 82. Con 3 ejecuciones por versión es una señal, no una prueba.
  - **Las desviaciones no bajan:** el árbitro discrepa de los números en unos 8 requisitos aunque los tenga delante, así que son diferencias de criterio, y justifica que las revise el usuario.
  - **El árbitro no marca ninguna discrepancia propia:** lo que se revisa sale todo de la plataforma.
- **Incidencias:**
  - en la tercera ejecución, el primer intento agotó sus turnos sin escribir la salida (98 turnos, unos 1,15 M tokens de entrada) y la reparación la escribió;
  - los intentos válidos no tienen dato de consumo, porque se detuvieron al compactar con la salida escrita (paso 95);
  - un arbitraje de LUNA del 16-10 sigue sin fecha de fin, porque la API se reinició durante él (paso 91).
- **El hito 17 queda completado.**
- **Pendientes:**
  - más repeticiones para confirmar la estabilidad;
  - el intento que agotó los turnos (relacionado con el paso 92);
  - el consumo de las ejecuciones detenidas (paso 95), que impide comparar el coste de las versiones;
  - la petición del autor de una tabla de coste en tiempo y dinero por fase y versión.

---

## 19-10-2026, 13:05 (sesión)

**Resumen para la bitácora (19-10-2026, hito 18, paso 95, y coste en tiempo y dinero)**

- **Petición del autor:** una tabla con el coste en tiempo y en dinero de las fases del proceso, antes y después de cada cambio (por ejemplo, de los embeddings), para justificar los cambios del TFG también por su coste.
- **Decisiones del autor:**
  - **precios** de la pasarela LiteLLM (captura de su configuración), en dólares por millón de tokens: SOL y SOL 6.1, 2 de entrada y 10 de salida; LUNA, 0,10 y 0,50;
  - **estimar como se pueda** las ejecuciones sin dato (paso 95, adelantado), siempre que no se salte nada necesario. No depende de los pasos 90–94;
  - **comparar sobre el proyecto de clínicas veterinarias** y, si otro proyecto tiene la misma fase en varias versiones, usarlo también;
  - **medir el tiempo de CPU de los embeddings;**
  - **el mockup cuenta como coste**, porque ahora se hace con SOL por la pasarela;
  - el script, las tablas, el antes y el después y poder regenerarlo, como proponía Claude.
- **Qué se hizo:**
  - **`UsageEstimator`** (Core):
    - separa los intentos de la consola guardada de cada ejecución (las reparaciones publican en la del primer intento, con «[reintento]»);
    - reconstruye el contexto turno a turno, sabiendo que cada llamada vuelve a enviarlo entero: cada Read suma las líneas de su tramo (leídas de la carpeta de la ejecución), cada Write el fichero escrito y cada texto su longitud, y una compactación envía el contexto una vez y lo deja en un resumen;
    - convierte el resultado en tokens con `a·turnos + b·caracteres`;
    - 7 tests.
  - **`scripts/evaluacion/ajustar-estimador.cs`:** ajusta los coeficientes por mínimos cuadrados con las 125 ejecuciones con dato real y consola, por separado para el mockup (97) y las fases (28), y mide el error dejando cada ejecución fuera de su ajuste. Error típico por ejecución: 5 % y 9 % (mockup, entrada y salida) y 9 % y 20 % (fases). Error del total: −0,7 % y −1,0 %, y −3,5 % y −5,1 %. Resultado en `docs/costes/estimador.md`.
    - En el prototipo, la simulación sin compactaciones no servía para las fases (error del total del +60 %). Con ellas, el error bajó al −3,5 %.
  - **Informe:** cada ejecución sin dato lleva su estimación (`estimatedInputTokens`, `estimatedOutputTokens`), el trabajo la suma aparte y el CSV la añade en dos columnas. La web dice cuánto es estimado.
  - **Tiempo de los embeddings** (`ComputeTime`): el de reloj y el de CPU del emparejamiento y de las desviaciones, guardados con cada arbitraje (`emparejamiento` en `resultado-arbitro.json`). Están en el informe, el CSV y `GET /jobs/{id}/matching`.
  - **`scripts/evaluacion/costes.cs` y `precios.json`:** generan `docs/costes/costes.md`, con los precios, cada trabajo de clínicas, el flujo de cada proyecto por fase, el antes y el después (misma fase, mismos modelos, distinta versión del prompt) y el coste por papel y modelo. Los proyectos que no son de ejemplo salen anonimizados, y las ejecuciones del proveedor `Fake` no cuentan.
  - **`docs/costes/README.md`:** el método en palabras sencillas, las conclusiones y los límites.
  - Tests: 884 en total.
- **Resultados:**
  - **el flujo completo** cuesta 22,25 $ y 105 minutos (proyecto grande) y 8,76 $ y 55 minutos (mediano); el mockup es el 69 % y el 74 % del coste;
  - **B (LUNA)** es el 0,5 % del coste del flujo grande, frente al 12 % de A;
  - **el árbitro del catálogo grande con SOL:** a la primera, v0.10 cuesta unos 1,40 $ (unos 414.000 tokens de entrada, estimados) y v0.9 entre 1,58 y 1,98 $. La media de v0.10 (2,44 $) sube por la ejecución con reparación (4,53 $);
  - **el emparejamiento** cuesta unos 13 s de reloj, unos 54 s de CPU y 0 $.
- **Correcciones del script durante la revisión:**
  - no mezclar en el antes y el después árbitros de modelos distintos (SOL y LUNA en la v0.9 daban una media engañosa);
  - excluir las ejecuciones del proveedor `Fake`;
  - poner «sin precio» en los mockups de la suscripción;
  - medir aparte el emparejamiento de los arbitrajes v0.10 anteriores a la medición.
- **Pendientes:**
  - volver a ajustar el estimador cuando haya muchas más ejecuciones;
  - regenerar `costes.md` tras cada medición;
  - siguen el paso 90 y los demás pasos por decidir del hito 18.

---

## 19-10-2026, 14:00 (sesión)

**Resumen para la bitácora (19-10-2026, hito 18 terminado: pasos 90 a 94, prompts v0.11)**

- **Petición del autor:** acabar el hito 18 entero. Claude explicó a la vez las decisiones de los pasos 90 a 94, con opciones y datos, y el autor eligió todo como se recomendaba.
- **Paso 91, trabajos interrumpidos** (commit propio): al arrancar, `InterruptedJobs` marca como fallidas las ejecuciones a medias y cierra sus trabajos con un resultado «Interrumpido: la API se reinició…», sin relanzarlos. Un mockup interrumpido se puede reanudar. Solo toca lo creado antes del arranque, porque la API puede aceptar un trabajo nuevo mientras limpia. Sale de `JobWorker` a una clase propia con su test, que no se podía probar con `ExecuteUpdateAsync` en la base en memoria.
- **Paso 92, compactar sin escribir** (commit propio):
  - el runner detiene una ejecución en su segunda compactación si no ha escrito su salida (`CompactingBeforeOutput`), y `RetryRunner` la repite una vez desde cero, como una perdida por inactividad (una sola repetición por tarea entre las dos causas);
  - datos: compactar una vez antes de escribir pasó 4 veces y 3 acabaron bien; el árbitro del 19-10 compactó 6 veces en 98 turnos y unos 1,15 M tokens sin escribir;
  - fixture sintético nuevo.
  - **Incidencia:** el commit se hizo con un test en rojo (el que cuenta los fixtures), porque la orden encadenaba el commit al código de salida de `tail` y no al de los tests. Se arregló en el commit siguiente, y desde entonces el commit depende del código de salida de `dotnet test`.
  - También se quitó de dos tests la comprobación del tiempo transcurrido, que fallaba con la máquina cargada y no aportaba nada: el mensaje ya demuestra que la ejecución se detuvo y no agotó el tiempo.
- **Paso 90, reparar editando:**
  - la revisión de un intento puede pasar ficheros y herramientas al siguiente (`AttemptReview.RetryFiles` y `RetryTools`);
  - la de las fases deja la salida anterior en su fichero, ya revisada por la plataforma (con sus correcciones) y con un elemento por línea;
  - cada error que empieza por el id de un elemento lleva su línea (`ARB-FALTA` no, porque su id es de una propuesta);
  - la reparación recibe Edit, y un Edit cuenta como escritura para el runner;
  - el anexo v0.11 ya no lleva `<salida_previa>`; `{{SALIDA_PREVIA}}` es una nota de dónde está y cómo corregirla, y las notas del mockup dicen que se corrija con Edit;
  - `FakeProvider` reconoce ahora una reparación por el título del anexo.
- **Paso 93, sugerencias prudentes:**
  - **aprobar:** `AcceptSuggestions` solo aprueba las sugerencias con base `documentacion`; si todas son suposiciones, lo dice. El botón de la web dice cuántas aprueba;
  - **prompt:** una decisión del cliente se sugiere como «A decidir por el cliente:» con las opciones, y ante la duda la base es suposición;
  - **catálogo:** la vista del catálogo trae las preguntas respondidas con una suposición de la IA aceptada tal cual (`QuestionRules.IsAcceptedGuess`; una editada por el usuario no cuenta). La web marca los requisitos que las citan («Suposición de la IA») y tiene un filtro;
  - **informe:** cuenta esos requisitos (`basados_en_suposiciones` en el CSV);
  - no bloquea la validación.
- **Paso 94, estilo del mockup:** `StyleDirections`, 8 direcciones de estilo concretas (paleta, tipografía, navegación, densidad, formas). Cada proyecto recibe la suya por su id, guardada en el plan del mockup para que reanudar no la cambie, y el §5 pide adaptarla al sector y a los usuarios. Con `.zip` no cambia nada, y los planes anteriores siguen con el texto de antes.
- **Prompts v0.11:** `PromptVersion` sube a v0.11. CHANGELOG, README de los prompts y sección v0.11 de la evolución, con los resultados pendientes.
- **Tests:** 890 (6 se saltan sin el modelo de embeddings). Los 46 de la referencia en Python siguen pasando.
- **Cambios de criterio:**
  - la reparación ya no lleva la salida previa;
  - «Aprobar todas» ya no aprueba las suposiciones;
  - sin `.zip`, el mockup ya no es «sobrio y legible» por defecto;
  - se cierran las decisiones abiertas de los pasos 90, 91, 92 y 93.
- **Pendientes:**
  - las pruebas reales del hito, que lanza el autor: el flujo completo del proyecto grande sin errores internos ni reparaciones que no caben (unos 22 $ y 2 horas), y dos mockups sin `.zip` con estilos distintos;
  - medir la v0.11 y regenerar las tablas de coste;
  - después, el hito 19.

---

## 20-10-2026, 09:25 (sesión)

**Resumen para la bitácora (20-10-2026, guía para ejecutar el proyecto desde cero)**

- **Qué se hizo:**
  - a petición del autor, la sección «Puesta en marcha» del README pasa a ser «Ejecutar el proyecto desde cero», una guía paso a paso para un ordenador sin nada instalado: instalar Git y Docker Desktop (con WSL 2 en Windows), clonar, crear `.env` con solo `POSTGRES_PASSWORD`, arrancar con `docker compose up --build -d` y comprobarlo, una primera prueba en modo simulado con los ficheros de `ejemplos/mediano-banco-de-alimentos/`, conectar los modelos reales, parar, arrancar, actualizar y borrar los datos, y una tabla de problemas frecuentes;
  - se mantiene la parte de desarrollo sin Docker;
  - se explicó al autor para qué sirve cada script nuevo: ninguno tiene que lanzarlo él. Los de calibración ya se usaron; los de evaluación y costes los lanza Claude tras cada medición; la descarga del modelo solo hace falta fuera de Docker. Sirven también para que el tribunal pueda repetir los resultados.
- **El repositorio pasa a ser público** (autor, 20-10-2026): el README da la dirección real para clonarlo (<https://github.com/carlosalejoss/Concilia>). Se revisaron los ficheros y todo el historial de git en busca de claves, direcciones internas y ficheros `.env`, y no hay ninguno; solo aparece la contraseña de ejemplo del README.
- **Auditoría del repositorio público** (petición del autor): revisión de los 631 ficheros y de todo el historial de git (183 commits, incluidos los ficheros borrados).
  - **Sin problemas:**
    - ni claves, tokens, contraseñas ni cadenas de conexión;
    - nunca se han subido `.env`, `data/`, `memoria/` ni `models/`;
    - los commits usan el correo anónimo de GitHub;
    - no hay rutas con el usuario de Windows;
    - correos y URL son ficticios (`.example`, `.invalid`) o públicos;
    - los datos personales de `ejemplos/` son inventados;
    - los metadatos de los Word, Excel, PowerPoint y PDF de ejemplo son los de las librerías que los generaron;
    - las grabaciones de Claude Code de los tests están saneadas;
    - los documentos internos solo aparecen anonimizados, con cifras agregadas, y ningún nombre de proyecto interno.
  - **A valorar por el autor (riesgo bajo):**
    - tres commits (`dec0657`, `d45059f`, `f90664a`) llevan en su mensaje un enlace a una sesión de Claude Code, que solo puede abrir su dueño. Quitarlo exigiría reescribir el historial y forzar el push, y se desaconseja;
    - el nombre de la empresa, los nombres de los modelos de la pasarela y sus precios por token, que conviene confirmar con la tutora de empresa;
    - el ejemplo del prompt del mockup (socio, TPV y junta) deja ver el dominio del proyecto de prueba del documento C, sin datos;
    - la licencia sigue sin definir.
- **Decisiones:** ninguna nueva.
- **Pendientes:** las pruebas reales del hito 18; después, el hito 19.

## 20-10-2026, 10:00 (sesión)

**Resumen para la bitácora (20-10-2026, hito 19, paso 96: el catálogo a Word y PDF)**

- **Contexto:** esta sesión trabajó a la vez que otra que mide las pruebas reales del hito 18 que lanza el autor. Para no pisarse: sin reconstruir ni reiniciar contenedores, sin lanzar trabajos (solo tests y `Fake`) y con cada fichero añadido al commit uno a uno.
- **Qué se hizo:**
  - `Concilia.Core/Deliverables/Export`:
    - `CatalogExport` arma una vez el contenido del documento a partir del catálogo guardado, los fragmentos del plan del trabajo que lo generó y las preguntas del proyecto;
    - `CatalogDocx` lo escribe en Word con DocumentFormat.OpenXml, que ya estaba en el proyecto;
    - `CatalogPdf` lo escribe en PDF con MigraDoc, con IBM Plex Sans embebida (`ExportFonts`).
  - `GET /projects/{id}/catalog.docx` y `.pdf` (`ExportEndpoints`), que solo leen. En la web, «Word» y «PDF» en la barra del catálogo, ocultos mientras hay cambios sin guardar.
  - Tests: 7 nuevos, más la descarga en el flujo completo (897 en total, todos correctos). El `.docx` pasa `OpenXmlValidator` sin errores y el PDF se vuelve a leer con PdfPig.
  - Comprobado a mano: Word abre el `.docx` del catálogo de ReservaSalas (5 páginas, 18 títulos en el panel de navegación). Un catálogo de 250 requisitos sale en unos 20 ms en Word y 0,3 s en PDF.
- **Decisiones del autor** (todas como recomendaba Claude):
  - PDF con PDFsharp + MigraDoc. Descartadas QuestPDF (licencia gratis solo por debajo de 1 M$ de ingresos), LibreOffice en la imagen (unos 500 MB) e imprimir desde el navegador (sin tests).
  - El texto de las fuentes y las preguntas con su respuesta, en dos anexos y una sola vez cada uno, no junto a cada requisito.
  - Se puede exportar el borrador, con «BORRADOR» en la portada y el pie.
  - Con las marcas «Suposición de la IA» y «Discrepancia sin revisar»; sin el origen A/B ni la explicación del árbitro.
- **Cambio de criterio:** al implementarlo apareció que PDFsharp y MigraDoc pasaron a AGPL-3.0 en la versión 6.5.0 (14-10-2026). Se preguntó al autor, que eligió quedarse en la 6.2.4, la última MIT, fijada en el `.csproj` con un comentario para que nadie la actualice. La alternativa era escribir el PDF con PdfPig (Apache 2.0, ya en el proyecto), pero con unas 150 líneas propias de maquetación.
- **Observado, sin tocar:** `dotnet build` avisa de vulnerabilidades en `System.Security.Cryptography.Pkcs` 10.0.12. El aviso ya existía, porque lo traen MimeKit y MsgReader; MigraDoc depende de la misma versión.
- **Pendientes:**
  - ~~probar las descargas en Docker~~: hecho el mismo día, con permiso del autor y `GET /jobs/active` en 204. Se reconstruyeron la API y la web. Clínicas (209 requisitos) da 62 páginas en PDF y 59 en Word, y el banco de alimentos (100), 28 páginas; ambos en menos de 1 s. La fuente va embebida, los acentos salen bien, la hora es la peninsular y los `.docx` pasan el validador; Word abre el de clínicas;
  - pasos 97 (backlog a Gherkin) y 98 (CSV para Jira y Azure DevOps).

---

## 20-10-2026, 10:25 (sesión)

**Resumen para la bitácora (20-10-2026, prueba real del hito 18: el catálogo falla por un error del paso 92, corregido)**

- **Prueba real del hito 18**, en un proyecto nuevo con los ficheros del proyecto grande de ejemplo («Clinicas Veterinarias v0.11», prompts v0.11):
  - **preguntas:** 63, y las 63 sugerencias salieron como suposición, 40 de ellas «A decidir por el cliente». Es lo esperado: las preguntas existen porque la documentación no las resuelve;
  - **respuestas:** a petición del autor, solo para la prueba, Claude escribió una respuesta concreta en las 32 que seguían sin decidir (inventando algunos valores, por ejemplo precios y pesos) y aceptó tal cual la única propuesta pendiente. Quedan 39 «ia-editada», 23 «ia» (tal cual) y 1 «usuario»;
  - **catálogo:**
    - A (SOL) terminó a la primera;
    - B (LUNA) necesitó una reparación, y **la reparación editando funcionó**: leyó solo las dos líneas con el error, las corrigió con Edit y gastó unos 42.000 tokens de entrada y 465 de salida, sin reescribir el catálogo;
    - **el árbitro falló dos veces**, detenido por «compactar por segunda vez sin haber escrito la salida». El trabajo falló sin resultado.
- **Causa: un error del paso 92.** El runner contaba los avisos `status: compacting`, y Claude Code da dos por la misma compactación antes de su `compact_boundary` (secuencia `SS` en las dos ejecuciones; las compactaciones de verdad salen como `SB`). Así cortó el árbitro en su **primera** compactación, que la regla permite. El árbitro del catálogo grande lee las dos propuestas, el emparejamiento y casi toda la documentación, y suele compactar una vez.
- **Corrección:** un aviso solo abre una compactación nueva cuando la anterior ha terminado (`compactionInProgress`). Fixture `synthetic-compacta-una-vez.jsonl` y su test, en el que dos avisos seguidos no detienen la ejecución.
  - La corrección se probó sola en un *worktree* aparte (892 tests y 6 saltados, sin el modelo de embeddings), porque la carpeta del repositorio tiene cambios sin commit de la otra sesión (hito 19). El commit incluye solo los ficheros de esta corrección.
- **Coste perdido** por el fallo: los dos intentos del árbitro, estimados en unos 620.000 tokens de entrada (unos 1,3 $).
- **Pendientes:** reconstruir la API sin tareas en marcha y volver a lanzar el catálogo; después, el backlog y el mockup de la prueba, y el mockup del banco de alimentos.

---

## 20-10-2026, 10:31 (sesión)

**Resumen para la bitácora (20-10-2026, documentación de la API con Swagger, a petición del autor)**

- **Petición del autor:** un Swagger completo y cuidado con toda la información de la API, que se actualice solo cuando cambie o crezca la API.
- **Qué se hizo:**
  - `Concilia.Api/OpenApi/ApiDocumentation.cs`: el documento OpenAPI 3.1 con `Microsoft.AspNetCore.OpenApi` (`/openapi/v1.json`), con portada en Markdown (qué es Concilia, el flujo con sus endpoints y las convenciones: tareas en segundo plano con `202`, una tarea cada vez, errores como *ValidationProblem*, enumerados como texto) y 10 grupos (`ApiTags`) en el orden del flujo. Swagger UI en `/swagger` con enlaces directos, filtro, duración de cada petición y «Try it out» abierto.
  - Los 42 endpoints, documentados junto a su código: un `MapGroup("")` con su grupo por fichero, `WithSummary`, `WithDescription`, `Produces<T>` por respuesta y `[Description]` en parámetros y campos de las peticiones. `LaunchesJob()`, `ChangesQuestion()` y `ChangesDeliverable()` reúnen las respuestas que se repiten.
  - Dos transformaciones del documento: sin `servers` (detrás de nginx saldría el contenedor `api`, y «Try it out» fallaría) y los números como números (ASP.NET Core también los lee como texto y el generador los describía como «integer o string»).
  - Cambios pequeños en el código para que el documento se lea bien: `/health` y `/embeddings/similarity` devuelven un record (`HealthView`, `SimilarityView`) en lugar de un objeto anónimo, con el mismo JSON, y `Rearbitration.Request` pasa a `RearbitrationRequest`.
  - Estilo (`swagger-ui.css`, incrustado en la API): la paleta y la fuente de la web, la marca de Concilia en la barra y un color por método (GET azul de A, POST verde de validación, PUT ciruela de B, DELETE rojo). Swagger UI trae modo oscuro propio; la hoja redefine las variables en `html.dark-mode` y empieza cada regla por `html:root` para pesar lo mismo que las reglas oscuras de Swagger UI y ganarles por ir después. Comprobado con capturas de Edge sin ventana en los dos modos.
  - `/swagger` y `/openapi` pasan por nginx (Docker) y por el proxy de Vite; `launchSettings.json` abre `/swagger` al arrancar la API.
  - Copia en el repositorio: cada compilación en Debug escribe `docs/api/openapi.json` con `Microsoft.Extensions.ApiDescription.Server`, que arranca la aplicación sin servir y le pide el documento (unos segundos más por compilación). En Release no, y tampoco en Docker ni en GitHub Actions.
  - `OpenApiDocumentTests` (5 tests): toda operación tiene grupo declarado, resumen y respuesta 2xx que dice qué devuelve (salvo `204`); todos los endpoints de la API están en el documento; cada grupo tiene descripción y se usa; la copia de `docs/api/` coincide con la API (en CI, que compila en Release, avisa de un cambio subido sin la copia); y Swagger UI se sirve con el estilo. 903 tests, todos correctos, también los de la documentación en Release.
- **Decisiones (de Claude, a revisar por el autor):**
  - Swagger UI, como pidió el autor, y no Scalar ni Redoc. `Swashbuckle.AspNetCore.SwaggerUI` 10.2.3 (MIT) solo como interfaz, sobre el generador oficial de .NET 10; la 10.3.1 tenía un día.
  - Las descripciones van en el código de cada endpoint y no en comentarios XML: con lambdas el generador no los lee, y pasar a métodos con nombre habría cambiado los 42 endpoints.
  - La documentación de la API se publica sin autenticación, como el resto de la API.
  - Nueva regla en CLAUDE.md: cada endpoint nuevo o cambiado se documenta junto a su código y se sube con su `docs/api/openapi.json`.
- **Observado, sin tocar:** sigue el aviso de vulnerabilidades de `System.Security.Cryptography.Pkcs` 10.0.12 al compilar.
- **Pendientes:**
  - ver `/swagger` en Docker cuando no haya tareas en marcha y el autor permita reconstruir la API y la web;
  - los textos propios de Swagger UI («Try it out», «Filter by tag») siguen en inglés.

---

## 22-10-2026, 09:10 (sesión)

**Resumen para la bitácora (22-10-2026, prueba real del hito 18: el catálogo, relanzado, funciona)**

- **Catálogo relanzado** en «Clinicas Veterinarias v0.11» tras la corrección del paso 92. Terminó bien en 17 minutos:
  - **A (SOL):** bien a la primera.
  - **B (LUNA):** citó `DOC-14#F057`, que no existe. La corrección `REF-LINEA` no actuó, con razón: la línea 57 es de otro documento (DOC-01). La reparación editando corrigió solo esa línea (unos 42.000 tokens de entrada y 700 de salida).
  - **Árbitro, primer intento:** leyó 21 tramos de documentación, compactó una vez (`SSB`: los dos avisos de la misma compactación ya no lo cortan), leyó 13 tramos más y empezó a compactar por segunda vez sin haber escrito. Se detuvo, como debe, con unos 716.000 tokens de entrada estimados.
  - **Árbitro, repetición desde cero:** escribió el catálogo, pero dejó 2 requisitos de A sin clasificar (`ARB-FALTA`). La corrección automática no actuó porque no tenían un par «mismo» claro.
  - **Árbitro, reparación editando:** dos Edit, con 247.000 tokens de entrada y 4.700 de salida. Es la primera vez que la reparación de un árbitro de catálogo grande cabe en 128k: era el problema principal del hito 18.
- **Resultado:**
  - 235 requisitos (160 de ambas propuestas, 75 solo de A, 0 solo de B) y 4 descartados;
  - **12 discrepancias, todas de la plataforma** (desviaciones del emparejamiento) y ninguna del propio árbitro;
  - **55 requisitos marcados como «Suposición de la IA»**, a partir de las 23 respuestas aceptadas tal cual;
  - emparejamiento de 12,5 s (45,6 s de CPU).
- **Coste:**
  - el catálogo, unos 3,9 $ frente a 2,70 $ con la v0.9, porque el árbitro consulta casi toda la documentación aunque el prompt le dice que no;
  - las preguntas, 0,94 $ frente a 2,31 $;
  - el catálogo fallido del 20-10, unos 2,1 $ más.
- **Tablas de coste regeneradas.** El script reconoce ahora como proyecto de ejemplo uno cuyo nombre empieza por el de un ejemplo («Clinicas Veterinarias v0.11»), en vez de anonimizarlo.
- **Documentación:** resultados parciales de la v0.11 en la evolución del prompt y en la trazabilidad.
- **Mejora candidata** para la próxima versión de los prompts: que el árbitro consulte menos documentación.
- **Pendientes de la prueba:** el backlog (en marcha), el mockup sin `.zip` de este proyecto y el del banco de alimentos; al final, regenerar las tablas y cerrar el hito 18.


## 22-10-2026, 09:15 (sesión)

**Resumen para la bitácora (22-10-2026, hito 19, pasos 97 y 98: el backlog a Gherkin y a CSV)**

- **Contexto:** continúa la sesión del 20-10 (paso 96), otra vez a la vez que la que mide las pruebas reales del hito 18. Sin reconstruir contenedores ni lanzar trabajos, y con cada fichero añadido al commit uno a uno.
- **Hecho después del 20-10:** con permiso del autor y `GET /jobs/active` en 204, se reconstruyeron la API y la web y se comprobaron las descargas del catálogo en Docker (anotado en la entrada del 20-10). El autor comprobó en el navegador que el Word y el PDF se descargan bien, y se subieron a GitHub los commits pendientes, entre ellos los de la otra sesión.
- **Decisiones del autor (22-10):**
  - Gherkin con las palabras clave siempre en inglés y el texto tal cual (Claude recomendaba las del idioma del proyecto); todos los `.feature` en un `.zip`.
  - Un CSV por herramienta, Jira y Azure DevOps; en Azure DevOps, el proceso Agile.
- **Paso 97** (commit `c230161`):
  - `BacklogExport` arma una vez el contenido del backlog para los formatos de exportación;
  - `BacklogGherkin` escribe un `.feature` por épica: épica → `Feature`, historia → `Rule` con su narrativa y sus tareas, criterio → `Scenario`, con los ids como etiquetas. Cada fichero se comprueba con el analizador oficial (`Gherkin` 42.0.1, MIT) antes de entregarlo;
  - **descubierto al probar el analizador:** no rechaza nunca una línea de texto libre, pero `Scenario: …`, `Rule: …` o `Background: …` abren otro elemento, `@…` se vuelve etiqueta y `# …` desaparece como comentario. Esas líneas llevan «- » delante. `Parser.Parse(string)` toma el texto como una ruta de fichero: hay que pasarle un `StringReader`;
  - `GET /projects/{id}/backlog-gherkin.zip` y «Gherkin» en la barra del backlog. El nombre de fichero sin acentos pasa a Core (`ExportFiles.Slug`).
- **Paso 98:**
  - `BacklogCsv`: el CSV de Jira, con `Issue ID`/`Parent ID`, `Epic Name` y `Labels` repetidas, y el de Azure DevOps, con `Title 1/2/3`, `Acceptance Criteria` en HTML y `Tags`. Separados por comas, con todo entre comillas y en UTF-8 sin BOM;
  - en el HTML de Azure DevOps solo se escapan `&`, `<` y `>`: `WebUtility.HtmlEncode` convertía cada acento en una entidad numérica;
  - `GET /projects/{id}/backlog-jira.csv` y `backlog-azure-devops.csv`, y «CSV Jira» y «CSV Azure DevOps» en la barra del backlog.
- **Tests:** 910, todos correctos (5 de Gherkin y 2 de CSV nuevos, y las tres descargas en el flujo completo). Un test del runner (`Compacting_after_writing_the_output_stops_the_run_at_once`) falló una vez y pasó en tres repeticiones y en la batería completa: parece sensible al tiempo con la máquina cargada.
- **Pendientes:**
  - que el autor importe los dos CSV en un Jira y un Azure DevOps de verdad (hacen falta cuentas, por ejemplo las gratuitas): es lo único que falta para el «Hecho cuando» del hito 19;
  - probar las descargas del backlog en Docker cuando no haya tareas en marcha y el autor lo permita;
  - después, el hito 20.
- **Revisión tras el parón (petición del autor):** se comprobó el estado (árbol limpio, 910 tests correctos, `openapi.json` al día, la web sin avisos) y se releyó el código de los pasos 97 y 98. Aparecieron tres casos límite que no salen con lo que generan los modelos, pero sí con lo que puede escribir el usuario en un borrador:
  - el CSV de Jira de un backlog sin épicas daba un error 500 (`Max` de una lista vacía); ahora sale solo la cabecera;
  - un título con saltos de línea iba tal cual a `Summary` o `Title`, que no los admiten; ahora va en una línea;
  - un requisito con espacios («RF 001») daba una etiqueta de Jira no válida, y uno solo con espacios, una etiqueta `@` vacía que el analizador de Gherkin rechaza (otro error 500). `BacklogExport` limpia los ids una vez para todos los formatos.
  - 2 tests nuevos (912 en total).


## 22-10-2026, 09:50 (sesión)

**Resumen para la bitácora (22-10-2026, hito 18: backlog de la prueba real y error en la duración de los trabajos interrumpidos)**

- **Contexto:** sigue la prueba real del hito 18 en «Clinicas Veterinarias v0.11» (prompts v0.11, A y árbitro `SS - GPT-6-SOL`, B `SS - GPT-6-LUNA`). El autor lanzó el backlog desde la web y lo validó sin cambios; el mockup está en marcha. A la vez trabaja la otra sesión (hito 19), que ya hizo commit de lo suyo.
- **Backlog, resultados** (informe del trabajo):
  - A propuso 152 historias y B 56; la unión tiene 121 (52 de ambas, 68 solo de A, 1 solo de B) en 13 épicas, con 2 descartadas;
  - 17 discrepancias para revisar: 16 de la plataforma (desviaciones del árbitro respecto al emparejamiento) y 1 del árbitro. Con la v0.9, la plataforma habría marcado 20;
  - sin reparaciones; A, 5 turnos y 3,7 min; B, 5 turnos y 3,0 min; árbitro, 4,1 min;
  - el árbitro escribió el backlog y se detuvo al empezar a compactar, con la salida ya escrita y válida, y se aceptó (regla del 14-10-2026). Por eso no tiene dato de consumo: se estima en 243.000 tokens de entrada y 57.000 de salida;
  - emparejamiento: 7,1 s de reloj y 35,7 s de CPU;
  - coste: 447.072* tokens de entrada y 120.292* de salida, **1,67 $*** y 8 minutos, frente a 1,43 $* y 9 minutos con la v0.9 (* con la parte estimada).
- **Hallazgo: el árbitro recorta los criterios de aceptación en los backlogs grandes.** Contados en todos los backlogs arbitrados:
  - ReservaSalas: A 33, B 27, unión 34;
  - banco de alimentos: A 140, B 66, unión 98 (un 30 % menos que A);
  - clínicas, v0.9: A 193, B 97, unión 109 para 109 historias;
  - clínicas, v0.11: A 237, B 56, unión 121 para 121 historias.
  - En el proyecto grande queda un criterio por historia. Las tareas también quedan en una por historia, aunque ahí A y B ya proponían una. Explicación más probable: el árbitro resume para que todo el backlog quepa en una escritura.
  - No se cambia nada durante la prueba. Queda como **mejora prioritaria**: que el árbitro escriba el backlog por épicas, o que la plataforma cuente los criterios que se pierden y lo marque.
  - En palabras sencillas: con un backlog grande, el árbitro conserva las historias, pero de cada una deja solo una condición de «terminada», y se pierde detalle que A y B habían propuesto.
- **Error de medida encontrado al regenerar las tablas de coste (es del paso 91):**
  - `InterruptedJobs` cierra los trabajos que un reinicio dejó abiertos con la hora del arranque, y el informe medía la duración como «creado → cerrado». Cinco trabajos antiguos, del 13 al 16-10 (dos nuevos arbitrajes, dos mockups y un catálogo), contaban entre 74 y 149 horas; un mockup que trabajó 12 minutos, 101 horas. Los minutos de dos proyectos internos pasaron de 33 a 8.997 y de 1.317 a 14.713 en las tablas del 21-10;
  - **corrección:** un trabajo cerrado por `InterruptedJobs` (lo dice su mensaje) dura hasta la última ejecución que terminó antes del arranque; si no hay ninguna, no tiene duración. Los demás trabajos no cambian. `InterruptedJobs.Seconds` y un test nuevo (913 tests, todos correctos);
  - llega a la API en la próxima reconstrucción, que no se hace con el mockup en marcha; después se regeneran las tablas de coste. Hasta entonces, `docs/costes/costes.md` sigue con esos minutos inflados;
  - queda fuera un caso distinto, ya conocido: un mockup del 14-10 que dura 19 horas porque el equipo se suspendió por la noche con el trabajo abierto.
- **Otra observación de las tablas:** el tiempo del emparejamiento de los nuevos arbitrajes del 19-10, que se mide después (en cada petición del informe), salió ahora de 15,7 a 17,0 s, frente a 15,3–37,0 s en la regeneración anterior: depende de la carga del equipo en ese momento (ahora con el mockup en marcha). Las cifras medidas con el trabajo, las de los trabajos nuevos, no tienen ese problema.
- **Documentación:** resultados del backlog y hallazgo de los criterios en la evolución del prompt (v0.11) y en la trazabilidad; tablas de coste regeneradas con el backlog.
- **Pendientes:** medir y documentar el mockup en cuanto termine; reconstruir la API (con permiso del autor y sin tareas en marcha) y regenerar las tablas; el mockup sin `.zip` del banco de alimentos; cerrar el hito 18.

## 22-10-2026, 10:08 (sesión)

**Resumen para la bitácora (22-10-2026, hito 19 comprobado en Docker)**

- **Qué se hizo:** el autor subió a GitHub los commits de los pasos 97 y 98 y su revisión, reconstruyó la API y la web (`docker compose up --build -d api web`) y comprobó en el navegador que «Gherkin», «CSV Jira» y «CSV Azure DevOps» aparecen en la barra del backlog y descargan.
- **Decisiones:** ninguna nueva.
- **Pendientes:** importar los dos CSV en un Jira y un Azure DevOps de verdad (autor); después, el hito 20.

## 22-10-2026, 10:15 (sesión)

**Resumen para la bitácora (22-10-2026, hito 18: mockup de la prueba real)**

- **Mockup de «Clinicas Veterinarias v0.11»** (prompts v0.11, `SS - GPT-6-SOL` por la pasarela, sin `.zip`), lanzado por el autor tras validar el backlog:
  - 40 pantallas con las 121 historias del backlog validado; página de 1,74 MB (aviso a partir de 2 MB);
  - 41 ejecuciones (diseño y 40 pantallas), **todas válidas a la primera**: 0 reparaciones, 0 compactaciones, ninguna corrección `FRAGMENTO` y ninguna parte que corregir después de juntar las pantallas. Único aviso: `MAPA-PANTALLAS` (más de 25 pantallas). Entre 6 y 8 turnos por ejecución;
  - diseño: 157.645 tokens de entrada, 38.012 de salida, 9,6 min. Pantallas: mediana de 77.498 de entrada (de 68.937 a 244.353), 21.834 de salida (de 14.667 a 53.350) y 2,8 min (de 1,9 a 12,0), hasta 3 a la vez;
  - total: 3.696.191 tokens de entrada y 975.684 de salida, **17,15 $ y 52 minutos**, todos con dato real.
- **Comparación con la v0.9** (mismo proyecto, 15 y 16-10-2026): 38 pantallas con 109 historias, 1,54 MB, 15,46 $ y 73 minutos en dos trabajos, porque una pantalla falló por errores 504 de la pasarela y hubo que reanudar.
  - Por pantalla, 0,43 $ frente a 0,41 $; por historia, 0,14 $ en los dos. El coste sube porque hay más historias, no porque cada pantalla cueste más.
  - Esta vez no hubo fallos de la pasarela.
- **Estilo (paso 94):** al proyecto 33 le toca la dirección 1 de 8, «cálida y cercana». El `.html` la sigue: variables `--crema` y `--terracota`, títulos en serif (Georgia), esquinas de 26 px y navegación con pestañas. El autor: «el diseño del mockup tiene muy buena pinta».
- **Reconstrucción a las 10:05:** la API y la web se reiniciaron con una imagen nueva mientras se medía el mockup, ya terminado (`GET /jobs/active` en 204). No fue desde esta sesión; probablemente la otra, que tenía pendiente probar en Docker las descargas del backlog. La imagen lleva la corrección de las duraciones (commit `fdfb58a`): el mockup interrumpido que contaba 101 horas cuenta ahora 11,7 minutos.
- **Tablas de coste regeneradas** con el mockup y con las duraciones corregidas: los minutos del proyecto interno 1 pasan de 8.997 a 37, y los del 3, de 14.713 a 1.343. Lo que queda en el 3 es el mockup del 14-10, que siguió abierto mientras el equipo estaba suspendido por la noche: es una limitación ya conocida.
- **Siguiente prueba:** el mockup sin `.zip` del banco de alimentos (proyecto 32), al que le toca la dirección 0, «institucional y sobria», para comprobar que el estilo cambia de un proyecto a otro. Después, cerrar el hito 18.
- **Criterio de cierre del hito 18 en este proyecto:** el flujo completo terminó sin errores internos y todas las reparaciones cupieron. El único fallo, el primer catálogo, se debió al error del paso 92, corregido el 20-10.

## 22-10-2026, 10:15 (sesión)

**Resumen para la bitácora (22-10-2026, contraste de las exportaciones con la web)**

- **Qué se hizo:** se descargaron de la API (solo lectura, sin lanzar tareas) el catálogo y el backlog de los tres proyectos de ejemplo validados (clínicas, banco de alimentos y clínicas v0.11), con sus cinco exportaciones, y un script comparó cada fichero con lo que la API da a la web.
- **Resultado: sin diferencias.**
  - Word: los mismos ids en el mismo orden, todos los textos, tantas marcas de suposición y de discrepancia como en la web, y tantas fuentes y preguntas en los anexos como las citadas. Valida sin errores.
  - PDF: todos los ids y el pie «página N de M» en cada página (62, 28 y 67 páginas).
  - Gherkin: un fichero por épica que el analizador oficial acepta, una `Rule` por historia con sus etiquetas y un `Scenario` por criterio.
  - CSV: los mismos recuentos de épicas, historias y tareas, cada padre antes que sus hijos (Jira) y cada título en la columna de su nivel (Azure DevOps), sin BOM ni acentos convertidos en entidades.
- **Observación, no de la exportación:** en los dos backlogs de clínicas cada historia tiene exactamente un criterio y una tarea (109 y 121); en el banco de alimentos, 98 criterios y 88 tareas para 45 historias. Puede venir del prompt o del árbitro del backlog.
- **Pendientes:** importar los CSV en un Jira y un Azure DevOps de verdad (autor); después, el hito 20.

## 22-10-2026, 12:05 (sesión)

**Resumen para la bitácora (22-10-2026, hito 20, paso 99: trazabilidad)**

- **Qué se hizo:** la trazabilidad del catálogo y el backlog, en la API y en la web.
  - `Concilia.Core/Deliverables/Traceability`: cada requisito con sus fragmentos, sus preguntas y las historias que lo cubren; cada historia con su épica y sus requisitos; el texto de cada fragmento citado y cada pregunta con su respuesta, una vez cada uno; y lo citado que no existe (fragmento fuera de la documentación, `missingQuestions`, `missingRequirements`).
  - Se construye con `CatalogExport` y `BacklogExport`, los mismos del hito 19, para que el panel y el Word digan lo mismo. Para ello, `CatalogAsync` y `BacklogAsync` de `ExportEndpoints` pasan a `internal`.
  - `GET /projects/{id}/traceability`, que solo lee, documentado en OpenAPI (`docs/api/openapi.json` regenerado).
  - Web: botón «Trazabilidad» en cada requisito y cada historia abiertos, que abre `TracePanel` (sobre el `Drawer` existente). Dentro del panel se navega entre requisitos e historias con «Volver».
- **Decisiones (el autor las dejó a Claude):**
  - panel lateral navegable, en lugar de un bloque dentro del requisito, para ir de un requisito a una historia y a otro requisito sin cambiar de paso;
  - del catálogo y el backlog guardados, con el botón oculto mientras hay cambios sin guardar, como las descargas;
  - desde una historia, los fragmentos y preguntas de sus requisitos juntos y sin repetir, con los requisitos que citan cada uno;
  - sin vista de matriz de todo el catálogo por ahora, para que el paso sea pequeño;
  - sin librerías nuevas.
- **Tests:** 5 de `Traceability` y comprobaciones en el flujo completo (antes y después del backlog). 918 tests de C# en verde. La web compila con `tsc -b` y pasa `oxlint` sin avisos.
- **Sin probar:** el panel en el navegador y con los proyectos de ejemplo. Hace falta reconstruir la API y la web, y no se ha hecho para no interrumpir las pruebas reales del hito 18 que había en marcha.
- **Pendientes:** que el autor reconstruya y lo revise en el navegador; después, el paso 100 (comparación de A, B y el árbitro, con el emparejamiento guardado una sola vez por trabajo).

## 22-10-2026, 12:14 (sesión)

**Resumen para la bitácora (22-10-2026, hito 18 completado: mockup del banco de alimentos)**

- **Mockup de «Banco De Alimentos»** (proyecto 32, prompts v0.11, `SS - GPT-6-SOL` por la pasarela, sin `.zip`), lanzado por el autor con el mismo backlog validado de 45 historias que el mockup de la v0.9 del 16-10:
  - 24 pantallas; página de 0,68 MB;
  - 25 ejecuciones (diseño y 24 pantallas), **todas válidas a la primera**: 0 reparaciones, 0 compactaciones y ningún aviso ni corrección;
  - diseño: 94.669 tokens de entrada, 30.286 de salida, 5 turnos, 3,6 min. Pantallas: entrada de 58.379 a 74.250 (mediana 65.924), salida de 6.807 a 23.707 (mediana 15.792), de 1,1 a 2,7 min (mediana 2,0);
  - total: 1.666.670 tokens de entrada y 389.231 de salida, todos con dato real: **7,23 $ y 20 minutos**.
- **Comparación con la v0.9** (16-10): 20 pantallas, 6,48 $ y 36 minutos. Con la v0.11 hace 4 pantallas más con el mismo backlog; por pantalla, 0,30 $ frente a 0,32 $.
- **Estilo:** al proyecto 32 le toca la dirección 0, «institucional y sobria». El `.html` la sigue: menú lateral oscuro (`--lateral`, 244 px), acento azul petróleo (`--petroleo`), fondo gris claro, sans-serif del sistema, ninguna esquina de 12 px o más y 20 tablas. El de clínicas lleva crema, terracota, serif y esquinas de 26 px: **los dos estilos son claramente distintos.**
- **Hito 18 completado:** se cumplen las dos condiciones del «Hecho cuando», el flujo completo del proyecto grande sin errores internos ni reparaciones que no caben (clínicas) y el cambio de estilo sin `.zip` entre proyectos (clínicas y banco). Marcado en el plan, `CLAUDE.md`, el README, la trazabilidad y la evolución del prompt (v0.11).
- **Coste total de la prueba real de la v0.11 en clínicas:** 26,24 $* y 96 minutos (preguntas, sugerencias, dos catálogos, backlog y mockup; * con la parte estimada), más 7,23 $ del mockup del banco.
- **Tablas de coste regeneradas** con el mockup del banco.
- **Mejoras que deja la prueba** para la próxima versión de los prompts, sin cambiar nada ahora:
  - que el árbitro consulte menos documentación (explica el sobrecoste del catálogo);
  - que el árbitro del backlog no se quede con un criterio por historia en los backlogs grandes.
- **Pendientes:** decidir con el autor si esas dos mejoras van antes del hito 20 o después; el hito 20 lo lleva la otra sesión (paso 99 hecho).

## 22-10-2026, 13:33 (sesión)

**Resumen para la bitácora (22-10-2026, hito 18b, paso 107: el árbitro del catálogo consulta solo las fuentes que necesita)**

- **Decisión del autor:** abordar ya las dos mejoras que dejó la prueba real de la v0.11, antes de seguir con el hito 20, con el enfoque que propuso Claude («veo bien el enfoque»). Quedan como hito 18b, pasos 107 y 108, prompts v0.12. El autor pide que se le avise cuando terminen, para cerrar este chat.
- **Diagnóstico, con la consola guardada de la prueba:**
  - **catálogo:** el árbitro consultó 34 tramos de `documentacion.txt` (1.242 líneas), compactó, siguió leyendo y se paró en la segunda compactación; su repetición escribió la salida. Del emparejamiento salen 154 + 144 requisitos con par «mismo» (298 de 451) y 153 por decidir (126 de A y 27 de B);
  - **backlog:** en cuanto terminó de leer el catálogo, las dos propuestas y el emparejamiento (07:04:38), Claude Code intentó compactar y falló (`too_few_groups`): el contexto ya estaba lleno. Luego escribió en una sola respuesta los 120.444 caracteres del backlog (unos 36.000 tokens; 57.000 de salida estimados con el razonamiento, cerca del tope de 64.000), con exactamente un criterio y una tarea en cada una de las 121 historias.
- **Paso 107** (prompts v0.12):
  - `SourcesFile` escribe `input/fuentes.txt` con los fragmentos que citan los requisitos sin par «mismo» y los de los pares «mismo» cuyas cifras no coinciden (se comparan como conjunto, con «,» y «.» como la misma marca decimal), con la cabecera de cada documento y sus líneas «sigue». Sin emparejamiento, todos los requisitos necesitan su fuente;
  - el árbitro del catálogo recibe ese fichero en lugar de `documentacion.txt` (`PhaseTask.DocumentationOf`); el de las preguntas no cambia;
  - el número de línea que un modelo cita como fragmento se busca en el fichero que ve: el contexto lleva `LinesFile`, y la pista de `REF-FRAG` y la corrección `REF-LINEA` usan `fuentes.txt` en ese árbitro;
  - `arbitro_catalogo.md`: el §1 describe `fuentes.txt` y la discrepancia se comprueba en las fuentes. `PromptVersion` pasa a v0.12;
  - tests: `SourcesFileTests` (9 casos) y uno nuevo en `PhaseTaskTests`; 929 en total, todos correctos, ejecutados en un worktree limpio desde `HEAD` con solo estos cambios, porque la otra sesión tiene a medias el paso 100 (`FullFlowTests.cs` no compila en la carpeta compartida).
- **Pendientes:** el paso 108; reconstruir la API (con permiso del autor y sin tareas en marcha, y cuando la otra sesión haya hecho commit del paso 100) y medir los dos pasos con un nuevo arbitraje del catálogo y del backlog de «Clinicas Veterinarias v0.11».

## 22-10-2026, 13:35 (sesión)

**Resumen para la bitácora (22-10-2026, hito 20, paso 100: comparación de A, B y el árbitro)**

- **Comprobación del paso 99 en Docker:** el autor reconstruyó la API y la web. Con peticiones de solo lectura, la trazabilidad de los 7 proyectos coincide con el catálogo, el backlog y las preguntas que recibe la web, sin fragmentos sin texto ni referencias rotas, y responde en 50–190 ms. De 1.537 fragmentos citados, 1.530 aparecen tal cual en el texto de su documento; los otros 7 llevan `<` y `>` cambiados por `＜` y `＞`, que es lo que hace a propósito `Fragmenter.SafeText`. El autor lo revisó en el navegador: «todo funciona genial».
- **Qué se hizo en el paso 100:**
  - **El emparejamiento, una sola vez por trabajo.** `ProposalMatcher.Match` se divide en `Similarities` (la parte con embeddings: cada elemento con el más parecido de la otra propuesta y su similitud) y `Classify` (umbrales, recuentos, pares y acuerdo con el árbitro). La primera se guarda en `data/jobs/{id}/emparejamiento.json` (`MatchingCache`), con el nombre del modelo de embeddings (`ITextEmbedder.Name`, nuevo) y una versión; si cambian, se recalcula. Los trabajos nuevos la guardan al arbitrar (`PhaseJobResult.Similarities`, escrita con el resultado), y los antiguos, la primera vez que se pide (`JobMatching`). `GET /jobs/{id}/matching` la usa también, con cualquier umbral, y devuelve `fromCache`.
  - **Informe y CSV:** `embeddings` en el informe (solo A, solo B y el acuerdo con el árbitro) y una línea más en la web. En el CSV, `emb_solo_A`, `emb_solo_B` y `emb_acuerdo_arbitro_pct` al final, para no mover las columnas que ya había. El acuerdo va en %: el nombre lo dice, aunque en la propuesta se llamaba `emb_acuerdo_arbitro`.
  - **Comparación:** `GET /jobs/{id}/comparison` (`JobComparison`), documentado en OpenAPI. Da una fila por elemento de la unión, una por descartado (con su motivo) y una por elemento que el árbitro no clasificó. Cada elemento de A y de B lleva su par más parecido en la otra propuesta, su nivel y si el árbitro lo juntó, y la fila dice si el árbitro coincide con los embeddings. En la web, «Comparación de A, B y el árbitro» bajo el informe de cada trabajo con árbitro (`JobComparison.tsx`): tres columnas, filtros, búsqueda y filas de 50 en 50. Se carga al abrirla.
- **Decisiones del autor, todas como recomendaba Claude:** guardar solo la parte lenta, sin umbrales; tres columnas en el CSV; una fila por elemento de la unión.
- **Criterio:** un elemento descartado que tiene un «mismo» en la otra propuesta cuenta como «no coincide con los embeddings», igual que en el acuerdo del paso 87, aunque descartar un repetido sea correcto. Así la vista y la cifra del informe cuentan lo mismo.
- **Tests:** 3 de la caché (mismo resultado que calcular de nuevo, otros umbrales sin embeddings, otro modelo o fichero dañado), 5 de `JobComparison` y comprobaciones en el flujo completo (fichero guardado al arbitrar, `fromCache`, `embeddings` en el informe, filas de la comparación y columnas del CSV). 937 tests de C# en verde (con los cambios de la otra sesión en el árbol). La web pasa `tsc -b` y `oxlint` sin avisos.
- **Sin probar todavía:** la comparación en el navegador y con los trabajos reales; hace falta reconstruir la API y la web. La primera descarga del CSV de un proyecto con muchos trabajos tardará unos segundos por trabajo, mientras guarda su emparejamiento.
- **Pendientes:** que el autor reconstruya y revise la comparación; después, el hito 21.

## 22-10-2026, 13:40 (sesión)

**Resumen para la bitácora (22-10-2026, hito 18b, paso 108: el árbitro del backlog cita los criterios y las tareas por su id)**

- **Paso 108** (prompts v0.12, decisión del autor como recomendaba Claude):
  - `ProposalIds.Prefix` pone también el número de propuesta a los criterios y las tareas del backlog (`1:CA-012`, `2:T-030`);
  - `arbitro_backlog.md`: el §2 pide conservar todos los criterios y tareas de las historias que se unen, salvo los repetidos, citar por su id los que se conservan tal cual y escribir enteros solo los combinados o nuevos; el §4 muestra el formato (`"criterios": ["1:CA-012", {…}]`);
  - `CriteriaReferences` (antes de comprobar y en lo que se guarda, como `PlatformFixes`) pone el texto de los citados, numera de nuevo `CA-` y `T-` en todo el backlog y deja un aviso `ARB-CITA` con lo citado, lo escrito entero y lo que no se cita de las historias unidas. Un id que no está en su propuesta, o sin el número delante, es un error `ARB-CITA` y se quita, para que el esquema no lo cuente otra vez;
  - el formato del backlog que se guarda no cambia: la web, las exportaciones y el informe siguen igual.
- **Tests:** `CriteriaReferencesTests` (6) y uno de `PhaseTaskTests` cambiado (los criterios y las tareas sí llevan ahora el prefijo); 943 en total, todos correctos, en el worktree limpio desde `HEAD` con estos cambios.
- **Pendientes:** reconstruir la API cuando la otra sesión haya hecho commit del paso 100 y no haya tareas en marcha, con permiso del autor; medir los pasos 107 y 108 con un nuevo arbitraje del catálogo y otro del backlog de «Clinicas Veterinarias v0.11»; documentar los resultados y cerrar el hito 18b.
