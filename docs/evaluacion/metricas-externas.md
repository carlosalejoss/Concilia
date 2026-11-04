# Paso 104: métricas externas de calidad

La calidad del catálogo y del backlog, medida con herramientas de otros y no solo con las de Concilia. Las dos trabajan en inglés, así que se aplican a lo que generó el paso 105 (PURE · microcare, versión `evaluacion-1`): las tres propuestas de cada fase, la de A (SOL), la de B (LUNA) y la unión del árbitro.

## Las herramientas

Instaladas por Claude el 04-11-2026 en `C:\TFG\herramientas\`, fuera del repositorio. Se descargó todo de su origen y se comprobó, y se eligieron versiones publicadas hace al menos dos semanas.

### AQUSA

- **Qué es:** la versión de línea de órdenes de AQUSA (Lucassen, Dalpiaz, van der Werf y Brinkkemper, «Improving Agile Requirements: the Quality User Story Framework and Tool», *Requirements Engineering* 21, 2016), de la Universidad de Utrecht. Revisa historias de usuario con parte de los criterios del marco QUS.
- **Código:** `RELabUU/aqusa-core`, licencia MIT, commit `3043743` (09-03-2023, el último).
- **Entorno:** Python 3.8.20 con las versiones exactas de su `requirements.txt`: `nltk` 3.4.5 y `yattag` 1.11.1.
  - El README dice que está probado con Python 3.7, pero `uv` no ofrece la 3.7 para Windows. NLTK 3.4.5 admite la 3.8, la más cercana.
  - Con Python 3.14 y un NLTK reciente no arrancaría tal cual: los modelos que carga cambiaron de nombre. Y cambiar de versión podría cambiar los resultados.
  - El entorno lo crea `uv` 0.12.24 (08-10-2026), instalado con `pip` en un entorno aparte. Se descartó la 0.13.7, publicada el día anterior.
- **Datos de NLTK:** `punkt`, `averaged_perceptron_tagger` y `wordnet`, en su propia carpeta (`NLTK_DATA`), sin tocar el perfil del usuario.
- **Comandos:**

  ```bash
  cd C:/TFG/herramientas
  git clone https://github.com/RELabUU/aqusa-core.git
  python -m venv uv-env && uv-env/Scripts/python.exe -m pip install "uv==0.12.24"
  UV_PYTHON_INSTALL_DIR=C:/TFG/herramientas/python uv-env/Scripts/uv.exe venv --python 3.8.20 aqusa-env
  VIRTUAL_ENV=C:/TFG/herramientas/aqusa-env uv-env/Scripts/uv.exe pip install -r aqusa-core/requirements.txt
  aqusa-env/Scripts/python.exe -c "import nltk; [nltk.download(p, download_dir=r'C:\TFG\herramientas\nltk_data') for p in ['punkt', 'averaged_perceptron_tagger', 'wordnet']]"
  # uso: desde aqusa-core, con la entrada en input/ y la salida en output/
  NLTK_DATA=C:/TFG/herramientas/nltk_data PYTHONUTF8=1 ../aqusa-env/Scripts/python.exe aqusacore.py -i historias.txt -o historias -f txt
  ```

- **Qué criterios aplica esta versión** (de los 13 de QUS):
  - `well_formed`: tiene rol y lo que se quiere;
  - `well_formed_content`: el rol es un nombre y lo que se quiere lleva verbo y nombre;
  - `atomic`: una sola cosa, sin conjunciones como *and*, *or* o `/`;
  - `minimal`: sin texto de más entre paréntesis o tras puntuación;
  - `unique`: sin historias repetidas;
  - `uniform`: todas con la misma plantilla.
- **Prueba con tres historias escritas a mano:**
  - la buena («As a clinic receptionist, I want to cancel an appointment, so that the slot becomes free for other clients.»): sin avisos;
  - la que tiene dos cosas a la vez («…cancel an appointment and send an invoice…»): `atomic.conjunctions`;
  - la que no lleva «so that»: sin avisos. Es correcto, porque en QUS el «para qué» es opcional y una historia bien formada solo exige el rol y lo que se quiere.

  Con el ejemplo `g09.txt` que trae el repositorio (85 historias) da 7 avisos `atomic.conjunctions`.
- **Limitación:** sus autores dan una precisión del 72 %, así que sus avisos se cuentan, pero no se dan por buenos sin mirarlos.

### Vale con `vale-ears`

- **Vale** 3.24.0 (01-10-2026), el ejecutable para Windows de `vale-cli/vale` (antes `errata-ai/vale`), licencia MIT. Se descartaron la 3.25 y la 3.26, con menos de dos semanas. SHA-256 del `.zip`, igual al de su fichero de sumas: `3a8884774973b6f2826877e1c6388a5feae4704ef88a9a9c912c77ce186990f3`.
- **`vale-ears`** 0.1.0 (12-06-2026, la única versión), `tbhb/vale-ears`, licencia MIT. SHA-256 de `ears.zip`: `4784c76092c85213391dc854c369f42dd4d5e9ebebd9816dcc45b0af6ec10a7f`. Se descomprimió a mano en `styles/`, en lugar de usar `vale sync`, para leer antes sus reglas.
- **`.vale.ini`:**

  ```ini
  StylesPath = styles
  MinAlertLevel = suggestion

  [*.txt]
  BasedOnStyles = ears
  ```

- **Uso:** `vale --config C:/TFG/herramientas/vale/.vale.ini --output=JSON requisitos.txt`, con un requisito por párrafo, porque `ears.Syntax` revisa párrafo a párrafo.
- **Qué hace cada regla, leída en su código:**
  - `ears.Syntax`: pasa el texto a minúsculas y exige, en una frase con *shall*:
    - como mucho una condición (*when*, *while*, *where* o *if*) sin comas dentro, terminada en coma;
    - opcionalmente *then*;
    - un sujeto que empiece por *the* antes de *shall*.
  - `ears.Shall`: marca cualquier *must*, *will*, *should* o *may*, también dentro de la frase.
  - `ears.PassiveVoice` y `ears.PassiveVoiceAdverb`: un auxiliar (*is*, *was*, *be*…) seguido de un participio, según el etiquetador de Vale.
  - `ears.WeakWords`: una lista de palabras vagas (*fast*, *several*, *some*, *secure*, *reliable*…).
- **Prueba con frases escritas a mano:**
  - «WHEN the user cancels an appointment, the system shall free the slot.»: sin avisos;
  - «The appointment must be cancelled quickly by the system.»: `ears.Shall`, `ears.PassiveVoice` y `ears.WeakWords`.
- **¿Distingue mayúsculas?** No. «WHEN … SHALL» en mayúsculas, como escribe Concilia, pasa igual que en minúsculas, y también un requisito que empieza en minúscula («the Voucher Management System SHALL…»), la duda que dejó el paso 105.
- **Dos limitaciones del patrón de `ears.Syntax`:**
  - una condición con comas dentro («WHEN recording voucher prices, sales, returns, drug prices or claims, the system SHALL…») sale como error, aunque sea EARS correcto;
  - dos condiciones seguidas («WHILE a claim is on hold, WHEN a reviewer opens it, the system SHALL…», el patrón complejo de EARS) también.

  Esos avisos se revisan uno a uno.
- **Un fallo de configuración evitado:** el paquete trae un vocabulario (`config/vocabularies/ears/accept.txt`) que acepta *must*, *should*, *will* y *may*. Si se activa con `Vocab = ears`, Vale convierte esas palabras en excepciones de todas las reglas y `ears.Shall` no salta nunca. El README del paquete no lo activa, y aquí tampoco.

## Cómo se mide

Con `scripts/evaluacion/metricas-externas.cs`, en dos órdenes:

1. **`extraer`**, que lanza el autor, porque lee `data/`, a la que Claude no tiene acceso. Lee de la API los trabajos del catálogo y del backlog del proyecto y su unión y, de `data/runs/`, solo las salidas de A y B de esos trabajos, igual que la plataforma (`JobProposals`). Lo deja en `metricas-externas/entradas.json`.
2. **`medir`**, que lanza Claude. Escribe la entrada de cada herramienta, las ejecuta y deja cada aviso con su elemento en `metricas-externas/resultados.json` y los recuentos en `metricas-externas/tablas.md`.

Cómo se escribe cada elemento:

- **Una historia:** «As {como}, I want to {quiero}, so that I can {para}.».
  - En Concilia se guarda en tres campos sin esas palabras.
  - `quiero` y `para` empiezan por un verbo, así que la plantilla literal del plan («I want {quiero}, so that {para}») daría frases como «I want configure…».
  - `como` se deja como lo escribió cada modelo, con su artículo cuando lo puso (A escribe «an authorized user», B «authorized user»).
- **Un requisito:** su texto tal cual, uno por párrafo, y solo los de notación EARS. Los MASTeR usan *should* y *may* a propósito, y `ears.Shall` los marcaría.

## Resultados

Medido el 04-11-2026 con las propuestas del paso 105: el autor lanzó `extraer` y Claude, `medir`. Los recuentos en bruto están en `metricas-externas/tablas.md` y cada aviso, con su elemento, en `metricas-externas/resultados.json`. Como pide el plan, cada aviso se revisó a mano.

### Los requisitos EARS (Vale con `vale-ears`)

Se miden 136 de los 142 requisitos de SOL, 104 de los 115 de LUNA y 114 de los 122 de la unión: los demás son MASTeR.

| | SOL (A) | LUNA (B) | Unión |
| --- | --- | --- | --- |
| Requisitos con algún aviso, en bruto | 37 (27,2 por cada 100) | 18 (17,3) | 25 (21,9) |
| `ears.Shall` | 0 | 0 | 0 |
| `ears.Syntax`, avisos / reales | 8 / 0 | 4 / 1 | 6 / 0 |
| `ears.WeakWords`, avisos / reales | 1 / 0 | 1 / 0 | 1 / 0 |
| `ears.PassiveVoice` y `PassiveVoiceAdverb`: en la condición | 25 avisos | 4 | 14 |
| ídem: en la respuesta del sistema (requisitos) | 7 | 9 | 8 |
| **Requisitos con un defecto real** | **7 (5,1 por cada 100)** | **10 (9,6)** | **8 (7,0)** |

Cómo se revisó:

- **`ears.Syntax`:** 17 de los 18 avisos son condiciones con comas dentro, que el patrón de la regla no admite. Por ejemplo, «IF a claim's thumbprint is unreadable, a prior print is absent or the thumbprint reader is unavailable, THEN … SHALL put the claim on hold», o listas como «for a voucher number, patient type and visit». El único real es el RF-038 de LUNA, cuyo sujeto es un pronombre: «WHEN the Voucher Management System accepts a return, **it** SHALL…».
- **`ears.WeakWords`:** los tres avisos son «Good», uno de los valores de la escala de satisfacción del documento («Not at all, Moderate, Good, Completely»), no una palabra vaga.
- **La voz pasiva,** según dónde cae respecto a «SHALL»:
  - en la condición («WHEN a voucher is created, …») es el uso normal de EARS, porque el disparador describe un suceso, y no se cuenta;
  - en la respuesta del sistema oculta quién actúa, que es lo que la regla quiere evitar, y se cuenta. Por ejemplo, «…SHALL warn the user and allow the duplicate name to be saved or changed».

  Es un criterio objetivo y que se puede repetir. Cuenta también alguna pasiva de una condición metida dentro de la respuesta («…accept a claim only if its voucher has been sold…»).
- **`ears.Shall` no salta en ningún requisito EARS:** todos usan *shall*.

### Las historias de usuario (AQUSA)

| | SOL (A) | LUNA (B) | Unión |
| --- | --- | --- | --- |
| Historias | 50 | 28 | 34 |
| Avisos (todos `atomic.conjunctions`) | 5 (10,0 por cada 100) | 10 (35,7) | 8 (23,5) |
| **Reales: dos acciones en una historia** | **3 (6,0 por cada 100)** | **6 (21,4)** | **6 (17,6)** |

- **Ningún otro criterio salta** (`well_formed`, `well_formed_content`, `minimal`, `unique`, `uniform`): todas las historias tienen rol y lo que se quiere, sin texto de más ni repetidas, y con la misma plantilla. B escribe el rol sin artículo («As authorized user»), y AQUSA no lo marca.
- **Real:** lo que se quiere junta dos acciones. Por ejemplo, «I want to manage distributor status and inspect a distributor's transaction history» (SOL, HU-008) o «I want to deploy and operate the system» (LUNA, HU-027).
- **No real:** una sola acción con dos objetos, como «I want to prepare a voucher with client and partner portions» (LUNA, HU-003) o «maintain provider payment terms and shared reimbursable drug prices» (unión, HU-013).
- Lista de cada una:
  - SOL, reales: HU-002, HU-008, HU-032; no reales: HU-024, HU-050;
  - LUNA, reales: HU-004, HU-005, HU-012, HU-013, HU-018, HU-027; no reales: HU-002, HU-003, HU-015, HU-021;
  - unión, reales: HU-004, HU-005, HU-008, HU-016, HU-024, HU-027; no reales: HU-013, HU-023.
- **Aciertos de AQUSA aquí:** 15 de 23 avisos (65 %), cerca del 72 % que dan sus autores.

### Qué dicen

- **Los dos modelos escriben EARS y historias bien formados:** ningún requisito usa otra palabra que *shall*, y ninguna historia falla la forma, la plantilla ni la unicidad. Los defectos que quedan son de estilo: la voz pasiva en la respuesta y las historias que hacen dos cosas.
- **SOL escribe más atómico que LUNA,** en las dos fases: 6 historias con dos acciones por cada 100 frente a 21, y 5 requisitos con un defecto por cada 100 frente a 10.
- **La unión queda entre los dos,** más cerca de LUNA en las historias (18 por cada 100). Cuadra con lo que se vio en el paso 105: el árbitro del backlog adopta la granularidad de B y junta historias de A. Las 6 historias de la unión con dos acciones juntan cada una 2 o 3 historias atómicas de SOL en una sola, con la forma de la de LUNA. Por ejemplo, la HU-008 («allocate … and generate the distributor invoice») junta «Allocate vouchers to distributors» y «Generate distribution invoices» de SOL. Aquí la unión pierde una calidad que SOL tenía.
- **Las herramientas en bruto exageran:** sin revisar, Vale daría un 22 % de requisitos de la unión con defecto y la cifra revisada es un 7 %. Casi todo el ruido son las comas en las condiciones y las pasivas en el disparador.

### ¿Pasa en otros backlogs?

Comprobado el mismo día, sin coste, con la comparación de A, B y el árbitro (`/jobs/{id}/comparison`) de todos los backlogs terminados. Para cada uno, cuántas historias de la unión juntan dos o más historias de una misma propuesta. AQUSA no se puede pasar a los demás, porque están en español: esto cuenta las uniones, no si crean historias con dos acciones.

| Proyecto | Fecha | Árbitro | A | B | Unión | Juntan ≥ 2 de A | Juntan ≥ 2 de B |
| --- | --- | --- | --- | --- | --- | --- | --- |
| ReservaSalas | 09-10 | v0 | 17 | 12 | 16 | 1 (6 %) | 1 (6 %) |
| Proyecto interno 1 | 09-10 | v0.1 | 37 | 23 | 37 | 0 | 0 |
| Proyecto interno 2 | 13-10 | v0.3 | 58 | 20 | 53 | 6 (11 %) | 17 (32 %) |
| Proyecto interno 3 | 14-10 | v0.7 | 118 | 36 | 46 | 26 (57 %) | 0 |
| Clínicas | 15-10 | v0.9 | 109 | 54 | 109 | 0 | 7 (6 %) |
| Banco de alimentos | 16-10 | v0.9 | 45 | 28 | 45 | 1 (2 %) | 6 (13 %) |
| Clínicas v0.11 | 22-10 | v0.11 | 152 | 56 | 121 | 26 (21 %) | 1 (1 %) |
| Clínicas v0.11, nuevo arbitraje de las mismas propuestas | 22-10 | v0.12 | 152 | 56 | 152 | 0 | 1 (1 %) |
| Clínicas v0.11 | 23-10 | v0.12 | 96 | 85 | 94 | 10 (11 %) | 5 (5 %) |
| Clínicas v0.11 | 03-11 | v0.12 | 125 | 40 | 89 | 21 (24 %) | 0 |
| PURE · microcare | 04-11 | v0.12 | 50 | 28 | 34 | 14 (41 %) | 0 |

- **Pasa a menudo, pero no siempre:** de un 0 % a un 57 % de la unión. Es más frecuente cuando A propone muchas más historias que B, y el árbitro toma las de B como medida.
- **No es una regla fija del prompt, sino variación del árbitro:** con las mismas propuestas de clínicas, el árbitro v0.11 juntó 26 y un nuevo arbitraje con la v0.12 ninguna. Con la v0.12 y otras propuestas, entre un 11 % y un 41 %.
- **Juntar no siempre es malo:** si A parte de más una funcionalidad, juntar es correcto. Solo en PURE se ha podido medir que crea historias con dos acciones (las 6 de arriba).
- **Qué se hace:** nada antes de evaluar (cambiaría la versión). Las repeticiones del paso 119 cuentan estas uniones en cada backlog. Si se confirma, la mejora más fiable es que la plataforma marque como discrepancia, sin IA, la historia de la unión que junta varias de una misma propuesta, como ya hace con las otras desviaciones del árbitro. Además, el prompt puede pedir que no se junten historias de A que ya son atómicas. Anotado en «Después» del plan.

### Limitaciones

- Un solo documento y una sola ejecución. Las repeticiones del paso 119 dirán cuánto varía.
- La revisión de los avisos es de Claude, con los criterios de arriba. Para la memoria conviene que el autor revise al menos los casos dudosos (las pasivas dentro de la respuesta y las historias de la unión).
- AQUSA solo ve las conjunciones. No mide si una historia que no las tiene hace varias cosas, ni otros criterios de QUS que esta versión no implementa (por ejemplo, independencia o estimabilidad).
- **No se ha hecho** lo mismo con el proyecto de clínicas en inglés, que el plan dejaba «si da tiempo»: habría que generarlo de nuevo en inglés, con su coste.

## En palabras sencillas

Se pasó lo que generó Concilia con un documento público por dos herramientas de otros investigadores. Una revisa que los requisitos sigan las plantillas EARS; la otra, que las historias de usuario estén bien escritas.
- **Lo básico lo cumplen todos:** las plantillas, la palabra *shall* y la forma de las historias.
- **Lo que queda son detalles de estilo:**
  - unos pocos requisitos dicen «permitir que se guarde» sin decir quién lo guarda;
  - algunas historias piden dos cosas a la vez.
- **SOL escribe más fino que LUNA,** y la unión del árbitro queda entre los dos.
- **Las herramientas no se pueden creer a ciegas:** unos siete de cada diez avisos de Vale (63 de 90) eran falsas alarmas, y hubo que revisarlos uno a uno.
- **Las historias de la unión piden más a menudo dos cosas que las de SOL:** al juntar historias pequeñas de SOL en otras más grandes, como las de LUNA, salen historias que piden dos cosas a la vez.
