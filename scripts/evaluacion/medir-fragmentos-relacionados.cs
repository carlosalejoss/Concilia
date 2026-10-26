// Script de un solo fichero (.NET 10): sin compilación AOT, que no hace falta aquí.
#:property PublishAot=false
#:project ../../src/Concilia.Core/Concilia.Core.csproj

// Paso 112 (hito 22): mide cuántos fragmentos anteriores se parecen a los de
// una iteración nueva, para proponer al autor el tope de la opción C
// (RelatedFragments: obligatorio leer lo nuevo y los fragmentos anteriores
// relacionados, desde «parecido», 0,70, con un tope por decidir).
//
// No hay todavía una segunda iteración real (la del paso 116), así que se
// simula con la documentación del proyecto de clínicas veterinarias:
//   - cada documento como si llegara después de los anteriores (el 2 tras el 1,
//     el 3 tras el 1 y el 2…);
//   - dos tandas: los documentos 15 a 21 tras los 1 a 14, y los 11 a 21 tras los 1 a 10.
// Un fragmento anterior está relacionado si su parecido con algún fragmento
// nuevo llega al umbral; su parecido es el mayor de todos.
//
// Lee la documentación de la API en marcha (el proyecto «Clinicas Veterinarias
// v0.11»), calcula los embeddings en local con el modelo de models/ y escribe
// docs/fragmentos-relacionados.md.
//
// Uso, desde la raíz del repositorio, con la aplicación en marcha:
//   dotnet run scripts/evaluacion/medir-fragmentos-relacionados.cs
//   dotnet run scripts/evaluacion/medir-fragmentos-relacionados.cs -- http://localhost:8080 "Clinicas Veterinarias v0.11"

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Concilia.Core.Catalog;
using Concilia.Core.Embeddings;
using Concilia.Core.Phases;
using Concilia.Core.Runner;

var api = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:8080";
var projectName = args.Length > 1 ? args[1] : "Clinicas Veterinarias v0.11";
var es = CultureInfo.GetCultureInfo("es-ES");
double[] thresholds = [0.70, 0.75, 0.80, 0.85];

// ---------------------------------------------------------------- La documentación

using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(5) };
var project = (await http.GetFromJsonAsync<JsonArray>("/projects"))!.First(p => p!["name"]!.GetValue<string>() == projectName)!;
var files = (await http.GetFromJsonAsync<JsonArray>($"/projects/{project["id"]}/files"))!
    .Where(f => f!["textStoredName"] is JsonValue)
    .OrderBy(f => f!["uploadedAt"]!.GetValue<DateTimeOffset>()).ThenBy(f => f!["id"]!.GetValue<int>())
    .ToList();
var documents = new List<(string Id, string Name, List<Fragment> Fragments)>();
for (var i = 0; i < files.Count; i++)
{
    var text = await http.GetStringAsync($"/projects/{project["id"]}/files/{files[i]!["id"]}/text");
    var id = $"DOC-{i + 1:00}";
    documents.Add((id, files[i]!["fileName"]!.GetValue<string>(), Fragmenter.FragmentDocument(id, text)));
}
var all = documents.SelectMany(d => d.Fragments).ToList();
Console.WriteLine($"{documents.Count} documentos, {all.Count} fragmentos. Calculando los embeddings…");

// Los tokens de cada fragmento como los lee el modelo: su línea de input/documentacion.txt.
// (sin la cabecera del documento; un fragmento largo ocupa varias líneas).
int TokensOf(Fragment f)
{
    var built = DocumentationFile.Build([f], new Dictionary<string, string>());
    return InputBudget.EstimateTokens(built[(built.IndexOf('\n') + 1)..]);
}
var tokens = all.ToDictionary(f => f.Id, TokensOf);

using var embedder = new OnnxTextEmbedder(Path.Combine("models", "paraphrase-multilingual-MiniLM-L12-v2"));
var clock = System.Diagnostics.Stopwatch.StartNew();
var vectors = all.Zip(embedder.Embed(all.Select(f => f.Text).ToList())).ToDictionary(p => p.First.Id, p => p.Second);
Console.WriteLine($"Embeddings en {clock.Elapsed.TotalSeconds:F0} s.");

// ---------------------------------------------------------------- Los casos

var cases = new List<(string Name, List<int> New, List<int> Previous)>();
for (var i = 1; i < documents.Count; i++)
    cases.Add(($"{documents[i].Id} tras los {i} anteriores", [i], Enumerable.Range(0, i).ToList()));
cases.Add(("Tanda: DOC-15 a DOC-21 tras DOC-01 a DOC-14", Enumerable.Range(14, documents.Count - 14).ToList(), Enumerable.Range(0, 14).ToList()));
cases.Add(("Tanda: DOC-11 a DOC-21 tras DOC-01 a DOC-10", Enumerable.Range(10, documents.Count - 10).ToList(), Enumerable.Range(0, 10).ToList()));

