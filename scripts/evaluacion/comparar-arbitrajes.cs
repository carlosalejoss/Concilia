// Script de un solo fichero (.NET 10): sin compilación AOT, que no hace falta aquí.
#:property PublishAot=false

// Paso 88 (hito 17): compara los arbitrajes de las mismas propuestas de A y B,
// el trabajo original y sus nuevos arbitrajes, con la versión del prompt del
// árbitro de cada uno. Sirve para ver si el árbitro v0.10, que parte del
// emparejamiento de la plataforma, es más estable que el v0.9, que no lo tenía.
//
// De cada arbitraje: el tamaño de la unión, lo que viene de ambas, solo de A y
// solo de B, los descartes, las discrepancias que marcó el árbitro y las
// desviaciones respecto al emparejamiento (en los arbitrajes v0.9, las que la
// plataforma habría marcado). Escribe la tabla en
// docs/resultados-arbitro-v0.10/<proyecto>-<fase>.md.
//
// Uso, desde la raíz del repositorio, con la aplicación en marcha:
//   dotnet run scripts/evaluacion/comparar-arbitrajes.cs -- "Clinicas Veterinarias" catalog-v0
//   dotnet run scripts/evaluacion/comparar-arbitrajes.cs -- "Clinicas Veterinarias" questions http://localhost:8080

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

var projectName = args.Length > 0 ? args[0] : "Clinicas Veterinarias";
var kind = args.Length > 1 ? args[1] : "catalog-v0"; // questions, catalog-v0 o backlog
var api = args.Length > 2 ? args[2].TrimEnd('/') : "http://localhost:8080";
var es = CultureInfo.GetCultureInfo("es-ES");

using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(5) };
var project = (await http.GetFromJsonAsync<JsonArray>("/projects"))!.First(p => p!["name"]!.GetValue<string>() == projectName)!;
var jobs = (await http.GetFromJsonAsync<JsonArray>($"/projects/{project["id"]}/jobs"))!.ToList();

// El trabajo original de esa fase con más nuevos arbitrajes, y todos ellos.
var rows = new List<Row>();
var originals = jobs.Where(j => j!["job"]!["kind"]!.GetValue<string>() == kind).Select(j => j!["job"]!["id"]!.GetValue<string>()).ToList();
foreach (var job in jobs.OrderBy(j => j!["job"]!["createdAt"]!.GetValue<DateTimeOffset>()))
{
    var id = job!["job"]!["id"]!.GetValue<string>();
    var report = JsonNode.Parse(await http.GetStringAsync($"/jobs/{id}/report"))!;
    var source = report["rearbitrationOf"]?.GetValue<string>() ?? id;
    if (!originals.Contains(source)) continue;

    var arbiter = job["runs"]!.AsArray().Where(r => r!["slot"]!.GetValue<string>() == "ARB").LastOrDefault();
    var elements = report["elements"];
    int? deviations = null;
    if (elements is not null)
    {
        var matching = await http.GetAsync($"/jobs/{id}/matching");
        if (matching.IsSuccessStatusCode)
            deviations = JsonNode.Parse(await matching.Content.ReadAsStringAsync())!["deviationElements"]?.GetValue<int>();
    }
    rows.Add(new Row(source, job["job"]!["createdAt"]!.GetValue<DateTimeOffset>(), arbiter?["model"]?.GetValue<string>() ?? "?",
        arbiter?["promptVersion"]?.GetValue<string>() ?? "?", job["job"]!["finishedAt"] is null, report["succeeded"]!.GetValue<bool>(), report["repairs"]!.GetValue<int>(),
        report["seconds"]?.GetValue<double>(), elements?["merged"]?.GetValue<int>(), elements?["both"]?.GetValue<int>(),
        elements?["onlyA"]?.GetValue<int>(), elements?["onlyB"]?.GetValue<int>(), elements?["discarded"]?.GetValue<int>(),
        elements?["discrepancies"]?.GetValue<int>(), deviations));
}

