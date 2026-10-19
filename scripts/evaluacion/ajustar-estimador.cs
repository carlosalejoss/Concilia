// Script de un solo fichero (.NET 10): sin compilación AOT, que no hace falta aquí.
#:property PublishAot=false
#:project ../../src/Concilia.Core/Concilia.Core.csproj

// Paso 95 (hito 18): ajusta los coeficientes del estimador de consumo
// (Concilia.Core/Runs/UsageEstimate.cs) con las ejecuciones que sí tienen
// consumo real y consola guardada, y mide su error.
//
// Para cada intento: la simulación del contexto (UsageEstimator.Simulate) y su
// consumo real. Por mínimos cuadrados, sin término independiente:
//   tokens de entrada ≈ a · turnos + b · caracteres enviados
//   tokens de salida  ≈ c · turnos + d · caracteres escritos
// con un ajuste para el mockup y otro para las fases. El error se mide
// estimando cada ejecución con un ajuste hecho sin ella (validación cruzada
// dejando uno fuera), por ejecución y sobre el total.
//
// Escribe docs/costes/estimador.md. Los coeficientes se copian a mano en
// UsageEstimate.cs, para que la plataforma use siempre los mismos.
//
// Uso, desde la raíz del repositorio, con la aplicación en marcha:
//   dotnet run scripts/evaluacion/ajustar-estimador.cs
//   dotnet run scripts/evaluacion/ajustar-estimador.cs -- http://localhost:8080 data

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Concilia.Core.Runner;
using Concilia.Core.Runs;

var api = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:8080";
var data = args.Length > 1 ? args[1] : "data";
var es = CultureInfo.GetCultureInfo("es-ES");

using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(5) };
var runs = new Dictionary<Guid, (JsonNode Run, string JobKind)>();
foreach (var project in (await http.GetFromJsonAsync<JsonArray>("/projects"))!)
    foreach (var job in (await http.GetFromJsonAsync<JsonArray>($"/projects/{project!["id"]}/jobs"))!)
        foreach (var run in job!["runs"]!.AsArray())
            runs[run!["id"]!.GetValue<Guid>()] = (run, job["job"]!["kind"]!.GetValue<string>());

string? ReadFile(string path) =>
    UsageEstimator.InRunFolder(path) is { } p && File.Exists(Path.Combine(data, "runs", p.RunId.ToString(), p.Relative))
        ? File.ReadAllText(Path.Combine(data, "runs", p.RunId.ToString(), p.Relative))
        : null;

// The events of each attempt, from the console of its first attempt.
var samples = new List<Sample>();
foreach (var (id, (run, jobKind)) in runs)
{
    if (run["retryOfRunId"] is not null) continue; // the first attempt has the console of all of them
    var file = Path.Combine(data, "runs", id.ToString(), "events.json");
    if (!File.Exists(file)) continue;
    var events = JsonNode.Parse(File.ReadAllText(file))!.AsArray()
        .Select(e => new RunEvent(Enum.Parse<RunEventKind>(e!["Kind"]!.GetValue<string>()), e["Text"]!.GetValue<string>()));
    foreach (var (attemptId, attempt) in UsageEstimator.SplitAttempts(id, events))
    {
        if (!runs.TryGetValue(attemptId, out var a) || a.Run["inputTokens"] is null) continue;
        var simulation = UsageEstimator.Simulate(attempt, ReadFile);
        if (simulation.Turns == 0) continue;
        samples.Add(new Sample(jobKind == "mockup" ? "Mockup" : "Fases", simulation,
            a.Run["inputTokens"]!.GetValue<long>(), a.Run["outputTokens"]!.GetValue<long>()));
    }
}

