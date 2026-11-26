// Script de un solo fichero (.NET 10): sin compilación AOT, que no hace falta aquí.
#:property PublishAot=false
#:project ../../src/Concilia.Core/Concilia.Core.csproj

// Paso 120 (hito 27): Concilia frente a un solo modelo. Compara, fase a fase,
// lo que propone A solo, B solo y la unión del árbitro de un mismo trabajo,
// frente a la referencia del autor (paso 118): las trampas de ejemplos/README.md
// y la lista de funcionalidades de docs/evaluacion-2/referencia-clinicas.md.
//
// Tres órdenes; cada tanda (la evaluación con Kimi, «kimi», y SOL ‖ SOL del
// paso 121, «sol-sol») va en su carpeta, docs/evaluacion-2/revision/<tanda>/:
//
//   extraer  La lanza el autor, porque lee data/: la API no da las propuestas
//            completas de A y B, con sus fuentes. Toma la primera repetición
//            válida de cada fase del proyecto «<prefijo> 1» (las preguntas, el
//            catálogo y el backlog), con su informe, y lo deja en entradas.json.
//            Con --sin-data no lee data/: A y B salen solo con su texto, de la
//            comparación de la API (para probar el script con trabajos antiguos).
//   hojas    Prepara las hojas de revisión (CSV con «;», que Excel abre bien):
//            trampas-preguntas, trampas-catalogo, cobertura, muestra y
//            discrepancias. Son a ciegas: no dicen de qué propuesta viene cada
//            fila, y el orden está mezclado con una semilla fija; el origen
//            queda en claves.json. Un requisito con el mismo texto en dos
//            propuestas (la unión copia muchos de A) sale una sola vez.
//   calcular Lee las hojas rellenas y claves.json y escribe
//            docs/evaluacion-2/un-solo-modelo-<tanda>.md con, por fase, la
//            cobertura, la precisión, el F1, las trampas, el coste y los
//            minutos de A solo, B solo y Concilia, cada cifra con sus casos.
//
// Cómo se calcula:
//   - cobertura = (sí + ½·en parte) / funcionalidades revisadas, sin las que terminan en «(fuera de alcance)»;
//   - precisión = (bien + ½·en parte) / requisitos de la muestra, donde «bien»
//     es fiel, no repetido y no sobra, y «en parte» es fiel en parte y no
//     repetido ni sobra;
//   - F1 = 2·cobertura·precisión / (cobertura + precisión), en el catálogo;
//   - coste: el de las ejecuciones de A (A solo), de B (B solo) o todas
//     (Concilia), como en repeticiones.cs; minutos: los de A, los de B y los
//     del trabajo entero.
//
// Uso, desde la raíz del repositorio, con la aplicación en marcha:
//   dotnet run scripts/evaluacion/un-solo-modelo.cs -- extraer "Clínicas eval" kimi
//   dotnet run scripts/evaluacion/un-solo-modelo.cs -- hojas kimi
//   dotnet run scripts/evaluacion/un-solo-modelo.cs -- calcular kimi
// Opciones: --api http://localhost:8080; en extraer, --data data y --sin-data;
// en hojas, --referencia <fichero> (otra lista de funcionalidades, para probar).

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Concilia.Core.Catalog;
using Concilia.Core.Embeddings;

var options = args.Where(a => !a.StartsWith("--")).ToList();
string Option(string name, string fallback) => args.SkipWhile(a => a != name).Skip(1).FirstOrDefault() ?? fallback;
var api = Option("--api", "http://localhost:8080").TrimEnd('/');
var es = CultureInfo.GetCultureInfo("es-ES");
var utf8Bom = new UTF8Encoding(true);
var json = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
string[] Slots = ["A", "B", "unión"];
const int SampleSize = 30, Seed = 120;

string Folder(string batch) => Path.Combine("docs", "evaluacion-2", "revision", batch);

switch (options.FirstOrDefault())
{
    case "extraer" when options.Count >= 3:
        await Extract(options[1], options[2], Option("--data", "data"), args.Contains("--sin-data"));
        break;
    case "hojas" when options.Count >= 2:
        await Sheets(options[1]);
        break;
    case "calcular" when options.Count >= 2:
        Calculate(options[1]);
        break;
    default:
        Console.WriteLine("Uso: un-solo-modelo.cs extraer <prefijo> <tanda> [--data data] [--sin-data] | hojas <tanda> | calcular <tanda> [--api url]");
        return 1;
}
return 0;

// ======================================================================= extraer

