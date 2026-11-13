# Paso 132: cerrar el cambio del modelo B

13-11-2026. Último paso del hito 25. Tres cosas:
1. revisar con datos las medidas que se tomaron por LUNA;
2. los valores por defecto;
3. la documentación.

## Las medidas que se tomaron por LUNA

**Cómo se contó.** Todas las ejecuciones guardadas en los 18 proyectos que quedan, desde la API local, sin lanzar nada:
- se quitan las saltadas y las del mockup;
- se cuentan los avisos de las salidas aceptadas, que se guardan enteros;
- se cuenta el primer error de las salidas no válidas.

**Límites del recuento:**
- de una salida no válida solo se guarda el primer error, así que los códigos de error son un mínimo;
- los proyectos borrados no están, entre ellos el catálogo del hito 5, donde el `Write` de LUNA llegó cortado;
- de Kimi solo hay 5 ejecuciones (prueba de humo, ronda de preguntas, dos catálogos y un backlog).

**Con los prompts v0.12 y v0.13**, los que comparten todos los modelos (entre paréntesis, el número de ejecuciones):

| Medida (cuándo se tomó) | SOL como A (56) | LUNA como B (57) | Kimi como B (5) | SOL árbitro (55) |
|---|---|---|---|---|
| Salida de más de 32.000 tokens, que necesita el tope de 64.000 (hito 5) | 4 | 9 | 0\* | 0 |
| `Write` cortado: se para la ejecución (hito 5) | 0 | 0 | 0 | 0 |
| Reintento por inactividad (05-10) | 1 | 2 | 0 | 0 |
| `offset` y `limit` exactos: líneas sin leer (`LECTURA`, v0.2) | 0 | 0 | 0 | 0 |
| Pista de `REF-FRAG` (v0.7) | 0 | 4 | 0 | 0 |
| Claves repetidas (`CLAVE-DUP`, 16-10) | 0 | 0 | 0 | 0 |
| `REF-LINEA`, corregido por la plataforma (paso 89) | 0 | 3 | 0 | 0 |
| `TIPO`, corregido por la plataforma (paso 89) | 0 | 0 | 0 | 0 |
| `ARB-FALTA`, corregido por la plataforma (paso 89) | 0 | 0 | 0 | 0 |
| `ID-SEQ` (paso 122) | 0 | 4 | 0 | 0 |
| Error de esquema con su línea (paso 122) | 0 | 2 | 0 | 0 |
| Reparar editando: reparaciones lanzadas y válidas (paso 90) | 2 y 2 | 9 y 7 | 0 | 2 y 1 |

\* A 3 de las 5 ejecuciones de Kimi les falta el dato de tokens, porque se pararon al compactar después de escribir. Su backlog del paso 131 se estima en unos 54.000 tokens de salida, por encima de 32.000.

**Desde la v0** (todas las versiones guardadas), lo mismo, con más casos de LUNA (96 ejecuciones como B y 15 como árbitro en la v0.9):
- LUNA como B: 11 `REF-FRAG`, 3 `LECTURA`, 4 `REF-LINEA` (con 23 correcciones), 4 `ID-SEQ`, 5 errores de esquema y 21 reparaciones (17 válidas);
- LUNA como árbitro: 3 `ARB-FALTA` y 1 `TIPO`.

**Dos medidas que no estaban en la lista, pero que el recuento enseña:**

| Medida | SOL como A | LUNA como B | Kimi como B | SOL árbitro |
|---|---|---|---|---|
| Parar al compactar después de escribir y aceptar la salida (14-10) | 0 | 1 | **3** | 9 |
| Parar al compactar dos veces sin escribir (paso 92) | 2 | 2 | 0 | 4 |

### Qué dicen los datos

- **Kimi no ha disparado ninguna corrección de las pensadas para LUNA.** Pero solo son 5 ejecuciones: no basta para decir que no las necesite.
- **SOL sí necesita varias:**
  - el tope de salida de 64.000 tokens;
  - el reintento por inactividad;
  - las reparaciones editando;
  - las paradas al compactar, sobre todo como árbitro.
- **Kimi usa mucho la parada al compactar después de escribir:** actuó en 3 de sus 5 ejecuciones. Sin ella, esas ejecuciones habrían seguido después de escribir su salida válida, compactando y gastando, como el árbitro del 14-10 que agotó así los 30 minutos.
- **Ninguna medida estorba:** si no hacen falta, no actúan y no cuestan nada. Las correcciones de la plataforma solo cambian la salida cuando el error tiene una única solución posible.

### Decisión del autor

**Se mantienen todas** (13-11-2026, como recomendaba Claude). La evaluación 2 vuelve a contarlas, con muchas más ejecuciones de Kimi.

## Los valores por defecto

**Antes:** sin nada en `.env`, el modelo B era LUNA y usaba el proveedor general. Kimi por OpenCode Go solo se activaba con `RUNNER_PROVIDER_B` y `RUNNER_MODEL_B` en `.env`.

**Decisión del autor** (13-11-2026, como recomendaba Claude): **por defecto, B es Kimi K3.1 Pro por OpenCode Go, salvo en modo simulado.**
- **`RunnerOptions.ModelB`** pasa a `kimi-k3.1-pro`, también en `docker-compose.yml`.
- **`RunnerSetup.ProviderOfB`:**
  - si `RUNNER_PROVIDER_B` tiene valor, manda ese;
  - si no, OpenCode Go;
  - pero si el proveedor general es `Fake`, B también es `Fake`, y el arranque sin claves sigue igual que antes.
- **Sin la clave de OpenCode,** la API arranca, pero las tareas que usan B se rechazan con un mensaje claro (paso 128).
- **Para llevar B a la pasarela** (LUNA, o SOL contra SOL en el paso 121): `RUNNER_PROVIDER_B=LiteLlm` y su modelo. Si B y A usan el mismo modelo sin decirlo, la API no arranca y explica que un modelo no puede ir por dos proveedores (paso 128).

**Qué se queda igual:**
- el modo simulado;
- A, el árbitro, las sugerencias y el mockup;
- un `.env` que ya diga qué usar, como el del autor.

**Cómo se comprueba:**
- tests nuevos de `ProviderOfB` y de los valores por defecto;
- un test de B en la pasarela cuando su proveedor lo dice;
- los tests que daban por hecho a LUNA, actualizados.

1.060 tests en verde. Un test del mockup que no lee esta configuración (`Problems_between_the_screens_are_fixed_one_part_at_a_time`) falló 1 de cada 4 vueltas: queda anotado como test intermitente.

## La documentación

Al día con el modelo B:
- `CLAUDE.md`, el README (puesta en marcha y evaluación), `.env.example` y `docker-compose.yml`;
- `docs/costes/README.md`: Kimi y sus precios, y el límite común de Go con ventanas fijas;
- la sección temporal de la memoria.

La tabla de precios (`precios.json`) no cambia:
- ya tenía Kimi;
- Argon y Haiku se quedan, porque tienen ejecuciones de los pasos 129 y 130.
