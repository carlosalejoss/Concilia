// Script de un solo fichero (.NET 10): sin compilación AOT, que no hace falta aquí.
#:property PublishAot=false

// Paso 87 (hito 17): calibra los dos umbrales del emparejamiento por
// significado con la muestra que etiquetó el autor, y los comprueba con el
// consenso de los árbitros (opción D, 19-10-2026).
//
// 1. Criterio del autor: para cada umbral, cuántos pares de la muestra
//    clasifica como él. «Lo mismo» frente al resto da el umbral alto; «distinto»
//    frente al resto, el bajo.
// 2. Consenso de los árbitros: en el proyecto de clínicas veterinarias hay
//    varios arbitrajes válidos sobre las mismas propuestas de A y B. Un par es
//    «juntar» para el consenso si lo junta al menos el 80 % de los árbitros, y
//    «no juntar» si lo junta como mucho el 20 %; el resto queda como dudoso.
// 3. El árbitro frente al criterio del autor: en los pares de la muestra, si el
//    árbitro del trabajo los juntó.
// 4. Acuerdo por elemento en los trabajos originales de los tres proyectos
//    ficticios: «¿está también en la otra propuesta?», según los embeddings
//    (mismo o parecido) y según el árbitro (lo juntó con alguno de la otra).
//
// Lee docs/calibracion-embeddings/muestra-para-etiquetar.csv (con la columna
// «etiqueta» rellena) y muestra-clave.json, pide a la API en marcha los
// emparejamientos de clínicas y escribe resultados.md en la misma carpeta.
//
// Uso, desde la raíz del repositorio, con la aplicación en marcha:
//   dotnet run scripts/calibracion-embeddings/calibrar.cs
//   dotnet run scripts/calibracion-embeddings/calibrar.cs -- http://localhost:8080 [csv etiquetado] [alto bajo]
// Con «alto bajo» (por ejemplo 0.90 0.70, los que fijó el autor el 19-10-2026)
// las tablas usan esos umbrales en lugar de los propuestos por la calibración.

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var api = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:8080";
var folder = Path.Combine("docs", "calibracion-embeddings");
var labelledCsv = args.Length > 1 ? args[1] : Path.Combine(folder, "muestra-para-etiquetar.csv");
var es = CultureInfo.GetCultureInfo("es-ES");

// Los umbrales que se prueban: de 0,40 a 0,98, de 0,01 en 0,01.
var candidates = Enumerable.Range(40, 59).Select(n => n / 100.0).ToList();

// ---------------------------------------------------------------- 1. Criterio del autor

var key = JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "muestra-clave.json")))!.AsArray()
    .ToDictionary(item => item!["Id"]!.GetValue<string>(), item => item!["Pair"]!);
var labels = ReadLabels(labelledCsv);
var labelled = labels
    .Where(l => key.ContainsKey(l.Key))
    .Select(l => new Labelled(l.Key, key[l.Key]["Phase"]!.GetValue<string>(), key[l.Key]["Similarity"]!.GetValue<double>(), l.Value,
        key[l.Key]["ArbiterJoined"]?.GetValue<bool>()))
    .ToList();
if (labelled.Count == 0)
    throw new InvalidOperationException($"No hay ningún par etiquetado en {labelledCsv}.");

// Umbral alto: separa «mismo» del resto. Umbral bajo: separa «distinto» del resto.
var sameScan = Scan(labelled.Select(l => (l.Similarity, l.Label == "mismo")).ToList(), candidates);
var similarScan = Scan(labelled.Select(l => (l.Similarity, l.Label != "distinto")).ToList(), candidates);
var proposedSame = Middle(sameScan);
var proposedSimilar = Middle(similarScan.Where(s => s.Threshold < proposedSame).ToList());
var fixedByAuthor = args.Length > 3;
var same = fixedByAuthor ? double.Parse(args[2], CultureInfo.InvariantCulture) : proposedSame;
var similar = fixedByAuthor ? double.Parse(args[3], CultureInfo.InvariantCulture) : proposedSimilar;

string Level(double s) => s >= same ? "mismo" : s >= similar ? "parecido" : "distinto";

// ---------------------------------------------------------------- 2. Consenso de los árbitros

