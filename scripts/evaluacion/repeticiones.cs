// Script de un solo fichero (.NET 10): sin compilación AOT, que no hace falta aquí.
#:property PublishAot=false
#:project ../../src/Concilia.Core/Concilia.Core.csproj

// Paso 119 (hito 27): las 3 repeticiones de cada fase con la misma entrada.
//
// Lee de la API en marcha los trabajos de las repeticiones, solo por la API
// (no lee data/):
//   - las preguntas, la primera ronda de cada uno de los proyectos «<prefijo> 1»,
//     «<prefijo> 2» y «<prefijo> 3» (una segunda ronda en el mismo proyecto ya
//     vería las preguntas de la primera);
//   - el catálogo y el backlog, todos los trabajos de esa fase de «<prefijo> 1»
//     (cada uno parte de las mismas respuestas o del mismo catálogo validado).
//
// De cada trabajo saca el informe de siempre (resultado, reparaciones, minutos,
// tokens, coste para una empresa y elementos de A, de B y de la unión) y, de su
// resultado, las desviaciones del árbitro por tipo (en el backlog, cuántas
// historias de la unión juntan varias de una misma propuesta).
//
// Mide cuánto se parecen las repeticiones entre sí: de cada dos repeticiones de
// A, qué parte de los elementos de una tiene en la otra un elemento «mismo» o
// «parecido» (los umbrales de la plataforma, 0,90 y 0,70), con los embeddings
// del modelo de models/, como el emparejamiento de A y B. Lo mismo con B y con
// la unión. Si la unión se parece más entre repeticiones que A y que B, el
// árbitro da un resultado más estable que cada modelo por separado.
//
// El coste es lo que pagaría una empresa por uso, con los precios de
// scripts/evaluacion/precios.json: la entrada leída de la caché a su precio y
// el resto de la entrada (también la escrita en la caché) a precio de entrada.
// Las ejecuciones sin dato de consumo usan la estimación del informe (paso 95)
// y se marcan con *.
//
// Escribe docs/evaluacion-2/repeticiones-tablas.md, que se genera de nuevo cada
// vez; el análisis va aparte, en docs/evaluacion-2/repeticiones.md.
//
// Uso, desde la raíz del repositorio, con la aplicación en marcha:
//   dotnet run scripts/evaluacion/repeticiones.cs -- "Clínicas eval"
//   dotnet run scripts/evaluacion/repeticiones.cs -- "Clínicas eval" http://localhost:8080 docs/evaluacion-2/repeticiones-tablas.md

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Concilia.Core.Embeddings;

var prefix = args.Length > 0 ? args[0] : "Clínicas eval";
var api = args.Length > 1 ? args[1].TrimEnd('/') : "http://localhost:8080";
var output = args.Length > 2 ? args[2] : Path.Combine("docs", "evaluacion-2", "repeticiones-tablas.md");
var es = CultureInfo.GetCultureInfo("es-ES");
const double Same = 0.90, Similar = 0.70;

var prices = JsonNode.Parse(File.ReadAllText(Path.Combine("scripts", "evaluacion", "precios.json")))!;
using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(5) };
var projects = (await http.GetFromJsonAsync<JsonArray>("/projects"))!;

JsonNode? Project(string name) => projects.FirstOrDefault(p => p!["name"]!.GetValue<string>() == name);

async Task<List<JsonNode>> JobsOf(JsonNode project, string kind) =>
    (await http.GetFromJsonAsync<JsonArray>($"/projects/{project["id"]}/jobs"))!
        .Where(j => j!["job"]!["kind"]!.GetValue<string>() == kind)
        .OrderBy(j => j!["job"]!["createdAt"]!.GetValue<DateTimeOffset>())
        .Select(j => j!).ToList();

// ---------------------------------------------------------------- Los trabajos

// Test mode, to try the script with an old project: if there is no
// «<prefix> 1», every job of the project called <prefix> is a repetition.
var testMode = Project($"{prefix} 1") is null;
var first = Project(testMode ? prefix : $"{prefix} 1") ?? throw new InvalidOperationException($"No existe el proyecto «{prefix} 1» ni «{prefix}».");
if (testMode)
    Console.WriteLine($"Modo de prueba: todos los trabajos de «{prefix}».");

var phases = new List<Phase>();
var questionJobs = new List<JsonNode>();
if (testMode)
    questionJobs = await JobsOf(first, "questions");
else
    for (var n = 1; n <= 3; n++)
        if (Project($"{prefix} {n}") is { } project && (await JobsOf(project, "questions")).FirstOrDefault() is { } round)
            questionJobs.Add(round); // only the first round of each project
