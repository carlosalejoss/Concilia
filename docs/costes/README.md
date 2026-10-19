# Coste en tiempo y en dinero

Petición del autor (19-10-2026): medir cuánto cuesta cada fase del proceso, en tiempo y en dinero, para justificar los cambios del TFG no solo por la calidad, sino también por el coste. Un cambio puede ser mejor porque acierta más, porque es más estable, porque es más barato o porque tarda menos.

## Ficheros

| Fichero | Qué es |
| --- | --- |
| [`costes.md`](costes.md) | **Las tablas:** cada trabajo del proyecto grande de ejemplo, el flujo completo de cada proyecto, el antes y el después de cada cambio de prompt y el coste por papel y modelo. |
| [`estimador.md`](estimador.md) | El ajuste del estimador de las ejecuciones sin dato y su error. |
| `../../scripts/evaluacion/costes.cs` | Genera `costes.md` con la aplicación en marcha: `dotnet run scripts/evaluacion/costes.cs`. Se puede volver a generar en cualquier momento, y cada ejecución nueva entra sola. |
| `../../scripts/evaluacion/precios.json` | Los precios por modelo, editables. |
| `../../scripts/evaluacion/ajustar-estimador.cs` | Vuelve a ajustar el estimador con los datos que haya y mide su error: `dotnet run scripts/evaluacion/ajustar-estimador.cs`. |

## De dónde salen las cifras

- **Tokens y tiempos:** cada ejecución de un modelo guarda en la base de datos, desde el hito 3, su fase, su modelo, la versión del prompt, los tokens de entrada y de salida que da Claude Code al terminar, y sus horas de inicio y fin.
- **Precios** (pasarela LiteLLM de la empresa, captura del autor, 19-10-2026), en dólares por millón de tokens:

  | Modelo | Entrada | Salida |
  | --- | --- | --- |
  | SOL (6 y 6.1) | 2,00 | 10,00 |
  | LUNA | 0,10 | 0,50 |

  LUNA es 20 veces más barata que SOL.
- **Los mockups hechos con la suscripción del autor** (Sonnet) salen «sin precio»: la suscripción es una cuota fija, no se paga por token. Desde el proyecto grande de ejemplo el mockup se hace con SOL por la pasarela y sí tiene coste.
- **Los embeddings** se calculan en local: no cuestan dinero, solo tiempo de la API. Se mide el tiempo de reloj y el de CPU de cada emparejamiento, y se guarda con el resultado de cada trabajo.

## Las ejecuciones sin dato: el estimador (paso 95)

Claude Code solo da el consumo al terminar. Una ejecución que la plataforma detiene antes (porque empieza a compactar con la salida ya escrita) o que agota su tiempo no tiene dato. Pasa sobre todo en los árbitros de los catálogos grandes, justo los que más interesa comparar.

**En palabras sencillas:** la plataforma sabe qué ha ido entrando en la «memoria» del modelo durante la ejecución, porque guarda cada lectura con el tramo exacto que leyó y los ficheros que escribió. Cada vez que el modelo responde, la pasarela vuelve a cobrar toda esa memoria. Así que se reconstruye cómo creció turno a turno y se convierte en tokens con una fórmula sencilla, cuyos dos números se ajustan con las ejecuciones que sí tienen el dato real. Cuando Claude Code compacta, la memoria se resume y vuelve a empezar casi de cero, y el estimador lo tiene en cuenta.

**¿Cuánto se equivoca?** Se comprobó con 125 ejecuciones que sí tienen dato real, estimando cada una con un ajuste hecho sin ella:

| Grupo | Error típico de una ejecución (entrada / salida) | Error sobre el total (entrada / salida) |
| --- | --- | --- |
| Mockup (97 ejecuciones) | 5 % / 9 % | −0,7 % / −1,0 % |
| Fases y árbitros (28) | 9 % / 20 % | −3,5 % / −5,1 % |

Sirve para sumar el coste de un trabajo o de una fase. Una ejecución concreta puede desviarse más. En las tablas, las cifras con estimación llevan `*`.

## Lo que dicen los datos (19-10-2026)

- **El flujo completo** (preguntas, sugerencias, catálogo, backlog y mockup) cuesta **22,25 $ y 105 minutos** en el proyecto grande de ejemplo, y **8,76 $ y 55 minutos** en el mediano.
- **El mockup es la mayor parte del coste:** el 69 % en el proyecto grande (15,46 $) y el 74 % en el mediano. Si hay que ahorrar, es el primer sitio donde mirar.
- **B (LUNA) cuesta el 0,5 % del flujo del proyecto grande** (0,11 $), frente al 12 % de A (SOL, 2,64 $): la verificación cruzada con un segundo modelo barato añade muy poco coste.
- **El árbitro con embeddings (v0.10) frente al anterior (v0.9),** con las mismas propuestas del catálogo grande y SOL:
  - un arbitraje que sale a la primera cuesta unos **1,40 $**, frente a **1,58–1,98 $** con la v0.9, y lee unos 414.000 tokens de entrada frente a 449.000–682.000 (estimados);
  - la media de la v0.10 sube a 2,44 $ por una ejecución que necesitó una reparación (4,53 $, con 1,15 M tokens reales en el intento fallido);
  - **el emparejamiento cuesta unos 13 segundos (unos 54 s de CPU) y 0 $.**

  Con 2 y 3 ejecuciones es una señal, no una prueba.

## Límites

- **Pocas repeticiones** de cada configuración: las medias son orientativas.
- **Los tiempos** no sirven si el portátil se suspendió durante el trabajo (hay trabajos de mockup de más de 1.000 minutos por eso).
- **Sin consola guardada** (antes del 15-10-2026), una ejecución sin dato no se puede estimar; su cifra lleva `≥`.
- **El estimador** se ajustó con los datos hasta el 19-10-2026. Conviene volver a ajustarlo cuando haya muchas más ejecuciones.
- **Proyectos internos:** los que no son de ejemplo salen anonimizados y solo con cifras agregadas.
