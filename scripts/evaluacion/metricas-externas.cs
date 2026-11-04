// Script de un solo fichero (.NET 10): sin compilación AOT, que no hace falta aquí.
#:property PublishAot=false

// Paso 104 (hito 23): métricas externas de calidad. Mide el catálogo y el backlog
// de un proyecto con herramientas de otros: AQUSA (las historias de usuario,
// con los criterios del marco QUS) y Vale con el estilo vale-ears (los
// requisitos EARS). Las dos trabajan en inglés. Se miden las tres propuestas de
// cada fase: la de A (SOL), la de B (LUNA) y la unión del árbitro.
//
// Dos órdenes, para que lo que lee data/ quede aparte:
//   extraer: lee de la API los trabajos del catálogo y del backlog del proyecto
//            y, de data/runs/, las salidas de A y B de esos trabajos (la API no
//            las da). Las deja en docs/evaluacion/metricas-externas/entradas.json.
//   medir:   lee entradas.json, escribe la entrada de cada herramienta, las
//            ejecuta y cuenta los defectos. Escribe resultados.json (cada aviso,
//            con su elemento) y tablas.md (los recuentos), en la misma carpeta.
//
// Cómo se escribe cada elemento:
//   - una historia, «As {como}, I want to {quiero}, so that I can {para}.»: en
//     Concilia se guarda en tres campos sin esas palabras, «quiero» y «para»
//     empiezan por un verbo y «como» lleva su artículo cuando el modelo lo puso;
//   - un requisito, su texto tal cual, uno por párrafo, y solo los de notación
//     EARS: los MASTeR usan should y may a propósito, y ears.Shall los marcaría.
//
// Las herramientas se instalan fuera del repositorio (docs/evaluacion/metricas-externas.md):
//   <herramientas>/aqusa-core      el código de AQUSA (RELabUU/aqusa-core)
//   <herramientas>/aqusa-env       su entorno de Python 3.8, con nltk 3.4.5 y yattag 1.11.1
//   <herramientas>/nltk_data       punkt, averaged_perceptron_tagger y wordnet
//   <herramientas>/vale            vale.exe, su .vale.ini y el estilo ears en styles/
//
// Uso, desde la raíz del repositorio:
//   dotnet run scripts/evaluacion/metricas-externas.cs -- extraer "PURE · microcare" http://localhost:8080 data
//   dotnet run scripts/evaluacion/metricas-externas.cs -- medir ../herramientas

using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var folder = Path.Combine("docs", "evaluacion", "metricas-externas");
var inputsFile = Path.Combine(folder, "entradas.json");
var json = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
var utf8 = new UTF8Encoding(false);

switch (args.FirstOrDefault())
{
    case "extraer":
        await Extract(args.Length > 1 ? args[1] : "PURE · microcare", args.Length > 2 ? args[2].TrimEnd('/') : "http://localhost:8080", args.Length > 3 ? args[3] : "data");
        break;
    case "medir":
        Measure(Path.GetFullPath(args.Length > 1 ? args[1] : Path.Combine("..", "herramientas")));
        break;
    default:
        Console.WriteLine("Uso: metricas-externas.cs extraer [proyecto] [api] [data] | medir [herramientas]");
        return 1;
}
return 0;

// --- extraer ---------------------------------------------------------------