phases.Add(await PhaseOf("Preguntas", questionJobs));
phases.Add(await PhaseOf("Catálogo", await JobsOf(first, "catalog-v0")));
phases.Add(await PhaseOf("Backlog", await JobsOf(first, "backlog")));

async Task<Phase> PhaseOf(string name, List<JsonNode> jobs)
{
    var repetitions = new List<Repetition>();
    foreach (var item in jobs)
    {
        var id = item["job"]!["id"]!.GetValue<string>();
        var report = JsonNode.Parse(await http.GetStringAsync($"/jobs/{id}/report"))!;
        var succeeded = report["succeeded"]?.GetValue<bool>() == true && report["elements"] is not null;
        Texts? texts = null;
        var deviations = new Dictionary<string, int>();
        if (succeeded)
        {
            var comparison = JsonNode.Parse(await http.GetStringAsync($"/jobs/{id}/comparison"))!;
            texts = TextsOf(comparison);
            var result = JsonNode.Parse(await http.GetStringAsync($"/jobs/{id}/result"))!;
            // One element can have several deviations; each type is counted once per element.
            foreach (var group in (result["desviaciones"] as JsonArray ?? []).GroupBy(d => d!["tipo"]!.GetValue<string>()))
                deviations[group.Key] = group.Select(d => d!["id"]!.GetValue<string>()).Distinct().Count();
        }
        repetitions.Add(new Repetition(id, item["job"]!["createdAt"]!.GetValue<DateTimeOffset>(), succeeded, report, texts, deviations));
    }
    return new Phase(name, repetitions);
}

// The texts of A, of B and of the union, as the comparison shows them (the
// same texts the embeddings compared for A and B).
static Texts TextsOf(JsonNode comparison)
{
    var a = new Dictionary<string, string>();
    var b = new Dictionary<string, string>();
    var union = new List<string>();
    foreach (var row in comparison["rows"]!.AsArray())
    {
        foreach (var e in row!["a"]!.AsArray())
            a.TryAdd(e!["id"]!.GetValue<string>(), e["text"]!.GetValue<string>());
        foreach (var e in row["b"]!.AsArray())
            b.TryAdd(e!["id"]!.GetValue<string>(), e["text"]!.GetValue<string>());
        if (row["kind"]!.GetValue<string>() == "Merged" && row["arbiterText"]?.GetValue<string>() is { } text)
            union.Add(text);
    }
    return new Texts(a.Values.ToList(), b.Values.ToList(), union);
}

// ---------------------------------------------------------------- El coste

// Dollars of one run of the report, and whether its usage was estimated.
(double Dollars, bool Estimated) CostOf(JsonNode run)
{
    var price = prices[run["model"]!.GetValue<string>()];
    if (price is null)
        return (0, false);
    var estimated = run["inputTokens"] is null;
    var input = (run["inputTokens"] ?? run["estimatedInputTokens"])?.GetValue<long>() ?? 0;
    var outputTokens = (run["outputTokens"] ?? run["estimatedOutputTokens"])?.GetValue<long>() ?? 0;
    var cache = run["cacheReadTokens"]?.GetValue<long>() ?? 0;
    var dollars = ((input - cache) * price["entrada"]!.GetValue<double>() + cache * price["cache"]!.GetValue<double>()
        + outputTokens * price["salida"]!.GetValue<double>()) / 1_000_000;
    return (dollars, estimated);
}

// The cost of the runs of some slots ("A", "B", "ARB"; null, all).
(double Dollars, bool Estimated) Cost(JsonNode report, string? slot = null)
{
    var runs = report["runs"]!.AsArray().Where(r => slot is null || r!["slot"]!.GetValue<string>() == slot).Select(r => CostOf(r!)).ToList();
    return (runs.Sum(r => r.Dollars), runs.Any(r => r.Estimated));
}

long Tokens(JsonNode report, string field, string estimatedField) =>
    report["runs"]!.AsArray().Sum(r => (r![field] ?? r[estimatedField])?.GetValue<long>() ?? 0);

string Money((double Dollars, bool Estimated) c) => c.Dollars.ToString("F2", es) + (c.Estimated ? "*" : "");

// ---------------------------------------------------------------- Lo que se parecen

Console.WriteLine("Calculando los embeddings…");
using var embedder = new OnnxTextEmbedder(Path.Combine("models", "paraphrase-multilingual-MiniLM-L12-v2"));