using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(5) };
var projects = (await http.GetFromJsonAsync<JsonArray>("/projects"))!;
var clinics = projects.First(p => p!["name"]!.GetValue<string>() == "Clinicas Veterinarias")!;
var jobs = (await http.GetFromJsonAsync<JsonArray>($"/projects/{clinics["id"]}/jobs"))!.Select(j => j!["job"]!).ToList();

// Los arbitrajes con una salida válida del árbitro, agrupados por el trabajo del
// que salen sus propuestas: el original y sus nuevos arbitrajes. Solo los grupos
// con más de un arbitraje permiten hablar de consenso.
var views = new List<JsonNode>();
foreach (var job in jobs.Where(j => j["kind"]!.GetValue<string>() is "questions" or "catalog-v0" or "rearbitration"))
{
    var response = await http.GetAsync($"/jobs/{job["id"]}/matching?same={Invariant(same)}&similar={Invariant(similar)}");
    if (!response.IsSuccessStatusCode) continue;
    var view = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    if (view["hasArbiter"]!.GetValue<bool>())
        views.Add(view);
}
var consensus = new List<PhaseConsensus>();
foreach (var group in views.GroupBy(v => (v["rearbitrationOf"] ?? v["jobId"])!.GetValue<string>()).Where(g => g.Count() > 1))
{
    var arbitrations = group.ToList();
    var phase = arbitrations[0]["phase"]!.GetValue<string>();

    // Las mismas propuestas en todos: los mismos pares, con lo que hizo cada árbitro.
    var votes = new Dictionary<(string A, string B), (double Similarity, List<bool> Joined)>();
    foreach (var view in arbitrations)
        foreach (var pair in view["matching"]!["pairs"]!.AsArray())
        {
            var id = (pair!["aId"]!.GetValue<string>(), pair["bId"]!.GetValue<string>());
            if (!votes.TryGetValue(id, out var vote))
                votes[id] = vote = (pair["similarity"]!.GetValue<double>(), []);
            vote.Joined.Add(pair["arbiterJoined"]!.GetValue<bool>());
        }
    var models = arbitrations.GroupBy(v => v["arbiterModel"]?.GetValue<string>() ?? "?").Select(g => $"{g.Key}: {g.Count()}");
    consensus.Add(new PhaseConsensus(phase, arbitrations.Count, string.Join(", ", models),
        votes.Values.Select(v => (v.Similarity, Share: v.Joined.Count(j => j) / (double)v.Joined.Count)).ToList()));
}

// ---------------------------------------------------------------- Resultados

var md = new StringBuilder();
md.AppendLine("# Resultados de la calibración (paso 87)");
md.AppendLine();
md.AppendLine($"Generado por `scripts/calibracion-embeddings/calibrar.cs` el {DateTime.Now:dd-MM-yyyy} con {labelled.Count} pares etiquetados por el autor ({labels.Count - labelled.Count} sin clave) y los arbitrajes guardados del proyecto de clínicas veterinarias.");
md.AppendLine();

