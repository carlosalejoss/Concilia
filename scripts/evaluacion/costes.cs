// Script de un solo fichero (.NET 10): sin compilación AOT, que no hace falta aquí.
#:property PublishAot=false

// Coste en tiempo y en dinero de cada fase del proceso (petición del autor,
// 19-10-2026), para justificar los cambios también por lo que cuestan.
//
// Lee de la API en marcha todos los trabajos con sus ejecuciones y su informe
// (tokens reales y, en las ejecuciones sin dato, la estimación del paso 95), y
// aplica los precios de scripts/evaluacion/precios.json. Escribe
// docs/costes/costes.md con:
//   1. los precios usados;
//   2. cada trabajo del proyecto grande de ejemplo (clínicas veterinarias);
//   3. el flujo completo de cada proyecto, por fase;
//   4. antes y después: la misma fase del mismo proyecto con distintas
//      versiones del prompt;
//   5. el coste por papel (A, B, árbitro, mockup) y modelo.
// Los proyectos que no son de ejemplo salen anonimizados («proyecto interno 1»…).
//
// Uso, desde la raíz del repositorio, con la aplicación en marcha:
//   dotnet run scripts/evaluacion/costes.cs
//   dotnet run scripts/evaluacion/costes.cs -- http://localhost:8080

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

var api = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:8080";
var es = CultureInfo.GetCultureInfo("es-ES");
const string Main = "Clinicas Veterinarias";
string[] examples = ["Clinicas Veterinarias", "Banco De Alimentos", "ReservaSalasHito911.v2"];

var prices = JsonNode.Parse(File.ReadAllText(Path.Combine("scripts", "evaluacion", "precios.json")))!.AsObject()
    .Where(p => !p.Key.StartsWith('_'))
    .ToDictionary(p => p.Key, p => (In: p.Value!["entrada"]!.GetValue<double>(), Out: p.Value["salida"]!.GetValue<double>()));

using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(10) };
var jobs = new List<JobCost>();
var internalNumber = 0;
foreach (var project in (await http.GetFromJsonAsync<JsonArray>("/projects"))!.OrderBy(p => p!["id"]!.GetValue<int>()))
{
    var name = project!["name"]!.GetValue<string>();
    var shown = examples.Contains(name) ? name : $"Proyecto interno {++internalNumber}";
    foreach (var item in (await http.GetFromJsonAsync<JsonArray>($"/projects/{project["id"]}/jobs"))!)
    {
        var job = item!["job"]!;
        var runs = item["runs"]!.AsArray();
        if (runs.Count == 0) continue;
        var report = JsonNode.Parse(await http.GetStringAsync($"/jobs/{job["id"]}/report"))!;
        // The lines of the report come in the same order as the runs of the job (slot, lot, attempt).
        var lines = report["runs"]!.AsArray();
        var runCosts = runs.Select((run, i) =>
        {
            var line = lines[i]!;
            var real = line["inputTokens"] is not null;
            long? input = real ? line["inputTokens"]!.GetValue<long>() : line["estimatedInputTokens"]?.GetValue<long>();
            long? output = real ? line["outputTokens"]!.GetValue<long>() : line["estimatedOutputTokens"]?.GetValue<long>();
            // Simulated runs (the Fake provider) cost nothing and are left out.
            var started = run!["startedAt"] is not null && run["provider"]?.GetValue<string>() != "Fake";
            return new RunCost(run["slot"]?.GetValue<string>() ?? "", run["model"]!.GetValue<string>(), run["promptVersion"]?.GetValue<string>() ?? "",
                started, input, output, !real && input is not null);
        }).Where(r => r.Started).ToList();
        if (runCosts.Count == 0) continue;
        // What the matching of the arbiter cost: kept with the result since it is measured; for the
        // arbitrations with prompts v0.10 from before, measured now with the same computation.
        var time = report["matchingTime"];
        var measuredLater = false;
        if (time is null && report["succeeded"]!.GetValue<bool>() && runCosts.Any(r => r.Slot == "ARB" && string.CompareOrdinal(r.Prompt, "v0.10") >= 0 && r.Prompt.Length >= 5))
        {
            var matching = await http.GetAsync($"/jobs/{job["id"]}/matching");
            if (matching.IsSuccessStatusCode)
            {
                time = JsonNode.Parse(await matching.Content.ReadAsStringAsync())!["time"];
                measuredLater = true;
            }
        }
        jobs.Add(new JobCost(shown, job["kind"]!.GetValue<string>(), report["phase"]?.GetValue<string>(), report["rearbitrationOf"] is not null,
            job["createdAt"]!.GetValue<DateTimeOffset>(), report["succeeded"]!.GetValue<bool>(), report["seconds"]?.GetValue<double>(),
            report["repairs"]!.GetValue<int>(), time?["seconds"]?.GetValue<double>(), time?["cpuSeconds"]?.GetValue<double>(), measuredLater, runCosts));
    }
}