// Of the elements of x, the part that has in y an element at least "parecido";
// and the part with an element "mismo".
(double Similar, double Same) Overlap(List<string> x, List<string> y)
{
    if (x.Count == 0 || y.Count == 0)
        return (0, 0);
    var vectors = embedder.Embed(x.Concat(y).ToList());
    int similar = 0, same = 0;
    for (var i = 0; i < x.Count; i++)
    {
        var best = Enumerable.Range(0, y.Count).Max(j => VectorMath.Cosine(vectors[i], vectors[x.Count + j]));
        if (best >= Similar) similar++;
        if (best >= Same) same++;
    }
    return ((double)similar / x.Count, (double)same / x.Count);
}

// For each pair of valid repetitions, the mean of the two directions.
List<(string Pair, string Source, double Similar, double Same)> Stability(Phase phase)
{
    var valid = phase.Repetitions.Select((r, i) => (Number: i + 1, r)).Where(x => x.r.Texts is not null).ToList();
    var rows = new List<(string, string, double, double)>();
    for (var i = 0; i < valid.Count; i++)
        for (var j = i + 1; j < valid.Count; j++)
            foreach (var (source, pick) in new (string, Func<Texts, List<string>>)[] { ("A", t => t.A), ("B", t => t.B), ("Unión", t => t.Union) })
            {
                var x = pick(valid[i].r.Texts!);
                var y = pick(valid[j].r.Texts!);
                var forward = Overlap(x, y);
                var back = Overlap(y, x);
                rows.Add(($"{valid[i].Number}–{valid[j].Number}", source, (forward.Similar + back.Similar) / 2, (forward.Same + back.Same) / 2));
            }
    return rows;
}

// ---------------------------------------------------------------- El informe

string Pct(double x) => (x * 100).ToString("F0", es) + " %";
string Int(JsonNode? n) => n is null ? "—" : n.GetValue<int>().ToString(es);

var md = new StringBuilder();
md.AppendLine("# Repeticiones (paso 119): tablas");
md.AppendLine();
md.AppendLine($"Generado por `scripts/evaluacion/repeticiones.cs` el {DateTime.Now:dd-MM-yyyy HH:mm} " +
    (testMode ? $"en modo de prueba, con todos los trabajos de «{prefix}» (no son repeticiones de la misma entrada). " : $"con los proyectos «{prefix} 1» a «{prefix} 3». ") +
    "Coste: lo que pagaría una empresa por uso, con los precios de `precios.json` y la entrada leída de la caché a su precio; B se pagó con la suscripción Go. " +
    "`*`: con ejecuciones sin dato de consumo, estimadas a partir de su consola.");
md.AppendLine();

