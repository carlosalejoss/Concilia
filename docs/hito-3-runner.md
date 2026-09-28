# Hito 3: runner y primera ejecución desde la web

Cubre los pasos 16 a 22 de `docs/plan-de-construccion.md`. Léelo junto con `CLAUDE.md`. Esta especificación completa el plan; si choca con él o con `CLAUDE.md`, dilo antes de aplicarla. Los valores marcados como "por defecto" son propuestas configurables, no decisiones cerradas.

## Objetivo

Desde la web, pulsar "Ejecutar" en un proyecto lanza una tarea trivial (resumir los archivos del proyecto) contra un modelo de la pasarela, muestra el progreso en vivo y presenta el resultado.

## Reparto de trabajo

- **Claude Code** escribe el código y los tests. Sus pruebas usan `FakeProvider` y un `claude` falso. Nunca ejecutes nada contra la pasarela real.
- **El autor** prueba el `LiteLlmProvider` real en su propia terminal, con un `.env` que tú no lees.
- Para el paso 20 el autor aportará la versión exacta de `claude --version`.

## Reglas de seguridad para Claude Code

Copia estas reglas a `CLAUDE.md`:

- No ejecutes comandos que impriman variables de entorno, ni siquiera las de un contenedor: `docker compose config`, `docker inspect`, `docker exec ... env`, `printenv`, `Get-ChildItem Env:`.
- No leas `.env`. El repo solo lleva `.env.example`, con nombres y sin valores.
- Ningún test debe llamar a la pasarela.
- Los logs, eventos y filas de base de datos nunca contienen la clave ni la dirección de la pasarela (ver `SecretRedactor`).

## Paso 16: contrato del runner

`IRunnerProvider` con una operación asíncrona que recibe un `RunRequest`, publica eventos de progreso y devuelve un `RunResult`. Proveedores: `LiteLlm` y `Fake`. No crees un proveedor de cuota.

`RunRequest`:

| Campo | Notas |
|---|---|
| `RunId` | Identificador de la ejecución |
| `ModelId` | Literal de la configuración, por ejemplo `SS - GPT-6-SOL` (lleva espacios y prefijo) |
| `Prompt` | Texto que se envía por entrada estándar |
| `WorkDirectory` | Carpeta de trabajo de la ejecución |
| `ExpectedOutputFiles` | Archivos que el modelo debe dejar |
| `AllowedTools` | Por defecto `Read` y `Write` |
| `MaxTurns` | Por defecto 10 |
| `HeartbeatTimeout` | Por defecto 120 s |
| `TotalTimeout` | Por defecto 10 min |
| `MaxContextTokens` | Por defecto 128000; se pasa como `CLAUDE_CODE_MAX_CONTEXT_TOKENS` |
| `InputTokenBudget` | Por defecto 80000 tokens estimados de entrada |

`RunResult`:

| Campo | Notas |
|---|---|
| `Status` | `Done`, `Blocked`, `Failed`, `TimedOut` o `HeartbeatLost` |
| `Verdict` | Estado (`hecho` o `bloqueada`) y motivo, si lo hay |
| `ResultText` | Texto final del modelo |
| `OutputFiles` | Archivos de salida comprobados |
| `NumTurns`, `DurationMs`, `TerminalReason`, `IsError` | Del evento `result` |
| `Usage` | Tokens de entrada y salida |
| `Warnings` | Líneas de stderr y líneas no JSON |

No tomes decisiones con `total_cost_usd`: Claude Code lo calcula para un modelo que no conoce y no es fiable.

## Paso 17: parser de stream-json

- Una línea de stdout es un objeto JSON. Usa `JsonDocument`. No dependas del orden de las claves: en `result`, `type` no es la primera.
- Ignora tipos y campos desconocidos. Tolera CRLF y líneas vacías. Una línea que no sea JSON se registra como aviso y no rompe la ejecución.
- Tipos observados en las capturas reales: `system` (subtipo `init`, con modelo y herramientas), `assistant` (bloques `text` y `tool_use`), `user` (bloques `tool_result`) y `result`. Puede aparecer también un evento de reintento de la API: trátalo como progreso, no como fallo.
- Campos de `result` que se leen: `is_error`, `subtype`, `terminal_reason`, `num_turns`, `result`, `usage`, `modelUsage`, `duration_ms`, `session_id`, `permission_denials` y `api_error_status`.
- Fixtures reales en `tests/fixtures/stream-json/`: `tools-sol.jsonl` (Read, Write) y `tools-luna.jsonl` (Read, Glob, Write). Traen CRLF y un carácter corrupto (`ÔÇö`) por cómo se grabaron; no hagas depender ningún test de ese carácter.
- Fixtures sintéticos con prefijo `synthetic-`, derivados de los reales: sin veredicto, veredicto con JSON inválido, veredicto `bloqueada`, `is_error` en `true` y salida cortada sin evento `result`.
- Pendiente del autor: capturas reales `error-*.jsonl` y `sin-herramientas.jsonl`.

## Paso 18: lanzamiento del proceso