async Task Extract(string prefix, string batch, string data, bool withoutData)
{
    using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(5) };
    var projects = (await http.GetFromJsonAsync<JsonArray>("/projects"))!;
    // «<prefix> 1»; for a test with an old project, the project called <prefix>.
    var project = projects.FirstOrDefault(p => p!["name"]!.GetValue<string>() == $"{prefix} 1")
        ?? projects.FirstOrDefault(p => p!["name"]!.GetValue<string>() == prefix)
        ?? throw new InvalidOperationException($"No existe el proyecto «{prefix} 1» ni «{prefix}».");
    var jobs = (await http.GetFromJsonAsync<JsonArray>($"/projects/{project["id"]}/jobs"))!
        .OrderBy(j => j!["job"]!["createdAt"]!.GetValue<DateTimeOffset>()).ToList();

    var phases = new JsonArray();
    foreach (var (phase, kind, file, root) in new[] { ("preguntas", "questions", "preguntas.json", "preguntas"), ("catalogo", "catalog-v0", "catalogo.json", "requisitos"), ("backlog", "backlog", "backlog.json", "epicas") })
    {
        // The first job of the phase that ended with a union of the arbiter.
        JsonNode? item = null, report = null;
        foreach (var candidate in jobs.Where(j => j!["job"]!["kind"]!.GetValue<string>() == kind))
        {
            var r = JsonNode.Parse(await http.GetStringAsync($"/jobs/{candidate!["job"]!["id"]}/report"))!;
            if (r["succeeded"]?.GetValue<bool>() == true && r["elements"] is not null) { item = candidate; report = r; break; }
        }
        if (item is null)
        {
            Console.WriteLine($"{phase}: sin trabajo válido.");
            continue;
        }
        var jobId = item["job"]!["id"]!.GetValue<string>();
        var result = JsonNode.Parse(await http.GetStringAsync($"/jobs/{jobId}/result"))!;
        var proposals = new JsonObject();
        if (withoutData)
        {
            var comparison = JsonNode.Parse(await http.GetStringAsync($"/jobs/{jobId}/comparison"))!;
            foreach (var slot in new[] { "A", "B" })
                proposals[slot] = new JsonArray(comparison["rows"]!.AsArray().SelectMany(r => r![slot.ToLowerInvariant()]!.AsArray())
                    .GroupBy(e => e!["id"]!.GetValue<string>()).Select(g => (JsonNode)new JsonObject
                    { ["id"] = g.Key, ["texto"] = g.First()!["text"]!.GetValue<string>(), ["fuentes"] = new JsonArray(), ["preguntas"] = new JsonArray() }).ToArray());
        }
        else
        {
            foreach (var slot in new[] { "A", "B" })
            {
                // Like the platform (JobProposals): the output of the last attempt of the slot.
                var last = item["runs"]!.AsArray().Where(r => r!["slot"]!.GetValue<string>() == slot).OrderBy(r => r!["attempt"]!.GetValue<int>()).Last()!;
                var path = Path.Combine(data, "runs", last["id"]!.GetValue<string>(), "output", file);
                proposals[slot] = Elements(phase, JsonNode.Parse(File.ReadAllText(path))!, null);
            }
        }
        proposals["unión"] = Elements(phase, result["salida"]!, result["desviaciones"] as JsonArray);
        phases.Add(new JsonObject
        {
            ["fase"] = phase, ["trabajo"] = jobId, ["informe"] = report!.DeepClone(), ["propuestas"] = proposals,
        });
        Console.WriteLine($"{phase}: trabajo {jobId[..8]}, A {proposals["A"]!.AsArray().Count}, B {proposals["B"]!.AsArray().Count}, unión {proposals["unión"]!.AsArray().Count}.");
    }

    Directory.CreateDirectory(Folder(batch));
    var inputs = new JsonObject
    {
        ["proyecto"] = project["name"]!.GetValue<string>(), ["proyecto_id"] = project["id"]!.GetValue<int>(),
        ["fecha"] = DateTime.Now.ToString("dd-MM-yyyy HH:mm"), ["sin_data"] = withoutData, ["fases"] = phases,
    };
    File.WriteAllText(Path.Combine(Folder(batch), "entradas.json"), inputs.ToJsonString(json), new UTF8Encoding(false));
    Console.WriteLine($"Escrito {Path.Combine(Folder(batch), "entradas.json")}");
}