async Task Extract(string projectName, string api, string data)
{
    using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(5) };
    var project = (await http.GetFromJsonAsync<JsonArray>("/projects"))!.First(p => p!["name"]!.GetValue<string>() == projectName)!;
    var items = (await http.GetFromJsonAsync<JsonArray>($"/projects/{project["id"]}/jobs"))!;
    var phases = new List<Phase>();
    foreach (var (kind, phase, output) in new[] { ("catalog-v0", "catalogo", "catalogo.json"), ("backlog", "backlog", "backlog.json") })
    {
        // The last job of the phase that ended with a union of the arbiter.
        var item = items.Where(i => i!["job"]!["kind"]!.GetValue<string>() == kind && i["job"]!["finishedAt"] is not null)
            .OrderBy(i => i!["job"]!["createdAt"]!.GetValue<DateTimeOffset>()).Last()!;
        var jobId = item["job"]!["id"]!.GetValue<string>();
        var runs = item["runs"]!.AsArray();
        var proposals = new List<Proposal>();
        foreach (var slot in new[] { "A", "B" })
        {
            // Like the platform (JobProposals): the output of the last attempt of the slot, if it ended well.
            var last = runs.Where(r => r!["slot"]!.GetValue<string>() == slot).OrderBy(r => r!["attempt"]!.GetValue<int>()).Last()!;
            if (last["state"]!.GetValue<string>() != "Done")
                throw new InvalidOperationException($"{phase}: la última ejecución de {slot} no terminó bien.");
            var file = Path.Combine(data, "runs", last["id"]!.GetValue<string>(), "output", output);
            proposals.Add(new Proposal(slot, last["model"]!.GetValue<string>(), Elements(phase, JsonNode.Parse(File.ReadAllText(file))!)));
        }
        var result = JsonNode.Parse(await http.GetStringAsync($"/jobs/{jobId}/result"))!;
        var arbiter = runs.First(r => r!["slot"]!.GetValue<string>() == "ARB")!["model"]!.GetValue<string>();
        proposals.Add(new Proposal("unión", arbiter, Elements(phase, result["salida"]!)));
        phases.Add(new Phase(phase, jobId, proposals));
    }
    Directory.CreateDirectory(folder);
    File.WriteAllText(inputsFile, JsonSerializer.Serialize(new Inputs(projectName, DateTime.Now.ToString("dd-MM-yyyy HH:mm"), phases), json), utf8);
    foreach (var phase in phases)
        Console.WriteLine($"{phase.Name}: " + string.Join(", ", phase.Proposals.Select(p => $"{p.Slot} {p.Elements.Count}")));
    Console.WriteLine($"Escrito {inputsFile}");
}

// The fields each tool needs: the requirements with their notation, the stories with their three parts.
static List<Element> Elements(string phase, JsonNode root) => phase == "catalogo"
    ? root["requisitos"]!.AsArray().Select(r => new Element(r!["id"]!.GetValue<string>(), Text: r["texto"]!.GetValue<string>(),
        Notation: r["notacion"]!.GetValue<string>())).ToList()
    : root["epicas"]!.AsArray().SelectMany(e => e!["historias"]!.AsArray()).Select(h => new Element(h!["id"]!.GetValue<string>(),
        Role: h["como"]!.GetValue<string>(), Means: h["quiero"]!.GetValue<string>(), Ends: h["para"]!.GetValue<string>())).ToList();

// --- medir -----------------------------------------------------------------

void Measure(string tools)
{
    var inputs = JsonSerializer.Deserialize<Inputs>(File.ReadAllText(inputsFile), json)!;
    var work = Path.Combine(Path.GetTempPath(), "concilia-metricas-externas");
    Directory.CreateDirectory(work);
    var measured = new List<Measured>();
    foreach (var phase in inputs.Phases)
        foreach (var proposal in phase.Proposals)
        {
            var name = $"{phase.Name}-{Short(proposal)}";
            measured.Add(phase.Name == "catalogo" ? RunVale(tools, work, name, proposal) : RunAqusa(tools, name, proposal));
            Console.WriteLine($"{name}: {measured[^1].Counted} medidos, {measured[^1].Alerts.Count} avisos");
        }
    File.WriteAllText(Path.Combine(folder, "resultados.json"), JsonSerializer.Serialize(new Results(inputs.Project, DateTime.Now.ToString("dd-MM-yyyy HH:mm"), measured), json), utf8);
    File.WriteAllText(Path.Combine(folder, "tablas.md"), Tables(inputs, measured), utf8);
    Console.WriteLine($"Escritos resultados.json y tablas.md en {folder}");
}

