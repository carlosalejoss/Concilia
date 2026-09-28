# Plan de construcción

Plan por hitos, con una web mínima operativa desde el hito 1. Cada hito termina con algo que se ejecuta.

## Reglas

- Un paso cada vez, en orden, con un commit por paso.
- Si un paso se atasca más de 30 minutos, se anota el bloqueo y se pasa al siguiente que no dependa de él.
- No se pule la interfaz hasta que exista el árbitro.
- Decisiones abiertas que afectan al plan: origen de los embeddings (antes del árbitro) y jerarquía del backlog (antes de la fase de backlog).

## Hito 0: preparación y prueba de humo (completado)

- [x] 1. Repositorio privado creado, con `.gitignore` y README.
- [x] 2. Herramientas instaladas y comprobadas (.NET 10, Node LTS, Docker, Git).
- [x] 3. Credenciales de la pasarela guardadas fuera del repositorio.
- [x] 4. Lista de modelos y una llamada de chat a cada modelo.
- [x] 5. `claude -p` contra la pasarela, con y sin herramientas, y captura en stream-json.

## Hito 1: esqueleto que arranca en Docker

- [x] 6. Solución .NET con `ReqArbiter.Api`, `ReqArbiter.Core` y un proyecto de tests. Endpoint `GET /health`.
- [x] 7. Frontend con Vite, React 19, TypeScript y Tailwind, con una página que muestra el estado de `/health`.
- [x] 8. Dockerfile de la API y del frontend, y `docker-compose.yml` con `db`, `api` y `web`. `.env.example` con `LITELLM_BASE_URL` y `LITELLM_API_KEY` (solo nombres, sin valores).
- [ ] 9. EF Core y el proveedor de la base de datos elegida, con una primera migración de la tabla `Projects`.
- [ ] 10. Frontend: crear y listar proyectos (nombre). Primer flujo de punta a punta.
- [ ] 11. GitHub Actions mínimo: compilar y testear la API y compilar el frontend.

**Hecho cuando:** con `docker compose up` se crea un proyecto en el navegador, se reinician los contenedores y sigue ahí.

## Hito 2: subir archivos (sin IA)

- [ ] 12. Endpoint de subida que guarda el archivo en `data/projects/{id}/` y registra nombre, tipo, tamaño y hash en la base de datos.
- [ ] 13. Frontend: zona de arrastrar y soltar y lista de archivos del proyecto.
- [ ] 14. Extracción de texto solo para `.txt` y `.md`, guardada junto al original. PDF y DOCX, más adelante.
- [ ] 15. Límite de tamaño, tipos permitidos y mensajes de error claros.

**Hecho cuando:** se sube un `.md` y se ve su texto en la web.

## Hito 3: primera llamada a un modelo desde la web

- [ ] 16. En `Core`, contrato del runner: `IRunnerProvider`, `RunRequest` y `RunResult` (con veredicto). Primero `FakeProvider`, que devuelve un JSON fijo.
- [ ] 17. Proyecto `ReqArbiter.Runner` y parser de stream-json, probado con las salidas grabadas en la prueba de humo del hito 0 (saneadas antes de copiarlas al repositorio).
- [ ] 18. Lanzamiento del proceso con carpeta de trabajo, lectura línea a línea de stdout, lectura de stderr por separado (el aviso `unrecognized_model` no es un fallo), latido y tiempo máximo.
- [ ] 19. Lectura del veredicto final en el campo `result`, con reintento si falta o es inválido.
- [ ] 20. `LiteLlmProvider`: `claude -p` real contra la pasarela, con entorno construido de forma explícita, `CLAUDE_CONFIG_DIR` propio por ejecución y `--allowedTools`. Node y Claude Code en la imagen de la API.
- [ ] 21. `POST /projects/{id}/runs` con una tarea trivial (resumir los archivos) y tabla `Runs` con estado y resultado.
- [ ] 22. Progreso en vivo por SSE y una consola de eventos en la web.

**Hecho cuando:** se pulsa "Ejecutar", se ve el progreso y el resultado de un modelo.

## Hito 4: doble modelo, sin árbitro

- [ ] 23. La misma tarea a `SS - GPT-6-SOL` y `SS - GPT-6-LUNA` en paralelo, con dos ejecuciones ligadas a una misma tarea.
- [ ] 24. Vista de dos columnas (A y B) en la web.

**Hecho cuando:** se ven dos resultados independientes de la misma entrada.

## Hito 5: primer catálogo real

- [ ] 25. Prompt v1 en `prompts/generacion_requisitos/` y montaje de sus entradas (nombre del sistema, idioma, glosario, dudas resueltas vacías y documentación).
- [ ] 26. Esquema y validador portados a C#, con tests con el ejemplo ReservaSalas del propio prompt.
- [ ] 27. Reintento con la lista de errores del validador, máximo dos veces.
- [ ] 28. Catálogo validado en la web: tabla de requisitos y preguntas abiertas.

**Hecho cuando:** se sube un documento, A y B generan catálogos válidos y se ven en la web.

## Después

Árbitro con embeddings y fusión, fase de dudas, backlog, aprobación por umbral y evaluación.