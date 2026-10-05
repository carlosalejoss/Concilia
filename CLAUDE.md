# ReqArbiter

Plataforma web (TFG) que genera catálogos de requisitos (EARS/MASTeR) y backlogs ágiles con verificación cruzada: dos modelos de IA trabajan cada tarea en paralelo y un árbitro los compara y fusiona. Lee `README.md` y `docs/plan-de-construccion.md` antes de empezar.

## Estado

Hitos 0 a 5 completados. Hito 6 (catálogo v1.3) en curso: ver `docs/hito-6-catalogo-v1.3.md`. Se trabaja siguiendo `docs/plan-de-construccion.md`, un paso cada vez. El nombre del proyecto es provisional.

## Decisiones vigentes

- Stack: React 19 + TypeScript + Vite + Tailwind; .NET 10 (ASP.NET Core y EF Core); PostgreSQL; todo en Docker.
- Patrón: cada tarea se lanza a Modelo A y Modelo B en paralelo; un Árbitro compara, pregunta al usuario si hay discrepancias graves y fusiona. Se aplica a tres fases: dudas, catálogo de requisitos y backlog.
- Modelos: solo a través de la pasarela LiteLLM de la empresa. Ids exactos: `SS - GPT-6-SOL` y `SS - GPT-6-LUNA` (llevan prefijo y espacios). El reparto de roles es configuración.
- Runner: proceso hijo `claude -p`, con el prompt por entrada estándar, `--output-format stream-json --verbose` y veredicto final obligatorio (`VEREDICTO: {...}`). Proveedores: `LiteLlm` y `Fake`. No existe proveedor de suscripción personal ni debe crearse.
- Notación: EARS por defecto; MASTeR para requisitos no funcionales o complejos y para expresar obligatoriedad (shall, should, may).
- Árbitro: el emparejamiento por embeddings se calcula fuera del LLM; el LLM explica y fusiona. La pasarela no ofrece modelos de embeddings.
- Backlog: épica → historia → tarea por defecto; falta decidir si "feature" es un nivel propio.
- Catálogo: el modelo escribe `output/catalogo.json` con Write y termina con `VEREDICTO:`; el texto de cada requisito lo genera la plataforma a partir de los slots (D-23).
- La documentación se divide en fragmentos con id estable (`DOC-01#F001`) y en lotes que respetan el tope de salida (D-24). Un lote agrupa secciones completas, de uno o varios documentos.
- Ejecución de un trabajo: A y B en paralelo y, dentro de cada modelo, hasta 5 lotes a la vez (`Catalog:MaxParallelLots`; con 1, un lote detrás de otro). Sustituye a la ejecución de los lotes en orden del hito 4.
- Reparación del catálogo (paso 27, catálogo v1.2; sustituida en el paso 31): si falta `output/catalogo.json`, no es JSON, tiene errores o el `Write` llegó cortado, se reintenta con el prompt original más el anexo de reparación (catálogo previo y un error por línea). Los avisos, `bloqueada`, los tiempos agotados y los errores de la API no se reintentan. Máximo dos reintentos por lote y modelo, contando los del veredicto.
- El runner detiene la ejecución en cuanto Claude Code rechaza una llamada a herramienta por JSON incompleto, en lugar de dejar que el modelo la repita hasta agotar el tiempo.
- Catálogo de un modelo en un trabajo (paso 28, catálogo v1.2; actualizado en el paso 31): se unen sus lotes `Done` en orden, renumerando `REQ-` y `Q-` y sus referencias, sin deduplicar (lo hará el árbitro). Se sirve en `GET /jobs/{id}/catalogs/{A|B}` y, cuando terminan todos sus lotes, se guarda en `data/jobs/{id}/catalogo-{A|B}.json`.
- Con `Fake`, `FakeProvider` escribe catálogos reales: el del ejemplo ReservaSalas si se sube su `DOC-01.txt` y, para cualquier otro documento, uno válido que descarta todos los fragmentos. `RUNNER_SIMULATE_REPAIR=true` hace inválido el primer intento de cada lote.
- Independencia de A y B (1-10-2026): el prompt no menciona al otro modelo ni al árbitro; cada modelo construye su catálogo como si fuera el único. Sustituye al criterio de la v1.2 de que "ambos modelos corten igual". El árbitro empareja de varios a varios.
- Tipos de requisito con id por tipo: `RA` (arquitectura), `RT` (técnico), `RF` (funcional), `RI` (integración) y `RNF` (no funcional): `RF-001`, `RNF-001`… La prioridad (P0–P3) queda fuera por ahora.
- Catálogo v1.3 (hito 6, en curso): un requisito por capacidad con criterios de aceptación; descartes `design_detail` y `project_note`; normalizador de la plataforma; rechazo por requisito con aceptación parcial del lote; una reparación; deduplicación por modelo. EARS y MASTeR se mantienen.
- Catálogo v1.3, cobertura primero (2-10-2026): que no falte ninguna funcionalidad pesa más que el detalle. Cada modelo escribe su propio inventario de funcionalidades (`capabilities`) antes de los requisitos, y cada `RF`/`RI` indica cuáles cubre (`capability_ids`). Los errores que describe la fuente son criterios `[ERROR]` de su funcionalidad, no requisitos propios. Los criterios llevan etiqueta: `OPERATION`, `DATA`, `RULE`, `LIMIT`, `MESSAGE`, `ERROR`. Una funcionalidad del inventario sin requisito aceptado (C20) provoca la reparación del lote. Sin verbos paraguas con cuatro operaciones o menos (U7).
- Catálogo v1.3 en la plataforma (paso 31): prompt `req-1.3` con `{{INDICE}}` (el índice de todos los documentos va en el plan del trabajo). Cada intento de un lote se valida en tres niveles; el catálogo aceptado (normalizado, sin rechazados, renumerado) se guarda en `output/catalogo_aceptado.json`. Se repara solo por errores del lote, más del 20 % de requisitos rechazados o C20, como mucho `Catalog:MaxRepairs` veces (1 por defecto). Un catálogo válido deja el intento `Done` aunque pida reparación o falle la línea de veredicto (no se repite el lote por esa línea). La unión del trabajo usa el último intento `Done` de cada lote y renumera por tipo (`RF-`, `RNF-`, `RA-`, `RT-`, `RI-`), `Q-` y `CAP-`.
- Prompt de requisitos vigente: `req-1.4` (05-10-2026), con el esquema 1.3. Los cambios de cada versión y sus resultados están en `docs/evolucion-prompt-requisitos.md`.
- Los scripts de `prompts/generacion_requisitos/` son la referencia del fragmentador y del validador en C#.
- Validación de JSON Schema en C#: LateApexEarlySpeed.Json.Schema (BSD-3-Clause). Se descarta JsonSchema.Net: sus binarios exigen una cuota de mantenimiento a empresas con ingresos.
- Ventana de los modelos (confirmada por el team leader): 128k tokens de entrada y hasta 1M de salida. El runner pasa `CLAUDE_CODE_MAX_CONTEXT_TOKENS=128000`.