var rows = new List<Row>();
foreach (var (name, newDocs, previousDocs) in cases)
{
    var fresh = newDocs.SelectMany(d => documents[d].Fragments).ToList();
    var previous = previousDocs.SelectMany(d => documents[d].Fragments).ToList();
    // El parecido de cada fragmento anterior con lo nuevo: el mayor.
    var best = previous.Select(p => (p.Id, Similarity: fresh.Max(n => VectorMath.Cosine(vectors[p.Id], vectors[n.Id])))).ToList();
    var related = thresholds.ToDictionary(t => t, t => best.Where(b => b.Similarity >= t).OrderByDescending(b => b.Similarity).Select(b => b.Id).ToList());
    rows.Add(new Row(name, newDocs.Count == 1, fresh.Count, fresh.Sum(f => tokens[f.Id]), previous.Count, previous.Sum(f => tokens[f.Id]),
        related.ToDictionary(r => r.Key, r => (r.Value.Count, r.Value.Sum(id => tokens[id])))));
}

// ---------------------------------------------------------------- El catálogo validado (punto 1 de la revisión del autor)

// Qué requisitos del catálogo validado tendría que leer el modelo para ver si
// lo nuevo los contradice. Como el catálogo salió de toda la documentación, en
// cada caso cuenta como «catálogo anterior» lo que no cita nada de lo nuevo.
// Dos formas de elegirlos: a) los que citan algún fragmento relacionado (sin
// embeddings nuevos); b) los que se parecen a algún fragmento nuevo desde 0,70.
var catalogRows = new List<CatalogRow>();
var catalog = (await http.GetFromJsonAsync<JsonObject>($"/projects/{project["id"]}/deliverables/catalog"))?["content"]?["requisitos"]?.AsArray()
    .OfType<JsonObject>()
    .Select(r => (Id: r["id"]!.GetValue<string>(), Text: r["texto"]!.GetValue<string>(),
        Sources: r["fuentes"]!.AsArray().Select(s => s!.GetValue<string>()).ToList(),
        // Su línea en input/catalogo.json: los campos del esquema, en una línea.
        Tokens: InputBudget.EstimateTokens(new JsonObject
        {
            ["id"] = r["id"]!.DeepClone(), ["tipo"] = r["tipo"]!.DeepClone(), ["notacion"] = r["notacion"]!.DeepClone(),
            ["obligatoriedad"] = r["obligatoriedad"]!.DeepClone(), ["texto"] = r["texto"]!.DeepClone(),
            ["fuentes"] = r["fuentes"]!.DeepClone(), ["preguntas"] = r["preguntas"]!.DeepClone(),
        }.ToJsonString())))
    .ToList() ?? [];
if (catalog.Count > 0)
{
    var requirementVectors = catalog.Zip(embedder.Embed(catalog.Select(r => r.Text).ToList())).ToDictionary(p => p.First.Id, p => p.Second);
    foreach (var (name, newDocs, previousDocs) in cases)
    {
        var newIds = newDocs.Select(d => documents[d].Id).ToHashSet();
        var fresh = newDocs.SelectMany(d => documents[d].Fragments).ToList();
        var previous = previousDocs.SelectMany(d => documents[d].Fragments).ToList();
        var relatedFragments = previous.Where(p => fresh.Max(n => VectorMath.Cosine(vectors[p.Id], vectors[n.Id])) >= 0.70).Select(p => p.Id).ToHashSet();
        var before = catalog.Where(r => !r.Sources.Any(s => newIds.Contains(s.Split('#')[0]))).ToList();
        var bySources = before.Where(r => r.Sources.Any(relatedFragments.Contains)).ToList();
        var byMeaning = before.Where(r => fresh.Max(n => VectorMath.Cosine(requirementVectors[r.Id], vectors[n.Id])) >= 0.70).ToList();
        var both = bySources.Union(byMeaning).ToList();
        catalogRows.Add(new CatalogRow(name, newDocs.Count == 1, before.Count, before.Sum(r => r.Tokens),
            (bySources.Count, bySources.Sum(r => r.Tokens)), (byMeaning.Count, byMeaning.Sum(r => r.Tokens)), (both.Count, both.Sum(r => r.Tokens))));
    }
}

// ---------------------------------------------------------------- El informe

