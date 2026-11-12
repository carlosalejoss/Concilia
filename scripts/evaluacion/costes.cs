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
//   2. cada trabajo del proyecto grande de ejemplo (clínicas veterinarias),
//      también de sus nuevas pruebas («Clinicas Veterinarias v0.11»);
//   3. el flujo completo de cada proyecto, por fase;
//   4. antes y después: la misma fase del mismo proyecto con distintas
//      versiones del prompt;
//   5. el coste por papel (A, B, árbitro, mockup) y modelo.
// Y docs/costes/consumo.md (paso 144, autor, 10-11-2026): todo el consumo, de SOL
// por la pasarela y de los modelos de OpenCode Go, por proyecto y con el coste de
// cada tarea, más lo que queda de cada límite de Go. Se regenera tras cada tarea.
// Dos costes (autor, 10-11-2026): el principal cobra las lecturas de caché a su
// precio, porque es lo que de verdad se paga; el otro, a precio completo como se
// calculaba hasta entonces, sirve para comparar con lo de antes. Las ejecuciones
// anteriores al paso 128 no guardan la caché aparte y salen igual en los dos.
// Los proyectos que no son de ejemplo, de PURE ni del autor salen anonimizados («proyecto interno 1»…).
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
// The documents of the PURE dataset are public (steps 105 and 123) and LasCuarenta is a university project
// of the author (06-11-2026): those projects keep their name too.
string[] examples = ["Clinicas Veterinarias", "Clínicas", "Banco De Alimentos", "ReservaSalas", "PURE", "LasCuarenta"];

var priceFile = JsonNode.Parse(File.ReadAllText(Path.Combine("scripts", "evaluacion", "precios.json")))!.AsObject();
var prices = priceFile
    .Where(p => !p.Key.StartsWith('_'))
    .ToDictionary(p => p.Key, p => new Price(p.Value!["entrada"]!.GetValue<double>(), p.Value["salida"]!.GetValue<double>(),
        p.Value["cache"]?.GetValue<double>(), p.Value["suscripcion"]?.GetValue<string>(), p.Value["limite_mes_go"]?.GetValue<double>(),
        p.Value["descuento_go"]?["factor"]?.GetValue<double>(),
        p.Value["descuento_go"]?["hasta"] is JsonValue until ? DateTimeOffset.Parse(until.GetValue<string>(), CultureInfo.InvariantCulture) : null));
// The limits of OpenCode Go (step 144): the start of the subscription and the share of the monthly limit per week and per 5 hours.
var go = priceFile["_go"];
DateTimeOffset? goStart = go?["inicio_suscripcion"] is JsonValue start ? DateTimeOffset.Parse(start.GetValue<string>(), CultureInfo.InvariantCulture) : null;
var goWeekShare = go?["semana"]?.GetValue<double>() ?? 0.5;
var goFiveHourShare = go?["cinco_horas"]?.GetValue<double>() ?? 0.2;
// Consumption of Go the platform does not record (cut or cancelled runs), taken from the OpenCode console.
var goAdjustments = (go?["ajustes"]?.AsArray() ?? []).Select(a => new GoAdjustment(
    DateTimeOffset.Parse(a!["fecha"]!.GetValue<string>(), CultureInfo.InvariantCulture), a["modelo"]!.GetValue<string>(),
    a["coste_limite"]!.GetValue<double>(), a["coste_lista"]!.GetValue<double>(), a["motivo"]!.GetValue<string>())).ToList();