- El ejecutable es configurable (`Runner:ClaudeExecutable`): `claude` en Linux y Docker, `claude.cmd` en Windows.
- Los argumentos se pasan con `ProcessStartInfo.ArgumentList`, nunca concatenando una cadena. El id del modelo contiene espacios y debe llegar como un único argumento tras `--model`. Argumentos: `-p`, `--model`, `--output-format stream-json`, `--verbose`, `--allowedTools` y `--max-turns`.
- El prompt se escribe por stdin en UTF-8 sin BOM y se cierra la entrada.
- `StandardOutputEncoding` y `StandardErrorEncoding` en UTF-8 explícito.
- Lee stdout y stderr a la vez y por separado. El aviso `[claude-code:unrecognized_model] ...` sale por stderr: es un aviso, no un error.
- Directorio de trabajo: la carpeta de la ejecución, dentro de `data/runs/{runId}/`, con `input/` y `output/`.
- El entorno del proceso hijo se construye de forma explícita, sin heredar el del proceso padre. Solo lleva:
  - las variables mínimas para que el proceso arranque (`PATH` y las del sistema que haga falta);
  - `ANTHROPIC_BASE_URL`, que es la dirección de la pasarela sin el `/v1` final;
  - `ANTHROPIC_AUTH_TOKEN`, que es la clave de la pasarela;
  - `CLAUDE_CONFIG_DIR`, apuntando a una carpeta propia y vacía dentro de la carpeta de la ejecución;
  - `CLAUDE_CODE_MAX_CONTEXT_TOKENS`, con el valor de `Runner:MaxContextTokens` (por defecto 128000, mientras se aclara la ventana real de los modelos).
  Nunca pases `ANTHROPIC_API_KEY` ni credenciales del usuario.
- Latido: cualquier línea de stdout lo reinicia. Sin líneas durante `HeartbeatTimeout`, mata el árbol de procesos y devuelve `HeartbeatLost`. Al agotar `TotalTimeout`, `TimedOut`. Una cancelación también mata el árbol.
- `SecretRedactor`: sustituye el token y el host de la pasarela por `***` en cada línea antes de registrarla, guardarla o emitirla. Pruébalo con un secreto falso.

- Presupuesto de entrada: antes de lanzar una ejecución, estima los tokens de entrada (aproximadamente caracteres divididos entre 3,5). Si superan `InputTokenBudget`, no la lances y devuelve un error claro. Es necesario porque el límite de 10 MB de las subidas no protege frente a un desbordamiento de contexto. Añade tests para ambos casos.

## Paso 19: veredicto

- El prompt exige que el texto final termine con una línea `VEREDICTO: {"estado":"hecho"}` o `VEREDICTO: {"estado":"bloqueada","motivo":"..."}`.
- Se busca en `result.result`, en la última línea que empiece por `VEREDICTO:`. Si falta, el JSON es inválido o el estado es desconocido, el veredicto no es válido y no se da nada por bueno.
- Una ejecución es correcta si el proceso termina bien, `is_error` es `false`, `terminal_reason` es `completed`, el veredicto es `hecho` y existen los archivos de salida esperados.
- Con veredicto ausente o inválido se reintenta como máximo dos veces, añadiendo al prompt el motivo del fallo. Cada reintento es una ejecución nueva enlazada a la anterior.
- Tests con los fixtures sintéticos del paso 17. Para el proceso real, usa un pequeño ejecutable `FakeClaude` (consola .NET multiplataforma) que imprima líneas de un fixture con retardos, códigos de salida y mensajes por stderr configurables. Así se prueban latido, tiempo máximo y códigos de salida sin Claude Code ni red.

## Paso 20: `LiteLlmProvider`

- Configuración desde las variables de entorno `LITELLM_BASE_URL` y `LITELLM_API_KEY`. Si faltan, la aplicación falla al arrancar con el proveedor `LiteLlm`; con `Fake` funciona sin ellas.
- Imagen de la API: Node LTS y `@anthropic-ai/claude-code` con la versión fijada que dará el autor, usuario sin privilegios y `HOME` escribible.
- `docker-compose.yml` pasa esas dos variables desde `.env` solo al servicio `api`.
- Configuración de modelos: `SS - GPT-6-SOL` y `SS - GPT-6-LUNA`. Para este hito basta un modelo configurable, por defecto SOL.
- Claude Code escribe el código, pero no lo ejecuta contra la pasarela: la prueba real la hace el autor.

## Pasos 21 y 22: API y web

- Entidad `Run` con migración: id, proyecto, proveedor, modelo, estado, inicio, fin, estado y motivo del veredicto, texto del resultado, tokens de entrada y salida, número de turnos y mensaje de error. No guardes secretos ni `total_cost_usd`.
- `POST /projects/{id}/runs` lanza la ejecución en segundo plano (`BackgroundService` y `Channel<T>`) y devuelve 202 con el id. La tarea es resumir el texto extraído de los archivos del proyecto en `output/resumen.md`.
- `GET /projects/{id}/runs`, `GET /runs/{id}` y `GET /runs/{id}/events` (SSE). Los eventos son resúmenes saneados: tipo y texto corto, con los `tool_result` recortados.
- Web: botón "Ejecutar", lista de ejecuciones con estado, consola de eventos en vivo y vista del resultado. Sin pulido visual.
- Selección de proveedor por configuración (`Fake` o `LiteLlm`). `Fake` por defecto en desarrollo sin secretos y en tests.

## Definición de hecho

- Con `FakeProvider`: el flujo completo funciona en el navegador con `docker compose up`.
- Con `LiteLlm` (lo prueba el autor): una ejecución con SOL y otra con LUNA terminan con veredicto `hecho` y con `resumen.md`.

## Pendientes del autor

- Grabar `error-*.jsonl` y `sin-herramientas.jsonl`, antes del paso 19.
- Dar la versión de `claude --version`, antes del paso 20.
- Crear `.env` con los valores reales, antes de probar el paso 20.
- Aclarar con el team leader si la ventana de contexto es de 128k y el máximo de salida de 1M, o al revés. Hasta entonces se usan 128k.