// SOL, LUNA or unión: the last part of the model id names it ("SS - GPT-6-SOL" → SOL).
static string Short(Proposal p) => p.Slot == "unión" ? "union" : p.Model.Split('-').Last().Trim().ToLowerInvariant();

// Vale with vale-ears: one EARS requirement per paragraph, as ears.Syntax works by paragraph.
Measured RunVale(string tools, string work, string name, Proposal proposal)
{
    var ears = proposal.Elements.Where(e => e.Notation == "EARS").ToList();
    var file = Path.Combine(work, $"{name}.txt");
    // A line break inside a requirement would move the paragraphs that follow it.
    File.WriteAllText(file, string.Join("\n\n", ears.Select(e => Regex.Replace(e.Text!.Trim(), @"\s+", " "))) + "\n", utf8);
    var vale = Path.Combine(tools, "vale");
    var (stdout, _) = Run(Path.Combine(vale, "vale.exe"), ["--config", Path.Combine(vale, ".vale.ini"), "--output=JSON", file], vale, []);
    var alerts = new List<Alert>();
    // { "<file>": [ { "Check": "ears.Syntax", "Line": 3, "Message": "…", "Match": "…" }, … ] }
    foreach (var (_, list) in JsonNode.Parse(stdout)!.AsObject())
        foreach (var a in list!.AsArray())
        {
            // Paragraph i (from 0) starts on line 2i + 1.
            var line = a!["Line"]!.GetValue<int>();
            alerts.Add(new Alert(ears[(line - 1) / 2].Id, a["Check"]!.GetValue<string>(), a["Message"]!.GetValue<string>(), a["Match"]?.GetValue<string>() ?? ""));
        }
    return new Measured(name, proposal.Slot, proposal.Model, "Vale + vale-ears", proposal.Elements.Count, ears.Count, alerts);
}

// AQUSA: one story per line, in its own input/ folder; it writes its report in output/.
Measured RunAqusa(string tools, string name, Proposal proposal)
{
    var aqusa = Path.Combine(tools, "aqusa-core");
    var lines = proposal.Elements.Select(Story).ToList();
    File.WriteAllText(Path.Combine(aqusa, "input", $"{name}.txt"), string.Join("\n", lines) + "\n", utf8);
    var python = Path.Combine(tools, "aqusa-env", "Scripts", "python.exe");
    // PYTHONUTF8: Python 3.8 on Windows would read and write the files in the ANSI code page.
    Run(python, ["aqusacore.py", "-i", $"{name}.txt", "-o", name, "-f", "txt"], aqusa,
        new() { ["NLTK_DATA"] = Path.Combine(tools, "nltk_data"), ["PYTHONUTF8"] = "1" });
    var alerts = new List<Alert>();
    string? story = null;
    string? kind = null;
    // Story #2: "…" / Defect type: atomic.conjunctions / Message: …
    foreach (var line in File.ReadAllLines(Path.Combine(aqusa, "output", $"{name}.txt"), Encoding.UTF8))
    {
        if (Regex.Match(line, @"^Story #(\d+):") is { Success: true } s)
            story = proposal.Elements[int.Parse(s.Groups[1].Value) - 1].Id;
        else if (Regex.Match(line, @"^\s+Defect type: (\S+)") is { Success: true } d)
            kind = d.Groups[1].Value;
        else if (Regex.Match(line, @"^\s+Message: (.*)") is { Success: true } m && story is not null && kind is not null)
        {
            alerts.Add(new Alert(story, kind, m.Groups[1].Value, ""));
            kind = null;
        }
    }
    return new Measured(name, proposal.Slot, proposal.Model, "AQUSA", proposal.Elements.Count, proposal.Elements.Count, alerts);
}

