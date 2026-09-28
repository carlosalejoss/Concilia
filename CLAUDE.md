# ReqArbiter

Plataforma web (TFG) que genera catálogos de requisitos (EARS/MASTeR) y backlogs ágiles con verificación cruzada: dos modelos de IA trabajan cada tarea en paralelo y un árbitro los compara y fusiona. Lee `README.md` y `docs/plan-de-construccion.md` antes de empezar.

## Estado

Hitos 0, 1 y 2 completados. Se trabaja siguiendo `docs/plan-de-construccion.md`, un paso cada vez. El nombre del proyecto es provisional.

## Decisiones vigentes

- Stack: React 19 + TypeScript + Vite + Tailwind; .NET 10 (ASP.NET Core y EF Core); PostgreSQL; todo en Docker.
- Patrón: cada tarea se lanza a Modelo A y Modelo B en paralelo; un Árbitro compara, pregunta al usuario si hay discrepancias graves y fusiona. Se aplica a tres fases: dudas, catálogo de requisitos y backlog.
- Modelos: solo a través de la pasarela LiteLLM de la empresa. Ids exactos: `SS - GPT-6-SOL` y `SS - GPT-6-LUNA` (llevan prefijo y espacios). El reparto de roles es configuración.
- Runner: proceso hijo `claude -p`, con el prompt por entrada estándar, `--output-format stream-json --verbose` y veredicto final obligatorio (`VEREDICTO: {...}`). Proveedores: `LiteLlm` y `Fake`. No existe proveedor de suscripción personal ni debe crearse.
- Notación: EARS por defecto; MASTeR para requisitos no funcionales o complejos y para expresar obligatoriedad (shall, should, may).
- Árbitro: el emparejamiento por embeddings se calcula fuera del LLM; el LLM explica y fusiona. La pasarela no ofrece modelos de embeddings.
- Backlog: épica → historia → tarea por defecto; falta decidir si "feature" es un nivel propio.

## Decisiones abiertas (no las cierres sin preguntar)

Origen de los embeddings, jerarquía del backlog, qué modelo hace de Árbitro, umbral de "discrepancia grave" y ventana de contexto real de los modelos.

## Runner y pasarela (verificado)

- `ANTHROPIC_BASE_URL` es la dirección de la pasarela sin el `/v1` final, y `ANTHROPIC_AUTH_TOKEN` es la clave de la pasarela.
- Cada ejecución usa su propio `CLAUDE_CONFIG_DIR` vacío y un entorno construido de forma explícita: no se heredan variables del usuario ni `ANTHROPIC_API_KEY`.
- Las herramientas se permiten con `--allowedTools "Read,Write"`. Los dos modelos ejecutan herramientas correctamente.
- El aviso `unrecognized_model` sale por stderr y no es un error. Claude Code asume 200k de contexto y 32k de salida.
- El evento `result` final trae `is_error`, `terminal_reason`, `num_turns`, `result` (con el veredicto) y el uso de tokens. Su `total_cost_usd` no es fiable.
- Cada ejecución arrastra unos 15.000 tokens de contexto base.

## Secretos y datos

- Nunca escribas claves, tokens ni direcciones internas en código, documentación, tests ni mensajes de commit. Todo va por variables de entorno; el repo solo lleva `.env.example` con nombres y sin valores.
- No leas ni imprimas `.env` ni variables de entorno con credenciales.
- Usa solo datos ficticios o públicos (por ejemplo, el ejemplo ReservaSalas y los datasets públicos). Nunca documentos reales de la empresa, salvo autorización expresa del autor.

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