md.AppendLine("## 1. Frente al criterio del autor");
md.AppendLine();
md.AppendLine("Etiquetas de la muestra: " + string.Join(", ", labelled.GroupBy(l => l.Label).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}")) + ".");
md.AppendLine();
md.AppendLine($"Umbrales propuestos por la calibración: «lo mismo» desde {proposedSame.ToString("F2", es)} y «parecido» desde {proposedSimilar.ToString("F2", es)}. " +
    $"En cada caso, el centro del intervalo de umbrales con más aciertos: {Interval(sameScan)} para el alto y " +
    $"{Interval(similarScan.Where(r => r.Threshold < proposedSame).ToList())} para el bajo.");
md.AppendLine();
md.AppendLine(fixedByAuthor
    ? $"**Umbrales fijados por el autor, los que usan las tablas siguientes: «lo mismo» desde {same.ToString("F2", es)} y «parecido» desde {similar.ToString("F2", es)}.**"
    : "**Las tablas siguientes usan los umbrales propuestos.**");
md.AppendLine();
AppendScan(md, "Umbral alto: «mismo» frente al resto", sameScan, "mismo", "no mismo");
AppendScan(md, "Umbral bajo: «distinto» frente al resto", similarScan, "mismo o parecido", "distinto");

md.AppendLine("### Las tres etiquetas con los umbrales propuestos");
md.AppendLine();
string[] levels = ["mismo", "parecido", "distinto"];
md.AppendLine("| Autor \\ Embeddings | mismo | parecido | distinto |");
md.AppendLine("| --- | --- | --- | --- |");
foreach (var label in levels)
    md.AppendLine($"| {label} | " + string.Join(" | ", levels.Select(l => labelled.Count(p => p.Label == label && Level(p.Similarity) == l))) + " |");
var hits = labelled.Count(p => Level(p.Similarity) == p.Label);
md.AppendLine();
md.AppendLine($"Coinciden en {hits} de {labelled.Count} pares ({Percent(hits, labelled.Count)}).");
md.AppendLine();
md.AppendLine("Por fase: " + string.Join("; ", labelled.GroupBy(p => p.Phase).OrderBy(g => g.Key)
    .Select(g => $"{PhaseName(g.Key)} {g.Count(p => Level(p.Similarity) == p.Label)} de {g.Count()}")) + ".");
md.AppendLine();
md.AppendLine("Pares en los que no coinciden (para revisarlos a mano):");
md.AppendLine();
md.AppendLine("| Par | Fase | Similitud | Autor | Embeddings |");
md.AppendLine("| --- | --- | --- | --- | --- |");
foreach (var p in labelled.Where(p => Level(p.Similarity) != p.Label).OrderBy(p => p.Similarity))
    md.AppendLine($"| {p.Id} | {PhaseName(p.Phase)} | {p.Similarity.ToString("F3", es)} | {p.Label} | {Level(p.Similarity)} |");
md.AppendLine();

md.AppendLine("## 2. Frente al consenso de los árbitros (clínicas veterinarias)");
md.AppendLine();
md.AppendLine("El consenso «juntar» es un par que juntan al menos el 80 % de los árbitros, y «no juntar», uno que juntan como mucho el 20 %; el resto es dudoso. " +
    "Solo cuentan los pares que son el más parecido de uno de sus dos elementos.");
md.AppendLine();
md.AppendLine("El árbitro no solo junta lo que es «lo mismo»: también junta muchos pares «parecidos», conservando el detalle de cada uno (sección 3). " +
    "Por eso se compara su consenso con dos lecturas de los embeddings: «mismo» (desde el umbral alto) y «mismo o parecido» (desde el umbral bajo).");
md.AppendLine();
md.AppendLine("| Fase | Arbitrajes | Pares | Consenso «juntar» | Consenso «no juntar» | Dudosos | «Juntar» que los embeddings ven «mismo» | «Juntar» que ven «mismo o parecido» | «No juntar» que no ven «mismo» | «No juntar» que ven «distinto» |");
md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
foreach (var c in consensus)
{
    var yes = c.Pairs.Where(p => p.Share >= 0.8).ToList();
    var no = c.Pairs.Where(p => p.Share <= 0.2).ToList();
    md.AppendLine($"| {PhaseName(c.Phase)} | {c.Arbitrations} ({c.Models}) | {c.Pairs.Count} | {yes.Count} | {no.Count} | {c.Pairs.Count - yes.Count - no.Count} | " +
        $"{Share(yes, p => p.Similarity >= same)} | {Share(yes, p => p.Similarity >= similar)} | " +
        $"{Share(no, p => p.Similarity < same)} | {Share(no, p => p.Similarity < similar)} |");
}
md.AppendLine();
md.AppendLine("Similitud media de los pares según cuántos árbitros los juntan:");
md.AppendLine();
md.AppendLine("| Fase | Ninguno | Algunos | Todos |");
md.AppendLine("| --- | --- | --- | --- |");
foreach (var c in consensus)
{
    string Mean(IEnumerable<double> values) => values.Any() ? values.Average().ToString("F3", es) : "—";
    md.AppendLine($"| {PhaseName(c.Phase)} | {Mean(c.Pairs.Where(p => p.Share == 0).Select(p => p.Similarity))} | " +
        $"{Mean(c.Pairs.Where(p => p.Share > 0 && p.Share < 1).Select(p => p.Similarity))} | {Mean(c.Pairs.Where(p => p.Share == 1).Select(p => p.Similarity))} |");
}
md.AppendLine();

md.AppendLine("## 3. El árbitro frente al criterio del autor");
md.AppendLine();
md.AppendLine("En los pares de la muestra, si el árbitro del trabajo original juntó los dos elementos.");
md.AppendLine();
md.AppendLine("| Autor | Juntados por el árbitro | No juntados |");
md.AppendLine("| --- | --- | --- |");
foreach (var label in levels)
{
    var withArbiter = labelled.Where(p => p.Label == label && p.ArbiterJoined is not null).ToList();
    md.AppendLine($"| {label} | {withArbiter.Count(p => p.ArbiterJoined == true)} | {withArbiter.Count(p => p.ArbiterJoined == false)} |");
}
md.AppendLine();

md.AppendLine("## 4. Acuerdo por elemento con el árbitro de cada trabajo");
md.AppendLine();
md.AppendLine("En los trabajos originales de los tres proyectos ficticios, para cada elemento de A y de B: ¿está también en la otra propuesta? " +
    "Sí para los embeddings si su par es «mismo» o «parecido»; sí para el árbitro si lo juntó con alguno de la otra propuesta.");
md.AppendLine();
md.AppendLine("| Proyecto | Fase | Los dos sí | Solo embeddings | Solo árbitro | Los dos no | Acuerdo | Solo A (embeddings / árbitro) | Solo B (embeddings / árbitro) |");
md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
int[] total = [0, 0, 0, 0];
foreach (var name in new[] { "Banco De Alimentos", "Clinicas Veterinarias", "ReservaSalasHito911.v2" })
{
    var project = projects.First(p => p!["name"]!.GetValue<string>() == name)!;
    var projectJobs = (await http.GetFromJsonAsync<JsonArray>($"/projects/{project["id"]}/jobs"))!.Select(j => j!["job"]!);
    foreach (var job in projectJobs.Where(j => j["kind"]!.GetValue<string>() is "questions" or "catalog-v0" or "backlog").Reverse())
    {
        var response = await http.GetAsync($"/jobs/{job["id"]}/matching?same={Invariant(same)}&similar={Invariant(similar)}");
        if (!response.IsSuccessStatusCode) continue;
        var view = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        if (!view["hasArbiter"]!.GetValue<bool>()) continue;
        var report = JsonNode.Parse(await http.GetStringAsync($"/jobs/{job["id"]}/report"))!["elements"]!;
        var elements = view["matching"]!["elements"]!.AsArray().Select(e => (
            Embeddings: e!["level"]!.GetValue<string>() != "Different", Arbiter: e["arbiterJoined"]!.GetValue<bool>(), Slot: e["slot"]!.GetValue<string>())).ToList();
        int[] row = [elements.Count(e => e.Embeddings && e.Arbiter), elements.Count(e => e.Embeddings && !e.Arbiter),
            elements.Count(e => !e.Embeddings && e.Arbiter), elements.Count(e => !e.Embeddings && !e.Arbiter)];
        for (var i = 0; i < 4; i++) total[i] += row[i];
        var counts = view["matching"]!["counts"]!;
        md.AppendLine($"| {name} | {PhaseName(view["phase"]!.GetValue<string>())} | {row[0]} | {row[1]} | {row[2]} | {row[3]} | {Percent(row[0] + row[3], row.Sum())} | " +
            $"{counts["onlyA"]} / {report["onlyA"]} | {counts["onlyB"]} / {report["onlyB"]} |");
    }
}
md.AppendLine($"| **Total** | | {total[0]} | {total[1]} | {total[2]} | {total[3]} | **{Percent(total[0] + total[3], total.Sum())}** | | |");
md.AppendLine();
md.AppendLine("«Solo A» y «solo B» del árbitro son elementos de su unión que vienen de una sola propuesta; los de los embeddings, elementos sin ninguno «mismo» ni «parecido» en la otra.");
md.AppendLine();

File.WriteAllText(Path.Combine(folder, "resultados.md"), md.ToString(), new UTF8Encoding(false));
Console.WriteLine($"Umbrales propuestos: mismo desde {proposedSame:F2}, parecido desde {proposedSimilar:F2}. Usados: {same:F2} y {similar:F2}. Resultados en {Path.Combine(folder, "resultados.md")}");

// ---------------------------------------------------------------- Funciones

// Para cada umbral: aciertos, falsos positivos (el umbral dice sí y el autor no) y falsos negativos.
static List<ScanRow> Scan(List<(double Similarity, bool Yes)> pairs, List<double> thresholds) =>
    thresholds.Select(t => new ScanRow(t,
        pairs.Count(p => (p.Similarity >= t) == p.Yes),
        pairs.Count(p => p.Similarity >= t && !p.Yes),
        pairs.Count(p => p.Similarity < t && p.Yes),
        pairs.Count)).ToList();

// El centro del intervalo de umbrales con más aciertos (si hay varios intervalos, el primero).
static double Middle(List<ScanRow> scan)
{
    var best = scan.Max(s => s.Hits);
    var first = scan.FindIndex(s => s.Hits == best);
    var last = first;
    while (last + 1 < scan.Count && scan[last + 1].Hits == best) last++;
    return Math.Round((scan[first].Threshold + scan[last].Threshold) / 2, 2);
}

void AppendScan(StringBuilder md, string title, List<ScanRow> scan, string yes, string no)
{
    md.AppendLine($"### {title}");
    md.AppendLine();
    md.AppendLine($"| Umbral | Aciertos | Embeddings «{yes}», autor «{no}» | Embeddings «{no}», autor «{yes}» |");
    md.AppendLine("| --- | --- | --- | --- |");
    foreach (var row in scan.Where(r => Math.Round(r.Threshold * 100) % 5 == 0))
        md.AppendLine($"| {row.Threshold.ToString("F2", es)} | {row.Hits} de {row.Total} ({Percent(row.Hits, row.Total)}) | {row.FalseYes} | {row.FalseNo} |");
    md.AppendLine();
}

string Share(List<(double Similarity, double Share)> pairs, Func<(double Similarity, double Share), bool> test) =>
    $"{pairs.Count(test)} de {pairs.Count} ({Percent(pairs.Count(test), pairs.Count)})";

// Los umbrales con más aciertos, como intervalo («0,88–0,92»).
string Interval(List<ScanRow> scan)
{
    var best = scan.Max(r => r.Hits);
    var top = scan.Where(r => r.Hits == best).Select(r => r.Threshold).ToList();
    return top.Count == 1 ? top[0].ToString("F2", es) : $"{top.Min().ToString("F2", es)}–{top.Max().ToString("F2", es)}";
}

string Percent(int part, int total) => total == 0 ? "—" : (part / (double)total).ToString("P0", es);

static string Invariant(double value) => value.ToString(CultureInfo.InvariantCulture);

static string PhaseName(string phase) => phase switch
{
    "Questions" => "preguntas",
    "Catalog" => "catálogo",
    "Backlog" => "backlog",
    _ => phase,
};

// Las etiquetas del CSV, por id del par. Excel puede guardarlo con «;» o con
// «,», y en UTF-8 o en la codificación de Windows: se aceptan todas.
static Dictionary<string, string> ReadLabels(string path)
{
    var bytes = File.ReadAllBytes(path);
    string text;
    try
    {
        text = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes).TrimStart('﻿');
    }
    catch (DecoderFallbackException)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        text = Encoding.GetEncoding(1252).GetString(bytes);
    }
    var rows = ParseCsv(text);
    var header = rows[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
    int id = header.IndexOf("id"), label = header.IndexOf("etiqueta");
    var labels = new Dictionary<string, string>();
    foreach (var row in rows.Skip(1).Where(r => r.Count > Math.Max(id, label)))
    {
        var value = row[label].Trim().ToLowerInvariant();
        if (value.Length == 0) continue; // el autor no lo pudo decidir
        if (value is not ("mismo" or "parecido" or "distinto"))
            throw new InvalidDataException($"Etiqueta desconocida en {row[id]}: «{row[label]}». Valen mismo, parecido o distinto.");
        labels[row[id].Trim()] = value;
    }
    return labels;
}

// Un CSV con campos entre comillas (que pueden llevar el separador y comillas dobladas).
static List<List<string>> ParseCsv(string text)
{
    var separator = text.Split('\n')[0].Contains(';') ? ';' : ',';
    var rows = new List<List<string>>();
    var row = new List<string>();
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
        else if (c == separator) { row.Add(field.ToString()); field.Clear(); }
        else if (c == '\n') { row.Add(field.ToString().TrimEnd('\r')); field.Clear(); rows.Add(row); row = []; }
        else field.Append(c);
    }
    if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString().TrimEnd('\r')); rows.Add(row); }
    return rows;
}

record Labelled(string Id, string Phase, double Similarity, string Label, bool? ArbiterJoined);

record ScanRow(double Threshold, int Hits, int FalseYes, int FalseNo, int Total);

record PhaseConsensus(string Phase, int Arbitrations, string Models, List<(double Similarity, double Share)> Pairs);