// «As an authorized user, I want to create vouchers, so that I can supply the pilot.»
static string Story(Element e)
{
    var means = e.Means!.Trim();
    if (means.StartsWith("to ", StringComparison.OrdinalIgnoreCase)) means = means[3..];
    return $"As {e.Role!.Trim()}, I want to {means}, so that I can {e.Ends!.Trim().TrimEnd('.')}.";
}

static (string Out, string Err) Run(string exe, string[] arguments, string directory, Dictionary<string, string> env)
{
    var start = new ProcessStartInfo(exe) { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8 };
    foreach (var a in arguments) start.ArgumentList.Add(a);
    foreach (var (k, v) in env) start.Environment[k] = v;
    using var process = Process.Start(start)!;
    var stdout = process.StandardOutput.ReadToEndAsync();
    var stderr = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    // Vale ends with 1 when it finds errors: that is a result, not a failure.
    if (process.ExitCode > 1)
        throw new InvalidOperationException($"{Path.GetFileName(exe)} terminó con {process.ExitCode}: {stderr.Result}");
    return (stdout.Result, stderr.Result);
}

// Per proposal and check: the alerts, the elements with at least one and those per 100 measured.
string Tables(Inputs inputs, List<Measured> measured)
{
    var es = CultureInfo.GetCultureInfo("es-ES");
    var md = new StringBuilder();
    md.AppendLine("# Métricas externas: recuentos");
    md.AppendLine();
    md.AppendLine($"Generado por `scripts/evaluacion/metricas-externas.cs` el {DateTime.Now:dd-MM-yyyy HH:mm}, con las propuestas del proyecto «{inputs.Project}» extraídas el {inputs.Extracted}. Cada celda: elementos con al menos un aviso de ese criterio, entre paréntesis los avisos y, tras «·», los elementos con aviso por cada 100 medidos. El análisis está en `../metricas-externas.md`.");
    foreach (var tool in measured.Select(m => m.Tool).Distinct())
    {
        var group = measured.Where(m => m.Tool == tool).ToList();
        var checks = group.SelectMany(m => m.Alerts.Select(a => a.Check)).Distinct().Order().ToList();
        md.AppendLine();
        md.AppendLine($"## {tool}");
        md.AppendLine();
        md.AppendLine("| Propuesta | Elementos | Medidos | " + string.Join(" | ", checks.Select(c => $"`{c}`")) + " | Con algún aviso |");
        md.AppendLine("| --- | --- | --- | " + string.Join(" | ", checks.Select(_ => "---")) + " | --- |");
        foreach (var m in group)
        {
            string Cell(IEnumerable<Alert> alerts)
            {
                var list = alerts.ToList();
                var elements = list.Select(a => a.Element).Distinct().Count();
                var per100 = m.Counted == 0 ? 0 : elements * 100.0 / m.Counted;
                return $"{elements} ({list.Count}) · {per100.ToString("F1", es)}";
            }
            var who = m.Slot == "unión" ? "Unión" : $"{Short(new Proposal(m.Slot, m.Model, []))!.ToUpperInvariant()} ({m.Slot})";
            md.AppendLine($"| {who} | {m.Elements} | {m.Counted} | " + string.Join(" | ", checks.Select(c => Cell(m.Alerts.Where(a => a.Check == c)))) + $" | {Cell(m.Alerts)} |");
        }
    }
    return md.ToString();
}

record Element(string Id, string? Text = null, string? Notation = null, string? Role = null, string? Means = null, string? Ends = null);
record Proposal(string Slot, string Model, List<Element> Elements);
record Phase(string Name, string JobId, List<Proposal> Proposals);
record Inputs(string Project, string Extracted, List<Phase> Phases);
record Alert(string Element, string Check, string Message, string Match);
record Measured(string Name, string Slot, string Model, string Tool, int Elements, int Counted, List<Alert> Alerts);
record Results(string Project, string Measured, List<Measured> Proposals);