// What the sheets need of each element: its id, the text the embeddings
// compare (as ProposalMatcher), its sources and, in the union, the arbiter's
// mark and the platform's deviations.
static JsonArray Elements(string phase, JsonNode root, JsonArray? deviations)
{
    var items = (phase == "backlog"
        ? root["epicas"]!.AsArray().SelectMany(e => e!["historias"]!.AsArray())
        : root[phase == "preguntas" ? "preguntas" : "requisitos"]!.AsArray()).OfType<JsonNode>();
    string S(JsonNode e, string field) => e[field]?.GetValue<string>() ?? "";
    return new JsonArray(items.Select(e => (JsonNode)new JsonObject
    {
        ["id"] = S(e!, "id"),
        ["texto"] = phase switch
        {
            "preguntas" => S(e!, "pregunta"),
            "catalogo" => S(e!, "texto"),
            _ => $"{S(e!, "titulo")}. Como {S(e!, "como")}, quiero {S(e!, "quiero")}, para {S(e!, "para")}.",
        },
        ["fuentes"] = (e!["fuentes"] ?? new JsonArray()).DeepClone(),
        ["preguntas"] = (e["preguntas"] ?? new JsonArray()).DeepClone(),
        ["discrepancia"] = e["discrepancia"]?.GetValue<bool>() ?? false,
        ["explicacion"] = S(e, "explicacion"),
        ["desviaciones"] = new JsonArray((deviations ?? []).Where(d => d!["id"]!.GetValue<string>() == S(e, "id"))
            .Select(d => (JsonNode)d!["explicacion"]!.GetValue<string>()).ToArray()),
    }).ToArray());
}

// ======================================================================= hojas

