// Script de un solo fichero (.NET 10): sin compilación AOT, que no hace falta aquí.
#:property PublishAot=false

// Paso 87 (hito 17): prepara la muestra de pares de A y B que el autor etiqueta
// a mano para calibrar los dos umbrales del emparejamiento por significado
// (opción D, 19-10-2026: el criterio del autor fija los umbrales y el consenso
// de los árbitros los comprueba).
//
// Pide a la API en marcha el emparejamiento (/jobs/{id}/matching) de los
// trabajos de preguntas, catálogo y backlog de los proyectos ficticios, y
// elige pares repartidos por tramos de similitud. Al azar no serviría: casi
// todos los pares son claramente distintos y no enseñan nada.
//
// Escribe en docs/calibracion-embeddings/:
//   muestra-para-etiquetar.csv  los dos textos de cada par, sin la similitud ni
//                               lo que hizo el árbitro, para no influir; el
//                               autor rellena la columna «etiqueta»
//   muestra-clave.json          de dónde sale cada par y su similitud, para el
//                               cálculo posterior
//
// Uso, desde la raíz del repositorio, con la aplicación en marcha:
//   dotnet run scripts/calibracion-embeddings/muestra.cs
//   dotnet run scripts/calibracion-embeddings/muestra.cs -- http://localhost:8080

using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

var api = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:8080";
var output = Path.Combine("docs", "calibracion-embeddings");

// Solo proyectos ficticios (decisión del autor, 19-10-2026).
string[] projectNames = ["Banco De Alimentos", "Clinicas Veterinarias", "ReservaSalasHito911.v2"];
// Los trabajos originales: un nuevo arbitraje repite las mismas propuestas de A y B.
string[] phaseKinds = ["questions", "catalog-v0", "backlog"];

// Tramos de similitud y pares por tramo: 6 × 8 = 48 pares.
double[] bandStarts = [0.4, 0.5, 0.6, 0.7, 0.8, 0.9];
const int PerBand = 8;

// Semilla fija: la misma muestra cada vez que se ejecuta con los mismos datos.
var random = new Random(87);

using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(5) };
var projects = (await http.GetFromJsonAsync<JsonArray>("/projects"))!;

var pairs = new List<Candidate>();
foreach (var name in projectNames)
{
    var project = projects.FirstOrDefault(p => p!["name"]!.GetValue<string>() == name)
        ?? throw new InvalidOperationException($"No existe el proyecto «{name}».");
    var jobs = (await http.GetFromJsonAsync<JsonArray>($"/projects/{project["id"]}/jobs"))!;
    foreach (var job in jobs.Select(j => j!["job"]!).Where(j => phaseKinds.Contains(j["kind"]!.GetValue<string>())))
    {
        var response = await http.GetAsync($"/jobs/{job["id"]}/matching");
        if (!response.IsSuccessStatusCode)
            continue; // sin propuesta válida de A o de B
        var view = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        var phase = view["phase"]!.GetValue<string>();
        foreach (var pair in view["matching"]!["pairs"]!.AsArray())
            pairs.Add(new Candidate(name, job["id"]!.GetValue<string>(), phase,
                pair!["aId"]!.GetValue<string>(), pair["bId"]!.GetValue<string>(),
                pair["aText"]!.GetValue<string>(), pair["bText"]!.GetValue<string>(),
                pair["similarity"]!.GetValue<double>(), pair["arbiterJoined"]?.GetValue<bool>()));
        Console.WriteLine($"{name}, {phase}: {view["matching"]!["pairs"]!.AsArray().Count} pares");
    }
}

// En cada tramo, pares de las tres fases por turnos, al azar dentro de cada fase.
var chosen = new List<Candidate>();
foreach (var start in bandStarts)
{
    var byPhase = pairs.Where(p => p.Similarity >= start && p.Similarity < start + 0.1)
        .GroupBy(p => p.Phase)
        .Select(g => new Queue<Candidate>(g.OrderBy(_ => random.Next())))
        .ToList();
    var taken = 0;
    while (taken < PerBand && byPhase.Any(q => q.Count > 0))
        foreach (var queue in byPhase.Where(q => q.Count > 0))
        {
            if (taken == PerBand) break;
            chosen.Add(queue.Dequeue());
            taken++;
        }
    Console.WriteLine($"Tramo {start:F1}–{start + 0.1:F1}: {taken} pares");
}

// Orden al azar, y A unas veces primero y otras segundo, para que no se note de qué modelo es cada texto.
var sample = chosen.OrderBy(_ => random.Next())
    .Select((c, i) => new SampleItem($"M{i + 1:D2}", random.Next(2) == 0, c))
    .ToList();

Directory.CreateDirectory(output);
var csv = new StringBuilder();
csv.AppendLine("id;fase;texto_1;texto_2;etiqueta");
foreach (var item in sample)
{
    var (first, second) = item.AFirst ? (item.Pair.AText, item.Pair.BText) : (item.Pair.BText, item.Pair.AText);
    csv.AppendLine(string.Join(';', item.Id, PhaseName(item.Pair.Phase), Cell(first), Cell(second), ""));
}
// Con BOM y «;», como el CSV del informe: Excel en español lo abre con sus columnas y tildes.
File.WriteAllText(Path.Combine(output, "muestra-para-etiquetar.csv"), csv.ToString(), new UTF8Encoding(true));
File.WriteAllText(Path.Combine(output, "muestra-clave.json"), JsonSerializer.Serialize(sample,
    new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
Console.WriteLine($"{sample.Count} pares escritos en {output}");

static string PhaseName(string phase) => phase switch
{
    "Questions" => "pregunta",
    "Catalog" => "requisito",
    "Backlog" => "historia",
    _ => phase,
};

static string Cell(string text) => "\"" + text.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";

record Candidate(string Project, string JobId, string Phase, string AId, string BId, string AText, string BText,
    double Similarity, bool? ArbiterJoined);

record SampleItem(string Id, bool AFirst, Candidate Pair);