var md = new StringBuilder();
md.AppendLine("# Fragmentos relacionados: medición del tope (paso 112)");
md.AppendLine();
md.AppendLine($"Generado por `scripts/evaluacion/medir-fragmentos-relacionados.cs` el {DateTime.Now:dd-MM-yyyy} con el proyecto «{projectName}»: {documents.Count} documentos, {all.Count} fragmentos y unos {tokens.Values.Sum():N0} tokens de documentación. Embeddings en local ({embedder.Name}), en {clock.Elapsed.TotalSeconds:F0} s para todos los fragmentos.");
md.AppendLine();
md.AppendLine("Un fragmento anterior está **relacionado** con una iteración nueva si su parecido con alguno de sus fragmentos llega al umbral (0,70 es «parecido», el umbral del emparejamiento). Los tokens son los estimados de sus líneas en `input/documentacion.txt`.");
md.AppendLine();

void Table(IEnumerable<Row> selected)
{
    md.AppendLine("| Caso | Nuevos (tokens) | Anteriores (tokens) | Relacionados ≥ 0,70 | Tokens | % de lo anterior | ≥ 0,75 | ≥ 0,80 | ≥ 0,85 |");
    md.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
    foreach (var r in selected)
    {
        var (n70, t70) = r.Related[0.70];
        md.AppendLine(string.Create(es, $"| {r.Name} | {r.New} ({r.NewTokens:N0}) | {r.Previous} ({r.PreviousTokens:N0}) | {n70} | {t70:N0} | {(r.Previous == 0 ? 0 : 100.0 * n70 / r.Previous):F0} % | {r.Related[0.75].Count} | {r.Related[0.80].Count} | {r.Related[0.85].Count} |"));
    }
    md.AppendLine();
}

md.AppendLine("## Cada documento tras los anteriores");
md.AppendLine();
Table(rows.Where(r => r.OneDocument));
md.AppendLine("## Tandas de varios documentos");
md.AppendLine();
Table(rows.Where(r => !r.OneDocument));

md.AppendLine("## Resumen (relacionados desde 0,70)");
md.AppendLine();
md.AppendLine("| Medida | Fragmentos | Tokens |");
md.AppendLine("| --- | ---: | ---: |");
foreach (var (label, selected) in new[] { ("Un documento nuevo", rows.Where(r => r.OneDocument).ToList()), ("Todos los casos", rows) })
{
    var counts = selected.Select(r => r.Related[0.70].Count).Order().ToList();
    var sums = selected.Select(r => r.Related[0.70].Tokens).Order().ToList();
    md.AppendLine(string.Create(es, $"| {label}: mediana | {Percentile(counts, 0.5):N0} | {Percentile(sums, 0.5):N0} |"));
    md.AppendLine(string.Create(es, $"| {label}: percentil 90 | {Percentile(counts, 0.9):N0} | {Percentile(sums, 0.9):N0} |"));
    md.AppendLine(string.Create(es, $"| {label}: máximo | {counts[^1]:N0} | {sums[^1]:N0} |"));
}
md.AppendLine();

if (catalogRows.Count > 0)
{
    md.AppendLine("## Requisitos del catálogo validado relacionados con lo nuevo");
    md.AppendLine();
    md.AppendLine($"Para el punto 1 de la revisión de los prompts v0.13: qué requisitos del catálogo validado ({catalog.Count} requisitos, unos {catalog.Sum(r => r.Tokens):N0} tokens en `input/catalogo.json`) tendría que leer el modelo para ver si la documentación nueva los contradice. En cada caso, el «catálogo anterior» son los requisitos que no citan nada de lo nuevo. **a)** los que citan algún fragmento relacionado (desde 0,70); **b)** los que se parecen por significado a algún fragmento nuevo (desde 0,70); **a o b**, cualquiera de los dos.");
    md.AppendLine();
    md.AppendLine("| Caso | Catálogo anterior (tokens) | a) por sus fuentes (tokens) | b) por significado (tokens) | a o b (tokens) | % de lo anterior |");
    md.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: |");
    foreach (var r in catalogRows)
        md.AppendLine(string.Create(es, $"| {r.Name} | {r.Before} ({r.BeforeTokens:N0}) | {r.BySources.Count} ({r.BySources.Tokens:N0}) | {r.ByMeaning.Count} ({r.ByMeaning.Tokens:N0}) | {r.Both.Count} ({r.Both.Tokens:N0}) | {(r.Before == 0 ? 0 : 100.0 * r.Both.Count / r.Before):F0} % |"));
    md.AppendLine();
}

Directory.CreateDirectory("docs");
File.WriteAllText(Path.Combine("docs", "fragmentos-relacionados.md"), md.ToString());
Console.WriteLine(md.ToString());

static int Percentile(List<int> sorted, double p) => sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(p * sorted.Count) - 1)];

record CatalogRow(string Name, bool OneDocument, int Before, int BeforeTokens, (int Count, int Tokens) BySources, (int Count, int Tokens) ByMeaning, (int Count, int Tokens) Both);

record Row(string Name, bool OneDocument, int New, int NewTokens, int Previous, int PreviousTokens, Dictionary<double, (int Count, int Tokens)> Related);