async Task Sheets(string batch)
{
    var inputs = JsonNode.Parse(File.ReadAllText(Path.Combine(Folder(batch), "entradas.json")))!;
    var phases = inputs["fases"]!.AsArray().ToDictionary(p => p!["fase"]!.GetValue<string>(), p => p!);
    List<Item> Of(string phase, string slot) => phases.TryGetValue(phase, out var p)
        ? p["propuestas"]![slot]!.AsArray().Select(e => new Item(e!["id"]!.GetValue<string>(), e["texto"]!.GetValue<string>(), e)).ToList()
        : [];

    var traps = Traps();
    var allFeatures = Features(Option("--referencia", Path.Combine("docs", "evaluacion-2", "referencia-clinicas.md")));
    // What the documents leave out of scope stays in the reference, but not in the coverage: a catalog
    // that rightly leaves it out would count as "not covered". The traps G3, G14 and G26 measure it.
    var features = allFeatures.Where(f => !f.OutOfScope).ToList();
    Console.WriteLine($"{traps.Count} trampas y {features.Count} funcionalidades de la referencia " +
        $"(sin las {allFeatures.Count - features.Count} «fuera de alcance», que no cuentan en la cobertura).");
    if (features.Count == 0)
        Console.WriteLine("Aviso: referencia-clinicas.md no tiene funcionalidades todavía; cobertura.csv sale vacía.");

    // The documentation and the questions of the project, for the sample.
    using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(5) };
    var projectId = inputs["proyecto_id"]!.GetValue<int>();
    var files = (await http.GetFromJsonAsync<JsonArray>($"/projects/{projectId}/files"))!
        .Where(f => f!["textStoredName"] is JsonValue)
        .OrderBy(f => f!["uploadedAt"]!.GetValue<DateTimeOffset>()).ThenBy(f => f!["id"]!.GetValue<int>()).ToList();
    var fragments = new Dictionary<string, string>();
    for (var i = 0; i < files.Count; i++)
        foreach (var f in Fragmenter.FragmentDocument($"DOC-{i + 1:00}", await http.GetStringAsync($"/projects/{projectId}/files/{files[i]!["id"]}/text")))
            fragments[f.Id] = f.Text;
    var questions = (await http.GetFromJsonAsync<JsonArray>($"/projects/{projectId}/questions"))!
        .ToDictionary(q => q!["code"]!.GetValue<string>(), q => $"{q!["text"]} → {q["answer"]?.GetValue<string>() ?? "(sin respuesta)"}");

    Console.WriteLine("Calculando los embeddings…");
    using var embedder = new OnnxTextEmbedder(Path.Combine("models", "paraphrase-multilingual-MiniLM-L12-v2"));
    var vectorCache = new Dictionary<string, float[]>();
    void Embed(IEnumerable<string> texts)
    {
        var missing = texts.Distinct().Where(t => !vectorCache.ContainsKey(t)).ToList();
        for (var start = 0; start < missing.Count; start += 256)
        {
            var batchTexts = missing.Skip(start).Take(256).ToList();
            var vectors = embedder.Embed(batchTexts);
            for (var i = 0; i < batchTexts.Count; i++)
                vectorCache[batchTexts[i]] = vectors[i];
        }
    }
    List<(Item Item, double Similarity)> Nearest(string query, List<Item> items, int count, string? skipId = null)
    {
        Embed(items.Select(i => i.Text).Append(query));
        return items.Where(i => i.Id != skipId).Select(i => (i, VectorMath.Cosine(vectorCache[query], vectorCache[i.Text])))
            .OrderByDescending(x => x.Item2).Take(count).ToList();
    }
    string Top3(string query, List<Item> items) => string.Join("\n", Nearest(query, items, 3).Select((x, n) => $"{n + 1}. {x.Item.Text}"));

    var keys = new JsonObject();
    var random = new Random(Seed);
    var folder = Folder(batch);

    // 1. trampas-preguntas: each open or resolved trap against each proposal of questions.
    {
        var rows = new List<(string[] Cells, JsonObject Key)>();
        foreach (var trap in traps.Where(t => t.Kind is "abierta" or "resuelta"))
            foreach (var slot in Slots)
                rows.Add(([trap.Id, trap.Kind, trap.Topic, trap.Says, trap.ResolvedIn, Top3($"{trap.Topic}. {trap.Says}", Of("preguntas", slot)), ""],
                    new JsonObject { ["trampa"] = trap.Id, ["tipo"] = trap.Kind, ["propuesta"] = slot }));
        WriteSheet(folder, "trampas-preguntas", ["trampa", "tipo", "tema", "que_dice_cada_documento", "se_resuelve_en", "preguntas_mas_parecidas", "trata"], rows, keys, random);
    }

    // 2. trampas-catalogo: resolved traps, gaps and out of scope against each catalog.
    {
        var rows = new List<(string[] Cells, JsonObject Key)>();
        foreach (var trap in traps.Where(t => t.Kind is "resuelta" or "hueco" or "fuera de alcance"))
            foreach (var slot in Slots)
                rows.Add(([trap.Id, trap.Kind, trap.Topic, trap.Says, trap.ResolvedIn, Top3($"{trap.Topic}. {trap.Says} {trap.ResolvedIn}", Of("catalogo", slot)), ""],
                    new JsonObject { ["trampa"] = trap.Id, ["tipo"] = trap.Kind, ["propuesta"] = slot }));
        WriteSheet(folder, "trampas-catalogo", ["trampa", "tipo", "tema", "que_dice_cada_documento", "se_resuelve_en", "requisitos_mas_parecidos", "correcto"], rows, keys, random);
    }

    // 3. cobertura: each feature against each catalog and each backlog.
    {
        var rows = new List<(string[] Cells, JsonObject Key)>();
        foreach (var feature in features)
            foreach (var phase in new[] { "catalogo", "backlog" })
                foreach (var slot in Slots)
                    rows.Add(([feature.Id, feature.Name, feature.Document, phase == "catalogo" ? "catálogo" : "backlog", Top3(feature.Name, Of(phase, slot)), ""],
                        new JsonObject { ["funcionalidad"] = feature.Id, ["fase"] = phase, ["propuesta"] = slot }));
        WriteSheet(folder, "cobertura", ["funcionalidad", "nombre", "documento", "fase", "elementos_mas_parecidos", "cubierta"], rows, keys, random);
    }

    // 4. muestra: 30 requirements at random of each catalog; the same text in
    // several proposals is reviewed once. "fiel" and "sobra" depend only on the
    // requirement, so a text reviewed in another batch comes filled in.
    {
        var reviewed = Reviewed(batch);
        var sampled = new Dictionary<string, (Item Item, List<string> Slots, string Nearest)>();
        // Its own generator, so the same catalogs always give the same sample,
        // whatever the other sheets hold.
        var sampler = new Random(Seed);
        foreach (var slot in Slots)
        {
            var catalog = Of("catalogo", slot);
            foreach (var item in catalog.OrderBy(_ => sampler.Next()).Take(SampleSize))
            {
                if (sampled.TryGetValue(item.Text, out var seen)) { seen.Slots.Add(slot); continue; }
                var nearest = Nearest(item.Text, catalog, 1, item.Id).FirstOrDefault();
                sampled[item.Text] = (item, [slot], nearest.Item is null ? "" : $"{nearest.Item.Text} ({nearest.Similarity.ToString("F2", es)})");
            }
        }
        var rows = sampled.Values.Select(s =>
        {
            var sources = string.Join("\n", (s.Item.Element["fuentes"] as JsonArray ?? []).Select(f => f!.GetValue<string>())
                .Select(id => $"[{id}] {fragments.GetValueOrDefault(id, "(fragmento no encontrado)")}"));
            var cited = string.Join("\n", (s.Item.Element["preguntas"] as JsonArray ?? []).Select(q => q!.GetValue<string>())
                .Select(code => $"[{code}] {questions.GetValueOrDefault(code, "(pregunta no encontrada)")}"));
            var before = reviewed.GetValueOrDefault(s.Item.Text);
            return (Cells: new[] { s.Item.Text, sources, cited, s.Nearest, before.Faithful ?? "", "", before.Extra ?? "", before.Faithful is null ? "" : "sí" },
                Key: new JsonObject { ["propuestas"] = new JsonArray(s.Slots.Select(x => (JsonNode)x).ToArray()), ["texto"] = s.Item.Text });
        }).ToList();
        WriteSheet(folder, "muestra", ["requisito", "fuentes_citadas", "preguntas_citadas", "mas_parecido_del_mismo_catalogo", "fiel", "repetido", "sobra", "reutilizado"], rows, keys, random);
    }

    // 5. discrepancias: those of the union of the catalog and the backlog (not blind: they are all of the union).
    {
        var rows = new List<(string[] Cells, JsonObject Key)>();
        foreach (var phase in new[] { "catalogo", "backlog" })
            foreach (var item in Of(phase, "unión").Where(i => i.Element["discrepancia"]!.GetValue<bool>() || i.Element["desviaciones"]!.AsArray().Count > 0))
                rows.Add(([phase == "catalogo" ? "catálogo" : "backlog", item.Id, item.Text, item.Element["explicacion"]!.GetValue<string>(),
                    string.Join("\n", item.Element["desviaciones"]!.AsArray().Select(d => d!.GetValue<string>())), ""],
                    new JsonObject { ["fase"] = phase, ["id"] = item.Id }));
        WriteSheet(folder, "discrepancias", ["fase", "id", "elemento", "explicacion_del_arbitro", "explicacion_de_la_plataforma", "real"], rows, keys, random);
    }

    File.WriteAllText(Path.Combine(folder, "claves.json"), keys.ToJsonString(json), new UTF8Encoding(false));
    Console.WriteLine($"Hojas en {folder}");
}

