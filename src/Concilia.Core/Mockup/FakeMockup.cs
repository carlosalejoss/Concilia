using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Concilia.Core.Mockup;

// What FakeProvider writes in each run of a mockup job (prompts v0.6): in the
// design, a map with one screen per epic and a shell; in each screen, a
// section with a block per story; the integration changes nothing. Joined by
// MockupTask.Assemble, the page passes MockupTask.Validate, so the mockup step
// can be tried without a model.
public static class FakeMockup
{
    // The map: one screen per epic of input/backlog.json, with its stories.
    public static string Map(string workDirectory)
    {
        var backlog = JsonNode.Parse(File.ReadAllText(Path.Combine(workDirectory, MockupTask.BacklogFile)))!;
        var screens = new JsonArray();
        var number = 0;
        foreach (var epic in backlog["epicas"] as JsonArray ?? [])
        {
            number++;
            screens.Add(new JsonObject
            {
                ["id"] = $"P{number:00}",
                ["titulo"] = epic!["titulo"]!.GetValue<string>(),
                ["descripcion"] = "Pantalla simulada por FakeProvider.",
                ["historias"] = new JsonArray((epic["historias"] as JsonArray ?? [])
                    .Select(h => (JsonNode)h!["id"]!.GetValue<string>()).ToArray()),
            });
        }
        return new JsonObject { ["pantallas"] = screens }.ToJsonString();
    }

    // The shell: a menu with a link per screen of the map it just wrote.
    public static string App(string workDirectory)
    {
        var map = JsonNode.Parse(Map(workDirectory))!;
        var menu = string.Concat((map["pantallas"] as JsonArray ?? []).Select(s =>
            $"<li><a href=\"#{s!["id"]}\" onclick=\"mostrarPantalla('{s["id"]}')\">{Text(s["titulo"])}</a></li>"));
        return $$"""
            <!doctype html>
            <html lang="es">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Mockup simulado</title>
            <style>
              body { font-family: system-ui, sans-serif; margin: 0; display: flex; flex-wrap: wrap; }
              nav { width: 16rem; padding: 1rem; background: #f3f4f6; }
              main { flex: 1; padding: 1rem; min-width: 16rem; }
              .hu { font-size: .75rem; color: #6b7280; }
              .nota { color: #92400e; }
            </style>
            <script>
            const DATOS = { usuarios: [{ nombre: "Ana Prueba" }, { nombre: "Luis Ejemplo" }] };
            function mostrarPantalla(id) {
              document.querySelectorAll("section[data-pantalla]").forEach(s => { s.hidden = s.dataset.pantalla !== id; });
            }
            </script>
            </head>
            <body>
            <nav><ul>{{menu}}</ul></nav>
            <main>
            {{MockupTask.Placeholder}}
            </main>
            <script>
            const primera = document.querySelector("section[data-pantalla]");
            if (primera) mostrarPantalla(primera.dataset.pantalla);
            </script>
            </body>
            </html>
            """;
    }

    // A screen: a block per story of input/historias.json. broken: the first
    // attempt with simulateRepair leaves out the mark of the last story, so
    // the repair can be seen working.
    public static string Screen(string workDirectory, string screenId, bool broken)
    {
        var stories = (JsonNode.Parse(File.ReadAllText(Path.Combine(workDirectory, MockupTask.StoriesInput)))!["historias"] as JsonArray ?? [])
            .Select(h => h!).ToList();
        var html = new StringBuilder($"<section data-pantalla=\"{screenId}\" hidden>\n");
        for (var i = 0; i < stories.Count; i++)
        {
            var story = stories[i];
            var id = story["id"]!.GetValue<string>();
            var mark = broken && i == stories.Count - 1 ? "" : $" data-hu=\"{id}\"";
            html.Append($"""
                  <article{mark}>
                    <p class="hu">{id} · {Text(story["titulo"])}</p>
                    <h2>{Text(story["quiero"])}</h2>
                    <p>Como {Text(story["como"])}, para {Text(story["para"])}.</p>
                  </article>

                """);
        }
        html.Append("  <p class=\"nota\">Pantalla simulada por FakeProvider; ningún modelo ha intervenido.</p>\n</section>\n");
        return html.ToString();
    }

    private static string Text(JsonNode? node) => WebUtility.HtmlEncode(node?.GetValue<string>() ?? "");
}
