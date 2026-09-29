# ReqArbiter

Plataforma web (TFG) que genera catálogos de requisitos (EARS/MASTeR) y backlogs ágiles con verificación cruzada: dos modelos de IA trabajan cada tarea en paralelo y un árbitro los compara y fusiona. Lee `README.md` y `docs/plan-de-construccion.md` antes de empezar.

## Estado

Hitos 0 a 4 completados. Se trabaja siguiendo `docs/plan-de-construccion.md`, un paso cada vez. El nombre del proyecto es provisional.

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

## Al terminar cada sesión

Imprime un resumen para la bitácora del TFG: qué se hizo, decisiones tomadas, cambios de criterio respecto a decisiones anteriores (sin borrar las anteriores) y pendientes. El autor lo llevará al proyecto de Claude donde se mantiene la bitácora.