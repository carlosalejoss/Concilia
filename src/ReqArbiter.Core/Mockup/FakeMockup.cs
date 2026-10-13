using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace ReqArbiter.Core.Mockup;

// What FakeProvider writes in a mockup run (prompts v0.4: by parts): a base
// with a menu by epic and the <!-- VISTAS --> line, and one file per epic with
// a view per story of the backlog in input/backlog.json. Once joined by
// MockupTask.Assemble it passes MockupTask.Validate, so the mockup step can be
// tried without a model.
public static class FakeMockup
{
    // Writes the file of each epic in output/mockup/ and returns the base,
    // which FakeProvider writes as the expected output. broken: the first
    // attempt with simulateRepair leaves out the file of the last epic, so the
    // repair can be seen working.
    public static string WriteParts(string workDirectory, bool broken)
    {
        var backlog = JsonNode.Parse(File.ReadAllText(Path.Combine(workDirectory, MockupTask.BacklogFile)))!;
        var epics = (backlog["epicas"] as JsonArray ?? []).Select(e => e!).ToList();
        Directory.CreateDirectory(Path.Combine(workDirectory, MockupTask.PartsFolder));

        var menu = new StringBuilder();
        for (var i = 0; i < epics.Count; i++)
        {
            var epic = epics[i];
            var epicId = epic["id"]!.GetValue<string>();
            menu.Append($"<li><strong>{Text(epic["titulo"])}</strong><ul>");
            var views = new StringBuilder();
            foreach (var story in Stories(epic))
            {
                var id = story["id"]!.GetValue<string>();
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
            if (!(broken && i == epics.Count - 1))
                File.WriteAllText(Path.Combine(workDirectory, MockupTask.EpicFile(epicId)), views.ToString());
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
            <script>
            const DATOS = { usuarios: [{ nombre: "Ana Prueba" }, { nombre: "Luis Ejemplo" }] };
            function mostrar(id) {
              document.querySelectorAll("section[data-hu]").forEach(s => { s.hidden = s.dataset.hu !== id; });
            }
            </script>
            </head>
            <body>
            <nav><ul>{{menu}}</ul></nav>
            <main>
            {{MockupTask.Placeholder}}
            </main>
            <script>
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
