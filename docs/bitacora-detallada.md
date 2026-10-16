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