double? CostOf(RunCost r) =>
    r.Input is { } i && r.Output is { } o && prices.TryGetValue(r.Model, out var p) ? (i * p.In + o * p.Out) / 1_000_000 : null;

var md = new StringBuilder();
md.AppendLine("# Coste en tiempo y en dinero");
md.AppendLine();
md.AppendLine($"Generado por `scripts/evaluacion/costes.cs` el {DateTime.Now:dd-MM-yyyy HH:mm}, con los datos guardados de cada ejecución. Material para justificar los cambios del TFG también por lo que cuestan.");
md.AppendLine();
md.AppendLine("**Cómo leer las cifras:**");
md.AppendLine("- **Tokens:** los que da Claude Code al terminar cada ejecución. Una ejecución que la plataforma detuvo antes del final no tiene dato y se **estima** a partir de su consola (paso 95; error en `estimador.md`). Las cifras con estimación llevan `*`; las que tienen ejecuciones sin dato ni estimación son un mínimo y llevan `≥`.");
md.AppendLine("- **Coste:** tokens × precio del modelo (sección 1), en dólares. Los embeddings se calculan en local y no cuestan dinero, solo tiempo.");
md.AppendLine("- **Minutos:** desde que se crea el trabajo hasta que termina. No sirven si el equipo se suspendió durante el trabajo.");
md.AppendLine();

md.AppendLine("## 1. Precios");
md.AppendLine();
md.AppendLine("| Modelo | Entrada (USD por millón de tokens) | Salida (USD por millón de tokens) |");
md.AppendLine("| --- | --- | --- |");
foreach (var (model, p) in prices)
    md.AppendLine($"| {model} | {p.In.ToString("F2", es)} | {p.Out.ToString("F2", es)} |");
md.AppendLine();

md.AppendLine($"## 2. Cada trabajo del proyecto grande de ejemplo ({Main})");
md.AppendLine();
md.AppendLine("| Fecha | Trabajo | Prompt | Resultado | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) | Minutos | Reparaciones | Embeddings (s / s de CPU) |");
md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
foreach (var j in jobs.Where(j => j.Project == Main).OrderBy(j => j.Created))
    md.AppendLine($"| {j.Created.ToLocalTime():dd-MM HH:mm} | {Name(j)} | {Version(j)} | {(j.Succeeded ? "válido" : "fallido")} | {j.Runs.Count} | " +
        $"{Tokens(j.Runs, r => r.Input)} | {Tokens(j.Runs, r => r.Output)} | {Money(j.Runs)} | {Minutes(j.Seconds)} | {j.Repairs} | " +
        $"{(j.MatchingSeconds is { } s ? $"{s.ToString("F1", es)} / {j.MatchingCpu!.Value.ToString("F1", es)}{(j.MatchingMeasuredLater ? " (medido después)" : "")}" : "—")} |");
md.AppendLine();