using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(10) };
var jobs = new List<JobCost>();
var internalNumber = 0;
foreach (var project in (await http.GetFromJsonAsync<JsonArray>("/projects"))!.OrderBy(p => p!["id"]!.GetValue<int>()))
{
    var name = project!["name"]!.GetValue<string>();
    // A project of the examples, also a new run of one ("Clinicas Veterinarias v0.11"), keeps its name.
    var shown = examples.Any(e => name.StartsWith(e, StringComparison.Ordinal)) ? name : $"Proyecto interno {++internalNumber}";
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
            // Step 128: the part of the input read from the cache (only with real usage, since then).
            long? cacheRead = real ? run["cacheReadTokens"]?.GetValue<long>() : null;
            return new RunCost(run["slot"]?.GetValue<string>() ?? "", run["model"]!.GetValue<string>(), run["promptVersion"]?.GetValue<string>() ?? "",
                started, input, output, !real && input is not null, cacheRead,
                run["provider"]?.GetValue<string>() ?? "", run["startedAt"]?.GetValue<DateTimeOffset>(),
                run["attempt"]?.GetValue<int>() ?? 1, run["state"]?.GetValue<string>() ?? "");
        }).Where(r => r.Started).ToList();
        if (runCosts.Count == 0) continue;
        // What the matching of the arbiter cost: kept with the result since it is measured; for the
        // arbitrations with prompts v0.10 from before, measured now with the same computation.
        var time = report["matchingTime"];
        var measuredLater = false;
        if (time is null && report["succeeded"]!.GetValue<bool>() && runCosts.Any(r => r.Slot == "ARB" && string.CompareOrdinal(r.Prompt, "v0.10") >= 0 && r.Prompt.Length >= 5))
        {
            var matching = await http.GetAsync($"/jobs/{job["id"]}/matching");
            // Since step 100 the matching of a job is saved: read again, it is not computed, and its
            // time says nothing. Those jobs show no time (22-10-2026; measured later they took 15–37 s).
            var body = matching.IsSuccessStatusCode ? JsonNode.Parse(await matching.Content.ReadAsStringAsync())! : null;
            if (body is not null && body["fromCache"]?.GetValue<bool>() != true)
            {
                time = body["time"];
                measuredLater = true;
            }
        }
        jobs.Add(new JobCost(shown, job["kind"]!.GetValue<string>(), report["phase"]?.GetValue<string>(), report["rearbitrationOf"] is not null,
            job["createdAt"]!.GetValue<DateTimeOffset>(), report["succeeded"]!.GetValue<bool>(), report["seconds"]?.GetValue<double>(),
            report["repairs"]!.GetValue<int>(), time?["seconds"]?.GetValue<double>(), time?["cpuSeconds"]?.GetValue<double>(), measuredLater, runCosts,
            job["id"]!.GetValue<string>()));
    }
}

// The main cost: the input read from the cache at its price, the rest of the input
// (new and written to the cache) at the input price. Without the cache apart (runs
// from before step 128, or estimated) it is the full price.
double? CostOf(RunCost r)
{
    if (r.Input is not { } i || r.Output is not { } o || !prices.TryGetValue(r.Model, out var p)) return null;
    if (r.CacheRead is { } read && p.Cache is { } cache)
        return ((i - read) * p.In + read * cache + o * p.Out) / 1_000_000;
    return (i * p.In + o * p.Out) / 1_000_000;
}

// The cost at full price, as computed until 10-11-2026: to compare with what came before.
double? FullCostOf(RunCost r) =>
    r.Input is { } i && r.Output is { } o && prices.TryGetValue(r.Model, out var p) ? (i * p.In + o * p.Out) / 1_000_000 : null;

