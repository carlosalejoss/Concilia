using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace ReqArbiter.Core.Mockup;

// What FakeProvider writes in a mockup run: a plain .html that passes
// MockupTask.Validate, with a menu by epic and one view per story of the
// backlog in input/backlog.json, so the mockup step can be tried without a model.
public static class FakeMockup
{
    // broken: the first attempt with simulateRepair, without the view of the
    // last story, so the repair can be seen working.
    public static string For(string workDirectory, bool broken)
    {
        var backlog = JsonNode.Parse(File.ReadAllText(Path.Combine(workDirectory, MockupTask.BacklogFile)))!;
        var epics = (backlog["epicas"] as JsonArray ?? []).Select(e => e!).ToList();
        var lastStory = epics.SelectMany(Stories).LastOrDefault()?["id"]?.GetValue<string>();

        var menu = new StringBuilder();
        var views = new StringBuilder();
        foreach (var epic in epics)
        {
            menu.Append($"<li><strong>{Text(epic["titulo"])}</strong><ul>");
            foreach (var story in Stories(epic))
            {
                var id = story["id"]!.GetValue<string>();
                if (broken && id == lastStory)
                    continue;
                menu.Append($"<li><a href=\"#{id}\" onclick=\"mostrar('{id}')\">{Text(story["titulo"])}</a></li>");
                views.Append($"""
                    <section data-hu="{id}" id="{id}" hidden>
                      <p class="hu">{id} · {Text(story["titulo"])}</p>
                      <h2>{Text(story["quiero"])}</h2>
                      <p>Como {Text(story["como"])}, para {Text(story["para"])}.</p>
                      <p class="nota">Vista simulada por FakeProvider; ningún modelo ha intervenido.</p>
                    </section>

                    """);
            }
            menu.Append("</ul></li>");
        }

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
            </head>
            <body>
            <nav><ul>{{menu}}</ul></nav>
            <main>
            {{views}}</main>
            <script>
            const DATOS = { usuarios: [{ nombre: "Ana Prueba" }, { nombre: "Luis Ejemplo" }] };
            function mostrar(id) {
              document.querySelectorAll("section[data-hu]").forEach(s => { s.hidden = s.dataset.hu !== id; });
            }
            const primera = document.querySelector("section[data-hu]");
            if (primera) mostrar(primera.dataset.hu);
            </script>
            </body>
            </html>
            """;
    }

    private static IEnumerable<JsonNode> Stories(JsonNode epic) => (epic["historias"] as JsonArray ?? []).Select(h => h!);

    private static string Text(JsonNode? node) => WebUtility.HtmlEncode(node?.GetValue<string>() ?? "");
}