// Writes a sheet with its rows shuffled and numbered, and keeps the key of each row.
void WriteSheet(string folder, string name, string[] header, List<(string[] Cells, JsonObject Key)> rows, JsonObject keys, Random random)
{
    var shuffled = rows.OrderBy(_ => random.Next()).ToList();
    var csv = new StringBuilder();
    csv.AppendLine(string.Join(";", new[] { "fila" }.Concat(header).Select(Cell)));
    var sheetKeys = new JsonObject();
    for (var i = 0; i < shuffled.Count; i++)
    {
        csv.AppendLine(string.Join(";", new[] { (i + 1).ToString() }.Concat(shuffled[i].Cells).Select(Cell)));
        sheetKeys[(i + 1).ToString()] = shuffled[i].Key;
    }
    keys[name] = sheetKeys;
    File.WriteAllText(Path.Combine(folder, $"{name}.csv"), csv.ToString(), utf8Bom);
    Console.WriteLine($"{name}.csv: {shuffled.Count} filas");
}

static string Cell(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

// The traps of the large project: the table «Grande» of ejemplos/README.md.
// The kind comes from its last column, as in step 118.
static List<Trap> Traps()
{
    var traps = new List<Trap>();
    foreach (var line in File.ReadLines(Path.Combine("ejemplos", "README.md")))
    {
        var m = Regex.Match(line, @"^\|\s*(G\d+)\s*\|(.*)\|(.*)\|(.*)\|\s*$");
        if (!m.Success) continue;
        var resolvedIn = m.Groups[4].Value.Trim();
        var kind = resolvedIn.StartsWith("Fuera de alcance") ? "fuera de alcance"
            : resolvedIn is "—" or "-" ? "hueco"
            : Regex.IsMatch(resolvedIn, @"^No\b") ? "abierta"
            : "resuelta";
        traps.Add(new Trap(m.Groups[1].Value, kind, m.Groups[2].Value.Trim(), m.Groups[3].Value.Trim(), resolvedIn));
    }
    return traps;
}

// The features of the author's list: «F01 · nombre · documento».
static List<Feature> Features(string path)
{
    return !File.Exists(path) ? [] : File.ReadLines(path)
        .Select(l => Regex.Match(l, @"^(F\d+)\s*·\s*(.+?)\s*·\s*(.+?)\s*$")).Where(m => m.Success)
        .Select(m => new Feature(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value)).ToList();
}

// "fiel" and "sobra" of the requirements already reviewed in the sample of other batches.
Dictionary<string, (string? Faithful, string? Extra)> Reviewed(string batch)
{
    var reviewed = new Dictionary<string, (string?, string?)>();
    var root = Path.Combine("docs", "evaluacion-2", "revision");
    if (!Directory.Exists(root)) return reviewed;
    foreach (var other in Directory.GetDirectories(root).Where(d => Path.GetFileName(d) != batch))
    {
        var sheet = Path.Combine(other, "muestra.csv");
        if (!File.Exists(sheet)) continue;
        foreach (var row in ReadSheet(sheet).Where(r => Answer(r["fiel"]) is not null))
            reviewed[row["requisito"]] = (row["fiel"], row["sobra"]);
    }
    return reviewed;
}

// ======================================================================= calcular

void Calculate(string batch)
{
    var folder = Folder(batch);
    var keys = JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "claves.json")))!;
    var inputs = JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "entradas.json")))!;
    var prices = JsonNode.Parse(File.ReadAllText(Path.Combine("scripts", "evaluacion", "precios.json")))!;
    var pending = new List<string>();

    // The answers of a sheet with the key of each row; unanswered rows are counted apart.
    List<(Dictionary<string, string> Row, JsonNode Key)> Answered(string name, string column)
    {
        var rows = ReadSheet(Path.Combine(folder, $"{name}.csv"));
        var missing = rows.Count(r => Answer(r[column]) is null);
        if (missing > 0) pending.Add($"{name}.csv: {missing} de {rows.Count} filas sin «{column}»");
        return rows.Where(r => Answer(r[column]) is not null).Select(r => (r, keys[name]![r["fila"]]!)).ToList();
    }
    double Score(string? answer) => answer switch { "sí" => 1, "en parte" => 0.5, _ => 0 };
    string Ratio(double value, int cases) => cases == 0 ? "—" : $"{(value * 100).ToString("F0", es)} % (n = {cases})";

    var md = new StringBuilder();
    md.AppendLine($"# Concilia frente a un solo modelo (paso 120): {batch}");
    md.AppendLine();
    md.AppendLine($"Generado por `scripts/evaluacion/un-solo-modelo.cs` el {DateTime.Now:dd-MM-yyyy HH:mm} con las hojas de `docs/evaluacion-2/revision/{batch}/` " +
        $"(proyecto «{inputs["proyecto"]}», extraído el {inputs["fecha"]}). A solo y B solo son las propuestas de A y B del mismo trabajo; Concilia, la unión del árbitro. " +
        "Coste: lo que pagaría una empresa por uso (`precios.json`, la entrada leída de la caché a su precio); B por OpenCode Go se pagó con la suscripción. `n`: casos con los que se calcula cada cifra.");
    md.AppendLine();

    // Coverage, sample and traps, per slot.
    var coverage = Answered("cobertura", "cubierta");
    var sample = Answered("muestra", "fiel");
    var trapsQ = Answered("trampas-preguntas", "trata");
    var trapsC = Answered("trampas-catalogo", "correcto");
    var discrepancies = Answered("discrepancias", "real");

    (double Value, int Cases) Coverage(string phase, string slot)
    {
        var rows = coverage.Where(x => x.Key["fase"]!.GetValue<string>() == phase && x.Key["propuesta"]!.GetValue<string>() == slot).ToList();
        return (rows.Count == 0 ? 0 : rows.Average(x => Score(Answer(x.Row["cubierta"]))), rows.Count);
    }
    (double Value, int Cases) Precision(string slot)
    {
        var rows = sample.Where(x => x.Key["propuestas"]!.AsArray().Any(p => p!.GetValue<string>() == slot)).ToList();
        double Of(Dictionary<string, string> r) => Answer(r["repetido"]) == "sí" || Answer(r["sobra"]) == "sí" ? 0 : Score(Answer(r["fiel"]));
        return (rows.Count == 0 ? 0 : rows.Average(x => Of(x.Row)), rows.Count);
    }
    (double Value, int Cases) TrapScore(List<(Dictionary<string, string> Row, JsonNode Key)> rows, string column, string slot, params string[] kinds)
    {
        var mine = rows.Where(x => x.Key["propuesta"]!.GetValue<string>() == slot && kinds.Contains(x.Key["tipo"]!.GetValue<string>())).ToList();
        return (mine.Count == 0 ? 0 : mine.Average(x => Score(Answer(x.Row[column]))), mine.Count);
    }
    static double F1(double recall, double precision) => recall + precision == 0 ? 0 : 2 * recall * precision / (recall + precision);

    // Cost and minutes of a phase for a slot, from the job's report.
    (double Dollars, bool Estimated, double Minutes) CostOf(string phase, string slot)
    {
        var report = inputs["fases"]!.AsArray().FirstOrDefault(p => p!["fase"]!.GetValue<string>() == phase)?["informe"];
        if (report is null) return (0, false, 0);
        var runs = report["runs"]!.AsArray().Where(r => slot == "unión" || r!["slot"]!.GetValue<string>() == slot).ToList();
        double dollars = 0; var estimated = false;
        foreach (var run in runs)
        {
            var price = prices[run!["model"]!.GetValue<string>()];
            if (price is null) continue;
            estimated |= run["inputTokens"] is null;
            var input = (run["inputTokens"] ?? run["estimatedInputTokens"])?.GetValue<long>() ?? 0;
            var output = (run["outputTokens"] ?? run["estimatedOutputTokens"])?.GetValue<long>() ?? 0;
            var cache = run["cacheReadTokens"]?.GetValue<long>() ?? 0;
            dollars += ((input - cache) * price["entrada"]!.GetValue<double>() + cache * price["cache"]!.GetValue<double>() + output * price["salida"]!.GetValue<double>()) / 1_000_000;
        }
        var minutes = slot == "unión" ? (report["seconds"]?.GetValue<double>() ?? 0) / 60 : runs.Sum(r => r!["seconds"]?.GetValue<double>() ?? 0) / 60;
        return (dollars, estimated, minutes);
    }
    string Money(string phase, string slot) { var c = CostOf(phase, slot); return c.Dollars.ToString("F2", es) + (c.Estimated ? "*" : "") + " $"; }
    string Minutes(string phase, string slot) => CostOf(phase, slot).Minutes.ToString("F1", es);
    string Name(string slot) => slot switch { "A" => "A solo", "B" => "B solo", _ => "Concilia" };

    md.AppendLine("## Preguntas");
    md.AppendLine();
    md.AppendLine("| | Trampas abiertas que pregunta (más es mejor) | Trampas resueltas que pregunta de más (menos es mejor) | Coste | Minutos |");
    md.AppendLine("| --- | --- | --- | --- | --- |");
    foreach (var slot in Slots)
    {
        var open = TrapScore(trapsQ, "trata", slot, "abierta");
        var resolved = TrapScore(trapsQ, "trata", slot, "resuelta");
        md.AppendLine($"| {Name(slot)} | {Ratio(open.Value, open.Cases)} | {Ratio(resolved.Value, resolved.Cases)} | {Money("preguntas", slot)} | {Minutes("preguntas", slot)} |");
    }
    md.AppendLine();

    md.AppendLine("## Catálogo");
    md.AppendLine();
    md.AppendLine("| | Cobertura | Precisión (muestra) | F1 | Trampas bien tratadas | Coste | Minutos |");
    md.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
    foreach (var slot in Slots)
    {
        var c = Coverage("catalogo", slot);
        var p = Precision(slot);
        var t = TrapScore(trapsC, "correcto", slot, "resuelta", "hueco", "fuera de alcance");
        md.AppendLine($"| {Name(slot)} | {Ratio(c.Value, c.Cases)} | {Ratio(p.Value, p.Cases)} | {(c.Cases > 0 && p.Cases > 0 ? (F1(c.Value, p.Value) * 100).ToString("F0", es) + " %" : "—")} | " +
            $"{Ratio(t.Value, t.Cases)} | {Money("catalogo", slot)} | {Minutes("catalogo", slot)} |");
    }
    md.AppendLine();
    md.AppendLine("Trampas del catálogo por tipo:");
    md.AppendLine();
    md.AppendLine("| | Resueltas (recoge la decisión final) | Huecos (los recoge) | Fuera de alcance (no los incluye) |");
    md.AppendLine("| --- | --- | --- | --- |");
    foreach (var slot in Slots)
    {
        var r = TrapScore(trapsC, "correcto", slot, "resuelta");
        var h = TrapScore(trapsC, "correcto", slot, "hueco");
        var f = TrapScore(trapsC, "correcto", slot, "fuera de alcance");
        md.AppendLine($"| {Name(slot)} | {Ratio(r.Value, r.Cases)} | {Ratio(h.Value, h.Cases)} | {Ratio(f.Value, f.Cases)} |");
    }
    md.AppendLine();
    md.AppendLine("Precisión, en detalle: de los requisitos de la muestra, los fieles a su fuente, los repetidos y los que sobran.");
    md.AppendLine();
    md.AppendLine("| | Fiel (sí / en parte / no) | Repetido | Sobra |");
    md.AppendLine("| --- | --- | --- | --- |");
    foreach (var slot in Slots)
    {
        var rows = sample.Where(x => x.Key["propuestas"]!.AsArray().Any(p => p!.GetValue<string>() == slot)).Select(x => x.Row).ToList();
        int Count(string column, string answer) => rows.Count(r => Answer(r[column]) == answer);
        md.AppendLine($"| {Name(slot)} | {Count("fiel", "sí")} / {Count("fiel", "en parte")} / {Count("fiel", "no")} | {Count("repetido", "sí")} | {Count("sobra", "sí")} |");
    }
    md.AppendLine();

    md.AppendLine("## Backlog");
    md.AppendLine();
    md.AppendLine("| | Cobertura | Coste | Minutos |");
    md.AppendLine("| --- | --- | --- | --- |");
    foreach (var slot in Slots)
    {
        var c = Coverage("backlog", slot);
        md.AppendLine($"| {Name(slot)} | {Ratio(c.Value, c.Cases)} | {Money("backlog", slot)} | {Minutes("backlog", slot)} |");
    }
    md.AppendLine();

    md.AppendLine("## Discrepancias de la unión");
    md.AppendLine();
    foreach (var phase in new[] { "catalogo", "backlog" })
    {
        var rows = discrepancies.Where(x => x.Key["fase"]!.GetValue<string>() == phase).ToList();
        md.AppendLine($"- **{(phase == "catalogo" ? "Catálogo" : "Backlog")}:** {rows.Count(x => Answer(x.Row["real"]) == "sí")} de {rows.Count} eran algo que revisar de verdad.");
    }
    md.AppendLine();

    if (pending.Count > 0)
    {
        md.AppendLine("**Sin rellenar** (no cuentan en las cifras):");
        md.AppendLine();
        foreach (var p in pending) md.AppendLine($"- {p}");
        md.AppendLine();
    }

    var output = Path.Combine("docs", "evaluacion-2", $"un-solo-modelo-{batch}.md");
    File.WriteAllText(output, md.ToString(), new UTF8Encoding(false));
    foreach (var p in pending) Console.WriteLine($"Aviso: {p}");
    Console.WriteLine($"Escrito {output}");
}