var md = new StringBuilder();
md.AppendLine("# Coste en tiempo y en dinero");
md.AppendLine();
md.AppendLine($"Generado por `scripts/evaluacion/costes.cs` el {DateTime.Now:dd-MM-yyyy HH:mm}, con los datos guardados de cada ejecución. Material para justificar los cambios del TFG también por lo que cuestan.");
md.AppendLine();
md.AppendLine("**Cómo leer las cifras:**");
md.AppendLine("- **Tokens:** los que da Claude Code al terminar cada ejecución. Una ejecución que la plataforma detuvo antes del final no tiene dato y se **estima** a partir de su consola (paso 95; error en `estimador.md`). Las cifras con estimación llevan `*`; las que tienen ejecuciones sin dato ni estimación son un mínimo y llevan `≥`.");
md.AppendLine("- **Coste:** tokens × precio del modelo (sección 1), en dólares. Desde el paso 128 la entrada leída de la caché se cobra a su precio, que es lo que de verdad se paga (autor, 10-11-2026). Las ejecuciones de antes no guardaban la caché aparte y van a precio completo. Los embeddings se calculan en local y no cuestan dinero, solo tiempo.");
md.AppendLine("- **A precio completo:** toda la entrada al precio de entrada, como se calculaba hasta el 10-11-2026. Solo sirve para comparar con lo de antes.");
md.AppendLine("- **Suscripción:** los modelos de OpenCode Go (el modelo B desde el hito 25) se pagan con la suscripción Go (10 $ al mes). Su coste se calcula por token, con los precios por uso de OpenCode: es lo que pagaría una empresa que pagase por uso.");
md.AppendLine("- **Minutos:** desde que se crea el trabajo hasta que termina. No sirven si el equipo se suspendió durante el trabajo.");
md.AppendLine();

md.AppendLine("## 1. Precios");
md.AppendLine();
md.AppendLine("| Modelo | Entrada (USD por millón de tokens) | Salida (USD por millón de tokens) | Lectura de caché (USD por millón de tokens) | Se paga con |");
md.AppendLine("| --- | --- | --- | --- | --- |");
foreach (var (model, p) in prices)
    md.AppendLine($"| {model} | {p.In.ToString("F2", es)} | {p.Out.ToString("F2", es)} | {(p.Cache is { } c ? c.ToString("F2", es) : "—")} | " +
        $"{(p.Subscription is { } sub ? $"suscripción {sub}; el coste es por token" : "pago por uso")} |");
md.AppendLine();

md.AppendLine($"## 2. Cada trabajo del proyecto grande de ejemplo ({Main}, también en sus nuevas pruebas)");
md.AppendLine();
md.AppendLine("Los trabajos de un nuevo arbitraje sin tiempo de embeddings son anteriores al paso 100: su emparejamiento se guarda desde entonces y ya no se vuelve a calcular (medido después, tardaba entre 15 y 37 s).");
md.AppendLine();
md.AppendLine("| Proyecto | Fecha | Trabajo | Prompt | Resultado | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) | A precio completo (USD) | Minutos | Reparaciones | Embeddings (s / s de CPU) |");
md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
foreach (var j in jobs.Where(j => j.Project.StartsWith(Main, StringComparison.Ordinal)).OrderBy(j => j.Created))
    md.AppendLine($"| {j.Project} | {j.Created.ToLocalTime():dd-MM HH:mm} | {Name(j)} | {Version(j)} | {(j.Succeeded ? "válido" : "fallido")} | {j.Runs.Count} | " +
        $"{Tokens(j.Runs, r => r.Input)} | {Tokens(j.Runs, r => r.Output)} | {Money(j.Runs)} | {Money(j.Runs, FullCostOf)} | {Minutes(j.Seconds)} | {j.Repairs} | " +
        $"{(j.MatchingSeconds is { } s ? $"{s.ToString("F1", es)} / {j.MatchingCpu!.Value.ToString("F1", es)}{(j.MatchingMeasuredLater ? " (medido después)" : "")}" : "—")} |");
md.AppendLine();