## Decisiones abiertas (no las cierres sin preguntar)

Origen de los embeddings, jerarquía del backlog, qué modelo hace de Árbitro y umbral de "discrepancia grave".

## Runner y pasarela (verificado)

- `ANTHROPIC_BASE_URL` es la dirección de la pasarela sin el `/v1` final, y `ANTHROPIC_AUTH_TOKEN` es la clave de la pasarela.
- Cada ejecución usa su propio `CLAUDE_CONFIG_DIR` vacío y un entorno construido de forma explícita: no se heredan variables del usuario ni `ANTHROPIC_API_KEY`.
- Las herramientas se permiten con `--allowedTools "Read,Write"`. Los dos modelos ejecutan herramientas correctamente.
- `--allowedTools` no limita las herramientas disponibles: en una ejecución real del hito 4, SOL usó `Bash` (Claude Code aprueba solo los comandos de lectura) e intentó el skill `claude-api`, siguiendo instrucciones del prompt de sistema de Claude Code. Desde entonces el runner pasa también `--tools "Read,Write"` (herramientas que existen) y `--disable-slash-commands` (sin skills). Verificado con SOL y LUNA: solo usan Read y Write.
- El aviso `unrecognized_model` sale por stderr y no es un error. Claude Code asume 200k de contexto y 32k de salida.
- Con `CLAUDE_CODE_MAX_CONTEXT_TOKENS=128000` Claude Code asume 128k de contexto (verificado en el hito 3 con Claude Code 2.1.283: `modelUsage.contextWindow` = 128000). La salida máxima sigue en 32k.
- Ejecución real desde la web verificada con `SS - GPT-6-SOL` y `SS - GPT-6-LUNA`: veredicto `hecho` y `resumen.md` escrito.
- El evento `result` final trae `is_error`, `terminal_reason`, `num_turns`, `result` (con el veredicto) y el uso de tokens. Su `total_cost_usd` no es fiable.
- Cada ejecución arrastra unos 15.000 tokens de contexto base.
- El razonamiento del modelo cuenta en el tope de salida. En la primera prueba real del catálogo (hito 5), LUNA gastó los 32.000 tokens en cada respuesta (unos 28.000 razonando) y su `Write` llegó cortado, con JSON imposible de leer; tras dos intentos agotó los 30 minutos. SOL usó 23.000 y terminó. Desde entonces el runner pasa `CLAUDE_CODE_MAX_OUTPUT_TOKENS` (`Runner:MaxOutputTokens`, 64.000 por defecto). En el código de Claude Code 2.1.283, para un modelo que no reconoce, la salida vale 32.000 por defecto y la variable se acepta hasta 128.000 (por encima, se recorta a 128.000). El `maxOutputTokens` de `modelUsage` es solo el valor por defecto, no el aplicado. Verificado con la pasarela el 30-09-2026: un lote de SOL terminó `Done` con 41.880 tokens de salida, por encima de los 32.000.
- Catálogo real verificado (hito 5) con el `DOC-01.txt` de ReservaSalas: SOL y LUNA terminan en un lote, sin reparación, con catálogos válidos de 13 requisitos y 3 preguntas abiertas y métricas al 100 %. SOL: 34.145 tokens de entrada y 5.591 de salida; LUNA: 34.085 y 8.115.
- Con `--tools "Read,Write"` y `--disable-slash-commands` el consumo baja mucho: la misma tarea de resumen pasó de 59.000–89.000 a unos 21.400 tokens de entrada (3 turnos), también en el modelo que no usaba herramientas de más.