foreach (var phase in phases)
{
    md.AppendLine($"## {phase.Name}");
    md.AppendLine();
    if (phase.Repetitions.Count == 0)
    {
        md.AppendLine("Sin trabajos.");
        md.AppendLine();
        continue;
    }
    var failed = phase.Repetitions.Count(r => !r.Succeeded);
    md.AppendLine($"{phase.Repetitions.Count} trabajos, {phase.Repetitions.Count - failed} válidos y {failed} fallidos.");
    md.AppendLine();
    md.AppendLine("| Rep. | Trabajo | Resultado | Reparaciones | Minutos | Tokens de entrada | Tokens de salida | Coste A | Coste B | Coste árbitro | Coste total | A | B | Unión | De ambas | Solo A | Solo B | Descartados | Discrepancias | Desviaciones |");
    md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
    for (var i = 0; i < phase.Repetitions.Count; i++)
    {
        var r = phase.Repetitions[i];
        var e = r.Report["elements"];
        var minutes = r.Report["seconds"]?.GetValue<double>() is { } s ? (s / 60).ToString("F1", es) : "—";
        var deviations = r.Deviations.Count == 0 ? (r.Succeeded ? "0" : "—") : string.Join(", ", r.Deviations.Select(d => $"{d.Key} {d.Value}"));
        md.AppendLine($"| {i + 1} | `{r.JobId[..8]}` | {(r.Succeeded ? "válido" : "fallido")} | {Int(r.Report["repairs"])} | {minutes} | " +
            $"{Tokens(r.Report, "inputTokens", "estimatedInputTokens").ToString("N0", es)} | {Tokens(r.Report, "outputTokens", "estimatedOutputTokens").ToString("N0", es)} | " +
            $"{Money(Cost(r.Report, "A"))} | {Money(Cost(r.Report, "B"))} | {Money(Cost(r.Report, "ARB"))} | {Money(Cost(r.Report))} | " +
            $"{Int(e?["a"])} | {Int(e?["b"])} | {Int(e?["merged"])} | {Int(e?["both"])} | {Int(e?["onlyA"])} | {Int(e?["onlyB"])} | " +
            $"{Int(e?["discarded"])} | {Int(e?["discrepancies"])} | {deviations} |");
    }
    md.AppendLine();

    // What changes between the repetitions, in one line per measure.
    var valid = phase.Repetitions.Where(r => r.Succeeded).ToList();
    if (valid.Count > 1)
    {
        string Range(Func<Repetition, int?> pick) => valid.Select(pick).OfType<int>().ToList() is { Count: > 0 } v ? $"{v.Min()}–{v.Max()}" : "—";
        md.AppendLine($"- **Elementos, mínimo–máximo:** A {Range(r => r.Texts!.A.Count)}, B {Range(r => r.Texts!.B.Count)}, unión {Range(r => r.Texts!.Union.Count)}.");
        if (phase.Name == "Backlog")
        {
            md.AppendLine($"- **Historias de la unión que juntan varias de una misma propuesta:** {string.Join(", ", valid.Select(r => r.Deviations.GetValueOrDefault("junta-varias")))} " +
                $"de {string.Join(", ", valid.Select(r => r.Texts!.Union.Count))} historias.");
            md.AppendLine($"- **Criterios sin citar por el árbitro** (aviso `ARB-CITA`: pueden estar repetidos entre A y B, combinados o perdidos): {string.Join(", ", valid.Select(UncitedCriteria))}.");
        }
        md.AppendLine();
        md.AppendLine("Lo que se parecen las repeticiones: de los elementos de una, la parte que tiene en la otra un elemento «parecido» o más (≥ 0,70) y «mismo» (≥ 0,90); la media de las dos direcciones.");
        md.AppendLine();
        md.AppendLine("| Repeticiones | Propuesta | Con parecido o mismo | Con mismo |");
        md.AppendLine("| --- | --- | --- | --- |");
        var stability = Stability(phase);
        foreach (var s in stability)
            md.AppendLine($"| {s.Pair} | {s.Source} | {Pct(s.Similar)} | {Pct(s.Same)} |");
        foreach (var source in new[] { "A", "B", "Unión" })
        {
            var mine = stability.Where(s => s.Source == source).ToList();
            if (mine.Count > 0)
                md.AppendLine($"| **media** | **{source}** | **{Pct(mine.Average(s => s.Similar))}** | **{Pct(mine.Average(s => s.Same))}** |");
        }
        md.AppendLine();
    }

    // The arbiter's runs that did not end as usual: stopped while compacting
    // with the output written (near the limit of the context) or repaired.
    var notes = phase.Repetitions.Select((r, i) => (i, Runs: r.Report["runs"]!.AsArray()
        .Where(x => x!["slot"]!.GetValue<string>() == "ARB" && (x["error"] is JsonValue || x["inputTokens"] is null)).ToList()))
        .Where(x => x.Runs.Count > 0).ToList();
    if (notes.Count > 0)
    {
        md.AppendLine("Ejecuciones del árbitro con un mensaje o sin dato de consumo. «Se detuvo al empezar a compactar» o «compactó el contexto» quiere decir que fue al límite de los 128.000 tokens:");
        md.AppendLine();
        foreach (var (i, runs) in notes)
            foreach (var run in runs)
                md.AppendLine($"- Repetición {i + 1}, intento {run!["attempt"]}: {(run["error"]?.GetValue<string>() ?? "sin dato de consumo").TrimEnd('.')}.");
        md.AppendLine();
    }
}

// "no cita 59 criterios" of the ARB-CITA warning of the arbiter's last run: "59 de 277".
static string UncitedCriteria(Repetition r)
{
    var message = r.Report["runs"]!.AsArray().LastOrDefault(x => x!["slot"]!.GetValue<string>() == "ARB")?["error"]?.GetValue<string>() ?? "";
    var uncited = System.Text.RegularExpressions.Regex.Match(message, @"no cita (\d+) criterios");
    var total = System.Text.RegularExpressions.Regex.Match(message, @"De los (\d+) criterios");
    return uncited.Success ? $"{uncited.Groups[1].Value} de {(total.Success ? total.Groups[1].Value : "?")}" : "sin aviso";
}

Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, md.ToString(), new UTF8Encoding(false));
Console.WriteLine($"Escrito {output}");

record Texts(List<string> A, List<string> B, List<string> Union);
record Repetition(string JobId, DateTimeOffset Created, bool Succeeded, JsonNode Report, Texts? Texts, Dictionary<string, int> Deviations);
record Phase(string Name, List<Repetition> Repetitions);