var md = new StringBuilder();
md.AppendLine("# Estimador del consumo de las ejecuciones sin dato (paso 95)");
md.AppendLine();
md.AppendLine($"Generado por `scripts/evaluacion/ajustar-estimador.cs` el {DateTime.Now:dd-MM-yyyy HH:mm} con {samples.Count} intentos que tienen consumo real y consola guardada.");
md.AppendLine();
md.AppendLine("| Grupo | Intentos | Entrada = a·turnos + b·caracteres | Salida = c·turnos + d·caracteres escritos | Error por ejecución, mediana (entrada / salida) | Error del total (entrada / salida) |");
md.AppendLine("| --- | --- | --- | --- | --- | --- |");
foreach (var group in samples.GroupBy(s => s.Group).OrderBy(g => g.Key))
{
    var rows = group.ToList();
    var (a, b) = Fit(rows, s => (s.Simulation.Turns, s.Simulation.InputChars), s => s.Input);
    var (c, d) = Fit(rows, s => (s.Simulation.Turns, s.Simulation.WrittenChars), s => s.Output);

    // Leave one out: each run estimated with the fit of the others.
    var errorsIn = new List<double>(); var errorsOut = new List<double>();
    double estimatedIn = 0, estimatedOut = 0, realIn = 0, realOut = 0;
    for (var i = 0; i < rows.Count; i++)
    {
        var rest = rows.Where((_, j) => j != i).ToList();
        var (ai, bi) = Fit(rest, s => (s.Simulation.Turns, s.Simulation.InputChars), s => s.Input);
        var (ci, di) = Fit(rest, s => (s.Simulation.Turns, s.Simulation.WrittenChars), s => s.Output);
        var r = rows[i];
        var ei = ai * r.Simulation.Turns + bi * r.Simulation.InputChars;
        var eo = ci * r.Simulation.Turns + di * r.Simulation.WrittenChars;
        errorsIn.Add(Math.Abs(ei - r.Input) / r.Input); errorsOut.Add(Math.Abs(eo - r.Output) / r.Output);
        estimatedIn += ei; estimatedOut += eo; realIn += r.Input; realOut += r.Output;
    }
    md.AppendLine($"| {group.Key} | {rows.Count} | a = {a.ToString("N0", es)}, b = 1 / {(1 / b).ToString("F2", es)} | c = {c.ToString("N0", es)}, d = {d.ToString("F3", es)} | " +
        $"{Median(errorsIn).ToString("P0", es)} / {Median(errorsOut).ToString("P0", es)} | " +
        $"{(estimatedIn / realIn - 1).ToString("+0.0 %;-0.0 %", es)} / {(estimatedOut / realOut - 1).ToString("+0.0 %;-0.0 %", es)} |");
    Console.WriteLine($"{group.Key}: new({a:F0}, 1 / {1 / b:F2}, {c:F0}, {d:F3})");
}
md.AppendLine();
md.AppendLine("Los coeficientes se copian en `src/Concilia.Core/Runs/UsageEstimate.cs` (`UsageEstimator.Mockup` y `UsageEstimator.Phases`).");

Directory.CreateDirectory(Path.Combine("docs", "costes"));
File.WriteAllText(Path.Combine("docs", "costes", "estimador.md"), md.ToString(), new UTF8Encoding(false));
Console.Write(md);

// y ≈ p·x1 + q·x2, by least squares (the normal equations of two unknowns).
static (double P, double Q) Fit(List<Sample> rows, Func<Sample, (double X1, double X2)> x, Func<Sample, double> y)
{
    double s11 = 0, s22 = 0, s12 = 0, y1 = 0, y2 = 0;
    foreach (var r in rows)
    {
        var (x1, x2) = x(r);
        s11 += x1 * x1; s22 += x2 * x2; s12 += x1 * x2; y1 += x1 * y(r); y2 += x2 * y(r);
    }
    var det = s11 * s22 - s12 * s12;
    return ((y1 * s22 - y2 * s12) / det, (s11 * y2 - s12 * y1) / det);
}

static double Median(List<double> values)
{
    var sorted = values.Order().ToList();
    return sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;
}

record Sample(string Group, ContextSimulation Simulation, long Input, long Output);
