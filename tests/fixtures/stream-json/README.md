# Fixtures de stream-json

Salidas de `claude -p --output-format stream-json --verbose` que usan los tests del parser y del runner. Git las guarda byte a byte (`.gitattributes`): conservan los saltos de línea CRLF con los que se grabaron.

## Capturas reales

Grabadas por el autor contra la pasarela LiteLLM, con el ejemplo ficticio ReservaSalas.

| Archivo | Contenido |
|---|---|
| `tools-sol.jsonl` | `SS - GPT-6-SOL` con herramientas: Read, Write y veredicto `hecho` (3 turnos). |
| `tools-luna.jsonl` | `SS - GPT-6-LUNA` con herramientas: Read, Glob, Write y veredicto `hecho` (4 turnos). |
| `sin-herramientas.jsonl` | `SS - GPT-6-SOL` sin herramientas: respuesta de texto con tildes y raya (`—`) bien codificadas. |
| `error-modelo-inexistente.jsonl` | Modelo que no existe: la pasarela responde 403. `is_error` es `true`, `terminal_reason` es `api_error` y, aun así, `subtype` es `success`. |

Saneado:

- El autor sustituyó los identificadores (`session_id`, `uuid`, `id`) por valores `00000000-…` y la carpeta de trabajo por `/work`.
- En las copias del repositorio se normalizaron además `memory_paths` y `messaging_socket_path`.
- No contienen la clave ni la dirección de la pasarela.

`tools-sol.jsonl` y `tools-luna.jsonl` traen `ÔÇö` en lugar de `—` dentro de un `tool_result`. Viene de cómo se guardó la salida desde la consola de Windows, no de Claude Code. Ningún test depende de ese carácter.

## Fixtures sintéticos

Derivados de `tools-sol.jsonl` cambiando solo lo indicado.

| Archivo | Cambio |
|---|---|
| `synthetic-sin-veredicto.jsonl` | El texto final no tiene línea `VEREDICTO:`. |
| `synthetic-veredicto-invalido.jsonl` | `VEREDICTO: {estado: hecho` (JSON inválido). |
| `synthetic-bloqueada.jsonl` | Veredicto `bloqueada` con motivo. |
| `synthetic-cortada.jsonl` | Salida cortada tras el `tool_result` del Write: no hay texto final ni evento `result`. |
| `synthetic-reintento-api.jsonl` | Añade una línea `system` con subtipo `api_retry` tras `init`. **La forma de esa línea es una suposición**: no hay captura real de un reintento. |
