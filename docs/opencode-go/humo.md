# Paso 129: la prueba de humo de los tres candidatos

Prueba del 11-11-2026, el mismo día en que el autor contrató OpenCode Go (plan Go).

**El autor:**
- creó la cuenta y la clave (espacio de trabajo con Go, permisos restringidos a llamar a los modelos y caducidad de 140 días);
- la puso en `.env` con `RUNNER_PROVIDER_B=OpenCodeGo` y `RUNNER_MODEL_B=kimi-k3.1-pro`.

Claude no leyó la clave ni el `.env`.

## Cómo se hizo

**Una ronda de preguntas de ReservaSalas** (el `DOC-01.txt` del ejemplo, ficticio) con cada candidato como B, en un proyecto propio para cada uno: «ReservaSalas humo Kimi», «… Argon» y «… Haiku». Prompts v0.12. A y el árbitro, SOL 6 por la pasarela.
- **Nombre del sistema:** «el sistema ReservaSalas». Si no, la plataforma toma el del proyecto, y en el paso 145 eso provocó una pregunta sobre la identidad del sistema.
- **Cambio de candidato:** recreando la API con `RUNNER_MODEL_B` en el entorno, sin tocar `.env` y sin tareas en marcha. Al terminar, la API vuelve a lo que dice `.env` (Kimi).
- **Sin la prueba de resumen del hito 3:** ese endpoint ya no existe. La ronda comprueba lo mismo y más (autor, 11-11-2026).

## Resultados

| | Kimi K3.1 Pro | Gemini 4 Argon | Claude Haiku 5.5 |
|---|---|---|---|
| Resultado de B | `Done`, veredicto «hecho», a la primera | `Done`, veredicto «hecho», a la primera | `Done`, veredicto «hecho», a la primera |
| Minutos de B (A con SOL: 0,9) | 2,8 | 5,0 | 0,5 |
| Turnos | 3 | 3 | 3 |
| Tokens de entrada (leídos de caché / escritos) | 15.922 (6.912 / 8.448) | 12.058 (3.039 / 0) | 42.258 (24.162 / 18.090) |
| Tokens de salida | 4.732 | 20.490 | 5.704 |
| Coste de B (caché a su precio) | 0,07 $ | 0,45 $ (0,23 $ con el descuento hasta el 30-11) | 0,02 $ |
| Preguntas de B / unión (de ambas, solo A, solo B) | 9 / 11 (8, 2, 1) | 8 / 9 (8, 1, 0) | 11 / 12 (7, 1, 4) |
| Ventana de contexto que asume Claude Code | **128.000** | **128.000** | **200.000** |
| Minutos del trabajo entero | 3,9 | 5,9 | 2,0 |

**Lo que se comprobó, en los tres:**
- **Herramientas:** solo `Read`, con el `offset` y el `limit` exactos del plan de lectura, y `Write`, sin reparaciones.
- **Consola:** la plataforma lee bien sus eventos. El aviso `unrecognized_model` sale en los tres y no es un error, como con SOL.
- **Tokens:** el evento final trae la entrada, la salida y la caché por separado, y se guardan (paso 128). Ninguna ejecución necesitó el estimador.
- **Veredicto y español:** la línea `VEREDICTO` correcta y las preguntas en español.
- **Compactaciones:** ninguna.
- **Velocidad:** ningún problema en una tarea pequeña. Kimi, el más lento según Artificial Analysis, tardó 2,8 minutos. La prueba de verdad es el catálogo de clínicas del paso 130.
- **La traducción del protocolo funciona:** Kimi (Chat Completions) y Argon (protocolo de Google) llaman a las herramientas igual que Haiku, que es nativo. El riesgo que se cubría con Haiku de reserva no se ha dado.
- **La clave:** funciona con los tres modelos. No apareció el «Missing API Key» del que hablaba un hilo de GitHub de OpenCode.

## Hallazgos

- **Haiku no cumple la condición de los 128.000 tokens.** Claude Code lo reconoce como un modelo de Anthropic y asume su ventana real de 200.000, sin hacer caso a `CLAUDE_CODE_MAX_CONTEXT_TOKENS`. Con Kimi y Argon, que no reconoce, asume los 128.000 del runner. Como Haiku es la reserva, solo se anota: si hubiera que elegirlo, antes habría que conseguir que trabaje con 128.000.
- **Argon razona mucho:** 20.490 tokens de salida para 8 preguntas, frente a 4.732 de Kimi. Con su precio de salida (20 $ por millón), eso lo encarece mucho: 6 veces lo de Kimi en esta ronda. Se confirma o no en el paso 130 con el catálogo de clínicas.
- **La caché de la pasarela, ya medida:** en estas tres rondas, A y el árbitro de SOL leyeron de la caché la mitad o más de su entrada. Con el coste principal (paso 128), SOL sale un poco más barato que a precio completo.

## Lo que queda por comprobar

- **Que el consumo cuadra con la consola de OpenCode,** y que el límite de cada modelo es independiente: lo comprueba el autor en su espacio de trabajo y se compara con `docs/costes/consumo.md` (Kimi 0,07 $, Argon 0,45 $ a precio de lista o 0,23 $ con descuento, Haiku 0,02 $).
- **Qué pasa al llegar al límite a mitad de una tarea:** no se puede probar sin gastar el límite. La documentación de Go dice que, sin «Extra Usage», al llegar al límite se bloquean las peticiones. Por eso, antes de cada tarea se estima su coste y solo se lanza si cabe en lo que queda de las 5 horas, como dice el plan.

## Coste de la prueba

- **OpenCode Go, a precio de lista:** 0,54 $ (Kimi 0,07, Argon 0,45 y Haiku 0,02), cada uno de su propio límite.
- **Pasarela, A y árbitro con SOL:** 0,32 $.

Detalle en `docs/costes/consumo.md`.

## En palabras sencillas

Se probó si la plataforma puede hablar con los tres modelos nuevos a través de OpenCode Go, con una tarea pequeña y barata. Los tres funcionaron a la primera: leyeron el documento, escribieron sus preguntas en español y terminaron bien, y la plataforma registró cuánto gastó cada uno. La «traducción» que temíamos para dos de ellos no dio problemas. Dos datos para la elección: Gemini piensa mucho y sale bastante más caro, y Haiku no respeta el mismo tamaño de memoria que los demás.