md.AppendLine("## 3. El flujo completo de cada proyecto, por fase");
md.AppendLine();
md.AppendLine("Solo los trabajos del flujo, no los nuevos arbitrajes de los experimentos. Si una fase se repitió, cuentan todas sus ejecuciones: es lo que costó llegar al resultado.");
md.AppendLine();
md.AppendLine("| Proyecto | Fase | Trabajos | Tokens de entrada | Tokens de salida | Coste (USD) | Minutos |");
md.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
foreach (var project in jobs.Where(j => !j.Rearbitration).GroupBy(j => j.Project))
{
    foreach (var phase in project.GroupBy(Name).OrderBy(g => g.Min(j => j.Created)))
    {
        var runs = phase.SelectMany(j => j.Runs).ToList();
        md.AppendLine($"| {project.Key} | {phase.Key} | {phase.Count()} | {Tokens(runs, r => r.Input)} | {Tokens(runs, r => r.Output)} | {Money(runs)} | {Minutes(phase.Sum(j => j.Seconds ?? 0))} |");
    }
    var all = project.SelectMany(j => j.Runs).ToList();
    md.AppendLine($"| **{project.Key}** | **Total** | {project.Count()} | **{Tokens(all, r => r.Input)}** | **{Tokens(all, r => r.Output)}** | **{Money(all)}** | **{Minutes(project.Sum(j => j.Seconds ?? 0))}** |");
}
md.AppendLine();

md.AppendLine("## 4. Antes y después: la misma fase con distintas versiones del prompt");
md.AppendLine();
md.AppendLine("Medias por trabajo, para la misma fase del mismo proyecto. Los nuevos arbitrajes (solo el árbitro, con las mismas propuestas) van aparte de los trabajos completos (A, B y árbitro).");
md.AppendLine();
md.AppendLine("| Proyecto | Fase | Modelos | Prompt | Trabajos (válidos) | Entrada (media) | Salida (media) | Coste medio (USD) | Coste (mín.–máx.) | Minutos (media) | Reparaciones (media) | Embeddings (s de CPU, media) |");
md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
// The same phase of the same project with the same models, in more than one version of the prompt.
foreach (var group in jobs.GroupBy(j => (j.Project, Phase: Name(j), Models: Models(j))).Where(g => g.Select(Version).Distinct().Count() > 1)
             .OrderBy(g => g.Key.Project).ThenBy(g => g.Key.Phase))
    foreach (var version in group.GroupBy(Version).OrderBy(v => v.Key.Length).ThenBy(v => v.Key, StringComparer.Ordinal))
    {
        var n = version.Count();
        string Mean(Func<RunCost, long?> of) => version.All(j => j.Runs.All(r => of(r) is not null))
            ? (version.Average(j => j.Runs.Sum(r => of(r)!.Value))).ToString("N0", es) + (version.Any(j => j.Runs.Any(r => r.Estimated)) ? "*" : "")
            : "≥ " + version.Average(j => j.Runs.Sum(r => of(r) ?? 0)).ToString("N0", es);
        var costs = version.Select(j => j.Runs.Sum(r => CostOf(r) ?? 0)).ToList();
        var priced = version.All(j => j.Runs.All(r => prices.ContainsKey(r.Model)));
        md.AppendLine($"| {group.Key.Project} | {group.Key.Phase} | {group.Key.Models} | {version.Key} | {n} ({version.Count(j => j.Succeeded)}) | {Mean(r => r.Input)} | {Mean(r => r.Output)} | " +
            $"{(priced ? costs.Average().ToString("F2", es) : "sin precio")} | {(priced ? $"{costs.Min().ToString("F2", es)}–{costs.Max().ToString("F2", es)}" : "—")} | " +
            $"{Minutes(version.Average(j => j.Seconds ?? 0))} | {version.Average(j => j.Repairs).ToString("F1", es)} | " +
            $"{(version.Any(j => j.MatchingCpu is not null) ? version.Where(j => j.MatchingCpu is not null).Average(j => j.MatchingCpu!.Value).ToString("F1", es) : "—")} |");
    }
md.AppendLine();