// El grupo con más arbitrajes (si la fase tiene varios trabajos originales).
var group = rows.GroupBy(r => r.Source).OrderByDescending(g => g.Count()).First().ToList();

var md = new StringBuilder();
md.AppendLine($"# Arbitrajes de las mismas propuestas: {projectName}, {kind}");
md.AppendLine();
md.AppendLine($"Generado por `scripts/evaluacion/comparar-arbitrajes.cs` el {DateTime.Now:dd-MM-yyyy HH:mm}. Trabajo original `{group[0].Source}` y sus nuevos arbitrajes, con las mismas propuestas de A y B. " +
    "Desviaciones: elementos de la unión en los que el árbitro se aparta del emparejamiento de la plataforma (en la v0.9, los que habría marcado).");
md.AppendLine();
md.AppendLine("| Fecha | Árbitro | Prompt | Resultado | Reparaciones | Minutos | Unión | De ambas | Solo A | Solo B | Descartados | Discrepancias del árbitro | Desviaciones |");
md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
foreach (var r in group)
    md.AppendLine($"| {r.Created.ToLocalTime():dd-MM HH:mm} | {r.Model} | {r.Prompt} | {(r.Running ? "en curso" : r.Succeeded ? "válido" : "fallido")} | {r.Repairs} | " +
        $"{(r.Seconds is { } s ? (s / 60).ToString("F0", es) : "—")} | {Cell(r.Merged)} | {Cell(r.Both)} | {Cell(r.OnlyA)} | {Cell(r.OnlyB)} | " +
        $"{Cell(r.Discarded)} | {Cell(r.Discrepancies)} | {Cell(r.Deviations)} |");
md.AppendLine();

md.AppendLine("Resumen de los arbitrajes válidos, por árbitro y versión del prompt:");
md.AppendLine();
md.AppendLine("| Árbitro | Prompt | Válidos de | Unión (mín.–máx., media) | Solo B (media) | Desviaciones (media) |");
md.AppendLine("| --- | --- | --- | --- | --- | --- |");
foreach (var g in group.GroupBy(r => (r.Model, r.Prompt)).OrderBy(g => g.Key.Prompt).ThenBy(g => g.Key.Model))
{
    var valid = g.Where(r => !r.Running && r.Succeeded && r.Merged is not null).ToList();
    var finished = g.Count(r => !r.Running);
    string Range(Func<Row, int?> of) => valid.Count == 0 ? "—"
        : $"{valid.Min(r => of(r))}–{valid.Max(r => of(r))}, {valid.Average(r => of(r) ?? 0).ToString("F1", es)}";
    string Mean(Func<Row, int?> of) => valid.Count == 0 || valid.Any(r => of(r) is null) ? "—" : valid.Average(r => of(r)!.Value).ToString("F1", es);
    md.AppendLine($"| {g.Key.Model} | {g.Key.Prompt} | {valid.Count} de {finished} | {Range(r => r.Merged)} | {Mean(r => r.OnlyB)} | {Mean(r => r.Deviations)} |");
}
md.AppendLine();

var folder = Path.Combine("docs", "resultados-arbitro-v0.10");
Directory.CreateDirectory(folder);
var file = Path.Combine(folder, $"{Slug(projectName)}-{kind}.md");
File.WriteAllText(file, md.ToString(), new UTF8Encoding(false));
Console.Write(md);
Console.WriteLine($"Escrito en {file}");

static string Cell(int? value) => value?.ToString() ?? "—";

static string Slug(string name) => name.ToLowerInvariant().Replace(' ', '-');

record Row(string Source, DateTimeOffset Created, string Model, string Prompt, bool Running, bool Succeeded, int Repairs, double? Seconds,
    int? Merged, int? Both, int? OnlyA, int? OnlyB, int? Discarded, int? Discrepancies, int? Deviations);