md.AppendLine("## 3. El flujo completo de cada proyecto, por fase");
md.AppendLine();
md.AppendLine("Solo los trabajos del flujo, no los experimentos (los nuevos arbitrajes y la ronda que lo lee todo del paso 116). Si una fase se repitió, cuentan todas sus ejecuciones: es lo que costó llegar al resultado.");
md.AppendLine();
md.AppendLine("| Proyecto | Fase | Trabajos | Tokens de entrada | Tokens de salida | Coste (USD) | A precio completo (USD) | Minutos |");
md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
foreach (var project in jobs.Where(InFlow).GroupBy(j => j.Project))
{
    foreach (var phase in project.GroupBy(Name).OrderBy(g => g.Min(j => j.Created)))
    {
        var runs = phase.SelectMany(j => j.Runs).ToList();
        md.AppendLine($"| {project.Key} | {phase.Key} | {phase.Count()} | {Tokens(runs, r => r.Input)} | {Tokens(runs, r => r.Output)} | {Money(runs)} | {Money(runs, FullCostOf)} | {Minutes(phase.Sum(j => j.Seconds ?? 0))} |");
    }
    var all = project.SelectMany(j => j.Runs).ToList();
    md.AppendLine($"| **{project.Key}** | **Total** | {project.Count()} | **{Tokens(all, r => r.Input)}** | **{Tokens(all, r => r.Output)}** | **{Money(all)}** | **{Money(all, FullCostOf)}** | **{Minutes(project.Sum(j => j.Seconds ?? 0))}** |");
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
md.AppendLine("| Papel | Modelo | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) | A precio completo (USD) | Parte del coste |");
md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
var mainRuns = jobs.Where(j => j.Project == Main && InFlow(j)).SelectMany(j => j.Runs).ToList();
var mainTotal = mainRuns.Sum(r => CostOf(r) ?? 0);
foreach (var role in mainRuns.GroupBy(r => (Role(r.Slot), r.Model)).OrderByDescending(g => g.Sum(r => CostOf(r) ?? 0)))
{
    var runs = role.ToList();
    var cost = runs.Sum(r => CostOf(r) ?? 0);
    md.AppendLine($"| {role.Key.Item1} | {role.Key.Model} | {runs.Count} | {Tokens(runs, r => r.Input)} | {Tokens(runs, r => r.Output)} | {Money(runs)} | {Money(runs, FullCostOf)} | {(mainTotal == 0 ? "—" : (cost / mainTotal).ToString("P1", es))} |");
}
md.AppendLine();

Directory.CreateDirectory(Path.Combine("docs", "costes"));
File.WriteAllText(Path.Combine("docs", "costes", "costes.md"), md.ToString(), new UTF8Encoding(false));
Console.Write(md);

// ---- docs/costes/consumo.md (step 144) ----
// Everything spent, experiments included: what counts against the limits is what
// was run, failed attempts too. The cost of OpenCode Go uses the main cost (the
// cache at its price), which is how OpenCode counts its limits, at list price.

var now = DateTimeOffset.Now;
var allRuns = jobs.SelectMany(j => j.Runs).ToList();
var goModels = prices.Where(p => p.Value.GoMonthlyLimit is not null).Select(p => p.Key).ToList();
static bool OnGo(RunCost r) => r.Provider == "OpenCodeGo";

// The current month of Go: from the day of the subscription, the same day of each month.
DateTimeOffset? goMonthStart = null;
if (goStart is { } subscribed)
{
    var cycle = subscribed;
    while (cycle.AddMonths(1) <= now) cycle = cycle.AddMonths(1);
    goMonthStart = cycle;
}

var cm = new StringBuilder();
cm.AppendLine("# Consumo de los modelos");
cm.AppendLine();
cm.AppendLine($"Generado por `scripts/evaluacion/costes.cs` el {now:dd-MM-yyyy HH:mm}, con los datos guardados de cada ejecución (paso 144). Se regenera tras cada tarea, junto con `costes.md`.");
cm.AppendLine();
cm.AppendLine("**Cómo leer las cifras:**");
cm.AppendLine("- **Todo lo que se ha ejecutado,** también los experimentos y los intentos fallidos: todo cuenta en el límite.");
cm.AppendLine("- **Coste:** tokens × precio por token de `precios.json` (sección 1 de `costes.md`), con la entrada leída de la caché a su precio. «A precio completo», como se calculaba hasta el 10-11-2026.");
cm.AppendLine("- **OpenCode Go** se paga con la suscripción Go (10 $ al mes), con un límite en dólares a precio de lista por modelo. La cifra es lo que pagaría una empresa por uso y lo que gasta del límite. Gemini 4 Argon tiene un 50 % de descuento hasta el 30-11-2026: la sección 1 descuenta del límite el precio rebajado, como hace OpenCode (comprobado con su consola); las demás secciones dan el precio de lista.");
cm.AppendLine("- `*`: con ejecuciones sin dato de consumo, estimadas a partir de su consola (paso 95). `≥`: con ejecuciones sin dato ni estimación, así que es un mínimo.");
cm.AppendLine();

cm.AppendLine("## 1. Lo que queda de cada límite de OpenCode Go");
cm.AppendLine();
if (goMonthStart is null)
    cm.AppendLine("Sin suscripción todavía: se contrata en el paso 129 y su fecha va en `precios.json` (`_go.inicio_suscripcion`). Hasta entonces no hay mes de Go; la semana y las 5 horas se calculan igual.");
else
    cm.AppendLine($"Mes de Go desde el {goMonthStart:dd-MM-yyyy} (suscripción del {goStart:dd-MM-yyyy}). La semana son los últimos 7 días y las 5 horas, las últimas 5 horas: OpenCode no dice si sus ventanas son móviles o fijas, y se comprueba con su consola.");
cm.AppendLine();
cm.AppendLine("| Modelo | Mes: gastado / límite | Mes: queda | Semana: gastado / límite | Semana: queda | 5 horas: gastado / límite | 5 horas: queda |");
cm.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
foreach (var model in goModels)
{
    var monthly = prices[model].GoMonthlyLimit!.Value;
    string Window(DateTimeOffset? from, double limit)
    {
        if (from is null) return "— | —";
        var runs = allRuns.Where(r => OnGo(r) && r.Model == model && r.StartedAt >= from).ToList();
        // What OpenCode takes from the limit: a temporary discount applies (Argon until 30-11-2026).
        var spent = runs.Sum(r => (CostOf(r) ?? 0) * (r.StartedAt <= prices[model].GoDiscountUntil ? prices[model].GoDiscount!.Value : 1));
        // Plus what the platform did not record (cut or cancelled runs), from the OpenCode console.
        var adjusted = goAdjustments.Where(a => a.Model == model && a.Date >= from).ToList();
        spent += adjusted.Sum(a => a.LimitCost);
        var mark = (runs.Any(r => r.Estimated) ? "*" : "") + (adjusted.Count > 0 ? "†" : "");
        return $"{spent.ToString("F2", es)}{mark} / {limit.ToString("F2", es)} | {Math.Max(0, limit - spent).ToString("F2", es)}";
    }
    cm.AppendLine($"| {model} | {Window(goMonthStart, monthly)} | {Window(now.AddDays(-7), monthly * goWeekShare)} | {Window(now.AddHours(-5), monthly * goFiveHourShare)} |");
}
cm.AppendLine();
if (goAdjustments.Count > 0)
{
    cm.AppendLine("`†`: incluye consumo que la plataforma no registra (ejecuciones cortadas o canceladas, sin evento final), tomado de la consola de OpenCode (`_go.ajustes` en `precios.json`):");
    cm.AppendLine();
    foreach (var a in goAdjustments.OrderByDescending(a => a.Date))
        cm.AppendLine($"- {a.Date.ToLocalTime():dd-MM-yyyy HH:mm}, {a.Model}: {a.LimitCost.ToString("F2", es)} $ del límite ({a.ListCost.ToString("F2", es)} $ a precio de lista). {a.Reason}.");
    cm.AppendLine();
}

cm.AppendLine("## 2. Gasto total, por proveedor y modelo");
cm.AppendLine();
cm.AppendLine("| Proveedor | Modelo | Ejecuciones | Tokens de entrada | Tokens de salida | De ellos, leídos de la caché | Coste (USD) | A precio completo (USD) |");
cm.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
foreach (var group in allRuns.GroupBy(r => (r.Provider, r.Model)).OrderBy(g => g.Key.Provider).ThenByDescending(g => g.Sum(r => CostOf(r) ?? 0)))
{
    var runs = group.ToList();
    var cached = runs.Where(r => r.CacheRead is not null).ToList();
    cm.AppendLine($"| {ProviderName(group.Key.Provider)} | {group.Key.Model} | {runs.Count} | {Tokens(runs, r => r.Input)} | {Tokens(runs, r => r.Output)} | " +
        $"{(cached.Count == 0 ? "—" : cached.Sum(r => r.CacheRead!.Value).ToString("N0", es))} | {Money(runs)} | {Money(runs, FullCostOf)} |");
}
cm.AppendLine();
cm.AppendLine("«De ellos, leídos de la caché»: solo las ejecuciones desde el paso 128, que lo guardan aparte.");
cm.AppendLine();

cm.AppendLine("## 3. Gasto por proyecto");
cm.AppendLine();
cm.AppendLine("| Proyecto | Tareas | Pasarela (USD) | OpenCode Go (USD) | Total (USD) | A precio completo (USD) | Última tarea |");
cm.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
var byProject = jobs.GroupBy(j => j.Project).OrderByDescending(g => g.Max(j => j.Created)).ToList();
// The mockups made with the author's subscription have no price: they go apart.
static bool Priced(RunCost r) => r.Provider != "ClaudeSubscription";
foreach (var project in byProject)
{
    var runs = project.SelectMany(j => j.Runs).ToList();
    var priced = runs.Where(Priced).ToList();
    var unpriced = runs.Count - priced.Count;
    string Or(IEnumerable<RunCost> some, Func<RunCost, double?>? cost = null) => some.Any() ? Money(some, cost) : "—";
    cm.AppendLine($"| {project.Key} | {project.Count()} | {Or(runs.Where(r => r.Provider == "LiteLlm"))} | {Or(runs.Where(OnGo))} | " +
        $"{Or(priced)}{(unpriced > 0 ? " (más el mockup con la suscripción, sin precio)" : "")} | {Or(priced, FullCostOf)} | {project.Max(j => j.Created).ToLocalTime():dd-MM-yyyy HH:mm} |");
}
cm.AppendLine();

cm.AppendLine("## 4. Cada tarea, por proyecto");
cm.AppendLine();
cm.AppendLine("De la más reciente a la más antigua. Una fila por papel; «Ejecuciones» cuenta también los intentos repetidos.");
foreach (var project in byProject)
{
    cm.AppendLine();
    cm.AppendLine($"### {project.Key}");
    cm.AppendLine();
    cm.AppendLine("| Fecha | Tarea | Resultado | Papel | Modelo | Proveedor | Ejecuciones | Tokens de entrada | Tokens de salida | Coste (USD) |");
    cm.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
    foreach (var j in project.OrderByDescending(j => j.Created))
    {
        var repeated = j.Runs.Count(r => r.Attempt > 1);
        var result = (j.Succeeded ? "válido" : "fallido") + (repeated > 0 ? $", {repeated} {(repeated == 1 ? "repetición" : "repeticiones")}" : "");
        var first = true;
        foreach (var role in j.Runs.GroupBy(r => (Role(r.Slot), r.Model, r.Provider)).OrderBy(g => g.Key.Item1))
        {
            var runs = role.ToList();
            var head = first ? $"{j.Created.ToLocalTime():dd-MM-yyyy HH:mm} | {Name(j)} (`{j.Id[..8]}`) | {result}" : " | | ";
            cm.AppendLine($"| {head} | {role.Key.Item1} | {role.Key.Model} | {ProviderName(role.Key.Provider)} | {runs.Count} | " +
                $"{Tokens(runs, r => r.Input)} | {Tokens(runs, r => r.Output)} | {Money(runs)} |");
            first = false;
        }
        cm.AppendLine($"| | **Total de la tarea** | | | | | {j.Runs.Count} | **{Tokens(j.Runs, r => r.Input)}** | **{Tokens(j.Runs, r => r.Output)}** | **{Money(j.Runs)}** |");
    }
}

File.WriteAllText(Path.Combine("docs", "costes", "consumo.md"), cm.ToString(), new UTF8Encoding(false));

// ---- Helpers ----

static string ProviderName(string provider) => provider switch
{
    "LiteLlm" => "pasarela",
    "OpenCodeGo" => "OpenCode Go (suscripción)",
    "ClaudeSubscription" => "suscripción del autor",
    _ => provider,
};

string Tokens(IEnumerable<RunCost> runs, Func<RunCost, long?> of)
{
    var list = runs.ToList();
    var sum = list.Sum(r => of(r) ?? 0).ToString("N0", es);
    if (list.Any(r => of(r) is null)) return "≥ " + sum;
    return list.Any(r => r.Estimated) ? sum + "*" : sum;
}

// cost: CostOf (the main one) unless another is given (FullCostOf).
string Money(IEnumerable<RunCost> runs, Func<RunCost, double?>? cost = null)
{
    cost ??= CostOf;
    var list = runs.ToList();
    if (list.Any(r => !prices.ContainsKey(r.Model))) return "sin precio"; // a model of the subscription (the mockup with Sonnet)
    var sum = list.Sum(r => cost(r) ?? 0).ToString("F2", es);
    if (list.Any(r => cost(r) is null)) return "≥ " + sum;
    return list.Any(r => r.Estimated) ? sum + "*" : sum;
}

// The jobs of the flow: not the experiments (a new arbitration, or the centred
// round that reads everything of step 116), which change nothing of the project.
static bool InFlow(JobCost j) => !j.Rearbitration && j.Kind != "questions-read-all";

string Minutes(double? seconds) => seconds is { } s ? (s / 60).ToString("F0", es) : "—";

static string Name(JobCost j) => j.Kind switch
{
    // Step 112: a round centred on some iterations (prompts v0.13) is another phase, not a new version of the round.
    "questions" when Version(j) == "v0.13" => "Preguntas centradas",
    "questions" => "Preguntas",
    "questions-read-all" => "Preguntas centradas, leyéndolo todo (experimental)",
    "catalog-update" => "Actualización del catálogo",
    "backlog-update" => "Actualización del backlog",
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

// Provider, StartedAt, Attempt and State: for docs/costes/consumo.md (step 144).
record RunCost(string Slot, string Model, string Prompt, bool Started, long? Input, long? Output, bool Estimated, long? CacheRead = null,
    string Provider = "", DateTimeOffset? StartedAt = null, int Attempt = 1, string State = "");

// Dollars per million tokens. Cache: reading from the cache (null if unknown).
// Subscription: the plan it is paid with (OpenCode Go), or null if paid per use.
// GoMonthlyLimit: the monthly limit of a model of OpenCode Go, in dollars at list price.
// GoDiscount and GoDiscountUntil: a temporary discount of OpenCode Go, which it applies to its limit too.
record Price(double In, double Out, double? Cache, string? Subscription, double? GoMonthlyLimit = null,
    double? GoDiscount = null, DateTimeOffset? GoDiscountUntil = null);

record JobCost(string Project, string Kind, string? Phase, bool Rearbitration, DateTimeOffset Created, bool Succeeded, double? Seconds,
    int Repairs, double? MatchingSeconds, double? MatchingCpu, bool MatchingMeasuredLater, List<RunCost> Runs, string Id = "");

// Consumption of OpenCode Go not recorded by the platform, from its console (step 130).
record GoAdjustment(DateTimeOffset Date, string Model, double LimitCost, double ListCost, string Reason);