// ======================================================================= CSV

// A sheet as written by WriteSheet (and saved again by Excel, with «;» or «,»).
static List<Dictionary<string, string>> ReadSheet(string path)
{
    var text = File.ReadAllText(path).TrimStart('﻿');
    var separator = text.Split('\n')[0].Count(c => c == ';') >= text.Split('\n')[0].Count(c => c == ',') ? ';' : ',';
    var records = new List<List<string>>();
    var record = new List<string>();
    var field = new StringBuilder();
    var quoted = false;
    for (var i = 0; i < text.Length; i++)
    {
        var c = text[i];
        if (quoted)
        {
            if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
            else if (c == '"') quoted = false;
            else field.Append(c);
        }
        else if (c == '"') quoted = true;
        else if (c == separator) { record.Add(field.ToString()); field.Clear(); }
        else if (c == '\n') { record.Add(field.ToString().TrimEnd('\r')); field.Clear(); records.Add(record); record = []; }
        else field.Append(c);
    }
    if (field.Length > 0 || record.Count > 0) { record.Add(field.ToString().TrimEnd('\r')); records.Add(record); }
    var header = records[0];
    return records.Skip(1).Where(r => r.Count > 1 || r[0].Length > 0)
        .Select(r => header.Select((h, i) => (h, v: i < r.Count ? r[i] : "")).ToDictionary(x => x.h, x => x.v)).ToList();
}

// sí / en parte / no, written in any usual way; null if empty or not understood.
static string? Answer(string? value)
{
    var v = (value ?? "").Trim().ToLowerInvariant();
    return v switch
    {
        "sí" or "si" or "s" or "x" or "1" => "sí",
        "en parte" or "parte" or "p" or "parcial" => "en parte",
        "no" or "n" or "0" => "no",
        _ => null,
    };
}

record Item(string Id, string Text, JsonNode Element);
record Trap(string Id, string Kind, string Topic, string Says, string ResolvedIn);
record Feature(string Id, string Name, string Document)
{
    public bool OutOfScope => Name.EndsWith("(fuera de alcance)", StringComparison.OrdinalIgnoreCase);
}