## Secretos y datos

- Nunca escribas claves, tokens ni direcciones internas en código, documentación, tests ni mensajes de commit. Todo va por variables de entorno; el repo solo lleva `.env.example` con nombres y sin valores.
- No leas ni imprimas `.env` ni variables de entorno con credenciales.
- Usa solo datos ficticios o públicos (por ejemplo, el ejemplo ReservaSalas y los datasets públicos). Nunca documentos reales de la empresa, salvo autorización expresa del autor.

## Reglas de seguridad para Claude Code

Copiadas de `docs/hito-3-runner.md`.

- No ejecutes comandos que impriman variables de entorno, ni siquiera las de un contenedor: `docker compose config`, `docker inspect`, `docker exec ... env`, `printenv`, `Get-ChildItem Env:`.
- No leas `.env`. El repo solo lleva `.env.example`, con nombres y sin valores.
- Ningún test debe llamar a la pasarela.
- Los logs, eventos y filas de base de datos nunca contienen la clave ni la dirección de la pasarela (ver `SecretRedactor`).

## Convenciones

- Código, identificadores y commits en inglés (Conventional Commits: `feat:`, `fix:`, `docs:`, `test:`, `chore:`). Documentación y memoria en español.
- Un commit por paso del plan. No hagas commit de cambios que no compilen o cuyos tests fallen.
- No hagas `git push` ni cambies la configuración de git salvo que se pida.
- Crea carpetas y proyectos solo cuando el paso lo requiera; no montes el árbol completo por adelantado.
- Prefiere lo simple: sin autenticación de usuarios y sin servicios adicionales que el plan no pida.
- Añade tests cuando el paso los pida, en especial para el parser del runner y el validador del catálogo.

## Cómo trabajar

- Haz un paso del plan cada vez y explica en pocas líneas qué has cambiado y por qué.
- Si un paso exige una decisión abierta, para y pregunta.
- Si algo del plan choca con este archivo, dilo antes de aplicarlo.
- El autor debe poder explicar cada línea en su defensa del TFG: prefiere código claro y sin trucos, y explica lo que no sea evidente.
- Siempre que cambie el prompt de requisitos (su texto, el anexo de reparación, el esquema o el validador que lo acompaña) o se mida una versión con modelos, actualiza `docs/evolucion-prompt-requisitos.md` en el mismo commit: sección de la versión con **Qué hace**, **Resultados** y **Qué hay que mejorar**, y su fila en el resumen. Resultados con las métricas de "Cómo se mide", fecha, documento y modelos; de documentos internos, solo cifras agregadas y anonimizadas. Es material para la memoria del TFG.

## Al terminar cada sesión

Imprime un resumen para la bitácora del TFG: qué se hizo, decisiones tomadas, cambios de criterio respecto a decisiones anteriores (sin borrar las anteriores) y pendientes. El autor lo llevará al proyecto de Claude donde se mantiene la bitácora.