md.AppendLine($"## 5. Coste por papel y modelo ({Main}, flujo completo)");
md.AppendLine();
md.AppendLine("| Papel | Modelo | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) | Parte del coste |");
md.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
var mainRuns = jobs.Where(j => j.Project == Main && !j.Rearbitration).SelectMany(j => j.Runs).ToList();
var mainTotal = mainRuns.Sum(r => CostOf(r) ?? 0);
foreach (var role in mainRuns.GroupBy(r => (Role(r.Slot), r.Model)).OrderByDescending(g => g.Sum(r => CostOf(r) ?? 0)))
{
    var runs = role.ToList();
    var cost = runs.Sum(r => CostOf(r) ?? 0);
    md.AppendLine($"| {role.Key.Item1} | {role.Key.Model} | {runs.Count} | {Tokens(runs, r => r.Input)} | {Tokens(runs, r => r.Output)} | {Money(runs)} | {(mainTotal == 0 ? "—" : (cost / mainTotal).ToString("P1", es))} |");
}
md.AppendLine();

Directory.CreateDirectory(Path.Combine("docs", "costes"));
File.WriteAllText(Path.Combine("docs", "costes", "costes.md"), md.ToString(), new UTF8Encoding(false));
Console.Write(md);

// ---- Helpers ----

string Tokens(IEnumerable<RunCost> runs, Func<RunCost, long?> of)
{
    var list = runs.ToList();
    var sum = list.Sum(r => of(r) ?? 0).ToString("N0", es);
    if (list.Any(r => of(r) is null)) return "≥ " + sum;
    return list.Any(r => r.Estimated) ? sum + "*" : sum;
}

string Money(IEnumerable<RunCost> runs)
{
    var list = runs.ToList();
    if (list.Any(r => !prices.ContainsKey(r.Model))) return "sin precio"; // a model of the subscription (the mockup with Sonnet)
    var sum = list.Sum(r => CostOf(r) ?? 0).ToString("F2", es);
    if (list.Any(r => CostOf(r) is null)) return "≥ " + sum;
    return list.Any(r => r.Estimated) ? sum + "*" : sum;
}

string Minutes(double? seconds) => seconds is { } s ? (s / 60).ToString("F0", es) : "—";

static string Name(JobCost j) => j.Kind switch
{
    "questions" => "Preguntas",
    "catalog-v0" => "Catálogo",
    "backlog" => "Backlog",
    "mockup" => "Mockup",
    "answer-suggestions" => "Sugerencias de respuesta",
    "rearbitration" => $"Nuevo arbitraje ({j.Phase switch { "Questions" => "preguntas", "Catalog" => "catálogo", "Backlog" => "backlog", _ => j.Phase }})",
    "catalog" => "Catálogo por lotes (v1.x)",
    _ => j.Kind,
};

// The models of a job, as "A SOL · B LUNA · árbitro SOL" (only the roles it has).
static string Models(JobCost j) => string.Join(" · ", j.Runs.GroupBy(r => Role(r.Slot)).OrderBy(g => g.Key)
    .Select(g => $"{g.Key} {string.Join("/", g.Select(r => r.Model.Replace("SS - GPT-", "")).Distinct())}"));

// The version of the prompt of a job: the one of most of its runs.
static string Version(JobCost j) => j.Runs.GroupBy(r => r.Prompt).OrderByDescending(g => g.Count()).First().Key;

static string Role(string slot) => slot switch
{
    "A" => "A",
    "B" => "B",
    "ARB" => "Árbitro",
    "SUG" => "Sugerencias",
    _ => "Mockup",
};

record RunCost(string Slot, string Model, string Prompt, bool Started, long? Input, long? Output, bool Estimated);

record JobCost(string Project, string Kind, string? Phase, bool Rearbitration, DateTimeOffset Created, bool Succeeded, double? Seconds,
    int Repairs, double? MatchingSeconds, double? MatchingCpu, bool MatchingMeasuredLater, List<RunCost> Runs);
